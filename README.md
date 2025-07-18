# AutoLegalityMod

A specialized ALM fork for [PokeBot](https://github.com/hexbyt3/PokeBot) (SysBot.NET) - dedicated to legalizing Pokémon within the trade bot ecosystem.

## Overview

This fork is specifically maintained for SysBot.NET integration and is **not intended as a standalone PKHeX plugin**.

### Credits

- **Fork Owner**: [@hexbyt3](https://github.com/hexbyt3)
- **Original Project**: [@architdate](https://github.com/architdate) & [@kwsch](https://github.com/kwsch)
- **Fork Source**: [santacrab2's PKHeX-Plugins](https://github.com/santacrab2/PKHeX-Plugins)

## Prerequisites

- Visual Studio 2022 or compatible .NET IDE
- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

## Build Instructions

### Standard Build

```bash
git clone https://github.com/hexbyt3/AutoLegalityMod.git
cd AutoLegalityMod
```

1. Open the solution in Visual Studio
2. Right-click the solution → **Rebuild All**
3. Find the compiled library at:
   ```
   PKHeX.Core.AutoMod\bin\...\net9.0\PKHeX.Core.AutoMod.dll
   ```

> **Note**: If the build fails due to incompatible NuGet package dependencies, use the bleeding edge build method from the original repository.

## Contributing

We welcome contributions! Please:

- Submit pull requests following the existing code style
- Test your changes thoroughly
- Provide clear commit messages

All contributions are greatly appreciated!

## License

This project inherits the license from the original PKHeX-Plugins repository.
