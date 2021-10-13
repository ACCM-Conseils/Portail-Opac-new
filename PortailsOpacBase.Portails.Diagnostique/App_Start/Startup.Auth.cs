using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Threading.Tasks;
using System.Security.Claims;
using PortailsOpacBase.Portails.Diagnostique.Models;
using System.Xml;
using System.ServiceModel.Security;
using System.IO;
using System.ServiceModel;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols.WSTrust.Bindings;
using Microsoft.Owin.Security.WsFederation;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Host.SystemWeb;
using System.IdentityModel.Selectors;
using PortailsOpacBase.Authentication;
using PortailsOpacBase.Portails.Diagnostique;
using System.Net;

namespace PortailsOpacBase
{
    public partial class Startup
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // Pour plus d’informations sur la configuration de l’authentification, rendez-vous sur http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            log.Info("Début ConfigureAuth portail");

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            var audienceRestriction = new AudienceRestriction(AudienceUriMode.Never);

            var issuerRegistry = new ConfigurationBasedIssuerNameRegistry();
            issuerRegistry.AddTrustedIssuer(System.Configuration.ConfigurationManager.AppSettings["cert_thumbprint"], "http://adfs.opacoise.fr/adfs/services/trust");

            app.UseWsFederationAuthentication(new WsFederationAuthenticationOptions(WsFederationAuthenticationDefaults.AuthenticationType)
            {
                MetadataAddress = "https://adfs.opacoise.fr/federationmetadata/2007-06/federationmetadata.xml",
                Wtrealm = "https://portail-ged.opacoise.fr/claimapp",
                //Wtrealm = "https://dev-portail-ged.opacoise.fr",
                SignInAsAuthenticationType = WsFederationAuthenticationDefaults.AuthenticationType,
                AuthenticationMode = AuthenticationMode.Active,                
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
                },
                Notifications = new WsFederationAuthenticationNotifications()
                {
                    SecurityTokenValidated = context =>
                    {
                        log.Info("Security token validated");
                        ClaimsIdentity identity = context.AuthenticationTicket.Identity;
                        Tuple<String, List<String>> result = DoSomethingWithLoggedInUser(identity);

                        var applicationUserIdentity = new ClaimsIdentity(context.OwinContext.Authentication.User.Identity);
                        context.OwinContext.Authentication.User.AddIdentity(applicationUserIdentity);

                        var emailClaim = context.AuthenticationTicket.Identity.Claims.ToList().FirstOrDefault(c => c.Type == ClaimTypes.Email);

                        if (emailClaim != null)
                        {
                            Guid conn = Guid.NewGuid();

                            String Profils = String.Empty;

                            foreach(String s in result.Item2)
                            {
                                Profils += s + ";";
                            }

                            using (var dbContext = new DiagnostiquesEntities())
                            {
                                dbContext.connexions.Add(new connexions() { id = Guid.NewGuid(), idconnexion = conn, nom = result.Item1, profil = Profils });

                                dbContext.SaveChanges();
                            }

                            context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimTypes.Email, emailClaim.Value));

                            int Niveau = 1;

                            if (result.Item2.Contains("BDES") && (result.Item2.Contains("DPE") || result.Item2.Contains("OR") || result.Item2.Contains("REF") || result.Item2.Contains("ENT")))
                                Niveau = 2;
                            else if (result.Item2.Contains("BDES") && (!result.Item2.Contains("DPE") && !result.Item2.Contains("OR") && !result.Item2.Contains("REF") && !result.Item2.Contains("ENT")))
                                Niveau = 3;

                            if (Niveau == 1)
                                context.AuthenticationTicket.Properties.RedirectUri = "/claimapp/Home/Index/" + conn;
                            else if(Niveau == 2)
                                context.AuthenticationTicket.Properties.RedirectUri = "/claimapp/Choix/Index/" + conn;
                            else if (Niveau == 3)
                                context.AuthenticationTicket.Properties.RedirectUri = "/claimapp/BDES/Index/" + conn;
                            context.State = Microsoft.Owin.Security.Notifications.NotificationResultState.Continue;
                        }

                        return Task.FromResult(0);
                    }
                }
            });
        }

        public Tuple<String, List<String>> DoSomethingWithLoggedInUser(ClaimsIdentity identity)
        {
            if (identity != null)
            {
                log.Info("claimsIdentity trouvé");

                log.Info(identity.Name);

                log.Info("Claims trouvés : " + identity.Claims.Count());

                String Email = String.Empty;
                List<String> Profil = new List<String>();

                foreach (System.Security.Claims.Claim claim in identity.Claims)
                {
                    log.Info("Claim : " + claim.Type + " / " + claim.Value);

                    if (claim.Type.EndsWith("emailaddress"))
                        Email = claim.Value;
                    else if (claim.Type.EndsWith("role"))
                        Profil.Add(claim.Value);
                }

                return new Tuple<String, List<String>>(Email, Profil);
            }
            else
            {
                return null;
            }
        }
    }

}