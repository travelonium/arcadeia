using Arcadeia.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Arcadeia
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
                 // Remove all existing JsonConfigurationSource instances
                 foreach (var source in builder.Sources.Where(source => source is JsonConfigurationSource).ToList())
                 {
                    builder.Sources.Remove(source);
                 }

                 // Find and temporarily remove the EnvironmentVariablesConfigurationSource
                 var environmentVariablesSource = builder.Sources.FirstOrDefault(source => source is EnvironmentVariablesConfigurationSource);
                 if (environmentVariablesSource != null)
                 {
                    builder.Sources.Remove(environmentVariablesSource);
                 }

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

                 // Re-add the EnvironmentVariablesConfigurationSource at the end
                 if (environmentVariablesSource != null)
                 {
                    builder.Sources.Add(environmentVariablesSource);
                 }
              })
              .ConfigureWebHostDefaults(builder =>
              {
                 builder.UseStartup<Startup>();
              });
   }
}
