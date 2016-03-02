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
    [AccessDeniedAuthorize(Roles = "Staff")]
    public class ReportsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Reports
        public ActionResult Index()
        {
            return View(db.Reports.ToList());
        }

        // GET: Reports/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Report report = db.Reports.Find(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            return View(report);
        }

        // GET: Reports/Create
        public ActionResult Create(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            CourseAssignment ca = db.CourseAssignments.Find(id);
            if (ca == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            ReportViewModel rvm = new ReportViewModel();
            rvm.CourseAssignment = ca;
            return View(rvm);
        }

        // POST: Reports/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title,Content")] Report report, int id)
        {
            CourseAssignment ca = db.CourseAssignments.Find(id);
            if (ModelState.IsValid)
            {
                report.Assignment = ca;
                db.Reports.Add(report);
                ReportNotification rn = CreateNotify(ca, report);
                db.ReportNotifications.Add(rn);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                ReportViewModel rvm = new ReportViewModel();
                rvm.CourseAssignment = ca;
                rvm.Report = report;
            }

            return View(report);
        }

        // GET: Reports/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Report report = db.Reports.Find(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            return View(report);
        }

        // POST: Reports/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Content")] Report report)
        {
            if (ModelState.IsValid)
            {
                db.Entry(report).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(report);
        }

        // GET: Reports/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Report report = db.Reports.Find(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            return View(report);
        }

        // POST: Reports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Report report = db.Reports.Find(id);
            db.Reports.Remove(report);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private ReportNotification CreateNotify(CourseAssignment ca, Report r)
        {
            ReportNotification rn = new ReportNotification();
            rn.Message = "New report in " + ca.Course.Code + " : " + ca.Start + " - " + ca.End;
            rn.Read = false;
            rn.Report = r;
            return rn;
        }

        public ActionResult Notification()
        {
            ApplicationUser currentUser = db.Users.Find(User.Identity.GetUserId());
            string userId = User.Identity.GetUserId();
            List<ReportNotification> rns = db.ReportNotifications
                .Where(rn => rn.Read == false)
                .Where(rn => rn.Report.Assignment.Course.Managers.Any(m => m.Manager.Id == userId))
                //.Where(r => r.Report.Assignment.Role == "cm")
                .ToList<ReportNotification>();
            return View(rns);
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
