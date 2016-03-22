namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FacultyAssignmentRemoveUnusedField : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.FacultyAssignments", "Role");
        }
        
        public override void Down()
        {
            AddColumn("dbo.FacultyAssignments", "Role", c => c.String());
        }
    }
}
