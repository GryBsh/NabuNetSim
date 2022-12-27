# NABU Adaptor Emulator

This is an Emulator for the NABU network adapter for use with real NABU PCs and Emulators,
and can host multiple instances of either. Making it possible to host a NABU Network from one
Computer. The adaptor emulator is complete in that it handles all known messages from the NABU.
But it's not guaranteed for any purpose, blah, blah, blah.

`This is a work in progress, and may contain bugs, issues, poor code, etc.`

## Known Issues

- None, but I'm 100% sure there are some.

## Configuration

Can be set via command line arguments, in the usual dotnet way.

### Sources

- SOURCENAME:
  - Type (Folder, NabuRetroNet): The type of source
  - ListUrl: the list url for this source (* NabuRetroNet only)
  - NabuRoot: the root for NABU files
  - PakRoot: the root for PAK files (* NabuRetroNet only)

### Adaptors (Any number you'd like)

- Type: Serial or TCP
- Port: Either a name or path for Serial OR a number for TCP
- Enabled: Enables or disables the Adaptor
- ChannelPrompt: Prompts for the user for the AdaptorChannel (for authenticity purposes only)
- AdaptorChannel: The channel used by the adapter (for authenticity purposes only)
- Source: The source to pull channels and segments from
- Channel: The channel to send segments from
- BaudRate (Serial Only, default: 111865): The send/receive rate of the serial adapter.

### Logging

This is the standard dotnet logging section.

## Special Thanks

- DJ Sures: for his tireless work on the official recreation
- Leo Binkowski: for stealing all that stuff from NABU and preserving it
- DKGrizzley: for his PICO emulator to fill in the parts I couldn't figure out
- York University: for their recreation efforts, they are both numerous and awesome
- RetroNET and Discord Chaters (in no particular order):
  - Sark
  - Nath (The legend who decrypted NPAK files)
  - Brijohn (NABU MAME!!)
  - Worm
  - Hans
  - Guidol
  - VTTCP
  - HungryMarmot
