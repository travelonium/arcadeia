using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaCurator
{
   public class MediaContainer
   {
      #region Fields

      protected readonly IConfiguration _configuration;

      /// <summary>
      /// Gets the root MediaContainer of this particular MediaContainer descendant.
      /// </summary>
      /// <value>The root MediaContainer of this particular MediaContainer descendant.</value>
      public MediaContainer Root
      {
         get
         {
            if (Parent == null)
            {
               return this;
            }
            else
            {
               return Parent.Root;
            }
         }
      }

      /// <summary>
      /// The parent MediaContainer based type of this instance.
      /// </summary>
      public MediaContainer Parent = null;

      /// <summary>
      /// The XML Element this particular container is associated with in the MediaLibrary. This
      /// is either created or located in the MediaLibrary at the time of initialization.
      /// </summary>
      public XElement Self = null;

      /// <summary>
      /// Gets or sets the Name attribute of the container element. This is directly read or written
      /// from and to the MediaLibrary.
      /// </summary>
      /// <value>
      /// The Name attribute of the container element.
      /// </value>
      public string Name
      {
         get
         {
            return Tools.GetAttributeValue(Self, "Name");
         }

         set
         {
            Tools.SetAttributeValue(Self, "Name", value);
         }
      }

      /// <summary>
      /// Gets the type of the MediaContainer. This is directly extracted from the XML tag Name
      /// associated with this container.
      /// </summary>
      /// <value>
      /// The type in string format.
      /// </value>
      public string Type
      {
         get
         {
            return Self.Name.ToString();
         }
      }

      /// <summary>
      /// Gets the full path of the container including the filename if the container is of a
      /// MediaFile type.
      /// </summary>
      /// <value>
      /// The full path of the container.
      /// </value>
      public string FullPath
      {
         get
         {
            string path = null;
            MediaContainer container = this;

            do
            {
               switch (container.Type)
               {
                  case "Drive":
                     path = container.Name + ":\\" + path;
                     break;

                  case "Server":
                     path = "\\\\" + container.Name + "\\" + path;
                     break;

                  case "Folder":
                     path = (container.Parent == null ? Platform.Separator.Path : "") + container.Name + Platform.Separator.Path + path;
                     break;

                  case "Audio":
                     path += container.Name;
                     break;

                  case "Video":
                     path += container.Name;
                     break;

                  case "Photo":
                     path += container.Name;
                     break;
               }
            }
            while ((container = container.Parent) != null);

            return path;
         }
      }

      /// <summary>
      /// Gets or sets the flags attribute of the media container.
      /// </summary>
      /// <value>
      /// The flags attribute value. The individual flags can be accessed using their respective name.
      /// </value>
      public MediaContainerFlags Flags
      {
         get;
         set;
      }

      /// <summary>
      /// Gets the MediaContainer model used when returning a JSON response from a Controller.
      /// </summary>
      public virtual Models.MediaContainer Model => new Models.MediaContainer
      {
         Name = Name,
         Type = Type,
         FullPath = FullPath,
      };

      /// <summary>
      /// Gets the tooltip text of this media container. Must be overridden in the child type.
      /// </summary>
      public virtual string ToolTip
      {
         get
         {
            throw new NotImplementedException("This MediaContainer does not offer ToolTips!");
         }
      }

      #endregion // Fields

      #region Constructors

      public MediaContainer(IConfiguration configuration, string path)
      {
         _configuration = configuration;

         if (path == null)
         {
            // Reached the bottom! Return for the moment. But later, perhaps the Parent could be set
            // to the MediaLibrary itself!
            return;
         }

         // Split the path in parent, child components.
         Tuple<string, string> pathTuple = GetPathComponents(path);

         if (pathTuple.Item2 != null)
         {
            // There is yet another parent to take care of before attending to the child! First try
            // to infer the parent type.
            Type parentType = GetMediaContainerType(pathTuple.Item2);

            // Now make sure a type was successfully deduced.
            if (parentType != null)
            {
               // Now the Parent will be taken care of!
               Parent = (MediaContainer)Activator.CreateInstance(parentType, configuration, path);
            }
         }

         // Now that we're here, we can assume that the parent(s) of this element has been created
         // or better yet located already. Now the constructor of the inherited calling class will
         // take care of the element itself.
      }

      public MediaContainer(IConfiguration configuration, XElement element, bool update = false)
      {
         _configuration = configuration;

         if (element != null)
         {
            Self = element;

            if (element.Parent != null)
            {
               // There is yet another parent to take care of. First try to infer the parent type.
               Type parentType = GetMediaContainerType(element.Parent);

               // Now make sure a type was successfully deduced.
               if (parentType != null)
               {
                  // Now the Parent will be taken care of!
                  Parent = (MediaContainer)Activator.CreateInstance(parentType, configuration, element.Parent, update);
               }
            }

            // Initialize the flags.
            Flags = new MediaContainerFlags(Self);
         }
      }

      #endregion // Constructors

      #region Public Methods

      /// <summary>
      /// Returns the type of the current MediaContainer.
      /// </summary>
      /// <returns>Returns a value defined in <typeparamref name="MediaContainerType"/>.</returns>
      public MediaContainerType GetMediaContainerType()
      {
         switch (Type)
         {
            case "Drive":
               return MediaContainerType.Drive;

            case "Server":
               return MediaContainerType.Server;

            case "Folder":
               return MediaContainerType.Folder;

            case "Audio":
               return MediaContainerType.Audio;

            case "Video":
               return MediaContainerType.Video;

            case "Photo":
               return MediaContainerType.Photo;

            default:
               return MediaContainerType.Unknown;
         }
      }

      #endregion // Public Methods

      #region Overridables

      /// <summary>
      /// Checks whether or not this MediaContainer has physical existence. It shall be overridden
      /// for each specific type if necessary.
      /// </summary>
      /// <returns><c>true</c> if the MediaContainer physically exists and <c>false</c> otherwise.
      /// </returns>
      /// <exception cref="NotImplementedException">If used for a type but not implemented for it.
      /// </exception>
      public virtual bool Exists()
      {
         throw new NotImplementedException("This MediaContainer does not offer a Exists() method!");
      }

      /// <summary>
      /// Deletes the MediaContainer from the disk and the MediaLibrary. Each MediaContainer type has
      /// to override and implement its own delete method.
      /// </summary>
      /// <param name="permanent">if set to <c>true</c>, it permanently deletes the file from the disk
      /// and otherwise moves it to the recycle bin.</param>
      /// <param name="deleteXmlEntry">if set to <c>true</c> the node itself in the MediaLibrary is also
      /// deleted.</param>
      /// <exception cref="NotImplementedException">This MediaContainer does not offer a Delete() method!</exception>
      public virtual void Delete(bool permanent = false, bool deleteXmlEntry = false)
      {
         throw new NotImplementedException("This MediaContainer does not offer a Delete() method!");
      }

      #endregion // Overridables

      #region Static Methods

      /// <summary>
      /// Splits the path supplied into two parts. The second part is the name of the current file,
      /// folder or drive/server and the first part is the path leading to it. In certain cases like
      /// when the supplied path has only one level, the parent will be null.
      ///
      /// -----------------------------------------------------------------------------------------
      /// |                                       WINDOWS                                         |
      /// -----------------------------------------------------------------------------------------
      /// | Path                            ->     Parent               +     Child               |
      /// -----------------------------------------------------------------------------------------
      /// | C:\                             ->     null                 +     C:\                 |
      /// | C:\File.ext                     ->     C:\                  +     File.ext            |
      /// | \\Server1\                      ->     null                 +     \\Server1\          |
      /// | \\Server1\File.ext              ->     \\Server1\           +     File.ext            |
      /// | C:\Folder1\Folder2\File.ext     ->     C:\Folder1\Folder2\  +     File.ext            |
      /// | C:\Folder1\                     ->     C:\                  +     Folder1\            |
      /// -----------------------------------------------------------------------------------------
      ///
      /// -----------------------------------------------------------------------------------------
      /// |                                      LINUX/OSX                                        |
      /// -----------------------------------------------------------------------------------------
      /// | Path                            ->     Parent               +     Child               |
      /// -----------------------------------------------------------------------------------------
      /// | /File.ext                       ->     null                 +     File.ext            |
      /// | /Folder1/                       ->     null                 +     Folder1/            |
      /// | /Folder1/Folder2/File.ext       ->     /Folder1/Folder2/    +     File.ext            |
      /// -----------------------------------------------------------------------------------------
      ///
      /// </summary>
      /// <param name="path">The path supplied.</param>
      /// <returns>A Tuple<string,string> in which the first part is the parent and the second item
      /// is the child or in other words the current level.
      /// </returns>

      public static Tuple<string, string> GetPathComponents(string path)
      {
         string parent = null;
         string child = null;

         for (int i = path.Length - 1; i >= 0; i--)
         {
            if (path[i].Equals('\\') || path[i].Equals('/'))
            {
               if (i == (path.Length - 1))
               {
                  if ((i == 2) && (path[i - 1].Equals(':')))
                  {
                     // It appears to be a drive letter. The parent will be null.
                     child = path.Substring(0, 3);
                     break;
                  }

                  // Keep looking for the next backslash.
                  continue;
               }
               else if ((i == 1) && (path[i - 1].Equals('\\')))
               {
                  // It appears to be a server name. The parent will be null.
                  child = path.Substring(0, path.Length);
                  break;
               }
               else if (i == 0)
               {
                  // This must be the leading slash in a Linux path name. The parent will be null.
                  child = path.Substring(1, path.Length - 1);
                  break;
               }
               else
               {
                  // It appears to either be a file name or a folder name.
                  child = path.Substring(i + 1, path.Length - (i + 1));
                  parent = path.Substring(0, i + 1);
                  break;
               }
            }
         }

         return new Tuple<string, string>(parent, child);
      }

      #endregion // Static Methods

      #region Private Methods

      private Type GetMediaContainerType(string container)
      {
         Type childType = null;

         if ((container != null) && (container.Length > 1))
         {
            if ((container.Length >= 2) &&
                (container.Count(c => c == ':') == 1) &&
                (container[1] == ':'))
            {
               // It's a drive name.
               return typeof(MediaDrive);
            }

            if ((container.Length > 2) &&
                (container.Count(c => c == '\\') >= 2) &&
                (container[0] == '\\') &&
                (container[1] == '\\'))
            {
               // It's a server name.
               return typeof(MediaServer);
            }

            if ((container.Length > 1) &&
                (container.Count(c => (c == '\\') || (c == '/')) == 1) &&
                ((container[container.Length - 1] == '\\') || (container[container.Length - 1] == '/')))
            {
               // It's a folder.
               return typeof(MediaFolder);
            }

            throw new NotSupportedException("The supplied child is of an invalid type! This " +
                                            "method is not meant to handle files.");
         }

         return childType;
      }

      private Type GetMediaContainerType(XElement container)
      {
         Type childType = null;

         if (container != null)
         {
            if (container.Name.ToString().Equals("Drive"))
            {
               // It's a drive name.
               return typeof(MediaDrive);
            }

            if (container.Name.ToString().Equals("Server"))
            {
               // It's a server name.
               return typeof(MediaServer);
            }

            if (container.Name.ToString().Equals("Folder"))
            {
               // It's a folder.
               return typeof(MediaFolder);
            }

            if (container.Name.ToString().Equals("Video"))
            {
               // It's a video file.
               return typeof(VideoFile);
            }

            if (container.Name.ToString().Equals("Audio"))
            {
               // It's an audio file.
               throw new NotImplementedException("Audio files cannot yet be handled!");
            }

            if (container.Name.ToString().Equals("Photo"))
            {
               // It's a photo file.
               throw new NotImplementedException("Photo files cannot yet be handled!");
            }

            if (container.Name.ToString().Equals("MediaLibrary"))
            {
               // It's the MediaLibrary itself. We hit the bottom.
               return null;
            }

            throw new NotSupportedException("The supplied child is of an invalid type! This " +
                                            "method is not meant to handle files.");
         }

         return childType;
      }

      #endregion // Private Methods
   }
}
