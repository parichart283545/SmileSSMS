using System.Web.Mvc;
using System.Web.Routing;

namespace SmileSCommunicate
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapMvcAttributeRoutes(); //Enables Attribute Routing

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "SMS", action = "SMSReport", id = UrlParameter.Optional }
            );
        }
    }
}