# NABU NetSim

![NABU NetSim](./Assets/ui.png)

This is an Emulator for the NABU network adapter for use with real NABU PCs and Emulators (MAME),
and can host multiple instances of either. Making it possible to host a NABU Network from one
Computer. The adaptor emulator is complete in that it handles all known messages from the NABU.
But it's not guaranteed for any purpose, blah, blah, blah.

> This is a work in progress, and may contain bugs, issues, poor code, etc.

## Whats New

- > Since even Nabu.Ca is using raw cycle files now, NetSim no longer supports encrypted PAK files.
- > Restores support for RetroNet, including new support for Cloud CP/M.
- > Web UI.
- > Experimental Python support.
- > New Look, Same great taste!
- > Docker support.

## Known Issues

- > macOS ARM64 (Apple Silicon) builds were not signed, for the moment, please use the X64 build.
- > While using the X64 build on macOS, the serial port may not work.
- > RetroNet support, including Cloud CP/M is experimental
- > NHACP support is experimental.
- > Python Support is experimental, and probably doesn't work on non-Windows platforms.
- > The MagicalMysteryPacket confirms the GetStatus message, as documented, is backwards. The adaptor is sending status not asking for it.
- > I'm 100% sure there are more.

## Systen Requirements

- OS: Windows, macOS, or Linux
- CPU: Average x64, arm, or arm64 CPU. (I've tested on a Raspberry Pi 3,4 and it works well.)
- Memory: 30MB base, 10MB per emulated adaptor, it uses less in my tests.

Realistically, a Pi 3 can serve a dozen or so adaptors, and a Pi 4 can handle 20+
A PC can potentially serve hundreds.

## Basics

### Choose you weapon

Nabu NetSim is available in 2 flavors:

- Console Service App (Nabu.Netsim / nns)
- Web UI (Nabu.NetsimWeb / nns-wui)

The console service app runs headless, and runs in a set configuration. Configure, start, and go. This is ideal for in place installations, where the sources and adaptors needed are known and static. 
The Web UI is a web based interface, that allows you to configure the service on the fly.

For most regular users, the Web UI is recommended.

### Configuration

```json
{
  "Settings": {
    "Adaptors": {
      "Serial": [
        {
          "Port": "COM3",                 // Port, COM for Windows, /dev/... for Linux/macOS
          "Source": "Local Cycle 1",      // The name of the source, defined below
          "StoragePath": "./Files"        // The storage root, where NHACP/Retronet looks for files
        }
      ],
      "TCP": [
        {
          "Port": 5816,
          "Source": "Local Cycle 1",
          "StoragePath": "./Files" 
        }
      ]
    },
    "Sources": [
      {
        "Name": "Local Cycle 1",        // Source Name
        "Path": "./PAKs/cycle1"         // Source Path
      },
      {
        "Name": "Nabu.Ca",
        "Path": "https://cloud.nabu.ca/HomeBrew/titles/filesv2.txt",
        "EnableRetroNet": true           // Enable RetroNet support for this source
      }
    ]
  }
}
```

## Advanced

### Docker (Linux)

Preliminary Docker support is available, but is not extensively tested. It #WorksOnMyMachine.

```bash
  docker build -f "./Nabu.NetSimWeb/Dockerfile" --force-rm -t nnswui:dev .
  docker run -d \
    -p 5005:80 \
    -p 5816:5816 \
    -v /path/to/NABUs:/app/NABUs \ # for NABU files
    -v /path/to/Files:/app/Files \ # for NHACP/Retronet files
    -v /path/to/logs:/app/logs \   # Optional - for Logs, you can still view them in the container.
    -v /path/to/cache:/app/cache \ # Optional - for remote file caching.
    --name fabulous-falconer \
    --restart unless-stopped \
    nnswui:dev
```

## No hardware? No Problem

NABU PC can be emulated with MAME, and the standalone NABU emulator [Marduk](https://github.com/buricco/marduk)

## Special Thanks

- [Leo "The Undipsuted God-Legend" Binkowski](https://www.youtube.com/@leo.binkowski) : for preserving all that sweet hardware and software.
- DKGrizzley: for his PICO emulator to fill in the parts I couldn't figure out
- York University: for their recreation efforts, they are both numerous and awesome
- [Geek with Social Skills](https://www.youtube.com/@geekwithsocialskills)
- BriJohn: [NABU Mame](https://github.com/brijohn/mame/tree/nabupc_wip)
- GTAMP: [NABU MAME Windows Builds](https://gtamp.com/nabu)
- RetroNET and Discord Chaters (in no particular order):
  - Sark
  - Nath (The legend who decrypted NPAK files)
  - Guidol
  - VTTCP
  - HungryMarmot
- DJ Sures: For his work on Nabu.Ca
- And many many more

## Join us on Discord

The NABU Community is already coming together, join us!

> [NABU PC](https://discord.gg/NgxTXvND2A)
