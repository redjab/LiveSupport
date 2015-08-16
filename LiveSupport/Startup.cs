using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(LiveSupport.Startup))]
namespace LiveSupport
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
