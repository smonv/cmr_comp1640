namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCourseDescription : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Courses", "Description", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Courses", "Description");
        }
    }
}
