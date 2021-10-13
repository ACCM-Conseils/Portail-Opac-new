using System.Web;
using System.Web.Mvc;

namespace PortailsOpacBase.Portails.Diagnostique
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
