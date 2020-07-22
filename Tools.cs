using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace MediaCurator
{
   class Tools
   {
      /// <summary>
      /// Retrieves the child elements of a parent element having its Id attribute set to the specified
      /// value and returns the first one found. This function is case-insensitive.
      /// </summary>
      /// <param name="parent">The parent element.</param>
      /// <param name="type">The element names we are interested in.</param>
      /// <param name="id">The element Id we are interested in.</param>
      /// <returns>The element with the specified Id or null if none found.</returns>
      public static XElement GetElementByIdAttribute(XElement parent, string type, string id)
      {
         XElement element = null;
         IEnumerable<XElement> elements = null;

         if (parent == null)
         {
            return element;
         }

         // Extract the element which is named type and its Name attribute value is name.
         elements = parent.Descendants(type)
                          .Where(el => ((string)el.Attribute("Id")).ToLower() == id.ToLower());

         // Verify if any such elements where found and if so, return the first one.
         if (elements.Count() > 0)
         {
            element = elements.First();
         }

         return element;
      }

      /// <summary>
      /// Retrieves the child elements of a parent element having its name attribute set to the
      /// specified value and returns the first one found. This function is case-insensitive.
      /// </summary>
      /// <param name="parent">The parent element.</param>
      /// <param name="type">The element names we are interested in.</param>
      /// <param name="name">The value of the Name attribute.</param>
      /// <returns></returns>
      public static XElement GetElementByNameAttribute(XElement parent, string type, string name)
      {
         XElement element = null;
         IEnumerable<XElement> elements = null;

         if (parent == null)
         {
            return element;
         }

         // Extract the element which is named type and its Name attribute value is name.
         elements = parent.Elements(type)
                          .Where(el => ((string)el.Attribute("Name")).ToLower() == name.ToLower());

         // Verify if any such elements where found and if so, return the first one.
         if (elements.Count() > 0)
         {
            element = elements.First();
         }

         return element;
      }

      /// <summary>
      /// Retrieves the child elements of a parent element having its name attribute set to the
      /// specified value and return them all. This function is case-insensitive.
      /// </summary>
      /// <param name="parent">The parent element.</param>
      /// <param name="type">The element names we are interested in.</param>
      /// <param name="name">The value of the Name attribute.</param>
      /// <returns>All the elements with a matching name and type.</returns>
      public static IEnumerable<XElement> GetElementsByNameAttribute(XElement parent, string type, string name)
      {
         IEnumerable<XElement> elements = null;

         if (parent == null)
         {
            return elements;
         }

         // Extract the elements which are named type and its Name attribute value is name.
         elements = parent.Elements(type)
                          .Where(el => ((string)el.Attribute("Name")).ToLower() == name.ToLower());

         return elements;
      }

      /// <summary>
      /// Counts the number of specific decendants of a specific parent element.
      /// </summary>
      /// <param name="parent">The element whose decendants are of interest.</param>
      /// <param name="type">The matching value of the name of the elements.</param>
      /// <param name="flags">The flags to filter the results.</param>
      /// <param name="values">The values of the flags to filter with.</param>
      /// <param name="recursive">If set to <c>True</c>, the search will include all levels.</param>
      /// <returns>The number of decendants of the parent element matching the name and the flags.</returns>
      public static uint GetDecendantsCount(XElement parent, string type = null, uint flags = 0,
                                             uint values = 0, bool recursive = true)
      {
         uint count = 0;

         foreach (XElement item in (recursive ? parent.Descendants() : parent.Nodes()))
         {
            if ((type != null) ? (item.Name.ToString() == type) : true)
            {
               string flagsTag = Tools.GetAttributeValue(item, "Flags");
               uint flagsInteger = Convert.ToUInt32(flagsTag.Length > 0 ? flagsTag : "0", 16);

               // Mask out only the flags we care about and compare them to the expected values.
               if (((flagsInteger & flags) ^ (values & flags)) > 0)
               {
                  // The requested flags don't match those of the item's.
                  continue;
               }

               count++;
            }
         }

         return count;
      }

      public static string GetAttributeValue(XElement element, string attributeName)
      {
         string value = "";

         if (element != null)
         {
            XAttribute attribute = element.Attribute(attributeName);

            if (attribute != null)
            {
               value = attribute.Value;
            }
         }

         return value;
      }

      public static void SetAttributeValue(XElement element, string name, string value)
      {
         if (element != null)
         {
            if (name != "")
            {
               element.SetAttributeValue(name, value);
            }
         }
      }

      #region Disk Tools

      /// <summary>
      /// Gets the Serial Number of a specific drive letter.
      /// </summary>
      /// <param name="volumeLetter">The volume drive letter without colons.</param>
      /// <returns>The volume serial number as a string or null in case of failure.</returns>
      public static string GetVolumeSerialNumber(string volumeLetter)
      {
         string volumeSerialNumber = null;

         /* TODO: Implement a cross-platform serial number support.
         // Make sure a drive letter has been supplied.
         if (volumeLetter == "" || volumeLetter == null)
         {
            return volumeSerialNumber;
         }

         try
         {
            ManagementObject disk = new ManagementObject("Win32_LogicalDisk.DeviceID=\"" + volumeLetter + ":\"");

            disk.Get();

            volumeSerialNumber = disk["VolumeSerialNumber"].ToString();
         }
         catch (ManagementException)
         {
            Console.WriteLine("Failed to retrieve the VolumeSerialNumber of {0}:", volumeLetter);
         }
         */

         return volumeSerialNumber;
      }

      /// <summary>
      /// Gets the label of a specific drive letter.
      /// </summary>
      /// <param name="volumeLetter">The volume drive letter without colons.</param>
      /// <returns>The volume label as a string or null in case of failure.</returns>
      public static string GetVolumeLabel(string volumeLetter)
      {
         string volumeLabel = null;

         /* TODO: Implement a cross-platform volume label name support. 
         // Make sure a drive letter has been supplied.
         if (volumeLetter == "" || volumeLetter == null)
         {
            return volumeLabel;
         }

         try
         {
            ManagementObject disk = new ManagementObject("Win32_LogicalDisk.DeviceID=\"" + volumeLetter + ":\"");

            disk.Get();

            volumeLabel = disk["VolumeName"].ToString();
         }
         catch (ManagementException)
         {
            Console.WriteLine("Failed to retrieve the VolumeName of {0}:", volumeLetter);
         }
         */

         return volumeLabel;
      }

      #endregion // Disk Tools
   }
}
