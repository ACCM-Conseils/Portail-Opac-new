using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortailsOpacBase.Portails.Diagnostique.Models
{
    public class RapportLogement
    {
        public string gbal { get; set; }
        public string piece { get; set; }
        public bool amiante { get; set; }
        public bool plomb { get; set; }
        public int prelevements { get; set; }
        public string zone { get; set; }
        public string commentaire { get; set; }
        public string element_traite { get; set; }
        public string nature_travaux { get; set; }
    }
}