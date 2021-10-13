using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortailsOpacBase.Portails.Diagnostique.Models
{
    public class Logements
    {
        public List<Item> items { get; set; }
        public bool hasMore { get; set; }
        public int limit { get; set; }
        public int offset { get; set; }
        public int count { get; set; }
        public List<Link> links { get; set; }
    }
    public class Item
    {
        public int uniges { get; set; }
        public int numgrpe { get; set; }
        public int numbati { get; set; }
        public int numall { get; set; }
        public int numloc { get; set; }
        public string usage { get; set; }
        public string typloc { get; set; }
        public string numvoie { get; set; }
        public string typevoie { get; set; }
        public string nomvoie { get; set; }
        public string numcom { get; set; }
        public string nomcom { get; set; }
        public string appt { get; set; }
        public string commerce { get; set; }
        public string loc_ass { get; set; }
        public string foyer { get; set; }
        public string bureau { get; set; }
        public string gere { get; set; }
        public string categorie { get; set; }
    }

    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
    }

}