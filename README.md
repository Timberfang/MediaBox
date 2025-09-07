# MediaBox

A wrapper for [FFmpeg](https://ffmpeg.org) and [libvips](https://www.libvips.org) for video, audio, and image transcoding. Written in C#. Designed for media libraries.

> ⚠️ This project is an **early** work-in-progress. It is not stable software; features may be added or remove without warning, and bugs are to be expected. Use at your own risk.

## Features

- Powered by FFmpeg and libvips
- Easy-to-use command-line interface (CLI)
- Designed for bulk transcoding
- Automatic cropping
- Low overhead
- Cross-platform

## Known Limitations

Both FFmpeg and libvips are *very* powerful tools. MediaBox only provides a thin layer of abstraction on top of them. Since it was specifically designed for transcoding (converting between media formats), it *only* supports those features. The vast majority of FFmpeg and libvips functionality will not be accessible. Should you want access to the full feature set of either FFmpeg or libvps, you should use those directly.

Other limitations include:

1. Quality/speed/size settings can only be changed via the built-in presets.
2. Custom file names are not supported.
3. When MediaBox exits, FFmpeg will *not* exit until it finishes with the file it's working on.

## Installation

1. Download FFmpeg and extract it to a location on your system. Add the `bin` directory of the FFmpeg installation to your system's PATH environment variable.
2. Download MediaBox and extract it to a location on your system. The archive includes all dependencies besides FFmpeg.
3. Open a terminal or command prompt and navigate to the extracted MediaBox directory.
4. Run the command `MediaBox --help` to verify that the installation was successful.

## Usage

```cmd
MediaBox.exe transcode -t <type> -p <path> -d <destination>
```

- `transcode`: The command to transcode media files.
- `-t <type>`: The type of media to transcode. Must be one of `video`, `audio`, or `image`.
- `-p <path>`: The path to the input media file or directory of media files.
- `-d <destination>`: The path to the output directory.

## Contributing

**Please do not submit pull requests**. This project is hosted on my private [Forgejo](https://forgejo.org) server and mirrored to GitHub. Any pull requests merged on GitHub will be automatically overwritten the next time Forgejo pushes changes to the repository.

## Sample Files

Should you want to test the software on sample data, a [collection](https://drive.proton.me/urls/DJ3PVE9Y8G#42fEmnay6H3Z) of media is available. This media, created by Kevin MacLeod and the Blender Foundation, is, to the best of my knowledge, made available under Creative Commons licenses. For attribution information, consult the `ThirdPartyNotices.txt` file included with the media bundle.

Should any author(s) of the provided media wish to have it removed, please notify me by email at <timberfang.code@pm.me>.

## License

MediaBox is provided under the GNU GPLv3. See the [LICENSE](LICENSE) file for details.
