using System.Diagnostics;
using MediaCurator.Configuration;

namespace MediaCurator.Services
{
   public class FileSystemMount
   {
      #region Fields

      public string Types;

      public string Options;

      public string Device;

      public string Folder;

      public bool Attached { get; private set; }

      public string Error = string.Empty;

      private readonly ILogger<FileSystemMount>? _logger;

      public bool Available
      {
         get
         {
            try
            {
               string? output = null;
               string executable = "/bin/mountpoint";

               using Process process = new();

               process.StartInfo.FileName = executable;
               process.StartInfo.Arguments = $"-q {Folder}";
               process.StartInfo.CreateNoWindow = true;
               process.StartInfo.UseShellExecute = false;
               process.StartInfo.RedirectStandardOutput = true;

               process.Start();

               _logger?.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

               output = process.StandardOutput.ReadToEnd();

               process.WaitForExit(10000);

               if (!process.HasExited || (process.ExitCode != 0))
               {
                  return false;
               }

               return true;
            }
            catch (Exception e)
            {
               _logger?.LogWarning("Failed To Check Availability For: {}, Because: {}", Folder, e.Message);

               return false;
            }
         }
      }

      #endregion // Fields

      #region Constructors

      public FileSystemMount(MountSettings mount, ILogger<FileSystemMount>? logger = null)
      {
         Attached = false;
         Types = mount.Types;
         Options = mount.Options;
         Device = mount.Device;
         Folder = mount.Folder;

         _logger = logger;

         if (string.IsNullOrEmpty(Types) || string.IsNullOrEmpty(Options) || string.IsNullOrEmpty(Device) || string.IsNullOrEmpty(Folder))
         {
            throw new ArgumentException("One or more mount keys are missing or are empty.");
         }

         try
         {
            Attach();

            _logger?.LogInformation("Mounted: {} @ {}", Device, Folder);
         }
         catch (Exception e)
         {
            Error = e.Message;

            _logger?.LogError(e, "Failed To Mount: {} Because: {}", Device, e.Message);
         }
      }

      #endregion // Constructors

      #region Destructors

      ~FileSystemMount()
      {
         if (!Attached) return;

         try
         {
            Detach();

            _logger?.LogInformation("Unmounted: {} @ {}", Device, Folder);
         }
         catch (Exception e)
         {
            Error = e.Message;

            _logger?.LogError(e, "Failed To Unmount: {} Because: {}", Device, e.Message);
         }
      }

      #endregion // Destructors

      public void Attach()
      {
         string? error = null;
         string? output = null;
         string executable = "/bin/mount";

         Directory.CreateDirectory(Folder);

         using Process process = new();

         process.StartInfo.FileName = executable;
         process.StartInfo.Arguments = $"-t {Types} -o {Options} {Device} {Folder}";
         process.StartInfo.CreateNoWindow = true;
         process.StartInfo.UseShellExecute = false;
         process.StartInfo.RedirectStandardError = true;
         process.StartInfo.RedirectStandardOutput = true;

         process.Start();

         _logger?.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

         error = process.StandardError.ReadToEnd();
         output = process.StandardOutput.ReadToEnd();

         process.WaitForExit(10000);

         if (!process.HasExited || (process.ExitCode != 0))
         {
            throw new Exception(string.IsNullOrEmpty(error) ? output : error);
         }

         Attached = true;
      }

      public void Detach()
      {
         string? error = null;
         string? output = null;
         string executable = "/bin/umount";

         using Process process = new();

         process.StartInfo.FileName = executable;
         process.StartInfo.Arguments = Folder;
         process.StartInfo.CreateNoWindow = true;
         process.StartInfo.UseShellExecute = false;
         process.StartInfo.RedirectStandardError = true;
         process.StartInfo.RedirectStandardOutput = true;

         process.Start();

         _logger?.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

         error = process.StandardError.ReadToEnd();
         output = process.StandardOutput.ReadToEnd();

         process.WaitForExit(10000);

         if (!process.HasExited || (process.ExitCode != 0))
         {
            throw new Exception(string.IsNullOrEmpty(error) ? output : error);
         }

         Attached = false;
      }
   }
}

