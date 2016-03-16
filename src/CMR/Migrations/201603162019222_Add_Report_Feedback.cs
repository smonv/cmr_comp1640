namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_Report_Feedback : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ReportFeedbacks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Content = c.String(),
                        Report_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Reports", t => t.Report_Id)
                .Index(t => t.Report_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReportFeedbacks", "Report_Id", "dbo.Reports");
            DropIndex("dbo.ReportFeedbacks", new[] { "Report_Id" });
            DropTable("dbo.ReportFeedbacks");
        }
    }
}
