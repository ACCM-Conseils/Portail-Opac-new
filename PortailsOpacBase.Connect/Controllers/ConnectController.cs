using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PortailsOpacBase.Connect.Controllers
{
    public class ConnectController : Controller
    {
        // GET: Connect
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Redirect()
        {
            return Redirect("http://www.google.com");
        }
    }
}