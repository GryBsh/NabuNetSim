# NABU NetSim

![NABU NetSim](./Assets/ui.png)

> NOTE: `NNS` is used to abbreviate `NABU NetSim` in this document.

This is an Emulator for the NABU network adapter for use with real NABU PCs and Emulators (Marduk, MAME),
and can host multiple instances of either. Making it possible to host a whole NABU Network from one
adaptor. It supports the Classic NABU protocol as well as NHACP and RetroNet, and can be extended to support 
your own custom protocols via JavaScript and Python.

> This is a work in progress, and may contain bugs, issues, poor code, etc.

**IF YOU ARE ON A VERSION BEFORE 0.9.8: BACKUP YOUR CONFIG FILES** and drop in the one from the current version. Then apply your customizations. Many of the sources previously in the config file have been moved to packages.

## Stand Out Features

- > Supports multiple NABU adaptors, serial and TCP. You can use multiple serial adaptors and listen for clients on multiple ports at the same time if you so choose.
- > Supports NAPA packages, so you can drop in content updates, without a restart
- > Supports local file system NABU files and cycles, with the classic cycles included
- > Supports feeds from NABUNetwork.com and Nabu.ca.
- > Supports NHACP and Retronet, so it can run IshkurCPM and Cloud CPM.
- > Deeply integrated offline caching of remote files/programs.
- > Optional Web UI for configuration, with news from NabuNetwork.com.
- > Extensible protocol support, you can add your own protocol handlers in Python or Javascript.

## Whats New(ish)

- > NAPA Package support, content is now provided as NAPA packages, so they can be updated seperately.
![Package Storage](./Assets/storage.png)
    Content from packages is symlinked into storage by default, but files users will alter are copied, to avoid clobbering.
- > User Storage Isolation
  - > This allows users to have their own copy of local storage files, and will avoid clobber issues with RetroNet and NHACP.
  ![Storage](./Assets/isolated-storage.png)
- > Hybrid IskurCPM support, allows physical disk drives
- > RetroNet TCP Client/Server Support
  - > Telnet/RetroNet Chat/etc are working.
- > Revamped Web UI Log Viewer, with pagination and search
  - > This does mean higher memory usage, but it's worth it.
- > JavaScript support has switched from Jint to ClearScript V8.
  - > This means support for all JavaScript Features like modules and the ability to use compile and use Typescript.
- > Storage redirection support for NHACP and RetroNet, local and remote files are supported.
  - > Useful for disk images, etc. This will enable future features like Client Storage Isolation.
- > 98% of all byte arrays are now dealt with using `Span<T>` and `Memory<T>` for better performance, the fastest adaptor is now even faster!

## Known Issues

- > macOS ARM64 (Apple Silicon) builds were not signed, for the moment, please use the X64 build.
- > While using the X64 build on macOS, the serial port may not work.
- > RetroNet support is experimental.
- > Only one client way open and use the RetroNet TCP Server at a time, because RetroNet was designed that way.
- > NHACP support is experimental.
- > Python support is experimental, and probably doesn't work on non-Windows platforms.
- > JavaScript support is experimental.
- > I'm 100% sure there are more.

## System Requirements

Console:

- OS: Windows, macOS, or Linux
- CPU: Average x64, arm, or arm64 CPU. (I've tested on a Raspberry Pi 3,4 and it works well.)
- Memory: 100MB base, 10MB per emulated adaptor, it uses less in my tests.

Web:

- Memory: 250MB minimum, 512MB-1GB recommended.

Realistically, a Pi 3 can serve a dozen or so adaptors, and a Pi 4 can handle 20+
A PC can potentially serve hundreds.

## Basics

### Choose you weapon

Nabu NetSim is available in 2 flavors:

- Console Service App (Nabu.Netsim / nns)
- Web UI (Nabu.NetsimWeb / nns-wui)

The console service app runs headless, and runs in a set configuration. Configure, start, and go. This is ideal for in place installations, where the sources and adaptors needed are known and static.
The Web UI is a web based interface, that allows you to configure the service on the fly, to a large degree. It also lets you manipulate individual sources for TCP clients post connection.

Both have the same set of features, but for most regular users, the Web UI is recommended.

**Each release package starts with `nns`, for headless, or `nnsweb` for the Web UI. Choose the one for your desired platform/architecture**

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
        "EnableRetroNet": true,           // Enable RetroNet support for this source, false by default
        "EnableRetroNetTCPServer": false, // Enable RetroNet TCP Server support for this source, false by default
        "RetroNetTCPServerPort": 5815,    // The port to listen on for RetroNet TCP Server, 5815 by default
        "StorageRedirects": {             // Storage Redirects for NHACP/Retronet, case sensitive
          "C.dsk": "my/disk.dsk"
        }
      }
    ],
    // Optional Extensible Protocol Support
    "Protocols": [
      {
        "Path": "test.js",                // Protocol Path
        "type": "javascript",             // Protocol Type, javascript or python
        "Commands": [ 131 ]              // Commands handled by this protocol
      },
    ],
    "EnableJavaScript": false,            // Support for JavaScript custom protocols, disabled by default
    "EnablePython": false,                // Support for Python custom protocols, disabled by default, experimental

    // Other Settings
    "EnableLocalFileCache": true,         // Enable local file caching of HTTP/HTTPS files, enabled by default

    // Web UI Settings
    "MaxLogEntryAgeHours": 12,            // How long to keep log entries in the UI, in hours, 12 hours by default
    "LogCleanupIntervalMinutes": 15,      // How often to clean up old log entries, in hours, 15r by default
    "CacheDatabasePath": "cache.db",      // The path to the UI cache database, relative to the working directory
    "DatabasePath": "data.db",            // The path to the backend database, relative to the working directory
  },
  // Web UI Settings
  "Urls": "http://*:5000"                 // The URL to listen on, * for all, *:5000 by default
}
```

## Advanced

### Changing the hostname, port (Web UI)

Add the following property to the appsettings.json config file:

```json
  "Urls": "http://*:5000"
```

To change the hostname, replace `*` with the hostname or IP address you want to use.
To change the port number, replace `5000` with the port number you want to use.

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

## What's in the pipe (Coming Soon, in no particular order)

- > NabuNetwork.com Headless Support
  - > This will allow you to use the on NABU browser/launcher with NNS


## No hardware? No Problem

NABU PC can be emulated with MAME, and the standalone NABU emulator [Marduk](https://github.com/buricco/marduk)

## Brought to you by

- [NabuNetwork.com](https://nabunetwork.com)
- [NABU Discord](https://discord.gg/NgxTXvND2A)!

## Special Thanks

- [Leo "The Undisputed God-Legend" Binkowski](https://www.youtube.com/@leo.binkowski) : for preserving all that sweet hardware and software.
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
