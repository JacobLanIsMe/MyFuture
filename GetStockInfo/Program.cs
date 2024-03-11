using Amazon.Runtime.Internal.Util;
using Caches.Caches;
using Caches.Interfaces;
using GetStockInfo.BackgroundServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDbProvider;
using Repositories.Interfaces;
using Repositories.Repositories;
using System.Threading.Tasks;

class Program
{
    public static void Main(string[] args)
    {
        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Host terminated unexpectedly with error {ex.ToString()}");
        }
    }
    public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
        .ConfigureHostConfiguration(config =>
        {
            config.AddEnvironmentVariables(prefix: "ASPNETCORE_");
            if (args != null)
            {
                config.AddCommandLine(args);
            }
        })
          .ConfigureAppConfiguration((hostContext, config) =>
          {
              config.SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location))
               .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
               .AddJsonFile(path: $"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
          })
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        })
        .ConfigureServices((hostContext, services) =>
        {
            services.AddHttpClient();
            services.AddScoped<ICacheStockTech, CacheStockTech>();
            services.AddScoped<ICacheStockEps, CacheStockEps>();
            services.AddScoped<ICacheStockRevenue, CacheStockRevenue>();
            services.AddScoped<IStockRepository, StockRepository>();
            services.AddScoped<IMongoDbService, MongoDbService>();
            services.AddHostedService<BackgroundStockTech>();
            //services.AddHostedService<BackgroundStockEps>();
            //services.AddHostedService<BackgroundStockRevenue>();
            //services.AddHostedService<WriteStockInfoToMemory>();
            //services.AddMemoryCache();
        });
}
