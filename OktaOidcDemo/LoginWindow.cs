using Flurl;
using Flurl.Http;
using Microsoft.Web.WebView2.Core;
using OktaOidcDemo.Dto;
using System.Net;
using System.Security.Cryptography;
using System.Text;

/**
 * Author: Hazael Mojica.
 * Use the OIDC Authentication Code Flow inside a Desktop App.
 * */
namespace OktaOidcDemo {
    public partial class LoginWindow : Form {

        // ****************************************************************************************************
        // The Okta Token will be placed here after a successful login
        // ****************************************************************************************************
        public OktaToken? OktaToken { get; set; }

        // ****************************************************************************************************
        // Here goes all your infrastructure settings
        // ****************************************************************************************************

        // Authorize URL and Token URL are from your own Authorization Server in OKTA
        // using /v1/authorize and /v1/token respectively
        // The Redirect URL belongs to your OIDC app, this is the URL that the browser is suppose to redirect to
        // after a succesful login
        // The Clien ID also identifies your OIDC app
        // Add all the scopes you need, even some custom scopes, it all depends on how you configured your Auth Server
        private string oktaAuthorizeUrl = "https://<YourOktaDomain>/oauth2/<AuthServerName>/v1/authorize";
        private string oktaTokenUrl = "https://<YourOktaDomain>/oauth2/<AuthServerName>/v1/token";
        private string oktaRedirectUrl = "http://localhost:8080/login/callback";
        private string oktaCliendId = "<Your Okta App Client Id>";
        private string oktaScopes = "openid profile";


        // ****************************************************************************************************
        // Some useful instance variables
        // ****************************************************************************************************
        private string state;
        private string codeVerifier;


        public LoginWindow() {
            InitializeComponent();
            ConfigProxy();

            // Just a randomly generated string that we can send to Okta on the login
            // and expect the same string to get back along with the auth_code
            state = Guid.NewGuid().ToString();

            // Random URL-safe string with a minimum length of 43 characters
            codeVerifier = (Guid.NewGuid().ToString() + Guid.NewGuid().ToString())
                .TrimEnd('=')
                .Replace("-", "");

            // Base64 URL-encoded SHA-256 hash of the code verifier
            string codeChallenge;
            using (var sha256 = SHA256.Create()) {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));

                // SHA256 uses char '=' as padding, so we need to remove
                // https://stackoverflow.com/a/30666838/3455589
                codeChallenge = Convert.ToBase64String(challengeBytes)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');
            }

            var oktaLoginUrl = oktaAuthorizeUrl
                                .SetQueryParam("response_type", "code")
                                .SetQueryParam("scope", oktaScopes)
                                .SetQueryParam("client_id", oktaCliendId)
                                .SetQueryParam("state", state)
                                .SetQueryParam("redirect_uri", oktaRedirectUrl)
                                .SetQueryParam("code_challenge_method", "S256")
                                .SetQueryParam("code_challenge", codeChallenge);

            // You could put a break point here and get the full URL
            // Copy and paste it into a web browser, just for fun.
            var oktaSignInUriUri = new Uri(oktaLoginUrl.ToString());
            OktaToken = null;

            // Make the WebView navigate to that Login URL
            webView2.Source = oktaSignInUriUri;
        }

        /*
         * The call to this method is optional if you need any proxy settings.
         * Failing to correctly set the proxy settings might result in the webview not correctly
         * displaying the Okta Login page and silently failing.
         * */
        private void ConfigProxy() {
            // Disable Proxy on WebView2
            // Or You can configure them using argument "--proxy-server=http://1.2.3.4:8888"
            CoreWebView2EnvironmentOptions Options = new CoreWebView2EnvironmentOptions();
            Options.AdditionalBrowserArguments = "--no-proxy-server";
            CoreWebView2Environment env = CoreWebView2Environment.CreateAsync(null, null, Options).Result;
            webView2.EnsureCoreWebView2Async(env);

            // Disable Proxy on Flurl Http Client
            FlurlHttp.Clients.WithDefaults(builder => builder
            .ConfigureInnerHandler(hch => {
                hch.UseProxy = false;
            }));
        }

        /*
         * The NavigationStarting event is used here to capture the Redirect action happening when
         * the user completes the Login succesfully and the WebView tries to redirect to the configured Redirect URL.
         * The Redirect URL will contain the Auth Code as a query param.
         * We need that Auth Code to generate an Access Token later on.
         */
        private void webView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e) {
            if (e.Uri.Contains(oktaRedirectUrl)) {
                // Replace the nasty # and put a nice ? so that is URL compliant
                var urlFormated = e.Uri.Replace("#", "?");
                var url = new Flurl.Url(urlFormated);
                var authCode = GetQueryParamOrEmptyString(url, "code");
                var stateReceived = GetQueryParamOrEmptyString(url, "state");
                var error = GetQueryParamOrEmptyString(url, "error");
                var errorDescription = GetQueryParamOrEmptyString(url, "error_description");

                var errorMessage = string.Empty;
                if (!string.IsNullOrEmpty(error) || !string.IsNullOrEmpty(errorDescription)) {
                    errorMessage = $"{error} {errorDescription}";
                } else {
                    if (string.Equals(stateReceived, state)) {
                        if (!string.IsNullOrEmpty(authCode)) {
                            try {
                                OktaToken = FetchAccessToken(authCode, codeVerifier);
                                MessageBox.Show($"Okta Token acquired: {OktaToken.AccessToken}", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                Close();
                            } catch (Exception ex) {
                                errorMessage = $"Error while trying to fetch an access token: {ex.Message}";

                                if (ex.InnerException is FlurlHttpException) {
                                    var flurEx = (FlurlHttpException)ex.InnerException;
                                    var oktaErrorResponse = flurEx.GetResponseJsonAsync<OktaErrorResponse>().Result;
                                    errorMessage += $"\n{oktaErrorResponse.Error}. {oktaErrorResponse.ErrorDescription}";
                                }
                            }
                        } else {
                            errorMessage = "Auth code received by Okta Authorization Server is not valid";
                        }
                    } else {
                        errorMessage = "State string received does not match the one generated";
                    }
                }
                // If it reached this point is because there's an error of some sort.
                MessageBox.Show(errorMessage, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private OktaToken FetchAccessToken(string authCode, string codeVerifier) {
            var tokenResponse = oktaTokenUrl
                .WithHeaders(new {
                    Accept = "application/json",
                    Cache_Control = "no-cache",
                    Content_Type = "application/x-www-form-urlencoded"
                })
                .PostUrlEncodedAsync(new {
                    grant_type = "authorization_code",
                    client_id = oktaCliendId,
                    redirect_uri = oktaRedirectUrl,
                    code = authCode,
                    code_verifier = codeVerifier
                }).ReceiveJson<OktaToken>().Result;
            return tokenResponse;
        }

        private static string GetQueryParamOrEmptyString(Flurl.Url url, string queryParamName) {
            return url.QueryParams.FirstOrDefault(queryParamName) != null ? url.QueryParams.FirstOrDefault(queryParamName).ToString() : "";
        }
    }
}
