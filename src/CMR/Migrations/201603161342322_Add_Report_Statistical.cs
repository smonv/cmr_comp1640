namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_Report_Statistical : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ReportStatisticals",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Mean = c.Int(nullable: false),
                        Median = c.Int(nullable: false),
                        Type = c.String(),
                        Report_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Reports", t => t.Report_Id)
                .Index(t => t.Report_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReportStatisticals", "Report_Id", "dbo.Reports");
            DropIndex("dbo.ReportStatisticals", new[] { "Report_Id" });
            DropTable("dbo.ReportStatisticals");
        }
    }
}
