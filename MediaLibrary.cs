using System;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace MediaCurator
{
   class MediaLibrary : IMediaLibrary
   {
      private readonly IConfiguration _configuration;

      private readonly ILogger<MediaLibrary> _logger;

      private readonly IThumbnailsDatabase _thumbnailsDatabase;

      /// <summary>
      /// The static XDocument that contains the media database itself. There is only one instance
      /// of this throughout the whole application. It's either loaded from the existing XML file or
      /// is created during the first instantiation of the MediaLibrary.
      /// </summary>
      public static XDocument Document = null;

      /// <summary>
      /// The full path to the database file.
      /// </summary>
      public string FullPath
      {
         get
         {
            return _configuration["MediaLibrary:Path"] + Platform.Separator.Path + _configuration["MediaLibrary:Name"];
         }
      }

      public static bool Modified
      {
         get;
         private set;
      }

      /// <summary>
      /// The supported file extensions for each media type.
      /// </summary>
      public readonly MediaContainerTypeExtensions SupportedExtensions;

      /// <summary>
      /// Initializes a new instance of the <see cref="MediaCurator.MediaLibrary"/> class. 
      /// Before doing anything it checks whether the MediaLibrary has already been instantiated 
      /// once which means that the MediaLibrary has already been loaded and in that case it 
      /// doesn't do anything and only returns. The only exception to this is if a new database 
      /// file has been supplied which means that the MediaLibrary has to be re-initialzied.
      /// If it has not before been instantiated, it checks whether the database XML file exists. If
      /// it does, it loads its contents into Document and it creates a new one otherwise.
      /// </summary>
      public MediaLibrary(IConfiguration configuration, ILogger<MediaLibrary> logger, IThumbnailsDatabase thumbnailsDatabase)
      {
         _logger = logger;
         _configuration = configuration;
         _thumbnailsDatabase = thumbnailsDatabase;

         // Read and store the supported extensions from the configuration file.
         SupportedExtensions = new MediaContainerTypeExtensions(_configuration);

         // Check if the XML Database file already exists.
         if (File.Exists(FullPath))
         {
            // Yes it does! Load it then.
            Document = XDocument.Load(FullPath);

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
               CreateNewDatabase(FullPath);

               // Install a handler for the MediaLibrary's Changed event. This is where we set 
               // the Modified flag.
               Document.Changed += Document_Changed;

               // Reset the Modified flag.
               Modified = false;
            }
            catch (System.IO.IOException)
            {
               // It appears that the MediaLibrary path in which it has to be created is not
               // available at the moment. Let's ignore it for now.
            }
            catch (Exception)
            {
               throw;
            }
         }
      }

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
            new XElement("MediaLibrary",
               new XAttribute("Id", Path.GetRandomFileName()),
               new XAttribute("Version", 1.0),
               new XAttribute("DateCreated", DateTime.Now.ToString(CultureInfo.InvariantCulture)),
               new XAttribute("DateModified", DateTime.Now.ToString(CultureInfo.InvariantCulture))));

         // Save the Xml Database to the disk.
         Document.Save(database);
      }


      /* TODO: Implement a similar method which returns MediaContainers instead.
      public ObservableCollection<MediaLibraryTreeItem> EnumerateMediaLibraryTree(XElement parentElement = null,
                                                                                  uint flags = 0,
                                                                                  uint values = 0,
                                                                                  MediaLibraryTreeItem parentItem = null)
      {
         ObservableCollection<MediaLibraryTreeItem> mediaLibraryTree = new ObservableCollection<MediaLibraryTreeItem>();

         if (parentElement == null)
         {
            parentElement = MediaCurator.MediaLibrary.Document.Root;
         }

         foreach (XElement item in parentElement.Nodes())
         {
            switch (item.Name.ToString())
            {
               case "Drive":

                  if ((Tools.GetDecendantsCount(item, "Audio", flags, values, true) == 0) &&
                        (Tools.GetDecendantsCount(item, "Video", flags, values, true) == 0) &&
                        (Tools.GetDecendantsCount(item, "Photo", flags, values, true) == 0))
                  {
                     // This would be an empty drive. Let's ignore it.
                     continue;
                  }

                  MediaDrive newMediaDrive = new MediaDrive(item);
                  string serialNumber = Tools.GetVolumeSerialNumber(newMediaDrive.Name);
                  string volumeLabel = Tools.GetVolumeLabel(newMediaDrive.Name);
                  if (serialNumber != null)
                  {
                     if (newMediaDrive.SerialNumber == serialNumber)
                     {
                        MediaLibraryTreeItem newDriveItem = new MediaLibraryTreeItem
                        {
                           Parent = parentItem,
                           Name = volumeLabel.ToUpper() + " (" + newMediaDrive.Name + ":)",
                           FullPath = newMediaDrive.FullPath,
                           Thumbnail = new BitmapImage(new Uri("pack://application:,,,/Icons/24x24/Drive.png"))
                        };

                        newDriveItem.Items = EnumerateMediaLibraryTree(item, flags, values, newDriveItem);

                        mediaLibraryTree.Add(newDriveItem);
                     }
                  }
                  break;

               case "Server":

                  if ((Tools.GetDecendantsCount(item, "Audio", flags, values, true) == 0) &&
                      (Tools.GetDecendantsCount(item, "Video", flags, values, true) == 0) &&
                      (Tools.GetDecendantsCount(item, "Photo", flags, values, true) == 0))
                  {
                     // This would be an empty server. Let's ignore it.
                     continue;
                  }

                  MediaServer newMediaServer = new MediaServer(item);

                  MediaLibraryTreeItem newServerItem = new MediaLibraryTreeItem
                  {
                     Parent = parentItem,
                     Name = newMediaServer.Name,
                     FullPath = newMediaServer.FullPath,
                     Thumbnail = new BitmapImage(new Uri("pack://application:,,,/Icons/24x24/Server.png"))
                  };

                  newServerItem.Items = EnumerateMediaLibraryTree(item, flags, values, newServerItem);

                  mediaLibraryTree.Add(newServerItem);

                  break;

               case "Folder":

                  if ((Tools.GetDecendantsCount(item, "Audio", flags, values, true) == 0) &&
                      (Tools.GetDecendantsCount(item, "Video", flags, values, true) == 0) &&
                      (Tools.GetDecendantsCount(item, "Photo", flags, values, true) == 0))
                  {
                     // This would be an empty folder. Let's ignore it.
                     continue;
                  }

                  MediaFolder newMediaFolder = new MediaFolder(item);

                  MediaLibraryTreeItem newFolderItem = new MediaLibraryTreeItem
                  {
                     Parent = parentItem,
                     Name = newMediaFolder.Name,
                     FullPath = newMediaFolder.FullPath,
                     Thumbnail = new BitmapImage(new Uri("pack://application:,,,/Icons/24x24/Folder.png"))
                  };

                  newFolderItem.Items = EnumerateMediaLibraryTree(item, flags, values, newFolderItem);

                  mediaLibraryTree.Add(newFolderItem);

                  break;
            }
         }

         return mediaLibraryTree;
      }
      */

      /// <summary>
      /// Enumerates and returns an ObservableCollection of the media containers having a matching
      /// flags pattern supplied using the flags and values.
      /// </summary>
      /// <param name="path">The path in which the MediaContainers exist. It can be a Drive, a Server
      /// or a Folder.</param>
      /// <param name="flags">The flags mask of the flags we're interested in picking.</param>
      /// <param name="values">The flag values of interest to include/exclude in/from the returned
      /// collection.</param>
      /// <returns>An ObservableCollection containing the enumerated MediaContainers.</returns>
      public List<MediaContainer> ListMediaContainers(string path,
                                                      IProgress<Tuple<double, double, string>> progress,
                                                      uint flags = 0,
                                                      uint values = 0)
      {
         double index = 0.0, total = 0.0;
         MediaContainer mediaContainer = new MediaContainer(_configuration, path);
         List<MediaContainer> mediaContainers = new List<MediaContainer>();

         // In case of a Server or a Drive or a Folder located in the root, the mediaContainer's Self
         // will be null and it only will have found a Parent element which is the one we need. As a
         // workaround, we replace the item with its parent.

         if ((mediaContainer.Self == null) && (mediaContainer.Parent != null))
         {
            mediaContainer.Self = mediaContainer.Parent.Self;
         }
         else if ((mediaContainer.Self == null) && (mediaContainer.Parent == null))
         {
            mediaContainer.Self = Document.Root;
         }

         // Using mediaFolder.Self.Descendants() instead of mediaContainer.Self.Nodes() will cause
         // a recursive display of all descending nodes of the parent node.

         foreach (XElement item in mediaContainer.Self.Nodes())
         {
            total++;
         }

         foreach (XElement item in mediaContainer.Self.Nodes())
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

                  mediaContainers.Add(new MediaDrive(_configuration, item));
                  break;

               case "Server":

                  if ((Tools.GetDecendantsCount(item, "Audio", flags, values, true) == 0) &&
                     (Tools.GetDecendantsCount(item, "Video", flags, values, true) == 0) &&
                     (Tools.GetDecendantsCount(item, "Photo", flags, values, true) == 0))
                  {
                     // This would be an empty server. Let's ignore it.
                     break;
                  }

                  mediaContainers.Add(new MediaServer(_configuration, item));
                  break;

               case "Folder":

                  if ((Tools.GetDecendantsCount(item, "Audio", flags, values, true) == 0) &&
                     (Tools.GetDecendantsCount(item, "Video", flags, values, true) == 0) &&
                     (Tools.GetDecendantsCount(item, "Photo", flags, values, true) == 0))
                  {
                     // This would be an empty folder. Let's ignore it.
                     break;
                  }

                  mediaContainers.Add(new MediaFolder(_configuration, item));
                  break;

               case "Audio":
                  // TODO: mediaContainers.Add( new AudioFile( item ) );
                  break;

               case "Video":
                  mediaContainers.Add(new VideoFile(_configuration, _thumbnailsDatabase, item));
                  break;

               case "Photo":
                  // TODO: mediaContainers.Add( new PhotoFile( item ) );
                  break;
            }

            progress.Report(new Tuple<double, double, string>(++index, total, "Loading..."));
         }

         progress.Report(new Tuple<double, double, string>(0, 100, ""));

         return mediaContainers;
      }

      public MediaFile InsertMedia(string path,
                                   IProgress<Tuple<double, double>> progress,
                                   IProgress<byte[]> preview)
      {
         string fileName = Path.GetFileName(path);
         MediaContainerType mediaType = GetMediaType(path);
         MediaFile newMediaFile = null;

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

               InsertAudioFile(path, progress, preview);

               break;

            /*-------------------------------------------------------------------------------------
                                                VIDEO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Video:

               newMediaFile = InsertVideoFile(path, progress, preview);

               break;

            /*-------------------------------------------------------------------------------------
                                                PHOTO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Photo:

               InsertPhotoFile(path, progress, preview);

               break;
         }

         return newMediaFile;
      }

      /// <summary>
      /// Checks an already present element in the MediaLibrary against its physical file to see if
      /// anything has changed and if the element needs to be updated or deleted.
      /// </summary>
      /// <param name="mediaElement">The media element to be checked for chanegs.</param>
      /// <param name="progress">The file progress if it required regeneration of thumbnails.</param>
      /// <param name="preview">The thumbnail preview.</param>
      public void UpdateMedia(XElement element,
                              IProgress<Tuple<double, double>> progress,
                              IProgress<byte[]> preview)
      {
         // Instantiate a MediaFile using the acquired element.
         MediaFile mediaFile = new MediaFile(_configuration, _thumbnailsDatabase, element);

         // Reset the Current File Progress.
         progress.Report(new Tuple<double, double>(0, 0));

         if (mediaFile != null)
         {
            MediaContainer rootMediaContainer = mediaFile.Root;

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

                  UpdateVideoFile(element, progress, preview);

                  break;

               /*-------------------------------------------------------------------------------------
                                                   PHOTO FILE
               -------------------------------------------------------------------------------------*/

               case MediaContainerType.Photo:

                  throw new NotImplementedException("Photo files cannot yet be handled!");
            }
         }
      }

      private void InsertAudioFile(string path, IProgress<Tuple<double, double>> progress, IProgress<byte[]> preview)
      {

      }

      private VideoFile InsertVideoFile(string path, IProgress<Tuple<double, double>> progress, IProgress<byte[]> preview)
      {
         VideoFile videoFile = new VideoFile(_configuration, _thumbnailsDatabase, path);

         if (videoFile.Self != null)
         {
            // Check if there are any thumbnails already generated for this file.
            if (videoFile.Thumbnails.Count == 0)
            {
               // Make sure the video file is valid and not corrupted or empty.
               if ((videoFile.Size > 0) &&
                   (videoFile.Resolution.Height != 0) &&
                   (videoFile.Resolution.Width != 0))
               {
                  Debug.Write("GENERATING THUMBNAILS: " + videoFile.FullPath);

                  // Nope, looks like we need to generate the thumbnails.
                  videoFile.GenerateThumbnails(progress, preview);
               }
            }
         }

         return videoFile;
      }

      private void UpdateVideoFile(XElement element, IProgress<Tuple<double, double>> progress, IProgress<byte[]> preview)
      {
         VideoFile videoFile = new VideoFile(_configuration, _thumbnailsDatabase, element, true);

         if (!videoFile.Flags.Deleted)
         {
            // Check if there are any thumbnails already generated for this file or if the file is
            // modified and update it if so.
            if ((videoFile.Thumbnails.Count == 0) || (videoFile.Modified))
            {
               // Make sure the video file is valid and not corrupted or empty.
               if ((videoFile.Size > 0) &&
                   (videoFile.Resolution.Height != 0) &&
                   (videoFile.Resolution.Width != 0))
               {
                  Debug.Write("GENERATING THUMBNAILS: " + videoFile.FullPath);

                  // Nope, looks like we need to re-generate the thumbnails.
                  videoFile.GenerateThumbnails(progress, preview);
               }
            }
         }
      }

      private void InsertPhotoFile(string path, IProgress<Tuple<double, double>> progress, IProgress<byte[]> preview)
      {

      }

      public void UpdateDatabase()
      {
         if (Modified)
         {
            // The Media Library seems to have been modified.
            Debug.Write("\r\nUpdating the Media Database...");

            // Update the DateModified with the current date and time.
            Document.Root.Attribute("DateModified").Value = DateTime.Now.ToString(CultureInfo.InvariantCulture);

            try
            {
               // Save the XML Database to the disk.
               Document.Save(FullPath);

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
         }
      }

      private MediaContainerType GetMediaType(string path)
      {
         // Extract the file extension including the '.' character.
         string fileExtension = Path.GetExtension(path).ToLower();

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
