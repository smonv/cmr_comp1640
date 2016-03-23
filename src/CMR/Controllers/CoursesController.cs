using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Hosting;
using System.Web.Mvc;
using CMR.EmailModels;
using CMR.Helpers;
using CMR.Models;
using CMR.ViewModels;
using Hangfire;
using Microsoft.AspNet.Identity;
using Postal;

namespace CMR.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _msgs = new List<string>();

        // GET: Courses
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Index()
        {
            return View(_db.Courses.ToList());
        }

        // GET: Courses/Details/5
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var course = _db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            var am = new AssignViewModel();
            am.Course = course;
            var roleId = _db.Roles.Single(r => r.Name == "Staff").Id;
            var staffs = _db.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId)).ToList();
            am.Staffs = staffs;
            return View(am);
        }

        // GET: Courses/Create
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Create()
        {
            var fcm = new FacultyCourseModel();
            var faculties = _db.Faculties.ToList();
            fcm.Faculties = faculties;
            fcm.Course = new Course();
            return View(fcm);
        }

        // POST: Courses/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Create([Bind(Include = "Id,Code,Name")] Course course, string[] selectedFaculties)
        {
            if (selectedFaculties != null)
            {
                if (ModelState.IsValid)
                {
                    _db.Courses.Add(course);
                    _db.SaveChanges();
                    UpdateFaculties(course, selectedFaculties, _db);
                    return RedirectToAction("Index");
                }
            }
            else
            {
                _errors.Add("Please select faculty.");
                TempData["errors"] = _errors;
            }

            var fcm = new FacultyCourseModel();
            var faculties = _db.Faculties.ToList();
            fcm.Faculties = faculties;
            fcm.Course = new Course();

            return View(fcm);
        }

        // GET: Courses/Edit/5
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var fcm = new FacultyCourseModel();
            var faculties = _db.Faculties.ToList();
            fcm.Faculties = faculties;

            var course = _db.Courses.Find(id);

            if (course == null)
            {
                return HttpNotFound();
            }
            fcm.Course = course;
            return View(fcm);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Edit([Bind(Include = "Id,Code,Name")] Course course, string[] selectedFaculties)
        {
            if (selectedFaculties != null)
            {
                if (ModelState.IsValid)
                {
                    _db.Entry(course).State = EntityState.Modified;
                    UpdateFaculties(course, selectedFaculties, _db);
                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            else
            {
                _errors.Add("Please select faculty.");
                TempData["errors"] = _errors;
            }

            var fcm = new FacultyCourseModel();
            var faculties = _db.Faculties.ToList();
            fcm.Faculties = faculties;
            fcm.Course = course;
            return View(fcm);
        }

        // GET: Courses/Delete/5
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var course = _db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult DeleteConfirmed(int id)
        {
            var course = _db.Courses.Find(id);
            _db.Courses.Remove(course);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void UpdateFaculties(Course course, string[] selectedFaculties, ApplicationDbContext context)
        {
            if (selectedFaculties == null)
            {
                return;
            }
            context.Entry(course).Collection(c => c.Faculties).Load();
            if (course.Faculties == null)
            {
                course.Faculties = new List<Faculty>();
            }
            var courseFaculties = course.Faculties.Select(f => f.Id);
            foreach (var f in _db.Faculties.ToList())
            {
                var pos = Array.IndexOf(selectedFaculties, f.Id.ToString());
                if (pos > -1)
                {
                    if (!courseFaculties.Contains(f.Id))
                    {
                        course.Faculties.Add(f);
                    }
                }
                else
                {
                    if (courseFaculties.Contains(f.Id))
                    {
                        course.Faculties.Remove(f);
                    }
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Assign(int id, string cl, string cm, string start, string end)
        {
            var course = _db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            ValidateAssignUser(cl, cm);
            ValidateAssignYear(start, end);
            if (_errors.Count == 0)
            {
                var ch = new ConvertHelper();
                var startYear = ch.YearStringToDateTime(start);
                var endYear = ch.YearStringToDateTime(end);
                if (startYear.HasValue && endYear.HasValue)
                {
                    using (var transaction = _db.Database.BeginTransaction())
                    {
                        try
                        {
                            var sYear = startYear.GetValueOrDefault().Year;
                            var eYear = endYear.GetValueOrDefault().Year;
                            CourseAssignment ca = null;
                            if (
                                !_db.CourseAssignments.Where(c => c.Start.Year == sYear)
                                    .Where(c => c.End.Year == eYear)
                                    .Any(c => c.Course.Id == course.Id))
                            {
                                ca = new CourseAssignment(course, startYear.Value, endYear.Value);
                                _db.CourseAssignments.Add(ca);
                            }
                            else
                            {
                                ca = _db.CourseAssignments.Where(c => c.Start.Year == sYear)
                                    .Where(c => c.End.Year == eYear).
                                    Single(c => c.Course.Id == course.Id);
                            }


                            if (ca != null)
                            {
                                var leader = _db.Users.Find(cl);
                                if (leader != null)
                                {
                                    AssignManagerAddOrUpdate(ca, leader, "cl");
                                }

                                var manager = _db.Users.Find(cm);
                                if (manager != null)
                                {
                                    AssignManagerAddOrUpdate(ca, manager, "cm");
                                }
                            }
                            _db.SaveChanges();
                            transaction.Commit();
                            _msgs.Add("Assign Completed");
                            BuildMail(new[] {cl, cm}, ca);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _errors.Add("Assign Error");
                        }
                    }
                }
            }
            TempData["msgs"] = _msgs;
            TempData["errors"] = _errors;
            return RedirectToAction("Details", new {id});
        }

        [AccessDeniedAuthorize(Roles = "Staff")]
        public ActionResult Assigned()
        {
            var cUser = User.Identity.GetUserId();
            var courseAssignments =
                _db.CourseAssignmentManagers.
                    Include(cam => cam.CourseAssignment).
                    Where(cam => cam.User.Id == cUser).ToList();
            return View(courseAssignments);
        }

        private void AssignManagerAddOrUpdate(CourseAssignment ca, ApplicationUser user, string role)
        {
            if (_db.CourseAssignmentManagers.Where(c => c.CourseAssignment.Id == ca.Id).Any(c => c.Role == role))
            {
                var cam =
                    _db.CourseAssignmentManagers.Where(c => c.CourseAssignment.Id == ca.Id).Single(c => c.Role == role);
                cam.User = user;
            }
            else
            {
                var cam = new CourseAssignmentManager(role, user, ca);
                _db.CourseAssignmentManagers.Add(cam);
            }
        }

        private void ValidateAssignYear(string start, string end)
        {
            try
            {
                if (start == "" || end == "")
                {
                    _errors.Add("Please enter Academic Year");
                }
                else
                {
                    var minYear = Convert.ToInt32(ConfigurationManager.AppSettings["MinYear"]);
                    var maxYear = Convert.ToInt32(ConfigurationManager.AppSettings["MaxYear"]);
                    var intStart = Convert.ToInt32(start);
                    var intEnd = Convert.ToInt32(end);
                    if (intEnd < intStart)
                    {
                        _errors.Add("End year must greator than Start year");
                    }
                    if (intStart < minYear || intStart > maxYear)
                    {
                        _errors.Add("Start year out of range " + minYear + " - " + maxYear);
                    }
                    if (intEnd < minYear || intEnd > maxYear)
                    {
                        _errors.Add("End year out of range " + minYear + " - " + maxYear);
                    }
                }
            }
            catch (Exception ex)
            {
                _errors.Add("Wrong Year");
            }
        }

        private void ValidateAssignUser(string cl, string cm)
        {
            if (cl == "" && cm == "")
            {
                _errors.Add("Please select Course Leader or Manager or both.");
            }
            else
            {
                if (cl == cm)
                {
                    _errors.Add("Leader and Manager cannot be the same person.");
                }
            }
        }

        private void BuildMail(string[] ids, CourseAssignment ca)
        {
            var course = ca.Course;
            var subject = "Course Assignment";
            var courseinfo = course.Code + " - " + course.Name + " : " +
                             ca.Start.Year + " - " + ca.End.Year;
            var callbackUrl = Url.Action("Assigned", "Courses", null, Request.Url.Scheme);

            foreach (var user in ids.Select(id => _db.Users.Find(id)).Where(user => user != null))
            {
                BackgroundJob.Enqueue(() => SendMail(user.Email, subject, callbackUrl, courseinfo));
            }
        }

        public void SendMail(string to, string subject, string callbackUrl, string course)
        {
            var viewsPath = Path.GetFullPath(HostingEnvironment.MapPath(@"~/Views/Emails"));
            var engines = new ViewEngineCollection();
            engines.Add(new FileSystemRazorViewEngine(viewsPath));

            var emailService = new Postal.EmailService(engines);

            var email = new CourseAssignEmail
            {
                To = to,
                Subject = subject,
                CallbackUrl = callbackUrl,
                CourseInfo = course
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