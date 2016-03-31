using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Hosting;
using System.Web.Mvc;
using CMR.Custom;
using CMR.EmailModels;
using CMR.Models;
using CMR.ViewModels;
using Hangfire;
using Microsoft.AspNet.Identity;
using Postal;

namespace CMR.Controllers
{
    public class FacultiesController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _msgs = new List<string>();

        // GET: Faculties
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Index()
        {
            var faculties =
                _db.Faculties.Include(f => f.FacultyAssignment.Select(fa => fa.Managers.Select(m => m.User))).ToList();
            return View(faculties);
        }

        // GET: Faculties/Details/5
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var faculty = _db.Faculties.Find(id);
            if (faculty == null)
            {
                return HttpNotFound();
            }
            var fam = new FacultyAssignmentModel();
            fam.Faculty = faculty;
            var roleId = _db.Roles.Single(r => r.Name == "Staff").Id;
            var staffs = _db.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId)).ToList();
            fam.Staffs = staffs;
            return View(fam);
        }

        // GET: Faculties/Create
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Faculties/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Create([Bind(Include = "Id,Name")] Faculty faculty)
        {
            if (ModelState.IsValid)
            {
                if (_db.Faculties.Any(f => f.Name == faculty.Name))
                {
                    _errors.Add(faculty.Name + " already exists.");
                    TempData["errors"] = _errors;
                    return RedirectToAction("Create");
                }

                _db.Faculties.Add(faculty);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(faculty);
        }

        // GET: Faculties/Edit/5
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var faculty = _db.Faculties.Find(id);
            if (faculty == null)
            {
                return HttpNotFound();
            }
            return View(faculty);
        }

        // POST: Faculties/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Edit([Bind(Include = "Id,Name")] Faculty faculty)
        {
            if (ModelState.IsValid)
            {
                _db.Entry(faculty).State = EntityState.Modified;
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            if (_db.Faculties.Any(f => f.Name == faculty.Name))
            {
                _errors.Add(faculty.Name + " already exists.");
                TempData["errors"] = _errors;
            }
            return View(faculty);
        }

        [AccessDeniedAuthorize(Roles = "Staff")]
        public ActionResult Assigned()
        {
            var cUser = User.Identity.GetUserId();
            var facultyAssignments = _db.FacultyAssignmentManagers.
                Include(fam => fam.FacultyAssignment).
                Where(fam => fam.User.Id == cUser).ToList();
            return View(facultyAssignments);
        }

        // POST: Faculties/Assign/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Assign(int id, string pvc, string dlt)
        {
            var faculty = _db.Faculties.Find(id);
            if (faculty == null)
            {
                return HttpNotFound();
            }

            ValidateAssignUser(pvc, dlt);

            if (_errors.Count == 0)
            {
                using (var transaction = _db.Database.BeginTransaction())
                {
                    try
                    {
                        FacultyAssignment fa = null;
                        if (_db.FacultyAssignments.Any(f => f.Faculty.Id == faculty.Id))
                        {
                            fa = _db.FacultyAssignments.Single(f => f.Faculty.Id == faculty.Id);
                        }
                        else
                        {
                            fa = new FacultyAssignment(faculty);
                            _db.FacultyAssignments.Add(fa);
                        }

                        if (fa != null)
                        {
                            var pvcUser = _db.Users.Find(pvc);
                            if (pvcUser != null)
                            {
                                if (!CheckAssignExists(fa, pvcUser))
                                {
                                    AssignAddOrUpdate(fa, pvcUser, "pvc");
                                }
                                else
                                {
                                    _errors.Add(
                                        "Pro-Vice Chancellor and Director of Learning and Quality cannot be the same person.");
                                }
                            }
                            var dltUser = _db.Users.Find(dlt);
                            if (dltUser != null)
                            {
                                if (!CheckAssignExists(fa, dltUser))
                                {
                                    AssignAddOrUpdate(fa, dltUser, "dlt");
                                }
                                else
                                {
                                    _errors.Add(
                                        "Pro-Vice Chancellor and Director of Learning and Quality cannot be the same person.");
                                }
                            }
                        }
                        if (_errors.Count == 0)
                        {
                            _db.SaveChanges();
                            transaction.Commit();
                            _msgs.Add("Assign Complete");
                            BuildMail(new[] {pvc, dlt}, fa);
                        }
                        else
                        {
                            transaction.Rollback();
                        }
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        _errors.Add("Error! Assign not complete.");
                    }
                }
            }
            TempData["errors"] = _errors;
            TempData["msgs"] = _msgs;
            return RedirectToAction("Details", new {id = faculty.Id});
        }

        public void AssignAddOrUpdate(FacultyAssignment fa, ApplicationUser user, string role)
        {
            if (_db.FacultyAssignmentManagers.Where(fam => fam.FacultyAssignment.Id == fa.Id)
                .Any(fam => fam.Role == role))
            {
                var fam =
                    _db.FacultyAssignmentManagers.Where(f => f.FacultyAssignment.Id == fa.Id)
                        .Single(f => f.Role == role);
                fam.User = user;
            }
            else
            {
                var fam = new FacultyAssignmentManager(role, user, fa);
                _db.FacultyAssignmentManagers.Add(fam);
            }
        }

        private bool CheckAssignExists(FacultyAssignment fa, ApplicationUser user)
        {
            var result =
                _db.FacultyAssignmentManagers.Where(fam => fam.FacultyAssignment.Id == fa.Id)
                    .Any(fam => fam.User.Id == user.Id);
            return result;
        }

        private void ValidateAssignUser(string pvc, string dlt)
        {
            if (pvc == "" && dlt == "")
            {
                _errors.Add("Please select Pro-Vice Chancellor or Director of Learning and Quality or both.");
            }
            else
            {
                if (pvc == dlt)
                {
                    _errors.Add("Pro-Vice Chancellor and Director of Learning and Quality cannot be the same person.");
                }
            }
        }

        private void BuildMail(string[] ids, FacultyAssignment fa)
        {
            var faculty = fa.Faculty.Name;
            var subject = "Faculty Assignment";
            var callbackUrl = Url.Action("Assigned", "Faculties", null, Request.Url.Scheme);

            foreach (var user in ids.Select(id => _db.Users.Find(id)).Where(user => user != null))
            {
                BackgroundJob.Enqueue(() => SendMail(user.Email, subject, callbackUrl, faculty));
            }
        }

        public void SendMail(string to, string subject, string callbackUrl, string faculty)
        {
            var viewsPath = Path.GetFullPath(HostingEnvironment.MapPath(@"~/Views/Emails"));
            var engines = new ViewEngineCollection();
            engines.Add(new FileSystemRazorViewEngine(viewsPath));

            var emailService = new Postal.EmailService(engines);

            var email = new FacultyAssignEmail
            {
                To = to,
                Subject = subject,
                CallbackUrl = callbackUrl,
                FacultyInfo = faculty
            };
            emailService.Send(email);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}