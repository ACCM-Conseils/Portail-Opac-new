using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortailsOpacBase.Portails.Diagnostique.Models
{
    [Serializable]
    public class Logement
    {
        public string antenne { get; set; }
        public string lib_antenne { get; set; }
        public string numgrpe { get; set; }
        public string numbati { get; set; }
        public string numall { get; set; }
        public string numloc { get; set; }
        public string usage { get; set; }
        public string typloc { get; set; }
        public string adresse { get; set; }
        public string numcom { get; set; }
        public string nomcom { get; set; }
        public object appt { get; set; }
        public string commerce { get; set; }
        public string loc_ass { get; set; }
        public string foyer { get; set; }
        public string bureau { get; set; }
        public string gere { get; set; }
        public string categorie { get; set; }
        public string identlogt { get; set; }
        public int id_epci { get; set; }
        public string nom_epci { get; set; }
        public string statut_logt { get; set; }
    }
    [Serializable]
    public class Lien
    {
        public string rel { get; set; }
        public string href { get; set; }
    }
    [Serializable]
    public class Gbal
    {
        public List<Logement> items { get; set; }
        public bool hasMore { get; set; }
        public int limit { get; set; }
        public int offset { get; set; }
        public int count { get; set; }
        public List<Lien> links { get; set; }
    }
}