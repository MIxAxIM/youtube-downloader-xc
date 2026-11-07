# Settings reference

Open **Settings** from the main window. Changes can be applied with **Apply**, confirmed with **OK**, or discarded with **Cancel**. **Restore defaults** resets the current tab.

## General

| Setting | Meaning |
| --- | --- |
| Theme | System, light, or dark |
| Language | UI language |
| Auto-update | Check for updates on startup (stable builds) |
| Add new cards on top | Insert pasted items at the top of the queue |
| Start downloads when ready | Auto-start when options finish loading |
| Remove completed cards | Drop finished cards from the queue |
| Load full playlist when pasting a video with a playlist link | See [queue-and-playlists.md](queue-and-playlists.md) |
| Add copied YouTube links | Watch the clipboard for YouTube URLs |
| Minimize to tray | Hide to tray instead of closing the window |
| History retention | Days to keep history (`0` = forever, max `30`) |
| Export / Import settings | Backup or restore settings JSON |
| Clear thumbnail cache | Free cached poster images |
| FFmpeg path | Override the FFmpeg binary location |

## Download

| Setting | Meaning |
| --- | --- |
| Simultaneous downloads | Parallel download limit |
| Video folder / Audio folder | Default output directories |
| Use audio folder for video | Single shared output folder |
| Playlist subfolder | Nest files under the playlist title (video / audio toggles) |
| Play when complete | Open the file after download |
| Pause on metered network | Hold downloads on metered connections (where detected) |

## Network

| Setting | Meaning |
| --- | --- |
| Use proxy | Enable HTTP/SOCKS proxy |
| Protocol / Host / Port | Proxy endpoint |
| Username / Password | Optional proxy auth |
| Connections per download | Parallel connections for a single file |
| Test proxy | Connectivity check |

## Audio

| Setting | Meaning |
| --- | --- |
| File name template | Pattern for audio file names |
| Skip existing files | Do not re-download if the target path exists |
| Remove emoji from names | Strip emoji from titles used in paths |
| Normalize loudness | FFmpeg loudnorm pass |
| Inject alternative languages | Embed extra audio tracks when available |

## Video

| Setting | Meaning |
| --- | --- |
| File name template | Pattern for video file names |
| Inject subtitles | Mux available captions |
| Save subtitles as `.srt` | Also write side-car subtitle files |
| Prefer higher bitrate | Bias stream selection toward higher bitrate |
| SponsorBlock | Skip or cut categories via SponsorBlock |

## Tags

| Setting | Meaning |
| --- | --- |
| Inject media tags | Write title/artist/album/etc. |
| Year source | How the year tag is chosen |
| Default album artist | Fallback album artist |
| Write artwork | Embed cover art |
| Description as comment | Store the description in the comment tag |
| Playlist album tags | Treat playlist metadata as album fields |
| Use channel as artist | Prefer the channel name as artist |

## Authorization

| Setting | Meaning |
| --- | --- |
| Persist authentication | Keep login cookies after restart |
| Import cookies from Chrome / Edge | Pull YouTube cookies from a local browser profile |

Sign-in itself is done from the main window auth button (WebView dialog), not from this tab.
