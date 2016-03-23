namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NullableReportApproveAt : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Reports", "ApproveAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Reports", "ApproveAt", c => c.DateTime(nullable: false));
        }
    }
}
