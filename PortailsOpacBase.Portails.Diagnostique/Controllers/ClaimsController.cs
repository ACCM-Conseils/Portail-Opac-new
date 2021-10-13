using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace PortailsOpacBase.Portails.Diagnostique.Controllers
{
    public class ClaimsController : Controller
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Claims
        [Authorize]
        public ActionResult Index()
        {
            try
            {
                log.Info("Recherche claims");

                var authenticationManager = Request.GetOwinContext().Authentication.User;

                if (authenticationManager != null)
                {
                    log.Info("Claims trouvé : "+ authenticationManager.Claims.Count());

                    if (authenticationManager.Claims.Count() > 0)
                    {
                        

                        Session["auth_agent"] = true;

                        foreach (System.Security.Claims.Claim claim in authenticationManager.Claims)
                        {
                            if (claim.Type.EndsWith("emailaddress"))
                                Session["email_agent"] = claim.Value;
                            else if (claim.Type.EndsWith("role"))
                                Session["role_agent"] = claim.Value;
                        }

                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    log.Info("Pas de claims...");
                }

                ClaimsPrincipal principal = Thread.CurrentPrincipal as ClaimsPrincipal;

                if (principal != null)
                {
                    log.Info("Claims trouvé : " + principal.Claims.Count());

                    if (principal.Claims.Count() > 0)
                    {
                        Session["auth_agent"] = true;

                        foreach (System.Security.Claims.Claim claim in principal.Claims)
                        {
                            if (claim.Type.EndsWith("emailaddress"))
                                Session["email_agent"] = claim.Value;
                            else if (claim.Type.EndsWith("role"))
                                Session["role_agent"] = claim.Value;
                        }

                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    log.Info("Pas de claims...");
                }
            }
            catch(Exception e)
            {
                log.Error(e);
            }

            return View();
        }
    }
}