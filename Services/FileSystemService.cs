using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Permissions;
using System.IO;
using System.Diagnostics;

namespace MediaCurator.Services
{
   public class FileSystemService : IHostedService, IDisposable
   {
      protected readonly IConfiguration _configuration;

      private readonly ILogger<FileSystemService> _logger;

      private readonly CancellationToken _cancellationToken;

      public List<Dictionary<string, string>> Mounts
      {
         get
         {
            if (_configuration.GetSection("Mounts").Exists())
            {
               return _configuration.GetSection("Mounts").Get<List<Dictionary<string, string>>>();
            }
            else
            {
               return new List<Dictionary<string, string>>();
            }
         }
      }

      #region Constructors

      public FileSystemService(IConfiguration configuration,
                               ILogger<FileSystemService> logger,
                               IHostApplicationLifetime applicationLifetime)
      {
         _logger = logger;
         _configuration = configuration;
         _cancellationToken = applicationLifetime.ApplicationStopping;
      }

      #endregion // Constructors

      [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
      public Task StartAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Starting FileSystem Service...");

         // Try to mount all the configured mounts.
         foreach (var mount in Mounts)
         {
            Mount(mount);
         }

         _logger.LogInformation("FileSystem Service Started.");

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         // Unmount all the configured mounts.
         foreach (var mount in Mounts)
         {
            Unmount(mount);
         }

         _logger.LogInformation("FileSystem Service Stopped.");

         return Task.CompletedTask;
      }

      public bool Mount(Dictionary<string, string> mount)
      {
         string output = null;
         string executable = "/bin/mount";
         string types = mount.GetValueOrDefault("Types", null);
         string options = mount.GetValueOrDefault("Options", null);
         string device = mount.GetValueOrDefault("Device", null);
         string directory = mount.GetValueOrDefault("Directory", null);

         Directory.CreateDirectory(directory);

         using (Process process = new Process())
         {
            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = "-t " + types;
            process.StartInfo.Arguments += " -o " + options;
            process.StartInfo.Arguments += " " + device;
            process.StartInfo.Arguments += " " + directory;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            _logger.LogInformation("Mounting: {}", process.StartInfo.Arguments);

            process.Start();

            output = process.StandardOutput.ReadToEnd();

            process.WaitForExit(10000);

            if (!process.HasExited || (process.ExitCode != 0))
            {
               _logger.LogError("Failed To Mount: {} Because: ", device, output);

               return false;
            }
         }

         _logger.LogInformation("Mounted: {} @ {}", device, directory);

         return true;
      }

      public bool Unmount(Dictionary<string, string> mount)
      {
         string output = null;
         string executable = "/bin/umount";
         string device = mount.GetValueOrDefault("Device", null);
         string directory = mount.GetValueOrDefault("Directory", null);

         using (Process process = new Process())
         {
            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = directory;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            output = process.StandardOutput.ReadToEnd();

            process.WaitForExit(10000);

            if (!process.HasExited || (process.ExitCode != 0))
            {
               _logger.LogError("Failed To Unmount: {} Because: ", device, output);

               return false;
            }
         }

         _logger.LogInformation("Unmounted: {} @ {}", device, directory);

         return true;
      }

      public void Dispose()
      {
      }
   }
}
