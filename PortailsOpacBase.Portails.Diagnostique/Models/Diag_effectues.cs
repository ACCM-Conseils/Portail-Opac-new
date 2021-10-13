using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PortailsOpacBase.Portails.Diagnostique.Models
{
    public class Diag_effectues
    {
        public string piece { get; set; }
        public int nblogements { get; set; }
        public int prelevements { get; set; }
        public string elements_traites { get; set; }
        public bool amiante { get; set; }
        public bool plomb { get; set; }
        public Guid groupe { get; set; }
        public Guid id { get; set; }
        public int numligne { get; set; }
        public string gbal { get; set; }
    }
}