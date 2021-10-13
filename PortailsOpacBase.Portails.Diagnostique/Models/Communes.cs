using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortailsOpacBase.Portails.Diagnostique.Models
{
    public class Ville
    {
        public string numcom { get; set; }
        public string nomcom { get; set; }
    }

    public class LienCommune
    {
        public string rel { get; set; }
        public string href { get; set; }
    }

    public class Communes
    {
        public List<Item> items { get; set; }
        public bool hasMore { get; set; }
        public int limit { get; set; }
        public int offset { get; set; }
        public int count { get; set; }
        public List<Link> links { get; set; }
    }
}