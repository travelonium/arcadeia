---
outline: deep
description: Configure Arcadeia scanning, network storage, video previews, transcription, subtitles and playback.
---

# Settings

Use the application's settings screen to configure your library, scanning and playback. For Docker deployments, `appsettings.Production.json` in the installation directory overrides the bundled defaults. `./start production` creates an empty file if one is missing.

Use complete JSON objects when editing this file, and keep an existing configuration's other sections when adding an override. Paths refer to the application container's filesystem unless noted otherwise.

This reference follows the current source configuration; available options can differ in older releases.

## Defaults

These defaults are included directly from the current source configuration:

```json
<!--@include: ../../appsettings.json-->
```

## Settings in the application

Open **Settings** from the application navigation and select a tab from the sidebar. Most controls save when changed; **Network Mounts uses Apply and Reset**. Numeric transcription timeouts save when you leave the field, and sliders save when you release them or finish a keyboard adjustment. Read-only instances disable editing. Error notifications report unsuccessful requests; reload the screen to confirm the effective value after an error.

### Media Scanner tab

| UI control | How to use it | Default / configuration |
| --- | --- | --- |
| **Startup Scan** | Turn ON to find new or modified media whenever the scanner starts. **Start Now** queues a scan immediately without changing this switch. | ON; `Scanner.StartupScan` |
| **Startup Update** | Turn ON to revisit indexed files and remove entries for deleted media when the scanner starts. **Start Now** queues one update. Original media is not deleted. | ON; `Scanner.StartupUpdate` |
| **Startup Cleanup** | Turn ON to remove orphaned thumbnail records at scanner startup. **Start Now** queues one cleanup. It neither rebuilds previews nor deletes original files. | OFF; `Scanner.StartupCleanup` |
| **Force Generate Missing Thumbnails** | Enable backfilling of empty thumbnail slots when files are processed. Existing previews are kept even if dimensions have changed. | OFF; `Scanner.ForceGenerateMissingThumbnails` |
| **Force Detect Missing Subtitles** | Check older indexed videos that have not had embedded subtitle detection. Already checked videos are skipped. | OFF; `Scanner.ForceDetectMissingSubtitles` |
| **Periodic Scan Interval** | Move the slider from OFF to a whole-hour interval, up to 168 hours. OFF disables periodic jobs, while startup and manual scans remain available. | 1 hour; stored as `Scanner.PeriodicScanIntervalMilliseconds` |
| **Parallel Scanner Tasks** | Choose simultaneous scanning tasks, from 1 up to the displayed logical processor count. Increase cautiously for large archives. | 4 in configuration; `Scanner.ParallelScannerTasks` |

Saving a scanner setting restarts the scanner, including watchers and the timer. Enabled startup jobs may run again. Manual buttons show **Starting…** while submitting and **Running…** while the corresponding job runs; scanner status is refreshed periodically. Manual actions are unavailable in library read-only mode or while that action is running.

For example, to fill missing previews, enable **Force Generate Missing Thumbnails**, queue a scan with **Start Now**, and wait for processing to finish. This does not replace existing previews. See [thumbnail regeneration](#thumbnail-fields-and-regeneration) before changing preview dimensions.

### Network Mounts tab

Add an **SMB/CIFS (Windows Share)** or **NFS (Network File System)** mount using the add menu. Each mount has **Basic** and **Advanced** editors.

| UI field | What to enter |
| --- | --- |
| **Folder** | Destination inside the application. In Basic mode, `Photos` becomes `/Network/Photos`, within the default scanned location. Advanced mode accepts the full destination path. Each mount needs a unique folder. |
| SMB **Server** | Server and share, such as `nas/photos`; Basic mode adds the `//` prefix to form `//nas/photos`. |
| SMB **Protocol Version** | Leave `default` to negotiate, or select a version supported by the server. |
| SMB **User Name**, **Password** | Credentials permitted to access the share. They are stored in mount configuration. |
| NFS **Share** | Server and exported path in the separate fields, such as `nas` and `/photos`, forming `nas:/photos`. |
| Advanced **Device** | Full share source: `//nas/photos` for SMB or `nas:/photos` for NFS. |
| Advanced **Options** | Comma-separated mount options, passed to the mount utility. The eye button reveals or conceals the displayed value. |

Select **Apply** to save valid edits and restart mounting. **Reset** discards unapplied edits for that mount. Apply is disabled when there are no changes or the fields fail the UI checks. The red minus button removes the mount configuration immediately; it does not delete files on the remote share. Adding an empty card alone does not save a working mount.

The status dot is green when available, red when an error exists, and dim otherwise. Hover over an error dot for its message. Confirm that the container can reach the server and has the required mounting privileges. Hidden password/options fields do not encrypt stored credentials; settings API output can include them.

### Transcoding tab

| UI control | How to use it | Configuration |
| --- | --- | --- |
| **Hardware Acceleration** | Select a discovered method, or **Disabled** (default). A compatible GPU, driver and container device access may still be needed. | `FFmpeg.HardwareAcceleration` |
| **Video Encoder** | Select an available encoder, or **Default** for the application's automatic choice. Hardware encoders have additional device requirements. | `FFmpeg.Encoder.Video` |
| **Audio Encoder** | Select an available audio encoder, or **Default** for automatic selection. Choose a codec compatible with browser playback. | `FFmpeg.Encoder.Audio` |

Dropdowns reflect the installed FFmpeg build. Choices save immediately and apply to subsequent processing; start new playback to try them. If playback fails after a change, restore **Disabled** and **Default** and inspect the logs. Executable directories, utility timeout, decoders and subtitle encoders are configured outside these dedicated UI controls.

### Transcription tab

| UI control | How to use it | Default / configuration |
| --- | --- | --- |
| **Enabled** | Turn ON to start local speech recognition workers. Processing is queued in the background; it does not complete when the switch changes. | OFF; `Transcription.Enabled` |
| **Model** | Choose Tiny, Base, Small, Medium or Large. **(Downloaded)** marks a model already present. Missing models download when needed; larger models require more resources. | Small; `Transcription.Model` |
| **Language** | Choose the recording's spoken language or **Detect Automatically**. Speech is recognized in its original language, rather than translated. | Automatic; `Transcription.Language` |
| **Timeout** | Enter a positive number of minutes and leave the field to save. Allow more time for long recordings or slow models. | 30 minutes; stored as `Transcription.TimeoutMilliseconds` |
| **Parallel Tasks** | Use the slider to select concurrent videos, from 1 up to the displayed logical processor count. | 1; `Transcription.ParallelTasks` |
| **Catch Up On Startup** | Queue eligible videos still missing transcripts when the workers start, including when enabling transcription live. | ON; `Transcription.CatchUpOnStartup` |

After changing **Parallel Tasks** or **Catch Up On Startup**, switch transcription OFF and ON to apply the new worker setup. Existing transcripts are not automatically replaced when changing model or language. The model dropdown uses the configured model directory; custom directories and executable paths require configuration/API changes.

To try speech search, leave Small and automatic language selected, enable transcription, then allow queued work to finish before searching for words spoken in a video. Supported embedded text subtitles can supply searchable text without speech recognition.

### Logging tab

Each category has a severity dropdown. Selecting a level saves immediately. Levels run from the most detailed **Trace** and **Debug**, through **Information**, **Warning**, **Error** and **Critical**; **None** suppresses messages for that filter.

| UI category | Default | What it controls |
| --- | --- | --- |
| **Default** | Information | Fallback for categories without a more specific setting. |
| **Microsoft** | Warning | Microsoft framework messages. |
| **Microsoft.AspNetCore.SpaProxy** | Information | Development frontend proxy messages. |
| **Microsoft.Hosting.Lifetime** | Information | Application startup and shutdown messages. |

**Recent Logs** displays messages streamed from the application, with timestamp, category and severity. The list follows new entries while at the bottom; scrolling back lets you read older displayed messages. It is not an archive or a separate download of the complete deployment logs. Review messages for media paths and credentials before sharing them. Use Debug temporarily, then restore the usual level. Console formatter settings are configured outside this screen.

### About tab

**About** is read-only information:

- **Version** shows the running build, source commit/date, runtime, operating system and architecture. Source/release links help identify the code actually deployed.
- **Components** reports available tool versions; unavailable components are marked accordingly.
- **License & Legal** explains the AGPL license and links to source code, the license text and the legal guide.
- **Media Rights and Privacy** describes media permissions, local transcription, external downloads and deployment responsibilities.
- **Bundled Tools**, **Server Libraries** and **Web Interface Libraries** link to principal third-party projects and license information.

### Settings without dedicated UI controls

The tabs above do not expose every configuration option. Thumbnail layout and storage, executable directories, scanner folder lists and ignored patterns, supported extensions, streaming segment duration, Solr routing and API read-only flags are configured through the supported configuration/API sections below. Session, host and proxy settings require file or environment configuration. The full defaults and reference on this page cover both UI controls and those additional options.

## Configuration files and updates

The application loads `appsettings.json`, then `appsettings.{Environment}.json`; environment variables are applied after these files. In production the override file is `appsettings.Production.json`. An environment variable such as `Transcription__Enabled=true` overrides the corresponding JSON value, including subsequent changes saved through the settings screen.

The settings API merges nested objects into the writable environment file and replaces arrays in that file. File providers combine configuration by key, including numbered array elements: shortening an override array does not necessarily remove entries inherited from the base file. Check the effective settings after changing lists. Empty arrays in the settings response can appear as empty objects because of the configuration serialization.

The API permits these top-level sections: `Thumbnails`, `Streaming`, `SupportedExtensions`, `FFmpeg`, `Transcription`, `YtDlp`, `Solr`, `Scanner`, `Mounts`, `Security` and `Logging`. `Session`, `AllowedHosts` and `KnownProxies` require file or environment configuration. The writable file must already exist and be writable by the application. Back it up before making changes.

### When changes take effect

| Change | Application behavior |
| --- | --- |
| `Scanner` saved through the API | Restarts the scanner, recreating watchers and its periodic timer; startup actions can run again. |
| `Mounts` saved through the API | Restarts the filesystem service to apply mounts. |
| Scanner or mounts edited directly in a file | Values reload, but running watchers, timers and mounts need an application restart to reliably match the new configuration. |
| `Transcription.Enabled` | Starts or stops transcription workers live after configuration reload. |
| `Transcription.ParallelTasks`, `CatchUpOnStartup` | Used when workers start; disable and re-enable transcription, or restart, to apply changes to an existing worker pool. |
| Transcription model, language, paths and timeouts; FFmpeg, yt-dlp, extensions and security flags | Read by subsequent operations; an operation already running may retain previous values. |
| Thumbnail dimensions and fields | Used during subsequent thumbnail generation; existing images are not automatically rebuilt. |
| Thumbnail database path/name | Database operations read current settings, but changing the location is not a migration. Stop the application and move/back up the database before switching. |
| `Solr.URL`, session timeout and proxy pipeline | Restart the application to reliably update services configured at startup. |
| Logging | Configuration-driven filters can reload; restart after changing console formatter setup if output does not change. |
| Streaming segment duration | Used for subsequent streaming work; restart playback after changing it. |

Restart a Docker deployment with `./stop` followed by `./start production`. These scripts affect the full deployment, so active playback and background jobs will be interrupted.

## Scanner

The scanner indexes photos and videos, extracts their metadata and generates thumbnails.

| Setting | Default | Purpose |
| --- | --- | --- |
| `StartupScan` | `true` | Find new or modified media at startup. |
| `StartupUpdate` | `true` | Revisit indexed media and remove entries for files that have been deleted. |
| `StartupCleanup` | `false` | Remove orphaned thumbnail records whose media IDs are absent from Solr at scanner startup. |
| `ForceGenerateMissingThumbnails` | `false` | Regenerate missing thumbnails when media is accessed or scanned. |
| `ForceDetectMissingSubtitles` | `false` | Check previously indexed videos for embedded subtitle streams that have not been detected. |
| `PeriodicScanIntervalMilliseconds` | `3600000` | Interval between scans, in milliseconds; `0` disables periodic scans. |
| `ParallelScannerTasks` | `4` | Number of concurrent scanning tasks. More tasks increase resource use. |
| `Folders` | `["/Network", "/Uploads"]` | Folders scanned recursively. |
| `WatchedFolders` | `[]` | Folders monitored for filesystem changes. |
| `IgnoredPatterns` | See defaults above | Regular expressions for paths excluded from indexing. |

`Folders` and `WatchedFolders` contain application-visible paths, not host paths. Unavailable directories are skipped with a warning. Watchers include subdirectories; file events can be unreliable on network shares, so periodic scans remain useful. The default ignored patterns exclude hidden paths containing a `/.` component and paths beginning with a dot. Use JSON escaping for backslashes in regular expressions.

The settings screen's scan action queues indexing; update revisits existing index entries; cleanup removes orphaned thumbnail rows. Cleanup does not delete original media or rebuild previews. Library read-only mode rejects these manual actions; it is not a general switch that disables every background service.

Start with a small media sample to check thumbnail generation and playback before scanning a large archive.

## Network storage

The `Mounts` array configures network mounts. Mounts use the container's `mount` utility and must be reachable from the Docker host and container.

For example, this override connects an NFS share:

```json
{
  "Mounts": [
    {
      "Device": "192.168.0.123:/photos",
      "Folder": "/Network/Photos",
      "Options": "nolock",
      "Types": "nfs"
    }
  ]
}
```

Mounting requires suitable container privileges, filesystem tools and network access. Mount errors and availability are reported in the settings screen. Mount options are returned by the settings API without password masking: do not expose this endpoint or share its output if options contain credentials.

Replace the example address and share with your own. `Folder` is the path inside the application container. A folder under `/Network/` is included in the default scanner configuration.

| Property | Meaning |
| --- | --- |
| `Device` | Share or device to mount, such as `server:/photos` for NFS. |
| `Folder` | Container directory where the share will appear. |
| `Options` | Options passed to `mount -o`. |
| `Types` | Filesystem type passed to `mount -t`, such as `nfs`. |

## Transcription

Optional speech transcription uses **whisper.cpp** to process video audio locally and index the transcript for search. It is **disabled by default**. The Docker image includes `whisper-cli` and a small model; a native installation needs its own executable and model.

Enable it in the application's transcription settings, or add this override:

```json
{
  "Transcription": {
    "Enabled": true
  }
}
```

Transcription runs in a background queue. Allow it to finish before searching for spoken words. It is CPU-intensive, and its accuracy depends on the language, recording and model.

| Setting | Default | Purpose |
| --- | --- | --- |
| `Enabled` | `false` | Enable speech transcription and transcript search. |
| `Path` | `null` | Directory containing `whisper-cli`; the process search path is used when unset. |
| `Model` | `/usr/share/whisper/ggml-small.bin` | Model file accessible to the application. |
| `Language` | `auto` | Detect the spoken language or use a configured language code. |
| `TimeoutMilliseconds` | `1800000` | Maximum processing time for one video: 30 minutes. |
| `ParallelTasks` | `1` | Number of concurrent transcription tasks. |
| `CatchUpOnStartup` | `true` | Queue existing videos that are still missing transcripts when transcription starts. |

A missing model file is downloaded on demand from the whisper.cpp model repository on Hugging Face using the configured filename. The model directory must be writable and outbound access must be available; this first download can take longer and requires additional disk space. Use an existing compatible `ggml-*.bin` model file to avoid downloading. Selecting a different model does not automatically replace transcripts already indexed.

`Path` is a directory, not a full executable filename. `Language` is passed to whisper-cli; use a language code supported by the installed executable or `auto`. `CatchUpOnStartup` runs when workers start, including when enabling transcription live. Embedded text subtitles can supply searchable text without running speech recognition.

## Subtitles

Arcadeia detects supported embedded text subtitle streams and converts them to WebVTT for the browser. Image-based DVD and Blu-ray subtitle formats are not supported by this conversion.

`Scanner.ForceDetectMissingSubtitles` checks older indexed videos that have not been checked yet. Speech transcription can provide captions for videos without embedded subtitle streams.

## Thumbnails

Thumbnails are stored in SQLite. In the Docker setup, `Thumbnails.Database.Path` defaults to `data` under `/var/lib/app/`, persisted by the `data` volume; the file is named `Thumbnails.sqlite`.

The defaults generate large, medium and small previews for photos and videos. Videos also receive a sprite and up to 24 indexed `T` frames for dynamic thumbnails. The actual number of frames depends on the media and processing result.

| Property | Meaning |
| --- | --- |
| `Count` | Number of frames requested for an indexed field or sprite. |
| `Width`, `Height` | Pixel dimensions; video uses FFmpeg scaling and photos use an aspect-preserving bounding box unless cropped. |
| `Sprite` | Combine frames into one sprite image. |
| `Crop` | Crop preview frames; see the current per-field defaults above. |

### Thumbnail fields and regeneration

| Setting | Default and behavior |
| --- | --- |
| `Database.Name` | `Thumbnails.sqlite`; SQLite filename combined with `Database.Path`. |
| `Database.Path` | `data`; relative to the application's working directory. Changing it does not copy existing data. |
| `Video`, `Photo` | Dictionaries of named thumbnail fields. Preserve the standard names expected by the UI. |
| `Audio` | Empty dictionary in the settings class; standalone audio generation is not implemented. |
| `Width`, `Height` | Omitted dimensions default to `-1`. Use positive pixel dimensions for predictable sizing. Photos preserve aspect ratio unless cropped; omitted dimensions use source dimensions as the bounding geometry. |
| `Count` | Omitted value is `0`, which still generates one image under the field name. A positive value produces indexed frames; video counts are capped at whole seconds of duration, with at least one generation attempt for a nonzero video. Photo counts repeat generation of the same source image. |
| `Sprite` | Defaults to `false`. For video, combines sampled frames into a sprite stored under the field name. Photo generation does not use this flag. |
| `Crop` | Defaults to `false`. With cropping enabled, photo previews fill the requested area and are cropped centrally; video generation scales first, then limits crop height to the configured height, or to a 16:9 height when no positive height is supplied. Specifying both video dimensions scales to that exact size and can alter the source aspect ratio. |

Default video fields are `Large` (480 px wide), `Medium` (320), `Small` (160), `Sprite` (up to 60 frames at 160 px wide), and `T` (up to 24 frames at 480 × 272). Default photo fields have the same three widths and one `T` image at 480 × 272. Video sample positions are distributed across the duration. The defaults JSON above is the authoritative field configuration.

Changing dimensions, count or cropping does not replace populated thumbnail slots. `ForceGenerateMissingThumbnails=true` fills missing slots when files are processed; it does **not** force regeneration of existing slots. Modified media with a changed checksum triggers forced generation. There is no documented settings switch to rebuild every existing thumbnail; do not delete the database as a routine resize step. Keep a backup and use a tested maintenance workflow if rebuilding is necessary.

If you change the database directory, update the Docker volume configuration to persist the new location.

## Playback and FFmpeg

FFmpeg and FFprobe extract media properties, generate thumbnails and transcode videos for browser playback. They are bundled in the Docker image.

`Streaming.Segments.Duration` defaults to `10` seconds. Transcoded playback uses an HLS playlist whose segments are generated as the browser requests them.

| FFmpeg setting | Default | Purpose |
| --- | --- | --- |
| `Path` | `null` | Directory containing `ffmpeg` and `ffprobe`; the process search path is used when unset. |
| `TimeoutMilliseconds` | `30000` | Timeout for FFmpeg utility operations. |
| `HardwareAcceleration` | `null` | Optional FFmpeg hardware acceleration method. |
| `Encoder.Video`, `Encoder.Audio`, `Encoder.Subtitle` | `null` | Optional encoder overrides. |
| `Decoder.Video`, `Decoder.Audio`, `Decoder.Subtitle` | `null` | Optional decoder overrides. |

The defaults leave encoder and decoder choices unset. Configure hardware acceleration only when the host, container and codec support the selected method.

## Supported extensions

`SupportedExtensions.Video` and `SupportedExtensions.Photo` list the accepted filename extensions, including their leading dots. See the defaults above for the current list.

An accepted extension does not guarantee that every codec inside that format will play directly in a browser. FFmpeg handles supported transcoding paths. Standalone audio files are not currently supported; the audio extension list is empty.

## URL imports

Dropping a supported media URL into the upload area uses **yt-dlp** to download and index the video. `YtDlp.Path` defaults to `null` for executable discovery, and `YtDlp.Options` accepts additional command-line options.

`YtDlp.Path` is the directory containing `yt-dlp`. `Options` defaults to `[]`; entries are joined with spaces into its argument string, so preserve any quoting needed for values containing spaces. Extra options can conflict with the application’s own download arguments.

Downloads can fail when a source restricts access or changes its site. Import only media you have permission to download and store.

## Solr

`Solr.URL` defaults to `/solr/Library`. The application resolves this to the bundled Solr service in Docker, and the browser accesses it through the proxy. Solr stores the searchable media index; it does not store your original video files. An absolute `http://` or `https://` URL selects an external endpoint. A relative `/solr/` path is resolved against `http://solr:8983` inside Docker or `http://localhost` outside Docker. Ensure that browser/proxy routing also reaches the intended core; changing the application URL alone does not configure the proxy.

## Session

`Session.IdleTimeoutSeconds` defaults to `600` seconds. Accessing a session resets its idle timer. Browser sessions are not user authentication.

## Security

`Security.Library.ReadOnly` disables library modifications through the application API. `Security.Settings.ReadOnly` disables edits through the settings API. Both default to `false`. Setting settings read-only prevents changing it back through that API; edit the configuration file or environment to unlock it.

These settings do not add authentication or protect every exposed service. Follow the [deployment guidance](/docs/index.html#deployment-status) and restrict access to your instance as a whole.


## Logging

Logging sends messages to the application log stream and console. Category filters control verbosity; more verbose levels can produce large logs.

| Key | Default | Purpose |
| --- | --- | --- |
| `Logging.LogLevel.Default` | `Information` | Fallback for categories without a more specific filter. |
| `Logging.LogLevel.Microsoft` | `Warning` | Filter for Microsoft framework categories. |
| `Logging.LogLevel.Microsoft.AspNetCore.SpaProxy` | `Information` | SPA proxy messages. The dotted name is a literal JSON key within `LogLevel`. |
| `Logging.LogLevel.Microsoft.Hosting.Lifetime` | `Information` | Application startup/shutdown messages. |
| `Logging.Console.FormatterName` | `simple` | Console formatter; the application also selects the simple formatter during startup. |
| `Logging.Console.FormatterOptions.TimestampFormat` | `[yyyy-MM-dd HH:mm:ss] ` | Timestamp pattern, including the trailing space. |

Levels are `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical` and `None`. Use `Debug` temporarily to investigate an issue, then restore a less verbose filter. Review logs for filenames, paths or mount details before publishing them.

## Host filtering and reverse proxies

| Setting | Default | Meaning |
| --- | --- | --- |
| `AllowedHosts` | `*` | Host filtering configuration. `*` allows all hosts; a restricted value is a semicolon-separated list of hostnames without schemes or port numbers. It does not provide authentication or configure listen addresses. |
| `KnownProxies` | `[]` | IP address strings intended to identify trusted reverse proxies. Hostnames and CIDR ranges are not accepted by the current `IPAddress.Parse` code. |

**Current implementation limitation:** startup registers configured proxy addresses in dependency-injection options, but the production pipeline passes a separate `ForwardedHeadersOptions` instance to the middleware. Consequently, do not assume that adding an address to `KnownProxies` changes the active trust list. The production middleware processes `X-Forwarded-For` and `X-Forwarded-Proto` subject to that instance's defaults. Proxy trust needs an implementation fix and deployment verification before relying on additional proxies. Development does not install this production forwarded-header middleware.

Configure these settings in the file or environment, then restart. Neither setting secures Solr or other published services, enables TLS, or replaces deployment access controls.

## Value constraints and error handling

Use the types shown in the defaults: JSON booleans for switches, integer numbers for durations/counts and arrays of strings for lists. `null` is meaningful for optional executable directories and codec overrides. A required value must remain present in the effective configuration, even when only a partial override is saved.

| Value | Constraint in the settings definitions |
| --- | --- |
| `Session.IdleTimeoutSeconds` | Integer from 1 to 2147483647. |
| `FFmpeg.TimeoutMilliseconds`, `Transcription.TimeoutMilliseconds`, `Transcription.ParallelTasks`, `Scanner.ParallelScannerTasks` | Integer from 1 to 2147483647. |
| `Scanner.PeriodicScanIntervalMilliseconds` | Unsigned integer from 0 to 4294967295; 0 disables the timer. Large values can still exceed runtime timer limits; use practical intervals. |
| FFmpeg encoder/decoder names | Nullable strings, at most 32 characters in the settings definitions. Availability depends on the installed FFmpeg build. |
| Supported extension entries | Definition requires a leading dot followed by word characters, such as `.mp4`. Use lowercase values matching the default conventions. |
| Thumbnail dimensions/counts and streaming duration | No explicit range annotations. Use positive dimensions, nonnegative counts and a positive segment duration; a lack of validation does not make arbitrary values usable. |
| Model, database name/path, Solr URL and required lists/sections | Required configuration values; paths and endpoints must also work in the running environment. |

Options are bound and validated during startup and reload, but data-annotation validation is not uniformly recursive across nested objects. The settings POST endpoint checks the top-level whitelist and writes changes before all consumers necessarily validate or use them. A successful save is not proof that executables, models, codecs, mounts or endpoints work. Invalid changes can cause startup or subsequent operations to fail; restore the override file from backup if needed.

## Read-only status and discovered capabilities

The settings response also includes computed values. They are status information, not settings to save:

- `System.ProcessorCount`: logical processor count.
- `System.FFmpeg.HardwareAcceleration` and `System.FFmpeg.Codecs`: capabilities discovered from FFmpeg. These are cached; restart after replacing FFmpeg to refresh them.
- `System.Transcription.Models`: model filenames currently present in the configured model directory, refreshed when settings are requested.
- `Mounts[].Available` and `Mounts[].Error`: mount status.
- `Scanner.Scanning`, `Scanner.Updating` and `Scanner.Cleaning`: current job status.

The top-level `System` object is rejected by the settings POST whitelist. Submit configuration values rather than copying the entire GET response back unchanged.
