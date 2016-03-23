namespace CMR.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCreateAtColumnForReport : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Reports", "CreateAt", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Reports", "CreateAt");
        }
    }
}
