using System.Web;
using System.Web.Optimization;

namespace DossierExplorer
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {

            bundles.Add(new ScriptBundle("~/FileExplorer/Scripts/js").Include(
                                      "~/FileExplorer/Scripts/jquery-{version}.js",
                                      //"~/Scripts/jquery-migrate-{version}.js",
                                      "~/FileExplorer/Scripts/jquery-ui-{version}.js",
                                      "~/FileExplorer/Scripts/jquery.validate*"));

            #region elFinder bundles

            bundles.Add(new ScriptBundle("~/FileExplorer/Scripts/elfinder").Include(
                             "~/FileExplorer/Content/elfinder/js/elfinder.full.js"
                             //"~/Content/elfinder/js/i18n/elfinder.pt_BR.js"
                             ));

            bundles.Add(new StyleBundle("~/FileExplorer/Content/elfinder").Include(
                            "~/FileExplorer/Content/elfinder/css/elfinder.full.css",
                            "~/FileExplorer/Content/elfinder/css/theme.css"));

            #endregion

            bundles.Add(new StyleBundle("~/Content/css").Include(
                                        "~/Content/site.css"));

            bundles.Add(new StyleBundle("~/FileExplorer/Content/themes/ui-lightness/css").Include(
                                        "~/FileExplorer/Content/themes/ui-lightness/jquery.ui.all.css"));


            #region jquery
            bundles.Add(new ScriptBundle("~/Scripts/js").Include(
                                      "~/Scripts/jquery-{version}.js",
                                      //"~/Scripts/jquery-migrate-{version}.js",
                                      "~/Scripts/jquery-ui-{version}.js",
                                      "~/Scripts/jquery.validate*"));

            #endregion


            #region bootstrap
            bundles.Add(new StyleBundle("~/Content/bootstrap").Include(
                                        "~/Content/bootstrap*"));
            bundles.Add(new ScriptBundle("~/Scripts/bootstrap").Include(
                             "~/Scripts/bootstrap*"
                             //"~/Content/elfinder/js/i18n/elfinder.pt_BR.js"
                             ));
            #endregion
        }
    }
}
