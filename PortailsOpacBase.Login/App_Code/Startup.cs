using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PortailsOpacBase.Login.Startup))]
namespace PortailsOpacBase.Login
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
