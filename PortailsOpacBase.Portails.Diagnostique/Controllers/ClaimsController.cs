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

                if (User.Identity.IsAuthenticated)
                {

                    log.Info("Claims trouvé : " + System.Security.Claims.ClaimsPrincipal.Current.Claims.Count());

                    Session["auth_agent"] = true;

                   /* foreach (var c in System.Security.Claims.ClaimsPrincipal.Current.Claims)
                    {

                        if (c.Type.EndsWith("emailaddress"))
                            Session["email_agent"] = c.Value;
                        else if (c.Type.EndsWith("affectation"))
                            Session["role_agent"] = c.Value;
                        else if (c.Type.EndsWith("displayname"))
                            Session["nom_agent"] = c.Value;
                    }*/

                    Guid? idConnect = Guid.NewGuid();

                    using (var dbContext = new DiagnostiquesEntities())
                    {
                        connexions c = new connexions();
                        c.profil = "Referent";
                        c.idconnexion = idConnect;
                        c.id = Guid.NewGuid();
                        c.dateheure = DateTime.Now;
                        c.nom = System.Security.Claims.ClaimsPrincipal.Current.Claims.FirstOrDefault(m => m.Type.EndsWith("displayname")).Value;

                        return RedirectToAction("Index", "Home", new { id = idConnect });
                    }

                }
                else
                {
                    return RedirectToAction("Connect", "Login");
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }

            return View();
        }
    }
}