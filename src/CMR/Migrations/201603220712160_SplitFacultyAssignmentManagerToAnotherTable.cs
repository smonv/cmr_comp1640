namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SplitFacultyAssignmentManagerToAnotherTable : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.FacultyAssignments", "Staff_Id", "dbo.AspNetUsers");
            DropIndex("dbo.FacultyAssignments", new[] { "Staff_Id" });
            CreateTable(
                "dbo.FacultyAssignmentManagers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Role = c.String(),
                        FacultyAssignment_Id = c.Int(),
                        User_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.FacultyAssignments", t => t.FacultyAssignment_Id)
                .ForeignKey("dbo.AspNetUsers", t => t.User_Id)
                .Index(t => t.FacultyAssignment_Id)
                .Index(t => t.User_Id);
            
            DropColumn("dbo.FacultyAssignments", "Staff_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.FacultyAssignments", "Staff_Id", c => c.String(maxLength: 128));
            DropForeignKey("dbo.FacultyAssignmentManagers", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.FacultyAssignmentManagers", "FacultyAssignment_Id", "dbo.FacultyAssignments");
            DropIndex("dbo.FacultyAssignmentManagers", new[] { "User_Id" });
            DropIndex("dbo.FacultyAssignmentManagers", new[] { "FacultyAssignment_Id" });
            DropTable("dbo.FacultyAssignmentManagers");
            CreateIndex("dbo.FacultyAssignments", "Staff_Id");
            AddForeignKey("dbo.FacultyAssignments", "Staff_Id", "dbo.AspNetUsers", "Id");
        }
    }
}
