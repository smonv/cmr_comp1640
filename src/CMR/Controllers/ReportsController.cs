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
        List<string> msgs = new List<string>();

        // GET: Reports
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            //return View(db.Reports.Where(r => r.Assignment.Course.Managers.Any(m => m.Manager.Id == userId)).ToList());

            return View(db.Reports.ToList());
        }

        // GET: Reports/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (!db.Reports.Any(r => r.Id == id.Value))
            {
                return HttpNotFound();
            }
            Report report = db.Reports.Include(r => r.Assignment).Single(r => r.Id == id.Value);
            report.Assignment.Managers =
                db.CourseAssignmentManagers.Include(cam => cam.User).Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();

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
            ca.Managers = db.CourseAssignmentManagers.Include(cam => cam.User).Where(cam => cam.CourseAssignment.Id == ca.Id).ToList();
            if (CheckCourseManager(ca))
            {
                errors.Add("Course Manager cannot create report.");
                TempData["errors"] = errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            if (db.Reports.Any(r => r.Assignment.Id == ca.Id))
            {
                errors.Add("Report for this course and academic session already exists.");
                TempData["errors"] = errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            var rvm = new ReportViewModel();
            rvm.CourseAssignment = ca;
            rvm.CourseAssignment.Managers = db.CourseAssignmentManagers.Include(cam => cam.User)
                    .Where(cam => cam.CourseAssignment.Id == ca.Id)
                    .ToList();
            return View(rvm);
        }

        // POST: Reports/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(int id, int totalStudent, string action,
            int meanCw1, int meanCw2, int meanExam, int medianCw1, int medianCw2, int medianExam,
            int badCw1, int averageCw1, int goodCw1, int badCw2, int averageCw2, int goodCw2, int badExam, int averageExam, int goodExam)
        {
            var report = new Report(totalStudent, action);

            var ca = db.CourseAssignments.Find(id);
            ca.Managers = db.CourseAssignmentManagers.Include(cam => cam.User).Where(cam => cam.CourseAssignment.Id == ca.Id).ToList();
            if (CheckCourseManager(ca))
            {
                errors.Add("Course Manager cannot create report.");
                TempData["errors"] = errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
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
                    //await SendEmail(report);
                    return Redirect("/Courses/Assigned");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var rvm = new ReportViewModel();
                    rvm.CourseAssignment = ca;
                    rvm.Report = report;
                    rvm.CourseAssignment.Managers = db.CourseAssignmentManagers.Include(cam => cam.User)
                        .Where(cam => cam.CourseAssignment.Id == ca.Id).ToList();
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
            rvm.CourseAssignment.Managers = db.CourseAssignmentManagers.Include(cam => cam.User)
                        .Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();
            return View(rvm);
        }

        // POST: Reports/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, int totalStudent, string action,
            int meanCw1, int meanCw2, int meanExam, int medianCw1, int medianCw2, int medianExam,
            int badCw1, int averageCw1, int goodCw1, int badCw2, int averageCw2, int goodCw2, int badExam, int averageExam, int goodExam)
        {
            if (!db.Reports.Any(r => r.Id == id))
            {
                return HttpNotFound();
            }
            var report = db.Reports.Include(r => r.Assignment).Single(r => r.Id == id);

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
                    //await SendEmail(report);
                    return RedirectToAction("Edit", new { id = report.Id });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var rvm = new ReportViewModel();
                    rvm.Report = report;
                    rvm.CourseAssignment = report.Assignment;
                    rvm.CourseAssignment.Managers = db.CourseAssignmentManagers.Include(cam => cam.User)
                        .Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();
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

            if (!db.Reports.Any(r => r.Id == id))
            {
                return HttpNotFound();
            }

            Report report = db.Reports.Include(r => r.Assignment).Single(r => r.Id == id.Value);
            report.Assignment.Managers =
                db.CourseAssignmentManagers.Include(cam => cam.User).
                Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();

            if (!CheckCourseManager(report.Assignment))
            {
                return Redirect(Url.Action("Denied", "Account"));
            }
            report.IsApproved = true;
            db.SaveChanges();
            msgs.Add("Report Approved");
            TempData["msgs"] = msgs;
            return RedirectToAction("Details", new { id = id });
        }

        public ActionResult UnApprove(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!db.Reports.Any(r => r.Id == id))
            {
                return HttpNotFound();
            }

            Report report = db.Reports.Include(r => r.Assignment).Single(r => r.Id == id.Value);
            report.Assignment.Managers =
                db.CourseAssignmentManagers.Include(cam => cam.User).
                Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();

            if (!CheckCourseManager(report.Assignment))
            {
                return Redirect(Url.Action("Denied", "Account"));
            }
            report.IsApproved = false;
            db.SaveChanges();
            msgs.Add("Report unapprove");
            TempData["msgs"] = msgs;
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comment(int id, string comment)
        {
            Report report = db.Reports.Find(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            if (!CanComment(report))
            {
                errors.Add("You don't have permission to comment.");
            }
            else {
                if (comment != "")
                {
                    var cUser = db.Users.Find(User.Identity.GetUserId());
                    db.ReportComments.Add(new ReportComment(comment, report, cUser));
                    db.SaveChanges();
                    msgs.Add("New comment added");
                }
                else
                {
                    errors.Add("Please enter comment content!");
                }
            }
            TempData["msgs"] = msgs;
            TempData["errors"] = errors;
            return RedirectToAction("Details", new { id = report.Id });
        }

        public bool CheckCourseManager(CourseAssignment ca)
        {
            var cUser = User.Identity.GetUserId();
            return ca.Managers.Where(m => m.Role == "cm").Any(m => m.User.Id == cUser);
        }

        public bool CanComment(Report report)
        {
            var result = false;
            var cUser = User.Identity.GetUserId();
            result = db.CourseAssignmentManagers.
                Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).
                Any(cam => cam.User.Id == cUser);
            if (!result)
            {
                result =
                    db.FacultyAssignmentManagers.Where(
                        fam => fam.FacultyAssignment.Faculty.Courses.Any(c => c.Id == report.Assignment.Course.Id))
                        .Any(fam => fam.User.Id == cUser);
            }
            return result;
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
            //var cm = report.Assignment.Course.Managers.Single(u => u.Role == "cm").Manager;
            var subject = report.Assignment.Start.Year + " - " + report.Assignment.End.Year;
            var reportUrl = Url.Action("Details", "Reports", new { id = report.Id }, protocol: Request.Url.Scheme);
            var body = report.Assignment.Course.Name + " " +
                report.Assignment.Start.Year + " - " +
                report.Assignment.End.Year +
                " have new report. Click <a href='" + reportUrl + "'>here</a>";
            var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            await userManager.SendEmailAsync("aaa", subject, body);
        }
    }
}
