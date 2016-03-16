namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_Report_Distribution : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ReportDistributions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Bad = c.Int(nullable: false),
                        Average = c.Int(nullable: false),
                        Good = c.Int(nullable: false),
                        Type = c.String(),
                        Report_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Reports", t => t.Report_Id)
                .Index(t => t.Report_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReportDistributions", "Report_Id", "dbo.Reports");
            DropIndex("dbo.ReportDistributions", new[] { "Report_Id" });
            DropTable("dbo.ReportDistributions");
        }
    }
}
