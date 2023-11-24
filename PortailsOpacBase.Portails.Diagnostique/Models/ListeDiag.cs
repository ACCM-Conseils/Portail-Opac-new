using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortailsOpacBase.Portails.Diagnostique.Models
{
    [Serializable]
    public class ElemDiag
    {
        public string type_diag { get; set; }
        public string nom_liste { get; set; }
        public string libelle { get; set; }
        public object ordre { get; set; }
        public string pc { get; set; }
        public string logt { get; set; }
        public DateTime date_deb { get; set; }
        public DateTime date_fin { get; set; }
        public object user_cre { get; set; }
        public object date_cre { get; set; }
        public object user_modif { get; set; }
        public object date_modif { get; set; }
        public object commentaire { get; set; }
    }
    [Serializable]
    public class LinkDiag
    {
        public string rel { get; set; }
        public string href { get; set; }
    }
    [Serializable]
    public class ListeDiag
    {
        public List<ElemDiag> items { get; set; }
        public bool hasMore { get; set; }
        public int limit { get; set; }
        public int offset { get; set; }
        public int count { get; set; }
        public List<LinkDiag> links { get; set; }
    }
}