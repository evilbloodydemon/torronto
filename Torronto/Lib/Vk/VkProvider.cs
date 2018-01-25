using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RestSharp;
using SimpleAuthentication.Core;
using SimpleAuthentication.Core.Exceptions;
using SimpleAuthentication.Core.Providers;
using SimpleAuthentication.Core.Tracing;

namespace Torronto.Lib.Vk
{
    public class AccessTokenResult
    {
        public string AccessToken { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
    }

    public class UserInfoResult
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Email { get; set; }
        public string Nickname { get; set; }
    }

    public class VkUserInfoResponse
    {
        public List<UserInfoResult> Response { get; set; }
    }

    public class VkAccessToken : AccessToken
    {
        public string UserId { get; set; }
        public string Email { get; set; }
    }

    public class VkProvider : BaseOAuth20Provider<AccessTokenResult>
    {
        private const string AccessTokenKey = "access_token";

        public VkProvider(ProviderParams providerParams)
            : this("Vk", providerParams)
        {
        }

        protected VkProvider(string name, ProviderParams providerParams)
            : base(name, providerParams)
        {
            AuthenticateRedirectionUrl = new Uri("https://oauth.vk.com/authorize");
        }

        #region BaseOAuth20Token<AccessTokenResult> Implementation

        public override IEnumerable<string> DefaultScopes
        {
            get { return new[] { "email" }; }
        }

        public override string ScopeSeparator
        {
            get { return ","; }
        }

        protected override IRestResponse<AccessTokenResult> ExecuteRetrieveAccessToken(string authorizationCode,
                                                                                       Uri redirectUri)
        {
            if (string.IsNullOrEmpty(authorizationCode))
            {
                throw new ArgumentNullException("authorizationCode");
            }

            if (redirectUri == null ||
                string.IsNullOrEmpty(redirectUri.AbsoluteUri))
            {
                throw new ArgumentNullException("redirectUri");
            }

            var restRequest = new RestRequest("/access_token", Method.POST);
            restRequest.AddParameter("client_id", PublicApiKey);
            restRequest.AddParameter("client_secret", SecretApiKey);
            restRequest.AddParameter("redirect_uri", redirectUri.AbsoluteUri);
            restRequest.AddParameter("code", authorizationCode);
            restRequest.AddParameter("grant_type", "authorization_code");
            restRequest.AddParameter("v", "5.25");

            var restClient = RestClientFactory.CreateRestClient("https://oauth.vk.com");
            TraceSource.TraceVerbose("Retrieving Access Token endpoint: {0}",
                                     restClient.BuildUri(restRequest).AbsoluteUri);

            return restClient.Execute<AccessTokenResult>(restRequest);
        }

        protected override AccessToken MapAccessTokenResultToAccessToken(AccessTokenResult accessTokenResult)
        {
            if (accessTokenResult == null)
            {
                throw new ArgumentNullException("accessTokenResult");
            }

            if (string.IsNullOrEmpty(accessTokenResult.AccessToken)
                || string.IsNullOrEmpty(accessTokenResult.UserId)
                || string.IsNullOrEmpty(accessTokenResult.Email))
            {
                var errorMessage =
                    string.Format(
                        "Retrieved a Vk Access Token but it doesn't contain one or more of either: {0}, {1}, {2}",
                        AccessTokenKey,
                        "user_id",
                        "email");

                TraceSource.TraceError(errorMessage);
                throw new AuthenticationException(errorMessage);
            }

            return new VkAccessToken
            {
                PublicToken = accessTokenResult.AccessToken,
                UserId = accessTokenResult.UserId,
                Email = accessTokenResult.Email
            };
        }

        protected override UserInformation RetrieveUserInformation(AccessToken vkAccessToken)
        {
            var accessToken = vkAccessToken as VkAccessToken;

            if (accessToken == null)
            {
                throw new ArgumentNullException("vkAccessToken");
            }

            if (string.IsNullOrEmpty(accessToken.PublicToken))
            {
                throw new ArgumentException("accessToken.PublicToken");
            }

            IRestResponse<VkUserInfoResponse> response;

            try
            {
                var restRequest = new RestRequest("/method/users.get", Method.GET);
                restRequest.AddParameter(AccessTokenKey, accessToken.PublicToken);
                restRequest.AddParameter("uids", "{uid}");
                restRequest.AddParameter("fields", "uid,first_name,last_name,nickname");
                restRequest.AddParameter("v", "5.25");

                var restClient = RestClientFactory.CreateRestClient("https://api.vk.com");

                restClient.UserAgent = PublicApiKey;

                TraceSource.TraceVerbose("Retrieving user information. Vk Endpoint: {0}",
                                         restClient.BuildUri(restRequest).AbsoluteUri);

                response = restClient.Execute<VkUserInfoResponse>(restRequest);
            }
            catch (Exception exception)
            {
                throw new AuthenticationException("Failed to obtain User Info from Vk.", exception);
            }

            if (response == null ||
                response.StatusCode != HttpStatusCode.OK)
            {
                throw new AuthenticationException(
                    string.Format(
                        "Failed to obtain User Info from Vk OR the the response was not an HTTP Status 200 OK. Response Status: {0}. Response Description: {1}",
                        response == null ? "-- null response --" : response.StatusCode.ToString(),
                        response == null ? string.Empty : response.StatusDescription));
            }

            // Lets check to make sure we have some bare minimum data.
            if (string.IsNullOrEmpty(accessToken.UserId) ||
                string.IsNullOrEmpty(accessToken.Email))
            {
                throw new AuthenticationException(
                    string.Format(
                        "Retrieve some user info from the Vk Api, but we're missing one or both: UserId: '{0}' and Email: '{1}'.",
                        string.IsNullOrEmpty(accessToken.UserId) ? "--missing--" : accessToken.UserId,
                        string.IsNullOrEmpty(accessToken.Email) ? "--missing--" : accessToken.Email));
            }


            var userInfo = response.Data.Response.FirstOrDefault();

            var finalUserInfo = new UserInformation
            {
                Id = accessToken.UserId,
                Email = accessToken.Email
            };

            if (userInfo != null)
            {
                finalUserInfo.UserName = userInfo.FirstName + " " + userInfo.LastName;
                finalUserInfo.Name = userInfo.Nickname;
            }

            return finalUserInfo;
        }

        #endregion
    }
}