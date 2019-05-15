using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(DocumentSearchMVC.Startup))]
namespace DocumentSearchMVC
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
