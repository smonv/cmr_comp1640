using System;
using System.Collections.Generic;
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
    [AccessDeniedAuthorize(Roles = "Staff")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _msgs = new List<string>();

        // GET: Reports
        public ActionResult Index()
        {
            var cUser = User.Identity.GetUserId();
            return View(_db.Reports.Where(r => r.Assignment.Managers.Any(m => m.User.Id == cUser)).ToList());
        }

        // GET: Reports/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (!_db.Reports.Any(r => r.Id == id.Value))
            {
                return HttpNotFound();
            }
            var report = _db.Reports.Include(r => r.Assignment).Single(r => r.Id == id.Value);
            report.Assignment.Managers =
                _db.CourseAssignmentManagers.Include(cam => cam.User)
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
            var ca = _db.CourseAssignments.Find(id);
            if (ca == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }
            ca.Managers =
                _db.CourseAssignmentManagers.Include(cam => cam.User)
                    .Where(cam => cam.CourseAssignment.Id == ca.Id)
                    .ToList();
            if (CheckCourseManager(ca))
            {
                _errors.Add("Course Manager cannot create report.");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            if (_db.Reports.Any(r => r.Assignment.Id == ca.Id))
            {
                _errors.Add("Report for this course and academic session already exists.");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            var rvm = new ReportViewModel();
            rvm.CourseAssignment = ca;
            rvm.CourseAssignment.Managers = _db.CourseAssignmentManagers.Include(cam => cam.User)
                .Where(cam => cam.CourseAssignment.Id == ca.Id)
                .ToList();
            return View(rvm);
        }

        // POST: Reports/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int id, int totalStudent, string action,
            int meanCw1, int meanCw2, int meanExam, int medianCw1, int medianCw2, int medianExam,
            int badCw1, int averageCw1, int goodCw1, int badCw2, int averageCw2, int goodCw2, int badExam,
            int averageExam, int goodExam)
        {
            var report = new Report(totalStudent, action);

            var ca = _db.CourseAssignments.Find(id);
            ca.Managers =
                _db.CourseAssignmentManagers.Include(cam => cam.User)
                    .Where(cam => cam.CourseAssignment.Id == ca.Id)
                    .ToList();
            if (CheckCourseManager(ca))
            {
                _errors.Add("Course Manager cannot create report.");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            if (_db.Reports.Any(r => r.Assignment.Id == ca.Id))
            {
                _errors.Add("Report for this course and academic session already exists");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Assigned", "Courses"));
            }
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    report.Assignment = ca;
                    _db.Reports.Add(report);

                    _db.ReportStatistical.AddRange(new List<ReportStatistical>(new[]
                    {
                        new ReportStatistical(meanCw1, medianCw1, "cw1", report),
                        new ReportStatistical(meanCw2, medianCw2, "cw2", report),
                        new ReportStatistical(meanExam, medianExam, "exam", report)
                    }));

                    _db.ReportDistribution.AddRange(new List<ReportDistribution>(new[]
                    {
                        new ReportDistribution(badCw1, averageCw1, goodCw1, "cw1", report),
                        new ReportDistribution(badCw2, averageCw2, goodCw2, "cw2", report),
                        new ReportDistribution(badExam, averageExam, goodExam, "exam", report)
                    }));
                    _db.SaveChanges();
                    transaction.Commit();
                    BuildEmail(report, "Create", User.Identity.GetUserId(), false);
                    return Redirect("/Courses/Assigned");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var rvm = new ReportViewModel();
                    rvm.CourseAssignment = ca;
                    rvm.Report = report;
                    rvm.CourseAssignment.Managers = _db.CourseAssignmentManagers.Include(cam => cam.User)
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
            var report = _db.Reports.Find(id);
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
            rvm.CourseAssignment.Managers = _db.CourseAssignmentManagers.Include(cam => cam.User)
                .Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();
            return View(rvm);
        }

        // POST: Reports/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, int totalStudent, string action,
            int meanCw1, int meanCw2, int meanExam, int medianCw1, int medianCw2, int medianExam,
            int badCw1, int averageCw1, int goodCw1, int badCw2, int averageCw2, int goodCw2, int badExam,
            int averageExam, int goodExam)
        {
            if (!_db.Reports.Any(r => r.Id == id))
            {
                return HttpNotFound();
            }
            var report = _db.Reports.Include(r => r.Assignment).Single(r => r.Id == id);

            if (report.IsApproved)
            {
                _errors.Add("You cannot edit approved report");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Index", "Reports"));
            }

            using (var transaction = _db.Database.BeginTransaction())
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

                    _db.SaveChanges();
                    transaction.Commit();
                    BuildEmail(report, "Edit", User.Identity.GetUserId(), false);
                    return RedirectToAction("Edit", new {id = report.Id});
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    var rvm = new ReportViewModel();
                    rvm.Report = report;
                    rvm.CourseAssignment = report.Assignment;
                    rvm.CourseAssignment.Managers = _db.CourseAssignmentManagers.Include(cam => cam.User)
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
            var report = _db.Reports.Find(id);
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
            var report = _db.Reports.Find(id);
            _db.Reports.Remove(report);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Approve(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!_db.Reports.Any(r => r.Id == id))
            {
                return HttpNotFound();
            }

            var report = _db.Reports.Include(r => r.Assignment).Single(r => r.Id == id.Value);
            report.Assignment.Managers =
                _db.CourseAssignmentManagers.Include(cam => cam.User).
                    Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();

            if (!CheckCourseManager(report.Assignment))
            {
                return Redirect(Url.Action("Denied", "Account"));
            }
            report.IsApproved = true;
            report.ApproveAt = DateTime.Now;

            _db.SaveChanges();
            _msgs.Add("Report Approved");
            BuildEmail(report, "Approve", User.Identity.GetUserId(), false);
            TempData["msgs"] = _msgs;
            return RedirectToAction("Details", new {id});
        }

        public ActionResult UnApprove(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (!_db.Reports.Any(r => r.Id == id))
            {
                return HttpNotFound();
            }

            var report = _db.Reports.Include(r => r.Assignment).Single(r => r.Id == id.Value);
            report.Assignment.Managers =
                _db.CourseAssignmentManagers.Include(cam => cam.User).
                    Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();

            if (!CheckCourseManager(report.Assignment))
            {
                return Redirect(Url.Action("Denied", "Account"));
            }
            report.IsApproved = false;
            _db.SaveChanges();
            _msgs.Add("Report unapprove");
            BuildEmail(report, "Unapprove", User.Identity.GetUserId(), false);
            TempData["msgs"] = _msgs;
            return RedirectToAction("Details", new {id});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comment(int id, string comment)
        {
            var report = _db.Reports.Find(id);
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
                    var cUser = _db.Users.Find(User.Identity.GetUserId());
                    _db.ReportComments.Add(new ReportComment(comment, report, cUser));
                    _db.SaveChanges();
                    _msgs.Add("New comment added");
                    BuildEmail(report, "Comment", User.Identity.GetUserId(), false);
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
            result = _db.CourseAssignmentManagers.
                Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).
                Any(cam => cam.User.Id == cUser);
            if (!result)
            {
                result =
                    _db.FacultyAssignmentManagers.Where(
                        fam => fam.FacultyAssignment.Faculty.Courses.Any(c => c.Id == report.Assignment.Course.Id))
                        .Any(fam => fam.User.Id == cUser);
            }
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        private void BuildEmail(Report report, string action, string exceptUserId, bool onlyCourseAssignment)
        {
            var reportUrl = Url.Action("Details", "Reports", new {id = report.Id}, Request.Url.Scheme);
            var subject = "";

            if (action == "Create")
            {
                subject = "New report for " + report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year;
            }
            else if (action == "Approve")
            {
                subject = report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year
                          + " have been approved";
                ;
            }
            else if (action == "Unapprove")
            {
                subject = report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year
                          + " have been unapproved";
            }
            else if (action == "Edit")
            {
                subject = report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year
                          + " have been edited";
            }
            else if (action == "Comment")
            {
                subject = "New comment in report " + report.Assignment.Course.Code + " - "
                          + report.Assignment.Course.Name + " : "
                          + report.Assignment.Start.Year + " - " + report.Assignment.End.Year;
            }

            var cams = _db.CourseAssignmentManagers.Include(cam => cam.User)
                .Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();

            foreach (var cam in cams)
            {
                if (cam.User != null)
                {
                    if (cam.User.Id != exceptUserId)
                    {
                        BackgroundJob.Enqueue(() => SendEmail(cam.User.Email, subject, reportUrl));
                    }
                }
            }

            if (!onlyCourseAssignment)
            {
                var fams = _db.FacultyAssignmentManagers.Include(fam => fam.User)
                    .Where(fam => fam.FacultyAssignment.Faculty.Courses.Any(c => c.Id == report.Assignment.Course.Id))
                    .ToList();
                foreach (var fam in fams)
                {
                    if (fam.User != null)
                    {
                        if (fam.User.Id != exceptUserId)
                        {
                            BackgroundJob.Enqueue(() => SendEmail(fam.User.Email, subject, reportUrl));
                        }
                    }
                }
            }
        }

        public void SendEmail(string to, string subject, string callbackUrl)
        {
            var viewsPath = Path.GetFullPath(HostingEnvironment.MapPath(@"~/Views/Emails"));
            var engines = new ViewEngineCollection();
            engines.Add(new FileSystemRazorViewEngine(viewsPath));

            var emailService = new Postal.EmailService(engines);

            var email = new ReportEmail
            {
                To = to,
                Subject = subject,
                CallbackUrl = callbackUrl
            };
            emailService.Send(email);
        }
    }
}