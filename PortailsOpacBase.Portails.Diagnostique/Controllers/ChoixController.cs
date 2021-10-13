using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PortailsOpacBase.Portails.Diagnostique.Controllers
{
    public class ChoixController : Controller
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: Choix
        public ActionResult Index(Guid id)
        {
            log.Info("Home id : " + id);

            try
            {
                using (var dbContext = new DiagnostiquesEntities())
                {
                    connexions c = dbContext.connexions.FirstOrDefault(m => m.idconnexion == id);

                    if (c != null)
                    {
                        Session["Compte"] = c.nom;
                        Session["Profil"] = c.profil;
                        Session["Connect"] = id;

                        log.Info("Compte : " + Session["Compte"]);

                        log.Info("Profil : " + Session["Profil"]);
                    }
                    else
                    {
                        Response.Redirect("https://adfs.opacoise.fr/adfs/ls/?wa=wsignout1.0");
                    }
                }

                if (!String.IsNullOrEmpty(Session["Compte"].ToString()) && !String.IsNullOrEmpty(Session["Profil"].ToString()))
                {
                    ViewBag.Compte = Session["Compte"];
                    ViewBag.Profil = Session["Profil"];
                }
                return View();
            }
            catch (Exception e)
            {
                log.Info(e.Message);

                return View();
            }
        }

        public ActionResult Diagnostics()
        {
            return Json(new { connect = Session["Connect"] }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult BDES()
        {
            return Json(new { connect = Session["Connect"] }, JsonRequestBehavior.AllowGet);
        }
    }
}