using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MediaCurator
{
    class MediaServer : MediaContainer
    {
        #region Fields

        #endregion // Fields

        #region Constructors

        public MediaServer(string path)
           : base(MediaContainer.GetPathComponents(path).Item1)
        {
            // The base class constructor will take care of the parents and below we'll take care of
            // the element itself.

            if (Parent == null)
            {
                // Extract the Server Name from the supplied path removing the \ characters. 
                string name = MediaContainer.GetPathComponents(path).Item2.Trim(new Char[] { '\\' });

                // Retrieve the element if it already exists.
                Self = Tools.GetElementByNameAttribute(MediaLibrary.MediaDatabase.Root, "Server", name);

                // Did we find an already existing element?
                if (Self != null)
                {
                    // We found the element. We may want to make sure it has not been modified since the
                    // last time the MediaDatabase was updated and update it if needed.
                }
                else
                {
                    // Looks like there is no such element! Let's create one then!
                    MediaLibrary.MediaDatabase.Root.Add(
                       new XElement("Server",
                          new XAttribute("Name", name)));

                    // Retrieve the newly created element.
                    Self = Tools.GetElementByNameAttribute(MediaLibrary.MediaDatabase.Root,
                                                            "Server",
                                                            name);

                    // Make sure that we succeeded to put our hands on it.
                    if (Self == null)
                    {
                        throw new Exception("Failed to add the new Server element to the MediaDatabase.");
                    }
                }

                // Initialize the flags.
                Flags = new MediaContainerFlags(Self);
            }

            // Set the Thumbnail.
            Thumbnail = new MediaContainerThumbnail("pack://application:,,,/Icons/256x144/Server.png");
        }

        public MediaServer(XElement element, bool update = false)
           : base(element, update)
        {
            // Set the Thumbnail.
            Thumbnail = new MediaContainerThumbnail("pack://application:,,,/Icons/256x144/Server.png");
        }

        #endregion // Constructors
    }
}