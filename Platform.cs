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
