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

using System.ComponentModel.DataAnnotations;

namespace Arcadeia.Configuration
{
   public class Settings
   {
      [Required(ErrorMessage = "The 'Session' configuration is required.")]
      public required SessionSettings Session { get; set; }

      [Required(ErrorMessage = "The 'Thumbnails' configuration is required.")]
      public required ThumbnailsSettings Thumbnails { get; set; }

      [Required(ErrorMessage = "The 'Streaming' configuration is required.")]
      public required StreamingSettings Streaming { get; set; }

      [Required(ErrorMessage = "The 'SupportedExtensions' configuration is required.")]
      public required SupportedExtensionsSettings SupportedExtensions { get; set; }

      [Required(ErrorMessage = "The 'FFmpeg' configuration is required.")]
      public required FFmpegSettings FFmpeg { get; set; }

      [Required(ErrorMessage = "The 'YtDlp' configuration is required.")]
      public required YtDlpSettings YtDlp { get; set; }

      [Required(ErrorMessage = "The 'Solr' configuration is required.")]
      public required SolrSettings Solr { get; set; }

      [Required(ErrorMessage = "The 'Scanner' configuration is required.")]
      public required ScannerSettings Scanner { get; set; }

      [Required(ErrorMessage = "The 'Mounts' configuration is required.")]
      public required List<MountSettings> Mounts { get; set; } = [];

      [Required(ErrorMessage = "The 'Security' configuration is required.")]
      public required SecuritySettings Security { get; set; }

      [Required(ErrorMessage = "The 'Logging' configuration is required.")]
      public required LoggingSettings Logging { get; set; }

      public List<string> KnownProxies { get; set; } = [];

      public string AllowedHosts { get; set; } = "*";
   }

   public class SessionSettings
   {
      [Required(ErrorMessage = "IdleTimeoutSeconds is required.")]
      [Range(1, int.MaxValue, ErrorMessage = "IdleTimeoutSeconds must be greater than zero.")]
      public int IdleTimeoutSeconds { get; set; }
   }


   public class ThumbnailsSettings
   {
      [Required(ErrorMessage = "The 'Database' configuration is required for thumbnails.")]
      public required ThumbnailsDatabaseSettings Database { get; set; }

      public required Dictionary<string, ThumbnailSettings> Video { get; set; } = [];

      public required Dictionary<string, ThumbnailSettings> Photo { get; set; } = [];

      public required Dictionary<string, ThumbnailSettings> Audio { get; set; } = [];
   }

   public class ThumbnailsDatabaseSettings
   {
      [Required(ErrorMessage = "The 'Name' property is required for the thumbnails database.")]
      public required string Name { get; set; }

      [Required(ErrorMessage = "The 'Path' property is required for the thumbnails database.")]
      public required string Path { get; set; }
   }

   public class ThumbnailSettings
   {
      public int Width { get; set; } = -1;

      public int Height { get; set; } = -1;

      public int Count { get; set; } = 0;

      public bool Sprite { get; set; } = false;

      public bool Crop { get; set; } = false;
   }

   public class StreamingSettings
   {
      [Required(ErrorMessage = "The 'Segments' configuration is required for streaming.")]
      public required StreamingSegmentsSettings Segments { get; set; }
   }

   public class StreamingSegmentsSettings
   {
      [Required(ErrorMessage = "The 'Duration' property is required for streaming segments.")]
      public int Duration { get; set; }
   }

   public class SupportedExtensionsSettings
   {
      [Required(ErrorMessage = "The 'Audio' list is required in supported extensions.")]
      [RegularExpressions(@"^\.\w+$", ErrorMessage = "Must be a valid file extension starting with a dot.")]
      public List<string> Audio { get; set; } = [];

      [Required(ErrorMessage = "The 'Video' list is required in supported extensions.")]
      [RegularExpressions(@"^\.\w+$", ErrorMessage = "Must be a valid file extension starting with a dot.")]
      public List<string> Video { get; set; } = [];

      [Required(ErrorMessage = "The 'Photo' list is required in supported extensions.")]
      [RegularExpressions(@"^\.\w+$", ErrorMessage = "Must be a valid file extension starting with a dot.")]
      public List<string> Photo { get; set; } = [];
   }

   public class FFmpegSettings
   {
      public string? Path { get; set; }

      [Required(ErrorMessage = "The 'TimeoutMilliseconds' property is required for FFmpeg settings.")]
      [Range(1, int.MaxValue, ErrorMessage = "TimeoutMilliseconds must be a positive integer.")]
      public int TimeoutMilliseconds { get; set; }

      public string? HardwareAcceleration { get; set; }

      public FFmpegTranscoderSettings? Encoder { get; set; }

      public FFmpegTranscoderSettings? Decoder { get; set; }
   }

   public class FFmpegTranscoderSettings
   {
      [StringLength(32, ErrorMessage = "Video length must not exceed 32 characters.")]
      public string? Video { get; set; }

      [StringLength(32, ErrorMessage = "Audio length must not exceed 32 characters.")]
      public string? Audio { get; set; }

      [StringLength(32, ErrorMessage = "Subtitle length must not exceed 32 characters.")]
      public string? Subtitle { get; set; }
   }

   public class YtDlpSettings
   {
      public string? Path { get; set; }

      [Required(ErrorMessage = "The 'Options' list is required for yt-dlp settings.")]
      public required List<string> Options { get; set; } = [];
   }

   public class SolrSettings
   {
      [Required(ErrorMessage = "The 'URL' property is required for Solr settings.")]
      public required string URL { get; set; }
   }

   public class ScannerSettings
   {
      [Required(ErrorMessage = "The 'WatchedFolders' list is required for scanner settings.")]
      public required List<string> WatchedFolders { get; set; } = [];

      [Required(ErrorMessage = "The 'Folders' list is required for scanner settings.")]
      public required List<string> Folders { get; set; }

      [Required(ErrorMessage = "The 'IgnoredPatterns' list is required for scanner settings.")]
      public required List<string> IgnoredPatterns { get; set; }

      public bool StartupScan { get; set; } = false;
      public bool StartupUpdate { get; set; } = false;
      public bool StartupCleanup { get; set; } = false;
      public bool ForceGenerateMissingThumbnails { get; set; }

      [Required(ErrorMessage = "The 'PeriodicScanIntervalMilliseconds' is required for scanner settings.")]
      [Range(0, uint.MaxValue, ErrorMessage = "TimeoutMilliseconds must be an integer.")]
      public uint PeriodicScanIntervalMilliseconds { get; set; }

      [Required(ErrorMessage = "The 'ParallelScannerTasks' is required for scanner settings.")]
      [Range(1, int.MaxValue, ErrorMessage = "TimeoutMilliseconds must be a positive integer.")]
      public int ParallelScannerTasks { get; set; }
   }

   public class MountSettings
   {
      [Required(ErrorMessage = "The 'Types' is required for mount settings.")]
      public required string Types { get; set; }

      public required string? Options { get; set; }

      [Required(ErrorMessage = "The 'Device' is required for mount settings.")]
      public required string Device { get; set; }

      [Required(ErrorMessage = "The 'Folder' is required for mount settings.")]
      public required string Folder { get; set; }
   }

   public class SecuritySettings
   {
      [Required(ErrorMessage = "The 'Library' is required for security settings.")]
      public required SecurityLibrarySettings Library { get; set; }

      [Required(ErrorMessage = "The 'Settings' is required for security settings.")]
      public required SecuritySettingsSettings Settings { get; set; }
   }

   public class SecurityLibrarySettings
   {
      [Required(ErrorMessage = "The 'ReadOnly' is required for library security settings.")]
      public required bool ReadOnly { get; set; }
   }

   public class SecuritySettingsSettings
   {
      [Required(ErrorMessage = "The 'ReadOnly' is required for security settings settings.")]
      public required bool ReadOnly { get; set; }
   }

   public class LoggingSettings
   {
      [Required(ErrorMessage = "The 'LogLevel' configuration is required for logging settings.")]
      public required LogLevelSettings LogLevel { get; set; }
   }

   public class LogLevelSettings
   {
      [Required(ErrorMessage = "The 'Default' log level is required.")]
      public required string Default { get; set; }

      [Required(ErrorMessage = "The 'Microsoft' log level is required.")]
      public required string Microsoft { get; set; }

      [Required(ErrorMessage = "The 'MicrosoftAspNetCoreSpaProxy' log level is required.")]
      public required string MicrosoftAspNetCoreSpaProxy { get; set; }

      [Required(ErrorMessage = "The 'MicrosoftHostingLifetime' log level is required.")]
      public required string MicrosoftHostingLifetime { get; set; }
   }
}
