# NABU Adaptor Emulator

This is an Emulator for the NABU network adapter for use with real NABU PCs and Emulators.
The adaptor emulator is complete in that it handles all known messages from the NABU.

`This is a work in progress, and may contain bugs, issues, poor code, etc.`

## Known Issues

- It can only load PAK files from the official recreation currently, 

## Configuration

All of these can be hard set as command line arguments as `--Section:Item=VALUE`

### Service

- Serial (true/false) : Enables or Disables Serial Listener
- TCP (true/false) : Enables or Disables TCP Listener

### Serial

- Port: The name (or path) or the serial port
- Other settings you shouldn't mess with unless you have to

### TCP

- Port (default 5816)

### Adaptor

- ChannelPrompt (true/false): Enables or disables the channel prompt on NABU
- Channel (default 0): the channel used by the adaptor

### Network

- Source: The key of the source to use from the Sources section
- Channel: the name of the specific program from that source to run

### Sources

SOURCENAME:

- Type (Folder, NabuRetroNet): The type of source
- ListUrl: the list url for this source (* NabuRetroNet only)
- NabuRoot: the root for NABU files
- PakRoot: the root for PAK files (* NabuRetroNet only)

### Logging

This is the standard dotnet logging section.

## Special Thanks

- DJ Sures: for his tireless work on the official recreation
- Leo Binkowski: for stealing all that stuff from NABU and preserving it
- DKGrizzley: for his PICO emulator to fill in the parts I couldn't figure out
- RetroNET Chaters (in no particular order):
  - Sark
  - Nath (The legend who decrypted NPAK files)
  - Worm
  - Hans
  - Guidol
  - VTTCP
