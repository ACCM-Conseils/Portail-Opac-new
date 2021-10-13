using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace PortailsOpacBase.Authentication.Controllers
{
    public class GetLoginController : Controller
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: GetLogin
        public ActionResult Index()
        {
            var claimsIdentity = User.Identity as System.Security.Claims.ClaimsIdentity;

            if (claimsIdentity != null)
            {
                log.Info("claimsIdentity trouvé");

                log.Info(claimsIdentity.Name);

                log.Info("Claims trouvés : " + claimsIdentity.Claims.Count());

                foreach (System.Security.Claims.Claim claim in claimsIdentity.Claims)
                {
                    log.Info(claim.Type);
                    log.Info(claim.Value);
                }
            }
            else
            {
                log.Info("Pas de claims...");
            }

            return View();
        }
    }
}