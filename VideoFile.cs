﻿using System;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaCurator
{
   class VideoFile : MediaFile
   {
      #region Constants

      private static readonly int[,] TotalThumbnails =
      {
      /* Total number of thumbnails based on the length in seconds :                              */ 
      /*   From      To       Count             Maximum Duration     Every X Seconds              */
         { 0,        60,      24       },    // 1m                   2.5s
         { 61,       120,     24       },    // 2m                   5s
         { 121,      240,     24       },    // 4m                   10s  
         { 241,      480,     24       },    // 8m                   20s  
         { 481,      960,     24       },    // 16m                  40s  
         { 961,      1920,    24       },    // 32m                  80s  
         { 1921,     3840,    24       },    // 64m                  160s  
         { 3841,     7680,    24       },    // 128m                 320s  
         { 7681,     15360,   24       },    // 256m                 640s  
         { 15361,    30720,   24       },    // 512m                 1280s  
         { 30721,    61440,   24       },    // 1024m                2560s 
      };

      #endregion // Constants

      #region Fields

      private ILogger<VideoFile> _logger;

      private IConfiguration _configuration { get; }

      public double Duration
      {
         get
         {
            string duration = Tools.GetAttributeValue(Self, "Duration");
            return Double.Parse((duration.Length > 0) ? duration : "0.0",
                                 CultureInfo.InvariantCulture);
         }

         set
         {
            Tools.SetAttributeValue(Self, "Duration", value.ToString(CultureInfo.InvariantCulture));
         }
      }

      /// <summary>
      /// Gets the video duration in text to be shown on thumbnails.
      /// </summary>
      /// <value>
      /// The video duration in text format.
      /// </value>
      public string DurationText
      {
         get
         {
            string duration = "";

            if (Duration <= 0)
            {
               return duration;
            }

            TimeSpan timespan = TimeSpan.FromSeconds(Duration);

            if (timespan.Hours > 0.0)
            {
               duration += string.Format("{0:D2}:", timespan.Hours);
            }

            duration += string.Format("{0:D2}:{1:D2}",
                                       timespan.Minutes,
                                       timespan.Seconds);

            return " " + duration + " ";
         }
      }

      public ResolutionType Resolution
      {
         get
         {
            return new ResolutionType(Tools.GetAttributeValue(Self, "Resolution"));
         }

         set
         {
            Tools.SetAttributeValue(Self, "Resolution", value.ToString());
         }
      }

      /// <summary>
      /// Gets the tooltip text of this video file.
      /// </summary>
      public override string ToolTip
      {
         get
         {
            string tooltip = "";
            TimeSpan duration = TimeSpan.FromSeconds(Duration);

            tooltip += String.Format("{0}", Name);

            if (Duration < 3600)               // < 1h
            {
               tooltip += String.Format("\nDuration: {0:D2}:{1:D2}",
                                         duration.Minutes, duration.Seconds);
            }
            else                                // >= 1h
            {
               tooltip += String.Format("\nDuration: {0:D2}:{1:D2}:{2:D2}",
                                         duration.Hours, duration.Minutes, duration.Seconds);
            }

            tooltip += String.Format("\nResolution: {0}", Resolution);

            tooltip += String.Format("\nDate Created: {0}", DateCreated);
            tooltip += String.Format("\nDate Modified: {0}", DateModified);

            if (Size < 0x400L)                 // < 1KB
            {
               tooltip += String.Format("\nSize: {0}B", Size);
            }
            else if (Size < 0x100000L)         // < 1MB
            {
               tooltip += String.Format("\nSize: {0:F2}KB", (double)Size / (double)0x400UL);
            }
            else if (Size < 0x40000000L)       // < 1GB
            {
               tooltip += String.Format("\nSize: {0:F2}MB", (double)Size / (double)0x100000UL);
            }
            else if (Size < 0x10000000000L)    // < 1TB
            {
               tooltip += String.Format("\nSize: {0:F2}GB", ((double)Size / (double)0x40000000UL));
            }

            return tooltip;
         }
      }

      #endregion // Fields

      #region Constructors

      public VideoFile(IConfiguration configuration, ILogger<VideoFile> logger, string path)
         : base(configuration, logger, "Video", path)
      {
         _logger = logger;
         _configuration = configuration;

         // The base class constructor will take care of the parents and the creation or retrieval 
         // of the element itself. Here we'll attend to additional properties of a video file.

         if (Self != null)
         {
            // TODO: Initialize the Thumbnails. This will enumerate the already existing Thumbnail files.
            // Thumbnail = new MediaContainerThumbnail(Id, Properties.Settings.Default.ThumbnailsCachingMethod);

            if (Created)
            {
               // This element did not exist before and has been createdn or it did exist but it has
               // been marked as "Modified". Therefore, the missing/updated fields need be filled in here.

               // Acquire the additional video file information.
               var jsonVideoFileInfo = GetFileInfo(path);

               /*----------------------------------------------------------------------------------
                                                   DURATION
               ----------------------------------------------------------------------------------*/

               try
               {
                  Duration = Double.Parse((string)jsonVideoFileInfo["format"]["duration"],
                                          CultureInfo.InvariantCulture);
               }
               catch (Exception e)
               {
                  Debug.WriteLine(path + " :");
                  Debug.WriteLine(e.Message);
               }

               /*----------------------------------------------------------------------------------
                                                   RESOLUTION
               ----------------------------------------------------------------------------------*/

               try
               {
                  Resolution = new ResolutionType(String.Format("{0}x{1}",
                                                  jsonVideoFileInfo["streams"][0]["width"],
                                                  jsonVideoFileInfo["streams"][0]["height"]));
               }
               catch (Exception e)
               {
                  Debug.WriteLine(path + " :");
                  Debug.WriteLine(e.Message);
               }
            }
         }
      }

      public VideoFile(IConfiguration configuration, ILogger<VideoFile> logger, XElement element, bool update = false)
         : base(configuration, logger, element, update)
      {
         _logger = logger;
         _configuration = configuration;

         if (Self != null)
         {
            // TODO: Initialize the Thumbnails. This will enumerate the already existing Thumbnail files.
            // Thumbnail = new MediaContainerThumbnail(Id, Properties.Settings.Default.ThumbnailsCachingMethod);

            if (update)
            {
               if (Modified)
               {
                  // Update the additional video file information.
                  var jsonVideoFileInfo = GetFileInfo(FullPath);

                  /*--------------------------------------------------------------------------------
                                                    DURATION
                  --------------------------------------------------------------------------------*/

                  try
                  {
                     Duration = Double.Parse((string)jsonVideoFileInfo["format"]["duration"],
                                             CultureInfo.InvariantCulture);
                  }
                  catch (Exception e)
                  {
                     Debug.WriteLine(FullPath + " : ");
                     Debug.WriteLine(e.Message);
                  }

                  /*--------------------------------------------------------------------------------
                                                    RESOLUTION
                  --------------------------------------------------------------------------------*/

                  try
                  {
                     Resolution = new ResolutionType(String.Format("{0}x{1}",
                                                     jsonVideoFileInfo["streams"][0]["width"],
                                                     jsonVideoFileInfo["streams"][0]["height"]));
                  }
                  catch (Exception e)
                  {
                     Debug.WriteLine(FullPath + " : ");
                     Debug.WriteLine(e.Message);
                  }

                  // TODO: Remove the previously generated thumbnails as they are most likely out of date.
                  // ThumbnailsDatabase.Instance.DeleteThumbnails(Id);
               }

               if (Flags.Deleted)
               {
                  // TODO: Remove the previously generated thumbnails as they are most likely out of date.
                  // ThumbnailsDatabase.Instance.DeleteThumbnails(Id);
               }
            }
         }
      }

      #endregion // Constructors

      #region Video File Operations

      private Dictionary<string, dynamic> GetFileInfo(string path)
      {
         string output = null;
         string executable = _configuration["FFmpeg:Path"] + Platform.Separator.Path + "ffmpeg" + Platform.Extension.Executable;

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffmpeg not found at the specified path: " + executable);
         }

         using (Process ffprobe = new Process())
         {
            ffprobe.StartInfo.FileName = executable;
            ffprobe.StartInfo.Arguments = "-v quiet -print_format json -show_format ";
            ffprobe.StartInfo.Arguments += "-show_streams -select_streams v:0 " + "\"" + path + "\"";
            ffprobe.StartInfo.CreateNoWindow = true;
            ffprobe.StartInfo.UseShellExecute = false;
            ffprobe.StartInfo.RedirectStandardOutput = true;

            ffprobe.Start();

            output = ffprobe.StandardOutput.ReadToEnd();

            ffprobe.WaitForExit(_configuration.GetSection("FFmpeg:Timeout").Get<Int32>());
         }

         return JsonSerializer.Deserialize<Dictionary<string, dynamic>>(output);
      }

      private byte[] GenerateThumbnail(string filePath, int position, int width, int index)
      {
         byte[] output = null;
         string fileName = Path.GetTempPath() + Platform.Separator.Path + System.IO.Path.GetRandomFileName() + ".jpg";
         string executable = _configuration["FFmpeg:Path"] + Platform.Separator.Path + "ffmpeg" + Platform.Extension.Executable;

         int waitInterval = 100;
         int totalWaitTime = 0;

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffmpeg not found at the specified path: " + executable);
         }

         using (Process ffmpeg = new Process())
         {
            ffmpeg.StartInfo.FileName = executable;

            ffmpeg.StartInfo.Arguments = "-ss " + position.ToString() + " -i \"" + filePath + "\" ";
            ffmpeg.StartInfo.Arguments += "-y -vf select=\"eq(pict_type\\,I),scale=";
            ffmpeg.StartInfo.Arguments += width.ToString() + ":-1\" -vframes 1 ";
            ffmpeg.StartInfo.Arguments += "\"" + fileName + "\"";

            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;

            ffmpeg.Start();

            do
            {
               Thread.Sleep(waitInterval);
               totalWaitTime += waitInterval;
            }
            while ((!ffmpeg.HasExited) &&
                   (totalWaitTime < _configuration.GetSection("FFmpeg:Timeout").Get<Int32>()));

            if (ffmpeg.HasExited)
            {
               if (File.Exists(fileName))
               {
                  try
                  {
                     output = File.ReadAllBytes(fileName);
                  }
                  catch
                  {
                     throw;
                  }

                  File.Delete(fileName);
               }

               Debug.Write(".");
            }
            else
            {
               // It's been too long. Kill it!
               ffmpeg.Kill();

               Debug.Write("x");
            }
         }

         return output;
      }

      /// <summary>
      /// Generates the thumbnails for the current video file.
      /// </summary>
      /// <param name="progress">The progress which is reflected in a ProgressBar and indicates how
      /// far the thumbnail generation for the current file has gone.</param>
      /// <param name="preview">The name of the recently generated thumbnail file in order to be 
      /// previewed for the user.</param>
      public void GenerateThumbnails(IProgress<Tuple<double, double>> progress,
                                     IProgress<byte[]> preview)
      {
         int totalThumbnails = 0;

         /*----------------------------------------------------------------------------------
                                         GENERATE THUMBNAILS
         ----------------------------------------------------------------------------------*/

         // Determine the count of thumbnails to generate based on the duration. 
         for (int row = 0; row < TotalThumbnails.GetLength(0); row++)
         {
            // Initialize the totalThumbnails with the largest value so far.
            totalThumbnails = TotalThumbnails[row, 2];

            if ((Duration >= TotalThumbnails[row, 0]) &&
                (Duration <= TotalThumbnails[row, 1]))
            {
               // Break out of the loop and have the current value of totalThumbnails persist.
               break;
            }
         }

         if (progress != null)
         {
            // Initialize the Current File Progress ProgressBar.
            progress.Report(new Tuple<double, double>(0.0, totalThumbnails));
         }

         Debug.Write(" [");

         for (int index = 1; index <= totalThumbnails; index++)
         {
            byte[] thumbnail = null;
            int position = (int)((index - 0.5) * Duration / totalThumbnails);

            // Generate the thumbnail.
            thumbnail = GenerateThumbnail(FullPath, position, 512, index);

            if ((thumbnail != null) && (thumbnail.Length > 0))
            {
               // TODO: Add the newly generated thumbnail to the database.
               // ThumbnailsDatabase.Instance.SetThumbnail(Id, index - 1, ref thumbnail);

               if (preview != null)
               {
                  // Update the thumbnail preview.
                  preview.Report(thumbnail);
               }
            }

            if (progress != null)
            {
               // Update the Current File Progress ProgressBar.
               progress.Report(new Tuple<double, double>(index, totalThumbnails));
            }
         }

         Debug.WriteLine("]");

         // TODO: Re-Initialize the Thumbnails. This will enumerate the newly generated Thumbnail files.
         // Thumbnail = new MediaContainerThumbnail(Id, Properties.Settings.Default.ThumbnailsCachingMethod);
      }

      #endregion // Video File Operations
   }
}