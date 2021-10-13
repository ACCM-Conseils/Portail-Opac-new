using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Owin;

namespace PortailsOpacBase.Portails
{
    public partial class Startup
    {
        private static string realm = ConfigurationManager.AppSettings["ida:Wtrealm"];
        private static string adfsMetadata = ConfigurationManager.AppSettings["ida:ADFSMetadata"];

        public void ConfigureAuth(IAppBuilder app)
        {
            try
            {
                app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

                app.UseCookieAuthentication(new CookieAuthenticationOptions());

                var audienceRestriction = new AudienceRestriction(AudienceUriMode.Always);
                audienceRestriction.AllowedAudienceUris.Add(new Uri("https://portail-diagnostics.opacoise.fr"));
                audienceRestriction.AllowedAudienceUris.Add(new Uri("https://portail-diagnostics.opacoise.fr/claimapp"));

                var issuerRegistry = new ConfigurationBasedIssuerNameRegistry();
                issuerRegistry.AddTrustedIssuer("80AC05C6C1DAD7E78ABE6728F0BD265910A98236", "http://adfs.opacoise.fr/adfs/services/trust");
                issuerRegistry.AddTrustedIssuer("213672D669D5A72A5898826BA8AA73B8601A82A5", "http://adfs.opacoise.fr/adfs/services/trust");

                app.UseWsFederationAuthentication(new WsFederationAuthenticationOptions(WsFederationAuthenticationDefaults.AuthenticationType)
                {
                    MetadataAddress = "https://adfs.opacoise.fr/federationmetadata/2007-06/federationmetadata.xml",
                    Wtrealm = "https://portail-diagnostics.opacoise.fr/claimapp",
                    Wreply = "https://portail-diagnostics.opacoise.fr",
                    SignInAsAuthenticationType = WsFederationAuthenticationDefaults.AuthenticationType,
                    SecurityTokenHandlers = new SecurityTokenHandlerCollection
                {
                    new EncryptedSecurityTokenHandlerEx(new X509CertificateStoreTokenResolver(StoreName.My,StoreLocation.LocalMachine)),
                    new SamlSecurityTokenHandlerEx
                    {
                        CertificateValidator = X509CertificateValidator.None,
                        Configuration = new SecurityTokenHandlerConfiguration()
                        {
                            AudienceRestriction = audienceRestriction,
                            IssuerNameRegistry = issuerRegistry
                        }
                    }
                }
                });
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }
    }
}