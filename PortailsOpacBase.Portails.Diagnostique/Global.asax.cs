using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace PortailsOpacBase.Portails.Diagnostique
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            log4net.Config.XmlConfigurator.Configure();
        }

        protected void Session_Start(object sender, EventArgs e)
        {
        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            HttpCookie authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                //Extract the forms authentication cookie
                FormsAuthenticationTicket authTicket = FormsAuthentication.Decrypt(authCookie.Value);

                /*JwtSecurityToken jwTok = TokenHelper.GetJWTokenFromCookie(authCookie);

                // Create the IIdentity instance
                IIdentity id = new FormsIdentity(authTicket);

                // Create the IPrinciple instance
                IPrincipal principal = new GenericPrincipal(id, TokenHelper.GetRolesFromToken(jwTok).ToArray());

                // Set the context user
                Context.User = principal;*/
            }
        }

    }
}
