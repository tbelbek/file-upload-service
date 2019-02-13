using System.Web.Mvc;
using System.Web.Routing;

namespace FileUploader
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "FileLinkSimpler",                                           // Route name
                "{fileId}",                            // URL with parameters
                new { controller = "Home", action = "GetFile" }  // Parameter defaults
            );

            routes.MapRoute(
                "FileLink",                                           // Route name
                "FileLink/{fileId}",                            // URL with parameters
                new { controller = "Home", action = "GetFile" }  // Parameter defaults
            );



            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
