﻿using System.Web;
using System.Web.Mvc;

namespace PortailsOpacBase.AuthTest
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
