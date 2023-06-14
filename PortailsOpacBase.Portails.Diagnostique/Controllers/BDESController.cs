using DocuWare.Platform.ServerClient;
using DocuWare.WebIntegration;
using PortailsOpacBase.Portails.Diagnostique.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PortailsOpacBase.Portails.Diagnostique.Controllers
{
    public class BDESController : Controller
    {
        private static Organization org;
        static public FileCabinet defaultBasket;
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static String StaticGBAL = "";

        // GET: BDES
        public ActionResult Index(Guid id)
        {
            log.Info("Home id : " + id);

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
                    Response.Redirect("~/Saml2/Logout");
                }
            }

            if (!String.IsNullOrEmpty(Session["Compte"].ToString()) && !String.IsNullOrEmpty(Session["Profil"].ToString()))
            {
                ViewBag.Compte = Session["Compte"];
                ViewBag.Profil = Session["Profil"];

            }

            return View();

        }

        public ActionResult ListeBDES(bool archive = false)
        {
            try
            {

                log.Info("Début de la recherche BDES");

                List<DocumentBDES> documents = new List<DocumentBDES>();

                String cpt = string.Empty;
                String mdp = string.Empty;

                switch (Session["Profil"].ToString())
                {
                    case "OR":
                        cpt = "PortailOR";
                        mdp = "OPAC1";
                        break;
                    case "ENT":
                        cpt = "PortailENT";
                        mdp = "OPAC2";
                        break;
                    case "DPE":
                        cpt = "PortailDPE";
                        mdp = "OPAC3";
                        break;
                    case "REFERENT":
                        cpt = "PortailREFERENT";
                        mdp = "OPAC4";
                        break;
                    default:
                        cpt = System.Configuration.ConfigurationManager.AppSettings["Login"];
                        mdp = System.Configuration.ConfigurationManager.AppSettings["MDP"];
                        break;
                }

                ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], cpt, mdp);

                if (connect != null)
                {

                    log.Info("Connexion OK");

                    org = PortailsOpacBase.Provider.Docuware.DocuwareProvider.GetOrganization(connect);

                    //log.Info("Organization ID : " + org.Id);

                    var fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                    log.Info("Armoires : " + fileCabinets.Count());

                    defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetBDESID"]).First();

                    if (defaultBasket != null)
                        log.Info("Armoire trouvée");

                    //log.Info("FileCabinet trouvé : " + ((defaultBasket != null) ? "Oui" : "Non"));

                    var dialogInfoItems = defaultBasket.GetDialogInfosFromSearchesRelation();
                    var dialog = dialogInfoItems.Dialog.FirstOrDefault(m => m.Id == System.Configuration.ConfigurationManager.AppSettings["SearchDialogBDESID"]).GetDialogFromSelfRelation();

                    if (dialog != null)
                        log.Info("Search dialog trouvée");

                    var cond = new List<DialogExpressionCondition>();

                    if(!archive)
                        cond.Add(DialogExpressionCondition.Create("STATUT", "Actif"));

                    var q = new DialogExpression()
                    {
                        Condition = cond,
                        Count = Int32.MaxValue
                    };

                    var queryResult = dialog.Query.PostToDialogExpressionRelationForDocumentsQueryResult(q);

                    log.Info(queryResult.Items.Count() + " résultat(s)");

                    foreach (Document d in queryResult.Items)
                    {
                        DocumentBDES doc = new DocumentBDES();

                        doc.DocId = System.Convert.ToInt32(d.Fields.FirstOrDefault(o => o.FieldName == "DWDOCID").Item.ToString());
                        doc.NomDocument = ((d.Fields.FirstOrDefault(o => o.FieldName == "NOM_DU_DOCUMENT").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "NOM_DU_DOCUMENT").Item.ToString() : "");
                        doc.Niveau1 = ((d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_1").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_1").Item.ToString() : "");
                        doc.Niveau2 = ((d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_2").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_2").Item.ToString() : "");
                        doc.Niveau3 = ((d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_3").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_3").Item.ToString() : "");
                        doc.Niveau4 = ((d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_4").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_4").Item.ToString() : "");
                        doc.Niveau5 = ((d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_5").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "NIVEAU_5").Item.ToString() : "");
                        doc.Statut = ((d.Fields.FirstOrDefault(o => o.FieldName == "STATUT").Item != null) ? d.Fields.FirstOrDefault(o => o.FieldName == "STATUT").Item.ToString() : "");

                        documents.Add(doc);
                    }

                    return PartialView(documents);
                }

                return PartialView(new List<DocumentBDES>());
            }
            catch (Exception e)
            {
                log.Info(e.Message);

                return PartialView(new List<DocumentBDES>());
            }
        }

        public ActionResult DocPreview(int iddoc)
        {
            ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], System.Configuration.ConfigurationManager.AppSettings["Login"], System.Configuration.ConfigurationManager.AppSettings["MDP"]);

            if (connect != null)
            {
                Actions.AddAction("Téléchargement", Session["Compte"].ToString(), Session["Profil"].ToString(), iddoc, StaticGBAL);

                log.Info("Connexion OK");

                org = PortailsOpacBase.Provider.Docuware.DocuwareProvider.GetOrganization(connect);

                //log.Info("Organization ID : " + org.Id);

                var fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                log.Info("Armoires : " + fileCabinets.Count());

                defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetBDESID"]).First();

                if (defaultBasket != null)
                    log.Info("Armoire trouvée");

                //log.Info("FileCabinet trouvé : " + ((defaultBasket != null) ? "Oui" : "Non"));

                var dialogInfoItems = defaultBasket.GetDialogInfosFromSearchesRelation();
                var dialog = dialogInfoItems.Dialog.FirstOrDefault(m => m.Id == System.Configuration.ConfigurationManager.AppSettings["SearchDialogBDESID"]).GetDialogFromSelfRelation();

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
            ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], System.Configuration.ConfigurationManager.AppSettings["Login"], System.Configuration.ConfigurationManager.AppSettings["MDP"]);

            if (connect != null)
            {
                Actions.AddAction("Téléchargement", Session["Compte"].ToString(), Session["Profil"].ToString(), iddoc, StaticGBAL);

                log.Info("Connexion OK");

                org = PortailsOpacBase.Provider.Docuware.DocuwareProvider.GetOrganization(connect);

                //log.Info("Organization ID : " + org.Id);

                var fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                log.Info("Armoires : " + fileCabinets.Count());

                defaultBasket = fileCabinets.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetBDESID"]).First();

                if (defaultBasket != null)
                    log.Info("Armoire trouvée");

                //log.Info("FileCabinet trouvé : " + ((defaultBasket != null) ? "Oui" : "Non"));

                var dialogInfoItems = defaultBasket.GetDialogInfosFromSearchesRelation();
                var dialog = dialogInfoItems.Dialog.FirstOrDefault(m => m.Id == System.Configuration.ConfigurationManager.AppSettings["SearchDialogBDESID"]).GetDialogFromSelfRelation();

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

                log.Info("Recherche du document avec l'id : " + iddoc.ToString());

                if (queryResult.Items != null)
                {
                    log.Info(queryResult.Items.Count + " résultat(s)");
                }

                if (queryResult.Items.Count > 0)
                {
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
                        String pathToFile = Racine + downloadedFile.FileName.Replace("\"", "");

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
                }
                log.Info("Aucun résultat");
                return null;
            }
            return null;
        }
    }
}