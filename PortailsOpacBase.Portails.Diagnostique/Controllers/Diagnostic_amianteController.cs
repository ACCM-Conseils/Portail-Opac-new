using DocuWare.Platform.ServerClient;
using Newtonsoft.Json;
using PortailsOpacBase.Portails.Diagnostique;
using PortailsOpacBase.Portails.Diagnostique.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;

namespace PortailsOpacBase.Portails.Diagnostic.Controllers
{
    public class Diagnostic_amianteController : Controller
    {
        //private static Guid idRapport = Guid.Empty;
        private static Organization org;
        static public FileCabinet defaultBasket;
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // GET: Diagnostique_amiante
        public ActionResult Index(Guid id, Guid connect)
        {
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
            
            return View();
        }

        public ActionResult GetFichiers(int typelogement)
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

                log.Info(jsonResp2);

                streamIn2.Close();

                Session["ListeDiag"] = JsonConvert.DeserializeObject<ListeDiag>(jsonResp2);

                log.Info("Eléments trouvés : " + ((ListeDiag)Session["ListeDiag"]).items.Count());

                if (typelogement == 1)
                    return PartialView(((ListeDiag)Session["ListeDiag"]).items.Where(m => m.type_diag == "AMIANTE" && m.nom_liste == "FICHIER" && m.pc == "O").OrderBy(m => m.libelle).ToList());
                else
                    return PartialView(((ListeDiag)Session["ListeDiag"]).items.Where(m => m.type_diag == "AMIANTE" && m.nom_liste == "FICHIER" && m.logt == "O").OrderBy(m => m.libelle).ToList());


            }
            catch (Exception e)
            {
                log.Error("Erreur : " + e.Message);

                return PartialView(new List<ElemDiag>());
            }
        }

        public ActionResult GetDiag(Guid diag)
        {
            diag d = diag_logement.GetDiagBase(diag);

            if(d==null)
                return Json(new { trouve = false, numcmd = "", societe = "", corres = "", nomdiag = "" }, JsonRequestBehavior.AllowGet);
            else
                return Json(new { trouve = false, numcmd = d.numcommande, societe = d.nomsociete, corres = d.correspondant, nomdiag = d.diagnostiqueur }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCommande(String numcommande)
        {
            try
            {
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

                        defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetDiagID"]).First();

                        if (defaultBasket != null)
                            log.Info("Armoire trouvée");

                        //log.Info("FileCabinet trouvé : " + ((defaultBasket != null) ? "Oui" : "Non"));

                        var dialogInfoItems = defaultBasket.GetDialogInfosFromSearchesRelation();
                        var dialog = dialogInfoItems.Dialog.FirstOrDefault(m => m.Id == System.Configuration.ConfigurationManager.AppSettings["SearchDialogDiagID"]).GetDialogFromSelfRelation();

                        if (dialog != null)
                            log.Info("Search dialog trouvée");

                        var q = new DialogExpression()
                        {
                            Condition = new List<DialogExpressionCondition>()
                        {
                            DialogExpressionCondition.Create("DOCUMENT_TYPE", "COMMANDE"),
                            DialogExpressionCondition.Create("NUMERO_COMMANDE", numcommande.ToUpper()),
                        },
                            Count = Int32.MaxValue
                        };

                        var queryResult = dialog.Query.PostToDialogExpressionRelationForDocumentsQueryResult(q);

                        log.Info("Document trouvé : " + queryResult.Items.Count);

                        if (queryResult.Items.Count > 0)
                        {
                            Document d = queryResult.Items.First();

                            return Json(new { trouve = true, societe = d.Fields.FirstOrDefault(o => o.FieldName == "NOM_SOCIETE").Item.ToString(), contact = d.Fields.FirstOrDefault(o => o.FieldName == "NOM_CONTACT_OPAC").Item.ToString() }, JsonRequestBehavior.AllowGet);
                        }
                        else
                            return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
                    }
                    else
                        return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception e)
                {
                    return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
                }
                finally
                {
                    connect.Disconnect();
                }
            }
            catch (Exception e)
            {
                return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Logements(int choix)
        {
            if (choix == 1)
            {
                List<Logement> logements = ((List<Logement>)Session["Adresses"]).Where(m => String.IsNullOrEmpty(m.numloc)).ToList();

                return PartialView(logements);
            }
            else
            {
                List<Logement> logements = ((List<Logement>)Session["Adresses"]).Where(m => !String.IsNullOrEmpty(m.numloc)).ToList();

                return PartialView(logements);
            }
        }

        public ActionResult LogementsFiltre(int choix, String g, String b, String a, String l)
        {
            if (choix == 1)
            {
                List<Logement> logements = ((List<Logement>)Session["Adresses"]).Where(m => String.IsNullOrEmpty(m.numloc) && m.numgrpe == g && m.numbati == b).ToList();

                if (!String.IsNullOrEmpty(a))
                    logements = logements.Where(m => m.numall == a).ToList();

                if (!String.IsNullOrEmpty(l))
                    logements = logements.Where(m => m.numloc == l).ToList();

                return PartialView("Logements", logements);
            }
            else
            {
                List<Logement> logements = ((List<Logement>)Session["Adresses"]).Where(m => !String.IsNullOrEmpty(m.numloc) && m.numgrpe == g && m.numbati == b).ToList();

                if (!String.IsNullOrEmpty(a))
                    logements = logements.Where(m => m.numall == a).ToList();

                if (!String.IsNullOrEmpty(l))
                    logements = logements.Where(m => m.numloc == l).ToList();

                return PartialView("Logements", logements);
            }
        }

        public ActionResult LogementsAControler()
        {
            List<diag_logement> diags = diag_logement.GetDiag(((Guid)Session["idRapport"])).OrderBy(m => m.gbal).ToList();

            return PartialView(diags);
        }

        public ActionResult listeLogements()
        {
            List<ElemDiag> diags = ((ListeDiag)Session["ListeDiag"]).items.Where(m => m.type_diag == "AMIANTE" && m.nom_liste == "PIECE_LOGT").OrderBy(m => m.ordre).ToList();

            return PartialView(diags);
        }

        public ActionResult listeCommuns()
        {
            List<ElemDiag> diags = ((ListeDiag)Session["ListeDiag"]).items.Where(m => m.type_diag == "AMIANTE" && m.nom_liste == "PIECE_PC").OrderBy(m => m.ordre).ToList();

            return PartialView(diags);
        }

        public ActionResult listeDAPP()
        {
            List<ElemDiag> diags = ((ListeDiag)Session["ListeDiag"]).items.Where(m => m.type_diag == "AMIANTE" && m.nom_liste == "PIECE_DAPP").OrderBy(m => m.ordre).ToList();

            return PartialView(diags);
        }

        public ActionResult listeLocaux()
        {
            List<ElemDiag> diags = ((ListeDiag)Session["ListeDiag"]).items.Where(m => m.type_diag == "AMIANTE" && m.nom_liste == "PIECE_LOCAUX").OrderBy(m => m.ordre).ToList();

            return PartialView(diags);
        }

        public ActionResult listeZones()
        {
            List<ElemDiag> diags = ((ListeDiag)Session["ListeDiag"]).items.Where(m => m.type_diag == "AMIANTE" && (m.nom_liste == "ZONES")).OrderBy(m => m.ordre).ToList();

            return PartialView(diags);
        }

        public ActionResult LoadDiag(Guid id)
        {
            diag d = diag_logement.GetDiagBase(id);

            if (d != null)
            {
                if (d.numcommande != null)
                    return Json(new { trouve = true, commande = d.numcommande, societe = d.nomsociete, corres = d.correspondant, nomdiag = d.diagnostiqueur, datedurapport = d.daterapport.Value.ToShortDateString(), datedepot = d.datedepot.Value.ToShortDateString(), grouperapport = d.groupe, batirapport = d.bati, alleerapport = d.allee, export = d.export, type_parties = d.type_parties }, JsonRequestBehavior.AllowGet);
                else
                    return Json(new { trouve = false }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { trouve = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DiagLogements()
        {
            List<diag_logement> diags = diag_logement.GetDiag(((Guid)Session["idRapport"])).OrderBy(m => m.gbal).ToList();

            return PartialView(diags);
        }

        public ActionResult DiagLogementsFiltre(string g, string b, string a, string l)
        {
            List<diag_logement> diags = diag_logement.GetDiag(((Guid)Session["idRapport"])).OrderBy(m => m.gbal).ToList();

            return PartialView(diags);
        }

        public ActionResult Fichiers()
        {
            List<diag_logement_fichiers> diags = diag_logement.GetFichiers(((Guid)Session["idRapport"])).OrderBy(m => m.gbal).ToList();

            return PartialView(diags);
        }

        public ActionResult NewDiag(Guid diag, String cmd)
        {
            Guid idRapport = Guid.NewGuid();

            Guid newdiag = diag_logement.CopyDiag(diag);

            String URL = @"/claimapp/Diagnostic_amiante/Index?id=" + newdiag + "&connect=" + Session["Connect"] + "&cmd=" + cmd;

            return Json(new { url = URL }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CreateDiag(Guid diag, String cmd)
        {
            Guid idRapport = Guid.NewGuid();

            Guid newdiag = diag_logement.NewDiag(diag);

            String URL = @"/claimapp/Diagnostic_amiante/Index?id=" + newdiag + "&connect=" + Session["Connect"];

            return Json(new { url = URL }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Selections(String[] selection, String g, String b, String a, bool ajout=false)
        {
            for (int i = 0; i < selection.Length; i++)
            {
                String[] _gbal = selection[i].Split('-');

                if (_gbal.Count() > 3)
                    diag_logement.AddRapport(((Guid)Session["idRapport"]), selection[i]);
                else
                    diag_logement.AddRapportCommun(((Guid)Session["idRapport"]), _gbal);

                diag_logement.AddFichier(((Guid)Session["idRapport"]), "", "", selection[i], "", ajout);
            }

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SaveDiag(String adresse, String[] liste_logements)
        {
            try
            {
                log.Info("Début SaveDiag");

                log.Info("Session : " + Session["idRapport"]);

                List<diag_logement_fichiers> Fichiers = diag_logement.GetAllFichiers(((Guid)Session["idRapport"]));
                List<diag_logement> diags = diag_logement.GetDiag(((Guid)Session["idRapport"])).OrderBy(m => m.gbal).ToList();
                List<RapportLogement> DiagLogements = diag_logement.GetDiagsByIDRapport(((Guid)Session["idRapport"]));

                log.Info("Fichiers : "+ Fichiers.Count());
                log.Info("Logements : " + diags.Count());
                log.Info("Rapports : " + DiagLogements.Count());


                Logement logt = (Logement)Session["LogementEnCours"];

                String ville = logt.nomcom;
                String addresse = (System.Convert.ToBoolean(Session["a"]) ? logt.adresse : "");

                try
                {
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

                            defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetID"]).First();

                            if (defaultBasket != null)
                                log.Info("Armoire trouvée");
                            diag diag = diag_logement.GetDiagBase(((Guid)Session["idRapport"]));

                            if (diag != null)
                            {                              
                                foreach (diag_logement_fichiers f in Fichiers)
                                {
                                    String g = "";
                                    String b = "";
                                    String a = "";
                                    String l = "";

                                    log.Info("Fichier : " + f.nom_fichier);

                                    String[] gbal = f.gbal.Split('-');

                                    if (gbal.Length > 0)
                                        g = gbal[0];
                                    if (gbal.Length > 1)
                                        b = gbal[1];
                                    if (gbal.Length > 2)
                                        a = gbal[2];
                                    if (gbal.Length > 3)
                                        l = gbal[3];

                                    String CheminFichier = System.Configuration.ConfigurationManager.AppSettings["Racine"] + @"Upload\" + ((Guid)Session["idRapport"]) + @"\" + f.nom_fichier;

                                    if (String.IsNullOrEmpty(l))
                                        UploadSingleFileToFileCabinet(defaultBasket, "AMIANTE", "Immeuble", g, b, a, l, f.nom_fichier, f.type_fichier, CheminFichier, diag.diagnostiqueur, ville, addresse, diag.numcommande, diag.daterapport.Value, diag.correspondant, Session["Compte"].ToString(), Session["Profil"].ToString());
                                    else
                                        UploadSingleFileToFileCabinet(defaultBasket, "AMIANTE", "Logement", g, b, a, l, f.nom_fichier, f.type_fichier, CheminFichier, diag.diagnostiqueur, ville, addresse, diag.numcommande, diag.daterapport.Value, diag.correspondant, Session["Compte"].ToString(), Session["Profil"].ToString());

                                }



                                log.Info("Début création XML");

                                log.Info("Diag trouvé");

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


                                foreach (RapportLogement r in DiagLogements.OrderBy(m => m.gbal))
                                {
                                    XmlElement newdiag = xmldoc.CreateElement(string.Empty, "Diagnostic", string.Empty);
                                    element1_3.AppendChild(newdiag);

                                    String[] _gbal = r.gbal.Split('-');

                                    XmlElement elementg = xmldoc.CreateElement(string.Empty, "Groupe", string.Empty);
                                    XmlText textd_1 = xmldoc.CreateTextNode(_gbal[0]);
                                    elementg.AppendChild(textd_1);
                                    newdiag.AppendChild(elementg);

                                    if (_gbal.Length > 1)
                                    {
                                        XmlElement elementb = xmldoc.CreateElement(string.Empty, "Bati", string.Empty);
                                        XmlText textd_1_b = xmldoc.CreateTextNode(_gbal[1]);
                                        elementb.AppendChild(textd_1_b);
                                        newdiag.AppendChild(elementb);
                                    }

                                    if (_gbal.Length > 2)
                                    {
                                        XmlElement elementa = xmldoc.CreateElement(string.Empty, "Allee", string.Empty);
                                        XmlText textd_1_a = xmldoc.CreateTextNode(_gbal[2]);
                                        elementa.AppendChild(textd_1_a);
                                        newdiag.AppendChild(elementa);
                                    }

                                    if (_gbal.Length > 3)
                                    {
                                        XmlElement elementl = xmldoc.CreateElement(string.Empty, "Logement", string.Empty);
                                        XmlText textd_1_l = xmldoc.CreateTextNode(_gbal[3]);
                                        elementl.AppendChild(textd_1_l);
                                        newdiag.AppendChild(elementl);
                                    }

                                    XmlElement elementpiece = xmldoc.CreateElement(string.Empty, "Pièce", string.Empty);
                                    XmlText textd_2 = xmldoc.CreateTextNode(r.piece);
                                    elementpiece.AppendChild(textd_2);
                                    newdiag.AppendChild(elementpiece);

                                    XmlElement elementamiante = xmldoc.CreateElement(string.Empty, "Amiante", string.Empty);
                                    XmlText textd_3 = xmldoc.CreateTextNode(r.amiante ? "Oui" : "Non");
                                    elementamiante.AppendChild(textd_3);
                                    newdiag.AppendChild(elementamiante);

                                    XmlElement elementprelev = xmldoc.CreateElement(string.Empty, "Prélèvements", string.Empty);
                                    XmlText textd_4 = xmldoc.CreateTextNode(r.prelevements.ToString());
                                    elementprelev.AppendChild(textd_4);
                                    newdiag.AppendChild(elementprelev);

                                    XmlElement elementzone = xmldoc.CreateElement(string.Empty, "Zone", string.Empty);
                                    XmlText textd_5 = xmldoc.CreateTextNode(((r.zone != null) ? r.zone : ""));
                                    elementzone.AppendChild(textd_5);
                                    newdiag.AppendChild(elementzone);

                                    XmlElement elementcomm = xmldoc.CreateElement(string.Empty, "Commentaire", string.Empty);
                                    XmlText textd_6 = xmldoc.CreateTextNode(((r.commentaire != null) ? r.commentaire : ""));
                                    elementcomm.AppendChild(textd_6);
                                    newdiag.AppendChild(elementcomm);

                                }

                                XmlElement element1_4 = xmldoc.CreateElement(string.Empty, "Fichiers", string.Empty);
                                Diag.AppendChild(element1_4);


                                foreach (diag_logement_fichiers r in Fichiers)
                                {
                                    XmlElement newdiag = xmldoc.CreateElement(string.Empty, "Fichier", string.Empty);
                                    element1_4.AppendChild(newdiag);

                                    XmlElement elementnomfichier = xmldoc.CreateElement(string.Empty, "NomFichier", string.Empty);
                                    XmlText textd_1 = xmldoc.CreateTextNode(r.nom_fichier);
                                    elementnomfichier.AppendChild(textd_1);
                                    newdiag.AppendChild(elementnomfichier);

                                    XmlElement elementtypefichier = xmldoc.CreateElement(string.Empty, "TypeFichier", string.Empty);
                                    XmlText textd_2 = xmldoc.CreateTextNode(r.type_fichier);
                                    elementtypefichier.AppendChild(textd_2);
                                    newdiag.AppendChild(elementtypefichier);

                                    String[] _gbal = r.gbal.Split('-');

                                    XmlElement elementg = xmldoc.CreateElement(string.Empty, "Groupe", string.Empty);
                                    XmlText textd_4 = xmldoc.CreateTextNode(_gbal[0]);
                                    elementg.AppendChild(textd_4);
                                    newdiag.AppendChild(elementg);

                                    if (_gbal.Length > 1)
                                    {
                                        XmlElement elementb = xmldoc.CreateElement(string.Empty, "Bati", string.Empty);
                                        XmlText textd_1_b = xmldoc.CreateTextNode(_gbal[1]);
                                        elementb.AppendChild(textd_1_b);
                                        newdiag.AppendChild(elementb);
                                    }

                                    if (_gbal.Length > 2)
                                    {
                                        XmlElement elementa = xmldoc.CreateElement(string.Empty, "Allee", string.Empty);
                                        XmlText textd_1_a = xmldoc.CreateTextNode(_gbal[2]);
                                        elementa.AppendChild(textd_1_a);
                                        newdiag.AppendChild(elementa);
                                    }

                                    if (_gbal.Length > 3)
                                    {
                                        XmlElement elementl = xmldoc.CreateElement(string.Empty, "Logement", string.Empty);
                                        XmlText textd_1_l = xmldoc.CreateTextNode(_gbal[3]);
                                        elementl.AppendChild(textd_1_l);
                                        newdiag.AppendChild(elementl);
                                    }

                                    XmlElement elementrapport = xmldoc.CreateElement(string.Empty, "NumRapport", string.Empty);
                                    XmlText textd_5 = xmldoc.CreateTextNode(((r.numrapport != null) ? r.numrapport : ""));
                                    elementrapport.AppendChild(textd_5);
                                    newdiag.AppendChild(elementrapport);
                                }

                                xmldoc.AppendChild(Diag);

                                if (!Directory.Exists(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"AMIANTE\"))
                                    Directory.CreateDirectory(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"AMIANTE\");

                                xmldoc.Save(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"AMIANTE\" + ((Guid)Session["idRapport"]) + ".xml");
                            }

                            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                        }
                        else
                            return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception e)
                    {
                        log.Error(e.Message);

                        return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
                    }
                    finally
                    {
                        connect.Disconnect();
                    }
                }
                catch (Exception e)
                {
                    log.Error(e.Message);

                    return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message);

                return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult SaveRAT(String adresse)
        {
            try
            {
                log.Info("Début SaveRAT");

                log.Info("Session : " + Session["idRapport"]);

                List<diag_logement_fichiers> Fichiers = diag_logement.GetAllFichiers(((Guid)Session["idRapport"]));
                List<diag_logement> diags = diag_logement.GetDiag(((Guid)Session["idRapport"])).OrderBy(m => m.gbal).ToList();
                List<RapportLogement> DiagLogements = diag_logement.GetDiagsByIDRapport(((Guid)Session["idRapport"]));

                log.Info("Fichiers : " + Fichiers.Count());
                log.Info("Logements : " + diags.Count());
                log.Info("Rapports : " + DiagLogements.Count());


                Logement logt = (Logement)Session["LogementEnCours"];

                String ville = logt.nomcom;
                String addresse = (System.Convert.ToBoolean(Session["a"]) ? logt.adresse : "");

                try
                {
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

                            defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetID"]).First();

                            if (defaultBasket != null)
                                log.Info("Armoire trouvée");
                            diag diag = diag_logement.GetDiagBase(((Guid)Session["idRapport"]));

                            if (diag != null)
                            {
                                foreach (diag_logement_fichiers f in Fichiers)
                                {
                                    String g = "";
                                    String b = "";
                                    String a = "";
                                    String l = "";

                                    log.Info("Fichier : " + f.nom_fichier);

                                    String[] gbal = f.gbal.Split('-');

                                    if (gbal.Length > 0)
                                        g = gbal[0];
                                    if (gbal.Length > 1)
                                        b = gbal[1];
                                    if (gbal.Length > 2)
                                        a = gbal[2];
                                    if (gbal.Length > 3)
                                        l = gbal[3];

                                    String CheminFichier = System.Configuration.ConfigurationManager.AppSettings["Racine"] + @"Upload\" + ((Guid)Session["idRapport"]) + @"\" + f.nom_fichier;

                                    if (String.IsNullOrEmpty(l))
                                        UploadSingleFileToFileCabinet(defaultBasket, "AMIANTE", "Immeuble", g, b, a, l, f.nom_fichier, f.type_fichier, CheminFichier, diag.diagnostiqueur, ville, addresse, diag.numcommande, diag.daterapport.Value, diag.correspondant, Session["Compte"].ToString(), Session["Profil"].ToString());
                                    else
                                        UploadSingleFileToFileCabinet(defaultBasket, "AMIANTE", "Logement", g, b, a, l, f.nom_fichier, f.type_fichier, CheminFichier, diag.diagnostiqueur, ville, addresse, diag.numcommande, diag.daterapport.Value, diag.correspondant, Session["Compte"].ToString(), Session["Profil"].ToString());

                                }



                                log.Info("Début création XML");

                                log.Info("Diag trouvé");

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

                                XmlElement element2_4 = xmldoc.CreateElement(string.Empty, "Adresse", string.Empty);
                                XmlText text3_4 = xmldoc.CreateTextNode(addresse.TrimEnd());
                                element2_4.AppendChild(text3_4);
                                element1_2.AppendChild(element2_4);

                                XmlElement element2_5 = xmldoc.CreateElement(string.Empty, "Ville", string.Empty);
                                XmlText text3_5 = xmldoc.CreateTextNode(ville.TrimStart());
                                element2_5.AppendChild(text3_5);
                                element1_2.AppendChild(element2_5);                                

                                XmlElement element1_4 = xmldoc.CreateElement(string.Empty, "Fichiers", string.Empty);
                                Diag.AppendChild(element1_4);


                                foreach (diag_logement_fichiers r in Fichiers)
                                {
                                    XmlElement newdiag = xmldoc.CreateElement(string.Empty, "Fichier", string.Empty);
                                    element1_4.AppendChild(newdiag);

                                    XmlElement elementnomfichier = xmldoc.CreateElement(string.Empty, "NomFichier", string.Empty);
                                    XmlText textd_1 = xmldoc.CreateTextNode(r.nom_fichier);
                                    elementnomfichier.AppendChild(textd_1);
                                    newdiag.AppendChild(elementnomfichier);

                                    XmlElement elementtypefichier = xmldoc.CreateElement(string.Empty, "TypeFichier", string.Empty);
                                    XmlText textd_2 = xmldoc.CreateTextNode(r.type_fichier);
                                    elementtypefichier.AppendChild(textd_2);
                                    newdiag.AppendChild(elementtypefichier);

                                    String[] _gbal = r.gbal.Split('-');

                                    XmlElement elementg = xmldoc.CreateElement(string.Empty, "Groupe", string.Empty);
                                    XmlText textd_4 = xmldoc.CreateTextNode(_gbal[0]);
                                    elementg.AppendChild(textd_4);
                                    newdiag.AppendChild(elementg);

                                    if (_gbal.Length > 1)
                                    {
                                        XmlElement elementb = xmldoc.CreateElement(string.Empty, "Bati", string.Empty);
                                        XmlText textd_1_b = xmldoc.CreateTextNode(_gbal[1]);
                                        elementb.AppendChild(textd_1_b);
                                        newdiag.AppendChild(elementb);
                                    }

                                    if (_gbal.Length > 2)
                                    {
                                        XmlElement elementa = xmldoc.CreateElement(string.Empty, "Allee", string.Empty);
                                        XmlText textd_1_a = xmldoc.CreateTextNode(_gbal[2]);
                                        elementa.AppendChild(textd_1_a);
                                        newdiag.AppendChild(elementa);
                                    }

                                    if (_gbal.Length > 3)
                                    {
                                        XmlElement elementl = xmldoc.CreateElement(string.Empty, "Logement", string.Empty);
                                        XmlText textd_1_l = xmldoc.CreateTextNode(_gbal[3]);
                                        elementl.AppendChild(textd_1_l);
                                        newdiag.AppendChild(elementl);
                                    }

                                    XmlElement elementrapport = xmldoc.CreateElement(string.Empty, "NumRapport", string.Empty);
                                    XmlText textd_5 = xmldoc.CreateTextNode(((r.numrapport != null) ? r.numrapport : ""));
                                    elementrapport.AppendChild(textd_5);
                                    newdiag.AppendChild(elementrapport);
                                }

                                xmldoc.AppendChild(Diag);

                                if (!Directory.Exists(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"AMIANTE\"))
                                    Directory.CreateDirectory(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"AMIANTE\");

                                xmldoc.Save(System.Configuration.ConfigurationManager.AppSettings["RacineDIAG"] + @"AMIANTE\" + ((Guid)Session["idRapport"]) + ".xml");
                            }

                            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                        }
                        else
                            return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (Exception e)
                    {
                        log.Error(e.Message);

                        return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
                    }
                    finally
                    {
                        connect.Disconnect();
                    }
                }
                catch (Exception e)
                {
                    log.Error(e.Message);

                    return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message);

                return Json(new { trouve = false, societe = "", corres = "" }, JsonRequestBehavior.AllowGet);
            }

        }

        public static Document UploadSingleFileToFileCabinet(FileCabinet fileCabinet, String Famille, String Patrimoine, String G, String B, String A, String L, string nomdoc, string typedoc, string CheminFichier, string diagnostiqueur, string ville, string addresse, string numcommande, DateTime datediag, string corres, String Compte, String Profil)
        {
            var indexData = new Document()
            {
                Fields = new List<DocumentIndexField>()
                {
                    DocumentIndexField.Create("DOCUMENT_TYPE", typedoc),
                    DocumentIndexField.Create("TYPE_PATRIMOINE", Patrimoine),
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

            Actions.AddAction("Dépôt Aminante", Compte, Profil, uploadedDocument.Id, GBAL);

            return uploadedDocument;
        }

        public ActionResult DiagEffectues(String[] selection, String[] pieces, bool amiante, int prelevements, bool maj, Guid? idGroupe, string zone, string commentaire)
        {
            try
            {
                Guid _idGroupe = Guid.NewGuid();

                if (maj)
                {
                    diag_logement.DeleteDiagsByGroup((Guid)idGroupe);

                    for (int i = 0; i < selection.Length; i++)
                    {
                        for (int j = 0; j < pieces.Length; j++)
                        {
                            diag_logement.AddDiagEffectues(Guid.Parse(selection[i]), pieces[j], amiante, prelevements, (Guid)idGroupe, zone, commentaire);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < selection.Length; i++)
                    {
                        for (int j = 0; j < pieces.Length; j++)
                        {
                            diag_logement.AddDiagEffectues(Guid.Parse(selection[i]), pieces[j], amiante, prelevements, _idGroupe, zone, commentaire);
                        }
                    }
                }

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                log.Error(e);
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult DeleteGroupe(Guid idgroupe)
        {
            diag_logement.DeleteDiagsByGroup(idgroupe);

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult modcontrole(Guid groupe)
        {
            List<diag_logement_pieces> lignes = diag_logement.GetDiagsByGroup(groupe);

            List<string> _logements = new List<string>();
            List<string> _pieces = new List<string>();

            bool _amiante = false;
            int _prelevements = 0;
            String _zone = String.Empty;
            String _commentaire = String.Empty;

            foreach (diag_logement_pieces p in lignes)
            {
                _logements.Add(diag_logement.GetDiagByLigne((Guid)p.idligne).gbal);
                _pieces.Add(p.piece);
                _amiante = (bool)p.amiante;
                _prelevements = (int)p.prelevements;
                _zone = p.zone;
                _commentaire = p.commentaires;
            }

            return Json(new { success = true, logements = _logements, pieces = _pieces, amiante = _amiante, prelevements = _prelevements, zone = _zone, commentaire = _commentaire }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DelFichier(String id, bool ajout)
        {
            diag_logement.DelFichierByID(Guid.Parse(id), ((Guid)Session["idRapport"]).ToString(), ajout);

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DelDiag(String[] ids)
        {
            bool ok = true;
            for (int i = 0; i < ids.Count(); i++)
            {
                bool result = diag_logement.DelDiagByID(Guid.Parse(ids[i]));
                if (!result)
                    ok = false;
            }

            return Json(new { success = ok }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Upload(int? chunk, string name, Guid id, String typeDoc, String numrapport)
        {
            log.Info("id upload : " + id);
            log.Info("typeDoc upload : " + typeDoc);
            log.Info("numrapport upload : " + numrapport);
            var fileUpload = Request.Files[0];
            var uploadPath = System.Configuration.ConfigurationManager.AppSettings["Racine"] + @"Upload\" + ((Guid)Session["idRapport"]) + @"\";
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            chunk = chunk ?? 0;
            using (var fs = new FileStream(Path.Combine(uploadPath, name), chunk == 0 ? FileMode.Create : FileMode.Append))
            {
                var buffer = new byte[fileUpload.InputStream.Length];
                fileUpload.InputStream.Read(buffer, 0, buffer.Length);
                fs.Write(buffer, 0, buffer.Length);
            }

            diag_logement.updateFichier(id, name, typeDoc, numrapport);

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UploadRAT(int? chunk, string name, String typeDoc, String Gbal, String numrapport, bool ajout=false)
        {
            log.Info("UploadRAT");
            log.Info("typeDoc upload : " + typeDoc);
            log.Info("numrapport upload : " + numrapport);
            var fileUpload = Request.Files[0];
            var uploadPath = System.Configuration.ConfigurationManager.AppSettings["Racine"] + @"Upload\" + ((Guid)Session["idRapport"]) + @"\";
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            chunk = chunk ?? 0;
            using (var fs = new FileStream(Path.Combine(uploadPath, name), chunk == 0 ? FileMode.Create : FileMode.Append))
            {
                var buffer = new byte[fileUpload.InputStream.Length];
                fileUpload.InputStream.Read(buffer, 0, buffer.Length);
                fs.Write(buffer, 0, buffer.Length);
            }

            diag_logement.AddFichier((Guid)Session["idRapport"], name, typeDoc, Gbal, numrapport, ajout);

            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListeDiagEffectues()
        {
            var results = diag_logement.Diags(((Guid)Session["idRapport"]));

            return PartialView(results);
        }

        public ActionResult Valider(String g, String b, String a, String cmd, String societe, String corres, String nomdiag, String daterapport, String datedepot, int controle)
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

            string data2 = "?limit=1000000&q={\"numgrpe\":" + g + ",\"numbati\":" + b + "}";

            log.Info("Requete logement : " + data2);

            try
            {
                WebRequest wb2 = WebRequest.Create(System.Configuration.ConfigurationManager.AppSettings["url_DW_PATRI"] + data2);

                wb2.Method = "GET";
                wb2.ContentType = "application/x-www-form-urlencoded";
                wb2.Headers.Add("Authorization", "Bearer " + OracleToken.access_token);

                StreamReader streamIn2 = new StreamReader(wb2.GetResponse().GetResponseStream());

                String jsonResp2 = streamIn2.ReadToEnd();

                Gbal listegbal = JsonConvert.DeserializeObject<Gbal>(jsonResp2);

                Session["Adresses"] = listegbal.items;

                if (!String.IsNullOrEmpty(a))
                    Session["Adresses"] = ((List<Logement>)Session["Adresses"]).Where(m => m.numall == a).ToList();

                streamIn2.Close();

                if (((List<Logement>)Session["Adresses"]).Count > 0)
                {
                    diag_logement.UpdateDiag(((Guid)Session["idRapport"]), g, b, a, cmd, societe, corres, nomdiag, daterapport, datedepot, controle);

                    foreach (Logement l in (List<Logement>)Session["Adresses"])
                    {
                        log.Info(l.adresse + " - " + l.nomcom);
                    }

                    if (String.IsNullOrEmpty(a))
                    {
                        Session["a"] = false;
                        Session["LogementEnCours"] = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == null).First(m => String.IsNullOrEmpty(m.numloc));
                        return Json(new { trouve = true, adresse = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == null).First(m => String.IsNullOrEmpty(m.numloc)).nomcom }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        Session["a"] = true;
                        Session["LogementEnCours"] = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == a).First(m => String.IsNullOrEmpty(m.numloc));
                        return Json(new { trouve = true, adresse = ((List<Logement>)Session["Adresses"]).Where(m => m.numgrpe == g && m.numbati == b && m.numall == a).First(m => String.IsNullOrEmpty(m.numloc)).adresse + " - " + ((List<Logement>)Session["Adresses"]).First(m => String.IsNullOrEmpty(m.numloc)).nomcom }, JsonRequestBehavior.AllowGet);
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
    }
}