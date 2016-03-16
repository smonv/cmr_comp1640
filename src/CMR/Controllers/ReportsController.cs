using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using CMR.Models;
using CMR.ViewModels;
using CMR.Helpers;
using System.Net.Mail;
using System.IO;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity.Owin;
using System.Collections.Generic;
using System;

namespace CMR.Controllers
{
    [AccessDeniedAuthorize(Roles = "Staff")]
    public class ReportsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        List<string> errors = new List<string>();
        // GET: Reports
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            return View(db.Reports.Where(r => r.Assignment.Manager.Id == userId).ToList());
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
            if (db.Reports.Any(r => r.Assignment.Id == ca.Id))
            {
                errors.Add("Report for this course and academic session already exists");
                TempData["errors"] = errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            var rvm = new ReportViewModel();
            rvm.CourseAssignment = ca;
            return View(rvm);
        }

        // POST: Reports/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(int id, int totalStudent, string comment, string action,
            int meanCw1, int meanCw2, int meanExam, int medianCw1, int medianCw2, int medianExam,
            int badCw1, int averageCw1, int goodCw1, int badCw2, int averageCw2, int goodCw2, int badExam, int averageExam, int goodExam)
        {
            var report = new Report(totalStudent, comment, action);

            var ca = db.CourseAssignments.Find(id);
            if (db.Reports.Any(r => r.Assignment.Id == ca.Id))
            {
                errors.Add("Report for this course and academic session already exists");
                TempData["errors"] = errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    report.Assignment = ca;
                    db.Reports.Add(report);

                    db.ReportStatistical.AddRange(new List<ReportStatistical>(new ReportStatistical[] {
                        new ReportStatistical(meanCw1, medianCw1, "cw1", report),
                        new ReportStatistical(meanCw2, medianCw2, "cw2", report),
                        new ReportStatistical(meanExam, medianExam, "exam", report)
                    }));

                    db.ReportDistribution.AddRange(new List<ReportDistribution>(new ReportDistribution[] {
                        new ReportDistribution(badCw1, averageCw1, goodCw1, "cw1", report),
                        new ReportDistribution(badCw2, averageCw2, goodCw2, "cw2", report),
                        new ReportDistribution(badExam, averageExam, goodExam, "exam", report)
                    }));
                    db.SaveChanges();
                    transaction.Commit();
                    await SendEmail(report);
                    return Redirect("/Courses/Assigned");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var rvm = new ReportViewModel();
                    rvm.CourseAssignment = ca;
                    rvm.Report = report;
                    errors.Add(ex.Message);
                    TempData["errors"] = errors;
                    return View(rvm);
                }
            }
        }

        // GET: Reports/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var report = db.Reports.Find(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            if (report.IsApproved)
            {
                errors.Add("You cannot edit approved report");
                TempData["errors"] = errors;
                return Redirect(Url.Action("Index", "Reports"));
            }
            var rvm = new ReportViewModel();
            rvm.Report = report;
            rvm.CourseAssignment = report.Assignment;
            return View(rvm);
        }

        // POST: Reports/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, int totalStudent, string comment, string action,
            int meanCw1, int meanCw2, int meanExam, int medianCw1, int medianCw2, int medianExam,
            int badCw1, int averageCw1, int goodCw1, int badCw2, int averageCw2, int goodCw2, int badExam, int averageExam, int goodExam)
        {
            var report = db.Reports.Find(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            if (report.IsApproved)
            {
                errors.Add("You cannot edit approved report");
                TempData["errors"] = errors;
                return Redirect(Url.Action("Index", "Reports"));
            }
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    report.TotalStudent = totalStudent;
                    report.Comment = comment;
                    report.Action = action;
                    foreach (var statistical in report.Statisticals)
                    {
                        if (statistical.Type == "cw1")
                        {
                            statistical.Mean = meanCw1;
                            statistical.Median = medianCw1;
                        }
                        else if (statistical.Type == "cw2")
                        {
                            statistical.Mean = meanCw2;
                            statistical.Median = medianCw2;
                        }
                        else if (statistical.Type == "exam")
                        {
                            statistical.Mean = meanExam;
                            statistical.Median = medianExam;
                        }

                    }

                    foreach (var distribution in report.Distributions)
                    {
                        if (distribution.Type == "cw1")
                        {
                            distribution.Bad = badCw1;
                            distribution.Average = averageCw1;
                            distribution.Good = goodCw1;
                        }
                        else if (distribution.Type == "cw2")
                        {
                            distribution.Bad = badCw2;
                            distribution.Average = averageCw2;
                            distribution.Good = goodCw2;
                        }
                        else if (distribution.Type == "exam")
                        {
                            distribution.Bad = badExam;
                            distribution.Average = averageExam;
                            distribution.Good = goodExam;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();
                    await SendEmail(report);
                    return RedirectToAction("Edit", new { id = report.Id });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var rvm = new ReportViewModel();
                    rvm.Report = report;
                    rvm.CourseAssignment = report.Assignment;
                    errors.Add(ex.Message);
                    TempData["errors"] = errors;
                    return View(rvm);
                }
            }
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

        public ActionResult Approve(int? id)
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
            var userId = User.Identity.GetUserId();
            if (!CheckCourseManager(report))
            {
                return Redirect(Url.Action("Denied", "Account"));
            }
            report.IsApproved = true;
            db.SaveChanges();
            return RedirectToAction("Details", new { id = id });
        }

        public ActionResult UnApprove(int? id)
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
            var userId = User.Identity.GetUserId();
            if (!CheckCourseManager(report))
            {
                return Redirect(Url.Action("Denied", "Account"));
            }
            report.IsApproved = false;
            db.SaveChanges();
            return RedirectToAction("Details", new { id = id });
        }

        public bool CheckCourseManager(Report report)
        {
            var userId = User.Identity.GetUserId();
            return report.Assignment.Course.Managers.Where(m => m.Role == "cm").Any(m => m.Manager.Id == userId);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private async Task SendEmail(Report report)
        {
            var cm = report.Assignment.Course.Managers.Single(u => u.Role == "cm").Manager;
            var subject = report.Assignment.Start.Year + " - " + report.Assignment.End.Year;
            var reportUrl = Url.Action("Details", "Reports", new { id = report.Id }, protocol: Request.Url.Scheme);
            var body = report.Assignment.Course.Name + " " +
                report.Assignment.Start.Year + " - " +
                report.Assignment.End.Year +
                " have new report. Click <a href='" + reportUrl + "'>here</a>";
            var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            await userManager.SendEmailAsync(cm.Id, subject, body);
        }
    }
}
