using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Loader;
using System.Xml.Linq;

string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
string gameDir = Path.GetFullPath(args.Length > 1 ? args[1] : Path.Combine(root, "../.."));
string harmonyDir = Path.GetFullPath(args.Length > 2 ? args[2] : Path.Combine(gameDir, "../../workshop/content/294100/2009463077/1.7/Assemblies"));
AssemblyLoadContext.Default.Resolving += (context, name) => {
    foreach (string dir in new[] { gameDir, harmonyDir }) {
        string candidate = Path.Combine(dir, name.Name + ".dll");
        if (File.Exists(candidate)) return context.LoadFromAssemblyPath(candidate);
    }
    return null;
};
Assembly game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(gameDir, "Assembly-CSharp.dll"));
string modPath = Path.GetFullPath(Path.Combine(root, "1.7/Assemblies/MoreBetterDeepDrill.dll"));
Assembly mod = AssemblyLoadContext.Default.LoadFromAssemblyPath(modPath);
var types = mod.GetTypes();
Console.WriteLine($"Loaded {types.Length} mod types against RimWorld {game.GetName().Version}.");
int failures = 0;
void Check(bool pass, string message) {
    Console.WriteLine((pass ? "PASS " : "FAIL ") + message);
    if (!pass) failures++;
}
using (var stream = File.OpenRead(modPath)) {
    var pe = new PEReader(stream);
    var metadata = pe.GetMetadataReader();
    int checkedMembers = 0;
    foreach (var handle in metadata.MemberReferences) {
        try { mod.ManifestModule.ResolveMember(MetadataTokens.GetToken(handle)); checkedMembers++; }
        catch (Exception ex) { Check(false, $"Member reference {MetadataTokens.GetToken(handle):X8}: {ex.Message}"); }
    }
    Console.WriteLine($"Resolved {checkedMembers} member references.");
}
Type ResolveType(string name) => mod.GetType(name) ?? game.GetType(name) ?? game.GetType("Verse." + name) ?? game.GetType("RimWorld." + name);
foreach (Type patch in types.Where(t => t.Namespace == "MoreBetterDeepDrill.Patch")) {
    foreach (var attr in patch.CustomAttributes.Where(a => a.AttributeType.Name == "HarmonyPatch")) {
        if (attr.ConstructorArguments.Count < 2) continue;
        Type target = (Type)attr.ConstructorArguments[0].Value;
        string methodName = (string)attr.ConstructorArguments[1].Value;
        var targets = target.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where(m => m.Name == methodName).ToArray();
        Check(targets.Length == 1, $"Patch target {target.FullName}.{methodName} resolves uniquely");
        if (targets.Length != 1) continue;
        foreach (var postfix in patch.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == "Postfix")) {
            foreach (var p in postfix.GetParameters()) {
                Type actual;
                if (p.Name == "__instance") actual = target;
                else if (p.Name.StartsWith("___")) actual = target.GetField(p.Name.Substring(3), BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)?.FieldType;
                else actual = targets[0].GetParameters().FirstOrDefault(a => a.Name == p.Name)?.ParameterType;
                Check(actual != null && p.ParameterType.IsAssignableFrom(actual), $"Patch injection {patch.Name}.{p.Name}");
            }
        }
    }
}
foreach (string path in Directory.GetFiles(Path.Combine(root, "1.7"), "*.xml", SearchOption.AllDirectories)) {
    XDocument doc = XDocument.Load(path);
    foreach (var node in doc.Descendants().Where(e => new[] { "driverClass", "giverClass", "compClass", "graphicClass" }.Contains(e.Name.LocalName)))
        Check(ResolveType(node.Value) != null, $"XML type {node.Value}");
    foreach (var attr in doc.Descendants().Attributes("Class"))
        Check(ResolveType(attr.Value) != null, $"XML class {attr.Value}");
    foreach (var node in doc.Descendants("placeWorkers").Elements("li"))
        Check(ResolveType(node.Value) != null, $"Place worker {node.Value}");
    if (doc.Root.Name == "Defs") {
        foreach (var def in doc.Root.Elements()) {
            Type type = ResolveType(def.Name.LocalName);
            Check(type != null, $"Def type {def.Name}");
            foreach (var field in def.Elements())
                Check(type?.GetField(field.Name.LocalName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null, $"{def.Name}.{field.Name} exists");
        }
    }
}
// Check the actual packaged DLL and the load-folder configuration together.
var about = XDocument.Load(Path.Combine(root, "About/About.xml"));
Check(about.Root.Element("supportedVersions").Elements("li").Any(e => e.Value == "1.7"), "About.xml declares 1.7");
var folders = XDocument.Load(Path.Combine(root, "LoadFolders.xml")).Root.Element("v1.7").Elements("li").Select(e => e.Value).ToArray();
Check(folders.SequenceEqual(new[] { "Cont", "1.7" }), "1.7 loads shared assets and the dedicated package");
foreach (string folder in folders) Check(Directory.Exists(Path.Combine(root, folder)), $"Load folder {folder} exists");
var packagedDlls = folders.SelectMany(f => Directory.Exists(Path.Combine(root, f, "Assemblies")) ? Directory.GetFiles(Path.Combine(root, f, "Assemblies"), "*.dll", SearchOption.AllDirectories) : Array.Empty<string>()).ToArray();
Check(packagedDlls.Length == 1 && Path.GetFullPath(packagedDlls[0]) == modPath, "Only the 1.7 core DLL is packaged for loading");

Check(game.GetName().Version.Major == 1 && game.GetName().Version.Minor == 7, "Game version is 1.7");
Console.WriteLine($"Compatibility check finished: {failures} failures.");
return failures == 0 ? 0 : 1;
