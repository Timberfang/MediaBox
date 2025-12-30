# MediaBox

*Manage your digital media.*

> [!WARNING]
> This project is not stable software; features may be added or remove without warning, and bugs are to be expected. Use at your own risk.

## Features

- Easy-to-use, cross-platform command-line interface (CLI)
- Transcode (convert/format/encode) video, audio, and images in bulk
- Create backups of DVDs and Blu-rays
- Compatible with most common formats thanks to [FFmpeg](https://ffmpeg.org), [libvips](https://www.libvips.org), and [MakeMKV](https://www.makemkv.com/)

## Known Limitations

MediaBox is designed to support media libraries like [Plex](https://www.plex.tv/) or [Jellyfin](https://jellyfin.org/) by automating common media processing tasks. While FFmpeg and libvips are very powerful tools, MediaBox acts as a wrapper for a subset of their full functionality. This includes encoding video, audio, and images using reasonable default settings and creating backups of optical disks, converting all tracks into `.mkv` files using. Should additional features from these programs be required, consider using those programs directly.

## Installation

1. Download FFmpeg and/or MakeMKV and extract them to a location on your system. Add the appropriate directories to your system's PATH environment variable.
2. Download MediaBox and extract it to a location on your system. The archive includes all dependencies besides FFmpeg and MakeMKV.
3. Open a terminal or command prompt and navigate to the extracted MediaBox directory.
4. Run the command `mediabox --help` to verify that the installation was successful.

## Usage

```cmd
mediabox transcode <type> <path> <destination>
```

- `transcode`: The command to transcode media files.
- `<type>`: The type of media to transcode. Must be one of `video`, `audio`, or `image`.
- `<path>`: The path to the input media file or directory of media files.
- `<destination>`: The path to the output directory.

Run `mediabox --help` to see a full list of features and commands.

## Contributing

**Please do not submit pull requests**. This project is hosted on my private [Forgejo](https://forgejo.org) server and mirrored to GitHub. Any pull requests merged on GitHub will be automatically overwritten the next time Forgejo pushes changes to the repository.

## License

MediaBox is provided under the GNU GPLv3. See the [LICENSE](LICENSE) file for details.
