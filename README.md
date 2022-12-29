# NABU Adaptor Emulator

This is an Emulator for the NABU network adapter for use with real NABU PCs and Emulators (MAME),
and can host multiple instances of either. Making it possible to host a NABU Network from one
Computer. The adaptor emulator is complete in that it handles all known messages from the NABU.
But it's not guaranteed for any purpose, blah, blah, blah.

> This is a work in progress, and may contain bugs, issues, poor code, etc.

## Known Issues

- > The purpose of the `Magical Mystery Message` is still unknown. It's sent by the NABU PC
  with one value when it's first connecting: `0x8F|0x05`, then when the program is
  first started another: `0x0F|0x05`. It seems to be signaling something, but what?
- > I'm 100% sure there are more.

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
  - > The default is 111865 on Windows, which is the actual rate of the NABU Adaptor.
    On Linux / macOS, the default is 115200, due to an issue with the dotnet serial port library
    not accepting the non-standard rate. It seems to work better this way on Windows too,
    and may become the default on all platforms in the future.
  - > !! This has no effect on the TCP Adaptor !!
- SendDelay: The delay in iterations between each byte sent in "Slower Send" mode.
  - > For Serial the default is 500, for TCP it is 200,000.
  - > I don't know why MAME is 400x slower than the real thing, but it is.

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
  - Hans23
  - Guidol
  - VTTCP
  - HungryMarmot
  - Wormetti
- And many many more

## Join us on Discord

The NABU Community is already coming together, join us!

> [NABU PC](https://discord.gg/NgxTXvND2A)
