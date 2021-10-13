using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Owin;

namespace PortailsOpacBase.AuthTest
{
    public partial class Startup
    {
        private static string realm = ConfigurationManager.AppSettings["ida:Wtrealm"];
        private static string adfsMetadata = ConfigurationManager.AppSettings["ida:ADFSMetadata"];

        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseCookieAuthentication(
            new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType
            });
            app.UseWsFederationAuthentication(
            new WsFederationAuthenticationOptions
            {
                MetadataAddress = adfsMetadata,
                Wtrealm = realm,
            });

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
        }
    }
}