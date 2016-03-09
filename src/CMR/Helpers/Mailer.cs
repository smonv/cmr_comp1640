using System;
using System.Configuration;
using System.Net.Mail;
using System.Net;

namespace CMR.Helpers
{
    public class Mailer
    {
        public readonly SmtpClient client;

        public Mailer()
        {
            client = new SmtpClient();
            var credential = new NetworkCredential
            {
                UserName = ConfigurationManager.AppSettings["MailerUserName"],
                Password = ConfigurationManager.AppSettings["MailerPassword"]
            };
            client.Credentials = credential;
            client.Host = ConfigurationManager.AppSettings["MailerHost"];
            client.Port = Int32.Parse(ConfigurationManager.AppSettings["MailerPort"]);
            client.EnableSsl = Boolean.Parse(ConfigurationManager.AppSettings["MailerSSL"]);
        }

        public MailMessage BuildMessage(MailMessage message)
        {
            message.From = new MailAddress("cmr@no-reply.com");
            message.IsBodyHtml = true;
            return message;
        }
    }
}