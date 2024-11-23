using MediaCurator.Configuration;
using Microsoft.Extensions.Options;

namespace MediaCurator
{
   public class Program
   {
      public static void Main(string[] args)
      {
         CreateHostBuilder(args).Build().Run();
      }

      public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
              .ConfigureAppConfiguration((context, builder) =>
              {
                 // Clear default configuration providers
                 builder.Sources.Clear();

                 // Get the current environment
                 var environment = context.HostingEnvironment.EnvironmentName;

                 builder.Add(new WritableJsonConfigurationSource
                 {
                    Path = "appsettings.json",
                    Optional = false,
                    ReloadOnChange = true
                 });

                 // Add environment-specific configuration file if it exists
                 builder.Add(new WritableJsonConfigurationSource
                 {
                    Path = $"appsettings.{environment}.json",
                    Optional = true,
                    ReloadOnChange = true
                 });
              })
              .ConfigureWebHostDefaults(builder =>
              {
                 builder.UseStartup<Startup>();
              });
   }
}
