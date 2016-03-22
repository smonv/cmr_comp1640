using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using CMR.Helpers;
using CMR.Models;
using CMR.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

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
            return View(_db.Faculties.ToList());
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
            return View(faculty);
        }

        // GET: Faculties/Delete/5
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Delete(int? id)
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

        // POST: Faculties/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult DeleteConfirmed(int id)
        {
            var faculty = _db.Faculties.Find(id);
            _db.Faculties.Remove(faculty);
            _db.SaveChanges();
            return RedirectToAction("Index");
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
        public async Task<ActionResult> Assign(int id, string pvc, string dlt)
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
                                AssignAddOrUpdate(fa, pvcUser, "pvc");
                            }
                            var dltUser = _db.Users.Find(dlt);
                            if (dltUser != null)
                            {
                                AssignAddOrUpdate(fa, dltUser, "dlt");
                            }
                        }
                        _db.SaveChanges();
                        transaction.Commit();
                        _msgs.Add("Assign Complete");
                        await SendMail(new[] {pvc, dlt}, fa);
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
                    _db.FacultyAssignmentManagers.Where(f => f.FacultyAssignment.Id == fa.Id).Single(f => f.Role == role);
                fam.User = user;
            }
            else
            {
                var fam = new FacultyAssignmentManager(role, user, fa);
                _db.FacultyAssignmentManagers.Add(fam);
            }
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

        private async Task SendMail(string[] ids, FacultyAssignment fa)
        {
            var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var faculty = fa.Faculty;
            var subject = "You have been assigned to manage: " + faculty.Name;
            var callbackUrl = Url.Action("Assigned", "Faculties", Request.Url.Scheme);
            var body = "You can view your faculties in <a href='" + callbackUrl + "'>Faculties Assigned List</a>";
            foreach (var id in ids)
            {
                await userManager.SendEmailAsync(id, subject, body);
            }
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