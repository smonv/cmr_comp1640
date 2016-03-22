namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RenameReportFeedbackToReportComment : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.ReportFeedbacks", newName: "ReportComments");
            DropColumn("dbo.Reports", "Comment");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Reports", "Comment", c => c.String());
            RenameTable(name: "dbo.ReportComments", newName: "ReportFeedbacks");
        }
    }
}
