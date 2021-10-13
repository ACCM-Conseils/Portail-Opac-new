using DocuWare.Platform.ServerClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace PortailsOpacBase.Portails.Diagnostique.Models
{
    public static class Actions
    {
        private static Organization org;
        static public FileCabinet defaultBasket;
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Login(Guid id)
        {
            
        }

        public static bool AddAction(String Action, String Compte, String Profil)
        {
            ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], System.Configuration.ConfigurationManager.AppSettings["Login"], System.Configuration.ConfigurationManager.AppSettings["MDP"]);

            try
            {
                org = connect.Organizations[0];

                if (connect != null)
                {
                    log.Info("Connexion Docuware Local OK");
                    Console.WriteLine("Connexion Docuware Local OK");
                }
                else
                {
                    log.Info("Connexion Docuware Local impossible");
                    Console.WriteLine("Connexion Docuware Local impossible");
                }

                List<FileCabinet> armoires = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                log.Info("Armoire(s) Locale trouvée(s) : " + armoires.Count());

                defaultBasket = armoires.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetActiviteID"]).First();

                var indexData = new Document()
                {
                    Fields = new List<DocumentIndexField>()
                    {
                        DocumentIndexField.CreateDate("DATE", DateTime.Now),
                        DocumentIndexField.Create("COMPTE", Compte),
                        DocumentIndexField.Create("ACTION", Action),
                        DocumentIndexField.Create("PROFIL", Profil),

                    }
                };

                var uploadedDocument = defaultBasket.UploadDocument(indexData, new FileInfo(@"c:\temp\blank.pdf"));

                return true;
            }
            catch(Exception e)
            {
                return false;
            }
            finally
            {
                connect.Disconnect();
            }
        }

        public static bool AddAction(String Action, String Compte, String Profil, int idDoc, String gbal)
        {
            ServiceConnection connect = PortailsOpacBase.Provider.Docuware.DocuwareProvider.Connect(System.Configuration.ConfigurationManager.AppSettings["URLDocuware"], System.Configuration.ConfigurationManager.AppSettings["Login"], System.Configuration.ConfigurationManager.AppSettings["MDP"]);

            try
            {

                org = connect.Organizations[0];

                if (connect != null)
                {
                    log.Info("Connexion Docuware Local OK");
                    Console.WriteLine("Connexion Docuware Local OK");
                }
                else
                {
                    log.Info("Connexion Docuware Local impossible");
                    Console.WriteLine("Connexion Docuware Local impossible");
                }

                List<FileCabinet> armoires = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;

                log.Info("Armoire(s) Locale trouvée(s) : " + armoires.Count());

                defaultBasket = armoires.Where(f => !f.IsBasket && f.Id == System.Configuration.ConfigurationManager.AppSettings["FileCabinetActiviteID"]).First();

                var indexData = new Document()
                {
                    Fields = new List<DocumentIndexField>()
                    {
                        DocumentIndexField.CreateDate("DATE", DateTime.Now),
                        DocumentIndexField.Create("COMPTE", Compte),
                        DocumentIndexField.Create("ACTION", Action),
                        DocumentIndexField.Create("PROFIL", Profil),
                        DocumentIndexField.Create("GBAL", gbal),
                        DocumentIndexField.Create("IDDOC", idDoc),
                    }
                };

                var uploadedDocument = defaultBasket.UploadDocument(indexData, new FileInfo(@"c:\temp\blank.pdf"));

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                connect.Disconnect();
            }
        }
    }
}