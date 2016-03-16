namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Change_Report_Fields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Reports", "TotalStudent", c => c.Int(nullable: false));
            AddColumn("dbo.Reports", "Comment", c => c.String());
            AddColumn("dbo.Reports", "Action", c => c.String());
            DropColumn("dbo.Reports", "Title");
            DropColumn("dbo.Reports", "Content");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Reports", "Content", c => c.String());
            AddColumn("dbo.Reports", "Title", c => c.String());
            DropColumn("dbo.Reports", "Action");
            DropColumn("dbo.Reports", "Comment");
            DropColumn("dbo.Reports", "TotalStudent");
        }
    }
}
