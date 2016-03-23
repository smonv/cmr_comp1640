using System;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Mvc;
using CMR.EmailModels;
using CMR.Models;
using Hangfire;
using Postal;

namespace CMR.Jobs
{
    public class NotifyReportCommentDeadline
    {
        public static void Check()
        {
            using (var db = new ApplicationDbContext())
            {
                var approvedReports = db.Reports.Where(r => r.IsApproved).ToList();
                foreach (var report in approvedReports)
                {
                    var fams =
                        db.FacultyAssignmentManagers.Where(
                            fam => fam.FacultyAssignment.Faculty.Courses.Any(c => c.Id == report.Assignment.Course.Id))
                            .ToList();
                    var dlt = fams.SingleOrDefault(fam => fam.Role == "dlt");
                    if (dlt != null)
                    {
                        if (report.Comments.All(c => c.User.Id != dlt.User.Id))
                        {
                            var timepass = DateTime.Today.Day - report.CreateAt.Day;
                            if (timepass > 12 && timepass < 15)
                            {
                                var timeleft = 14 - report.CreateAt.Day;

                                BuildMail(report, dlt.User, timeleft);
                            }
                        }
                    }
                }
            }
        }

        private static void BuildMail(Report report, ApplicationUser user, int timeLeft)
        {
            var ca = report.Assignment;
            var subject = "Please take comment on report " + ca.Course.Code + " - " + ca.Course.Name + " : " +
                          ca.Start.Year + " - " + ca.End.Year;
            BackgroundJob.Enqueue(() => Notify(user.Email, subject, "", timeLeft));
        }

        public static void Notify(string to, string subject, string callbackUrl, int timeLeft)
        {
            var viewsPath = Path.GetFullPath(HostingEnvironment.MapPath(@"~/Views/Emails"));
            var engines = new ViewEngineCollection();
            engines.Add(new FileSystemRazorViewEngine(viewsPath));

            var emailService = new Postal.EmailService(engines);
            var email = new ReportCommentNotifyEmail
            {
                To = to,
                Subject = subject,
                CallbackUrl = callbackUrl,
                TimeLeft = timeLeft
            };
            emailService.Send(email);
        }
    }
}