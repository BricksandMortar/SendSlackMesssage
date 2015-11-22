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

            DefinedValueCache slackBot = null;
            var slackBotConfig = GetAttributeValue(action, "Token" );
            var slackBotGuid = slackBotConfig.AsGuidOrNull();
            if (slackBotGuid.HasValue)
            {
                slackBot = DefinedValueCache.Read(slackBotGuid.Value, rockContext);
            }

            //Get the workflow action channel attribute 
            string actionChannel = GetAttributeValue(action, "Channel");
            var slackActionChannelGuid = actionChannel.AsGuidOrNull();
            if (slackActionChannelGuid.HasValue)
            {
                var slackActionChannelValue = AttributeCache.Read(slackActionChannelGuid.Value, rockContext);
                if (slackActionChannelValue != null)
                {
                        actionChannel = slackActionChannelValue.Name;
                }
            }

            //Check if a channel has been specified as an attribute, if not default to the specified channel defined type
            var channel = !string.IsNullOrEmpty(actionChannel) ? actionChannel : null;

            //Get the workflow action bot name attribute 
            var actionBotName = GetAttributeValue(action,"BotName");
            var slackActionBotNameGuid = actionBotName.AsGuidOrNull();
            if (slackActionBotNameGuid.HasValue)
            {
                var slackActionBotNameValue = AttributeCache.Read(slackActionChannelGuid.Value, rockContext);
                if (slackActionBotNameValue != null)
                {
                    actionBotName = slackActionBotNameValue.Name;
                }
            }

            var botName = !string.IsNullOrEmpty(actionBotName) ? actionBotName : null;

            //Get the workflow action bot icon attribute 
            var actionBotIcon = GetAttributeValue(action, "BotIcon");
            var slackActionBotIconGuid = actionBotIcon.AsGuidOrNull();
            if (slackActionBotIconGuid.HasValue)
            {
                var slackActionBotIconValue = AttributeCache.Read(slackActionBotIconGuid.Value, rockContext);
                if (slackActionBotIconValue != null)
                {
                    actionBotIcon = slackActionBotIconValue.Name;
                }
            }
            
            var botIcon = !string.IsNullOrEmpty(actionBotIcon) ? actionBotIcon : null;

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
            request.AddParameter("token", slackBot.AttributeValues
                .Values
                .Where( b => b.AttributeKey == "Token" )
                .FirstOrDefault()
                .Value);
            request.AddParameter( "text", message );
            request.AddParameter( "channel", channel );
            if ( (botIcon != null && botIcon.Contains( "http" ) )
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

        public class SlackMessage
        {
            public string text { get; set; }
            public string username { get; set; }
            public string icon_url { get; set; }
            public string icon_emoji { get; set; }
            public string channel { get; set; }

            public bool ShouldSerializeusername()
            {
                if (string.IsNullOrEmpty(username))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public bool ShouldSerializeicon_url(){
                if (string.IsNullOrEmpty(icon_url))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public bool ShouldSerializeicon_emoji()
            {
                if (string.IsNullOrEmpty(icon_emoji))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            public bool ShouldSerializechannel()
            {
                if (string.IsNullOrEmpty(channel))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

        }
    }
}