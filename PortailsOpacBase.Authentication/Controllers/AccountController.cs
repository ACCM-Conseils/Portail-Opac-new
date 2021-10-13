using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.WsFederation;
using Microsoft.Owin.Security;

namespace PortailsOpacBase.Authentication.Controllers
{
    public class AccountController : Controller
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void SignIn()
        {
            log.Info("SignIn");
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/" },
                    WsFederationAuthenticationDefaults.AuthenticationType);
            }
        }

        public void SignOut()
        {
            log.Info("SignOut");
            string callbackUrl = Url.Action("SignOutCallback", "Account", routeValues: null, protocol: Request.Url.Scheme);

            HttpContext.GetOwinContext().Authentication.SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                WsFederationAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
        }

        public ActionResult SignOutCallback()
        {
            log.Info("SignOutCallback");
            if (Request.IsAuthenticated)
            {
                // Rediriger vers la page d'accueil si l'utilisateur est authentifié.
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}