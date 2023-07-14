# LorAuto

A bot for the game [Legends of Runeterra](https://playruneterra.com/en-us/) written in C#. It can play the game on a very superficial level.
**LorAuto** are Heavily inspired by this project [LoR-Bot](https://github.com/MOj0/LoR-Bot).

## Prerequisites

- [.NET Desktop Runtime 7 x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.9-windows-x64-installer).
  - You can use winget to install it `winget install Microsoft.DotNet.DesktopRuntime.7`.
- Game running full-screen at 1920x1080 resolution and at least medium quality, with Windows display scaling set to 100%.
- In-game third party endpoints enabled on port 21337 (should be on by default) Or pass the port using (-p option).
- Game option `Enable AUTOPASS` checked(true).

## How to run

- Download **LorAuto** latest release from here [releases](https://github.com/CorrM/LorAuto/releases).
- Run the game the way it is specified in the Prerequisites section.
- Run `LorAuto.exe` to start bot.
  - You can run `LorAuto.exe -h` to get more information about arguments.

Bot will then navigate through menus and always select the first deck in your collection and select the play button.
You can favorite the deck you want to use, or prepend the deck's name with a '.' (that way it will appear first on the list).

If you would like to obtain control of your mouse, hold down the `Ctrl` key. It may not work instantly, but it will stop in time.

If you would like to quit the application, you can press the `Ctrl-C` combination in the terminal.

## Credits

- [InputSimulatorStandard](https://github.com/GregsStack/InputSimulatorStandard)
- [Emgu CV](https://github.com/emgucv/emgucv)
- [IDisposableAnalyzers](https://github.com/DotNetAnalyzers/IDisposableAnalyzers)
- [P/Invoke](https://github.com/dotnet/pinvoke)
