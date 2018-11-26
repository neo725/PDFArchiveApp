using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Principal;

using PDFArchiveApp.Services;

using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Windows.UI.Popups;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Flows;

namespace PDFArchiveApp.Views
{
    // TODO WTS: Add other settings as necessary. For help see https://github.com/Microsoft/WindowsTemplateStudio/blob/master/docs/pages/settings-codebehind.md
    // TODO WTS: Change the URL for your privacy policy in the Resource File, currently set to https://YourPrivacyUrlGoesHere
    public sealed partial class SettingsPage : Page, INotifyPropertyChanged
    {
        private ElementTheme _elementTheme = ThemeSelectorService.Theme;
        
        private static string ApplicationName = Package.Current.DisplayName;

        /////// <summary>
        /////// OAuth 2.0 client configuration.
        /////// </summary>
        ////const string clientId = "338523250911-1ad660gvr6oqqcanpq01825vpcis0ivj.apps.googleusercontent.com";
        ////const string clientSecret = "OzwvUgFDN_DXmdoEeJijhi3g";
        
        //const string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        //const string tokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";
        //const string userInfoEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";


        public ElementTheme ElementTheme
        {
            get { return _elementTheme; }

            set { Set(ref _elementTheme, value); }
        }

        private string _versionDescription;

        public string VersionDescription
        {
            get { return _versionDescription; }

            set { Set(ref _versionDescription, value); }
        }

        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Initialize();
        }

        private void Initialize()
        {
            VersionDescription = GetVersionDescription();
        }

        private string GetVersionDescription()
        {
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{package.DisplayName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private bool IsUserAdministrator()
        {
            bool isAdmin = false;
            try
            {
                // this code ref from :
                // https://stackoverflow.com/questions/2818179/how-do-i-force-my-net-application-to-run-as-administrator
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }

            return isAdmin;
        }

        private async void ThemeChanged_CheckedAsync(object sender, RoutedEventArgs e)
        {
            var param = (sender as RadioButton)?.CommandParameter;

            if (param != null)
            {
                await ThemeSelectorService.SetThemeAsync((ElementTheme)param);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //var userId = "user";

            //GoogleOAuthBroker.UserId = userId;

            //GoogleOAuthBroker.clientId = clientId;
            //GoogleOAuthBroker.clientKey = clientSecret;
            ////GoogleOAuthBroker.callbackUrl = "urn:ietf:wg:oauth:2.0:oob";

            var tokenResponse = GoogleOAuthBroker.SavedGoogleAccessToken;

            if (true || tokenResponse == null)
            {
                tokenResponse = await GoogleOAuthBroker.InvokeGoogleSignIn();

                //var accessToken = new GoogleAccessToken(json);
            }

            var secrets = new ClientSecrets()
            {
                ClientId = GoogleOAuthBroker.ClientId,
                ClientSecret = GoogleOAuthBroker.ClientKey
            };


            // 2018-10-1 取得
            // "access_token":"ya29.GlspBjPp5fg4L5_ardcFTvo0-mEhny0_U9AWp5r2MP0r2E02clwwz-hE_g6M5NQkiEhSgOLG69v6bQOR6BD6WFzTzhgDaxYtBRjc-ObQc_yUnG0D-qFecG0_G7jG","expires_in":3600,"refresh_token":"1/zT7RqFnZQJC7UWU_eIeyRSEAYPpNsvIegikelpVtPWYVsKnao087EK4jQ_tzJ0FC"
            //var token = new TokenResponse { RefreshToken = "4/AAAgYL1AbCENklz6-mtufDjdty62SY8-i-ySYJsPTjHrJ4bIp9oLbPM" };

            var token = new TokenResponse {
                AccessToken = tokenResponse.access_token,
                ExpiresInSeconds = tokenResponse.expires_in,
                RefreshToken = tokenResponse.refresh_token
            };

            var credentials = new UserCredential(new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = secrets
                }),
                GoogleOAuthBroker.UserId,
                token
                );

            
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials,
                ApplicationName = ApplicationName
            });

            // Define parameters of request.
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Spaces = "appDataFolder";
            listRequest.PageSize = 1000; // range : [1 - 1000]
            // https://developers.google.com/drive/api/v3/migration#fields
            listRequest.Fields = "nextPageToken, files(id, name)";
            // Search for Parameters : https://developers.google.com/drive/api/v3/search-parameters
            //listRequest.Q = "'appDataFolder' in parents";

            // List files.
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute()
                .Files;
            Debug.WriteLine("Files:");
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    Debug.WriteLine("{0} ({1})", file.Name, file.Id);
                }
            }
            else
            {
                Debug.WriteLine("No files found.");
            }

            return;
            //if (IsUserAdministrator() == false)
            //{
            //    await new MessageDialog("程式不具有管理員權限，無法進行此操作!").ShowAsync();

            //    return;
            //}

            // PDFArchiveApp 2
            var oauth2 = new Chilkat.OAuth2();

            //  For Google OAuth2, set the listen port equal to the port used
            //  in the Authorized Redirect URL for the Client ID.
            //  For example, in this case the Authorized Redirect URL would be http://localhost:55568/
            //  Your app should choose a port not likely not used by any other application.
            oauth2.ListenPort = 55568;

            oauth2.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
            oauth2.TokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";

            //  Replace these with actual values.
            oauth2.ClientId = "338523250911-mc628v5pbmdkcos31okktdtgofara1bp.apps.googleusercontent.com";
            oauth2.ClientSecret = "F9I5-OTwr7FsQS6leZYHc8rG";

            oauth2.CodeChallenge = true;
            oauth2.CodeChallengeMethod = "S256";

            //  This is the scope for Google Drive.
            //  See https://developers.google.com/identity/protocols/googlescopes
            oauth2.Scope = "https://www.googleapis.com/auth/drive";

            //  Begin the OAuth2 three-legged flow.  This returns a URL that should be loaded in a browser.
            string url = oauth2.StartAuth();
            if (oauth2.LastMethodSuccess != true)
            {
                Debug.WriteLine(oauth2.LastErrorText);
                return;
            }

            //  At this point, your application should load the URL in a browser.
            //  For example,
            //  in C#:  System.Diagnostics.Process.Start(url);
            //  in Java: Desktop.getDesktop().browse(new URI(url));
            //  in VBScript: Set wsh=WScript.CreateObject("WScript.Shell")
            //               wsh.Run url
            //  The Google account owner would interactively accept or deny the authorization request.

            //  Add the code to load the url in a web browser here...
            //  Add the code to load the url in a web browser here...
            //  Add the code to load the url in a web browser here...

            //  Now wait for the authorization.
            //  We'll wait for a max of 30 seconds.
            int numMsWaited = 0;
            while ((numMsWaited < 30000) && (oauth2.AuthFlowState < 3))
            {
                oauth2.SleepMs(100);
                numMsWaited = numMsWaited + 100;
            }

            //  If there was no response from the browser within 30 seconds, then
            //  the AuthFlowState will be equal to 1 or 2.
            //  1: Waiting for Redirect. The OAuth2 background thread is waiting to receive the redirect HTTP request from the browser.
            //  2: Waiting for Final Response. The OAuth2 background thread is waiting for the final access token response.
            //  In that case, cancel the background task started in the call to StartAuth.
            if (oauth2.AuthFlowState < 3)
            {
                oauth2.Cancel();
                Debug.WriteLine("No response from the browser!");
                return;
            }

            //  Check the AuthFlowState to see if authorization was granted, denied, or if some error occurred
            //  The possible AuthFlowState values are:
            //  3: Completed with Success. The OAuth2 flow has completed, the background thread exited, and the successful JSON response is available in AccessTokenResponse property.
            //  4: Completed with Access Denied. The OAuth2 flow has completed, the background thread exited, and the error JSON is available in AccessTokenResponse property.
            //  5: Failed Prior to Completion. The OAuth2 flow failed to complete, the background thread exited, and the error information is available in the FailureInfo property.
            if (oauth2.AuthFlowState == 5)
            {
                Debug.WriteLine("OAuth2 failed to complete.");
                Debug.WriteLine(oauth2.FailureInfo);
                return;
            }

            if (oauth2.AuthFlowState == 4)
            {
                Debug.WriteLine("OAuth2 authorization was denied.");
                Debug.WriteLine(oauth2.AccessTokenResponse);
                return;
            }

            if (oauth2.AuthFlowState != 3)
            {
                Debug.WriteLine("Unexpected AuthFlowState:" + Convert.ToString(oauth2.AuthFlowState));
                return;
            }

            //  Save the full JSON access token response to a file.
            Chilkat.StringBuilder sbJson = new Chilkat.StringBuilder();
            sbJson.Append(oauth2.AccessTokenResponse);
            sbJson.WriteFile("qa_data/tokens/googleDrive.json", "utf-8", false);

            //  The saved JSON response looks like this:

            //  	{
            //  	 "access_token": "ya39.Ci-XA_C5bGgRDC3UaD-h0_NeL-DVIQnI2gHtBBBHkZzrwlARkwX6R3O0PCDEzRlfaQ",
            //  	 "token_type": "Bearer",
            //  	 "expires_in": 3600,
            //  	 "refresh_token": "1/r_2c_7jddspcdfesrrfKqfXtqo08D6Q-gUU0DsdfVMsx0c"
            //  	}
            // 
            Debug.WriteLine("OAuth2 authorization granted!");
            Debug.WriteLine("Access Token = " + oauth2.AccessToken);
        }
    }
}
