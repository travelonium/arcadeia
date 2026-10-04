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

## Scanner

The scanner indexes photos and videos, extracts their metadata and generates thumbnails.

| Setting | Default | Purpose |
| --- | --- | --- |
| `StartupScan` | `true` | Find new or modified media at startup. |
| `StartupUpdate` | `true` | Revisit indexed media and remove entries for files that have been deleted. |
| `StartupCleanup` | `false` | Reserved; cleanup is not implemented. |
| `ForceGenerateMissingThumbnails` | `false` | Regenerate missing thumbnails when media is accessed or scanned. |
| `ForceDetectMissingSubtitles` | `false` | Check previously indexed videos for embedded subtitle streams that have not been detected. |
| `PeriodicScanIntervalMilliseconds` | `3600000` | Interval between scans, in milliseconds; `0` disables periodic scans. |
| `ParallelScannerTasks` | `4` | Number of concurrent scanning tasks. More tasks increase resource use. |
| `Folders` | `["/Network", "/Uploads"]` | Folders scanned recursively. |
| `WatchedFolders` | `[]` | Folders monitored for filesystem changes. |
| `IgnoredPatterns` | See defaults above | Regular expressions for paths excluded from indexing. |

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
| `Path` | `null` | Location of `whisper-cli`; automatic discovery is used when unset. |
| `Model` | `/usr/share/whisper/ggml-small.bin` | Model file accessible to the application. |
| `Language` | `auto` | Detect the spoken language or use a configured language code. |
| `TimeoutMilliseconds` | `1800000` | Maximum processing time for one video: 30 minutes. |
| `ParallelTasks` | `1` | Number of concurrent transcription tasks. |
| `CatchUpOnStartup` | `true` | Queue existing videos that are still missing transcripts when transcription starts. |

## Subtitles

Arcadeia detects supported embedded text subtitle streams and converts them to WebVTT for the browser. Image-based DVD and Blu-ray subtitle formats are not supported by this conversion.

`Scanner.ForceDetectMissingSubtitles` checks older indexed videos that have not been checked yet. Speech transcription can provide captions for videos without embedded subtitle streams.

## Thumbnails

Thumbnails are stored in SQLite. In the Docker setup, `Thumbnails.Database.Path` defaults to `data` under `/var/lib/app/`, persisted by the `data` volume; the file is named `Thumbnails.sqlite`.

The defaults generate large, medium and small previews for photos and videos. Videos also receive a sprite and up to 24 indexed `T` frames for dynamic thumbnails. The actual number of frames depends on the media and processing result.

| Property | Meaning |
| --- | --- |
| `Count` | Number of frames requested for an indexed field or sprite. |
| `Width`, `Height` | Maximum dimensions; aspect ratio is preserved when one dimension is omitted. |
| `Sprite` | Combine frames into one sprite image. |
| `Crop` | Crop preview frames; see the current per-field defaults above. |

If you change the database directory, update the Docker volume configuration to persist the new location.

## Playback and FFmpeg

FFmpeg and FFprobe extract media properties, generate thumbnails and transcode videos for browser playback. They are bundled in the Docker image.

`Streaming.Segments.Duration` defaults to `10` seconds. Transcoded playback uses an HLS playlist whose segments are generated as the browser requests them.

| FFmpeg setting | Default | Purpose |
| --- | --- | --- |
| `Path` | `null` | Location of the executables; automatic discovery is used when unset. |
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

Downloads can fail when a source restricts access or changes its site. Import only media you have permission to download and store.

## Solr

`Solr.URL` defaults to `/solr/Library`. The application resolves this to the bundled Solr service in Docker, and the browser accesses it through the proxy. Solr stores the searchable media index; it does not store your original video files.

## Session

`Session.IdleTimeoutSeconds` defaults to `600` seconds. Accessing a session resets its idle timer. Browser sessions are not user authentication.

## Security

`Security.Library.ReadOnly` disables library modifications through the application API. `Security.Settings.ReadOnly` disables edits through the settings API. Both default to `false`.

These settings do not add authentication or protect every exposed service. Follow the [deployment guidance](/docs/index.html#deployment-status) and restrict access to your instance as a whole.
