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
using Newtonsoft.Json.Linq;
using System.Net;

namespace com.bricksandmortarstudio.Slack.Workflow.Action
{
    /// <summary>
    /// Sends message to Slack
    /// </summary>
    [Description( "Sends an Slack message to a specified channel." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Send Slack Message" )]

    [WorkflowTextOrAttribute( "Inviter", "Attribute Value", "The person that should invited the person to specific channel", false, "", "", 2, "Inviter", new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [WorkflowTextOrAttribute( "Channel", "Attribute Value", "The #channel or the attribute that contains the #channel that the user should be invited to. <span class='tip tip-lava'></span>", false, "", "", 1, "Channel", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowTextOrAttribute( "Person", "Attribute Value", "The person that should be invited to the specific channel", true, "", "", 2, "User", new string[] { "Rock.Field.Types.PersonFieldType", "Rock.Field.Types.TextFieldType" } )]

    public class ChannelInvite : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            var mergeFields = GetMergeFields( action );

            //Get token from inviter or default
            string token = String.Empty;
            var guidInviterAttribute = GetAttributeValue( action, "Inviter" ).AsGuidOrNull();
            if ( guidInviterAttribute.HasValue )
            {
                var attributeInvinter = AttributeCache.Read( guidInviterAttribute.Value, rockContext );
                if ( attributeInvinter != null )
                {
                    string attributeInvinterValue = action.GetWorklowAttributeValue( guidInviterAttribute.Value );
                    if ( !string.IsNullOrWhiteSpace( attributeInvinterValue ) )
                    {
                        if ( attributeInvinter.FieldType.Class == typeof( Rock.Field.Types.PersonFieldType ).FullName )
                        {
                            Guid inviterAliasGuid = attributeInvinterValue.AsGuid();
                            if ( !inviterAliasGuid.IsEmpty() )
                            {
                                token = new PersonAliasService( rockContext ).Queryable()
                                    .Where( a => a.Guid.Equals( inviterAliasGuid ) )
                                    .Select( a => a.Person )
                                    .FirstOrDefault()
                                    .Users
                                    .Where( u => u.UserName.Contains( "Slack" ) == true )
                                    .FirstOrDefault()
                                    .Password;
                            }
                        }
                        else
                        {
                            errorMessages.Add( "The attribute used to provide the inviter was not of type 'Person'." );
                        }
                    }
                }
            }
            else
            {
                var defaultTokenStore = DefinedValueCache.Read( SystemGuid.Slack.SLACK_FULL_ACCESS_TOKEN.AsGuidOrNull().Value, rockContext );
                if ( defaultTokenStore != null )
                {
                    token = defaultTokenStore.AttributeValues
                    .Values
                    .Where( b => b.AttributeKey == "Token" )
                    .FirstOrDefault()
                    .Value;
                }
                else
                {
                    errorMessages.Add( "The default access token could not be found." );
                }
            }

            if ( String.IsNullOrEmpty( token ) )
            {
                return false;
            }


            //Get the channel to invite the user to
            string channel = GetAttributeValue( action, "Channel" );
            var channelGuid = channel.AsGuidOrNull();
            if ( channelGuid.HasValue )
            {
                var channelValue = AttributeCache.Read( channelGuid.Value, rockContext );
                if ( channelValue != null )
                {
                    channel = channelValue.Name;
                    if (!channel.Any( char.IsDigit ))
                    {
                        channel = channel.Replace( "#", "" );
                        var channelClient = new RestClient( "https://slack.com/api/channels.list" );
                        var channelRequest = new RestRequest( Method.POST );
                        channelRequest.RequestFormat = DataFormat.Json;
                        channelRequest.AddHeader( "Accept", "application/json" );
                        channelRequest.AddParameter( "token", token );
                        channelRequest.AddParameter( "exclude_archived", 1 );
                        var channelResponse = channelClient.Execute( channelRequest );
                        if (channelResponse.StatusCode == HttpStatusCode.OK)
                        {
                            JObject channels = JObject.Parse( channelResponse.Content);
                            channel = channels["channels"].Children()
                                .Where( c => c["name"].ToString() == channel )
                                .FirstOrDefault()["id"].ToStringSafe();
                            if ( channel == null)
                            {
                                errorMessages.Add( "The specified channel code could not be found." );
                                return false;
                            }
                        }
                    }
                }
            }
            string userId = "";
            //Get the user who is being invited
            var guidUserAttribute = GetAttributeValue( action, "User" ).AsGuidOrNull();
            if ( guidUserAttribute.HasValue )
            {
                var attributeUser = AttributeCache.Read( guidUserAttribute.Value, rockContext );
                if ( attributeUser != null )
                {
                    string attributeUserValue = action.GetWorklowAttributeValue( guidInviterAttribute.Value );
                    if ( !string.IsNullOrWhiteSpace( attributeUserValue ) )
                    {
                        if ( attributeUser.FieldType.Class == typeof( Rock.Field.Types.PersonFieldType ).FullName )
                        {
                            Guid personAliasGuid = attributeUserValue.AsGuid();
                            if ( !personAliasGuid.IsEmpty() )
                            {
                                userId = new PersonAliasService( rockContext ).Queryable()
                                    .Where( a => a.Guid.Equals( personAliasGuid ) )
                                    .Select( a => a.Person )
                                    .FirstOrDefault().Users
                                     .Where( u => u.UserName.Contains( "Slack" ) == true )
                                     .FirstOrDefault()
                                     .UserName;
                                userId = userId.Substring( userId.LastIndexOf( '_' ) + 1 );
                            }
                        }
                        else
                        {
                            errorMessages.Add( "The attribute used to provide the user was not of type 'Person'." );
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }


            //Send
            var client = new RestClient( "https://slack.com/api/channels.invite" );
            var request = new RestRequest( Method.POST );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddParameter( "token", token );
            request.AddParameter( "channel", channel );
            request.AddParameter( "user", userId );
            var response = client.Execute( request );
            return true;

        }
    }
}