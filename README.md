# Process killer #

Simple Windows Service to monitor and kill hanging processes.

## Usage ##

1. Build with Visual Studio or MSBuild in Release mode.
2. Open `bin/Release/Terminator.exe.config` and specify process names to watch.
3. Run `bin/Release/install.bat` as admin to install the service.
4. Run `bin/Release/uninstall.bat` to uninstall the service.

## Configuration ##

See `Terminator.exe.config` in the output folder.

- `ProcessNames` – a list of processes to watch, separated by semicolon, file names without extension, for example `"java;notepad"`
- `CheckTimerInterval` – how often the service timer runs, in milliseconds
- `KillAfter` – how long can the process run before being killed, in milliseconds