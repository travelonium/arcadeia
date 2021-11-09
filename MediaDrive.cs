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

      public MediaDrive(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, string path)
         : base(configuration, thumbnailsDatabase, mediaLibrary, MediaContainer.GetPathComponents(path).Item1)
      {
         // The base class constructor will take care of the parents and below we'll take care of
         // the element itself.

         if (Parent == null)
         {
            // Generate a unique Id which can be used to create a unique link to each element.
            string id = System.IO.Path.GetRandomFileName();

            // Extract the Drive Name from the supplied path removing the \ and : characters. 
            string name = MediaContainer.GetPathComponents(path).Item2.Trim(new Char[] { '\\', ':' });

            // Retrieve the Serial Number of the volume if one was available.
            string serialNumber = Tools.GetVolumeSerialNumber(name);

            if (serialNumber == null)
            {
               throw new Exception("Failed to retrieve the serial number of the " + name + ": drive!");
            }

            // Retrieve all the elements with the same Name attribute.
            IEnumerable<XElement> candidates = Tools.GetElementsByNameAttribute(_mediaLibrary.Self, "Drive", name);

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
               // last time the MediaLibrary was updated and update it if needed.
            }
            else
            {
               // Looks like there is no such element! Let's create one then!
               _mediaLibrary.Self.Add(
                  new XElement("Drive",
                     new XAttribute("Id", id),
                     new XAttribute("Name", name),
                     new XAttribute("SerialNumber", serialNumber)));

               // Retrieve the newly created element.
               Self = _mediaLibrary.Self.Elements().Last();

               // Make sure that we succeeded to put our hands on it.
               if ((Self == null) || (Name != name) || (SerialNumber != serialNumber))
               {
                  throw new Exception("Failed to add the new Drive element to the MediaLibrary.");
               }
            }

            // Initialize the flags.
            Flags = new MediaContainerFlags(Self);
         }
      }

      public MediaDrive(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, XElement element, bool update = false)
          : base(configuration, thumbnailsDatabase, mediaLibrary, element, update)
      {
      }

      #endregion // Constructors
   }
}