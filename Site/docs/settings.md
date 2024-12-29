---
outline: deep
---

# Settings

This page describes the different options and ways you can configure and customize your **Arcadeia** instance.

## Advanced Settings
When you run **Arcadeia** for the first time, an empty `JSON` file called `appsettings.Production.json` is created. This file can override the default settings defined in `appsettings.json`. From here on we describe what each section does and how one can configure it.

### Defaults
The default settings are bundled with the application through the `appsettings.json` file:

```json
<!--@include: ../../appsettings.json-->
```

### Session
The Session section allows you to configure how browser sessions are managed in **Arcadeia**. These settings determine how long session data is retained and when it expires due to inactivity. Proper configuration of session settings is essential to ensure both optimal performance and security.

#### IdleTimeoutSeconds:
This setting specifies the maximum amount of time (in seconds) a browser session can remain idle before its contents are automatically discarded. If the session is accessed during the idle period, the timer resets, effectively extending the session’s lifespan.

### Thumbnails
Configures how and what thumbnails are generated for each type of media file, and where they are stored.

#### Database
The thumbnails are stored in a SQLite database and the database is located in a named docker volume called `data`.

##### Name
Configures the name of the database file.

**Default:** `Thumbnails.sqlite`

##### Path
Configures the path where the thumbnails database is stored inside the `/var/lib/app/` folder. Any modifications to this path requires that the bind volume be updated inside the `docker-compose-production.yml` file.

**Default:** `data`

#### Video
Describes what thumbnails should be generated for the supported video files. Currently the following thumbnails are generated and stored for each video file:

1. **Large:** (480xH)
2. **Medium:** (320xH)
3. **Small:** (160xH)
4. **Sprite:** 60 * (160xH)
5. **T:** 24 * (480x272)

##### Name
The name of a thumbnail field or the prefix of each indexed thumbnail field in a thumbnails database row.

##### Settings
1. **Count (integer):** In case of an indexed field such as `T` or a Sprite, specifies how many thumbnails should be extracted from the entire length of a video file. In case of an indexed field, the fields in the thumbnails database will be called `T0`, `T1`, `T2`, ...
2. **Width (integer):** The maximum width of the generated thumbnail. If omitted, the `Height` determines the size and the `Width` will be adjusted to preserve the aspect ratio.
3. **Height (integer):** The maximum height of the generated thumbnail. If omitted, the `Width` determines the size and the `Height` will be adjusted to preserve the aspect ratio.
4. **Sprite (boolean):** Determines whether the field is a Sprite consisting of a `Count` of thumbnails lined up in a single image and field.
5. **Crop (boolean):** Determines if the thumbnails are to be cropped to a `16:9` aspect ratio.

#### Photo
Describes what thumbnails should be generated for the supported photo files. Currently the following thumbnails are generated and stored for each photo file:

1. **Large:** (480xH)
2. **Medium:** (320xH)
3. **Small:** (160xH)
4. **T:** 1 * (480x272)

##### Name
The name of a thumbnail field or the prefix of each indexed thumbnail field in a thumbnails database row.

##### Settings
1. **Count (integer):** This is not quite applicable in case of photo files but is used to keep the `T` thumbnails consistent with video files and as such, only one of them is generated.
2. **Width (integer):** The maximum width of the generated thumbnail. If omitted, the `Height` determines the size and the `Width` will be adjusted to preserve the aspect ratio.
3. **Height (integer):** The maximum height of the generated thumbnail. If omitted, the `Width` determines the size and the `Height` will be adjusted to preserve the aspect ratio.
4. **Crop (boolean):** Determines if the thumbnails are to be cropped to a `16:9` aspect ratio.

### Streaming
**Arcadeia** supports transcoding and streaming video files that are not directly supported by web browsers to a format that is. Some of the parameters of these operations can be tweaked.

#### Segments
To make this work, a longer video file is divided into segments of fixed size. The browser first asks for a list of all the segments which is provided to it in a `.m3u8` playlist and starts requesting individual segments and buffering them. Each requested segment is then transcoded and streamed to the web browser on the fly.

##### Duration (integer)
Determines the duration of each segment in seconds.

**Default:** `10`

### SupportedExtensions

#### Audio (array)
Accepts an array of audio file extensions including a dot in the beginning.

> [!NOTE]
> Audio files are currently not supported and this setting has no effect.

#### Video (array)
Accepts an array of video file extensions including a dot in the beginning.

#### Photo (array)
Accepts an array of photo file extensions including a dot in the beginning.

### FFmpeg
The FFmpeg utilities are bundled in the Docker image and are used for the following purposes:

1. Extracting video file properties and metadata.
2. Generating thumbnails and sprites from video files.
3. Transcoding video files for streaming purposes.

#### Path (string)
Specifies where the `ffmpeg` and `ffprobe` executables are located.

**Default:** `/usr/bin`

#### TimeoutMilliseconds (integer)
Specifies the timeout in milliseconds after which any FFmpeg utility execution is forcefully terminated in case it takes an unusually long time to return.

**Default:** `30000`

#### HardwareAcceleration (string)
Specifies what hardware acceleration method should be used when calling `ffmpeg` through its `-hwaccel` argument. You can read more about this argument [here](https://trac.ffmpeg.org/wiki/HWAccelIntro).

**Default:** `null`

#### Encoder
Specifies the encoders to use when transcoding video files.

##### Video (string)
Specifies which video encoder to use when transcoding video files.

**Default:** `libx264`

##### Audio (string)
Specifies which audio encoder to use when transcoding video files.

**Default:** `aac`

##### Subtitle (string)
Specifies which subtitle encoder to use when transcoding video files.

> [!NOTE]
> Subtitles are currently not supported and this setting has no effect.

#### Decoder
Specifies the decoders to use when transcoding video files.

> [!TIP]
> FFmpeg is designed to automatically detect the appropriate decoder based on the input file’s format and codec. It's hard to imagine a scenario where one might need to tweak these.

##### Video (string)
Specifies which video decoder to use when transcoding video files.

##### Audio (string)
Specifies which audio decoder to use when transcoding video files.

##### Subtitle (string)
Specifies which subtitle decoder to use when transcoding video files.

> [!NOTE]
> Subtitles are currently not supported and this setting has no effect.

### YtDlp
[yt-dlp](https://github.com/yt-dlp/yt-dlp) is a feature-rich command-line audio/video downloader with support for thousands of sites. When a URL is dropped inside the Upload Zone and the URL is of a supported site, it is passed on to yt-dlp to download and add the video file to the media library.

#### Path (string)
Specifies where the `yt-dlp` executable is located.

**Default:** `/usr/local/bin`

#### Options (array)
Additional [options](https://github.com/yt-dlp/yt-dlp?tab=readme-ov-file#usage-and-options) to be passed to the `yt-dlp` executable.

### Solr
An Apache Solr core is the backbone of the media library, used for indexing and storing media file information. The core is then used in the user interface to browse and search media files.

#### URL (string)
The URL of the Solr core. The default core that is used and initialized at startup is called `Library`.

**Default:** `http://solr:8983/solr/Library`

### Scanner
The Scanner service is responsible for scanning specified folders for new or modified media files, indexing media file information, generating thumbnails and updating the media library.

#### StartupScan (boolean)
If enabled, the Scanner service will scan the available [Folders](#folders-array) and [WatchedFolders](#watchedfolders-array) at startup and insert or update the new and modified media filers to the media library. Startup Scanning does not remove deleted files from the media library.

**Default:** `true`

#### StartupUpdate (boolean)
If enabled, the Scanner service will go through the media library and update or remove the media files available in [Folders](#folders-array) and [WatchedFolders](#watchedfolders-array). Startup Update detects and removes the deleted files from the media library.

**Default:** `true`

#### StartupCleanup (boolean)

> [!NOTE]
> Startup Cleanup is not yet implemented and currently this setting has no effect.

**Default:** `false`

#### ForceGenerateMissingThumbnails (boolean)
Forces regeneration of any missing thumbnails upon access, startup scanning or startup update.

**Default:** `false`

#### PeriodicScanIntervalMilliseconds (integer)
Adjusts the interval in milliseconds at which a Periodic Scan is performed or disables it altogether. A Periodic Scan works the same way as a [Startup Scan](#startupscan-boolean) but is triggered periodically in order to keep the media library updated automatically.

**Default:** `3600000`

#### ParallelScannerTasks (integer)
Adjusts the number of parallel tasks spawned during [Startup Scan](#startupscan-boolean) and [Startup Update](#startupupdate-boolean).

**Default:** `4`

#### WatchedFolders (array)
The Scanner service can watch folders for changes and update the media library when media files are added, deleted or modified.

**Default:** `[]`

#### Folders (array)
Specifies the folders to be recursively scanned or updated during [Startup Scan](#startupscan-boolean) and [Startup Update](#startupupdate-boolean).

**Default:** `["/Network", "/Uploads"]`

#### IgnoredPatterns (array)
[Regex](https://en.wikipedia.org/wiki/Regular_expression) patterns that specify file names to be excluded from the media library during [Startup Scan](#startupscan-boolean) or any other operation that adds files to media library.

**Default:**
```json
[
    "\\/\\..*", // Matches hidden files or directories (those that start with a dot .) in paths that use forward slashes.
    "^\\..*$"   // Matches hidden files or directories (those starting with a dot .) at the beginning of the string.
]
```

### Mounts (array)
The FileSystem service is responsible for mounting and dismounting network mounts specified in this array. The arguments are directly passed on to the [mount](https://man7.org/linux/man-pages/man8/mount.8.html) utility.

**Example:**
```json
"Mounts": [
    {
      "Device": "192.168.0.123:/photos",
      "Folder": "/Network/Photos",
      "Options": "nolock",
      "Types": "nfs"
    }
]
```

#### Device (string)
The device name to mount. e.g. in the case of an NFS mount, device may look like `server:/dir`.

#### Folder (string)
The location where the filesystem will be mounted. The folder will be created if it does not exist. By convention, this should be a folder inside `/Network/`.

#### Options (string)
The mount options passed through the `-o, --options` argument.

#### Types (string)
The file system type passed through `-t, --types` argument.

### Security
Manages settings related to security.

#### Library
Manages settings related to library security.

##### ReadOnly (boolean)
Makes the library read-only and thus unmodifiable through external means such as the user interface.

#### Settings
Manages settings related to configuration security.

##### ReadOnly (boolean)
Makes the settings read-only and thus unmodifiable through external means such as the user interface.
