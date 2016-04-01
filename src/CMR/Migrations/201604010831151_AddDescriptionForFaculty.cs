namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddDescriptionForFaculty : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Faculties", "Description", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Faculties", "Description");
        }
    }
}
