# LorAuto

This project is a bot specifically designed for the popular card game, [Legends of Runeterra](https://playruneterra.com/en-us/).
The bot utilizes image processing techniques to recognize the current game state and update card information in real-time.

**LorAuto** are Heavily inspired by this project [LoR-Bot](https://github.com/MOj0/LoR-Bot).

if you think the structure of this project are overkill please check [Disclaimer](#disclaimer).

## Prerequisites

- [.NET Desktop Runtime 7 x64](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.9-windows-x64-installer).
  - You can use winget to install it `winget install Microsoft.DotNet.DesktopRuntime.7`.
- Game running full-screen at 1920x1080 resolution and at least medium quality, with Windows display scaling set to 100%.
- In-game third party endpoints enabled on port 21337 (should be on by default) Or pass the port using (-p option).
- Game option `Enable AUTOPASS` checked(true).

## Features

### Real-time card update

Real-time card updates as the bot dynamically adjusts card attributes, such as Attack, Health and ~Keywords~, during gameplay.

No longer fully confined to static data provided by the game client.

### Plugin System

Unleash your creativity and adaptability with our bot. Create strategies to fit your deck play-style,
allowing you to implement unique strategies that align perfectly with your card-set.
The bot also provides a plugin system that allows you to develop and integrate your own custom strategies seamlessly using C#.

With the plugin system, you can extend the bot capabilities and design tailored gameplay approaches, unlocking endless possibilities for your bot performance.
Contribute to the bot strategy library by developing and sharing your plugins with the community, fostering a collaborative environment of strategy exploration and innovation.

Note that the bot comes with a built-in strategy `generic` play style, but you have the flexibility to create and customize your own strategies using the plugin system.

Feel free to experiment, enhance, and customize the bot behavior with your own strategies using the provided plugin system.

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

## Disclaimer

**This project is a personal project undertaken during my free time with the aim of enhancing my skills in various fields.**
**Contributions are welcome from the community to help improve the project.**
**However, please note that this project is provided as-is and does not come with any guarantees or warranties.**
**Use it at your own discretion and risk.**
