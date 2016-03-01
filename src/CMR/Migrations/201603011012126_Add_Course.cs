namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_Course : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CourseAssignments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Role = c.String(),
                        Start = c.DateTime(nullable: false),
                        End = c.DateTime(nullable: false),
                        Manager_Id = c.String(maxLength: 128),
                        Course_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.Manager_Id)
                .ForeignKey("dbo.Courses", t => t.Course_Id)
                .Index(t => t.Manager_Id)
                .Index(t => t.Course_Id);
            
            CreateTable(
                "dbo.Courses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.FacultyCourses",
                c => new
                    {
                        Faculty_Id = c.Int(nullable: false),
                        Course_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Faculty_Id, t.Course_Id })
                .ForeignKey("dbo.Faculties", t => t.Faculty_Id, cascadeDelete: true)
                .ForeignKey("dbo.Courses", t => t.Course_Id, cascadeDelete: true)
                .Index(t => t.Faculty_Id)
                .Index(t => t.Course_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CourseAssignments", "Course_Id", "dbo.Courses");
            DropForeignKey("dbo.CourseAssignments", "Manager_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.FacultyCourses", "Course_Id", "dbo.Courses");
            DropForeignKey("dbo.FacultyCourses", "Faculty_Id", "dbo.Faculties");
            DropIndex("dbo.FacultyCourses", new[] { "Course_Id" });
            DropIndex("dbo.FacultyCourses", new[] { "Faculty_Id" });
            DropIndex("dbo.CourseAssignments", new[] { "Course_Id" });
            DropIndex("dbo.CourseAssignments", new[] { "Manager_Id" });
            DropTable("dbo.FacultyCourses");
            DropTable("dbo.Courses");
            DropTable("dbo.CourseAssignments");
        }
    }
}
