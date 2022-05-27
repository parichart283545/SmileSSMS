using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace SmileSCommunicateRESTfulService.Helper
{
    public class APIAuthorization : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            try
            {
                //Get Key from actionContext
                var actionContextKey = actionContext.Request.Headers.GetValues("Authorization").First();

                //Get User from key
                var projectid = GlobalFunction.GetAPIUserFromJWTKey(actionContextKey);

                if (string.IsNullOrEmpty(projectid))
                {
                    actionContext.Response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    return;
                }

                //Get valid projectid from Properties
                var IsUAT = Properties.Settings.Default.IsUAT;

                var strProjectId = (IsUAT ? Properties.Settings.Default.Allow_Project_ID_UAT : Properties.Settings.Default.Allow_Project_ID_PROD);

                var arrProjectId = strProjectId.Split(',');

                //valid user here
                var IsAuthorize = arrProjectId.Contains(projectid);

                //check authorize
                if (!IsAuthorize)
                {
                    actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                    return;
                }

                base.OnAuthorization(actionContext);
            }
            catch (Exception)
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                return;
            }
        }
    }
}