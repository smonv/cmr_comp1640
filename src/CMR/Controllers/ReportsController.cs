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
        public async Task<ActionResult> Create([Bind(Include = "Id,Title,Content")] Report report, int id)
        {
            CourseAssignment ca = db.CourseAssignments.Find(id);
            if (ModelState.IsValid)
            {
                report.Assignment = ca;
                db.Reports.Add(report);
                db.SaveChanges();
                Mailer mailer = new Mailer();
                var emailMessage = new MailMessage();
                ApplicationUser cm = report.Assignment.Course.Managers.Single(u => u.Role == "cm").Manager;
                emailMessage.To.Add(new MailAddress(cm.Email));
                emailMessage.Subject = report.Title + " " + report.Assignment.Start.ToString("yyyy") + " - " + report.Assignment.End.ToString("yyyy");
                var reportUrl = Url.Action("Details", "Reports", new { id = report.Id }, protocol: Request.Url.Scheme);
                string body = report.Assignment.Course.Name + " " +
                    report.Assignment.Start.ToString("yyyy") + " - " +
                    report.Assignment.End.ToString("yyyy") +
                    " have new report. Click <a href='" + reportUrl + "'>here</a>";
                emailMessage.Body = body;
                emailMessage = mailer.BuildMessage(emailMessage);
                await mailer.client.SendMailAsync(emailMessage);
                return Redirect("/Courses/Assigned");
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
