using DocuWare.Platform.ServerClient;
using DocuWare.WebIntegration;
using Microsoft.AspNet.Identity;
using Microsoft.IdentityModel.Protocols.WSFederation;
using Microsoft.IdentityModel.Web;
using Newtonsoft.Json;
using PortailsOpacBase.Portails.Diagnostique.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Script.Serialization;
using System.Web.Security;

namespace PortailsOpacBase.Portails.Diagnostique.Controllers
{
    public class HomeController : Controller
    {
        private static Gbal _gbal = null;
        private static Communes _communes = null;
        private static Organization org;
        static public FileCabinet defaultBasket;
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static String StaticGBAL = "";

        public ActionResult Index(Guid? id)
        {
            log.Info("Home id : " + id);

            try
            {
                using (var dbContext = new DiagnostiquesEntities())
                {
                    connexions c = dbContext.connexions.FirstOrDefault(m => m.idconnexion == id);

                    if (c != null)
                    {
                        if (!String.IsNullOrEmpty(c.profil))
                        {
                            Session["Compte"] = c.nom;
                            Session["Profil"] = c.profil;
                            Session["Connect"] = id;

                            log.Info("Compte : " + Session["Compte"]);

                            log.Info("Profil : " + Session["Profil"]);
                        }
                        else
                            return RedirectToAction("Index", "Erreur");
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

                    /*

                    */
                }
                return View(_communes);
            }
            catch(Exception e)
            {
                log.Info(e.Message);

                return View(new Communes());
            }
        }


        public ActionResult GetCommunes()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string dataToken = "grant_type=client_credentials";

            WebRequest wbToken = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_Auth_Etud"]);

            wbToken.Method = "POST";
            string postDataToken = dataToken;
            byte[] byteArrayToken = Encoding.UTF8.GetBytes(postDataToken);
            wbToken.ContentType = "application/x-www-form-urlencoded";
            wbToken.ContentLength = byteArrayToken.Length;
            String username = System.Configuration.ConfigurationManager.AppSettings["LoginORACLE"];
            String password = System.Configuration.ConfigurationManager.AppSettings["MDPORACLE"];
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            wbToken.Headers.Add("Authorization", "Basic " + encoded);
            Stream dataStreamToken = wbToken.GetRequestStream();
            dataStreamToken.Write(byteArrayToken, 0, byteArrayToken.Length);
            dataStreamToken.Close();

            HttpWebResponse responseToken = (HttpWebResponse)wbToken.GetResponse();
            Stream receiveStreamToken = responseToken.GetResponseStream();
            StreamReader readStreamToken = new StreamReader(receiveStreamToken, Encoding.UTF8);

            var jss = new JavaScriptSerializer();
            string jsonRespToken = readStreamToken.ReadToEnd();            

            Token OracleToken = JsonConvert.DeserializeObject<Token>(jsonRespToken);

            log.Info("Token : " + OracleToken.access_token);

            try
            {
                WebRequest wb2 = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_DW_COMMUNES"]  + "?limit=20000");

                wb2.Method = "GET";
                wb2.ContentType = "application/x-www-form-urlencoded";
                wb2.Headers.Add("Authorization", "Bearer " + OracleToken.access_token);

                StreamReader streamIn2 = new StreamReader(wb2.GetResponse().GetResponseStream());

                String jsonResp2 = streamIn2.ReadToEnd();

                streamIn2.Close();
                readStreamToken.Close();

                _communes = JsonConvert.DeserializeObject<Communes>(jsonResp2);

                return PartialView(_communes.items.OrderBy(m => m.nomcom).ToList());
            }
            catch (Exception e)
            {
                log.Error("Erreur : " + e.Message);

                return PartialView(_communes);
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Adresses(String ville)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                //Actions.AddAction("Connexion", Compte, "OR");

                string dataToken = "grant_type=client_credentials";

                WebRequest wbToken = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_Auth"]);

                wbToken.Method = "POST";
                string postDataToken = dataToken;
                byte[] byteArrayToken = Encoding.UTF8.GetBytes(postDataToken);
                wbToken.ContentType = "application/x-www-form-urlencoded";
                wbToken.ContentLength = byteArrayToken.Length;
                String username = System.Configuration.ConfigurationManager.AppSettings["LoginORACLE"];
                String password = System.Configuration.ConfigurationManager.AppSettings["MDPORACLE"];
                String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
                wbToken.Headers.Add("Authorization", "Basic " + encoded);
                Stream dataStreamToken = wbToken.GetRequestStream();
                dataStreamToken.Write(byteArrayToken, 0, byteArrayToken.Length);
                dataStreamToken.Close();

                HttpWebResponse responseToken = (HttpWebResponse)wbToken.GetResponse();
                Stream receiveStreamToken = responseToken.GetResponseStream();
                StreamReader readStreamToken = new StreamReader(receiveStreamToken, Encoding.UTF8);

                var jss = new JavaScriptSerializer();
                string jsonRespToken = readStreamToken.ReadToEnd();

                Token OracleToken = JsonConvert.DeserializeObject<Token>(jsonRespToken);

                log.Info("Token : " + OracleToken.access_token);

                string data2 = "?limit=10000000&q={\"nomcom\":\"" + ville + "\"}";

                try
                {
                    WebRequest wb2 = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_DW_PATRI"] + data2);

                    wb2.Method = "GET";
                    wb2.ContentType = "application/x-www-form-urlencoded";
                    wb2.Headers.Add("Authorization", "Bearer " + OracleToken.access_token);

                    StreamReader streamIn2 = new StreamReader(wb2.GetResponse().GetResponseStream());

                    String jsonResp2 = streamIn2.ReadToEnd();

                    _gbal = JsonConvert.DeserializeObject<Gbal>(jsonResp2);

                    log.Info("Logements trouvés : " + _gbal.items.Count());
;                    
                    streamIn2.Close();

                    List<Cible> adresses = _gbal.items.Where(m => !String.IsNullOrEmpty(m.numloc)).Select(r => new Cible()
                    {
                        adresse = r.adresse,
                        gbal = String.Format("{0}-{1}-{2}-{3}", r.numgrpe, r.numbati, r.numall, r.numloc)
                    }).Distinct().OrderBy(m => m.adresse).ToList();

                    return PartialView(adresses);
                }
                catch (Exception e)
                {
                    log.Error("Erreur : " + e.Message);

                    return PartialView(new List<Cible>());
                }

            }
            catch (Exception e)
            {
                log.Error("Erreur : " + e.Message);

                return PartialView(new List<Cible>());
            }
        }

        public ActionResult Search(int type, String g, String b, String a, String l)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                Actions.AddAction("Recherche", Session["Compte"].ToString(), Session["Profil"].ToString());

                log.Info("Début de la recherche");

                List<DocumentOpac> documents = new List<DocumentOpac>();

                String cpt = string.Empty;
                String mdp = string.Empty;

                String Profils = Session["Profil"].ToString();

                if (Profils.Contains("OR"))
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["LoginOR"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDPOR"];
                }
                else if (Profils.Contains("ENT"))
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["LoginENT"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDPENT"];
                }
                else if (Profils.Contains("DPE"))
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["LoginDPE"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDPDPE"];
                }
                else if (Profils.Contains("REFERENT"))
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["LoginREFERENT"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDPREFERENT"];
                }
                else
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["Login"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDP"];
                }

                ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], cpt, mdp);

                if (connect != null)
                {

                    log.Info("Connexion OK");

                    org = PortailsOpacBase.Provider.Docuware.DocuwareProvider.GetOrganization(connect);

                    //log.Info("Organization ID : " + org.Id);

                    var fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                    log.Info("Armoires : " + fileCabinets.Count());

                    defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetID"]).First();

                    if (defaultBasket != null)
                        log.Info("Armoire trouvée");

                    //log.Info("FileCabinet trouvé : " + ((defaultBasket != null) ? "Oui" : "Non"));

                    var dialogInfoItems = defaultBasket.GetDialogInfosFromSearchesRelation();
                    var dialog = dialogInfoItems.Dialog.FirstOrDefault(m => m.Id == System.Configuration.ConfigurationManager.AppSettings["SearchDialogID"]).GetDialogFromSelfRelation();

                    if (dialog != null)
                        log.Info("Search dialog trouvée");

                    //log.Info("Dialog trouvé : " + ((dialog != null) ? "Oui" : "Non"));

                    var cond = new List<DialogExpressionCondition>();

                    if (type == 1)
                    {
                        cond.Add(DialogExpressionCondition.Create("TYPE_DE_DIAGNOSTIC", "Immeuble"));
                    }
                    else if (type == 2)
                    {
                        cond.Add(DialogExpressionCondition.Create("TYPE_DE_DIAGNOSTIC", "Logement"));
                    }

                    cond.Add(DialogExpressionCondition.Create("GROUPE", g));
                    StaticGBAL = g;

                    if (!string.IsNullOrEmpty(b))
                    {
                        StaticGBAL += "-" + b;

                        cond.Add(DialogExpressionCondition.Create("BATI", b));
                    }
                    if (!string.IsNullOrEmpty(a))
                    {
                        StaticGBAL += "-" + a;

                        cond.Add(DialogExpressionCondition.Create("ALLEE", a));
                    }
                    if (!string.IsNullOrEmpty(l))
                    {
                        StaticGBAL += "-" + l;

                        cond.Add(DialogExpressionCondition.Create("LOCAL", l));
                    }

                    var q = new DialogExpression()
                    {
                        Condition = cond,
                        Count = Int32.MaxValue
                    };

                    var queryResult = dialog.Query.PostToDialogExpressionRelationForDocumentsQueryResult(q);

                    log.Info(queryResult.Items.Count() + " résultat(s)");

                    foreach (Document d in queryResult.Items)
                    {
                        DocumentOpac doc = new DocumentOpac();

                        doc.DocId = System.Convert.ToInt32(d.Fields.FirstOrDefault(o => o.FieldName == "DWDOCID").Item.ToString());
                        doc.TypeDocument = ((d.Fields.FirstOrDefault(o => o.FieldName == "DOCUMENT_TYPE").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "DOCUMENT_TYPE").Item.ToString() : "");
                        doc.TypePatrimoine = ((d.Fields.FirstOrDefault(o => o.FieldName == "TYPE_PATRIMOINE").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "TYPE_PATRIMOINE").Item.ToString() : "");
                        doc.FamilleDiagnostic = ((d.Fields.FirstOrDefault(o => o.FieldName == "FAMILLE_DIAGNOSTIC").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "FAMILLE_DIAGNOSTIC").Item.ToString() : "");
                        doc.Groupe = ((d.Fields.FirstOrDefault(o => o.FieldName == "GROUPE").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "GROUPE").Item.ToString() : "");
                        doc.Bati = ((d.Fields.FirstOrDefault(o => o.FieldName == "BATI").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "BATI").Item.ToString() : "");
                        doc.Allee = ((d.Fields.FirstOrDefault(o => o.FieldName == "ALLEE").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "ALLEE").Item.ToString() : "");
                        doc.Local = ((d.Fields.FirstOrDefault(o => o.FieldName == "LOCAL").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "LOCAL").Item.ToString() : "");
                        doc.Statut = ((d.Fields.FirstOrDefault(o => o.FieldName == "STATUT").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "STATUT").Item.ToString() : "");
                        doc.DateDiagnostic = ((d.Fields.FirstOrDefault(o => o.FieldName == "DATE_DU_DIAGNOSTIC").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "DATE_DU_DIAGNOSTIC").Item.ToString() : "");
                        doc.GBAL = doc.Groupe;
                        if (!String.IsNullOrEmpty(doc.Bati))
                            doc.GBAL += "-" + doc.Bati;
                        if (!String.IsNullOrEmpty(doc.Allee))
                            doc.GBAL += "-" + doc.Allee;
                        if (!String.IsNullOrEmpty(doc.Local))
                            doc.GBAL += "-" + doc.Local;

                        documents.Add(doc);
                    }

                    return PartialView(documents);
                }
                else
                    return PartialView(new List<DocumentOpac>());
            }
            catch (Exception e)
            {
                log.Error("Erreur : " + e.Message);
                return PartialView(new List<DocumentOpac>());
            }
        }

        public ActionResult SearchByRep(int type, String g, String b, String a, String l)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                Actions.AddAction("Recherche", Session["Compte"].ToString(), Session["Profil"].ToString());

                log.Info("Début de la recherche");

                List<DocumentOpac> documents = new List<DocumentOpac>();

                String cpt = string.Empty;
                String mdp = string.Empty;

                String Profils = Session["Profil"].ToString();

                if (Profils.Contains("OR"))
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["LoginOR"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDPOR"];
                }
                else if (Profils.Contains("ENT"))
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["LoginENT"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDPENT"];
                }
                else if (Profils.Contains("DPE"))
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["LoginDPE"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDPDPE"];
                }
                else if (Profils.Contains("REFERENT"))
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["LoginREFERENT"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDPREFERENT"];
                }
                else
                {
                    cpt = System.Configuration.ConfigurationManager.AppSettings["Login"];
                    mdp = System.Configuration.ConfigurationManager.AppSettings["MDP"];
                }

                ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], cpt, mdp);

                if (connect != null)
                {

                    log.Info("Connexion OK");

                    org = PortailsOpacBase.Provider.Docuware.DocuwareProvider.GetOrganization(connect);

                    //log.Info("Organization ID : " + org.Id);

                    var fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                    log.Info("Armoires : " + fileCabinets.Count());

                    defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetID"]).First();

                    if (defaultBasket != null)
                        log.Info("Armoire trouvée");

                    //log.Info("FileCabinet trouvé : " + ((defaultBasket != null) ? "Oui" : "Non"));

                    var dialogInfoItems = defaultBasket.GetDialogInfosFromSearchesRelation();
                    var dialog = dialogInfoItems.Dialog.FirstOrDefault(m => m.Id == System.Configuration.ConfigurationManager.AppSettings["SearchDialogID"]).GetDialogFromSelfRelation();

                    if (dialog != null)
                        log.Info("Search dialog trouvée");

                    //log.Info("Dialog trouvé : " + ((dialog != null) ? "Oui" : "Non"));

                    var cond = new List<DialogExpressionCondition>();

                    if (type == 1)
                    {
                        cond.Add(DialogExpressionCondition.Create("TYPE_DE_DIAGNOSTIC", "Immeuble"));
                    }
                    else if (type == 2)
                    {
                        cond.Add(DialogExpressionCondition.Create("TYPE_DE_DIAGNOSTIC", "Logement"));
                    }

                    cond.Add(DialogExpressionCondition.Create("GROUPE", g));
                    StaticGBAL = g;

                    if (!string.IsNullOrEmpty(b))
                    {
                        StaticGBAL += "-" + b;

                        cond.Add(DialogExpressionCondition.Create("BATI", b));
                    }
                    if (!string.IsNullOrEmpty(a))
                    {
                        StaticGBAL += "-" + a;

                        cond.Add(DialogExpressionCondition.Create("ALLEE", a));
                    }
                    if (!string.IsNullOrEmpty(l))
                    {
                        StaticGBAL += "-" + l;

                        cond.Add(DialogExpressionCondition.Create("LOCAL", l));
                    }

                    var q = new DialogExpression()
                    {
                        Condition = cond,
                        Count = Int32.MaxValue
                    };

                    var queryResult = dialog.Query.PostToDialogExpressionRelationForDocumentsQueryResult(q);

                    log.Info(queryResult.Items.Count() + " résultat(s)");

                    foreach (Document d in queryResult.Items)
                    {
                        DocumentOpac doc = new DocumentOpac();

                        doc.DocId = System.Convert.ToInt32(d.Fields.FirstOrDefault(o => o.FieldName == "DWDOCID").Item.ToString());
                        doc.TypeDocument = ((d.Fields.FirstOrDefault(o => o.FieldName == "DOCUMENT_TYPE").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "DOCUMENT_TYPE").Item.ToString() : "");
                        doc.TypePatrimoine = ((d.Fields.FirstOrDefault(o => o.FieldName == "TYPE_PATRIMOINE").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "TYPE_PATRIMOINE").Item.ToString() : "");
                        doc.FamilleDiagnostic = ((d.Fields.FirstOrDefault(o => o.FieldName == "FAMILLE_DIAGNOSTIC").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "FAMILLE_DIAGNOSTIC").Item.ToString() : "");
                        doc.Groupe = ((d.Fields.FirstOrDefault(o => o.FieldName == "GROUPE").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "GROUPE").Item.ToString() : "");
                        doc.Bati = ((d.Fields.FirstOrDefault(o => o.FieldName == "BATI").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "BATI").Item.ToString() : "");
                        doc.Allee = ((d.Fields.FirstOrDefault(o => o.FieldName == "ALLEE").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "ALLEE").Item.ToString() : "");
                        doc.Local = ((d.Fields.FirstOrDefault(o => o.FieldName == "LOCAL").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "LOCAL").Item.ToString() : "");
                        doc.Statut = ((d.Fields.FirstOrDefault(o => o.FieldName == "STATUT").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "STATUT").Item.ToString() : "");
                        doc.DateDiagnostic = ((d.Fields.FirstOrDefault(o => o.FieldName == "DATE_DU_DIAGNOSTIC").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "DATE_DU_DIAGNOSTIC").Item.ToString() : "");
                        doc.GBAL = doc.Groupe;
                        if (!String.IsNullOrEmpty(doc.Bati))
                            doc.GBAL += "-" + doc.Bati;
                        if (!String.IsNullOrEmpty(doc.Allee))
                            doc.GBAL += "-" + doc.Allee;
                        if (!String.IsNullOrEmpty(doc.Local))
                            doc.GBAL += "-" + doc.Local;

                        log.Info("-----------------");
                        log.Info(doc.TypePatrimoine);
                        log.Info(doc.FamilleDiagnostic);
                        log.Info(doc.GBAL);

                        documents.Add(doc);
                    }

                    return PartialView(documents);
                }
                else
                    return PartialView(new List<DocumentOpac>());
            }
            catch (Exception e)
            {
                log.Error("Erreur : " + e.Message);
                return PartialView(new List<DocumentOpac>());
            }
        }

        public ActionResult GBAL(String ville, String adresse)
        {
            String add = adresse;

            if (adresse.Contains("/"))
                add = adresse.Remove(adresse.IndexOf("/"));

            String g = _gbal.items.FirstOrDefault(m => m.nomcom == ville && m.adresse == add).numgrpe;
            String b = _gbal.items.FirstOrDefault(m => m.nomcom == ville && m.adresse == add).numbati;
            String a = _gbal.items.FirstOrDefault(m => m.nomcom == ville && m.adresse == add).numall;
            String l = _gbal.items.FirstOrDefault(m => m.nomcom == ville && m.adresse == add).numloc;

            String gbal = g + "-" + b + "-" + a + "-" + l;

            return PartialView(gbal);
        }

        public void LogOut()
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                Guid token = Guid.Parse(Session["Connect"].ToString());
                connexions c = dbContext.connexions.FirstOrDefault(m => m.idconnexion == token);

                dbContext.connexions.Remove(c);

                dbContext.SaveChanges();

                Response.Redirect("https://adfs.opacoise.fr/adfs/ls/?wa=wsignout1.0");
            }
        }

        public ActionResult NewDiagAmiante()
        {
            Guid idRapport = Guid.NewGuid();

            diag_logement.AddDiag(idRapport, Session["Profil"].ToString(), 1);

            ViewBag.connect = Session["Connect"];

            return Json(new { id = idRapport, connect = Session["Connect"] }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult NewDiagPlomb()
        {
            Guid idRapport = Guid.NewGuid();

            diag_logement.AddDiag(idRapport, Session["Profil"].ToString(), 6);

            ViewBag.connect = Session["Connect"];

            return Json(new { id = idRapport, connect = Session["Connect"] }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult NewTravauxAmiante()
        {
            Guid idRapport = Guid.NewGuid();

            diag_logement.AddDiag(idRapport, Session["Profil"].ToString(), 3);

            ViewBag.connect = Session["Connect"];

            return Json(new { id = idRapport, connect = Session["Connect"] }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult NewDPE()
        {
            Guid idRapport = Guid.NewGuid();

            diag_logement.AddDiag(idRapport, Session["Profil"].ToString(), 2);

            ViewBag.connect = Session["Connect"];

            return Json(new { id = idRapport, connect = Session["Connect"] }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult NewHAP()
        {
            Guid idRapport = Guid.NewGuid();

            diag_logement.AddDiag(idRapport, Session["Profil"].ToString(), 4);

            ViewBag.connect = Session["Connect"];

            return Json(new { id = idRapport, connect = Session["Connect"] }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult NewTravauxPlomb()
        {
            Guid idRapport = Guid.NewGuid();

            diag_logement.AddDiag(idRapport, Session["Profil"].ToString(), 5);

            ViewBag.connect = Session["Connect"];

            return Json(new { id = idRapport, connect = Session["Connect"] }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DocPreview(int iddoc)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], System.Configuration.ConfigurationManager.AppSettings["Login"], System.Configuration.ConfigurationManager.AppSettings["MDP"]);

            if (connect != null)
            {
                Actions.AddAction("Téléchargement", Session["Compte"].ToString(), Session["Profil"].ToString(), iddoc, StaticGBAL);

                log.Info("Connexion OK");

                org = PortailsOpacBase.Provider.Docuware.DocuwareProvider.GetOrganization(connect);

                //log.Info("Organization ID : " + org.Id);

                var fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                log.Info("Armoires : " + fileCabinets.Count());

                defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetID"]).First();

                if (defaultBasket != null)
                    log.Info("Armoire trouvée");

                //log.Info("FileCabinet trouvé : " + ((defaultBasket != null) ? "Oui" : "Non"));

                var dialogInfoItems = defaultBasket.GetDialogInfosFromSearchesRelation();
                var dialog = dialogInfoItems.Dialog.FirstOrDefault(m => m.Id == System.Configuration.ConfigurationManager.AppSettings["SearchDialogID"]).GetDialogFromSelfRelation();

                if (dialog != null)
                    log.Info("Search dialog trouvée");

                var q = new DialogExpression()
                {
                    Condition = new List<DialogExpressionCondition>()
                        {
                            DialogExpressionCondition.Create("DWDOCID", iddoc.ToString() )
                        },
                    Count = Int32.MaxValue
                };

                var queryResult = dialog.Query.PostToDialogExpressionRelationForDocumentsQueryResult(q);

                Document d = queryResult.Items.First();

                log.Info(queryResult.Items.Count() + " résultat(s)");

                Guid dlGuid = Guid.NewGuid();

                var downloadedFile = PortailsOpacBase.Provider.Docuware.DocuwareProvider.DownloadDocumentContent(d);

                String Racine = System.Configuration.ConfigurationManager.AppSettings["Racine"] + dlGuid + @"\";

                if (!Directory.Exists(Racine))
                {
                    Directory.CreateDirectory(Racine);
                }

                if (downloadedFile != null)
                {
                    String pathToFile = Racine + dlGuid + ".pdf";

                    log.Info("Fichier à télécharger : " + pathToFile);

                    if (System.IO.File.Exists(pathToFile))
                        System.IO.File.Delete(pathToFile);

                    using (var file = System.IO.File.Create(pathToFile))
                    using (var stream = downloadedFile.Stream)
                        stream.CopyTo(file);

                    byte[] fileBytes = System.IO.File.ReadAllBytes(pathToFile);

                    string fileName = downloadedFile.FileName.Replace("\"", "");

                    String URL = System.Configuration.ConfigurationManager.AppSettings["ViewerLocal"];
                    ViewBag.URL = URL;
                    return PartialView("DocuwarePreview", dlGuid + @"/" + dlGuid + ".pdf");
                }

                return PartialView("DocuwarePreview", "");
            }

            return PartialView("DocuwarePreview", "");
        }

        public ActionResult Download(int iddoc)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], System.Configuration.ConfigurationManager.AppSettings["Login"], System.Configuration.ConfigurationManager.AppSettings["MDP"]);

            if (connect != null)
            {
                Actions.AddAction("Téléchargement", Session["Compte"].ToString(), Session["Profil"].ToString(), iddoc, StaticGBAL);

                log.Info("Connexion OK");

                org = PortailsOpacBase.Provider.Docuware.DocuwareProvider.GetOrganization(connect);

                //log.Info("Organization ID : " + org.Id);

                var fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                log.Info("Armoires : " + fileCabinets.Count());

                defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetID"]).First();

                if (defaultBasket != null)
                    log.Info("Armoire trouvée");

                //log.Info("FileCabinet trouvé : " + ((defaultBasket != null) ? "Oui" : "Non"));

                var dialogInfoItems = defaultBasket.GetDialogInfosFromSearchesRelation();
                var dialog = dialogInfoItems.Dialog.FirstOrDefault(m => m.Id == System.Configuration.ConfigurationManager.AppSettings["SearchDialogID"]).GetDialogFromSelfRelation();

                if (dialog != null)
                    log.Info("Search dialog trouvée");

                var q = new DialogExpression()
                {
                    Condition = new List<DialogExpressionCondition>()
                        {
                            DialogExpressionCondition.Create("DWDOCID", iddoc.ToString() )
                        },
                    Count = Int32.MaxValue
                };

                var queryResult = dialog.Query.PostToDialogExpressionRelationForDocumentsQueryResult(q);

                Document d = queryResult.Items.First();

                log.Info(queryResult.Items.Count() + " résultat(s)");

                var downloadedFile = PortailsOpacBase.Provider.Docuware.DocuwareProvider.DownloadDocumentContentOriginal(d);

                String Racine = System.Configuration.ConfigurationManager.AppSettings["Racine"] + iddoc + @"\";

                if (!Directory.Exists(Racine))
                {
                    Directory.CreateDirectory(Racine);
                }

                if (downloadedFile != null)
                {
                    String pathToFile = Racine + downloadedFile.FileName.Replace("\"","");

                    log.Info("Fichier à télécharger : " + pathToFile);

                    if (System.IO.File.Exists(pathToFile))
                        System.IO.File.Delete(pathToFile);

                    using (var file = System.IO.File.Create(pathToFile))
                    using (var stream = downloadedFile.Stream)
                        stream.CopyTo(file);

                    byte[] fileBytes = System.IO.File.ReadAllBytes(pathToFile);

                    string fileName = downloadedFile.FileName.Replace("\"", "");

                    return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Pdf, fileName);
                }
                return null;
            }
            return null;
        }
    }
}