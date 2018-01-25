using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using System;
using Torronto.BLL;
using Torronto.BLL.Ext;
using Torronto.BLL.Models;

namespace Torronto.Modules
{
    public class UsersModule : NancyModule
    {
        private readonly UserService _userService;

        public UsersModule(
            UserService userService
        )
            : base("/api/users")
        {
            _userService = userService;
            Get["/"] = Index;
            Put["/"] = GlobalUpdate;
            Post["/{UserID}"] = Update;
        }

        private dynamic Index(dynamic parameters)
        {
            this.RequiresAuthentication();

            var userId = this.GetUserID();
            var profile = Convert.ToBoolean((string)Request.Query.profile);

            if (profile)
            {
                var userProfile = _userService.GetProfile(userId);
                userProfile.MergeID = Session["MergeID"] as int?;

                return userProfile;
            }

            return HttpStatusCode.NotFound;
        }

        private dynamic Update(dynamic parameters)
        {
            this.RequiresAuthentication();

            var userId = this.GetUserID();
            var rUserId = (int)parameters.UserID;

            if (userId == rUserId)
            {
                var profile = this.Bind<UserProfile>();
                _userService.UpdateProfile(userId, profile);

                return HttpStatusCode.OK;
            }

            return HttpStatusCode.NotFound;
        }

        private dynamic GlobalUpdate(dynamic parameters)
        {
            this.RequiresAuthentication();

            var userId = this.GetUserID();

            var merge = Convert.ToBoolean((string)Request.Query.merge);
            var stopmerge = Convert.ToBoolean((string)Request.Query.stopmerge);

            if (merge)
            {
                var mergeId = Session["MergeID"] as int?;

                if (mergeId != null && userId != mergeId)
                {
                    _userService.MergeAccounts(mergeId.GetValueOrDefault(), userId.GetValueOrDefault());
                    Session["MergeID"] = null;

                    return HttpStatusCode.OK;
                }
            }

            if (stopmerge)
            {
                Session["MergeID"] = null;

                return HttpStatusCode.OK;
            }

            return HttpStatusCode.InternalServerError;
        }
    }
}