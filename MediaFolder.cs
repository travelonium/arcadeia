using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        public MediaFolder(string path)
           : base(MediaContainer.GetPathComponents(path).Item1)
        {
            // The base class constructor will take care of the parents and below we'll take care of
            // the element itself.

            if (Parent != null)
            {
                // Extract the Folder Name from the supplied path removing the \ characters. 
                string name = MediaContainer.GetPathComponents(path).Item2.Trim(new Char[] { '\\' });

                // Retrieve the element if it already exists.
                Self = Tools.GetElementByNameAttribute(Parent.Self, "Folder", name);

                // Did we find an already existing element?
                if (Self != null)
                {
                    // We found the element. We may want to make sure it has not been modified since the
                    // last time the MediaDatabase was updated and update it if needed.
                }
                else
                {
                    // Looks like there is no such element! Let's create one then!
                    Parent.Self.Add(
                       new XElement("Folder",
                          new XAttribute("Name", name)));

                    // Retrieve the newly created element.
                    Self = Tools.GetElementByNameAttribute(Parent.Self, "Folder", name);

                    // Make sure that we succeeded to put our hands on it.
                    if (Self == null)
                    {
                        throw new Exception("Failed to add the new Folder element to the MediaDatabase.");
                    }
                }

                // Initialize the flags.
                Flags = new MediaContainerFlags(Self);
            }

            // Set the Thumbnail.
            Thumbnail = new MediaContainerThumbnail("pack://application:,,,/Icons/256x144/Folder.png");
        }

        public MediaFolder(XElement element, bool update = false)
           : base(element, update)
        {
            // Set the Thumbnail.
            Thumbnail = new MediaContainerThumbnail("pack://application:,,,/Icons/256x144/Folder.png");
        }

        #endregion // Constructors
    }
}