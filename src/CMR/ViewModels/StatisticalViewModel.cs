using CMR.Models;

namespace CMR.ViewModels
{
    public class StatisticalViewModel
    {
        public Faculty Faculty { get; set; }
        public int TotalCmr { get; set; }
        public int ApprovedCmr { get; set; }
        public double ApprovedPercent { get; set; }
        public int CommentedCmr { get; set; }
        public double CommentedPercent { get; set; }
    }
}