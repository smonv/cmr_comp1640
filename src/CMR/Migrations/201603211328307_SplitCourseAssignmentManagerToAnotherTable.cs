namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SplitCourseAssignmentManagerToAnotherTable : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CourseAssignments", "Manager_Id", "dbo.AspNetUsers");
            DropIndex("dbo.CourseAssignments", new[] { "Manager_Id" });
            CreateTable(
                "dbo.CourseAssignmentManagers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Role = c.String(),
                        CourseAssignment_Id = c.Int(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CourseAssignments", t => t.CourseAssignment_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.CourseAssignment_Id)
                .Index(t => t.User_Id);
            
            DropColumn("dbo.CourseAssignments", "Role");
            DropColumn("dbo.CourseAssignments", "Manager_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.CourseAssignments", "Manager_Id", c => c.String(maxLength: 128));
            AddColumn("dbo.CourseAssignments", "Role", c => c.String());
            DropForeignKey("dbo.CourseAssignmentManagers", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.CourseAssignmentManagers", "CourseAssignment_Id", "dbo.CourseAssignments");
            DropIndex("dbo.CourseAssignmentManagers", new[] { "User_Id" });
            DropIndex("dbo.CourseAssignmentManagers", new[] { "CourseAssignment_Id" });
            DropTable("dbo.CourseAssignmentManagers");
            CreateIndex("dbo.CourseAssignments", "Manager_Id");
            AddForeignKey("dbo.CourseAssignments", "Manager_Id", "dbo.AspNetUsers", "Id");
        }
    }
}
