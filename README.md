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

1. Codecs are currently hard-coded for AV1 video and OPUS audio.
2. Quality/speed/size settings can only be changed via the built-in presets.
3. Custom file names are not supported.
4. When MediaBox exits, FFmpeg will *not* exit until it finishes with the file it's working on.

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

## License

MediaBox is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
