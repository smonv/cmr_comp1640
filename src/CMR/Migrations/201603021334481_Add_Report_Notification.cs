namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_Report_Notification : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ReportNotifications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Read = c.Boolean(nullable: false),
                        Message = c.String(),
                        Report_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Reports", t => t.Report_Id)
                .Index(t => t.Report_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReportNotifications", "Report_Id", "dbo.Reports");
            DropIndex("dbo.ReportNotifications", new[] { "Report_Id" });
            DropTable("dbo.ReportNotifications");
        }
    }
}
