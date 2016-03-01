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

namespace CMR.Controllers
{
    public class CoursesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

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
            return View(course);
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
            if (ModelState.IsValid)
            {
                db.Courses.Add(course);
                db.SaveChanges();
                UpdateFaculties(course, selectedFaculties, db);
                return RedirectToAction("Index");
            }

            return View(course);
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
            if (ModelState.IsValid)
            {
                db.Entry(course).State = EntityState.Modified;
                UpdateFaculties(course, selectedFaculties, db);
                db.SaveChanges();
            }
            return RedirectToAction("Edit", new { id = course.Id });
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

        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Assign(int? id)
        {
            AssignViewModel am = new AssignViewModel();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Course course = db.Courses.Find(id);
            if(course == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            am.Course = course;
            var roleId = db.Roles.Single(r => r.Name == "Staff").Id;
            List<ApplicationUser> staffs = db.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId)).ToList();
            am.Staffs = staffs;
            if (course == null)
            {
                return HttpNotFound();
            }
            return View(am);
        }

        [HttpPost, ActionName("Assign")]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult AssignConfirm(int id, string staff, string role)
        {
            Course course = db.Courses.Find(id);
            ApplicationUser user = db.Users.Find(staff);
            CourseAssignment ca = new CourseAssignment();
            ca.Course = course;
            ca.Manager = user;
            ca.Role = role;
            ca.Start = DateTime.Now;
            ca.End = DateTime.Now;
            db.CourseAssignments.Add(ca);
            db.SaveChanges();
            TempData["message"] = "Assign Completed";
            return RedirectToAction("Assign", new { id = id });
        }

        [AccessDeniedAuthorize(Roles = "Staff")]
        public ActionResult Assigned()
        {
            ApplicationUser currentUser = db.Users.Find(User.Identity.GetUserId());
            return View(currentUser);
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
