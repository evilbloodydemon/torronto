using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Extensions;
using Nancy.ModelBinding;
using Nancy.Security;
using Nancy.SimpleAuthentication;
using NLog;
using ObjectDumper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Torronto.BLL;
using Torronto.BLL.Ext;
using Torronto.DAL;
using Torronto.DAL.Models;

namespace Torronto.Modules
{
    public class LoginModule : NancyModule
    {
        public LoginModule(
          UserService userService
      )
        {
            Get["/login"] = parameters =>
            {
                return View["login"];
            };

            Post["/login"] = parameters =>
            {
                var login = this.Bind<LoginPost>();

                var user = userService.GetByCredentials(login.Email, login.Password);

                if (user == null)
                {
                    return Context.GetRedirect("/login");
                }

                return this.LoginAndRedirect(user.Identifier, DateTime.Now.AddDays(7), login.RedirectUrl);
            };

            Get["/logout"] = parameters =>
            {
                return this.LogoutAndRedirect("/");
            };
        }
    }

    public class UserMapper : IUserMapper
    {
        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            using (var db = new DbTorronto())
            {
                var user = db.User.FirstOrDefault(x => x.Identifier == identifier);

                if (user != null)
                {
                    return new TorrontoUserIdentity
                    {
                        UserName = user.Identifier.ToString(),
                        Claims = new[] { "user" },
                        User = user
                    };
                }
            }

            return null;
        }
    }

    public class TorrontoUserIdentity : IUserIdentity
    {
        public string UserName { get; set; }
        public IEnumerable<string> Claims { get; set; }
        public User User { get; set; }
    }

    public class LoginPost
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string RedirectUrl { get; set; }
    }

    public class SocialAuthProvider : IAuthenticationCallbackProvider
    {
        private readonly UserService _userService;

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public SocialAuthProvider(UserService userService)
        {
            _userService = userService;
        }

        public dynamic Process(NancyModule nancyModule, AuthenticateCallbackData model)
        {
            var currentUserId = nancyModule.GetUserID();

            if (model.Exception != null)
            {
                _logger.Error("SOCIALAUTH", model.Exception);
                return nancyModule.Response.AsRedirect("/");
            }

            var userInfo = model.AuthenticatedClient.UserInformation;
            var providerName = model.AuthenticatedClient.ProviderName;
            var userName = userInfo.UserName ?? userInfo.Name ?? "Unknown";
            var email = userInfo.Email ?? string.Empty;

            var user = _userService.GetByIdentity(providerName, userInfo.Id);

            if (user == null)
            {
                if (currentUserId == null)
                {
                    user = _userService.AddUser(userName, email, providerName, userInfo.Id);
                }
                else
                {
                    user = _userService.AttachIdentity(currentUserId, userName, email, providerName, userInfo.Id);
                }
            }
            else
            {
                if (currentUserId != null && user.ID != currentUserId)
                {
                    nancyModule.Session["MergeID"] = user.ID;

                    return nancyModule.Response.AsRedirect("/#!/profile?tab=logins");
                }
            }

            return nancyModule.LoginAndRedirect(user.Identifier, DateTime.Now.AddMonths(3), model.ReturnUrl);
        }

        public dynamic OnRedirectToAuthenticationProviderError(NancyModule nancyModule, string errorMessage)
        {
            _logger.Error("SOCIALAUTH OnRedirectToAuthenticationProviderError", errorMessage);

            return nancyModule.Response.AsRedirect("/");
        }
    }
}