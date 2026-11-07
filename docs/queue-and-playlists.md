# Queue and playlists

## Paste flow

1. **Paste link** reads the clipboard (or you submit the query box).
2. A temporary loading card is created for each query line.
3. The resolver classifies the input as video, playlist, channel, or search.
4. Ready cards load download options (formats, captions, optional MusicBrainz tags).
5. Nothing is written to disk until you start a download.

## Video URLs that include a playlist

Many watch links look like:

```text
https://www.youtube.com/watch?v=VIDEO_ID&list=PLAYLIST_ID
```

Behavior is controlled by **Settings → General → Load full playlist when pasting a video with a playlist link**:

| Setting | Result |
| --- | --- |
| **On** | Resolve the playlist and add every video |
| **Off** | Resolve only `VIDEO_ID` and add that one video |

## Explicit playlist links

A URL that is primarily a playlist (for example `playlist?list=...`) still resolves as a playlist when there is no single video id to prefer. Use the expand setting mainly for `watch?v=...&list=...` links.

## Channel and search

Channel and search queries can return many videos. Those results expand into multiple queue cards after resolution.

## Duplicates and limits

- Duplicate video ids already in the queue are skipped (with a snackbar notice).
- If a max queue size is configured, new cards beyond that limit are not added.

## Starting downloads

- Per-card download buttons start one item.
- **Download all** starts every card in the Ready state.
- Parallelism follows **Simultaneous downloads** in Settings.
- Output folders respect video/audio mode, playlist subfolders, skip-existing, and unique path generation.
