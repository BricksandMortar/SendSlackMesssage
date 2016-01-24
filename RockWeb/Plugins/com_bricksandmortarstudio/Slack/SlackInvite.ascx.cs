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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using RestSharp;
using Rock.Attribute;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_bricksAndMortarStudio.Cms
{
    /// <summary>
    /// Block that syncs selected people to an exchange server.
    /// </summary>
    [DisplayName( "Slack Invite" )]
    [Category( "Bricks and Mortar" )]
    [Description( "Block that allows guests to quickly sign-up for a Slack community." )]

    [TextField( "Site Address", "The address for your site (e.g. rockrms.slack.com)")]
    [DefinedValueField(com.bricksandmortarstudio.Slack.SystemGuid.Slack.SLACK_FULL_ACCESS_TOKEN, "Token", "The Slack API token to use for the call.", true, order: 1 )]
    [TextField( "Button Text", "The button text.", true, "Get My Invite", order: 2 )]
    [CodeEditorField( "Success Message", "The message to display when successful.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, true, @"<div class='alert alert-success margin-t-sm'>Awesome... Your invite has been emailed to you.</div>", order: 3 )]
    [CodeEditorField( "Invited Already Message", "The message to display when successful.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, true, @"<div class='alert alert-warning margin-t-sm'>An invite to this team has already been sent to this address, Slack doesn't support sending another invite.</div>", order: 4 )]
    public partial class SlackInvite : Rock.Web.UI.RockBlock
    {
        
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:Init" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            BlockUpdated += Block_BlockUpdated;

            btnInvite.Text = GetAttributeValue( "ButtonText" );

            CheckSettings();

            // add the current person's email
            if (CurrentPerson != null && !string.IsNullOrWhiteSpace( CurrentPerson.Email ) )
            {
                txtEmail.Text = CurrentPerson.Email;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            CheckSettings();
        }

        /// <summary>
        /// Handles the Click event of the btnInvite control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnInvite_Click( object sender, EventArgs e )
        {
            string requestUrl = string.Format( "/api/users.admin.invite?email={0}&token={1}", txtEmail.Text, GetAttributeValue( "Token" ) );

            var request = new RestRequest( requestUrl, Method.GET );

            string serverAddress = "http://" + GetAttributeValue( "SiteAddress" );

            var client = new RestClient( serverAddress );
            client.Timeout = 12000;
            var response = client.Execute<InviteResponse>( request );

            if ( response.ResponseStatus == ResponseStatus.Completed )
            {
                if ( response.Data.ok )
                {
                    lMessages.Text = GetAttributeValue( "SuccessMessage" );
                }
                else
                {
                    switch ( response.Data.error )
                    {
                        case "already_in_team":
                        case "already_invited":
                            {
                                lMessages.Text = GetAttributeValue( "InvitedAlreadyMessage" );
                                break;
                            }
                        default:
                            {
                                lMessages.Text = string.Format("<div class='alert alert-danger margin-t-sm'>An error occured while processing your request. Message: {0}.</div>", response.Data.error);
                                break;
                            }
                    }
                }
            }
            else
            {
                lMessages.Text = "<div class='alert alert-danger margin-t-sm'>An error occured while processing your request. Please ensure that the Slack site and token are correct.</div>";
            }
        }
        #endregion


        #region Methods

        private void CheckSettings()
        {
            lMessages.Text = "";

            btnInvite.Enabled = true;

            string siteAddress = GetAttributeValue( "SiteAddress" );
            string token = GetAttributeValue( "Token" );

            if ( string.IsNullOrWhiteSpace( siteAddress ) )
            {
                lMessages.Text = "<div class='alert alert-warning margin-t-sm'>Your Slack site setting is missing.</div>";
                btnInvite.Enabled = false;
            }

            if ( string.IsNullOrWhiteSpace( token ) )
            {
                lMessages.Text += "<div class='alert alert-warning margin-t-sm'>Your Slack API token is missing.</div>";
            }
        }

        #endregion
        
    }

    /// <summary>
    /// 
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class InviteResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="InviteResponse"/> is ok.
        /// </summary>
        /// <value>
        ///   <c>true</c> if ok; otherwise, <c>false</c>.
        /// </value>
        public bool ok { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public string error { get; set; }
    }
}