using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MediaCurator
{
   class MediaDrive : MediaContainer
   {
      #region Fields

      public string SerialNumber
      {
         get
         {
            return Tools.GetAttributeValue(Self, "SerialNumber");
         }

         set
         {
            Tools.SetAttributeValue(Self, "SerialNumber", value);
         }
      }

      #endregion // Fields

      #region Constructors

      public MediaDrive(IConfiguration configuration, string path)
         : base(configuration, MediaContainer.GetPathComponents(path).Item1)
      {
         // The base class constructor will take care of the parents and below we'll take care of
         // the element itself.

         if (Parent == null)
         {
            // Extract the Drive Name from the supplied path removing the \ and : characters. 
            string name = MediaContainer.GetPathComponents(path).Item2.Trim(new Char[] { '\\', ':' });

            // Retrieve the Serial Number of the volume if one was available.
            string serialNumber = Tools.GetVolumeSerialNumber(name);

            if (serialNumber == null)
            {
               throw new Exception("Failed to retrieve the serial number of the " + name + ": drive!");
            }

            // Retrieve all the elements with the same Name attribute.
            IEnumerable<XElement> candidates = Tools.GetElementsByNameAttribute(MediaDatabase.Document.Root, "Drive", name);

            if (candidates != null)
            {
               foreach (var item in candidates)
               {
                  if (Tools.GetAttributeValue(item, "SerialNumber") == serialNumber)
                  {
                     // We found an identical element with a matching Serial Number.
                     Self = item;

                     break;
                  }
               }
            }

            // Did we find an already existing element?
            if (Self != null)
            {
               // We found the element. We may want to make sure it has not been modified since the
               // last time the MediaDatabase was updated and update it if needed.
            }
            else
            {
               // Looks like there is no such element! Let's create one then!
               MediaDatabase.Document.Root.Add(
                  new XElement("Drive",
                     new XAttribute("Name", name),
                     new XAttribute("SerialNumber", serialNumber)));

               // Retrieve the newly created element.
               Self = MediaDatabase.Document.Root.Elements().Last();

               // Make sure that we succeeded to put our hands on it.
               if ((Self == null) || (Name != name) || (SerialNumber != serialNumber))
               {
                  throw new Exception("Failed to add the new Drive element to the MediaDatabase.");
               }
            }

            // Initialize the flags.
            Flags = new MediaContainerFlags(Self);
         }
      }

      public MediaDrive(IConfiguration configuration, XElement element, bool update = false)
          : base(configuration, element, update)
      {
      }

      #endregion // Constructors
   }
}