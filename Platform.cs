using System;
using System.Runtime.InteropServices;

namespace Arcadeia
{
   public class Platform
   {
      public class Extension
      {
         public static string Executable
         {
            get
            {
               if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                  return "";
               else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                  return "";
               else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                  return ".exe";
               else
                  throw new NotSupportedException("Platform not supported!");
            }
         }
      }

      public class Separator
      {
         public static string Path
         {
            get
            {
               if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                  return "/";
               else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                  return "/";
               else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                  return "\\";
               else
                  throw new NotSupportedException("Platform not supported!");
            }
         }

         public static string Root
         {
            get
            {
               if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                  return "/";
               else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                  return "/";
               else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                  return "";
               else
                  throw new NotSupportedException("Platform not supported!");
            }
         }
      }
   }
}
