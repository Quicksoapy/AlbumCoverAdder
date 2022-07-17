# AlbumCoverAdder 

AlbumCoverAdder is a console application which adds album covers to music files missing one.

## Installation

Build it in your c# IDE.

### Requirements

- FFMPEG.exe in the folder.
- .NET 6

## Usage

I highly recommend backing up/copying whatever you edit using this program, this program is still fairly experimental.

0. make sure the metadata of album artists and albums are correct in your files. The program uses these to determine the correct album cover.
1. input the directory which contains the music files.
2. input your last.fm API key and secret
3. choose wether you'd like a new directory of the new files, or to replace them automatically (not recommended without back-up)
