using System;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaCurator
{
   class MediaServer : MediaContainer
   {
      #region Fields

      private IConfiguration _configuration { get; }

      #endregion // Fields

      #region Constructors

      public MediaServer(IConfiguration configuration, string path)
         : base(configuration, MediaContainer.GetPathComponents(path).Item1)
      {
         _configuration = configuration;

         // The base class constructor will take care of the parents and below we'll take care of
         // the element itself.

         if (Parent == null)
         {
            // Extract the Server Name from the supplied path removing the \ characters. 
            string name = MediaContainer.GetPathComponents(path).Item2.Trim(new Char[] { '\\' });

            // Retrieve the element if it already exists.
            Self = Tools.GetElementByNameAttribute(MediaDatabase.Document.Root, "Server", name);

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
                  new XElement("Server",
                     new XAttribute("Name", name)));

               // Retrieve the newly created element.
               Self = Tools.GetElementByNameAttribute(MediaDatabase.Document.Root, "Server", name);

               // Make sure that we succeeded to put our hands on it.
               if (Self == null)
               {
                  throw new Exception("Failed to add the new Server element to the MediaDatabase.");
               }
            }

            // Initialize the flags.
            Flags = new MediaContainerFlags(Self);
         }

         // TODO: Set the Thumbnail.
         // Thumbnail = new MediaContainerThumbnail("pack://application:,,,/Icons/256x144/Server.png");
      }

      public MediaServer(IConfiguration configuration, XElement element, bool update = false)
         : base(configuration, element, update)
      {
         _configuration = configuration;

         // TODO: Set the Thumbnail.
         // Thumbnail = new MediaContainerThumbnail("pack://application:,,,/Icons/256x144/Server.png");
      }

      #endregion // Constructors
   }
}