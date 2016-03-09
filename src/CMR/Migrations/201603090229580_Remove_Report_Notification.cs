namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Remove_Report_Notification : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ReportNotifications", "Report_Id", "dbo.Reports");
            DropIndex("dbo.ReportNotifications", new[] { "Report_Id" });
            DropTable("dbo.ReportNotifications");
        }
        
        public override void Down()
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
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.ReportNotifications", "Report_Id");
            AddForeignKey("dbo.ReportNotifications", "Report_Id", "dbo.Reports", "Id");
        }
    }
}
