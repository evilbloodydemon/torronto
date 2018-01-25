using Nancy;
using Torronto.DAL.Models;
using Torronto.Modules;

namespace Torronto.BLL.Ext
{
    public static class NancyExtensions
    {
        public static User GetUser(this NancyModule module)
        {
            var identity = module.Context.CurrentUser as TorrontoUserIdentity;

            if (identity != null)
            {
                return identity.User;
            }

            return null;
        }

        public static int? GetUserID(this NancyModule module)
        {
            var user = module.GetUser();

            return user == null ? null : user.ID;
        }
    }
}