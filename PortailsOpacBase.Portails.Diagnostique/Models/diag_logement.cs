using PortailsOpacBase.Portails.Diagnostique.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace PortailsOpacBase.Portails.Diagnostique
{
    public partial class diag_logement
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool AddRapport(Guid id, String GBAL)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                String[] descriptif = GBAL.Split('/');

                String _gbal = descriptif[0];

                if (dbContext.diag_logement.Where(m => m.gbal == _gbal && m.iddiag == id).Count() == 0)
                {
                    diag_logement p = new diag_logement();
                    p.id = Guid.NewGuid();
                    p.iddiag = id;
                    p.gbal = descriptif[0];
                    if(descriptif.Length>1)
                        p.appt = descriptif[1];
                    p.datediag = DateTime.Today;
                    p.valid = false;

                    dbContext.diag_logement.Add(p);

                    dbContext.SaveChanges();
                }

                return true;
            }
        }

        public static bool UpdateDiag(Guid idrapport, String g, String b, String a, String cmd, String societe, String corres, String nomdiag, String daterapport, String datedepot, int controle)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                diag d = dbContext.diag.FirstOrDefault(m => m.iddiag == idrapport);

                if (d != null)
                {
                    d.groupe = g;
                    d.bati = b;
                    d.allee = a;
                    d.numcommande = cmd;
                    d.nomsociete = societe;
                    d.correspondant = corres;
                    d.diagnostiqueur = nomdiag;
                    d.daterapport = DateTime.Parse(daterapport);
                    d.datedepot = DateTime.Parse(datedepot);
                    d.type_parties = controle;

                    dbContext.SaveChanges();
                }

                return true;
            }
        }

        public static bool AddDiag(Guid idrapport, String Profil, int Type)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                if (dbContext.diag.Where(m => m.iddiag == idrapport).Count() == 0)
                {
                    diag p = new diag();
                    p.id = Guid.NewGuid();
                    p.iddiag = idrapport;
                    p.profil = Profil;
                    p.type_diag = Type;

                    dbContext.diag.Add(p);

                    dbContext.SaveChanges();
                }

                return true;
            }
        }

        public static Guid CopyDiag(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                Guid iddiag = Guid.NewGuid();

                try
                {
                    diag d = dbContext.diag.FirstOrDefault(m => m.iddiag == id);

                    if (d != null)
                    {
                        diag newd = new diag();

                        newd.id = Guid.NewGuid();
                        newd.iddiag = iddiag;
                        newd.allee = d.allee;
                        newd.bati = d.bati;
                        newd.correspondant = d.correspondant;
                        newd.daterapport = d.daterapport;
                        newd.diagnostiqueur = d.diagnostiqueur;
                        newd.groupe = d.groupe;
                        newd.nomsociete = d.nomsociete;
                        newd.numcommande = d.numcommande;
                        newd.profil = d.profil;
                        newd.type_diag = d.type_diag;

                        dbContext.diag.Add(newd);

                        dbContext.SaveChanges();
                    }
                }
                catch(Exception e)
                {
                    log.Error(e);
                }

                return iddiag;
            }
        }

        public static Guid NewDiag(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                Guid iddiag = Guid.NewGuid();

                diag d = dbContext.diag.FirstOrDefault(m => m.iddiag == id);

                if (d != null)
                {
                    diag newd = new diag();

                    newd.id = Guid.NewGuid();
                    newd.iddiag = iddiag;
                    newd.profil = d.profil;
                    newd.type_diag = d.type_diag;

                    dbContext.diag.Add(newd);

                    dbContext.SaveChanges();
                }

                return iddiag;
            }
        }

        public static bool AddRapportCommun(Guid id, String[] _gbal)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                String gbal = _gbal[0];

                if (_gbal.Length > 1 )
                    gbal += "-" + _gbal[1];
                if (_gbal.Length > 2)
                    gbal += "-" + _gbal[2];

                if (dbContext.diag_logement.Where(m => m.gbal == gbal && m.iddiag == id).Count() == 0)
                {
                    diag_logement p = new diag_logement();
                    p.id = Guid.NewGuid();
                    p.iddiag = id;
                    p.gbal = gbal;
                    p.appt = "Parties communes";
                    p.datediag = DateTime.Today;
                    p.valid = false;

                    dbContext.diag_logement.Add(p);

                    dbContext.SaveChanges();
                }

                return true;
            }
        }

        public static bool AddFichier(Guid id, String nom, String typeDoc, String gbal, String numrapport)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                if (dbContext.diag_logement_fichiers.Where(m => m.iddiag == id && m.nom_fichier == nom && m.gbal == gbal && m.numrapport == numrapport).Count() == 0)
                {
                    diag_logement_fichiers p = new diag_logement_fichiers();
                    p.id = Guid.NewGuid();
                    p.iddiag = id;
                    p.nom_fichier = nom;
                    p.type_fichier = typeDoc;
                    p.gbal = gbal;
                    p.numrapport = numrapport;

                    dbContext.diag_logement_fichiers.Add(p);

                    dbContext.SaveChanges();
                }

                return true;
            }
        }

        public static bool updateFichier(Guid id, String nom, String typeDoc, String numrapport)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                diag_logement_fichiers ToUpdate = dbContext.diag_logement_fichiers.FirstOrDefault(m => m.id == id);

                if(ToUpdate != null)
                {
                    ToUpdate.nom_fichier = nom;
                    ToUpdate.type_fichier = typeDoc;
                    ToUpdate.numrapport = numrapport;

                    dbContext.SaveChanges();

                    return true;
                }

                return false;
            }
        }

        public static List<Diag_effectues> Diags(Guid idrapport)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                var sql = @"SELECT p.idligne, p.piece, p.amiante,p.zone AS elements_traites, SUM(p.prelevements) AS prelevements, diag_logement.gbal, p.groupe, p.numligne,
						  (SELECT COUNT(diag_logement_pieces.idligne) FROM diag_logement_pieces where diag_logement_pieces.groupe = p.groupe AND diag_logement_pieces.piece = p.piece) as nblogements
                          FROM diag_logement INNER JOIN 
                          diag_logement_pieces p ON diag_logement.id = p.idligne 
                          WHERE diag_logement.iddiag = {0}
                          GROUP BY p.idligne,diag_logement.id, diag_logement.gbal, p.groupe, p.piece, p.amiante,p.zone, p.numligne
                          ORDER BY max(p.numligne) DESC";

                /*var sql = @"SELECT diag_logement_pieces.idligne, diag_logement_pieces.piece, diag_logement_pieces.amiante,diag_logement_pieces.zone AS elements_traites, SUM(diag_logement_pieces.prelevements) AS prelevements, diag_logement_pieces.groupe,
						  (SELECT COUNT(diag_logement_pieces.id) FROM diag_logement_pieces WHERE diag_logement_pieces.idligne = diag_logement.id) as nblogements
                          FROM diag_logement INNER JOIN 
                          diag_logement_pieces ON diag_logement.id = diag_logement_pieces.idligne 
                          WHERE diag_logement.iddiag = {0}
                          GROUP BY diag_logement_pieces.idligne,diag_logement.id, diag_logement_pieces.groupe, diag_logement_pieces.piece, diag_logement_pieces.amiante,diag_logement_pieces.zone
                          ORDER BY max(diag_logement_pieces.numligne)";*/

                List<Diag_effectues> _diags = dbContext.Database.SqlQuery<Diag_effectues>(sql, idrapport).ToList();

                return _diags;
            }
        }

        public static List<Diag_effectues> Travaux(Guid idrapport)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                var sql = @"SELECT p.id, p.piece, p.elements_traites, diag_logement.gbal, p.groupe, p.numligne,
						  (SELECT COUNT(diag_logement_pieces.idligne) FROM diag_logement_pieces where diag_logement_pieces.groupe = p.groupe AND diag_logement_pieces.piece = p.piece) as nblogements
                          FROM diag_logement INNER JOIN 
                          diag_logement_pieces p ON diag_logement.id = p.idligne 
                          WHERE diag_logement.iddiag = {0}
                          GROUP BY p.id, p.groupe, p.piece, p.elements_traites, diag_logement.gbal, p.numligne
                          ORDER BY max(p.idligne)";

                List<Diag_effectues> _diags = dbContext.Database.SqlQuery<Diag_effectues>(sql, idrapport).ToList();

                return _diags;
            }
        }

        public static List<Diag_effectues> DiagsPlomb(Guid idrapport)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                var sql = @"SELECT p.piece, p.plomb,p.zone AS elements_traites, diag_logement.gbal, p.groupe, p.numligne,
						  (SELECT COUNT(diag_logement_pieces.idligne) FROM diag_logement_pieces where diag_logement_pieces.groupe = p.groupe AND diag_logement_pieces.piece = p.piece) as nblogements
                          FROM diag_logement INNER JOIN 
                          diag_logement_pieces p ON diag_logement.id = p.idligne 
                          WHERE diag_logement.iddiag = {0}
                          GROUP BY p.groupe, p.piece, p.plomb,p.zone, diag_logement.gbal, p.numligne
                          ORDER BY max(p.idligne)";

                List<Diag_effectues> _diags = dbContext.Database.SqlQuery<Diag_effectues>(sql, idrapport).ToList();

                return _diags;
            }
        }

        public static bool AddDiagEffectues(Guid id, String piece, bool amiante, int prelevements, Guid idGroupe, String zone, String commentaire)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {

                    diag_logement_pieces p = new diag_logement_pieces();
                    p.id = Guid.NewGuid();
                    p.idligne = id;
                    p.piece = piece;
                    p.amiante = amiante;
                    p.prelevements = prelevements;
                    p.groupe = idGroupe;
                    p.zone = zone;
                    p.commentaires = commentaire;

                    dbContext.diag_logement_pieces.Add(p);

                    dbContext.SaveChanges();
                

                return true;
            }
        }

        public static bool AddDiagPlombEffectues(Guid id, String piece, bool plomb, Guid idGroupe, String zone, String commentaire)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {

                    diag_logement_pieces p = new diag_logement_pieces();
                    p.id = Guid.NewGuid();
                    p.idligne = id;
                    p.piece = piece;
                    p.plomb = plomb;
                    p.groupe = idGroupe;
                    p.zone = zone;
                    p.commentaires = commentaire;

                    dbContext.diag_logement_pieces.Add(p);

                    dbContext.SaveChanges();
                

                return true;
            }
        }

        public static bool AddDiagEffectues(Guid id, String piece, Guid idGroupe, String element_traite, String nature_travaux, String zone)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                if (dbContext.diag_logement_pieces.Where(m => m.idligne == id && m.piece == piece).Count() == 0)
                {
                    diag_logement_pieces p = new diag_logement_pieces();
                    p.id = Guid.NewGuid();
                    p.idligne = id;
                    p.piece = piece;
                    p.groupe = idGroupe;
                    p.elements_traites = element_traite;
                    p.zone = zone;
                    p.nature_travaux = nature_travaux;

                    dbContext.diag_logement_pieces.Add(p);

                    dbContext.SaveChanges();
                }

                return true;
            }
        }

        public static bool AddTravauxEffectues(Guid id, String piece, Guid idGroupe, String element_traite, String nature_travaux, String commentaire)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {

                    diag_logement_pieces p = new diag_logement_pieces();
                    p.id = Guid.NewGuid();
                    p.idligne = id;
                    p.piece = piece;
                    p.groupe = idGroupe;
                    p.elements_traites = element_traite;
                    p.commentaires = commentaire;
                    p.nature_travaux = nature_travaux;

                    dbContext.diag_logement_pieces.Add(p);

                    dbContext.SaveChanges();

                return true;
            }
        }

        public static diag GetDiagBase(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                return dbContext.diag.FirstOrDefault(m => m.iddiag == id);
            }
        }

        public static List<diag_logement> GetDiag(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                return dbContext.diag_logement.Where(m => m.iddiag == id).ToList();
            }
        }

        public static bool DelFichierByID(Guid id, String idRapport)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                diag_logement_fichiers f = dbContext.diag_logement_fichiers.FirstOrDefault(m => m.id == id);

                List<diag_logement_fichiers> fichiers = dbContext.diag_logement_fichiers.Where(m => m.iddiag == f.iddiag && m.nom_fichier == f.nom_fichier).ToList();

                foreach (diag_logement_fichiers fi in fichiers)
                {
                    var uploadPath = @"C:\Temp\Upload\" + idRapport + @"\" + fi.nom_fichier;

                    File.Delete(uploadPath);

                    dbContext.diag_logement_fichiers.Remove(fi);
                }

                dbContext.SaveChanges();

                return true;
            }
        }

        public static bool DelDiagByID(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                int count = dbContext.diag_logement_pieces.Where(m => m.idligne == id).Count();

                if (count == 0)
                {
                    diag_logement f = dbContext.diag_logement.FirstOrDefault(m => m.id == id);

                    dbContext.diag_logement.Remove(f);

                    dbContext.SaveChanges();

                    return true;
                }
                else
                    return false;
            }
        }

        public static bool DelDiagLigneByID(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {

                diag_logement_pieces f = dbContext.diag_logement_pieces.FirstOrDefault(m => m.id == id);

                dbContext.diag_logement_pieces.Remove(f);

                dbContext.SaveChanges();

                return true;
            }
        }

        public static diag_logement GetDiagByLigne(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                return dbContext.diag_logement.FirstOrDefault(m => m.id == id);
            }
        }

        public static List<diag_logement_pieces> GetDiagsByGroup(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                return dbContext.diag_logement_pieces.Where(m => m.groupe == id).ToList();
            }
        }

        public static List<RapportLogement> GetDiagsByIDRapport(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                var result = (from diag_logement_pieces in dbContext.diag_logement_pieces
                              join diag_logement in dbContext.diag_logement on new { id = (Guid)diag_logement_pieces.idligne } equals new { id = diag_logement.id }
                              where diag_logement.iddiag == id
                              select new RapportLogement()
                              {
                                  gbal = diag_logement.gbal,
                                  piece = diag_logement_pieces.piece,
                                  amiante = (bool)diag_logement_pieces.amiante,
                                  prelevements = (int)diag_logement_pieces.prelevements,
                                  zone = diag_logement_pieces.zone,
                                  commentaire = diag_logement_pieces.commentaires
                              }).ToList();

                return result;
            }
        }

        public static List<RapportLogement> GetTravauxByIDRapport(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                var result = (from diag_logement_pieces in dbContext.diag_logement_pieces
                              join diag_logement in dbContext.diag_logement on new { id = (Guid)diag_logement_pieces.idligne } equals new { id = diag_logement.id }
                              where diag_logement.iddiag == id
                              select new RapportLogement()
                              {
                                  gbal = diag_logement.gbal,
                                  piece = diag_logement_pieces.piece,
                                  element_traite = diag_logement_pieces.elements_traites,
                                  nature_travaux = diag_logement_pieces.nature_travaux,
                                  zone = diag_logement_pieces.zone,
                                  commentaire = diag_logement_pieces.commentaires
                              }).ToList();

                return result;
            }
        }

        public static List<RapportLogement> GetDiagsPlombByIDRapport(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                var result = (from diag_logement_pieces in dbContext.diag_logement_pieces
                              join diag_logement in dbContext.diag_logement on new { id = (Guid)diag_logement_pieces.idligne } equals new { id = diag_logement.id }
                              where diag_logement.iddiag == id
                              select new RapportLogement()
                              {
                                  gbal = diag_logement.gbal,
                                  piece = diag_logement_pieces.piece,
                                  plomb = (bool)diag_logement_pieces.plomb,
                                  zone = diag_logement_pieces.zone,
                                  commentaire = diag_logement_pieces.commentaires
                              }).ToList();

                return result;
            }
        }

        public static bool DeleteDiagsByGroup(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                List<diag_logement_pieces> pieces = dbContext.diag_logement_pieces.Where(m => m.groupe == id).ToList();

                foreach(diag_logement_pieces p in pieces)
                {
                    dbContext.diag_logement_pieces.Remove(p);
                }

                dbContext.SaveChanges();

                return true;
            }
        }

        public static List<diag_logement_fichiers> GetFichiers(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                List<diag_logement_fichiers> filtered = new List<diag_logement_fichiers>();

                List<diag_logement_fichiers> result = dbContext.diag_logement_fichiers.Where(m => m.iddiag == id).ToList();

                foreach(diag_logement_fichiers d in result)
                {
                    if (!String.IsNullOrEmpty(d.numrapport) || !String.IsNullOrEmpty(d.nom_fichier))
                    {
                        if (filtered.Where(o => o.numrapport == d.numrapport && o.nom_fichier == d.nom_fichier).Count() == 0)
                            filtered.Add(d);
                    }
                    else
                        filtered.Add(d);
                }

                return filtered;
            }
        }

        public static List<diag_logement_fichiers> GetAllFichiers(Guid id)
        {
            using (var dbContext = new DiagnostiquesEntities())
            {
                List<diag_logement_fichiers> result = dbContext.diag_logement_fichiers.Where(m => m.iddiag == id).ToList();

                return result;
            }
        }
    }
}