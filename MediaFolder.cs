using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaCurator
{
   class MediaFolder : MediaContainer
   {
      #region Fields

      /// <summary>
      /// Gets the tooltip text of this foilder.
      /// </summary>
      public override string ToolTip
      {
         get
         {
            string tooltip = "";

            tooltip += String.Format("{0}", Name);

            /*
             * TODO: Implement a Size Field which would calculate the size of the folder.
             *
            if( Size < 0x400UL )                // < 1KB
            {
               tooltip += String.Format( "\nSize: {0}B", Size );
            }
            else if( Size < 0x100000UL )        // < 1MB
            {
               tooltip += String.Format( "\nSize: {0:F2}KB", (double)Size / (double)0x400UL );
            }
            else if( Size < 0x40000000UL )      // < 1GB
            {
               tooltip += String.Format( "\nSize: {0:F2}MB", (double)Size / (double)0x100000UL );
            }
            else if( Size < 0x10000000000UL )   // < 1TB
            {
               tooltip += String.Format( "\nSize: {0:F2}GB", ( (double)Size / (double)0x40000000UL ) );
            }
            */

            return tooltip;
         }
      }

      #endregion // Fields

      #region Constructors

      public MediaFolder(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, string path)
         : base(configuration, thumbnailsDatabase, mediaLibrary, MediaContainer.GetPathComponents(path).Item1)
      {
         // The base class constructor will take care of the parents and below we'll take care of
         // the element itself.

         // Generate a unique Id which can be used to create a unique link to each element.
         string id = System.IO.Path.GetRandomFileName();

         // Extract the Folder Name from the supplied path removing the \ and / characters.
         string name = MediaContainer.GetPathComponents(path).Item2.Trim(new Char[] { '\\', '/' });

         if (Parent == null)
         {
            // Retrieve the element if it already exists.
            Self = Tools.GetElementByNameAttribute(_mediaLibrary.Self, "Folder", name);

            // This folder resides in the Linux/OSX root filesystem.
            if (Self != null)
            {
               // We found the element. We may want to make sure it has not been modified since the
               // last time the MediaLibrary was updated and update it if needed.
            }
            else
            {
               // Looks like there is no such element! Let's create one then!
               _mediaLibrary.Self.Add(
                  new XElement("Folder",
                     new XAttribute("Id", id),
                     new XAttribute("Name", name)));

               // Retrieve the newly created element.
               Self = _mediaLibrary.Self.Elements().Last();

               // Make sure that we succeeded to put our hands on it.
               if ((Self == null) || (Name != name))
               {
                  throw new Exception("Failed to add the new Folder element to the MediaLibrary.");
               }
            }
         }
         else
         {
            // Retrieve the element if it already exists.
            Self = Tools.GetElementByNameAttribute(Parent.Self, "Folder", name);

            if (Self != null)
            {
               // We found the element. We may want to make sure it has not been modified since the
               // last time the MediaLibrary was updated and update it if needed.
            }
            else
            {
               // Looks like there is no such element! Let's create one then!
               Parent.Self.Add(
                  new XElement("Folder",
                     new XAttribute("Id", id),
                     new XAttribute("Name", name)));

               // Retrieve the newly created element.
               Self = Tools.GetElementByNameAttribute(Parent.Self, "Folder", name);

               // Make sure that we succeeded to put our hands on it.
               if (Self == null)
               {
                  throw new Exception("Failed to add the new Folder element to the MediaLibrary.");
               }
            }
         }

         // Initialize the flags.
         Flags = new MediaContainerFlags(Self);
      }

      public MediaFolder(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, XElement element, bool update = false)
         : base(configuration, thumbnailsDatabase, mediaLibrary, element, update)
      {
      }

      #endregion // Constructors

      #region Overrides

      /// <summary>
      /// Checks whether or not this directory has physical existence.
      /// </summary>
      /// <returns>
      ///   <c>true</c> if the directory physically exists and <c>false</c> otherwise.
      /// </returns>
      public override bool Exists()
      {
         return Directory.Exists(FullPath);
      }

      #endregion
   }
}