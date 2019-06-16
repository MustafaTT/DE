using DossierExplorer.Models;
using Hangfire;
using Microsoft.Owin;
using Owin;
using DossierExplorer.LuceneIndex;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Dashboard;
[assembly: OwinStartupAttribute(typeof(DossierExplorer.Startup))]
namespace DossierExplorer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {// Storage is the only thing required for basic configuration.
            ConfigureAuth(app);
            // Just discover what configuration options do you have.
            var optionsServer = new BackgroundJobServerOptions
            {
                Queues = new[] { "ahigh", "clow", "default" }
            };

            GlobalConfiguration.Configuration
                .UseSqlServerStorage(@"Data Source=(LocalDb)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\Hangfire.mdf;Initial Catalog=Hangfire;Integrated Security=True");
            //.UseActivator(...)
            //.UseLogProvider(...)
            
            app.UseHangfireServer(optionsServer);
            app.UseHangfireDashboard("/Dashboard", new DashboardOptions() {Authorization = new[] { new HangfireAuthorizationFilter() }}
            
            );
            
            RecurringJob.AddOrUpdate(() => IndexCRUD.AddUpdateLuceneIndex(Path.Combine(GlobalVariables.MyAppPath, "UsersData")), Cron.Weekly(System.DayOfWeek.Saturday,1));//update ındex weekly check If files deleted or added outsite of site 
            RecurringJob.AddOrUpdate(() => IndexCRUD.Optimize(), Cron.Daily(2));//optimize index every day
        }
        protected void Application_Start()
        {
           
            

        }
    }
}
