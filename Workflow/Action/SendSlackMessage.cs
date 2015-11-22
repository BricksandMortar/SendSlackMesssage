// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Rock;
using Rock.Workflow;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using RestSharp;
using Newtonsoft.Json;

namespace com.bricksandmortarstudio.Slack.Workflow.Action
{
    /// <summary>
    /// Sends message to Slack
    /// </summary>
    [Description("Sends an Slack message to a specified channel.")]
    [Export(typeof(ActionComponent))]
    [ExportMetadata("ComponentName", "Send Slack Message")]

    [DefinedValueField(com.bricksandmortarstudio.Slack.SystemGuid.Slack.SLACK, "Slack Bot", "The Slack bot that you want to use", true, false, "" , "", 0, "Token")]
    [WorkflowTextOrAttribute("Channel", "Attribute Value", "The #channel or @user or the attribute that contains the #channel or @user that message should be sent to. <span class='tip tip-lava'></span>", false, "", "", 1, "Channel", new string[] { "Rock.Field.Types.TextFieldType" })]
    [WorkflowTextOrAttribute("Message", "Attribute Value", "The text or an attribute that contains the text that should be sent to the channel. <span class='tip tip-lava'></span>", false, "", "", 2, "Message", new string[] { "Rock.Field.Types.TextFieldType" })]
    [WorkflowTextOrAttribute("Bot Name", "Attribute Value", "The name of the bot or an attribute that contains the name of the bot that should be used to message the channel. <span class='tip tip-lava'></span>", false, "", "", 3, "BotName", new string[] { "Rock.Field.Types.TextFieldType" })]
    [WorkflowTextOrAttribute("Bot Icon", "Attribute Value", "The url of an icon or an emoji or an attribute that contains the url of an icon or an emoji that should be used as the Slack icon for the bot. <span class='tip tip-lava'></span>", false, "", "", 4, "BotIcon", new string[] { "Rock.Field.Types.TextFieldType" })]
    
    public class SendSlackMessage : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute(RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages)
        {
            errorMessages = new List<string>();
            var mergeFields = GetMergeFields(action);

            DefinedValueCache bot = null;
            var botDefinedValue = GetAttributeValue(action, "Token" );
            var botGuid = botDefinedValue.AsGuidOrNull();
            if (botGuid.HasValue)
            {
                bot = DefinedValueCache.Read(botGuid.Value, rockContext);
            }

            //Get the workflow action channel attribute 
            string channel = GetAttributeValue(action, "Channel");
            var channelGuid = channel.AsGuidOrNull();
            if (channelGuid.HasValue)
            {
                var channelValue = AttributeCache.Read(channelGuid.Value, rockContext);
                if (channelValue != null)
                {
                        channel = channelValue.Name;
                }
            }

            //Get the workflow action bot name attribute 
            var botName = GetAttributeValue(action,"BotName");
            var botNameGuid = botName.AsGuidOrNull();
            if (botNameGuid.HasValue)
            {
                var botNameValue = AttributeCache.Read( botNameGuid.Value, rockContext);
                if (botNameValue != null)
                {
                    botName = botNameValue.Name;
                }
            }

            //Get the workflow action bot icon attribute 
            var botIcon = GetAttributeValue(action, "BotIcon");
            var botIconGuid = botIcon.AsGuidOrNull();
            if (botIconGuid.HasValue)
            {
                var botIconValue = AttributeCache.Read(botIconGuid.Value, rockContext);
                if (botIconValue != null)
                {
                    botIcon = botIconValue.Name;
                }
            }

            //Get the workflow message attribute 
            string message = GetAttributeValue(action, "Message");
            Guid messageGuid = message.AsGuid();
            if (!messageGuid.IsEmpty())
            {
                var attribute = AttributeCache.Read(messageGuid, rockContext);
                if (attribute != null)
                {
                    string messageAttributeValue = action.GetWorklowAttributeValue(messageGuid);
                    if (!string.IsNullOrWhiteSpace(messageAttributeValue))
                    {
                            message = messageAttributeValue.ResolveMergeFields(mergeFields);
                    }
                }
            }

            //Send
            var client = new RestClient( "https://slack.com/api/chat.postMessage" );
            RestRequest request = new RestRequest(Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Accept", "application/json");
            request.AddParameter("token", bot.AttributeValues
                .Values
                .Where( b => b.AttributeKey == "Token" )
                .FirstOrDefault()
                .Value);
            request.AddParameter( "text", message );
            request.AddParameter( "channel", channel );
            if ( (botIcon != null && botIcon.Contains( "http" ) ))
            {
                request.AddParameter( "icon_url", botIcon );
            }
            else if ( botIcon != null )
            {
                request.AddParameter( "icon_emoji", botIcon );
            }
            if (botName != null)
            {
                request.AddParameter( "username", botName );
            }
            var response = client.Execute(request);

            return true;

        }
    }
}