# batch-audio-extractor
A simple WPF app leveraging FFmpeg to extract audio tracks from .mp4 files and export them as .wav files.

There are simpler ways of going about extracting audio tracks from video files, but I figured this would be a fun project so I decided to do it anyways.
This app was mainly meant to be an introduction to WPF, as a result, the functionality is a bit limited and is not fully fleshed out.

---

## FFmpeg
This app requires FFmpeg to be installed. You can download it [here](https://ffmpeg.org/), but I recommend installing it using [Chocolatey](https://community.chocolatey.org/).
Just install Chocolately and use the `choco install ffmpeg` command to install FFmpeg on your machine.
I'm pretty sure I forgot to check if FFmpeg is install in the code, so if you don't install it correctly the app will likely just give you an obscure error when attempting to use it.
