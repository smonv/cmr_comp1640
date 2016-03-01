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

namespace CMR.Controllers
{
    public class CoursesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Courses
        public ActionResult Index()
        {
            return View(db.Courses.ToList());
        }

        // GET: Courses/Details/5
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
