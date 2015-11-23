using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RestSharp;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace com.bricksandmortarstudio.Slack.Authentication
{
    [Description( "Slack Authentication Provider" )]
    [Export( typeof( AuthenticationComponent ) )]
    [ExportMetadata( "ComponentName", "Slack" )]

    [TextField( "Client ID", "Your Slack Client ID" )]
    [TextField( "Client Secret", "Your Slack Client Secret" )]
    [TextField( "Team ID", "Your Slack Team's ID", false)]

    public class Slack : AuthenticationComponent
    {
        private static string state;
        private string teamId;

        /// <summary>
        /// Gets the type of the service.
        /// </summary>
        /// <value>
        /// The type of the service.
        /// </value>
        public override AuthenticationServiceType ServiceType
        {
            get { return AuthenticationServiceType.External; }
        }

        /// <summary>
        /// Determines if user is directed to another site (i.e. Facebook, Gmail, Twitter, etc) to confirm approval of using
        /// that site's credentials for authentication.
        /// </summary>
        /// <value>
        /// The requires remote authentication.
        /// </value>
        public override bool RequiresRemoteAuthentication
        {
            get { return true; }
        }

        /// <summary>
        /// Tests the Http Request to determine if authentication should be tested by this
        /// authentication provider.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public override bool IsReturningFromAuthentication( HttpRequest request )
        {
            return ( !String.IsNullOrWhiteSpace( request.QueryString["code"] ) &&
                !String.IsNullOrWhiteSpace( request.QueryString["state"] ) );
        }

        /// <summary>
        /// Generates the login URL.
        /// </summary>
        /// <param name="request">Forming the URL to obtain user consent</param>
        /// <returns></returns>
        public override Uri GenerateLoginUrl( HttpRequest request )
        {
            string returnUrl = request.QueryString["returnurl"];
            string redirectUri = GetRedirectUrl( request );
            state = returnUrl ?? FormsAuthentication.DefaultUrl;
            teamId = GetAttributeValue( "TeamId" );
            return new Uri( string.Format( "https://slack.com/oauth/authorize?&client_id={0}&redirect_uri={1}&state={2}&scope=channels:write groups:write users:read identify{3}",
                GetAttributeValue( "ClientID" ),
                HttpUtility.UrlEncode( redirectUri ),
                HttpUtility.UrlEncode( returnUrl ?? FormsAuthentication.DefaultUrl ), (!string.IsNullOrEmpty(teamId) ? "&"+teamId : null ) ));
        }

        /// <summary>
        /// Authenticates the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="username">The username.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns></returns>
        public override bool Authenticate( HttpRequest request, out string username, out string returnUrl )
        {
            username = string.Empty;
            returnUrl = request.QueryString["State"];
            string redirectUri = GetRedirectUrl( request );

            if ( returnUrl == state )
            {
                try
                {
                    // Get a new OAuth Access Token for the 'code' that was returned from the Google user consent redirect
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
                        JObject slackToken = JObject.Parse( restResponse.Content );
                        string accessToken = slackToken["access_token"].ToStringSafe();

                        if ( !string.IsNullOrEmpty( accessToken ) )
                        {
                            restRequest = new RestRequest( Method.GET );
                            restRequest.AddParameter( "token", accessToken );
                            restRequest.RequestFormat = DataFormat.Json;
                            restRequest.AddHeader( "Accept", "application/json" );
                            restClient = new RestClient( "https://slack.com/api/auth.test" );
                            restResponse = restClient.Execute( restRequest );

                            if ( restResponse.StatusCode == HttpStatusCode.OK )
                            {
                                string userID = JObject.Parse( restResponse.Content )["user_id"].ToString();
                                username = GetSlackUser( userID, accessToken, teamId );
                            }
                        }

                    }
                }

                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( ex, HttpContext.Current );
                }
            }

            return !string.IsNullOrWhiteSpace( username );
        }

        /// <summary>
        /// Gets the URL of an image that should be displayed.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override String ImageUrl()
        {
            return "";
        }

        private string GetRedirectUrl( HttpRequest request )
        {
            Uri uri = new Uri( request.Url.ToString() );
            return uri.Scheme + "://" + uri.GetComponents( UriComponents.HostAndPort, UriFormat.UriEscaped ) + uri.LocalPath;
        }

        /// <summary>
        /// Authenticates the user based on user name and password
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Authenticate( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Encodes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string EncodePassword( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating whether [supports change password].
        /// </summary>
        /// <value>
        /// <c>true</c> if [supports change password]; otherwise, <c>false</c>.
        /// </value>
        public override bool SupportsChangePassword
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="oldPassword">The old password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <param name="warningMessage">The warning message.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool ChangePassword( UserLogin user, string oldPassword, string newPassword, out string warningMessage )
        {
            warningMessage = "not supported";
            return false;
        }

        /// <summary>
        /// Gets the name of the Google user.
        /// </summary>
        /// <param name="googleUser">The Google user.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns></returns>
        public string GetSlackUser( string userId, string accessToken, string teamId )
        {
            string username = string.Empty;

            var client = new RestClient( "https://slack.com/api/users.info" );
            var request = new RestRequest( Method.POST );
            request.AddParameter( "token", accessToken );
            request.AddParameter( "user", userId );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            var restResponse = client.Execute( request );

            SlackUserResponse slackUserResponse = JsonConvert.DeserializeObject<SlackUserResponse>( restResponse.Content );
            SlackUser slackUser = slackUserResponse.user;

            string userName = "Slack_" + slackUser.name;
            UserLogin user = null;



            using ( var rockContext = new RockContext() )
            {

                // Query for an existing user 
                var userLoginService = new UserLoginService( rockContext );
                user = userLoginService.GetByUserName( userName );

                // If no user was found, see if we can find a match in the person table
                if ( user == null && ( teamId == null || slackUser.team_id.Equals( teamId ) ) )
                {
                    var profile = slackUser.profile;
                    // Get name/email from Google login
                    string lastName = profile.last_name;
                    string firstName = profile.first_name;
                    string email = string.Empty;
                    try { email = profile.email; }
                    catch { }

                    Person person = null;

                    // If person had an email, get the first person with the same name and email address.
                    if ( !string.IsNullOrWhiteSpace( email ) )
                    {
                        var personService = new PersonService( rockContext );
                        var people = personService.GetByMatch( firstName, lastName, email );
                        if ( people.Count() == 1 )
                        {
                            person = people.First();
                        }
                    }

                    var personRecordTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                    var personStatusPending = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;

                    rockContext.WrapTransaction( () =>
                    {
                        if ( person == null )
                        {
                            person = new Person();
                            person.IsSystem = false;
                            person.RecordTypeValueId = personRecordTypeId;
                            person.RecordStatusValueId = personStatusPending;
                            person.FirstName = firstName;
                            person.LastName = lastName;
                            person.Email = email;
                            person.IsEmailActive = true;
                            person.EmailPreference = EmailPreference.EmailAllowed;
                            person.Gender = Gender.Unknown;

                            if ( person != null )
                            {
                                PersonService.SaveNewPerson( person, rockContext, null, false );
                            }
                        }

                        if ( person != null )
                        {
                            int typeId = EntityTypeCache.Read( typeof( Slack ) ).Id;
                            user = UserLoginService.Create( rockContext, person, AuthenticationServiceType.External, typeId, "Slack_" + username, accessToken, true );
                        }

                    } );
                }
                if ( user != null )
                {
                    username = user.UserName;

                    if ( user.PersonId.HasValue )
                    {
                        var personService = new PersonService( rockContext );
                        var person = personService.Get( user.PersonId.Value );
                        if ( person != null && !person.PhotoId.HasValue )
                        {
                            string photoUrl = ( !string.IsNullOrEmpty( slackUser.profile.image_512 ) ) ? slackUser.profile.image_512 : ( !string.IsNullOrEmpty( slackUser.profile.image_512 ) ) ? slackUser.profile.image_192 : null;
                            if ( photoUrl != null )
                            {
                                var converter = new ExpandoObjectConverter();
                                var restClient = new RestClient( photoUrl );
                                var restRequest = new RestRequest( Method.GET );
                                restResponse = restClient.Execute( restRequest );
                                if ( restResponse.StatusCode == HttpStatusCode.OK )
                                {
                                    var bytes = restResponse.RawBytes;

                                    // Create and save the image
                                    BinaryFileType fileType = new BinaryFileTypeService( rockContext ).Get( Rock.SystemGuid.BinaryFiletype.PERSON_IMAGE.AsGuid() );
                                    if ( fileType != null )
                                    {
                                        var binaryFileService = new BinaryFileService( rockContext );
                                        var binaryFile = new BinaryFile();
                                        binaryFileService.Add( binaryFile );
                                        binaryFile.IsTemporary = false;
                                        binaryFile.BinaryFileType = fileType;
                                        binaryFile.MimeType = "image/jpeg";
                                        binaryFile.FileName = user.Person.NickName + user.Person.LastName + ".jpg";
                                        binaryFile.ContentStream = new MemoryStream( bytes );

                                        rockContext.SaveChanges();

                                        person.PhotoId = binaryFile.Id;
                                        rockContext.SaveChanges();
                                    }
                                }
                            }

                        }

                    }
                }

                return username;
            }
        }
    }


    /// <summary>
    /// Slack User Object
    /// </summary>

    public class Profile
    {
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string title { get; set; }
        public string skype { get; set; }
        public string phone { get; set; }
        public string real_name { get; set; }
        public string real_name_normalized { get; set; }
        public string email { get; set; }
        public string image_24 { get; set; }
        public string image_32 { get; set; }
        public string image_48 { get; set; }
        public string image_72 { get; set; }
        public string image_192 { get; set; }
        public string image_512 { get; set; }
    }

    public class SlackUser
    {
        public string id { get; set; }
        public string team_id { get; set; }
        public string name { get; set; }
        public bool deleted { get; set; }
        public object status { get; set; }
        public string color { get; set; }
        public string real_name { get; set; }
        public string tz { get; set; }
        public string tz_label { get; set; }
        public int tz_offset { get; set; }
        public Profile profile { get; set; }
        public bool is_admin { get; set; }
        public bool is_owner { get; set; }
        public bool is_primary_owner { get; set; }
        public bool is_restricted { get; set; }
        public bool is_ultra_restricted { get; set; }
        public bool is_bot { get; set; }
        public bool has_2fa { get; set; }
    }

    public class SlackUserResponse
    {
        public bool ok { get; set; }
        public SlackUser user { get; set; }
    }


}
