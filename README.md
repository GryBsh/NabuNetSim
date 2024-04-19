# NABU NetSim

![NABU NetSim](./Assets/ui.png)

> NOTE: `NNS` is used to abbreviate `NABU NetSim` in this document.

## What is it?

This is an Emulator for the NABU network adapter for use with real NABU PCs and Emulators (Marduk, MAME),
and can host multiple instances of either. Making it possible to host a whole NABU Network from one
adaptor. It supports the Classic NABU protocol as well as NHACP and RetroNet, and can be extended to support your own custom protocols via JavaScript.

> This is a work in progress, and may contain bugs, issues, poor code, etc.

## Stand Out Features

- > Supports multiple NABU adaptors, serial and TCP. You can use multiple serial adaptors and listen for clients on multiple ports at the same time if you so choose.
- > Supports NAPA packages, so you can drop in content updates, without a restart
- > Supports local file system NABU files and cycles, with the classic cycles included
- > Supports feeds from NABUNetwork.com and Nabu.ca.
- > Supports NHACP and Retronet, so it can run IshkurCPM and Cloud CPM.
- > A Fancy NABU Launcher, which can launch programs right from your NABU!
- > Supports RetroNET headless too
- > Deeply integrated offline caching of remote files/programs.
- > Web UI for configuration, with news from NabuNetwork.com.
- > Extensible protocol support, you can add your own protocol handlers in Python or Javascript.

## Whats New(ish)

- > Stability improvements in the NABU Launcher
- > NHACP bug fixes
- > Settings editor in the web ui
- > RetroNET Headless Support 
- > Precaching Support

## Known Issues

- > macOS ARM64 (Apple Silicon) builds were not signed, for the moment, please use the X64 build.
- > While using the X64 build on macOS, the serial port may not work.
- > JavaScript support is experimental.
- > I'm 100% sure there are more.

## System Requirements

Console:

- OS: Windows, macOS, or Linux
- CPU: Average x64, arm, or arm64 CPU. (I've tested on a Raspberry Pi 3,4 and it works well.)
- Memory: 100MB base, 10MB per emulated adaptor, it uses less in my tests.

Web:

- Memory: 300MB minimum, 512MB-1GB recommended.

Realistically, a Pi 3 can serve a dozen or so adaptors, and a Pi 4 can handle 20+
A PC can potentially serve hundreds.

## Basics

Download the latest release from Github, and extract it to a folder. Run the executable, and open your browser to `http://localhost:5000`.

## Advanced

### Docker (Linux)

Preliminary Docker support is available, but is not extensively tested. It #WorksOnMyMachine.

```bash
  docker build -f "./Nabu.NetSimWeb/Dockerfile" --force-rm -t nnswui:dev .
  docker run -d \
    -p 5000:80 \
    -p 5816:5816 \
    -v /path/to/NABUs:/app/NABUs \ # for NABU files
    -v /path/to/Files:/app/Files \ # for NHACP/Retronet files
    -v /path/to/logs:/app/logs \   # Optional - for Logs, you can still view them in the container.
    -v /path/to/cache:/app/cache \ # Optional - for remote file caching.
    -v /dev/ttyUSB0:/dev/ttyUSB0 \ # Optional - for serial port(s)
    --name fabulous-falconer \
    --restart unless-stopped \
    nnswui:dev
```

## No hardware? No Problem

NABU PC can be emulated with MAME, and the standalone NABU emulator [Marduk](https://github.com/buricco/marduk)

## Come chat with us!
- [NABU Discord](https://discord.gg/NgxTXvND2A)

## Special Thanks

- [Leo Binkowski](https://www.youtube.com/@leo.binkowski) : for preserving all that sweet hardware and software.
- DKGrizzley: for his PICO emulator to fill in the parts I couldn't figure out
- York University: for their recreation efforts, they are both numerous and awesome

- BriJohn: [NABU Mame](https://github.com/brijohn/mame/tree/nabupc_wip)
- GTAMP: [NABU MAME Windows Builds](https://gtamp.com/nabu)
- The great folks at the [NABU Discord](https://discord.gg/NgxTXvND2A)
- And many many more

## Join us on Discord

The NABU Community is already coming together, join us!

> [NABU PC](https://discord.gg/NgxTXvND2A)
