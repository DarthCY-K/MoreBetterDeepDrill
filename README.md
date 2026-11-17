# More Better Deep Drill

Adds ranged, large multi-operator, and archotech deep drills to RimWorld 1.5, 1.6, and 1.7.

## Workshop upload

Run `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Prepare-WorkshopUpload.ps1` to export a clean runtime package and a SteamCMD upload configuration. See [上传说明](docs/workshop-upload.md). The script prepares files locally; uploading is a separate command.

## Build the 1.7 core assembly

Requirements:

- .NET SDK 10.0 or later.
- RimWorld 1.7, with `Assembly-CSharp.dll` and `UnityStubs.dll` in the game directory.
- Harmony's `1.7/Assemblies/0Harmony.dll`, installed in the same Steam library for automatic path detection.

From the repository root, run:

```powershell
dotnet build "Source\1.7\More Better Deep Drill\MoreBetterDeepDrill.csproj" -c Release
```

The project targets `net10.0` and builds into `bin\Release\net10.0` before copying only the mod DLL to `1.7\Assemblies`. It rejects game assemblies from other minor versions. The 1.7 load folders are `Cont` (shared textures) and `1.7` (Defs, translations, and assembly).

For another installation layout:

```powershell
dotnet build "Source\1.7\More Better Deep Drill\MoreBetterDeepDrill.csproj" -c Release /p:RimWorldDir="D:\Games\RimWorld" /p:HarmonyAssembliesDir="D:\Games\Harmony\1.7\Assemblies"
```

Use `/p:DeployToPackage=false` to verify compilation without updating the packaged DLL. `RimWorldManagedDir` and `PackageAssembliesDir` can also be overridden. Project RimFactory integration is currently disabled for 1.6 and 1.7.

After building, check the packaged DLL, API references, patch signatures, XML type references, and load folders:

```powershell
dotnet run --project "Source\1.7\CompatibilityCheck\CompatibilityCheck.csproj" -- .
```

The check accepts optional game and Harmony assembly directories after the repository path: `-- . "D:\Games\RimWorld" "D:\Games\Harmony\1.7\Assemblies"`.

See [1.7 validation notes](docs/1.7-validation.md) for the checked build, the maintainer's in-game test result, and a gameplay regression checklist.

## Build the 1.6 core assembly

Requirements:

- A .NET SDK with the .NET Framework 4.7.2 reference assemblies.
- A live checkout at `<RimWorld>\Mods\<mod-folder>`, with the Harmony workshop mod installed in the same Steam library. Other layouts must use the path overrides below.

From the repository root, run:

```powershell
dotnet msbuild "Source\1.6\More Better Deep Drill\MoreBetterDeepDrill.csproj" /t:Rebuild /p:Configuration=Release
```

The project builds into `bin\Release` first and copies the DLL to `1.6\Assemblies` only after a successful build. For another layout, override the detected paths:

```powershell
dotnet msbuild "Source\1.6\More Better Deep Drill\MoreBetterDeepDrill.csproj" /t:Rebuild /p:Configuration=Release /p:RimWorldDir="D:\Games\RimWorld" /p:HarmonyAssembliesDir="D:\Games\Harmony\Current\Assemblies"
```

For verification builds that must not update the packaged DLL, add `/p:DeployToPackage=false` and optionally override `OutputPath`.
 
