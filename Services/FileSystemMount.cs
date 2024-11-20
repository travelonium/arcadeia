using System.Diagnostics;

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
               process.StartInfo.Arguments = "-q " + Folder;
               process.StartInfo.CreateNoWindow = true;
               process.StartInfo.UseShellExecute = false;
               process.StartInfo.RedirectStandardOutput = true;

               process.Start();

               output = process.StandardOutput.ReadToEnd();

               process.WaitForExit(10000);

               if (!process.HasExited || (process.ExitCode != 0))
               {
                  return false;
               }

               return true;
            }
            catch (Exception)
            {
               return false;
            }
         }
      }

      #endregion // Fields

      #region Constructors

      public FileSystemMount(Dictionary<string, string> mount)
      {
         Attached = false;
         Types = mount.GetValueOrDefault("Types") ?? string.Empty;
         Options = mount.GetValueOrDefault("Options") ?? string.Empty;
         Device = mount.GetValueOrDefault("Device") ?? string.Empty;
         Folder = mount.GetValueOrDefault("Folder") ?? string.Empty;

         if (string.IsNullOrEmpty(Types) || string.IsNullOrEmpty(Options) || string.IsNullOrEmpty(Device) || string.IsNullOrEmpty(Folder))
         {
            throw new ArgumentException("One or more mount keys are missing or are empty.");
         }
      }

      #endregion // Constructors

      public void Attach()
      {
         string? output = null;
         string executable = "/bin/mount";

         Directory.CreateDirectory(Folder);

         using Process process = new();

         process.StartInfo.FileName = executable;
         process.StartInfo.Arguments = "-t " + Types;
         process.StartInfo.Arguments += " -o " + Options;
         process.StartInfo.Arguments += " " + Device;
         process.StartInfo.Arguments += " " + Folder;
         process.StartInfo.CreateNoWindow = true;
         process.StartInfo.UseShellExecute = false;
         process.StartInfo.RedirectStandardOutput = true;

         process.Start();

         output = process.StandardOutput.ReadToEnd();

         process.WaitForExit(10000);

         if (!process.HasExited || (process.ExitCode != 0))
         {
               throw new Exception(string.Format("Failed To Mount: {0} Because: {1}", Device, output));
         }

         Attached = true;
      }

      public void Detach()
      {
         string? output = null;
         string executable = "/bin/umount";

         using Process process = new();

         process.StartInfo.FileName = executable;
         process.StartInfo.Arguments = Folder;
         process.StartInfo.CreateNoWindow = true;
         process.StartInfo.UseShellExecute = false;
         process.StartInfo.RedirectStandardOutput = true;

         process.Start();

         output = process.StandardOutput.ReadToEnd();

         process.WaitForExit(10000);

         if (!process.HasExited || (process.ExitCode != 0))
         {
            throw new Exception(String.Format("Failed To Unmount: {0} Because: {1}", Device, output));
         }

         Attached = false;
      }
   }
}

