using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using MediaCurator.Solr;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCurator
{
   class MediaLibrary : MediaContainer, IMediaLibrary
   {
      private readonly ILogger<MediaLibrary> _logger;

      private readonly IServiceProvider _serviceProvider;

      /// <summary>
      /// The XDocument that contains the media database itself. It's either loaded from the existing XML file or
      /// is created during the first instantiation of the MediaLibrary.
      /// </summary>
      public XDocument Document = null;

      public static bool Modified
      {
         get;
         private set;
      }

      /// <summary>
      /// The supported file extensions for each media type.
      /// </summary>
      public readonly MediaContainerTypeExtensions SupportedExtensions;

      #region Constructors

      /// <summary>
      /// Initializes a new instance of the <see cref="MediaCurator.MediaLibrary"/> class. 
      /// Before doing anything it checks whether the MediaLibrary has already been instantiated 
      /// once which means that the MediaLibrary has already been loaded and in that case it 
      /// doesn't do anything and only returns. The only exception to this is if a new database 
      /// file has been supplied which means that the MediaLibrary has to be re-initialzied.
      /// If it has not before been instantiated, it checks whether the database XML file exists. If
      /// it does, it loads its contents into Document and it creates a new one otherwise.
      /// </summary>
      public MediaLibrary(IConfiguration configuration,
                          ILogger<MediaLibrary> logger,
                          IThumbnailsDatabase thumbnailsDatabase,
                          IServiceProvider serviceProvider)
         : base(configuration, thumbnailsDatabase, null, null)
      {
         _logger = logger;
         _serviceProvider = serviceProvider;

         string name = _configuration["MediaLibrary:Name"];
         string path = _configuration["MediaLibrary:Path"];
         string fullPath = path + Platform.Separator.Path + name;

         // Read and store the supported extensions from the configuration file.
         SupportedExtensions = new MediaContainerTypeExtensions(_configuration);

         // Initialize the Solr Index Service.
         if (_configuration.GetSection("Solr:URL").Exists())
         {
            using IServiceScope scope = _serviceProvider.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();
            solrIndexService.Initialize();
         }

         // Check if the XML Database file already exists.
         if (File.Exists(fullPath))
         {
            // Yes it does! Load it then.
            Document = XDocument.Load(fullPath);

            // Now set the Self to its root element.
            Self = Document.Root;

            // Install a handler for the MediaLibrary's Changed event. This is where we set 
            // the Modified flag.
            Document.Changed += Document_Changed;

            // Reset the Modified flag.
            Modified = false;
         }
         else
         {
            try
            {
               // Create the hosting directory
               Directory.CreateDirectory(path);

               // Now create the database itself
               CreateNewDatabase(fullPath);

               // Now set the Self to its root element.
               Self = Document.Root;

               // Install a handler for the MediaLibrary's Changed event. This is where we set 
               // the Modified flag.
               Document.Changed += Document_Changed;

               // Reset the Modified flag.
               Modified = false;

               // Clear the Solr index in case it's already been populated before.
               if (_configuration.GetSection("Solr:URL").Exists())
               {
                  using IServiceScope scope = _serviceProvider.CreateScope();
                  ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();
                  solrIndexService.Clear();
               }

               _logger.LogInformation("Media Library Created: {}", fullPath);
            }
            catch (Exception e)
            {
               _logger.LogError("Media Library Creation Failed, Because: {}", e.Message);
            }
         }
      }

      #endregion // Constructors

      private void Document_Changed(object sender, XObjectChangeEventArgs e)
      {
         // Set the Modified flag to indicate that something has changed in the MediaLibrary.
         Modified = true;
      }

      ~MediaLibrary()
      {
         // Remove the handler for the MediaLibrary's Changed event.
         Document.Changed -= Document_Changed;
      }

      private void CreateNewDatabase(string database)
      {
         // Create an empty Xml Database structure.
         Document = new XDocument(
            new XElement("Library",
               new XAttribute("Id", System.IO.Path.GetRandomFileName()),
               new XAttribute("Version", 1.0),
               new XAttribute("DateCreated", DateTime.Now.ToString(CultureInfo.InvariantCulture)),
               new XAttribute("DateModified", DateTime.Now.ToString(CultureInfo.InvariantCulture))));

         // Save the Xml Database to the disk.
         Document.Save(database);
      }

      /// <summary>
      /// Enumerates and returns an ObservableCollection of the media containers having a matching
      /// flags pattern supplied using the flags and values.
      /// </summary>
      /// <param name="path">The path in which the MediaContainers exist. It can be a Drive, a Server
      /// or a Folder.</param>
      /// <param name="query">The partial query to search for when querying the MediaContainers.</param>
      /// <param name="flags">The flags mask of the flags we're interested in picking.</param>
      /// <param name="values">The flag values of interest to include/exclude in/from the returned
      /// collection.</param>
      /// <param name="recursive">Whether the search should be recursive going deep into the children's children.</param>
      /// <returns>An ObservableCollection containing the enumerated MediaContainers.</returns>
      public List<IMediaContainer> ListMediaContainers(string path, string query = null, uint flags = 0, uint values = 0, bool recursive = false)
      {
         IMediaContainer mediaContainer = new MediaContainer(_configuration, _thumbnailsDatabase, this, path);
         List<IMediaContainer> mediaContainers = new();

         // In case of a Server or a Drive or a Folder located in the root, the mediaContainer's Self
         // will be null and it only will have found a Parent element which is the one we need. As a
         // workaround, we replace the item with its parent.

         if ((mediaContainer.Self == null) && (mediaContainer.Parent != null))
         {
            mediaContainer = mediaContainer.Parent;
         }

         if (mediaContainer is MediaFile)
         {
            string flagsTag = Tools.GetAttributeValue(mediaContainer.Self, "Flags");
            uint flagsInteger = Convert.ToUInt32(flagsTag.Length > 0 ? flagsTag : "0", 16);

            // Mask out only the flags we care about and compare them to the expected values.
            if (((flagsInteger & flags) ^ (values & flags)) <= 0)
            {
               // The requested flags match those of the item's.
               mediaContainers.Add(mediaContainer);
            }
         }
         else
         {
            // Using mediaFolder.Self.Descendants() instead of mediaContainer.Self.Nodes() will cause
            // a recursive display of all descending nodes of the parent node.

            var nodes = recursive ? mediaContainer.Self.Descendants() : mediaContainer.Self.Elements();

            // Filter the elements using the supplied flags.
            if (flags != 0 || values != 0)
            {
               nodes = nodes.Where(node =>
               {
                  var attribute = Tools.GetAttributeValue(node, "Flags");
                  var value = Convert.ToUInt32(attribute.Length > 0 ? attribute : "0", 16);

                  if (((value & flags) ^ (values & flags)) <= 0)
                  {
                     return true;
                  }

                  return false;
               });
            }

            // Filter the elements using the supplied name.
            if (query != null)
            {
               query = query.ToLower();

               nodes = nodes.Where(node => Tools.GetAttributeValue(node, "Name").ToLower().Contains(query));
            }

            foreach (XElement item in nodes)
            {
               string flagsTag = Tools.GetAttributeValue(item, "Flags");
               uint flagsInteger = Convert.ToUInt32(flagsTag.Length > 0 ? flagsTag : "0", 16);

               // Mask out only the flags we care about and compare them to the expected values.
               if (((flagsInteger & flags) ^ (values & flags)) > 0)
               {
                  // The requested flags don't match those of the item's.
                  continue;
               }

               switch (item.Name.ToString())
               {
                  case "Drive":

                     if ((Tools.GetDecendantsCount(item, "Audio", flags, values, true) == 0) &&
                        (Tools.GetDecendantsCount(item, "Video", flags, values, true) == 0) &&
                        (Tools.GetDecendantsCount(item, "Photo", flags, values, true) == 0))
                     {
                        // This would be an empty drive. Let's ignore it.
                        break;
                     }

                     mediaContainers.Add(new MediaDrive(_configuration, _thumbnailsDatabase, _mediaLibrary, item));
                     break;

                  case "Server":

                     if ((Tools.GetDecendantsCount(item, "Audio", flags, values, true) == 0) &&
                        (Tools.GetDecendantsCount(item, "Video", flags, values, true) == 0) &&
                        (Tools.GetDecendantsCount(item, "Photo", flags, values, true) == 0))
                     {
                        // This would be an empty server. Let's ignore it.
                        break;
                     }

                     mediaContainers.Add(new MediaServer(_configuration, _thumbnailsDatabase, _mediaLibrary, item));
                     break;

                  case "Folder":

                     if ((Tools.GetDecendantsCount(item, "Audio", flags, values, true) == 0) &&
                        (Tools.GetDecendantsCount(item, "Video", flags, values, true) == 0) &&
                        (Tools.GetDecendantsCount(item, "Photo", flags, values, true) == 0))
                     {
                        // This would be an empty folder. Let's ignore it.
                        break;
                     }

                     mediaContainers.Add(new MediaFolder(_configuration, _thumbnailsDatabase, _mediaLibrary, item));
                     break;

                  case "Audio":
                     // TODO: mediaContainers.Add( new AudioFile( item ) );
                     break;

                  case "Video":
                      mediaContainers.Add(new VideoFile(_configuration, _thumbnailsDatabase, _mediaLibrary, item));
                     break;

                  case "Photo":
                     mediaContainers.Add(new PhotoFile(_configuration, _thumbnailsDatabase, _mediaLibrary, item));
                     break;
               }
            }
         }

         return mediaContainers;
      }

      public MediaFile InsertMedia(string path)
      {
         MediaFile mediaFile = null;
         MediaContainerType mediaType = GetMediaType(path);

         switch (mediaType)
         {
            /*-------------------------------------------------------------------------------------
                                                 UNKNOWN
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Unknown:

               break;

            /*-------------------------------------------------------------------------------------
                                                AUDIO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Audio:

               /*mediaFile = */ InsertAudioFile(path);

               break;

            /*-------------------------------------------------------------------------------------
                                                VIDEO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Video:

               mediaFile = InsertVideoFile(path);

               break;

            /*-------------------------------------------------------------------------------------
                                                PHOTO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Photo:

               mediaFile = InsertPhotoFile(path);

               break;
         }

         if ((mediaFile != null) && mediaFile.Created)
         {
            // Add the new media to the Solr index if indexing is enabled.
            if (_configuration.GetSection("Solr:URL").Exists())
            {
               using IServiceScope scope = _serviceProvider.CreateScope();
               ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();
               solrIndexService.Add(mediaFile.Model);
            }

            _logger.LogInformation("Media Added: {}", path);
         }

         return mediaFile;
      }

      /// <summary>
      /// Checks an already present element in the MediaLibrary against its physical file to see if
      /// anything has changed and if the element needs to be updated or deleted.
      /// </summary>
      /// <param name="mediaElement">The media element to be checked for chanegs.</param>
      public void UpdateMedia(XElement element)
      {
         // Instantiate a MediaFile using the acquired element.
         MediaFile mediaFile = new(_configuration, _thumbnailsDatabase, _mediaLibrary, element);

         if (mediaFile != null)
         {
            IMediaContainer rootMediaContainer = mediaFile.Root;

            // Check whether the MediaDrive is located on this computer. 
            if (rootMediaContainer.Type == "Drive")
            {
               string serialNumber = Tools.GetVolumeSerialNumber(rootMediaContainer.Name);

               if (serialNumber == null)
               {
                  return;
               }

               if (((MediaDrive)rootMediaContainer).SerialNumber != serialNumber)
               {
                  return;
               }
            }

            switch (mediaFile.GetMediaContainerType())
            {
               /*-------------------------------------------------------------------------------------
                                                   AUDIO FILE
               -------------------------------------------------------------------------------------*/

               case MediaContainerType.Audio:

                  throw new NotImplementedException("Audio files cannot yet be handled!");

               /*-------------------------------------------------------------------------------------
                                                   VIDEO FILE
               -------------------------------------------------------------------------------------*/

               case MediaContainerType.Video:

                  mediaFile = UpdateVideoFile(element);

                  break;

               /*-------------------------------------------------------------------------------------
                                                   PHOTO FILE
               -------------------------------------------------------------------------------------*/

               case MediaContainerType.Photo:

                  mediaFile = UpdatePhotoFile(element);

                  break;
            }

            if ((mediaFile != null) && mediaFile.Modified)
            {
               // Update or Delete the new media in the Solr index if indexing is enabled.
               if (_configuration.GetSection("Solr:URL").Exists())
               {
                  using IServiceScope scope = _serviceProvider.CreateScope();
                  ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

                  if (mediaFile.Flags.Deleted)
                  {
                     solrIndexService.Delete(mediaFile.Model);
                  }
                  else
                  {
                     solrIndexService.Update(mediaFile.Model);
                  }
               }

               if (mediaFile.Flags.Deleted)
               {
                  _logger.LogInformation("Media Deleted: {}", mediaFile.FullPath);
               }
               else
               {
                  _logger.LogInformation("Media Updated: {}", mediaFile.FullPath);
               }
            }
         }
      }

      /// <summary>
      /// Checks an already present element in the MediaLibrary against its physical file to see if
      /// anything has changed and if the element needs to be updated or deleted.
      /// </summary>
      /// <param name="path">The media element to be checked for chanegs.</param>
      /// <param name="progress">The file progress if it required regeneration of thumbnails.</param>
      /// <param name="preview">The thumbnail preview.</param>
      /// <returns></returns>
      public MediaFile UpdateMedia(string path)
      {
         string fileName = System.IO.Path.GetFileName(path);
         MediaContainerType mediaType = GetMediaType(path);
         MediaFile mediaFile = null;

         switch (mediaType)
         {
            /*-------------------------------------------------------------------------------------
                                                 UNKNOWN
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Unknown:

               break;

            /*-------------------------------------------------------------------------------------
                                                AUDIO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Audio:

               throw new NotImplementedException("Audio files cannot yet be handled!");

            /*-------------------------------------------------------------------------------------
                                                VIDEO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Video:

               mediaFile = new VideoFile(_configuration, _thumbnailsDatabase, _mediaLibrary, path);

               if (mediaFile.Self != null)
               {
                  UpdateVideoFile(mediaFile.Self);
               }

               break;

            /*-------------------------------------------------------------------------------------
                                                PHOTO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Photo:

               mediaFile = new PhotoFile(_configuration, _thumbnailsDatabase, _mediaLibrary, path);

               if (mediaFile.Self != null)
               {
                  UpdatePhotoFile(mediaFile.Self);
               }

               break;
         }

         if ((mediaFile != null) && mediaFile.Modified)
         {
            // Update or Delete the new media in the Solr index if indexing is enabled.
            if (_configuration.GetSection("Solr:URL").Exists())
            {
               using IServiceScope scope = _serviceProvider.CreateScope();
               ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

               if (mediaFile.Flags.Deleted)
               {
                  solrIndexService.Delete(mediaFile.Model);
               }
               else
               {
                  solrIndexService.Update(mediaFile.Model);
               }
            }

            if (mediaFile.Flags.Deleted)
            {
               _logger.LogInformation("Media Deleted: {}", mediaFile.FullPath);
            }
            else
            {
               _logger.LogInformation("Media Updated: {}", mediaFile.FullPath);
            }
         }

         return mediaFile;
      }

      private void InsertAudioFile(string path)
      {

      }

      private VideoFile InsertVideoFile(string path)
      {
         VideoFile videoFile = new(_configuration, _thumbnailsDatabase, _mediaLibrary, path);

         return videoFile;
      }

      private VideoFile UpdateVideoFile(XElement element)
      {
         VideoFile videoFile = new(_configuration, _thumbnailsDatabase, _mediaLibrary, element, true);

         return videoFile;
      }

      private PhotoFile InsertPhotoFile(string path)
      {
         PhotoFile photoFile = new(_configuration, _thumbnailsDatabase, _mediaLibrary, path);

         return photoFile;
      }

      private PhotoFile UpdatePhotoFile(XElement element)
      {
         PhotoFile photoFile = new(_configuration, _thumbnailsDatabase, _mediaLibrary, element, true);

         return photoFile;
      }

      public void UpdateDatabase()
      {
         if (Modified)
         {
            // The Media Library seems to have been modified.
            Debug.Write("Updating the Media Database...");

            // Update the DateModified with the current date and time.
            Document.Root.Attribute("DateModified").Value = DateTime.Now.ToString(CultureInfo.InvariantCulture);

            try
            {
               string name = _configuration["MediaLibrary:Name"];
               string path = _configuration["MediaLibrary:Path"];
               string fullPath = path + Platform.Separator.Path + name;

               // Save the XML Database to the disk.
               Document.Save(fullPath);

               // Now remove the Modified flag.
               Modified = false;

               // Done!
               Debug.WriteLine(" Done!");
            }
            catch (Exception)
            {
               // Failed!
               Debug.WriteLine(" Failed!");

               throw;
            }

            // Perform a WAL checkpoint on the Thumbnails Database
            _thumbnailsDatabase.Checkpoint();
         }
      }

      private MediaContainerType GetMediaType(string path)
      {
         // Extract the file extension including the '.' character.
         string fileExtension = System.IO.Path.GetExtension(path).ToLower();

         if ((fileExtension == null) || (fileExtension.Length == 0))
         {
            // The media type is not recognized as it has an invalid or no extensions.
            return MediaContainerType.Unknown;
         }

         // It appears that the file does have an extension. We may proceed.

         // Check if it's a recognized video format.
         if (SupportedExtensions[MediaContainerType.Video].Contains(fileExtension))
         {
            // Looks like the file is a recognized video format.
            return MediaContainerType.Video;
         }

         // Check if it's a recognized photo format.         
         if (SupportedExtensions[MediaContainerType.Photo].Contains(fileExtension))
         {
            // Looks like the file is a recognized photo format.
            return MediaContainerType.Photo;
         }

         // Check if it's a recognized audio format.
         if (SupportedExtensions[MediaContainerType.Audio].Contains(fileExtension))
         {
            // Looks like the file is a recognized audio format.
            return MediaContainerType.Audio;
         }

         // Unrecognized file format.
         return MediaContainerType.Unknown;
      }
   }
}
