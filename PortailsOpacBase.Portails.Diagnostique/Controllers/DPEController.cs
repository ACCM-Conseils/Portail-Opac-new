using DocuWare.Platform.ServerClient;
using Newtonsoft.Json;
using PortailsOpacBase.Portails.Diagnostique.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;

namespace PortailsOpacBase.Portails.Diagnostique.Controllers
{
    public class DPEController : Controller
    {
        private static Organization org;
        private static Gbal _gbal = null;
        static public FileCabinet defaultBasket;
        private static Communes _communes = null;
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // GET: DPE
        public ActionResult Index(Guid id, Guid connect)
        {
            log.Info("DPE id : " + id);

            Session["idRapport"] = id;
            ViewBag.connect = connect;

            using (var dbContext = new DiagnostiquesEntities())
            {
                connexions c = dbContext.connexions.FirstOrDefault(m => m.idconnexion == connect);

                if (c != null)
                {
                    Session["Compte"] = c.nom;
                    Session["Profil"] = c.profil;
                    Session["Connect"] = connect;

                    log.Info("Compte : " + Session["Compte"]);

                    log.Info("Profil : " + Session["Profil"]);
                }
                else
                {
                    Response.Redirect("https://adfs.opacoise.fr/adfs/ls/?wa=wsignout1.0");
                }
            }

            ViewBag.idRapport = id;

            if (!String.IsNullOrEmpty(Session["Compte"].ToString()) && !String.IsNullOrEmpty(Session["Profil"].ToString()))
            {
                return View();                
            }

            return Redirect("/Claimapp/Home");
        }

        public ActionResult GetCommunes()
        {
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
                WebRequest wb2 = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_DW_COMMUNES"] + "?limit=20000");

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


        public ActionResult Adresses(String ville)
        {
            try
            {
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
                        adresse = r.adresse + (r.appt != null ? "/ Appt " + r.appt.ToString() : ""),
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

            /*List<Cible> adresses = _gbal.items.Where(m => m.nomcom == ville && !String.IsNullOrEmpty(m.numloc)).Select(r => new Cible()
            {
                adresse = r.adresse + (r.appt != null ? "/ Appt " + r.appt.ToString() : ""),
                gbal = String.Format("{0}-{1}-{2}-{3}", r.numgrpe, r.numbati, r.numall, r.numloc)
            }).Distinct().OrderBy(m => m.adresse).ToList();

            return PartialView(adresses);*/
        }

        public ActionResult GetChauffage()
        {
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

            readStreamToken.Close();

            Token OracleToken = JsonConvert.DeserializeObject<Token>(jsonRespToken);


            try
            {
                WebRequest wb2 = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_DW_LISTE_DIAG"] + "?limit=200");

                wb2.Method = "GET";
                wb2.ContentType = "application/x-www-form-urlencoded";
                wb2.Headers.Add("Authorization", "Bearer " + OracleToken.access_token);

                StreamReader streamIn2 = new StreamReader(wb2.GetResponse().GetResponseStream());

                String jsonResp2 = streamIn2.ReadToEnd();

                streamIn2.Close();

                Session["ListeDiag"] = JsonConvert.DeserializeObject<ListeDiag>(jsonResp2);

                log.Info("Eléments trouvés : " + ((ListeDiag)Session["ListeDiag"]).items.Count());

                return PartialView(((ListeDiag)Session["ListeDiag"]).items.Where(m => m.type_diag == "DPE" && m.nom_liste == "CHAUFFAGE").OrderBy(m => m.libelle).ToList());


            }
            catch (Exception e)
            {
                log.Error("Erreur : " + e.Message);

                return PartialView(new List<Cible>());
            }
        }

        public ActionResult GetECS()
        {
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

            readStreamToken.Close();

            Token OracleToken = JsonConvert.DeserializeObject<Token>(jsonRespToken);


            try
            {
                WebRequest wb2 = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_DW_LISTE_DIAG"] + "?limit=200");

                wb2.Method = "GET";
                wb2.ContentType = "application/x-www-form-urlencoded";
                wb2.Headers.Add("Authorization", "Bearer " + OracleToken.access_token);

                StreamReader streamIn2 = new StreamReader(wb2.GetResponse().GetResponseStream());

                String jsonResp2 = streamIn2.ReadToEnd();

                streamIn2.Close();

                Session["ListeDiag"] = JsonConvert.DeserializeObject<ListeDiag>(jsonResp2);

                log.Info("Eléments trouvés : " + ((ListeDiag)Session["ListeDiag"]).items.Count());

                return PartialView(((ListeDiag)Session["ListeDiag"]).items.Where(m => m.type_diag == "DPE" && m.nom_liste == "ECS").OrderBy(m => m.libelle).ToList());


            }
            catch (Exception e)
            {
                log.Error("Erreur : " + e.Message);

                return PartialView(new List<Cible>());
            }
        }

        public ActionResult NewDPE(Guid diag, String cmd)
        {
            Guid idRapport = Guid.NewGuid();

            Guid newdiag = diag_logement.CopyDiag(diag);

            String URL = @"/claimapp/DPE/Index?id=" + newdiag + "&connect=" + Session["Connect"]+"&cmd="+ cmd;

            return Json(new { url = URL }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CreateDPE(Guid diag, String cmd)
        {
            Guid idRapport = Guid.NewGuid();

            Guid newdiag = diag_logement.NewDiag(diag);

            String URL = @"/claimapp/DPE/Index?id=" + newdiag + "&connect=" + Session["Connect"];

            return Json(new { url = URL }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCommande(Guid diag)
        {
            diag d = diag_logement.GetDiagBase(diag);

            return Json(new { numcmd = d.numcommande, societe = d.nomsociete, corres = d.correspondant, nomdiag = d.diagnostiqueur }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Valider(String g, String b, String a, String l, String cmd, String societe, String corres, String nomdiag, String daterapport, String datedepot, int controle)
        {
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

            string data2 = "?limit=1000000&q={\"numgrpe\":" + g + ",\"numbati\":" + b;

            if (!String.IsNullOrEmpty(a))
                data2 += ",\"numall\":" + a;
            if (!String.IsNullOrEmpty(l))
                data2 += ",\"numloc\":" + l;

            data2 += "}";

            try
            {
                WebRequest wb2 = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_DW_PATRI"] + data2);

                wb2.Method = "GET";
                wb2.ContentType = "application/x-www-form-urlencoded";
                wb2.Headers.Add("Authorization", "Bearer " + OracleToken.access_token);

                StreamReader streamIn2 = new StreamReader(wb2.GetResponse().GetResponseStream());

                String jsonResp2 = streamIn2.ReadToEnd();

                log.Info(jsonResp2);

                Session["GBAL"] = JsonConvert.DeserializeObject<Gbal>(jsonResp2);

                Session["Adresses"] = ((Gbal)Session["GBAL"]).items.ToList();

                streamIn2.Close();

                if (((List<Logement>)Session["Adresses"]).Count > 0)
                {
                    diag_logement.UpdateDiag(((Guid)Session["idRapport"]), g, b, a, cmd, societe, corres, nomdiag, daterapport, datedepot, controle);

                    if (String.IsNullOrEmpty(a))
                    {
                        Session["a"] = false;
                        Session["LogementEnCours"] = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == null && m.numloc == null).First();
                        return Json(new { trouve = true, adresse = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == null && m.numloc == null).First().nomcom }, JsonRequestBehavior.AllowGet);
                    }
                    else if (String.IsNullOrEmpty(l))
                    {
                        Session["a"] = true;
                        Session["LogementEnCours"] = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == a && m.numloc == null).First();
                        return Json(new { trouve = true, adresse = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == a && m.numloc == null).First().adresse + " - " + ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == a && m.numloc == null).First().nomcom }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        Session["a"] = true;
                        Session["LogementEnCours"] = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == a && m.numloc == l).First();
                        return Json(new { trouve = true, adresse = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == a && m.numloc == l).First().adresse + " - " + ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == a && m.numloc == l).First().nomcom }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                    return Json(new { trouve = false, adresse = "" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                log.Error(e.Message);

                return Json(new { trouve = false, adresse = "" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult SaveDiag(String adresse, String l, String etiquettedpe, String valeurdpe, String etiquetteges, String valeurges, String typechauffage, String typeecs, String mcarre, String numrapport)
        {
            Logement logt = (Logement)Session["LogementEnCours"];

            String ville = logt.nomcom;
            String addresse = (System.Convert.ToBoolean(Session["a"]) ? logt.adresse : "");

            ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], System.Configuration.ConfigurationManager.AppSettings["Login"], System.Configuration.ConfigurationManager.AppSettings["MDP"]);

            try
            {
                if (connect != null)
                {
                    log.Info("Connexion OK");

                    org = PortailsOpacBase.Provider.Docuware.DocuwareProvider.GetOrganization(connect);

                    //log.Info("Organization ID : " + org.Id);

                    var fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                    log.Info("Armoires : " + fileCabinets.Count());

                    defaultBasket = fileCabinets.Where(o => !o.IsBasket && o.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetID"]).First();

                    if (defaultBasket != null)
                        log.Info("Armoire trouvée");
                    diag diag = diag_logement.GetDiagBase(((Guid)Session["idRapport"]));

                    String[] fichiers = Directory.GetFiles(System.Configuration.ConfigurationManager.AppSettings["Racine"] + @"Upload\" + ((Guid)Session["idRapport"]) + @"\");

                    FileInfo f = new FileInfo(fichiers[0]);

                    if (diag != null)
                    {
                        UploadSingleFileToFileCabinet(defaultBasket, "DPE", "Logement", diag.groupe, diag.bati, diag.allee, l, f.Name, "DPE Logement", f.FullName, diag.diagnostiqueur, ville, addresse, diag.numcommande, diag.daterapport.Value, diag.correspondant, Session["Compte"].ToString(), Session["Profil"].ToString(), etiquettedpe, valeurdpe, etiquetteges, valeurges, typechauffage, typeecs, mcarre, numrapport);

                        XmlDocument xmldoc = new XmlDocument();

                        //(1) the xml declaration is recommended, but not mandatory
                        XmlDeclaration xmlDeclaration = xmldoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                        XmlElement root = xmldoc.DocumentElement;
                        xmldoc.InsertBefore(xmlDeclaration, root);

                        //(2) string.Empty makes cleaner code
                        XmlElement Diag = xmldoc.CreateElement(string.Empty, "Rapport", string.Empty);

                        //(2) string.Empty makes cleaner code
                        XmlElement element1 = xmldoc.CreateElement(string.Empty, "Identification", string.Empty);
                        Diag.AppendChild(element1);

                        XmlElement element3 = xmldoc.CreateElement(string.Empty, "Commande", string.Empty);
                        XmlText text2 = xmldoc.CreateTextNode(diag.numcommande);
                        element3.AppendChild(text2);
                        element1.AppendChild(element3);

                        XmlElement element4 = xmldoc.CreateElement(string.Empty, "Societe", string.Empty);
                        XmlText text3 = xmldoc.CreateTextNode(diag.nomsociete);
                        element4.AppendChild(text3);
                        element1.AppendChild(element4);

                        XmlElement element5 = xmldoc.CreateElement(string.Empty, "Correspondant", string.Empty);
                        XmlText text4 = xmldoc.CreateTextNode(diag.correspondant);
                        element5.AppendChild(text4);
                        element1.AppendChild(element5);

                        XmlElement element6 = xmldoc.CreateElement(string.Empty, "Diagnostiqueur", string.Empty);
                        XmlText text5 = xmldoc.CreateTextNode(diag.diagnostiqueur);
                        element6.AppendChild(text5);
                        element1.AppendChild(element6);

                        XmlElement element7 = xmldoc.CreateElement(string.Empty, "DateRapport", string.Empty);
                        XmlText text6 = xmldoc.CreateTextNode(diag.daterapport.Value.ToShortDateString());
                        element7.AppendChild(text6);
                        element1.AppendChild(element7);

                        XmlElement element8 = xmldoc.CreateElement(string.Empty, "CreeLe", string.Empty);
                        XmlText text7 = xmldoc.CreateTextNode(diag.datedepot.Value.ToShortDateString());
                        element8.AppendChild(text7);
                        element1.AppendChild(element8);

                        XmlElement element1_2 = xmldoc.CreateElement(string.Empty, "IdentificationLogement", string.Empty);
                        Diag.AppendChild(element1_2);

                        XmlElement element2_1 = xmldoc.CreateElement(string.Empty, "Groupe", string.Empty);
                        XmlText text2_2 = xmldoc.CreateTextNode(diag.groupe);
                        element2_1.AppendChild(text2_2);
                        element1_2.AppendChild(element2_1);

                        XmlElement element2_2 = xmldoc.CreateElement(string.Empty, "Bati", string.Empty);
                        XmlText text3_2 = xmldoc.CreateTextNode(diag.bati);
                        element2_2.AppendChild(text3_2);
                        element1_2.AppendChild(element2_2);

                        XmlElement element2_3 = xmldoc.CreateElement(string.Empty, "Allee", string.Empty);
                        XmlText text3_3 = xmldoc.CreateTextNode(diag.allee);
                        element2_3.AppendChild(text3_3);
                        element1_2.AppendChild(element2_3);

                        XmlElement element2_6 = xmldoc.CreateElement(string.Empty, "Local", string.Empty);
                        XmlText text3_6 = xmldoc.CreateTextNode(l);
                        element2_6.AppendChild(text3_6);
                        element1_2.AppendChild(element2_6);

                        XmlElement element2_4 = xmldoc.CreateElement(string.Empty, "Adresse", string.Empty);
                        XmlText text3_4 = xmldoc.CreateTextNode(addresse.TrimEnd());
                        element2_4.AppendChild(text3_4);
                        element1_2.AppendChild(element2_4);

                        XmlElement element2_5 = xmldoc.CreateElement(string.Empty, "Ville", string.Empty);
                        XmlText text3_5 = xmldoc.CreateTextNode(ville.TrimStart());
                        element2_5.AppendChild(text3_5);
                        element1_2.AppendChild(element2_5);

                        XmlElement element1_3 = xmldoc.CreateElement(string.Empty, "Diagnostics", string.Empty);
                        Diag.AppendChild(element1_3);

                        XmlElement newdiag = xmldoc.CreateElement(string.Empty, "Diagnostic", string.Empty);
                        element1_3.AppendChild(newdiag);

                        XmlElement elementDPE = xmldoc.CreateElement(string.Empty, "EtiquetteDPE", string.Empty);
                        XmlText textd_2 = xmldoc.CreateTextNode(etiquettedpe);
                        elementDPE.AppendChild(textd_2);
                        newdiag.AppendChild(elementDPE);

                        XmlElement elementvaleurDPE = xmldoc.CreateElement(string.Empty, "ValeurDPE", string.Empty);
                        XmlText textd_3 = xmldoc.CreateTextNode(valeurdpe);
                        elementvaleurDPE.AppendChild(textd_3);
                        newdiag.AppendChild(elementvaleurDPE);

                        XmlElement elementGES = xmldoc.CreateElement(string.Empty, "EtiquetteGES", string.Empty);
                        XmlText textd_4 = xmldoc.CreateTextNode(etiquetteges);
                        elementGES.AppendChild(textd_4);
                        newdiag.AppendChild(elementGES);

                        XmlElement elementvaleurGES = xmldoc.CreateElement(string.Empty, "ValeurGES", string.Empty);
                        XmlText textd_5 = xmldoc.CreateTextNode(valeurges);
                        elementvaleurGES.AppendChild(textd_5);
                        newdiag.AppendChild(elementvaleurGES);

                        XmlElement elementChauffage = xmldoc.CreateElement(string.Empty, "TypeChauffage", string.Empty);
                        XmlText textd_6 = xmldoc.CreateTextNode(typechauffage);
                        elementChauffage.AppendChild(textd_6);
                        newdiag.AppendChild(elementChauffage);

                        XmlElement elementEcs = xmldoc.CreateElement(string.Empty, "TypeECS", string.Empty);
                        XmlText textd_7 = xmldoc.CreateTextNode(typeecs);
                        elementEcs.AppendChild(textd_7);
                        newdiag.AppendChild(elementEcs);

                        XmlElement elementSurface = xmldoc.CreateElement(string.Empty, "Surface", string.Empty);
                        XmlText textd_8 = xmldoc.CreateTextNode(mcarre);
                        elementSurface.AppendChild(textd_8);
                        newdiag.AppendChild(elementSurface);

                        XmlElement element1_4 = xmldoc.CreateElement(string.Empty, "Fichiers", string.Empty);
                        Diag.AppendChild(element1_4);

                        XmlElement newdiagFile = xmldoc.CreateElement(string.Empty, "Fichier", string.Empty);
                        element1_4.AppendChild(newdiagFile);

                        XmlElement elementnomfichier = xmldoc.CreateElement(string.Empty, "NomFichier", string.Empty);
                        XmlText textd_1File = xmldoc.CreateTextNode(f.Name);
                        elementnomfichier.AppendChild(textd_1File);
                        newdiagFile.AppendChild(elementnomfichier);

                        XmlElement elementtypefichier = xmldoc.CreateElement(string.Empty, "TypeFichier", string.Empty);
                        XmlText textd_2File = xmldoc.CreateTextNode(f.Extension);
                        elementtypefichier.AppendChild(textd_2File);
                        newdiagFile.AppendChild(elementtypefichier);

                        XmlElement elementrapport = xmldoc.CreateElement(string.Empty, "NumRapport", string.Empty);
                        XmlText textd_4File = xmldoc.CreateTextNode(numrapport);
                        elementrapport.AppendChild(textd_4File);
                        newdiagFile.AppendChild(elementrapport);


                        xmldoc.AppendChild(Diag);

                        if (!Directory.Exists(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"DPE\"))
                            Directory.CreateDirectory(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"DPE\");

                        xmldoc.Save(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"DPE\" + ((Guid)Session["idRapport"]) + ".xml");

                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
            catch(Exception e)
            {
                log.Error(e);
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }

            
        }

        public static Document UploadSingleFileToFileCabinet(FileCabinet fileCabinet, String Famille, String Patrimoine, String G, String B, String A, String L, string nomdoc, string typedoc, string CheminFichier, string diagnostiqueur, string ville, string addresse, string numcommande, DateTime datediag, string corres, String Compte, String Profil, String etiquettedpe, String valeurdpe, String etiquetteges, String valeurges, String typechauffage, String typeecs, String mcarre, String numrapport)
        {
            var indexData = new Document()
            {
                Fields = new List<DocumentIndexField>()
                {
                    DocumentIndexField.Create("DOCUMENT_TYPE", typedoc),
                    DocumentIndexField.Create("TYPE_PATRIMOINE", Patrimoine),
                    DocumentIndexField.Create("ENERGIE_ETIQUETTE", etiquettedpe),
                    DocumentIndexField.Create("ENERGIE_CONSOMMATION", valeurdpe),
                    DocumentIndexField.Create("GES_ETIQUETTE", etiquetteges),
                    DocumentIndexField.Create("GES_EMISSIONS", valeurges),
                    DocumentIndexField.Create("TYPE_ENERGIE_CHAUFFAGE", typechauffage),
                    DocumentIndexField.Create("TYPE_ENERGIE_ECS", typeecs),
                    DocumentIndexField.Create("SURFACE", mcarre),
                    DocumentIndexField.Create("TYPE_CONTENU", "DPE Logement"),
                    DocumentIndexField.Create("NOM_DOCUMENT", nomdoc),
                    DocumentIndexField.Create("GROUPE", G),
                    DocumentIndexField.Create("BATI", B),
                    DocumentIndexField.Create("ALLEE", A),
                    DocumentIndexField.Create("LOCAL", L),
                    DocumentIndexField.Create("FAMILLE_DIAGNOSTIC", Famille),
                    DocumentIndexField.Create("CREE_PAR", diagnostiqueur),
                    DocumentIndexField.Create("COMMUNE", ville),
                    DocumentIndexField.Create("ADRESSE", addresse),
                    DocumentIndexField.Create("STATUT", "NOUVEAU"),
                    DocumentIndexField.Create("NUMERO_COMMANDE", numcommande),
                    DocumentIndexField.CreateDate("DATE_DU_DIAGNOSTIC", datediag),
                    DocumentIndexField.Create("CREE_LE", DateTime.Today),
                    DocumentIndexField.Create("DESTINATAIRE_OPAC", corres),
                    DocumentIndexField.Create("N_ADEME", numrapport),
                }
            };
            var uploadedDocument = fileCabinet.UploadDocument(indexData, new FileInfo(CheminFichier));

            String GBAL = G;
            if (!string.IsNullOrEmpty(B))
                GBAL += "-" + B;
            if (!string.IsNullOrEmpty(A))
                GBAL += "-" + A;
            if (!string.IsNullOrEmpty(L))
                GBAL += "-" + L;

            Actions.AddAction("Dépôt DPE", Compte, Profil, uploadedDocument.Id, GBAL);

            return uploadedDocument;
        }

        public ActionResult Fichiers()
        {
            List<diag_logement_fichiers> diags = diag_logement.GetFichiers(((Guid)Session["idRapport"])).ToList();

            return PartialView(diags);
        }

        public ActionResult Upload(int? chunk, string name, String[] Gbal, String numrapport, bool ajout=false)
        {
            log.Info("typeDoc upload : DPE");
            var fileUpload = Request.Files[0];
            var uploadPath = @"C:\Temp\Upload\" + ((Guid)Session["idRapport"]) + @"\";
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            chunk = chunk ?? 0;
            using (var fs = new FileStream(Path.Combine(uploadPath, name), chunk == 0 ? FileMode.Create : FileMode.Append))
            {
                var buffer = new byte[fileUpload.InputStream.Length];
                fileUpload.InputStream.Read(buffer, 0, buffer.Length);
                fs.Write(buffer, 0, buffer.Length);
            }

            foreach (String s in Gbal)
                diag_logement.AddFichier(((Guid)Session["idRapport"]), name, "DPE", s, numrapport, ajout);

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
    }
}