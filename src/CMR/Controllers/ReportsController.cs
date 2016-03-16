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
        public async Task<ActionResult> Create(int id, int totalStudent, string comment, string action,
            int meanCW1, int meanCW2, int meanEXAM, int medianCW1, int medianCW2, int medianEXAM,
            int badCW1, int averageCW1, int goodCW1, int badCW2, int averageCW2, int goodCW2, int badEXAM, int averageEXAM, int goodEXAM)
        {
            Report report = new Report(totalStudent, comment, action);

            CourseAssignment ca = db.CourseAssignments.Find(id);
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    report.Assignment = ca;
                    db.Reports.Add(report);
                    List<ReportStatistical> statisticals = new List<ReportStatistical>();
                    ReportStatistical cw1 = new ReportStatistical(meanCW1, medianCW1, "cw1", report);
                    ReportStatistical cw2 = new ReportStatistical(meanCW2, medianCW2, "cw2", report);
                    ReportStatistical exam = new ReportStatistical(meanEXAM, medianEXAM, "exam", report);
                    statisticals.Add(cw1);
                    statisticals.Add(cw2);
                    statisticals.Add(exam);
                    db.ReportStatistical.AddRange(statisticals);
                    List<ReportDistribution> distribution = new List<ReportDistribution>(new ReportDistribution[] {
                            new ReportDistribution(badCW1, averageCW1, goodCW1, "cw1", report),
                            new ReportDistribution(badCW2, averageCW2, goodCW2, "cw2", report),
                            new ReportDistribution(badEXAM, averageEXAM, goodEXAM, "exam", report)
                    });
                    db.ReportDistribution.AddRange(distribution);
                    db.SaveChanges();
                    transaction.Commit();
                    return Redirect("/Courses/Assigned");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ReportViewModel rvm = new ReportViewModel();
                    rvm.CourseAssignment = ca;
                    rvm.Report = report;
                    return View(report);
                }
            }
            if (ModelState.IsValid)
            {


                /*
                ApplicationUser cm = report.Assignment.Course.Managers.Single(u => u.Role == "cm").Manager;
                string subject = report.Assignment.Start.Year + " - " + report.Assignment.End.Year;
                var reportUrl = Url.Action("Details", "Reports", new { id = report.Id }, protocol: Request.Url.Scheme);
                string body = report.Assignment.Course.Name + " " +
                    report.Assignment.Start.Year + " - " +
                    report.Assignment.End.Year +
                    " have new report. Click <a href='" + reportUrl + "'>here</a>";
                var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
                await userManager.SendEmailAsync(cm.Id, subject, body);
    */

            }
            else
            {

            }


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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private string RenderRazorViewToString(Controller controller, string viewName, object model)
        {
            controller.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
                var viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(controller.ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}
