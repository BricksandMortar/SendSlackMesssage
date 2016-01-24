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
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Net;

namespace com.bricksandmortarstudio.Slack
{
    /// <summary>
    /// Prompts user for login credentials.
    /// </summary>
    [DisplayName( "Slack Verification" )]
    [Category( "Bricks and Mortar" )]
    [Description( "Prompts a user for Slack login credentials." )]

    [TextField( "Client ID", "The Slack Client ID" )]
    [TextField( "Client Secret", "The Slack Client Secret" )]
    [TextField( "Team ID", "Your Team ID" )]
    [LinkedPage( "Help Page", "Page to navigate to when user selects 'Help' option (if blank will use 'ForgotUserName' page route)", true, "", "", 1 )]
    [CodeEditorField( "Prompt Message", "Optional text (HTML) to display above username and password fields.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, false, @"", "", 8 )]
    public partial class OAuthLogin : Rock.Web.UI.RockBlock
    {

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( !IsPostBack && IsReturningFromAuthentication( Request ) )
            {
                string userName;
                string returnUrl;
                if ( Authenticate( Request, out userName, out returnUrl ) )
                {
                    ReturnUser(returnUrl );
                }
            }

            else if ( !Page.IsPostBack )
            {
               lPromptMessage.Text = GetAttributeValue( "PromptMessage" );
            }

            pnlMessage.Visible = false;
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void lbSlackAuth_Click( object sender, EventArgs e )
        {
            if (!(sender is LinkButton))
            {
                return;
            }
            var lb = (LinkButton) sender;

            if (lb.ID != "lbSlackAuth")
            {
                return;
            }
            var uri = GenerateLoginUrl( Request );
            Response.Redirect( uri.AbsoluteUri, false );
            Context.ApplicationInstance.CompleteRequest();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Displays the error.
        /// </summary>
        /// <param name="message">The message.</param>
        private void DisplayError( string message )
        {
            pnlMessage.Controls.Clear();
            pnlMessage.Controls.Add( new LiteralControl( message ) );
            pnlMessage.Visible = true;
        }

        /// <summary>
        /// Logs in the user.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        private void ReturnUser(string returnUrl )
        {
            if ( !string.IsNullOrWhiteSpace( returnUrl ) )
            {
                string redirectUrl = Server.UrlDecode( returnUrl );
                if (redirectUrl != null)
                {
                    Response.Redirect( redirectUrl );
                }
                Context.ApplicationInstance.CompleteRequest();
            }
            else
            {
                RockPage.Layout.Site.RedirectToDefaultPage();
            }
        }

        #endregion

        #region SlackAuth
        /// <summary>
        /// Tests the Http Request to determine if authentication should be tested by this
        /// authentication provider.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public bool IsReturningFromAuthentication( HttpRequest request )
        {
            return !string.IsNullOrWhiteSpace( request.QueryString["code"] ) &&
                   !string.IsNullOrWhiteSpace( request.QueryString["state"] );
        }

        /// <summary>
        /// Generates the login URL.
        /// </summary>
        /// <param name="request">Forming the URL to obtain user consent</param>
        /// <returns></returns>
        public Uri GenerateLoginUrl( HttpRequest request )
        {
            string returnUrl = request.QueryString["returnurl"];
            string redirectUri = GetRedirectUrl( request );
            string state = returnUrl ?? FormsAuthentication.DefaultUrl;
            string teamId = GetAttributeValue( "TeamId" );
            Session.Add( "state", state );
            return new Uri( string.Format( "https://slack.com/oauth/authorize?&client_id={0}&redirect_uri={1}&state={2}&scope=channels:write groups:write users:read identify{3}",
                GetAttributeValue( "ClientID" ),
                HttpUtility.UrlEncode( redirectUri ),
                HttpUtility.UrlEncode( returnUrl ?? FormsAuthentication.DefaultUrl ), !string.IsNullOrEmpty( teamId ) ? "&" + teamId : null ) );
        }

        /// <summary>
        /// Authenticates the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="username">The username.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns></returns>
        public bool Authenticate( HttpRequest request, out string username, out string returnUrl )
        {
            username = string.Empty;
            returnUrl = request.QueryString["State"];
            string redirectUri = GetRedirectUrl( request );
            string state = Session["state"].ToStringSafe();
            Session.Remove( state );

            if ( returnUrl == state )
            {
                try
                {
                    var restClient = new RestClient(
                        string.Format( "https://slack.com/api/oauth.access?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}",
                            GetAttributeValue( "ClientID" ),
                            HttpUtility.UrlEncode( redirectUri ),
                            GetAttributeValue( "ClientSecret" ),
                            request.QueryString["code"] ) );
                    var restRequest = new RestRequest( Method.POST );
                    var restResponse = restClient.Execute( restRequest );

                    if ( restResponse.StatusCode == HttpStatusCode.OK )
                    {
                        var slackToken = JObject.Parse( restResponse.Content );
                        string accessToken = slackToken["access_token"].ToStringSafe();

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            restRequest = new RestRequest( Method.GET );
                            restRequest.AddParameter( "token", accessToken );
                            restRequest.RequestFormat = DataFormat.Json;
                            restRequest.AddHeader( "Accept", "application/json" );
                            restClient = new RestClient( "https://slack.com/api/auth.test" );
                            restResponse = restClient.Execute( restRequest );
                            if (restResponse.StatusCode == HttpStatusCode.OK)
                            {
                                var slackUser = JObject.Parse( restResponse.Content );
                                username = slackUser["user"].ToString();
                                if ( CurrentPerson != null )
                                {
                                    var userLoginService = new UserLoginService( new RockContext() );
                                    var user = userLoginService.GetByUserName( "Slack_" + username );
                                    if (user == null)
                                    {
                                        int typeId = EntityTypeCache.Read( typeof( com.bricksandmortarstudio.Slack.Authentication.Slack) ).Id;
                                        UserLoginService.Create( new RockContext(), CurrentPerson, AuthenticationServiceType.External, typeId, "Slack_" + username, accessToken, true );
                                    }
                                    else
                                    {
                                        var globalMergeFields = Rock.Web.Cache.GlobalAttributesCache.GetMergeFields( null );
                                        lPreviouslyCompletedMessage.Text = GetAttributeValue( "PreviouslyCompleted" ).ResolveMergeFields( globalMergeFields );
                                    }
                                    
                                }
                            }

                        }
                    }

                }

                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( ex, HttpContext.Current );
                }

                return !string.IsNullOrWhiteSpace( username );
            }
            else
            {
                return false;
            }
        }

        private string GetRedirectUrl( HttpRequest request )
        {
            var uri = new Uri( request.Url.ToString() );
            return uri.Scheme + "://" + uri.GetComponents( UriComponents.HostAndPort, UriFormat.UriEscaped ) + uri.LocalPath;
        }
        #endregion
    }
    
}