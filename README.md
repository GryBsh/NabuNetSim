# NABU NetSim

This is an Emulator for the NABU network adapter for use with real NABU PCs and Emulators (MAME),
and can host multiple instances of either. Making it possible to host a NABU Network from one
Computer. The adaptor emulator is complete in that it handles all known messages from the NABU.
But it's not guaranteed for any purpose, blah, blah, blah.

> This is a work in progress, and may contain bugs, issues, poor code, etc.

## Known Issues

- > macOS ARM64 (Apple Silicon) builds were not signed, for the moment, please use the X64 build.
- > While using the X64 build on macOS, the serial port may not work.
- > The RetroNet Telnet app is supported, but no other parts. And it's experimental.
- > NHACP support is experimental.
- > The purpose of the `Magical Mystery Message` is still unknown. It's sent by the NABU PC
  with one value when it's first connecting: `0x8F|0x05`, then when the program is
  first started another: `0x0F|0x05`. It seems to be signaling something, but what?
- > I'm 100% sure there are more.

## Systen Requirements

- OS: Windows, macOS, or Linux
- CPU: Average x64, arm, or arm64 CPU. (I've tested on a Raspberry Pi 3,4 and it works well.)
- Memory: 30MB base, 10MB per emulated adaptor, it uses less in my tests.

Realistically, a Pi 3 can serve a dozen or so adaptors, and a Pi 4 can handle 20+
A PC can potentially serve hundreds.

## Configuration

Can be set via command line arguments, in the usual dotnet way.

### Adaptors

#### All Types

- Port: the name or path to the serial port to use.
- Source: the name of the source to use for the adaptor.
- Image: if the source contains multiple images, this is the one to use.
- Enabled: if the adaptor should be enabled or not. (Default: true)
- AdaptorChannel: setting this to 0 will show the channel prompt. (Default: 1)

#### Serial Only

- BaudRate: the baud rate to use for the serial port. (Default: 115200)
- ReadTimeout: the read timeout for the serial port. (Default: 1000)

### Sources

- Name: the name of the source.
- Path: the path to the source, either folder path or base URL.

### Logging

This app supports the standard dotnet logging section. Various components logging levels can be changed
this way.

> The default is `Information, Error, Warning` for all components.

## Special Thanks

- DJ Sures: for his tireless work on the official recreation
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
