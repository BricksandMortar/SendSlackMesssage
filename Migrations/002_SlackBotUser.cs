using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace com.bricksandmortarstudio.Slack.Migrations
{
    [MigrationNumber( 2, "1.3.0" )]
    class SlackBotUser : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.DeleteAttribute( "BF026984-C330-4EBB-879C-2C2AC671FA6D" ); // Webhook
            RockMigrationHelper.DeleteDefinedType( "495981C4-BD0E-4C9A-9FA1-50E07DB2DAB8" ); // Slack
            RockMigrationHelper.AddDefinedType( "Communication", "Slack Users", "Bots and fully authenticated users and tokens for use in Slack", "495981C4-BD0E-4C9A-9FA1-50E07DB2DAB8", @"Set up Bot Users by visiting by visiting yourdomain.slack.com > Configure Integrations > Bot Users" );
            RockMigrationHelper.AddDefinedTypeAttribute( "495981C4-BD0E-4C9A-9FA1-50E07DB2DAB8", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Token", "Token", "The API token provided by Slack. This can be found under Slack > Integrations > Bot Users > Integration Settings > API Token", 43, "", "84B96E9D-C012-4D49-A586-7CFD4C28EEA7" );
            RockMigrationHelper.AddAttributeQualifier( "84B96E9D-C012-4D49-A586-7CFD4C28EEA7", "ispassword", "False", "2BFA30A0-51F9-4CC1-BBE2-2B85279DB7D8" );
            RockMigrationHelper.AddDefinedValue( "495981C4-BD0E-4C9A-9FA1-50E07DB2DAB8", "Fully Authenticated User", "The token for this fully authenticated user can be obtained from api.slack.com/web. WARNING: Using this full access token will associate all actions with the team admin.", "BEFBCC3B-88E0-4E28-BD1E-D2E32D23C9D0", true );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "84B96E9D-C012-4D49-A586-7CFD4C28EEA7" ); // Token
            RockMigrationHelper.DeleteDefinedType( "495981C4-BD0E-4C9A-9FA1-50E07DB2DAB8" ); // Slack
        }
    }
}
