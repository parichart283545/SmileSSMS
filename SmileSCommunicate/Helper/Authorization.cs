using JWT.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SmileSCommunicate.Helper
{
    public class Authorization : ActionFilterAttribute, IActionFilter
    {
        public string Roles;

        void IActionFilter.OnActionExecuting(ActionExecutingContext filterContext)
        {
            //Get token name
            var tokenName = Properties.Settings.Default.TokenName;

            //keep token if token send from login page
            var tokenQueryString = filterContext.HttpContext.Request.QueryString["token"];
            if (!string.IsNullOrEmpty(tokenQueryString))
            {
                //keep cookie
                filterContext.HttpContext.Response.Cookies.Add(new System.Web.HttpCookie(tokenName)
                {
                    Value = tokenQueryString,
                    HttpOnly = true
                });
            }

            //Read Token cookie
            var cookie = filterContext.HttpContext.Request.Cookies[tokenName];

            //check token cookie
            if (cookie != null)
            {
                //Validate token cookie
                var validate = new SSOService.SSOServiceClient().ValidateKey(cookie.Value);

                if (validate)
                {
                    //key validate
                    //get tokendetail and check role
                    var lg = GetLoginDetail(filterContext.HttpContext);

                    //if no role required ,execute
                    if (Roles == null)
                    {
                        OnActionExecuting(filterContext);
                    }
                    else//required roles
                    {
                        var userRole = new SSOService.SSOServiceClient().GetRoleByUserName(lg.UserName);
                        //authenticate roles
                        if (IsAuthen(Roles, userRole))
                        {
                            OnActionExecuting(filterContext);
                        }
                        //unauthorized
                        else
                        {
                            filterContext.Result = new RedirectToRouteResult(
                              new RouteValueDictionary
                              {
                            {"Controller", "Auth"},
                            {"Action", "Unauthorize"}
                              });
                        }
                    }
                }
                else //Not Validate Token
                {
                    //Delete Cookie
                    filterContext.HttpContext.Response.Cookies.Add(new System.Web.HttpCookie(tokenName)
                    {
                        Value = null,
                        Expires = DateTime.Now.AddDays(-1),
                        HttpOnly = true
                    });

                    //get loginURL
                    var loginURL = Properties.Settings.Default.LoginPageURL;
                    //get webApplicationURL
                    var appURL = filterContext.HttpContext.Request.Url.OriginalString;
                    //Remove QueryString "token"
                    appURL = RemoveQueryStringByKey(appURL, "token");
                    //encode WebApplicationURL
                    appURL = filterContext.HttpContext.Server.UrlEncode(appURL);
                    //no token or token not validate : redirect to login
                    filterContext.Result = new RedirectResult(loginURL + "?url=" + appURL);
                }
            }
            else //No token
            {
                //get loginURL
                var loginURL = Properties.Settings.Default.LoginPageURL;
                //get webApplicationURL
                var appURL = filterContext.HttpContext.Request.Url.OriginalString;
                //Remove QueryString "token"
                appURL = RemoveQueryStringByKey(appURL, "token");
                //encode WebApplicationURL
                appURL = filterContext.HttpContext.Server.UrlEncode(appURL);
                //no token or token not validate : redirect to login
                filterContext.Result = new RedirectResult(loginURL + "?url=" + appURL);
            }
        }

        /// <summary>
        /// Check role in session is authen
        /// </summary>
        /// <param name="roleToCheck"></param>
        /// <param name="sessionRole"></param>
        /// <returns></returns>
        private bool IsAuthen(string roleToCheck, string sessionRole)
        {
            var result = false;

            var lstRoleToCheck = roleToCheck.Split(',').ToList();

            var lstSessionRole = sessionRole.Split(',').ToList();

            //intersec
            var intersectCount = lstRoleToCheck.Intersect(lstSessionRole).Count();

            result = (intersectCount > 0) ? true : false;

            return result;
        }

        /// <summary>
        /// Remove QueryString By Key
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private string RemoveQueryStringByKey(string url, string key)
        {
            var uri = new Uri(url);

            // this gets all the query string key value pairs as a collection
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);

            // this removes the key if exists
            newQueryString.Remove(key);

            // this gets the page path from root without QueryString
            string pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

            return newQueryString.Count > 0
                ? String.Format("{0}?{1}", pagePathWithoutQueryString, newQueryString)
                : pagePathWithoutQueryString;
        }

        public static LoginDetail GetLoginDetail(HttpContextBase context)
        {
            var secretKey = Properties.Settings.Default.SecretKey;
            var tokenName = Properties.Settings.Default.TokenName;

            //get key
            var c = context.Request.Cookies[tokenName];

            var result = new LoginDetail();

            //Decode token
            try
            {
                var payload = new JwtBuilder()
                        .WithSecret(secretKey)
                        .MustVerifySignature()
                        .Decode<IDictionary<string, object>>(c.Value);

                //Valid Token,Get details
                result.Result = LoginResult.SUCCESS;
                result.ErrorText = "";
                result.UserName = (payload["Username"]).ToString();
                result.User_ID = Convert.ToInt32(payload["User_ID"]);
                result.Person_ID = Convert.ToInt32(payload["Person_ID"]);
                result.Employee_ID = Convert.ToInt32(payload["Employee_ID"]);
                result.FirstName = (payload["FirstName"]).ToString();
                result.LastName = (payload["LastName"]).ToString();
                result.EmployeePositionDetail = (payload["EmployeePositionDetail"]).ToString();
                result.EmployeeTeamDetail = (payload["EmployeeTeamDetail"]).ToString();
                result.BranchDetail = (payload["BranchDetail"]).ToString();
                result.DepartmentDetail = (payload["DepartmentDetail"]).ToString();
                result.EmployeeTeam_ID = Convert.ToInt32(payload["EmployeeTeam_ID"]);
                result.Department_ID = Convert.ToInt32(payload["Department_ID"]);
                result.Branch_ID = Convert.ToInt32(payload["Branch_ID"]);
                result.EmployeePosition_ID = Convert.ToInt32(payload["EmployeePosition_ID"]);
                result.OrganizeCode = ((payload["OrganizeCode"]) != null ? (payload["OrganizeCode"]).ToString() : "");
                result.OrganizeDetail = ((payload["OrganizeDetail"]) != null ? (payload["OrganizeDetail"]) : "").ToString();
                if (payload["Organize_ID"] != null)
                {
                    result.Organize_ID = Convert.ToInt32(payload["Organize_ID"]);
                }
                result.EmployeeCode = (payload["EmployeeCode"]).ToString();
                result.FullName = (payload["FullName"]).ToString();
            }

            //Token Expire
            catch (JWT.TokenExpiredException)
            {
                //token has expire
                result.Result = LoginResult.ERROR;
                result.ErrorText = "cookie Expired";
            }

            //Invalid Signature
            catch (JWT.SignatureVerificationException)
            {
                //token has invalid signature
                result.Result = LoginResult.ERROR;
                result.ErrorText = "Invalid Signature";
            }

            //other
            catch (Exception ex)
            {
                result.Result = LoginResult.ERROR;
                result.ErrorText = ex.Message;
            }

            return result;
        }

        public static string GetChangePasswordURL()
        {
            return Properties.Settings.Default.ChangePasswordURL;
        }
    }

    public enum LoginResult
    {
        SUCCESS,
        ERROR
    }

    public class LoginDetail
    {
        public LoginResult Result { get; set; }
        public string ErrorText { get; set; }
        public string UserName { get; set; }
        public int User_ID { get; set; }
        public int Person_ID { get; set; }
        public int Employee_ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmployeePositionDetail { get; set; }
        public string EmployeeTeamDetail { get; set; }
        public string BranchDetail { get; set; }
        public string DepartmentDetail { get; set; }
        public int EmployeeTeam_ID { get; set; }
        public int Department_ID { get; set; }
        public int Branch_ID { get; set; }
        public int EmployeePosition_ID { get; set; }
        public string FullName { get; set; }
        public int? Organize_ID { get; set; }
        public string OrganizeCode { get; set; }
        public string OrganizeDetail { get; set; }
        public string EmployeeCode { get; set; }
    }
}