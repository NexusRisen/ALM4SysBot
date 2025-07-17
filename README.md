# About  
This project uses `PKHeX.Core` and PKHeX's `IPlugin` interface to add enhancements to the PKHeX program, namely **Auto**mated **Mod**ifications to simplify creation of legal Pokémon.
This Fork is owned by [@hexbyt3](https://github.com/hexbyt3)
The original project is owned by [@architdate](https://github.com/architdate) (Discord: thecommondude#8240) and [@kwsch](https://github.com/kwsch) (Discord: Kurt#6024).

## Building  
This project requires an IDE that supports compiling .NET based code, such as Visual Studio 2022, and the [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).

**Regular Builds**  
Regular builds will usually succeed unless there are changes that are incompatible with the NuGet [PKHeX.Core](https://www.nuget.org/packages/PKHeX.Core) package dependency specified in the `.csproj` files of the projects. If building fails, use the bleeding edge method instead.

- Clone the PKHeX-Plugins repository using: `$ git clone https://github.com/architdate/PKHeX-Plugins.git`.
- Right-click on the solution and click `Rebuild All`.
- These DLLs should be placed into a `plugins` directory where the PKHeX executable is.
   - The compiled DLL `AutoModPlugins.dll` for AutoLegality will be in the `AutoLegalityMod\bin\Release\net8.0-windows` directory.

## Usage  
To use the plugins:
- Create a folder named `plugins` in the same directory as PKHeX.exe.
- Put the compiled plugins from this project in the `plugins` folder. If you downloaded the plugins from online, you will need to unblock them.
- Start PKHeX.exe.
- The plugins should be available for use in `Tools > Auto Legality Mod` drop-down menu.

## Contributing
To contribute to the repository, you can submit a pull request to the repository. Try to follow a format similar to the current codebase. All contributions are greatly appreciated! If you would like to discuss possible contributions without using GitHub, please contact us on the support server above. 

Please ensure you run the unit tests prior to submitting a pull request to the repository. 

