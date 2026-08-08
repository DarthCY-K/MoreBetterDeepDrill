# More Better Deep Drill

Adds ranged, large multi-operator, and archotech deep drills to RimWorld 1.5 and 1.6.

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
 
