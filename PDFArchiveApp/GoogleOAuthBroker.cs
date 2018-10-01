using PDFArchiveApp.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Security.Authentication.Web;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace PDFArchiveApp
{
    /// <summary>
    /// ref : https://github.com/Romasz/UWPSamples/blob/master/GoogleRestAuth/GoogleRestAuth/GoogleAPI/GoogleClient.cs
    /// </summary>
    public class GoogleOAuthBroker
    {
        private enum TokenTypes { AccessToken, RefreshToken }
        private const string GoogleTokenTime = "GoogleTokenTime";

        public static string clientId = String.Empty;
        public static string clientKey = String.Empty;

        //public static string callbackUrl = "pw.oauth2:/oauth2redirect";
        //public static string callbackUrl = "uwp.app:/oauth2redirect";
        //public static string callbackUrl = "urn:ietf:wg:oauth:2.0:oob";
        public static string callbackUrl = "urn:ietf:wg:oauth:2.0:oob:auto";
        public static string scopes = "https://www.googleapis.com/auth/drive";

        public static string TokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";

        private static PasswordVault vault = new PasswordVault();
        private static HttpClient httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        private static string accessToken = string.Empty;
        private static GoogleAccessToken googleAccessToken = null;

        private static bool isAuthorized = false;
        public static bool IsAuthorized
        {
            get { return isAuthorized; }
            set { isAuthorized = value; }
        }

        public static string UserId { get; set; }


        private static Lazy<DateTimeOffset> tokenLastAccess = new Lazy<DateTimeOffset>(() =>
        {
            return DateTimeOffset.ParseExact(
                    Settings.ReadOrDefault(GoogleTokenTime, DateTimeOffset.MinValue.ToString("MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture)),
                "MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        });

        public static DateTimeOffset TokenLastAccess
        {
            get { return tokenLastAccess.Value; }
            set
            {
                tokenLastAccess = new Lazy<DateTimeOffset>(() => value);
                Settings.Save(GoogleTokenTime, value.ToString("MM/dd/yyyy HH:mm", CultureInfo.InvariantCulture));
            }
        }

        private static Lazy<GoogleAccessToken> savedGoogleAccessToken = new Lazy<GoogleAccessToken>(() =>
        {
            try
            {
                var json = Settings.ReadOrDefault<string>("GoogleAccessToken", null);
                return String.IsNullOrEmpty(json) ? null : new GoogleAccessToken(json);
            }
            catch (Exception ex)
            {
                return null;
            }
        });

        public static GoogleAccessToken SavedGoogleAccessToken
        {
            get
            {
                var token = savedGoogleAccessToken.Value;

                if (token != null) {
                    TokenLastAccess = token.IssueDateTimeUTC ?? default(DateTimeOffset);
                }
                return token;
            }
            set
            {
                savedGoogleAccessToken = new Lazy<GoogleAccessToken>(() => new GoogleAccessToken(value.RawJsonString));
                Settings.Save("GoogleAccessToken", value.RawJsonString);
            }
        }
        
        //public static async Task<String> InvokeGoogleSignIn()
        public static async Task<GoogleAccessToken> InvokeGoogleSignIn()
        {
            try
            {
                string state = randomDataBase64url(32);
                string code_verifier = randomDataBase64url(32);
                string code_challenge = base64urlencodeNoPadding(sha256(code_verifier));

                String GoogleURL = GetOAuthUrl(clientId, callbackUrl, state, code_verifier, code_challenge);

                System.Uri StartUri = new Uri(GoogleURL);
                // When using the desktop flow, the success code is displayed in the html title of this end uri
                System.Uri EndUri = new Uri("https://accounts.google.com/o/oauth2/approval");
                //System.Uri EndUri = new Uri(callbackUrl);

                var webAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.UseTitle,
                    StartUri,
                    EndUri);

                if (webAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
                {
                    var result = webAuthenticationResult.ResponseData.ToString();

                    if (result.Contains("Success"))
                    {
                        //// 取得需要的 code
                        //String code = result.Replace("Success ", "");
                        //Int32 firstIdx = code.IndexOf("code=");
                        //code = code.Substring(firstIdx + 5);
                        //code = code.Substring(0, code.IndexOf("&") == -1 ? code.Length : code.IndexOf("&"));
                        ////result = await RedirectGetAccessToken(code);
                        await GetAccessToken(result.Substring(result.IndexOf(' ') + 1), state, code_verifier);
                    }

                    //return result;
                    return googleAccessToken;
                }
                else if (webAuthenticationResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
                {
                    //return "HTTP Error returned by AuthenticateAsync() : " + webAuthenticationResult.ResponseErrorDetail.ToString();
                    throw new Exception("HTTP Error returned by AuthenticateAsync() : " + webAuthenticationResult.ResponseErrorDetail.ToString());
                }
                else
                {
                    //return "Error returned by AuthenticateAsync() : " + webAuthenticationResult.ResponseStatus.ToString();
                    throw new Exception("Error returned by AuthenticateAsync() : " + webAuthenticationResult.ResponseStatus.ToString());
                }
            }
            catch (Exception ex)
            {
                //
                // Bad Parameter, SSL/TLS Errors and Network Unavailable errors are to be handled here.
                //
                //return ex.Message;
                throw ex;
            }
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        public static string randomDataBase64url(uint length)
        {
            IBuffer buffer = CryptographicBuffer.GenerateRandom(length);
            return base64urlencodeNoPadding(buffer);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        public static IBuffer sha256(string inputStirng)
        {
            HashAlgorithmProvider sha = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(inputStirng, BinaryStringEncoding.Utf8);
            return sha.HashData(buff);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string base64urlencodeNoPadding(IBuffer buffer)
        {
            string base64 = CryptographicBuffer.EncodeToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        private static String GetOAuthUrl(String id, String back, string state, string code_verifier, string code_challenge)
        {
            // Generates state and PKCE values.
            //string state = randomDataBase64url(32);
            //string code_verifier = randomDataBase64url(32);
            //string code_challenge = base64urlencodeNoPadding(sha256(code_verifier));
            const string code_challenge_method = "S256";


            StringBuilder builder = new StringBuilder();
            builder.Append("https://accounts.google.com/o/oauth2/auth?");
            //builder.Append("https://accounts.google.com/o/oauth2/v2/auth");
            builder.AppendFormat("client_id={0}", Uri.EscapeDataString(id));
            //// 宣告取得 openid, profile, email 三個内容。
            //builder.AppendFormat("&scope={0}", Uri.EscapeDataString("openid profile email"));
            // 改用 drive
            builder.AppendFormat("&scope={0}", Uri.EscapeDataString(scopes));
            builder.AppendFormat("&state={0}", state);
            builder.AppendFormat("&code_challenge={0}", code_challenge);
            builder.AppendFormat("&code_challenge_method={0}", code_challenge_method);
            builder.AppendFormat("&redirect_uri={0}", Uri.EscapeDataString(back));
            builder.AppendFormat("&access_type={0}", "offline");
            builder.Append("&response_type=code");

            return builder.ToString();
        }

        private static async Task<GoogleAccessToken> GetAccessToken(string queryString, string expectedState, string codeVerifier)
        {
            // Parses URI params into a dictionary
            // ref: http://stackoverflow.com/a/11957114/72176
            Dictionary<string, string> queryStringParams =
                    //queryString.Substring(1).Split('&')
                    queryString.Split('&')
                         .ToDictionary(c => c.Split('=')[0],
                                       c => Uri.UnescapeDataString(c.Split('=')[1]));

            if (queryStringParams.ContainsKey("error"))
            {
                Debug.WriteLine($"OAuth error: {queryStringParams["error"]}.");
                return null;
            }

            if (!queryStringParams.ContainsKey("code") || !queryStringParams.ContainsKey("state"))
            {
                Debug.WriteLine($"Wrong response {queryString}");
                return null;
            }

            if (queryStringParams["state"] != expectedState)
            {
                Debug.WriteLine($"Invalid state {queryStringParams["state"]}");
                return null;
            }

            StringContent content = new StringContent($"code={queryStringParams["code"]}&client_secret={clientKey}&redirect_uri={Uri.EscapeDataString(callbackUrl)}&client_id={clientId}&code_verifier={codeVerifier}&grant_type=authorization_code",
                                                      Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await httpClient.PostAsync(TokenEndpoint, content);
            string responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine("Authorization code exchange failed.");
                return null;
            }

            googleAccessToken = new GoogleAccessToken(responseString);

            //JsonObject tokens = JsonObject.Parse(responseString);

            //foreach (var item in vault.RetrieveAll().Where((x) => x.Resource == TokenTypes.AccessToken.ToString() || x.Resource == TokenTypes.RefreshToken.ToString())) vault.Remove(item);

            ////vault.Add(new PasswordCredential(TokenTypes.AccessToken.ToString(), "MyApp", accessToken));
            ////vault.Add(new PasswordCredential(TokenTypes.RefreshToken.ToString(), "MyApp", tokens.GetNamedString("refresh_token")));

            ////vault.Add(new PasswordCredential(TokenTypes.AccessToken.ToString(), UserId, tokens.GetNamedString("access_token")));
            ////vault.Add(new PasswordCredential(TokenTypes.RefreshToken.ToString(), UserId, tokens.GetNamedString("refresh_token")));

            TokenLastAccess = DateTimeOffset.UtcNow;
            googleAccessToken.IssueDateTimeUTC = TokenLastAccess; // utc time
            googleAccessToken.IssueDateTime = DateTimeOffset.Now; // local time

            SavedGoogleAccessToken = googleAccessToken;
            IsAuthorized = true;

            return googleAccessToken;
        }
    }
}
