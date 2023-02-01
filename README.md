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
- > Experimental Web UI.

## Known Issues

- > macOS ARM64 (Apple Silicon) builds were not signed, for the moment, please use the X64 build.
- > While using the X64 build on macOS, the serial port may not work.
- > RetroNet support, including Cloud CP/M is experimental
- > NHACP support is experimental.
- > The MagicalMysteryPacket confirms the GetStatus message, as documented, is backwards. The adaptor is sending status not asking for it.
- > I'm 100% sure there are more.

## Systen Requirements

- OS: Windows, macOS, or Linux
- CPU: Average x64, arm, or arm64 CPU. (I've tested on a Raspberry Pi 3,4 and it works well.)
- Memory: 30MB base, 10MB per emulated adaptor, it uses less in my tests.

Realistically, a Pi 3 can serve a dozen or so adaptors, and a Pi 4 can handle 20+
A PC can potentially serve hundreds.

## Configuration

```json
{
  "Settings": {
    "Adaptors": {
      "Serial": [
        {
          "Port": "COM3",                 // Port, COM for Windows, /dev/... for Linux/macOS
          "Source": "Local Cycle 1",      // The name of the source, defined below
          "StoragePath": "./Files"        // The storage root, where Retronet looks for files
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
        "Name": "Local Cycle 2EX",        // Source Name
        "Path": "./PAKs/cycle2ex"         // Source Path
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

## Special Thanks

- DJ Sures: For his work on Nabu.Ca
- Leo Binkowski: for preserving all that sweet hardware and software
- DKGrizzley: for his PICO emulator to fill in the parts I couldn't figure out
- York University: for their recreation efforts, they are both numerous and awesome
- RetroNET and Discord Chaters (in no particular order):
  - Sark
  - Nath (The legend who decrypted NPAK files)
  - Brijohn (NABU MAME!!)
  - Worm
  - Hans23 (Creator of the HCCA-ACP spec)
  - Guidol
  - VTTCP
  - HungryMarmot
  - Wormetti
- And many many more

## Join us on Discord

The NABU Community is already coming together, join us!

> [NABU PC](https://discord.gg/NgxTXvND2A)
