# BeatSaber.SongHashing
A collection of utilities for generating Beat Saber beatmap hashes.

## Usage
This library's primary purpose is the `IBeatmapHasher` interface and the `Hasher` implementation that goes with it.
`IBeatmapHasher` exposes three functions:
* `HashResult HashDirectory(string songDirectory, CancellationToken cancellationToken)`
* `HashResult HashZippedBeatmap(string zipPath, CancellationToken cancellationToken);`
* `long QuickDirectoryHash(string songDirectory)`
  * This function should return a `long` that matches what SongCore generates for its quick directory hashes.
  
A HashResult consists of:
* `HashResultType ResultType`
  * `Success` - Beatmap was hashed successfully.
  * `Warn` - Beatmap was hashed with a warning (such as a missing difficulty file).
  * `Error` - Beatmap could not be hashed.
  * `Canceled` - Hashing was canceled by the `CancellationToken`.
* `string? Hash` - If successful, the hash (in uppercase) of the beatmap. Returns null if none of the beatmap was hashed.
  * Will still return a hash (with the `Warn` `HashResultType`) if the beatmap is missing one or more of the expected difficulty files.
* `string? Message` - May contain a description of any errors or warnings that occurred while hashing.
* `Exception? Exception` - Any `Exception` thrown will be stored here.
