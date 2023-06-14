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
    public class HAPController : Controller
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

            return Redirect("/Home");
        }

        public ActionResult NewHAP(Guid diag, String cmd)
        {
            Guid idRapport = Guid.NewGuid();

            Guid newdiag = diag_logement.CopyDiag(diag);

            String URL = @"/HAP/Index?id=" + newdiag + "&connect=" + Session["Connect"] + "&cmd=" + cmd;

            return Json(new { url = URL }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CreateHAP(Guid diag, String cmd)
        {
            Guid idRapport = Guid.NewGuid();

            Guid newdiag = diag_logement.NewDiag(diag);

            String URL = @"/HAP/Index?id=" + newdiag + "&connect=" + Session["Connect"];

            return Json(new { url = URL }, JsonRequestBehavior.AllowGet);
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

        public ActionResult Valider(String g, String cmd, String societe, String corres, String nomdiag, String daterapport, String datedepot, int controle)
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

            string data2 = "?limit=1000000&q={\"numgrpe\":" + g + "}";

            try
            {
                WebRequest wb2 = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_DW_PATRI"] + data2);

                wb2.Method = "GET";
                wb2.ContentType = "application/x-www-form-urlencoded";
                wb2.Headers.Add("Authorization", "Bearer " + OracleToken.access_token);

                StreamReader streamIn2 = new StreamReader(wb2.GetResponse().GetResponseStream());

                String jsonResp2 = streamIn2.ReadToEnd();

                Session["GBAL"] = JsonConvert.DeserializeObject<Gbal>(jsonResp2);

                Session["Adresses"] = ((Gbal)Session["GBAL"]).items.ToList();

                streamIn2.Close();

                if (((List<Logement>)Session["Adresses"]).Count > 0)
                {
                    diag_logement.UpdateDiag(((Guid)Session["idRapport"]), g, "", "", cmd, societe, corres, nomdiag, daterapport, datedepot, controle);

                    Session["a"] = false;
                    Session["LogementEnCours"] = ((List<Logement>)Session["Adresses"]).First(m => String.IsNullOrEmpty(m.numloc) && String.IsNullOrEmpty(m.numall));

                    return Json(new { trouve = true, adresse = ((List<Logement>)Session["Adresses"]).First(m => String.IsNullOrEmpty(m.numloc) && String.IsNullOrEmpty(m.numall)).nomcom }, JsonRequestBehavior.AllowGet);
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

        public ActionResult SaveDiag(String adresse, String numrapport)
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
                        UploadSingleFileToFileCabinet(defaultBasket, "HAP", "Immeuble", diag.groupe, f.Name, "HAP", f.FullName, diag.diagnostiqueur, ville, addresse, diag.numcommande, diag.daterapport.Value, diag.correspondant, Session["Compte"].ToString(), Session["Profil"].ToString());

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

                        XmlElement element2_4 = xmldoc.CreateElement(string.Empty, "Adresse", string.Empty);
                        XmlText text3_4 = xmldoc.CreateTextNode(addresse.TrimStart());
                        element2_4.AppendChild(text3_4);
                        element1_2.AppendChild(element2_4);

                        XmlElement element2_5 = xmldoc.CreateElement(string.Empty, "Ville", string.Empty);
                        XmlText text3_5 = xmldoc.CreateTextNode(ville.TrimEnd());
                        element2_5.AppendChild(text3_5);
                        element1_2.AppendChild(element2_5);

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

                        if (!Directory.Exists(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"HAP\"))
                            Directory.CreateDirectory(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"HAP\");

                        xmldoc.Save(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"HAP\" + ((Guid)Session["idRapport"]) + ".xml");

                        return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = false }, JsonRequestBehavior.AllowGet);
                }
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                log.Error(e);
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
        }

        public static Document UploadSingleFileToFileCabinet(FileCabinet fileCabinet, String Famille, String Patrimoine, String G, string nomdoc, string typedoc, string CheminFichier, string diagnostiqueur, string ville, string addresse, string numcommande, DateTime datediag, string corres, String Compte, String Profil)
        {
            var indexData = new Document()
            {
                Fields = new List<DocumentIndexField>()
                {
                    DocumentIndexField.Create("DOCUMENT_TYPE", typedoc),
                    DocumentIndexField.Create("TYPE_PATRIMOINE", Patrimoine),
                    DocumentIndexField.Create("TYPE_CONTENU", "HAP"),
                    DocumentIndexField.Create("NOM_DOCUMENT", nomdoc),
                    DocumentIndexField.Create("GROUPE", G),
                    DocumentIndexField.Create("FAMILLE_DIAGNOSTIC", Famille),
                    DocumentIndexField.Create("CREE_PAR", diagnostiqueur),
                    DocumentIndexField.Create("COMMUNE", ville),
                    DocumentIndexField.Create("ADRESSE", addresse),
                    DocumentIndexField.Create("STATUT", "NOUVEAU"),
                    DocumentIndexField.Create("NUMERO_COMMANDE", numcommande),
                    DocumentIndexField.CreateDate("DATE_DU_DIAGNOSTIC", datediag),
                    DocumentIndexField.Create("CREE_LE", DateTime.Today),
                    DocumentIndexField.Create("DESTINATAIRE_OPAC", corres),
                }
            };
            var uploadedDocument = fileCabinet.UploadDocument(indexData, new FileInfo(CheminFichier));

            String GBAL = G;

            Actions.AddAction("Dépôt DPE", Compte, Profil, uploadedDocument.Id, GBAL);

            return uploadedDocument;
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

        }
    }
}