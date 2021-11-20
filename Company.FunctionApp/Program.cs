using System;
using System.Threading.Tasks;
using Azure.Identity;
using Company.FunctionApp.Trace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Azure;

namespace Company.FunctionApp
{
    public class Program
    {
        public static void Main()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json")
                .AddJsonFile("local.user.settings.json", true)
                .AddEnvironmentVariables()
                .Build();
            
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(defaults =>
                {
                    defaults.UseMiddleware<LogToBlobStorage>();
                })
                .ConfigureServices(services =>
                {
                    services.AddAzureClients(builder =>
                    {
                        builder.AddBlobServiceClient(config.GetSection("Storage").GetValue<string>("ServiceUri"));
                    });
                })
                .Build();

            host.Run();
        }
    }
}