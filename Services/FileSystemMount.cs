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

using System.Diagnostics;
using Arcadeia.Configuration;

namespace Arcadeia.Services
{
   public class FileSystemMount : IDisposable
   {
      #region Fields

      private bool disposed = false;

      public string Types;

      public string? Options;

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
               string executable = "mountpoint";

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

         if (string.IsNullOrEmpty(Types) || string.IsNullOrEmpty(Device) || string.IsNullOrEmpty(Folder))
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

      public void Attach()
      {
         string? error = null;
         string? output = null;
         string executable = "mount";

         Directory.CreateDirectory(Folder);

         using Process process = new();

         process.StartInfo.FileName = executable;
         process.StartInfo.Arguments = string.IsNullOrEmpty(Options) ? $"-t {Types} {Device} {Folder}" : $"-t {Types} -o {Options} {Device} {Folder}";
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
         string executable = "umount";

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

      public void Dispose()
      {
         if (!Attached || disposed) return;

         try
         {
            Detach();

            _logger?.LogInformation("Unmounted: {} @ {}", Device, Folder);

            disposed = true;
         }
         catch (Exception e)
         {
            Error = e.Message;

            _logger?.LogError(e, "Failed To Unmount: {} Because: {}", Device, e.Message);
         }
      }
   }
}

