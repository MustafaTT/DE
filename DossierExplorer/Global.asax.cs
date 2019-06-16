using DossierExplorer.LuceneIndex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

public static class GlobalVariables
{

    public static string MyAppPath { get; internal set; }
}
namespace DossierExplorer
{
    public class MvcApplication : System.Web.HttpApplication
    {
  

        protected void Application_Start()
        {
            GlobalVariables.MyAppPath = Server.MapPath("~");
            
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            IndexCRUD.CreateFolders();
            IndexCRUD.CreateEmptyIndexFiles();
        }
    }
}
