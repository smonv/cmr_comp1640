namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Report_Change_Course_To_Assignment : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.Reports", name: "Course_Id", newName: "Assignment_Id");
            RenameIndex(table: "dbo.Reports", name: "IX_Course_Id", newName: "IX_Assignment_Id");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.Reports", name: "IX_Assignment_Id", newName: "IX_Course_Id");
            RenameColumn(table: "dbo.Reports", name: "Assignment_Id", newName: "Course_Id");
        }
    }
}
