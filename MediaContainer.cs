using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediaCurator.Solr;
using System.Globalization;

namespace MediaCurator
{
   public class MediaContainer : IMediaContainer
   {
      protected readonly IServiceProvider Services;

      protected readonly IConfiguration Configuration;

      protected readonly ILogger<MediaContainer> Logger;

      protected readonly IThumbnailsDatabase ThumbnailsDatabase;

      protected readonly IMediaLibrary MediaLibrary;

      private bool _disposed;

      #region Fields

      /// <summary>
      /// A flag indicating that the MediaContainer based entry did not exist and will be created.
      /// This will inform the destructor that it needs to add the entry to the index.
      /// </summary>
      public bool Created = false;

      /// <summary>
      /// A flag indicating that the MediaContainer based entry has been modified and will be updated.
      /// This will inform the destructor that it needs to updated the entry in the index.
      /// </summary>
      public bool Modified = false;

      /// <summary>
      /// A flag indicating that the MediaContainer based entity has been deleted from the disk.
      /// This will inform the destructor that it needs to remove the entry from the index.
      /// </summary>
      public bool Deleted = false;

      /// <summary>
      /// A flag indicating that the MediaContainer based entity is not to be processed and saved to
      /// the index but should be skipped.
      /// </summary>
      public bool Skipped = false;

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
         set
         {
            if (_parent != value)
            {
               Modified = (_parent != null);

               _parent = value;
            }
         }
      }

      private string _parentType = null;

      /// <summary>
      /// Type of the parent MediaContainer based type of this instance.
      /// </summary>
      public string ParentType
      {
         get => _parentType;
         set
         {
            if (_parentType != value)
            {
               Modified = (_parentType != null);

               _parentType = value;
            }
         }
      }

      private string _id = null;

      /// <summary>
      /// Gets or sets the "id" of the MediaContainer. The Id is used to located the SolR index entry
      /// and also the the thumbnails database entry of the MediaContainer.
      /// </summary>
      /// <value>
      /// The unique identifier of the container entry.
      /// </value>
      public string Id
      {
         get => _id;

         set
         {
            if (_id != value)
            {
               Modified = (_id != null);

               _id = value;
            }
         }
      }

      private string _name = null;

      /// <summary>
      /// Gets or sets the "name" attribute of the container entry. This is for example the folder,
      /// file or server name and extension if any.
      /// </summary>
      /// <value>
      /// The "name" attribute of the container entry.
      /// </value>
      public string Name
      {
         get => _name;

         set
         {
            if (_name != value)
            {
               Modified = (_name != null);

               _name = value;
            }
         }
      }

      private string _description = null;

      /// <summary>
      /// Gets or sets the "description" attribute of the container entry.
      /// </summary>
      /// <value>
      /// The "description" attribute of the container entry.
      /// </value>
      public string Description
      {
         get => _description;

         set
         {
            if (_description != value)
            {
               Modified = (_description != null);

               _description = value;
            }
         }
      }

      private string _type = null;

      /// <summary>
      /// Gets or sets the "type" attribute of the container entry.
      /// </summary>
      /// <value>
      /// The "type" attribute of the container entry.
      /// </value>
      public string Type
      {
         get => _type;

         set
         {
            if (_type != value)
            {
               Modified = (_type != null);

               _type = value;
            }
         }
      }

      /// <summary>
      /// Get the path the contianer is located in which is the parent's full path.
      /// </summary>
      /// <value>
      /// The path of the container.
      /// </value>
      public string Path
      {
         get
         {
            if (Parent == null)
            {
               return null;
            }
            else
            {
               return Parent.FullPath;
            }
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

      protected string _dateAdded = null;

      /// <summary>
      /// Gets or sets the date the container was added to the MediaLibrary.
      /// </summary>
      /// <value>
      /// The addition date in DateTime.
      /// </value>
      public DateTime DateAdded
      {
         get => DateTime.SpecifyKind(Convert.ToDateTime(_dateAdded, CultureInfo.InvariantCulture), DateTimeKind.Utc);

         set
         {
            TimeSpan difference = value - DateAdded;
            if (difference >= TimeSpan.FromSeconds(1))
            {
               Modified = (_dateAdded != null);

               _dateAdded = value.ToString(CultureInfo.InvariantCulture);
            }
         }
      }

      protected string _dateCreated = null;

      /// <summary>
      /// Gets or sets the creation date of the file.
      /// </summary>
      /// <value>
      /// The creation date in DateTime.
      /// </value>
      public DateTime DateCreated
      {
         get => DateTime.SpecifyKind(Convert.ToDateTime(_dateCreated, CultureInfo.InvariantCulture), DateTimeKind.Utc);

         set
         {
            TimeSpan difference = value - DateCreated;
            if (difference >= TimeSpan.FromSeconds(1))
            {
               Modified = (_dateCreated != null);

               _dateCreated = value.ToString(CultureInfo.InvariantCulture);
            }
         }
      }

      protected string _dateModified = null;

      /// <summary>
      /// Gets or sets the last modification date of the file.
      /// </summary>
      /// <value>
      /// The last modification date in DateTime.
      /// </value>
      public DateTime DateModified
      {
         get => DateTime.SpecifyKind(Convert.ToDateTime(_dateModified, CultureInfo.InvariantCulture), DateTimeKind.Utc);

         set
         {
            TimeSpan difference = value - DateModified;
            if (difference >= TimeSpan.FromSeconds(1))
            {
               Modified = (_dateModified != null);

               _dateModified = value.ToString(CultureInfo.InvariantCulture);
            }
         }
      }

      private MediaContainerFlags _flags = null;

      /// <summary>
      /// Gets or sets the "flags" attribute of the media container entry.
      /// </summary>
      /// <value>
      /// The flags attribute value. The individual flags can be accessed using their respective name.
      /// </value>
      public MediaContainerFlags Flags
      {
         get => _flags;

         set
         {
            if ((_flags == null) || (!_flags.All.SetEquals(value.All)))
            {
               Modified = (_flags != null);

               _flags = value;
            }
         }
      }

      /// <summary>
      /// Gets the MediaContainer model used when returning a JSON response from a Controller.
      /// </summary>
      public virtual Models.MediaContainer Model
      {
         get => new()
         {
            Id = Id,
            Parent = Parent?.Id,
            ParentType = Parent?.Type,
            Name = Name,
            Description = Description,
            Type = Type,
            Path = Path,
            FullPath = FullPath,
            DateAdded = DateAdded,
            DateCreated = DateCreated,
            DateModified = DateModified,
            Flags = Flags.ToArray()
         };

         set
         {
            if (value == null) return;

            Id = value.Id;
            ParentType = value.ParentType;

            IMediaContainer parent = null;

            if (value.ParentType == null)
            {
               // This must be the MediaLibrary itself.
            }
            else
            {
               var parentType = value.ParentType.ToEnum<MediaContainerType>();

               if (parentType == MediaContainerType.Library)
               {
                  parent = MediaLibrary;
               }
               else
               {
                  var type = value.ParentType.ToEnum<MediaContainerType>().ToType();

                  // Create the parent container of the right type.
                  parent = (MediaContainer) Activator.CreateInstance(type, Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, value.Parent, null);
               }
            }

            Parent = parent;
            Type = value.Type;

            // Handle a possible rename operation.
            if ((Name != null) && (value.Name != Name))
            {
               Move(FullPath, value.Path + value.Name);
            }

            Name = value.Name;
            Description = value.Description;

            DateAdded = value.DateAdded;
            DateCreated = value.DateCreated;
            DateModified = value.DateModified;

            Flags = new MediaContainerFlags(value.Flags);

            if (value.Path != Path)
            {
               throw new NotSupportedException("Moving media containers is currently not supported.");
            }
         }
      }

      #endregion // Fields

      #region Constructors

      public MediaContainer(ILogger<MediaContainer> logger,
                            IServiceProvider services,
                            IConfiguration configuration,
                            IThumbnailsDatabase thumbnailsDatabase,
                            IMediaLibrary mediaLibrary,
                            // Optional Named Arguments
                            string id = null, string path = null
      )
      {
         bool reused = false;

         Logger = logger;
         Services = services;
         Configuration = configuration;
         ThumbnailsDatabase = thumbnailsDatabase;
         MediaLibrary = mediaLibrary ?? (IMediaLibrary) this;

         // The Solr service needs to be initialized only once when the MediaLibrary is instantiated
         // but before the Load() is called and therefore we do it here.
         if (GetType().ToMediaContainerType() == MediaContainerType.Library)
         {
            // Consume the scoped Solr Index Service.
            using IServiceScope scope = Services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            // Initialize the Solr Index Service.
            solrIndexService.Initialize();
         }

         var model = Load(id: id, path: path);

         if (model != null)
         {
            Model = model;

            Modified = false;

            return;
         }

         // We couldn't find an entry in the Solr index corresponding to either the container's Id
         // or its Path. Let's create a new entry then.

         Id = mediaLibrary?.GenerateUniqueId(path, out reused) ?? System.IO.Path.GetRandomFileName();

         if ((id != null) || (path != null))
         {
            // Split the path in parent, child components.
            var pathComponents = GetPathComponents(path);

            if (pathComponents.Parent != null)
            {
               // There is a parent to take care of. Let's infer the parent type.
               Type parentType = GetMediaContainerType(GetPathComponents(pathComponents.Parent).Child);

               // Now make sure a type was successfully deduced.
               if (parentType != null)
               {
                  // Now let's instantiate the Parent.
                  Parent = (MediaContainer)Activator.CreateInstance(parentType, Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, null, pathComponents.Parent);
                  ParentType = parentType.ToMediaContainerType().ToString();
               }
            }
            else
            {
               // The path supplied refers to the root of the MediaLibrary i.e. the "/". We simply set
               // the parent to the MediaLibrary instance itself.

               Parent = MediaLibrary;
            }
         }
         else
         {
            // This is the MediaLibrary itself and thus has no parents. Leaving the Parent = null;
         }

         Name = GetMediaContainerName(path);
         Type = this.GetType().ToMediaContainerType().ToString();
         Flags = new MediaContainerFlags();
         DateAdded = DateTime.UtcNow;

         if (!reused)
         {
            Created = true;
         }

         // Now that we're here, we can assume that the container entry and its parent(s) have been
         // created. Now the constructor of the inherited calling class will take care of the
         // container-specific attributes.
      }

      #endregion // Constructors

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

      public static (string Parent, string Child) GetPathComponents(string path)
      {
         string parent = null;
         string child = null;

         if (String.IsNullOrEmpty(path)) return (Parent: parent, Child: child);

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
                  child = path[(i + 1)..];
                  parent = path.Substring(0, i + 1);
                  break;
               }
            }
         }

         return (Parent: parent, Child: child);
      }

      #endregion // Static Methods

      #region Public Methods

      /// <summary>
      /// Returns the type of the current MediaContainer.
      /// </summary>
      /// <returns>Returns a value defined in <typeparamref name="MediaContainerType"/>.</returns>
      public MediaContainerType GetMediaContainerType()
      {
         return Type.ToEnum<MediaContainerType>();
      }

      /// <summary>
      /// Saves the MediaContainer and its parents to the MediaLibrary if necessary.
      /// </summary>
      /// <returns></returns>
      public bool Save()
      {
         var result = false;

         // This can happen when a container has been loaded that does not exist on either disk or
         // the library.
         if (Created && Deleted) return false;

         // Save the Parent(s) before attending to the child!
         Parent?.Save();

         if (Skipped)
         {
            Logger.LogInformation("{} Skipped: {}", Type, FullPath);

            return result;
         }

         if (Deleted)
         {
            using IServiceScope scope = Services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            result = solrIndexService.Delete(Model);

            if (result)
            {
               Logger.LogInformation("{} Removed: {}", Type, FullPath);
            }

            return result;
         }

         if (Created)
         {
            using IServiceScope scope = Services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            result = solrIndexService.Add(Model);

            if (result)
            {
               Logger.LogInformation("{} Added: {}", Type, FullPath);

               Created = false;
               Modified = false;
            }

            return result;
         }

         if (Modified)
         {
            using IServiceScope scope = Services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            result = solrIndexService.Update(Model);

            if (result)
            {
               Logger.LogInformation("{} Updated: {}", Type, FullPath);

               Modified = false;
            }

            return result;
         }

         return result;
      }

      string GetMediaContainerName(string path)
      {

         var pathComponents = GetPathComponents(path);

         switch (GetType().ToMediaContainerType())
         {
            case MediaContainerType.Server:
               // Extract the Server Name from the supplied path removing the \ characters.
               return MediaContainer.GetPathComponents(path).Child?.Trim(new Char[] { '\\' });

            case MediaContainerType.Drive:
               // Extract the Drive Name from the supplied path removing the \ and : characters.
               return MediaContainer.GetPathComponents(path).Child?.Trim(new Char[] { '\\', ':' });

            case MediaContainerType.Folder:
               // Extract the Folder Name from the supplied path removing the \ and / characters.
               return MediaContainer.GetPathComponents(path).Child?.Trim(new Char[] { '\\', '/' });

            default:
               return pathComponents.Child;
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
      /// <exception cref="NotImplementedException">This MediaContainer does not offer a Delete() method!</exception>
      public virtual void Delete(bool permanent = false)
      {
         throw new NotImplementedException("This MediaContainer does not offer a Delete() method!");
      }

      /// <summary>
      /// Moves (or Renames) the MediaContainer from one location or name to another. Each MediaContainer type
      /// can if needed override and implement its own Move method.
      /// </summary>
      /// <param name="source">The fullpath of the original name and location.</param>
      /// <param name="destination">The fullpath of the new name and location.</param>
      public virtual void Move(string source, string destination)
      {
         switch (Type)
         {
            case "Drive":
            case "Server":
               throw new InvalidOperationException("This media container does not support a Move() operation!");
            case "Folder":
               throw new InvalidOperationException("This media container does not support a Move() operation!");
               // TODO: Moving folders need a bit more work namely to check whether the destination files and
               //       folders already exist on disk or in the library and throw an exception if so.
               // Directory.Move(source, destination);
               // break;
            case "Audio":
            case "Video":
            case "Photo":
               File.Move(source, destination);
               break;
            default:
               throw new InvalidOperationException("This media container does not support a Move() operation!");
         }
      }

      #endregion // Overridables

      #region Protected Methods

      protected Models.MediaContainer Load(string id = null, string path = null)
      {
         using IServiceScope scope = Services.CreateScope();
         ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

         var value = id ?? path ?? "Library";
         var field = (id != null) ? "id" : (path != null) ? "fullPath" : "type";

         var results = solrIndexService.Get(field, value);

         if (results.Count > 1)
         {
            throw new Exception(String.Format("Found multiple entries in the Solr index having the same {0}: {1}", field, value));
         }

         if (results.Count == 1) return results.First();

         return null;
      }

      #endregion // Protected Methods

      #region Private Methods

      private Type GetMediaContainerType(string container)
      {
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
                ((container[^1] == '\\') || (container[^1] == '/')))
            {
               // It's a folder.
               return typeof(MediaFolder);
            }

            // It's probably a file then, let's find its type.
            var supportedExtensions = new SupportedExtensions(Configuration);

            // Extract the file extension including the '.' character.
            string extension = System.IO.Path.GetExtension(container).ToLower();

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
                  return typeof(PhotoFile);
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

         return null;
      }

      protected virtual void Dispose(bool disposing)
      {
         if (!_disposed)
         {
            if (disposing)
            {
               Save();
            }

            _disposed = true;
         }
      }

      // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
      // ~MediaContainer()
      // {
      //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      //     Dispose(disposing: false);
      // }

      public void Dispose()
      {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         Dispose(disposing: true);
         GC.SuppressFinalize(this);
      }

      #endregion // Private Methods
   }
}
