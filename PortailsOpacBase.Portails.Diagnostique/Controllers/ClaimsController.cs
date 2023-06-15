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

                    String ProfilClaim = "";

                   foreach (var c in System.Security.Claims.ClaimsPrincipal.Current.Claims)
                    {

                        if (c.Type.StartsWith("group"))
                        {
                            switch (c.Value)
                            {
                                case "4ca2d1a6-c286-49e9-87b6-457aa109b3bd":
                                    ProfilClaim += "REFERENT;";
                                    break;
                                case "c99ea476-f0be-49d7-ad63-c82f6dd61692":
                                    ProfilClaim += "OR;";
                                    break;
                                case "db02ab2f-f924-4064-a4e6-d18dc46fa3a5":
                                    ProfilClaim += "DPE;";
                                    break;
                                case "f7cb8b84-d977-420b-989a-9d33c0745898":
                                    ProfilClaim += "ENT;";
                                    break;
                                case "83f762e0-c785-4396-9ce5-8120bef8bf60":
                                    ProfilClaim += "BDES;";
                                    break;
                            }

                            if(!string.IsNullOrEmpty(ProfilClaim))
                                break;
                        }
                    }

                    Guid? idConnect = Guid.NewGuid();

                    Session["Connect"] = idConnect;
                    Session["Profil"] = ProfilClaim;

                    using (var dbContext = new DiagnostiquesEntities())
                    {
                        connexions c = new connexions();
                        c.profil = ProfilClaim;
                        c.idconnexion = idConnect;
                        c.id = Guid.NewGuid();
                        c.dateheure = DateTime.Now;
                        c.nom = System.Security.Claims.ClaimsPrincipal.Current.Claims.FirstOrDefault(m => m.Type.EndsWith("displayname")).Value;

                        dbContext.connexions.Add(c);

                        dbContext.SaveChanges();

                        int num = 1;
                        if (ProfilClaim.Contains("BDES") && (ProfilClaim.Contains("DPE") || ProfilClaim.Contains("OR") || ProfilClaim.Contains("REF") || ProfilClaim.Contains("ENT")))
                            num = 2;
                        else if (ProfilClaim.Contains("BDES") && !ProfilClaim.Contains("DPE") && !ProfilClaim.Contains("OR") && !ProfilClaim.Contains("REF") && !ProfilClaim.Contains("ENT"))
                            num = 3;
                        switch (num)
                        {
                            case 1:
                                return RedirectToAction("Index", "Home", new { id = idConnect });
                            case 2:
                                return RedirectToAction("Index", "Choix", new { id = idConnect });
                            case 3:
                                return RedirectToAction("Index", "BDES", new { id = idConnect });
                        }
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