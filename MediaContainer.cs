using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaCurator
{
   public class MediaContainer : IMediaContainer
   {
      protected readonly IConfiguration _configuration;

      protected readonly IThumbnailsDatabase _thumbnailsDatabase;

      protected readonly IMediaLibrary _mediaLibrary;

      #region Fields

      /// <summary>
      /// Gets the root MediaContainer of this particular MediaContainer descendant.
      /// </summary>
      /// <value>The root MediaContainer of this particular MediaContainer descendant.</value>
      public IMediaContainer Root
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

      private IMediaContainer _parent = null;

      /// <summary>
      /// The parent MediaContainer based type of this instance.
      /// </summary>
      public IMediaContainer Parent
      {
         get => _parent;
         set => _parent = value;
      }

      private XElement _self = null;

      /// <summary>
      /// The XML Element this particular container is associated with in the MediaLibrary. This
      /// is either created or located in the MediaLibrary at the time of initialization.
      /// </summary>
      public XElement Self
      {
         get => _self;
         set => _self = value;
      }

      /// <summary>
      /// Gets or sets the Id of the file. The Id is used to locate the thumbnails directory of the 
      /// file, to uniquely locate and identify each file tag and as the unique identifier in the
      /// SolR index. This is directly read and written from and to the MediaLibrary.
      /// </summary>
      /// <value>
      /// The unique identifier of the container element.
      /// </value>
      public string Id
      {
         get
         {
            return Tools.GetAttributeValue(Self, "Id");
         }

         set
         {
            Tools.SetAttributeValue(Self, "Id", value);
         }
      }

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
            IMediaContainer container = this;

            do
            {
               switch (container.Type)
               {
                  case "Library":
                     path = Platform.Separator.Root + path;
                     break;

                  case "Drive":
                     path = container.Name + ":\\" + path;
                     break;

                  case "Server":
                     path = "\\\\" + container.Name + "\\" + path;
                     break;

                  case "Folder":
                     path = container.Name + Platform.Separator.Path + path;
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
      public virtual Models.MediaContainer Model
      {
         get
         {
            return new Models.MediaContainer
            {
               Name = Name,
               Type = Type,
               FullPath = FullPath,
               Flags = Flags.All.Select(flag => Enum.GetName(typeof(MediaContainerFlags.Flag), flag)).ToArray()
            };
         }

         set
         {
            if (!String.IsNullOrEmpty(value.Name) && (value.Name != Name))
            {
               // TODO: Implement Rename() and use it here.
            }

            if (!String.IsNullOrEmpty(value.FullPath) && (value.FullPath != FullPath))
            {
               // TODO: Implement Move() and use it here.
            }

            if (value.Flags != null)
            {
               Flags.SetFlags(value.Flags.Where(flag => Enum.GetNames(typeof(MediaContainerFlags.Flag)).Contains(flag))
                                         .Select(flag => Enum.Parse<MediaContainerFlags.Flag>(flag))
                                         .ToArray());
            }
         }
      }

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

      public MediaContainer(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, string path)
      {
         _configuration = configuration;
         _thumbnailsDatabase = thumbnailsDatabase;
         _mediaLibrary = mediaLibrary ?? (IMediaLibrary) this;

         if (path == null)
         {
            // This can happen on two occassions:
            // 1. The MediaLibrary instance is being initialized. On this occassion, the mediaLibrary
            //    instance is null and so we don't need to do anything else.
            // 2. The MediaLibrary instance has already been initialized. This happens when the MediaContainer
            //    being initialized is not a MediaLibrary but is one of the direct children of it. Here
            //    we set its parent to the MediaLibrary itself.

            if (mediaLibrary != null)
            {
               Parent = mediaLibrary;
            }

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
               Parent = (MediaContainer)Activator.CreateInstance(parentType, _configuration, _thumbnailsDatabase, _mediaLibrary, path);
            }
         }
         else
         {
            // The path supplied refers to the root of the MediaLibrary i.e. the "/". We simply set
            // the parent to the MediaLibrary instance itself.

            Parent = _mediaLibrary;
         }

         // Now that we're here, we can assume that the parent(s) of this element has been created
         // or better yet located already. Now the constructor of the inherited calling class will
         // take care of the element itself. Unless the path points to a file in which case, the
         // Parent of this instance will be the real target.
      }

      public MediaContainer(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, XElement element, bool update = false)
      {
         _configuration = configuration;
         _thumbnailsDatabase = thumbnailsDatabase;
         _mediaLibrary = mediaLibrary;

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
                  if (parentType == typeof(MediaLibrary))
                  {
                     Parent = _mediaLibrary;
                  }
                  else
                  {
                     // Now the Parent will be taken care of!
                     Parent = (MediaContainer)Activator.CreateInstance(parentType, _configuration, _thumbnailsDatabase, _mediaLibrary, element.Parent, update);
                  }
               }
            }

            if (update)
            {
               if (String.IsNullOrEmpty(Id))
               {
                  // Generate a unique Id which can be used to create a unique link to each element and
                  // also in creation of a unique folder for each file containing its thumbnails.
                  // This takes care of the existing elements created by previous versions that lack
                  // an Id attribute.
                  Id = System.IO.Path.GetRandomFileName();
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

            // It's probably a file then, let's find its type.
            var supportedExtensions = new MediaContainerTypeExtensions(_configuration);

            // Extract the file extension including the '.' character.
            string extension = Path.GetExtension(container).ToLower();

            if ((extension != null) && (extension.Length != 0))
            {
               // It appears that the file does have an extension. We may proceed.

               // Check if it's a recognized video format.
               if (supportedExtensions[MediaContainerType.Video].Contains(extension))
               {
                  // Looks like the file is a recognized video format.
                  return typeof(VideoFile);
               }

               // Check if it's a recognized photo format.
               if (supportedExtensions[MediaContainerType.Photo].Contains(extension))
               {
                  // Looks like the file is a recognized photo format.
                  throw new NotImplementedException("Photo files cannot yet be handled!");
               }

               // Check if it's a recognized audio format.
               if (supportedExtensions[MediaContainerType.Audio].Contains(extension))
               {
                  // Looks like the file is a recognized audio format.
                  throw new NotImplementedException("Audio files cannot yet be handled!");
               }
            }

            throw new NotSupportedException("The supplied child is of an invalid type!");
         }

         return childType;
      }

      private Type GetMediaContainerType(XElement container)
      {
         Type childType = null;

         if (container != null)
         {
            if (container.Name.ToString().Equals("Library"))
            {
               // It's the library itself.
               return typeof(MediaLibrary);
            }
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

            if (container.Name.ToString().Equals("Library"))
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
