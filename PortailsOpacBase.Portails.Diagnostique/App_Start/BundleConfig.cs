using System.Collections.Generic;
using System.Web;
using System.Web.Optimization;

namespace PortailsOpacBase.Portails.Diagnostique
{
    public class BundleConfig
    {
        // Pour plus d'informations sur le regroupement, visitez https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Utilisez la version de développement de Modernizr pour le développement et l'apprentissage. Puis, une fois
            // prêt pour la production, utilisez l'outil de génération à l'adresse https://modernizr.com pour sélectionner uniquement les tests dont vous avez besoin.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/bootstrap-datepicker.js",
                      "~/Scripts/locales/bootstrap-datepicker.fr.js",
                      "~/Scripts/chosen.jquery.js"));

            string pluploadBase = "~/scripts/Plupload/js/";
            var pluploadBundle = new ScriptBundle("~/bundles/plupload").Include(
                pluploadBase + "plupload.full.js");
            pluploadBundle.Orderer = new PassthruBundleOrderer();
            bundles.Add(pluploadBundle);

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css",
                      "~/Content/datepicker.css",
                      "~/Content/datepicker3.css",
                      "~/Content/chosen.css"));
        }

        class PassthruBundleOrderer : IBundleOrderer
        {
            public IEnumerable<BundleFile> OrderFiles(BundleContext context, IEnumerable<BundleFile> files)
            {
                return files;
            }
        }
    }
}
