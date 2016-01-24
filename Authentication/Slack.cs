using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json;
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
        private static string _state;
        private string _teamId;

        /// <summary>
        /// Gets the type of the service.
        /// </summary>
        /// <value>
        /// The type of the service.
        /// </value>
        public override AuthenticationServiceType ServiceType => AuthenticationServiceType.External;

        /// <summary>
        /// Determines if user is directed to another site (i.e. Facebook, Gmail, Twitter, etc) to confirm approval of using
        /// that site's credentials for authentication.
        /// </summary>
        /// <value>
        /// The requires remote authentication.
        /// </value>
        public override bool RequiresRemoteAuthentication => true;

        /// <summary>
        /// Tests the Http Request to determine if authentication should be tested by this
        /// authentication provider.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public override bool IsReturningFromAuthentication( HttpRequest request )
        {
            return !string.IsNullOrWhiteSpace( request.QueryString["code"] ) &&
                !string.IsNullOrWhiteSpace( request.QueryString["state"]);
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
            _state = returnUrl ?? FormsAuthentication.DefaultUrl;
            _teamId = GetAttributeValue( "TeamId" );
            return new Uri(
                $"https://slack.com/oauth/authorize?&client_id={GetAttributeValue("ClientID")}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&state={HttpUtility.UrlEncode(returnUrl ?? FormsAuthentication.DefaultUrl)}&scope=channels:write groups:write users:read identify{(!string.IsNullOrEmpty(_teamId) ? "&" + _teamId : null)}");
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

            if (returnUrl != _state)
            {
                return !string.IsNullOrWhiteSpace(username);
            }
            try
            {
                // Get a new OAuth Access Token for the 'code' that was returned from the Slack user consent redirect
                var restClient = new RestClient(
                    $"https://slack.com/api/oauth.access?client_id={GetAttributeValue("ClientID")}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&client_secret={GetAttributeValue("ClientSecret")}&code={request.QueryString["code"]}");
                var restRequest = new RestRequest( Method.POST );
                var restResponse = restClient.Execute( restRequest );

                if ( restResponse.StatusCode == HttpStatusCode.OK )
                {
                    var slackToken = JObject.Parse( restResponse.Content );
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
                            string userId = JObject.Parse( restResponse.Content )["user_id"].ToString();
                            username = GetSlackUser( userId, accessToken, _teamId );
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

        /// <summary>
        /// Gets the URL of an image that should be displayed.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string ImageUrl()
        {
            return "";
        }

        private string GetRedirectUrl( HttpRequest request )
        {
            var uri = new Uri( request.Url.ToString() );
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
        public override bool SupportsChangePassword => false;

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
        /// <param name="userId">The user identifier.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="teamId">The team identifier.</param>
        /// <returns></returns>
        public string GetSlackUser( string userId, string accessToken, string teamId )
        {
            var client = new RestClient( "https://slack.com/api/users.info" );
            var request = new RestRequest( Method.POST );
            request.AddParameter( "token", accessToken );
            request.AddParameter( "user", userId );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            var restResponse = client.Execute( request );

            var slackUserResponse = JsonConvert.DeserializeObject<SlackUserResponse>( restResponse.Content );
            var slackUser = slackUserResponse.user;

            string username = "Slack_" + slackUser.name + "_" + userId;
            UserLogin user;



            var rockContext = new RockContext();

                // Query for an existing user 
                var userLoginService = new UserLoginService( rockContext );
                user = userLoginService.GetByUserName( username );

                // If no user was found, see if we can find a match in the person table
                if ( user == null && ( teamId == null || slackUser.team_id.Equals( teamId ) ) )
                {
                    var profile = slackUser.profile;
                    // Get name/email from Slack login
                    string lastName = profile.last_name;
                    string firstName = profile.first_name;
                    string email = string.Empty;
                    try { email = profile.email; }
                    catch
                    {
                        // ignored
                    }

                    Person person = null;

                    // If person had an email, get the first person with the same name and email address.
                    if ( !string.IsNullOrWhiteSpace( email ) )
                    {
                        var personService = new PersonService( rockContext );
                        var people = personService.GetByMatch( firstName, lastName, email ).ToList();
                        if ( people.Count == 1 )
                        {
                            person = people.First();
                        }
                    }

                    int personRecordTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                    int personStatusPending = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ).Id;
                
                    rockContext.WrapTransaction( () =>
                    {
                        if ( person == null )
                        {
                            person = new Person
                            {
                                IsSystem = false,
                                RecordTypeValueId = personRecordTypeId,
                                RecordStatusValueId = personStatusPending,
                                FirstName = firstName,
                                LastName = lastName,
                                Email = email,
                                IsEmailActive = true,
                                EmailPreference = EmailPreference.EmailAllowed,
                                Gender = Gender.Unknown
                            };

                            if ( person != null )
                            {
                                PersonService.SaveNewPerson( person, rockContext, null );
                            }
                        }

                        if (person == null)
                        {
                            return;
                        }
                        int typeId = EntityTypeCache.Read( typeof( Slack ) ).Id;
                        // ReSharper disable once AccessToModifiedClosure
                        user = UserLoginService.Create( rockContext, person, AuthenticationServiceType.External, typeId, username, accessToken, true );
                    } );
                }
            if (user == null)
            {
                return username;
            }
                {
                    username = user.UserName;

                    if (!user.PersonId.HasValue)
                    {
                        return username;
                    }
                    var personService = new PersonService( rockContext );
                    var person = personService.Get( user.PersonId.Value );
                    if (person == null || person.PhotoId.HasValue)
                    {
                        return username;
                    }
                    string photoUrl = !string.IsNullOrEmpty( slackUser.profile.image_512 ) ? slackUser.profile.image_512 : !string.IsNullOrEmpty( slackUser.profile.image_192 ) ? slackUser.profile.image_192 : null;
                    if (photoUrl == null)
                    {
                        return username;
                    }
                    var restClient = new RestClient( photoUrl );
                    var restRequest = new RestRequest( Method.GET );
                    restResponse = restClient.Execute( restRequest );
                    if (restResponse.StatusCode != HttpStatusCode.OK)
                    {
                        return username;
                    }
                    var bytes = restResponse.RawBytes;

                    // Create and save the image
                    var fileType = new BinaryFileTypeService( rockContext ).Get( Rock.SystemGuid.BinaryFiletype.PERSON_IMAGE.AsGuid() );
                    if (fileType == null)
                    {
                        return username;
                    }
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

                return username;
        }

        public override void SetPassword( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Slack User Object
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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

    [SuppressMessage("ReSharper", "InconsistentNaming")]
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

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SlackUserResponse
    {
        public bool ok { get; set; }
        public SlackUser user { get; set; }
    }


}
