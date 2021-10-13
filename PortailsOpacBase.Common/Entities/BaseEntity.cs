using PortailsOpacBase.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortailsOpacBase.Common.Entities
{
    [Serializable]
    public class BaseEntity : IEntity
    {
        public static ILogHelper Logger;
    }
}
