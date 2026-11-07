# Features

Youtube Downloader XC is a desktop Avalonia app for pulling media from YouTube into a local queue and downloading it with FFmpeg.

## Adding media

- Paste a link from the clipboard (**Paste link**) or type a query in the query box
- Supported inputs: single video URL, playlist URL, channel URL, and keyword search
- Multi-line paste adds one queue card per line
- Optional auto-add when a YouTube link is copied to the clipboard

## Queue

- Each item is a card with title, channel, status, format, and progress
- Ready cards can be started one-by-one or with **Download all**
- Pause / resume individual downloads or from the tray menu
- Compact mode, selection, context menu actions (open folder, copy link, save audio, download again)
- Optional max queue size, add-on-top ordering, and auto-remove completed cards
- Session persistence for unfinished work where enabled

## Video and audio

- Choose container / format and quality preference
- Audio mode with bitrate preference and optional loudness normalization
- Separate default folders for video and audio (or use the audio folder for both)
- Playlist items can be saved under a playlist-named subfolder

## Enrichment

- Inject subtitles (and optional `.srt` export)
- Inject alternative-language audio tracks
- Media tags via MusicBrainz lookup, artwork, album/artist helpers
- SponsorBlock categories when enabled
- Optional emoji stripping from file names and skip-if-file-exists

## Network and auth

- Proxy (protocol, host, port, credentials) with a test button
- Connections-per-download limit and global speed limit
- YouTube sign-in through an embedded WebView (private profiles)
- Import cookies from Chrome / Edge when available
- Persist auth cookies across restarts (encrypted in settings)

## App chrome

- Custom title bar (minimize / maximize / close)
- System tray with open, pause/resume, and quit
- Settings in a separate window with tabbed categories
- Download history dialog
- Themes (system / light / dark) and multiple UI languages
- Auto-update for stable builds (disabled on development builds)

## Related docs

- [Settings reference](settings.md)
- [Queue and playlists](queue-and-playlists.md)
- [Building](building.md)
