// =============================================================================
// ApiSurfaceDumper
// -----------------------------------------------------------------------------
// Dumps the PUBLIC API surface of a .NET codebase to Markdown files, so an AI
// agent (Kiro / Copilot / Claude) can read "what exists and what is exposed"
// as compiler-verified ground truth instead of guessing from raw source.
//
// Two modes:
//
//   1) SEMANTIC (preferred, most accurate)
//      dotnet run --project tools/ApiSurfaceDumper -- <path/to/Solution.sln> <output-dir>
//      Loads the real solution with MSBuild + Roslyn. Produces, per project:
//      namespaces -> public types -> public/protected members with signatures
//      and XML-doc summaries, plus a project dependency graph.
//
//   2) SYNTAX-ONLY (fallback for legacy/non-SDK-style projects that fail to load)
//      dotnet run --project tools/ApiSurfaceDumper -- --syntax-only <source-root> <output-dir>
//      Parses .cs files directly without building. Less precise (no cross-project
//      resolution, no inherited members) but works on anything.
//
// Output layout:
//   <output-dir>/index.md               solution overview + dependency graph
//   <output-dir>/projects/<Name>.md     one file per project
// =============================================================================

using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length >= 3 && args[0].Equals("--syntax-only", StringComparison.OrdinalIgnoreCase))
                return SyntaxOnlyMode.Run(args[1], args[2]);

            if (args.Length >= 2 && args[0].EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                return await SemanticMode.RunAsync(args[0], args[1]);

            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  Semantic    : dotnet run --project tools/ApiSurfaceDumper -- <path/to/Solution.sln> <output-dir>");
            Console.Error.WriteLine("  Syntax-only : dotnet run --project tools/ApiSurfaceDumper -- --syntax-only <source-root> <output-dir>");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine("If this happened while loading the solution, retry with the --syntax-only fallback mode.");
            return 1;
        }
    }
}

// =============================================================================
// MODE 1: Semantic analysis via MSBuild + Roslyn
// =============================================================================
internal static class SemanticMode
{
    private static readonly SymbolDisplayFormat MemberFormat = new(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType
                        | SymbolDisplayParameterOptions.IncludeName
                        | SymbolDisplayParameterOptions.IncludeParamsRefOut
                        | SymbolDisplayParameterOptions.IncludeDefaultValue,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static async Task<int> RunAsync(string slnPath, string outDir)
    {
        slnPath = Path.GetFullPath(slnPath);
        if (!File.Exists(slnPath))
        {
            Console.Error.WriteLine($"Solution not found: {slnPath}");
            return 1;
        }
        Directory.CreateDirectory(Path.Combine(outDir, "projects"));

        // Register MSBuild BEFORE any code touching Microsoft.Build runs.
        // The analysis itself lives in AnalyzeAsync (non-inlined) so the JIT
        // does not resolve MSBuild types until registration has completed.
        var instance = MSBuildLocator.QueryVisualStudioInstances()
                                     .OrderByDescending(i => i.Version)
                                     .FirstOrDefault();
        if (instance is null)
        {
            Console.Error.WriteLine("No MSBuild / .NET SDK instance found. Install the .NET SDK, or use --syntax-only mode.");
            return 1;
        }
        MSBuildLocator.RegisterInstance(instance);
        Console.WriteLine($"Using MSBuild: {instance.Name} {instance.Version}");

        return await AnalyzeAsync(slnPath, outDir);
    }

    // Kept separate and non-inlined on purpose: see the comment in RunAsync.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> AnalyzeAsync(string slnPath, string outDir)
    {
        var slnDir = Path.GetDirectoryName(slnPath)!;
        var loadWarnings = new List<string>();
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) => loadWarnings.Add($"{e.Diagnostic.Kind}: {e.Diagnostic.Message}");

        Console.WriteLine($"Opening solution (this can take a while on large solutions): {slnPath}");
        var solution = await workspace.OpenSolutionAsync(slnPath);

        var projectRows = new List<string>();
        var graphEdges = new List<(string From, string To)>();
        var usedStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in solution.Projects
                     .Where(p => p.Language == LanguageNames.CSharp)
                     .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  Analyzing {project.Name} ...");
            var compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                loadWarnings.Add($"Skipped (no compilation): {project.Name}");
                continue;
            }

            var (tfm, packages) = ReadCsprojInfo(project.FilePath);
            var projectRefs = project.ProjectReferences
                .Select(r => solution.GetProject(r.ProjectId)?.Name)
                .Where(n => n is not null)
                .Select(n => n!)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var dep in projectRefs)
                graphEdges.Add((project.Name, dep));

            var types = CollectPublicTypes(compilation.Assembly)
                .OrderBy(t => t.ContainingNamespace.ToDisplayString(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Two repos in an umbrella can contain same-named projects; keep output files distinct.
            var stem = Sanitize(project.Name);
            for (var i = 2; !usedStems.Add(stem); i++)
                stem = $"{Sanitize(project.Name)}-{i}";

            WriteProjectFile(outDir, slnDir, project.Name, stem, project.FilePath, tfm, projectRefs, packages, types);
            projectRows.Add($"| [{project.Name}](projects/{stem}.md) | {tfm ?? "?"} | {types.Count} | {projectRefs.Count} |");
        }

        WriteIndex(outDir, slnPath, projectRows, graphEdges, loadWarnings, mode: "semantic (MSBuild + Roslyn)");

        Console.WriteLine($"Done. Output written to: {Path.GetFullPath(outDir)}");
        if (loadWarnings.Count > 0)
            Console.WriteLine($"Note: {loadWarnings.Count} load warning(s) recorded in index.md. Projects that failed to load can be covered with --syntax-only mode.");
        return 0;
    }

    private static IEnumerable<INamedTypeSymbol> CollectPublicTypes(IAssemblySymbol assembly)
    {
        var stack = new Stack<INamespaceOrTypeSymbol>();
        stack.Push(assembly.GlobalNamespace);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var member in current.GetMembers())
            {
                switch (member)
                {
                    case INamespaceSymbol ns:
                        stack.Push(ns);
                        break;
                    case INamedTypeSymbol type when type.DeclaredAccessibility == Accessibility.Public:
                        yield return type;
                        stack.Push(type); // also walk nested public types
                        break;
                }
            }
        }
    }

    private static void WriteProjectFile(
        string outDir, string slnDir, string projectName, string fileStem, string? csprojPath, string? tfm,
        List<string> projectRefs, List<(string Name, string? Version)> packages,
        List<INamedTypeSymbol> types)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {projectName} — Public API Surface");
        sb.AppendLine();
        sb.AppendLine($"> GENERATED FILE — do not edit by hand. Regenerate with ApiSurfaceDumper.");
        sb.AppendLine($"> Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC | Target: {tfm ?? "unknown"}");
        if (csprojPath is not null)
            sb.AppendLine($"> Project file: `{Rel(slnDir, csprojPath)}`");
        sb.AppendLine();

        sb.AppendLine("## Depends on (project references)");
        sb.AppendLine();
        sb.AppendLine(projectRefs.Count == 0 ? "_None._" : string.Join("\n", projectRefs.Select(r => $"- {r}")));
        sb.AppendLine();

        sb.AppendLine("## Package references");
        sb.AppendLine();
        sb.AppendLine(packages.Count == 0 ? "_None found in csproj._"
            : string.Join("\n", packages.Select(p => $"- {p.Name}{(p.Version is null ? "" : $" ({p.Version})")}")));
        sb.AppendLine();

        foreach (var nsGroup in types.GroupBy(t => t.ContainingNamespace.ToDisplayString()))
        {
            sb.AppendLine($"## Namespace `{nsGroup.Key}`");
            sb.AppendLine();
            foreach (var type in nsGroup)
                AppendType(sb, slnDir, type);
        }

        File.WriteAllText(Path.Combine(outDir, "projects", fileStem + ".md"), sb.ToString());
    }

    private static void AppendType(StringBuilder sb, string slnDir, INamedTypeSymbol type)
    {
        var kind = type.TypeKind switch
        {
            TypeKind.Class => type.IsRecord ? "record" : "class",
            TypeKind.Struct => type.IsRecord ? "record struct" : "struct",
            TypeKind.Interface => "interface",
            TypeKind.Enum => "enum",
            TypeKind.Delegate => "delegate",
            _ => type.TypeKind.ToString().ToLowerInvariant()
        };
        var modifiers = type.IsStatic ? "static " : type.IsAbstract && type.TypeKind == TypeKind.Class ? "abstract " : type.IsSealed && type.TypeKind == TypeKind.Class ? "sealed " : "";
        var name = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        sb.AppendLine($"### `{modifiers}{kind} {name}`");
        sb.AppendLine();

        var srcLoc = type.Locations.FirstOrDefault(l => l.IsInSource);
        if (srcLoc?.SourceTree is not null)
            sb.AppendLine($"Defined in: `{Rel(slnDir, srcLoc.SourceTree.FilePath)}`");

        var bases = new List<string>();
        if (type.BaseType is { SpecialType: not SpecialType.System_Object and not SpecialType.System_ValueType and not SpecialType.System_Enum } bt)
            bases.Add(bt.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        bases.AddRange(type.Interfaces.Select(i => i.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        if (bases.Count > 0)
            sb.AppendLine($"Inherits/implements: {string.Join(", ", bases.Select(b => $"`{b}`"))}");

        var summary = XmlSummary(type);
        if (summary is not null)
            sb.AppendLine($"\n> {summary}");
        sb.AppendLine();

        if (type.TypeKind == TypeKind.Enum)
        {
            var values = type.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue)
                .Select(f => $"`{f.Name}`");
            sb.AppendLine("Values: " + string.Join(", ", values));
            sb.AppendLine();
            return;
        }

        var members = type.GetMembers().Where(IsApiMember).ToList();
        if (members.Count == 0)
        {
            sb.AppendLine("_No public/protected members declared directly on this type._");
            sb.AppendLine();
            return;
        }

        foreach (var member in members.OrderBy(MemberSortKey).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
        {
            var sig = member.ToDisplayString(MemberFormat);
            var mSummary = XmlSummary(member);
            var tag = member switch
            {
                IMethodSymbol { MethodKind: MethodKind.Constructor } => "ctor",
                IMethodSymbol => "method",
                IPropertySymbol => "property",
                IEventSymbol => "event",
                IFieldSymbol => "field",
                _ => member.Kind.ToString().ToLowerInvariant()
            };
            var prot = member.DeclaredAccessibility != Accessibility.Public ? " (protected)" : "";
            sb.AppendLine($"- **{tag}{prot}** `{sig}`{(mSummary is null ? "" : $" — {mSummary}")}");
        }
        sb.AppendLine();
    }

    private static bool IsApiMember(ISymbol m)
    {
        if (m.IsImplicitlyDeclared) return false;
        if (m is INamedTypeSymbol) return false; // nested types are printed as their own sections
        if (m.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
            return false;
        if (m is IMethodSymbol method && method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet
            or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise)
            return false;
        return m is IMethodSymbol or IPropertySymbol or IEventSymbol or IFieldSymbol;
    }

    private static int MemberSortKey(ISymbol m) => m switch
    {
        IMethodSymbol { MethodKind: MethodKind.Constructor } => 0,
        IPropertySymbol => 1,
        IMethodSymbol => 2,
        IEventSymbol => 3,
        IFieldSymbol => 4,
        _ => 5
    };

    private static string? XmlSummary(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml)) return null;
        try
        {
            var summary = XDocument.Parse(xml).Descendants("summary").FirstOrDefault()?.Value;
            return string.IsNullOrWhiteSpace(summary) ? null : Collapse(summary);
        }
        catch
        {
            return null;
        }
    }

    private static (string? Tfm, List<(string Name, string? Version)> Packages) ReadCsprojInfo(string? csprojPath)
    {
        var packages = new List<(string, string?)>();
        string? tfm = null;
        if (csprojPath is null || !File.Exists(csprojPath)) return (tfm, packages);
        try
        {
            var doc = XDocument.Load(csprojPath);
            tfm = doc.Descendants().FirstOrDefault(e => e.Name.LocalName is "TargetFramework" or "TargetFrameworks" or "TargetFrameworkVersion")?.Value;
            foreach (var pr in doc.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
            {
                var name = pr.Attribute("Include")?.Value;
                if (name is null) continue;
                var version = pr.Attribute("Version")?.Value
                              ?? pr.Elements().FirstOrDefault(e => e.Name.LocalName == "Version")?.Value;
                packages.Add((name, version));
            }
        }
        catch { /* best effort; csproj parsing is informational only */ }
        return (tfm, packages);
    }

    private static void WriteIndex(string outDir, string source, List<string> projectRows,
        List<(string From, string To)> edges, List<string> warnings, string mode)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# API Surface Index");
        sb.AppendLine();
        sb.AppendLine($"> GENERATED FILE — do not edit by hand.");
        sb.AppendLine($"> Source: `{source}` | Mode: {mode} | Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();
        sb.AppendLine("| Project | Target | Public types | Project refs |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var row in projectRows) sb.AppendLine(row);
        sb.AppendLine();

        if (edges.Count > 0)
        {
            sb.AppendLine("## Project dependency graph");
            sb.AppendLine();
            sb.AppendLine("```mermaid");
            sb.AppendLine("graph LR");
            var ids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string Id(string name)
            {
                if (!ids.TryGetValue(name, out var id))
                {
                    id = "n" + ids.Count;
                    ids[name] = id;
                    sb.AppendLine($"  {id}[\"{name}\"]");
                }
                return id;
            }
            foreach (var (from, to) in edges.Distinct())
                sb.AppendLine($"  {Id(from)} --> {Id(to)}");
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (warnings.Count > 0)
        {
            sb.AppendLine("## Load warnings");
            sb.AppendLine();
            sb.AppendLine("Some projects may be missing or partially analyzed. Re-run failed areas with `--syntax-only`.");
            sb.AppendLine();
            foreach (var w in warnings.Distinct().Take(200))
                sb.AppendLine($"- {w}");
            sb.AppendLine();
        }

        File.WriteAllText(Path.Combine(outDir, "index.md"), sb.ToString());
    }

    private static string Rel(string baseDir, string path)
    {
        try { return Path.GetRelativePath(baseDir, path).Replace('\\', '/'); }
        catch { return path; }
    }

    private static string Sanitize(string name)
        => string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_'));

    private static string Collapse(string s)
        => string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

// =============================================================================
// MODE 2: Syntax-only fallback (no build required)
// =============================================================================
internal static class SyntaxOnlyMode
{
    private static readonly string[] SkipDirs = { "bin", "obj", ".git", ".vs", "packages", "node_modules", "TestResults" };

    public static int Run(string sourceRoot, string outDir)
    {
        sourceRoot = Path.GetFullPath(sourceRoot);
        if (!Directory.Exists(sourceRoot))
        {
            Console.Error.WriteLine($"Source root not found: {sourceRoot}");
            return 1;
        }
        Directory.CreateDirectory(Path.Combine(outDir, "projects"));

        var csFiles = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !SkipDirs.Any(d => p.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                            .Contains(d, StringComparer.OrdinalIgnoreCase)))
            .ToList();
        Console.WriteLine($"Parsing {csFiles.Count} .cs files under {sourceRoot} ...");

        var projectNameCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var byProject = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in csFiles)
        {
            var proj = NearestProjectName(Path.GetDirectoryName(file)!, sourceRoot, projectNameCache);
            if (!byProject.TryGetValue(proj, out var list)) byProject[proj] = list = new List<string>();
            list.Add(file);
        }

        var projectRows = new List<string>();
        foreach (var (project, files) in byProject)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {project} — Public API Surface (syntax-only)");
            sb.AppendLine();
            sb.AppendLine("> GENERATED FILE — syntax-only mode: signatures come straight from source text.");
            sb.AppendLine("> Cross-project references and inherited members are NOT resolved in this mode.");
            sb.AppendLine($"> Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine();

            int typeCount = 0;
            foreach (var file in files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                CompilationUnitSyntax unit;
                try
                {
                    unit = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"_Could not parse `{Rel(sourceRoot, file)}`: {ex.Message}_");
                    continue;
                }

                foreach (var type in unit.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
                                         .Where(t => t.Modifiers.Any(SyntaxKind.PublicKeyword)))
                {
                    typeCount++;
                    var ns = type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? "(global)";
                    var kindWord = type.Keyword.ValueText; // class / struct / interface / enum / record
                    sb.AppendLine($"### `{kindWord} {type.Identifier.ValueText}`  (`{ns}`)");
                    sb.AppendLine($"Defined in: `{Rel(sourceRoot, file)}`");
                    sb.AppendLine();

                    if (type is EnumDeclarationSyntax en)
                    {
                        sb.AppendLine("Values: " + string.Join(", ", en.Members.Select(m => $"`{m.Identifier.ValueText}`")));
                    }
                    else if (type is TypeDeclarationSyntax td)
                    {
                        var publicMembers = td.Members
                            .Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword) && m is not BaseTypeDeclarationSyntax)
                            .Select(Describe)
                            .Where(d => d is not null)
                            .ToList();
                        if (publicMembers.Count == 0)
                            sb.AppendLine("_No public members declared directly on this type._");
                        else
                            foreach (var d in publicMembers) sb.AppendLine($"- {d}");
                    }
                    sb.AppendLine();
                }
            }

            File.WriteAllText(Path.Combine(outDir, "projects", Sanitize(project) + ".md"), sb.ToString());
            projectRows.Add($"| [{project}](projects/{Sanitize(project)}.md) | (syntax-only) | {typeCount} | — |");
        }

        // Index
        var idx = new StringBuilder();
        idx.AppendLine("# API Surface Index (syntax-only mode)");
        idx.AppendLine();
        idx.AppendLine($"> GENERATED FILE. Source root: `{sourceRoot}` | Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        idx.AppendLine("> Note: syntax-only mode cannot resolve project references; the dependency graph is unavailable.");
        idx.AppendLine();
        idx.AppendLine("| Project | Target | Public types | Project refs |");
        idx.AppendLine("|---|---|---|---|");
        foreach (var row in projectRows) idx.AppendLine(row);
        File.WriteAllText(Path.Combine(outDir, "index.md"), idx.ToString());

        Console.WriteLine($"Done. Output written to: {Path.GetFullPath(outDir)}");
        return 0;
    }

    private static string? Describe(MemberDeclarationSyntax member) => member switch
    {
        MethodDeclarationSyntax m => $"**method** `{Collapse($"{m.ReturnType} {m.Identifier}{m.TypeParameterList}{m.ParameterList}")}`",
        ConstructorDeclarationSyntax c => $"**ctor** `{Collapse($"{c.Identifier}{c.ParameterList}")}`",
        PropertyDeclarationSyntax p => $"**property** `{Collapse($"{p.Type} {p.Identifier}")}`",
        IndexerDeclarationSyntax i => $"**indexer** `{Collapse($"{i.Type} this{i.ParameterList}")}`",
        EventDeclarationSyntax e => $"**event** `{Collapse($"{e.Type} {e.Identifier}")}`",
        EventFieldDeclarationSyntax ef => $"**event** `{Collapse(ef.Declaration.ToString())}`",
        FieldDeclarationSyntax f => $"**field** `{Collapse(f.Declaration.ToString())}`",
        DelegateDeclarationSyntax d => $"**delegate** `{Collapse($"{d.ReturnType} {d.Identifier}{d.ParameterList}")}`",
        OperatorDeclarationSyntax op => $"**operator** `{Collapse($"{op.ReturnType} operator {op.OperatorToken}{op.ParameterList}")}`",
        _ => null
    };

    private static string NearestProjectName(string dir, string root, Dictionary<string, string> cache)
    {
        var current = dir;
        var visited = new List<string>();
        while (current is not null && current.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            if (cache.TryGetValue(current, out var cached))
            {
                foreach (var v in visited) cache[v] = cached;
                return cached;
            }
            visited.Add(current);
            var csproj = Directory.EnumerateFiles(current, "*.csproj").FirstOrDefault();
            if (csproj is not null)
            {
                var name = Path.GetFileNameWithoutExtension(csproj);
                foreach (var v in visited) cache[v] = name;
                return name;
            }
            current = Path.GetDirectoryName(current);
        }
        var fallback = "(no-csproj) " + Path.GetFileName(dir);
        foreach (var v in visited) cache[v] = fallback;
        return fallback;
    }

    private static string Rel(string baseDir, string path)
    {
        try { return Path.GetRelativePath(baseDir, path).Replace('\\', '/'); }
        catch { return path; }
    }

    private static string Sanitize(string name)
        => string.Concat(name.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' or ' ' or '(' or ')' ? c : '_')).Replace(' ', '_').Replace("(", "").Replace(")", "");

    private static string Collapse(string s)
        => string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
