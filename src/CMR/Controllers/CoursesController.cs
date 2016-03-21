using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CMR.Models;
using CMR.ViewModels;
using CMR.Helpers;
using Microsoft.AspNet.Identity;
using System.Configuration;

namespace CMR.Controllers
{
    public class CoursesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        List<string> errors = new List<string>();
        List<string> msgs = new List<string>();

        // GET: Courses
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Index()
        {
            return View(db.Courses.ToList());
        }

        // GET: Courses/Details/5
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            AssignViewModel am = new AssignViewModel();
            am.Course = course;
            var roleId = db.Roles.Single(r => r.Name == "Staff").Id;
            List<ApplicationUser> staffs = db.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId)).ToList();
            am.Staffs = staffs;
            return View(am);
        }

        // GET: Courses/Create
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Create()
        {
            FacultyCourseModel fcm = new FacultyCourseModel();
            List<Faculty> faculties = db.Faculties.ToList<Faculty>();
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
                    db.Courses.Add(course);
                    db.SaveChanges();
                    UpdateFaculties(course, selectedFaculties, db);
                    return RedirectToAction("Index");
                }
            }
            else
            {
                errors.Add("Please select faculty.");
                TempData["errors"] = errors;
            }

            FacultyCourseModel fcm = new FacultyCourseModel();
            List<Faculty> faculties = db.Faculties.ToList<Faculty>();
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
            FacultyCourseModel fcm = new FacultyCourseModel();
            List<Faculty> faculties = db.Faculties.ToList<Faculty>();
            fcm.Faculties = faculties;

            Course course = db.Courses.Find(id);

            if (course == null)
            {
                return HttpNotFound();
            }
            fcm.Course = course;
            return View(fcm);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Edit([Bind(Include = "Id,Code,Name")] Course course, string[] selectedFaculties)
        {
            if (selectedFaculties != null)
            {
                if (ModelState.IsValid)
                {
                    db.Entry(course).State = EntityState.Modified;
                    UpdateFaculties(course, selectedFaculties, db);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            else
            {
                errors.Add("Please select faculty.");
                TempData["errors"] = errors;
            }

            FacultyCourseModel fcm = new FacultyCourseModel();
            List<Faculty> faculties = db.Faculties.ToList<Faculty>();
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
            Course course = db.Courses.Find(id);
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
            Course course = db.Courses.Find(id);
            db.Courses.Remove(course);
            db.SaveChanges();
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
            foreach (Faculty f in db.Faculties.ToList<Faculty>())
            {
                int pos = Array.IndexOf(selectedFaculties, f.Id.ToString());
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
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }
            ValidateAssignYear(start, end);
            if (errors.Count == 0)
            {

                ConvertHelper ch = new ConvertHelper();
                DateTime? startYear = ch.YearStringToDateTime(start);
                DateTime? endYear = ch.YearStringToDateTime(end);
                if (startYear.HasValue && endYear.HasValue)
                {
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            int sYear = startYear.GetValueOrDefault().Year;
                            int eYear = endYear.GetValueOrDefault().Year;
                            CourseAssignment ca = null;
                            if (!db.CourseAssignments.Where(c => c.Start.Year == sYear).Any(c => c.End.Year == eYear))
                            {

                                ca = new CourseAssignment(course, startYear.Value, endYear.Value);
                                db.CourseAssignments.Add(ca);
                            }
                            else
                            {
                                ca = db.CourseAssignments.Where(c => c.Start.Year == sYear)
                                        .Single(c => c.End.Year == eYear);
                            }


                            if (ca != null)
                            {
                                ApplicationUser leader = db.Users.Find(cl);
                                if (leader != null)
                                {
                                    AssignManagerAddOrUpdate(ca, leader, "cl");
                                }

                                ApplicationUser manager = db.Users.Find(cm);
                                if (manager != null)
                                {
                                    AssignManagerAddOrUpdate(ca, manager, "cm");
                                }
                            }
                            db.SaveChanges();
                            transaction.Commit();
                            msgs.Add("Assign Completed");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            errors.Add("Assign Error");
                        }
                    }
                }

            }
            TempData["msgs"] = msgs;
            TempData["errors"] = errors;
            return RedirectToAction("Details", new { id = id });
        }

        [AccessDeniedAuthorize(Roles = "Staff")]
        public ActionResult Assigned()
        {
            var cUser = User.Identity.GetUserId();
            List<CourseAssignmentManager> courseAssignments =
                db.CourseAssignmentManagers.
                Include(cam => cam.CourseAssignment).
                Where(cam => cam.User.Id == cUser).ToList();
            return View(courseAssignments);
        }

        private void AssignManagerAddOrUpdate(CourseAssignment ca, ApplicationUser user, string role)
        {
            if (db.CourseAssignmentManagers.Where(c => c.CourseAssignment.Id == ca.Id).Any(c => c.Role == role))
            {
                CourseAssignmentManager cam =
                    db.CourseAssignmentManagers.Where(c => c.CourseAssignment.Id == ca.Id).Single(c => c.Role == role);
                cam.User = user;
            }
            else
            {
                CourseAssignmentManager cam = new CourseAssignmentManager(role, user, ca);
                db.CourseAssignmentManagers.Add(cam);
            }
        }

        private void ValidateAssignYear(string start, string end)
        {
            try
            {
                if (start == "" || end == "")
                {
                    errors.Add("Assign Error! Please enter Academic Year");
                }
                else {
                    int minYear = Convert.ToInt32(ConfigurationManager.AppSettings["MinYear"]);
                    int maxYear = Convert.ToInt32(ConfigurationManager.AppSettings["MaxYear"]);
                    int intStart = Convert.ToInt32(start);
                    int intEnd = Convert.ToInt32(end);
                    if (intEnd < intStart)
                    {
                        errors.Add("End year must greator than Start year");
                    }
                    if (intStart < minYear || intStart > maxYear)
                    {
                        errors.Add("Assign Error! Start year out of range " + minYear + " - " + maxYear);
                    }
                    if (intEnd < minYear || intEnd > maxYear)
                    {
                        errors.Add("Assign Error! End year out of range " + minYear + " - " + maxYear);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add("Wrong Year");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
