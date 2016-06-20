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
    [ActionCategory( "Slack" )]

    [DefinedValueField(com.bricksandmortarstudio.Slack.SystemGuid.Slack.SLACK, "Slack Channel Config", "The Slack channel bot that you want to use", true, false, "" , "", 0, "SlackChannelConfig")]
    [WorkflowTextOrAttribute("Channel", "Attribute Value", "The #channel or @user or the attribute that contains the #channel or @user that message should be sent to. <span class='tip tip-lava'></span>", false, "", "", 1, "Channel", new string[] { "Rock.Field.Types.TextFieldType" })]
    [WorkflowTextOrAttribute("Message", "Attribute Value", "The text or an attribute that contains the text that should be sent to the channel. <span class='tip tip-lava'></span>", false, "", "", 2, "Message", new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.MemoFieldType" } )]
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

            DefinedValueCache slackChannel = null;
            var slackChannelConfigValue = GetAttributeValue(action, "SlackChannelConfig");
            var slackChannelGuid = slackChannelConfigValue.AsGuidOrNull();
            if (slackChannelGuid.HasValue)
            {
                slackChannel = DefinedValueCache.Read(slackChannelGuid.Value, rockContext);
            }

            // get the workflow action channel attribute 
            string slackActionChannel = GetAttributeValue(action, "Channel");
            var slackActionChannelGuid = slackActionChannel.AsGuidOrNull();
            if (slackActionChannelGuid.HasValue)
            {
                slackActionChannel = action.GetWorklowAttributeValue( slackActionChannelGuid.Value );
            }
            else
            {
                slackActionChannel = slackActionChannel.ResolveMergeFields( GetMergeFields( action ) );
            }

            // check if a channel has been specified as an attribute, if not default to the specified channel defined type
            var channel = !string.IsNullOrEmpty(slackActionChannel) ? slackActionChannel : null;

            // get the workflow action bot name attribute 
            var slackActionBotName = GetAttributeValue(action,"BotName");
            var slackActionBotNameGuid = slackActionBotName.AsGuidOrNull();
            if (slackActionBotNameGuid.HasValue)
            {
                slackActionBotName = action.GetWorklowAttributeValue( slackActionBotNameGuid.Value );
            }
            else
            {
                slackActionBotName = slackActionBotName.ResolveMergeFields( GetMergeFields( action ) );
            }

            var channelBotName = !string.IsNullOrEmpty(slackActionBotName) ? slackActionBotName : null;

            // get the workflow action bot icon attribute 
            var slackActionBotIcon = GetAttributeValue(action, "BotIcon");
            var slackActionBotIconGuid = slackActionBotIcon.AsGuidOrNull();
            if (slackActionBotIconGuid.HasValue)
            {
                slackActionBotIcon = action.GetWorklowAttributeValue( slackActionBotIconGuid.Value );
            }
            else
            {
                slackActionBotIcon = slackActionBotIcon.ResolveMergeFields( GetMergeFields( action ) );
            }
            
            var channelBotIcon = !string.IsNullOrEmpty(slackActionBotIcon) ? slackActionBotIcon : null;

            // get the workflow message attribute 
            string message = GetAttributeValue(action, "Message");
            var messageGuid = message.AsGuidOrNull();
            if ( messageGuid.HasValue )
            {
                message = action.GetWorklowAttributeValue( messageGuid.Value );
                message = message.Replace( @"\n", @"\\n" ); // fixed new line characters
            }
            else
            {
                message = message.ResolveMergeFields( GetMergeFields( action ) );
            }

            //Get the webhook defined value attribute 
            string webhook = slackChannel.AttributeValues
                .Values
                .Where(b => b.AttributeKey == "Webhook")
                .FirstOrDefault()
                .Value;

            //Create Slack Payload 
            SlackMessage slackSend = new SlackMessage();
            slackSend.text = message;
            slackSend.username = channelBotName;
            if (!(channelBotIcon == null) && channelBotIcon.Contains("http"))
            {
                slackSend.icon_url = channelBotIcon;
            }
            else
            {
                slackSend.icon_emoji = channelBotIcon;
            }
            slackSend.channel = channel;
            JsonSerializer jsonSerializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            string payload = JsonConvert.SerializeObject(slackSend);


            //Send
            var client = new RestClient(webhook);
            RestRequest request = new RestRequest(Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Accept", "application/json");
            request.AddParameter("payload", payload);
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