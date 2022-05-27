using System.Web.Mvc;

namespace SmileSCommunicate.Controllers
{
    public class AuthController:Controller
    {
        public ActionResult Logout()
        {
            var logoutURL = Properties.Settings.Default.LogoutURL;

            //get webApplicationURL
            var appURL = HttpContext.Request.Url.OriginalString.ToLower().Replace("/auth/logout","");
            //encode WebApplicationURL
            appURL = HttpContext.Server.UrlEncode(appURL);

            return Redirect(logoutURL + "?url=" + appURL);
        }

        public ActionResult UnAuthorize()
        {
            return View();
        }

        public ActionResult ChangePassword()
        {
            var changePasswordURL = Properties.Settings.Default.ChangePasswordURL;
            return Redirect(changePasswordURL);
        }
    }
}