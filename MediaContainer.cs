/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

using SolrNet;
using Arcadeia.Solr;
using System.Globalization;
using Microsoft.Extensions.Options;
using Arcadeia.Configuration;

namespace Arcadeia
{
   public class MediaContainer : IMediaContainer
   {
      protected readonly IServiceProvider Services;

      protected readonly IOptionsMonitor<Settings> Settings;

      protected readonly ILogger<MediaContainer> Logger;

      protected readonly IThumbnailsDatabase ThumbnailsDatabase;

      protected readonly IMediaLibrary MediaLibrary;

      protected readonly IProgress<float>? Progress;

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
      public bool Modified
      {
         get
         {
            return Model != Original;
         }
      }

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
      /// A flag indicating that the MediaContainer based entity is has been moved or renamed.
      /// </summary>
      public bool Moved = false;

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

      /// <summary>
      /// The parent MediaContainer based type of this instance.
      /// </summary>
      public IMediaContainer? Parent { get; set; }

      /// <summary>
      /// Type of the parent MediaContainer based type of this instance.
      /// </summary>
      public string? ParentType { get; set; }

      /// <summary>
      /// Gets a list of the MediaContainer parents of this MediaContainer instance.
      /// </summary>
      public IEnumerable<IMediaContainer> Parents
      {
         get
         {
            var parent = (IMediaContainer) this;
            var result = new List<IMediaContainer>();

            do
            {
               parent = parent.Parent;
               if (parent != null)
               {
                  result.Add(parent);
               }
            }
            while (parent != null);

            return result;
         }
      }

      /// <summary>
      /// Gets a list of the direct MediaContainer children of this MediaContainer instance.
      /// </summary>
      public IEnumerable<MediaContainer> Children
      {
         get
         {
            var result = new List<MediaContainer>();

            switch (GetType().ToMediaContainerType())
            {
               case MediaContainerType.Library:
               case MediaContainerType.Folder:
               case MediaContainerType.Server:
               case MediaContainerType.Drive:
                  break;
               default:
                  return result;
            }

            using IServiceScope scope = Services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            if (string.IsNullOrEmpty(Id))
            {
               throw new ArgumentNullException(nameof(Id), "The Id cannot be null or empty.");
            }

            var field = "parent";
            var value = Id;

            var children = solrIndexService.Get(field, value);

            foreach (var child in children)
            {
               if (!string.IsNullOrEmpty(child.Id) && !string.IsNullOrEmpty(child.Type))
               {
                  var id = child.Id;
                  var type = child.Type.ToEnum<MediaContainerType>().ToType();

                  if (type == null) throw new ArgumentNullException(nameof(type), "The child type could not be determined.");

                  using MediaContainer? mediaContainer = Activator.CreateInstance(type, Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, id, null, Progress) as MediaContainer;

                  if (mediaContainer != null) {
                     result.Add(mediaContainer);
                  }
                  else
                  {
                     Logger.LogWarning("Skipped Invalid {} Child: {}", type, id);
                  }
               }
            }

            return result;
         }
      }

      /// <summary>
      /// Gets or sets the "id" of the MediaContainer. The Id is used to located the SolR index entry
      /// and also the the thumbnails database entry of the MediaContainer.
      /// </summary>
      /// <value>
      /// The unique identifier of the container entry.
      /// </value>
      public string? Id { get; set; }

      /// <summary>
      /// Gets or sets the "name" attribute of the container entry. This is for example the folder,
      /// file or server name and extension if any.
      /// </summary>
      /// <value>
      /// The "name" attribute of the container entry.
      /// </value>
      public string? Name { get; set; }

      /// <summary>
      /// Gets or sets the "description" attribute of the container entry.
      /// </summary>
      /// <value>
      /// The "description" attribute of the container entry.
      /// </value>
      public string? Description { get; set; }

      /// <summary>
      /// Gets or sets the "type" attribute of the container entry.
      /// </summary>
      /// <value>
      /// The "type" attribute of the container entry.
      /// </value>
      public string? Type { get; set; }

      /// <summary>
      /// Get the path the container is located in which is the parent's full path.
      /// </summary>
      /// <value>
      /// The path of the container.
      /// </value>
      public string? Path
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
      public string? FullPath
      {
         get
         {
            string? path = null;
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
            while (container.Parent is not null && (container = container.Parent) != null);

            return path;
         }
      }

      /// <summary>
      /// Gets or sets the date the container was added to the MediaLibrary.
      /// </summary>
      /// <value>
      /// The addition date in DateTime.
      /// </value>
      public DateTime? DateAdded { get; set; }

      /// <summary>
      /// Gets or sets the creation date of the file.
      /// </summary>
      /// <value>
      /// The creation date in DateTime.
      /// </value>
      public DateTime? DateCreated { get; set; }

      /// <summary>
      /// Gets or sets the last modification date of the file.
      /// </summary>
      /// <value>
      /// The last modification date in DateTime.
      /// </value>
      public DateTime? DateModified { get; set; }

      /// <summary>
      /// Gets or sets the "flags" attribute of the media container entry.
      /// </summary>
      /// <value>
      /// The flags attribute value. The individual flags can be accessed using their respective name.
      /// </value>
      public MediaContainerFlags? Flags { get; set; }

      /// <summary>
      /// Gets or sets the original MediaContainer model assigned at load.
      /// </summary>
      public Models.MediaContainer? Original { get; set; }

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
            Parents = [.. Parents.Select(parent => parent.Id!)],
            Name = Name,
            Description = Description,
            Type = Type,
            Path = Path,
            FullPath = FullPath,
            DateAdded = DateAdded,
            DateCreated = DateCreated,
            DateModified = DateModified,
            Flags = Flags?.ToArray()
         };

         set
         {
            if (value == null) return;

            Id = value.Id;

            // Handle a possible rename or move operation.
            if (!Moved && ((Name != null && value.Name != Name) || (Path != null && value.Path != Path)))
            {
               if (string.IsNullOrEmpty(value.Path))
               {
                  throw new ArgumentNullException(nameof(value.Path), "The Path cannot be null or empty.");
               }

               if (string.IsNullOrEmpty(value.Name))
               {
                  throw new ArgumentNullException(nameof(value.Name), "The Name cannot be null or empty.");
               }

               Move(System.IO.Path.Combine(value.Path, value.Name));
            }
            else
            {
               Name = value.Name;
               ParentType = value.ParentType;
            }

            if (value.ParentType == null)
            {
               // This must be the MediaLibrary itself.
               Name ??= MediaContainerType.Library.ToString();
            }
            else if (Parent == null)
            {
               IMediaContainer? parent;
               var parentType = value.ParentType.ToEnum<MediaContainerType>();

               if (parentType == MediaContainerType.Library)
               {
                  parent = MediaLibrary;
               }
               else
               {
                  var type = value.ParentType.ToEnum<MediaContainerType>().ToType();

                  if (type == null) throw new ArgumentNullException(nameof(type), "The parent type could not be determined.");

                  // Create the parent container of the right type.
                  parent = Activator.CreateInstance(type, Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, value.Parent, null, Progress) as MediaContainer;
               }

               Parent = parent;
            }

            Type = value.Type;
            Description = value.Description;

            DateAdded = value.DateAdded;
            DateCreated = value.DateCreated;
            DateModified = value.DateModified;

            Flags = new MediaContainerFlags(value.Flags);
         }
      }

      #endregion // Fields

      #region Constructors

      public MediaContainer(ILogger<MediaContainer> logger,
                            IServiceProvider services,
                            IOptionsMonitor<Settings> settings,
                            IThumbnailsDatabase thumbnailsDatabase,
                            IMediaLibrary? mediaLibrary,
                            // Optional Named Arguments
                            string? id = null, string? path = null,
                            IProgress<float>? progress = null
      )
      {
         Logger = logger;
         Services = services;
         Settings = settings;
         ThumbnailsDatabase = thumbnailsDatabase;
         MediaLibrary = mediaLibrary ?? (IMediaLibrary) this;
         Progress = progress;

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

         Models.MediaContainer? model = Load(id: id, path: path);

         if (model != null)
         {
            Model = model;
            Original = model;

            return;
         }

         // We couldn't find an entry in the Solr index corresponding to either the container's Id
         // or its Path. Let's create a new entry then.

         Id = MediaLibrary.GenerateUniqueId(path, out bool reused) ?? System.IO.Path.GetRandomFileName();

         if ((id != null) || (path != null))
         {
            // Split the path in parent, child components.
            var pathComponents = GetPathComponents(path);

            if (pathComponents.Parent != null)
            {
               // There is a parent to take care of. Let's infer the parent type.
               Type? parentType = GetMediaContainerType(GetPathComponents(pathComponents.Parent).Child);

               // Now make sure a type was successfully deduced.
               if (parentType != null)
               {
                  // Now let's instantiate the Parent.
                  Parent = Activator.CreateInstance(parentType, Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, null, pathComponents.Parent, Progress) as MediaContainer;
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

      public static (string? Parent, string? Child) GetPathComponents(string? path)
      {
         string? parent = null;
         string? child = null;

         if (string.IsNullOrEmpty(path)) return (Parent: parent, Child: child);

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
      public MediaContainerType? GetMediaContainerType()
      {
         return Type?.ToEnum<MediaContainerType>();
      }

      public Type? GetMediaContainerType(string? container)
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
            // Extract the file extension including the '.' character.
            string extension = System.IO.Path.GetExtension(container).ToLower();

            if ((extension != null) && (extension.Length != 0))
            {
               // It appears that the file does have an extension. We may proceed.

               // Check if it's a recognized video format.
               if (Settings.CurrentValue.SupportedExtensions.Video.Contains(extension))
               {
                  // Looks like the file is a recognized video format.
                  return typeof(VideoFile);
               }

               // Check if it's a recognized photo format.
               if (Settings.CurrentValue.SupportedExtensions.Photo.Contains(extension))
               {
                  // Looks like the file is a recognized photo format.
                  return typeof(PhotoFile);
               }

               // Check if it's a recognized audio format.
               if (Settings.CurrentValue.SupportedExtensions.Audio.Contains(extension))
               {
                  // Looks like the file is a recognized audio format.
                  throw new NotImplementedException("Audio files cannot yet be handled!");
               }
            }

            throw new NotSupportedException("The supplied child is of an invalid type!");
         }

         return null;
      }

      /// <summary>
      /// Saves the MediaContainer and its parents to the MediaLibrary if necessary.
      /// </summary>
      /// <returns></returns>
      public bool Save()
      {
         var result = false;

         // This can happen when a container has been loaded that exists neither on the disk nor in the library.
         if (Created && Deleted) return false;

         if (Skipped)
         {
            Logger.LogDebug("{} Skipped: {}", Type, FullPath);

            return false;
         }

         if (Deleted && (!Children.Any()))
         {
            using IServiceScope scope = Services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            result = solrIndexService.Delete(Model);

            if (result)
            {
               Original = null;

               Logger.LogInformation("{} Removed: {}", Type, FullPath);
            }

            Parent?.Save();

            return result;
         }

         Parent?.Save();

         if (Created)
         {
            using IServiceScope scope = Services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            var model = Model;
            result = solrIndexService.Add(model);

            if (result)
            {
               Created = false;
               Original = model;

               Logger.LogInformation("{} Added: {}", Type, FullPath);
            }

            return result;
         }
         else if (Modified)
         {
            var model = Model;

            if (Original != null && Logger.IsEnabled(LogLevel.Trace))
            {
               var differences = Original?.Differences(model);

               if (differences?.Count() > 0)
               {
                  Logger.LogTrace("{} Modified: {}\n\t{}", Type, FullPath, string.Join("\n\t", differences));
               }
            }

            using IServiceScope scope = Services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            result = solrIndexService.Update(model);

            if (result)
            {
               Original = model;

               Logger.LogInformation("{} Updated: {}", Type, FullPath);
            }

            return result;
         }

         return result;
      }

      string? GetMediaContainerName(string? path)
      {
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
               return GetPathComponents(path).Child;
         }

      }

      #endregion // Public Methods

      #region Overridable

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
      /// <param name="destination">The full path of the new name and location.</param>
      public virtual void Move(string destination)
      {
         throw new InvalidOperationException("This MediaContainer does not support a Move() operation!");
      }

      #endregion // Overridable

      #region Protected Methods

      protected Models.MediaContainer? Load(string? id = null, string? path = null)
      {
         using IServiceScope scope = Services.CreateScope();
         ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

         string value = id ?? path ?? "Library";
         string field = (id != null) ? "id" : (path != null) ? "fullPath" : "type";
         SolrQueryByField query = new(field, value);
         SortOrder[] orders =
         [
            new SortOrder("dateAdded", Order.ASC)
         ];

         SolrQueryResults<Models.MediaContainer> results = solrIndexService.Get(query, orders);

         if (results.Count > 1)
         {
            Logger.LogWarning("{} Duplicate Solr Entries Detected: {}: {}", results.Count, field, value);
         }

         if (results.Count > 0) return results.First();

         return null;
      }

      #endregion // Protected Methods

      #region Private Methods

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
