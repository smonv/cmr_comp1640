using System.Collections.Generic;
using CMR.Models;

namespace CMR.ViewModels
{
    public class ReportsViewModel
    {
        public List<Report> Reports { get; set; }
        public int? SYear { get; set; }
    }
}