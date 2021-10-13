using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortailsOpacBase.Portails.Diagnostique.Models
{
    public class DocumentOpac
    {
        public int DocId { get; set; }
        public String TypeDocument { get; set; }
        public String TypePatrimoine { get; set; }
        public String FamilleDiagnostic { get; set; }
        public String Groupe { get; set; }
        public String Bati { get; set; }
        public String Allee { get; set; }
        public String Local { get; set; }
        public String Statut { get; set; }
        public String GBAL { get; set; }
        public String DateDiagnostic { get; set; }
    }

    public class DocumentBDES
    {
        public int DocId { get; set; }
        public String NomDocument { get; set; }
        public String Niveau1 { get; set; }
        public String Niveau2 { get; set; }
        public String Niveau3 { get; set; }
        public String Niveau4 { get; set; }
        public String Niveau5 { get; set; }
        public String Statut { get; set; }
    }
}