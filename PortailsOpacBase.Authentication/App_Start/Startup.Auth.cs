using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Selectors;
using System.IdentityModel.Services;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Owin;

namespace PortailsOpacBase.Authentication
{
    public partial class Startup
    {
        private static string realm = ConfigurationManager.AppSettings["ida:Wtrealm"];
        private static string adfsMetadata = ConfigurationManager.AppSettings["ida:ADFSMetadata"];
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        

        public void ConfigureAuth(IAppBuilder app)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                log.Info("Début ConfigureAuth");

                app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

                app.UseCookieAuthentication(new CookieAuthenticationOptions());

                var audienceRestriction = new AudienceRestriction(AudienceUriMode.Never);

                var issuerRegistry = new ConfigurationBasedIssuerNameRegistry();
                issuerRegistry.AddTrustedIssuer(System.Configuration.ConfigurationManager.AppSettings["cert_thumbprint"], "http://adfs.opacoise.fr/adfs/services/trust");

                app.UseWsFederationAuthentication(new WsFederationAuthenticationOptions(WsFederationAuthenticationDefaults.AuthenticationType)
                {
                    MetadataAddress = "https://adfs.opacoise.fr/federationmetadata/2007-06/federationmetadata.xml",
                    //Wtrealm = "https://portail-ged.opacoise.fr",
                    Wtrealm = "https://dw-dev-portail.opacoise.fr",
                    SignInAsAuthenticationType = WsFederationAuthenticationDefaults.AuthenticationType,
                    SecurityTokenHandlers = new SecurityTokenHandlerCollection
                {
                    new EncryptedSecurityTokenHandlerEx(new X509CertificateStoreTokenResolver(StoreName.My,StoreLocation.LocalMachine)),
                    new SamlSecurityTokenHandlerEx
                    {
                        CertificateValidator = X509CertificateValidator.None                      
                    }
                },
                    Notifications = new WsFederationAuthenticationNotifications()
                    {
                        SecurityTokenValidated = context =>
                        {
                            log.Info("Security token validated");

                            ClaimsIdentity identity = context.AuthenticationTicket.Identity;
                            DoSomethingWithLoggedInUser(identity);

                            return Task.FromResult(0);
                        }
                    }
                });
            }
            catch(Exception e)
            {
                log.Error(e);
            }

            /*app.UseWsFederationAuthentication(
                new WsFederationAuthenticationOptions
                {
                    Wtrealm = realm,
                    MetadataAddress = adfsMetadata,
                    SignInAsAuthenticationType = WsFederationAuthenticationDefaults.AuthenticationType,
                    AuthenticationType = "adfs",
                    SecurityTokenHandlers = new SecurityTokenHandlerCollection
                    {
                        new EncryptedSecurityTokenHandler
                        {
                            Configuration = new SecurityTokenHandlerConfiguration
                            {
                                IssuerTokenResolver = new X509CertificateStoreTokenResolver(StoreName.My,
                                    StoreLocation.LocalMachine)
                            }
                        },
                        new Saml2SecurityTokenHandler
                        {
                            CertificateValidator = X509CertificateValidator.None,

                        }
                    }
                });*/
        }

        public void DoSomethingWithLoggedInUser(ClaimsIdentity identity)
        {
            if (identity != null)
            {
                log.Info("claimsIdentity trouvé");

                log.Info(identity.Name);

                log.Info("Claims trouvés : " + identity.Claims.Count());

                foreach (System.Security.Claims.Claim claim in identity.Claims)
                {
                    log.Info(claim.Type);
                    log.Info(claim.Value);
                }
            }
            else
            {
                log.Info("Pas de claims...");
            }
        }
    }
}