namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCourseValidate : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Courses", "Code", c => c.String(nullable: false, maxLength: 20));
            AlterColumn("dbo.Courses", "Name", c => c.String(nullable: false, maxLength: 100));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Courses", "Name", c => c.String());
            AlterColumn("dbo.Courses", "Code", c => c.String());
        }
    }
}
