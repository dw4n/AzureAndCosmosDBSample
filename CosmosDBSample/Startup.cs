using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: WebJobsStartup(typeof(CosmosDBSample.Startup))]
namespace CosmosDBSample
{
    public class Startup : FunctionsStartup
    {

        private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
           .SetBasePath(Environment.CurrentDirectory)
           //.AddJsonFile("appsettings.json", true)
           .AddJsonFile("local.settings.json", true)
           .AddEnvironmentVariables()
           .Build();


        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton(s =>
            {
                //var connectionString = Configuration.GetConnectionString("CosmosDB");
                var connectionString = Configuration.GetConnectionString("CosmosDBConnection") ?? Configuration["CosmosDBConnection"];
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException(
                        "Please specify a valid CosmosDB connection string in the appSettings.json file or your Azure Functions Settings.");
                }
                return new CosmosClientBuilder(connectionString).Build();
            });
          
        }
    }
}
