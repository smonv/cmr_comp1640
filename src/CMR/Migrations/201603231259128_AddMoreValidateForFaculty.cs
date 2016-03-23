namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMoreValidateForFaculty : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Faculties", new[] { "Name" });
            AlterColumn("dbo.Faculties", "Name", c => c.String(nullable: false, maxLength: 200));
            CreateIndex("dbo.Faculties", "Name", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Faculties", new[] { "Name" });
            AlterColumn("dbo.Faculties", "Name", c => c.String(maxLength: 200));
            CreateIndex("dbo.Faculties", "Name", unique: true);
        }
    }
}
