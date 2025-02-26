/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

using Arcadeia.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Logging.Console;

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
              .ConfigureLogging(logging =>
              {
                 logging.ClearProviders();
                 logging.AddConsole(options =>
                 {
                     options.FormatterName = ConsoleFormatterNames.Simple;
                 });
              })
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
