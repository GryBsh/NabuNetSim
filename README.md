# NABU NetSim

This is an Emulator for the NABU network adapter for use with real NABU PCs and Emulators (MAME),
and can host multiple instances of either. Making it possible to host a NABU Network from one
Computer. The adaptor emulator is complete in that it handles all known messages from the NABU.
But it's not guaranteed for any purpose, blah, blah, blah.

> This is a work in progress, and may contain bugs, issues, poor code, etc.

## Known Issues

- > macOS ARM64 (Apple Silicon) builds were not signed, for the moment, please use the X64 build.
- > While using the X64 build on macOS, the serial port is not working, also a signing issue.
- > The RetroNet Telnet app is supported, but no other parts. And it's experimental.
- > HCCA-ACP support is experimental.
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

### Sources

- SOURCENAME:
  - Type (Folder, NabuRetroNet): The type of source
  - ListUrl: the list url for this source (* NabuRetroNet only)
  - NabuRoot: the root for NABU files
  - PakRoot: the root for PAK files

### Adaptors (Any number you'd like)

- Type: Serial or TCP
  - > Want something else? Open an issue and I'll see what I can do.
- Port: Either a name or path for a Serial port OR a number for TCP port.
  - > To use multiple instances of MAME, you'll need to start each one listening
    on a different port. Then define an adapter for that port.
- Enabled: Enables or disables the Adaptor
- AdaptorChannel: The channel used by the adapter (for authenticity purposes only)
  - > This will cause you NABU to show the channel prompt if set to 0, otherwise we
  simulate signal lock regardless of channel.
- Source: The source to pull channels and segments from
  - > From the sources defined above.
- Channel: The channel to send segments from.
  - > The file name, sans .nabu, for NABU files, or the folder name for PAK files.
- BaudRate (Serial Only): The send/receive rate of the serial adapter.
  - > The "correct" rate is `111865`. However, you cannot set this baudrate on
  macOS or Linux witht the dotnet's `System.IO.Ports`. So we're using a default of `115200`.
  If you experience issues, try setting this to `111865` if your on Windows or .
  - > !! This has no effect on the TCP Adaptor !!
- SendDelay: The delay in iterations between each byte sent in "Slower Send" mode, for RetroNet.
  - > For Serial the default is `500`, for TCP it is `130000`.
  - > I don't know why MAME is 260x slower than the real thing, but it is.

> !! It can run as many adaptors as you're system can handle. But you'll need to configure them all. !!

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
