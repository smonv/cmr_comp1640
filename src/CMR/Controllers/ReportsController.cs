using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Hosting;
using System.Web.Mvc;
using CMR.Custom;
using CMR.EmailModels;
using CMR.Models;
using CMR.ViewModels;
using Hangfire;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Postal;

namespace CMR.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _msgs = new List<string>();

        // GET: Reports
        [AccessDeniedAuthorize(Roles = "Staff")]
        public ActionResult Index()
        {
            var cUser = User.Identity.GetUserId();
            var reports = _db.Reports.Where(r => r.Assignment.Managers.Any(m => m.User.Id == cUser)).ToList();
            var enumerable = reports.Concat(
                _db.Reports.Where(
                    r =>
                        r.Assignment.Course.Faculties.Any(
                            f => f.FacultyAssignment.Any(fa => fa.Managers.Any(m => m.User.Id == cUser))))
                    .Where(r => r.IsApproved)
                    .ToList());
            return View(enumerable);
        }

        // GET: Reports/Details/5
        [AccessDeniedAuthorize(Roles = "Staff,Guest")]
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
        [AccessDeniedAuthorize(Roles = "Staff")]
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
                _db.CourseAssignmentManagers.Include(cam => cam.User).Include(cam => cam.CourseAssignment.Course)
                    .Where(cam => cam.CourseAssignment.Id == ca.Id)
                    .ToList();
            if (CheckCourseManager(ca) || CheckDlt(ca.Course))
            {
                _errors.Add("Only Course Leader can create report.");
                TempData["errors"] = _errors;
                return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Assigned", "Courses"));
            }
            if (_db.Reports.Any(r => r.Assignment.Id == ca.Id))
            {
                _errors.Add("Report for this course and academic session already exists.");
                TempData["errors"] = _errors;
                return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Assigned", "Courses"));
            }
            var rvm = new ReportViewModel();
            rvm.CourseAssignment = ca;
            rvm.CourseAssignment.Managers = _db.CourseAssignmentManagers.Include(cam => cam.User)
                .Where(cam => cam.CourseAssignment.Id == ca.Id)
                .ToList();
            return View(rvm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Staff")]
        public ActionResult Create(int id, int? totalStudent, string action,
            int? meanCw1, int? meanCw2, int? meanExam, int? medianCw1, int? medianCw2, int? medianExam,
            int? badCw1, int? averageCw1, int? goodCw1, int? badCw2, int? averageCw2, int? goodCw2, int? badExam,
            int? averageExam, int? goodExam)
        {
            var report = new Report(totalStudent.GetValueOrDefault(), action);

            var ca = _db.CourseAssignments.Find(id);
            ca.Managers =
                _db.CourseAssignmentManagers.Include(cam => cam.User)
                    .Include(cam => cam.CourseAssignment)
                    .Include(cam => cam.CourseAssignment.Course)
                    .Where(cam => cam.CourseAssignment.Id == ca.Id)
                    .ToList();
            if (CheckCourseManager(ca) || CheckDlt(ca.Course))
            {
                _errors.Add("Only Course Leader can create report.");
                TempData["errors"] = _errors;
                return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Assigned", "Courses"));
            }
            if (_db.Reports.Any(r => r.Assignment.Id == ca.Id))
            {
                _errors.Add("Report for this course and academic session already exists");
                TempData["errors"] = _errors;
                return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Assigned", "Courses"));
            }
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    report.Assignment = ca;
                    _db.Reports.Add(report);

                    _db.ReportStatistical.AddRange(new List<ReportStatistical>(new[]
                    {
                        new ReportStatistical(meanCw1.GetValueOrDefault(), medianCw1.GetValueOrDefault(), "cw1", report),
                        new ReportStatistical(meanCw2.GetValueOrDefault(), medianCw2.GetValueOrDefault(), "cw2", report),
                        new ReportStatistical(meanExam.GetValueOrDefault(), medianExam.GetValueOrDefault(), "exam",
                            report)
                    }));

                    _db.ReportDistribution.AddRange(new List<ReportDistribution>(new[]
                    {
                        new ReportDistribution(badCw1.GetValueOrDefault(), averageCw1.GetValueOrDefault(),
                            goodCw1.GetValueOrDefault(), "cw1", report),
                        new ReportDistribution(badCw2.GetValueOrDefault(), averageCw2.GetValueOrDefault(),
                            goodCw2.GetValueOrDefault(), "cw2", report),
                        new ReportDistribution(badExam.GetValueOrDefault(), averageExam.GetValueOrDefault(),
                            goodExam.GetValueOrDefault(), "exam", report)
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

        [AccessDeniedAuthorize(Roles = "Staff")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (!_db.Reports.Any(r => r.Id == id))
            {
                return HttpNotFound();
            }
            var report = _db.Reports.Include(r => r.Assignment)
                .Include(r => r.Assignment.Course)
                .Single(r => r.Id == id);

            if (report.IsApproved)
            {
                _errors.Add("You cannot edit approved report");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Index", "Reports"));
            }
            if (CheckCourseManager(report.Assignment) || CheckDlt(report.Assignment.Course))
            {
                _errors.Add("Only Course Leader can edit report.");
                TempData["errors"] = _errors;
                return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Reports"));
            }

            var rvm = new ReportViewModel();
            rvm.Report = report;
            rvm.CourseAssignment = report.Assignment;
            rvm.CourseAssignment.Managers = _db.CourseAssignmentManagers.Include(cam => cam.User)
                .Where(cam => cam.CourseAssignment.Id == report.Assignment.Id).ToList();
            return View(rvm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AccessDeniedAuthorize(Roles = "Staff")]
        public ActionResult Edit(int id, int? totalStudent, string action,
            int? meanCw1, int? meanCw2, int? meanExam, int? medianCw1, int? medianCw2, int? medianExam,
            int? badCw1, int? averageCw1, int? goodCw1, int? badCw2, int? averageCw2, int? goodCw2, int? badExam,
            int? averageExam, int? goodExam)
        {
            if (!_db.Reports.Any(r => r.Id == id))
            {
                return HttpNotFound();
            }
            var report = _db.Reports.Include(r => r.Assignment)
                .Include(r => r.Assignment.Course)
                .Single(r => r.Id == id);

            if (report.IsApproved)
            {
                _errors.Add("You cannot edit approved report");
                TempData["errors"] = _errors;
                return Redirect(Url.Action("Index", "Reports"));
            }

            if (CheckCourseManager(report.Assignment) || CheckDlt(report.Assignment.Course))
            {
                _errors.Add("Only Course Leader can edit report.");
                TempData["errors"] = _errors;
                return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Reports"));
            }

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    report.TotalStudent = totalStudent.GetValueOrDefault();
                    report.Action = action;
                    foreach (var statistical in report.Statisticals)
                    {
                        if (statistical.Type == "cw1")
                        {
                            statistical.Mean = meanCw1.GetValueOrDefault();
                            statistical.Median = medianCw1.GetValueOrDefault();
                        }
                        else if (statistical.Type == "cw2")
                        {
                            statistical.Mean = meanCw2.GetValueOrDefault();
                            statistical.Median = medianCw2.GetValueOrDefault();
                        }
                        else if (statistical.Type == "exam")
                        {
                            statistical.Mean = meanExam.GetValueOrDefault();
                            statistical.Median = medianExam.GetValueOrDefault();
                        }
                    }

                    foreach (var distribution in report.Distributions)
                    {
                        if (distribution.Type == "cw1")
                        {
                            distribution.Bad = badCw1.GetValueOrDefault();
                            distribution.Average = averageCw1.GetValueOrDefault();
                            distribution.Good = goodCw1.GetValueOrDefault();
                        }
                        else if (distribution.Type == "cw2")
                        {
                            distribution.Bad = badCw2.GetValueOrDefault();
                            distribution.Average = averageCw2.GetValueOrDefault();
                            distribution.Good = goodCw2.GetValueOrDefault();
                        }
                        else if (distribution.Type == "exam")
                        {
                            distribution.Bad = badExam.GetValueOrDefault();
                            distribution.Average = averageExam.GetValueOrDefault();
                            distribution.Good = goodExam.GetValueOrDefault();
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


        [AccessDeniedAuthorize(Roles = "Staff")]
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

        [AccessDeniedAuthorize(Roles = "Staff")]
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


        [AccessDeniedAuthorize(Roles = "Staff,Guest")]
        public ActionResult Approved(string session)
        {
            var rvm = new ReportsViewModel();
            if (session.IsNullOrWhiteSpace()) return View(rvm);
            var years = session.Split('-');
            if (!years.Any()) return View(rvm);
            try
            {
                var sYear = Convert.ToInt32(years[0]);
                rvm.SYear = sYear;
                var reports = _db.Reports.Where(r => r.IsApproved).Where(r => r.Assignment.Start.Year == sYear).ToList();
                rvm.Reports = reports;
                return View(rvm);
            }
            catch
            {
                return View(rvm);
            }
        }

        [AccessDeniedAuthorize(Roles = "Staff,Guest")]
        public ActionResult Statistical(string session)
        {
            var statisticals = new StatisticalsViewModel();
            if (session.IsNullOrWhiteSpace()) return View(statisticals);
            var years = session.Split('-');
            if (!years.Any()) return View(statisticals);
            try
            {
                var sYear = Convert.ToInt32(years[0]);
                statisticals.SYear = sYear;
                statisticals.Statisticals = new List<StatisticalViewModel>();
                var faculties = _db.Faculties.ToList();
                foreach (var faculty in faculties)
                {
                    var statistical = new StatisticalViewModel();
                    statistical.Faculty = faculty;
                    var totalCmr =
                        _db.Reports.Where(r => r.Assignment.Start.Year == sYear)
                            .Count(r => r.Assignment.Course.Faculties.Any(f => f.Id == faculty.Id));
                    var approvedCmr =
                        _db.Reports.Where(r => r.Assignment.Course.Faculties.Any(f => f.Id == faculty.Id))
                            .Count(r => r.IsApproved);
                    var commentedCmr =
                        _db.Reports.Where(r => r.Assignment.Course.Faculties.Any(f => f.Id == faculty.Id))
                            .Count(r => r.Comments.Any(c => c.User.FacultyAssignments.Any(fa => fa.Role == "dlt")));

                    statistical.TotalCmr = totalCmr;
                    statistical.ApprovedCmr = approvedCmr;
                    statistical.CommentedCmr = commentedCmr;
                    statisticals.Statisticals.Add(statistical);
                }
                return View(statisticals);
            }
            catch (Exception)
            {
                return View(statisticals);
            }
        }

        [AccessDeniedAuthorize(Roles = "Staff,Guest")]
        public ActionResult Exceptional(string session)
        {
            var evm = new ExceptionalViewModel();
            if (session.IsNullOrWhiteSpace()) return View(evm);
            var years = session.Split('-');
            if (!years.Any()) return View(evm);
            try
            {
                var sYear = Convert.ToInt32(years[0]);
                evm.SYear = sYear;
                var coursesNoManagers =
                    _db.Courses.Where(
                        c =>
                            !c.CourseAssignments.Any(ca => ca.Start.Year == sYear) ||
                            c.CourseAssignments.Any(ca => ca.Start.Year == sYear && ca.Managers.Count < 2))
                        .ToList();
                var coursesNoCmr =
                    _db.Courses.Where(
                        c =>
                            !c.CourseAssignments.Any(ca => ca.Start.Year == sYear) ||
                            c.CourseAssignments.Any(ca => ca.Start.Year == sYear && ca.Reports.Count == 0))
                        .ToList();
                var notApprovedReports =
                    _db.Reports.Where(r => r.Assignment.Start.Year == sYear).Where(r => !r.IsApproved).ToList();
                evm.CoursesNoManagers = coursesNoManagers;
                evm.CoursesNoCmr = coursesNoCmr;
                evm.NotApprovedReports = notApprovedReports;
                return View(evm);
            }
            catch (Exception)
            {
                return View(evm);
            }
        }


        public bool CheckCourseManager(CourseAssignment ca)
        {
            var cUser = User.Identity.GetUserId();
            return
                _db.CourseAssignmentManagers.Include(m => m.User)
                    .Where(cam => cam.CourseAssignment.Id == ca.Id)
                    .Where(m => m.Role == "cm")
                    .Any(m => m.User.Id == cUser);
        }

        public bool CheckDlt(Course course)
        {
            var cUser = User.Identity.GetUserId();
            var result =
                _db.Faculties.Where(f => f.Courses.Any(c => c.Id == course.Id))
                    .Any(f => f.FacultyAssignment.Any(fa => fa.Managers.Any(m => m.User.Id == cUser)));
            return result;
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