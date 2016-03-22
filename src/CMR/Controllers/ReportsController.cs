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
    [AccessDeniedAuthorize(Roles = "Staff")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _msgs = new List<string>();

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
                db.CourseAssignmentManagers.Include(cam => cam.User)
                    .Where(cam => cam.CourseAssignment.Id == report.Assignment.Id)
                    .ToList();

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
            ca.Managers =
                db.CourseAssignmentManagers.Include(cam => cam.User)
                    .Where(cam => cam.CourseAssignment.Id == ca.Id)
                    .ToList();
            if (CheckCourseManager(ca))
            {
                _errors.Add("Course Manager cannot create report.");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            if (db.Reports.Any(r => r.Assignment.Id == ca.Id))
            {
                _errors.Add("Report for this course and academic session already exists.");
                TempData["errors"] = _errors;
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
            int badCw1, int averageCw1, int goodCw1, int badCw2, int averageCw2, int goodCw2, int badExam,
            int averageExam, int goodExam)
        {
            var report = new Report(totalStudent, action);

            var ca = db.CourseAssignments.Find(id);
            ca.Managers =
                db.CourseAssignmentManagers.Include(cam => cam.User)
                    .Where(cam => cam.CourseAssignment.Id == ca.Id)
                    .ToList();
            if (CheckCourseManager(ca))
            {
                _errors.Add("Course Manager cannot create report.");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            if (db.Reports.Any(r => r.Assignment.Id == ca.Id))
            {
                _errors.Add("Report for this course and academic session already exists");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    report.Assignment = ca;
                    db.Reports.Add(report);

                    db.ReportStatistical.AddRange(new List<ReportStatistical>(new[]
                    {
                        new ReportStatistical(meanCw1, medianCw1, "cw1", report),
                        new ReportStatistical(meanCw2, medianCw2, "cw2", report),
                        new ReportStatistical(meanExam, medianExam, "exam", report)
                    }));

                    db.ReportDistribution.AddRange(new List<ReportDistribution>(new[]
                    {
                        new ReportDistribution(badCw1, averageCw1, goodCw1, "cw1", report),
                        new ReportDistribution(badCw2, averageCw2, goodCw2, "cw2", report),
                        new ReportDistribution(badExam, averageExam, goodExam, "exam", report)
                    }));
                    db.SaveChanges();
                    transaction.Commit();
                    await SendEmail(report, "Create", User.Identity.GetUserId());
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
                    _errors.Add(ex.Message);
                    TempData["errors"] = _errors;
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
                _errors.Add("You cannot edit approved report");
                TempData["errors"] = _errors;
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
            int badCw1, int averageCw1, int goodCw1, int badCw2, int averageCw2, int goodCw2, int badExam,
            int averageExam, int goodExam)
        {
            if (!db.Reports.Any(r => r.Id == id))
            {
                return HttpNotFound();
            }
            var report = db.Reports.Include(r => r.Assignment).Single(r => r.Id == id);

            if (report.IsApproved)
            {
                _errors.Add("You cannot edit approved report");
                TempData["errors"] = _errors;
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
                    await SendEmail(report, "Edit", User.Identity.GetUserId());
                    return RedirectToAction("Edit", new {id = report.Id});
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var rvm = new ReportViewModel();
                    rvm.Report = report;
                    rvm.CourseAssignment = report.Assignment;
                    rvm.CourseAssignment.Managers = db.CourseAssignmentManagers.Include(cam => cam.User)
                        .Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();
                    _errors.Add(ex.Message);
                    TempData["errors"] = _errors;
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

        public async Task<ActionResult> Approve(int? id)
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
            _msgs.Add("Report Approved");
            await SendEmail(report, "Approve", User.Identity.GetUserId());
            TempData["msgs"] = _msgs;
            return RedirectToAction("Details", new {id});
        }

        public async Task<ActionResult> UnApprove(int? id)
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
            _msgs.Add("Report unapprove");
            await SendEmail(report, "Unapprove", User.Identity.GetUserId());
            TempData["msgs"] = _msgs;
            return RedirectToAction("Details", new {id});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Comment(int id, string comment)
        {
            Report report = db.Reports.Find(id);
            if (report == null)
            {
                return HttpNotFound();
            }
            if (!CanComment(report))
            {
                _errors.Add("You don't have permission to comment.");
            }
            else
            {
                if (comment != "")
                {
                    var cUser = db.Users.Find(User.Identity.GetUserId());
                    db.ReportComments.Add(new ReportComment(comment, report, cUser));
                    db.SaveChanges();
                    _msgs.Add("New comment added");
                    await SendEmail(report, "Comment", User.Identity.GetUserId());
                }
                else
                {
                    _errors.Add("Please enter comment content!");
                }
            }
            TempData["msgs"] = _msgs;
            TempData["errors"] = _errors;
            return RedirectToAction("Details", new {id = report.Id});
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

        private async Task SendEmail(Report report, string action, string exceptUserId)
        {
            List<CourseAssignmentManager> cams = db.CourseAssignmentManagers.Include(cam => cam.User)
                .Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();
            List<FacultyAssignmentManager> fams = db.FacultyAssignmentManagers.Include(fam => fam.User)
                .Where(fam => fam.FacultyAssignment.Faculty.Courses.Any(c => c.Id == report.Assignment.Course.Id))
                .ToList();
            ApplicationUser cl = cams.Single(cam => cam.Role == "cl").User;
            ApplicationUser cm = cams.Single(cam => cam.Role == "cm").User;
            ApplicationUser dlt = fams.Single(fam => fam.Role == "dlt").User;
            List<ApplicationUser> receivers = new List<ApplicationUser>(new[] {cl, cm, dlt});

            var reportUrl = Url.Action("Details", "Reports", new {id = report.Id}, Request.Url.Scheme);
            var subject = "";
            var body = "";
            var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            if (action == "Create")
            {
                subject = "New report in " + report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year;

                body = report.Assignment.Course.Name + " " +
                       report.Assignment.Start.Year + " - " +
                       report.Assignment.End.Year +
                       " have new report. Click <a href='" + reportUrl + "'>here</a> to view details";
            }
            else if (action == "Approve")
            {
                subject = report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year
                          + " have been approved";

                body = report.Assignment.Course.Name + " " +
                       report.Assignment.Start.Year + " - " +
                       report.Assignment.End.Year +
                       " have been approved by " + cm.UserName + ". Click <a href='" + reportUrl +
                       "'>here</a> to view details";
            }
            else if (action == "Unapprove")
            {
                subject = report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year
                          + " have been unapproved";

                body = report.Assignment.Course.Name + " " +
                       report.Assignment.Start.Year + " - " +
                       report.Assignment.End.Year +
                       " have been unapproved by " + cm.UserName + ". Click <a href='" + reportUrl +
                       "'>here</a> to view details";
            }
            else if (action == "Edit")
            {
                subject = report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year
                          + " have been edited";

                body = report.Assignment.Course.Name + " " +
                       report.Assignment.Start.Year + " - " +
                       report.Assignment.End.Year +
                       " have been edited by " + cl.UserName + ". Click <a href='" + reportUrl +
                       "'>here</a> to view details";
            }
            else if (action == "Comment")
            {
                subject = "New report comment in " + report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year;

                body = report.Assignment.Course.Name + " " +
                       report.Assignment.Start.Year + " - " +
                       report.Assignment.End.Year +
                       " have new report comment. Click <a href='" + reportUrl + "'>here</a> to view details";
            }

            foreach (var receiver in receivers)
            {
                if (receiver.Id != exceptUserId)
                {
                    await userManager.SendEmailAsync(receiver.Id, subject, body);
                }
            }
        }
    }
}