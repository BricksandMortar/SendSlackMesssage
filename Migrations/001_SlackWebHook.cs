using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;
namespace com.bricksandmortarstudio.Slack.Migrations
{
    [MigrationNumber(1, "1.3.0")]
    public class SlackWebHook : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddDefinedType("Communication", "Slack", "Webhooks set up for use with Slack", "495981C4-BD0E-4C9A-9FA1-50E07DB2DAB8", @"Obtain your webhook by creating an ''incoming webhook'' Slack service integration. The values given in your webhook integration can be overridden at the workflow action level.");
            RockMigrationHelper.AddDefinedTypeAttribute("495981C4-BD0E-4C9A-9FA1-50E07DB2DAB8", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Webhook", "Webhook", "The webhook URL given by the incoming webhook Slack integration", 42, "", "BF026984-C330-4EBB-879C-2C2AC671FA6D");
            RockMigrationHelper.AddAttributeQualifier("BF026984-C330-4EBB-879C-2C2AC671FA6D", "ispassword", "False", "50CFECD0-C923-4090-9DAE-1837277D6179");
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute("BF026984-C330-4EBB-879C-2C2AC671FA6D"); // Webhook
            RockMigrationHelper.DeleteDefinedType("495981C4-BD0E-4C9A-9FA1-50E07DB2DAB8"); // Slack
        }
    }
}
