using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Postal;

namespace CMR.EmailModels
{
    public class ReportCommentNotifyEmail : Email
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string CallbackUrl { get; set; }
        public int TimeLeft { get; set; }
    }
}