using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CMR.Models;
using CMR.Helpers;
using CMR.ViewModels;
using Microsoft.AspNet.Identity;

namespace CMR.Controllers
{
    
    public class FacultiesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        List<string> errors = new List<string>();
        List<string> msgs = new List<string>();

        // GET: Faculties
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Index()
        {
            return View(db.Faculties.ToList());
        }

        // GET: Faculties/Details/5
        [AccessDeniedAuthorize(Roles = "Administrator")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Faculty faculty = db.Faculties.Find(id);
            if (faculty == null)
            {
                return HttpNotFound();
            }
            FacultyAssignmentModel fam = new FacultyAssignmentModel();
            fam.Faculty = faculty;
            var roleId = db.Roles.Single(r => r.Name == "Staff").Id;
            List<ApplicationUser> staffs = db.Users.Where(u => u.Roles.Any(r => r.RoleId == roleId)).ToList();
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
                db.Faculties.Add(faculty);
                db.SaveChanges();
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
            Faculty faculty = db.Faculties.Find(id);
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
                db.Entry(faculty).State = EntityState.Modified;
                db.SaveChanges();
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
            Faculty faculty = db.Faculties.Find(id);
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
            Faculty faculty = db.Faculties.Find(id);
            db.Faculties.Remove(faculty);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [AccessDeniedAuthorize(Roles = "Staff")]
        public ActionResult Assigned()
        {
            var cUser = User.Identity.GetUserId();
            List<FacultyAssignmentManager> facultyAssignments = db.FacultyAssignmentManagers.
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
            Faculty faculty = db.Faculties.Find(id);
            if (faculty == null)
            {
                return HttpNotFound();
            }

            ValidateAssignUser(pvc, dlt);

            if (errors.Count == 0)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        FacultyAssignment fa = null;
                        if (db.FacultyAssignments.Any(f => f.Faculty.Id == faculty.Id))
                        {
                            fa = db.FacultyAssignments.Single(f => f.Faculty.Id == faculty.Id);
                        }
                        else
                        {
                            fa = new FacultyAssignment(faculty);
                            db.FacultyAssignments.Add(fa);
                        }

                        if (fa != null)
                        {
                            ApplicationUser pvcUser = db.Users.Find(pvc);
                            if (pvcUser != null)
                            {
                                AssignAddOrUpdate(fa, pvcUser, "pvc");
                            }
                            ApplicationUser dltUser = db.Users.Find(dlt);
                            if (dltUser != null)
                            {
                                AssignAddOrUpdate(fa,dltUser, "dlt");
                            }
                        }
                        db.SaveChanges();
                        transaction.Commit();
                        msgs.Add("Assign Complete");
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        errors.Add("Error! Assign not complete.");
                    }
                }
            }
            TempData["errors"] = errors;
            TempData["msgs"] = msgs;   
            return RedirectToAction("Details", new {id = faculty.Id});
        }

        public void AssignAddOrUpdate(FacultyAssignment fa, ApplicationUser user, string role)
        {
            if (db.FacultyAssignmentManagers.Where(fam => fam.FacultyAssignment.Id == fa.Id)
                .Any(fam => fam.Role == role))
            {
                FacultyAssignmentManager fam =
                    db.FacultyAssignmentManagers.Where(f => f.FacultyAssignment.Id == fa.Id).Single(f => f.Role == role);
                fam.User = user;
            }
            else
            {
                FacultyAssignmentManager fam = new FacultyAssignmentManager(role, user, fa);
                db.FacultyAssignmentManagers.Add(fam);
            }
        }
        private void ValidateAssignUser(string pvc, string dlt)
        {
            if (pvc == "" && dlt == "")
            {
                errors.Add("Please select Pro-Vice Chancellor or Director of Learning and Quality or both.");
            }
            else {
                if (pvc == dlt)
                {
                    errors.Add("Pro-Vice Chancellor and Director of Learning and Quality cannot be the same person.");
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
