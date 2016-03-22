namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserToReportComment : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ReportComments", "User_Id", c => c.String(maxLength: 128));
            CreateIndex("dbo.ReportComments", "User_Id");
            AddForeignKey("dbo.ReportComments", "User_Id", "dbo.AspNetUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReportComments", "User_Id", "dbo.AspNetUsers");
            DropIndex("dbo.ReportComments", new[] { "User_Id" });
            DropColumn("dbo.ReportComments", "User_Id");
        }
    }
}
