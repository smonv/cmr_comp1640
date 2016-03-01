namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_Faculty : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Faculties",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true);
            
            CreateTable(
                "dbo.FacultyAssignments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Role = c.String(),
                        Faculty_Id = c.Int(),
                        Staff_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Faculties", t => t.Faculty_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.Staff_Id)
                .Index(t => t.Faculty_Id)
                .Index(t => t.Staff_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.FacultyAssignments", "Staff_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.FacultyAssignments", "Faculty_Id", "dbo.Faculties");
            DropIndex("dbo.FacultyAssignments", new[] { "Staff_Id" });
            DropIndex("dbo.FacultyAssignments", new[] { "Faculty_Id" });
            DropIndex("dbo.Faculties", new[] { "Name" });
            DropTable("dbo.FacultyAssignments");
            DropTable("dbo.Faculties");
        }
    }
}
