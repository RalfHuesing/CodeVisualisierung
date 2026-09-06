using CodeVisualisierung.CSharp.Contract;
using CodeVisualisierung.CSharp.Graph;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace CodeVisualisierung.CSharp.Analysis;

/// <summary>Stabile Zähler und Statuswerte eines Adapterlaufs.</summary>
public sealed record AnalysisSummary(
    string InputKind,
    int ProjectsLoaded,
    int ProjectsAnalyzed,
    int ProjectsSkipped,
    int ProjectsFailed,
    int DocumentsAnalyzed,
    int DocumentsSkipped,
    int DocumentsFailed,
    int NodesEmitted,
    int LinksEmitted,
    int ExternalDropped,
    int Unresolved,
    int Warnings,
    int Errors,
    string Status);

/// <summary>Graph und Summary eines abgeschlossenen Workspace-Laufs.</summary>
public sealed record AnalysisResult(GraphDocument Graph, AnalysisSummary Summary);

/// <summary>Signals that the requested project or solution cannot be loaded.</summary>
public sealed class InputLoadException : Exception
{
    /// <summary>Creates an input-loading failure with its Roslyn cause.</summary>
    public InputLoadException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>Lädt C#-Eingaben und erzeugt den deterministischen Inventargraphen.</summary>
public sealed class WorkspaceAnalysis
{
    /// <summary>Analysiert eine Solution oder ein einzelnes Projekt.</summary>
    public async Task<AnalysisResult> AnalyzeAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        RegisterMsBuild();
        var fullInputPath = Path.GetFullPath(inputPath);
        var inputKind = DetermineInputKind(fullInputPath);
        var diagnostics = new List<string>();
        using var workspace = CreateWorkspace(diagnostics);
        Solution solution;
        try
        {
            solution = await OpenInputAsync(workspace, inputKind, fullInputPath, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            throw new InputLoadException("Die Solution oder das Projekt konnte nicht ausgewertet werden.", exception);
        }
        if (diagnostics.Any(IsMissingInputDiagnostic))
        {
            throw new InputLoadException("Eine referenzierte Projektdatei wurde nicht gefunden.", new InvalidOperationException(diagnostics.First(IsMissingInputDiagnostic)));
        }
        var rootPath = Path.GetDirectoryName(fullInputPath)!;
        var builder = new GraphBuilder();
        builder.AddNode(new GraphNode { Id = "solution:root", TypeId = "solution", Label = Path.GetFileName(fullInputPath) });
        var projects = GetProjects(solution);
        var projectIndex = CreateProjectIndex(projects, rootPath);
        var counters = new AnalysisCounters();
        var analyzedProjects = new List<AnalyzedProject>();
        foreach (var project in projects)
        {
            var context = new ProjectAnalysisContext(projectIndex, rootPath, builder, counters, diagnostics);
            var analyzedProject = await AnalyzeProjectAsync(project, context, cancellationToken);
            if (analyzedProject is not null)
                analyzedProjects.Add(analyzedProject);
        }

        var exportedNodes = new ExportedNodeIndex();
        foreach (var analyzedProject in analyzedProjects)
        {
            analyzedProject.Declarations.Emit(builder);
            foreach (var entry in analyzedProject.Declarations.ExportedEntries)
                exportedNodes.Add(analyzedProject.ProjectId, entry.Symbol, entry.Id);
        }

        foreach (var analyzedProject in analyzedProjects)
            EmitRelations(analyzedProject, exportedNodes, builder, counters);

        var graph = builder.Build();
        GraphMetricsAndProjections.Apply(graph);
        GraphContractValidator.Validate(graph);
        return new AnalysisResult(graph, counters.CreateSummary(inputKind, projects.Length, graph, diagnostics));
    }

    private static string DetermineInputKind(string inputPath) => Path.GetExtension(inputPath).ToLowerInvariant() switch
    {
        ".slnx" => "slnx",
        ".sln" => "sln",
        ".csproj" => "csproj",
        _ => throw new InvalidOperationException("Nicht unterstütztes Eingabeformat.")
    };

    private static MSBuildWorkspace CreateWorkspace(List<string> diagnostics)
    {
        var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
        {
            ["DesignTimeBuild"] = "true",
            ["SkipCompilerExecution"] = "true",
            ["ProvideCommandLineArgs"] = "true",
            ["RunAnalyzers"] = "false"
        });
        workspace.RegisterWorkspaceFailedHandler(eventArgs => diagnostics.Add(eventArgs.Diagnostic.Message));
        return workspace;
    }

    private static async Task<Solution> OpenInputAsync(MSBuildWorkspace workspace, string inputKind, string inputPath, CancellationToken cancellationToken) => inputKind == "csproj"
        ? (await workspace.OpenProjectAsync(inputPath, cancellationToken: cancellationToken)).Solution
        : await workspace.OpenSolutionAsync(inputPath, cancellationToken: cancellationToken);

    private static Project[] GetProjects(Solution solution) => solution.Projects
        .OrderBy(project => project.FilePath ?? project.Name, StringComparer.Ordinal).ToArray();

    private static async Task<AnalyzedProject?> AnalyzeProjectAsync(Project project, ProjectAnalysisContext context, CancellationToken cancellationToken)
    {
        if (project.FilePath is null)
        {
            context.Counters.ProjectFailed();
            return null;
        }

        var projectInfo = context.ProjectIndex[project.Id];
        var projectId = projectInfo.ProjectId;
        var assemblyId = projectInfo.AssemblyId;
        var moduleId = projectInfo.ModuleId;
        var declarations = new DeclarationAccumulator(projectId);
        var relations = new List<RelationCandidate>();
        AddProjectNodes(project, projectId, assemblyId, moduleId, context.RootPath, context.Builder);
        AddProjectReferences(project, context.ProjectIndex, projectId, assemblyId, context.Builder);
        foreach (var document in project.Documents.OrderBy(document => document.FilePath, StringComparer.Ordinal))
        {
            var documentContext = new DocumentAnalysisContext(project, projectId, moduleId, context, declarations, relations);
            await AnalyzeDocumentAsync(document, documentContext, cancellationToken);
        }

        context.Counters.ProjectAnalyzed();
        return new AnalyzedProject(project, projectId, declarations, relations);
    }

    private static void AddProjectNodes(Project project, string projectId, string assemblyId, string moduleId, string rootPath, GraphBuilder builder)
    {
        builder.AddNode(new GraphNode
        {
            Id = projectId, TypeId = "project", Label = project.Name, GroupId = "solution:root",
            Attributes = new Dictionary<string, object?> { ["path"] = RelativePath(rootPath, project.FilePath!) }
        });
        builder.AddNode(new GraphNode { Id = assemblyId, TypeId = "assembly", Label = project.AssemblyName ?? project.Name, GroupId = projectId });
        builder.AddNode(new GraphNode { Id = moduleId, TypeId = "module", Label = project.AssemblyName ?? project.Name, GroupId = projectId });
        builder.AddLink("contains", "solution:root", projectId);
        builder.AddLink("contains", projectId, assemblyId);
        builder.AddLink("contains", assemblyId, moduleId);
        builder.AddDeclarationLink("solution:root", projectId);
        builder.AddDeclarationLink(projectId, assemblyId);
        builder.AddDeclarationLink(assemblyId, moduleId);
    }

    private static void AddProjectReferences(
        Project project,
        IReadOnlyDictionary<ProjectId, ProjectReferenceIndexEntry> projectIndex,
        string projectId,
        string assemblyId,
        GraphBuilder builder)
    {
        var referencedProjectIds = project.ProjectReferences
            .Select(reference => reference.ProjectId)
            .ToHashSet();
        foreach (var referencedProjectId in referencedProjectIds)
        {
            if (!projectIndex.TryGetValue(referencedProjectId, out var targetProject))
            {
                continue;
            }

            builder.AddLink("project-reference", projectId, targetProject.ProjectId);
            builder.AddLink("references-assembly", assemblyId, targetProject.AssemblyId);
        }
    }

    private static IReadOnlyDictionary<ProjectId, ProjectReferenceIndexEntry> CreateProjectIndex(Project[] projects, string rootPath) =>
        projects.ToDictionary(project => project.Id, project => CreateProjectIndexEntry(project, rootPath));

    private static ProjectReferenceIndexEntry CreateProjectIndexEntry(Project project, string rootPath)
    {
        var projectId = CreateProjectId(project, rootPath);
        var assemblyName = project.AssemblyName ?? project.Name;
        var assemblyId = $"assembly:{projectId}:{assemblyName}";
        return new ProjectReferenceIndexEntry(project, projectId, assemblyId, $"module:{projectId}:{assemblyName}");
    }

    private static async Task AnalyzeDocumentAsync(Document document, DocumentAnalysisContext context, CancellationToken cancellationToken)
    {
        if (document.FilePath is null || !IsOwnSourceDocument(context.Project, document.FilePath))
        {
            context.Analysis.Counters.DocumentSkipped();
            return;
        }

        var relativePath = RelativePath(context.Analysis.RootPath, document.FilePath);
        var fileId = $"file:{context.ProjectId}:{relativePath}";
        context.Analysis.Builder.AddNode(new GraphNode
        {
            Id = fileId, TypeId = "file", Label = Path.GetFileName(document.FilePath), GroupId = context.ProjectId,
            Attributes = new Dictionary<string, object?> { ["path"] = relativePath }
        });
        context.Analysis.Builder.AddLink("contains", context.ModuleId, fileId);
        context.Analysis.Builder.AddDeclarationLink(context.ModuleId, fileId);
        try
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Dokument '{document.Name}' besitzt keinen Syntaxbaum.");
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken)
                ?? throw new InvalidOperationException($"Dokument '{document.Name}' besitzt kein SemanticModel.");
            context.Analysis.Builder.SetNodeMetric(fileId, "loc", SourceMetrics.GetNonEmptyLines(root).Count); var namespaces = GetNamespaces(root, semanticModel, cancellationToken);
            foreach (var namespaceName in namespaces)
            {
                var namespaceId = $"namespace:{context.ProjectId}:{namespaceName}";
                context.Analysis.Builder.AddNode(new GraphNode { Id = namespaceId, TypeId = "namespace", Label = namespaceName, GroupId = context.ProjectId });
                context.Analysis.Builder.AddLink("contains", namespaceId, fileId);
                context.Analysis.Builder.AddLink("contains", GetAssemblyId(context.Project, context.ProjectId), namespaceId);
                context.Analysis.Builder.AddDeclarationLink(fileId, namespaceId);
                context.Analysis.Builder.AddDeclarationLink(GetAssemblyId(context.Project, context.ProjectId), namespaceId);
            }

            var declarations = DeclarationCollector.Collect(
                root,
                semanticModel,
                context.Analysis.RootPath,
                context.ProjectId,
                cancellationToken);
            foreach (var declaration in declarations)
                context.Declarations.Add(declaration);
            foreach (var relation in SemanticRelationCollector.Collect(
                         root,
                         semanticModel,
                         context.ProjectId,
                         cancellationToken))
                context.Relations.Add(relation);
            foreach (var relation in SemanticRelationCollector.CollectDeclarationRelations(
                         root,
                         semanticModel,
                         context.ProjectId,
                         cancellationToken))
                context.Relations.Add(relation);

            context.Analysis.Counters.DocumentAnalyzed();
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            context.Analysis.Counters.DocumentFailed();
            context.Analysis.Diagnostics.Add(exception.Message);
        }
    }

    private static string[] GetNamespaces(SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var namespaceDeclarations = root.DescendantNodesAndSelf().OfType<BaseNamespaceDeclarationSyntax>();
        var declaredNamespaces = namespaceDeclarations.Select(node => semanticModel.GetDeclaredSymbol(node, cancellationToken));
        var namespaceNames = declaredNamespaces.OfType<INamespaceSymbol>().Select(GetNamespaceName);
        var namespaces = namespaceNames.Distinct(StringComparer.Ordinal).ToArray();
        return namespaces.Length == 0 ? ["global"] : namespaces.Order(StringComparer.Ordinal).ToArray();
    }

    private static string GetNamespaceName(INamespaceSymbol symbol)
    {
        var name = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return name.StartsWith("global::", StringComparison.Ordinal) ? name[8..] : name;
    }

    private static string GetAssemblyId(Project project, string projectId) => $"assembly:{projectId}:{project.AssemblyName ?? project.Name}";

    private static void EmitRelations(
        AnalyzedProject analyzedProject,
        ExportedNodeIndex exportedNodes,
        GraphBuilder builder,
        AnalysisCounters counters)
    {
        foreach (var relation in analyzedProject.Relations)
        {
            if (!TryResolveReference(relation.Source, exportedNodes, counters, out var sourceId, out _)
                || !TryResolveReference(relation.Target, exportedNodes, counters, out var targetId, out var targetProjectId))
                continue;

            builder.AddRelationLink(relation.TypeId, sourceId, targetId, relation.RelationshipWeight);
            if (analyzedProject.IsTestProject && relation.Source?.IsTestMethod == true
                && targetProjectId != analyzedProject.ProjectId && relation.TypeId != "tests")
                builder.AddRelationLink("tests", sourceId, targetId, 1);
        }
    }

    private static bool TryResolveReference(
        SymbolReference? reference,
        ExportedNodeIndex exportedNodes,
        AnalysisCounters counters,
        out string nodeId,
        out string projectId)
    {
        nodeId = string.Empty;
        projectId = string.Empty;
        if (reference is null)
        {
            counters.UnresolvedRelation();
            return false;
        }

        if (reference.IsGenerated || reference.IsExternal || string.IsNullOrEmpty(reference.NodeTypeId))
        {
            counters.ExternalRelationDropped();
            return false;
        }

        if (!exportedNodes.TryResolve(reference, out nodeId, out projectId))
        {
            counters.UnresolvedRelation();
            return false;
        }

        return true;
    }

    private static string CreateProjectId(Project project, string rootPath) => $"project:{RelativePath(rootPath, project.FilePath!)}";

    private static string RelativePath(string rootPath, string path) => Path.GetRelativePath(rootPath, path).Replace(Path.DirectorySeparatorChar, '/');

    private static bool IsOwnSourceDocument(Project project, string documentPath)
    {
        var projectDirectory = Path.GetDirectoryName(project.FilePath)!;
        var fullDocumentPath = Path.GetFullPath(documentPath);
        var prefix = projectDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullDocumentPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !Path.GetExtension(fullDocumentPath).Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(projectDirectory, fullDocumentPath);
        return !relativePath.Split(Path.DirectorySeparatorChar).Any(segment => segment.Equals("obj", StringComparison.OrdinalIgnoreCase) || segment.Equals("bin", StringComparison.OrdinalIgnoreCase))
            && !IsGeneratedFileName(Path.GetFileName(fullDocumentPath));
    }

    private static bool IsGeneratedFileName(string fileName) =>
        fileName.Equals("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".AssemblyAttributes.cs", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);

    private static bool IsMissingInputDiagnostic(string diagnostic) =>
        diagnostic.Contains("not found", StringComparison.OrdinalIgnoreCase)
        || diagnostic.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
        || diagnostic.Contains("nicht gefunden", StringComparison.OrdinalIgnoreCase)
        || diagnostic.Contains("nicht vorhanden", StringComparison.OrdinalIgnoreCase);

    private static void RegisterMsBuild()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }

    private sealed record ProjectAnalysisContext(
        IReadOnlyDictionary<ProjectId, ProjectReferenceIndexEntry> ProjectIndex,
        string RootPath,
        GraphBuilder Builder,
        AnalysisCounters Counters,
        List<string> Diagnostics);

    private sealed record ProjectReferenceIndexEntry(Project Project, string ProjectId, string AssemblyId, string ModuleId);

    private sealed record AnalyzedProject(
        Project Project,
        string ProjectId,
        DeclarationAccumulator Declarations,
        IReadOnlyList<RelationCandidate> Relations)
    {
        public bool IsTestProject => Project.Name.EndsWith("Tests", StringComparison.OrdinalIgnoreCase)
            || Project.Name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
            || Path.GetFileName(Path.GetDirectoryName(Project.FilePath) ?? string.Empty)
                .EndsWith("Tests", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record DocumentAnalysisContext(
        Project Project,
        string ProjectId,
        string ModuleId,
        ProjectAnalysisContext Analysis,
        DeclarationAccumulator Declarations,
        ICollection<RelationCandidate> Relations);

    private sealed class AnalysisCounters
    {
        private readonly int[] values = new int[7];

        public int ProjectsAnalyzed => values[0];
        public int ProjectsFailed => values[1];
        public int DocumentsAnalyzed => values[2];
        public int DocumentsSkipped => values[3];
        public int DocumentsFailed => values[4];

        public void ProjectAnalyzed() => values[0]++;
        public void ProjectFailed() => values[1]++;
        public void DocumentAnalyzed() => values[2]++;
        public void DocumentSkipped() => values[3]++;
        public void DocumentFailed() => values[4]++;

        public void ExternalRelationDropped() => values[5]++;
        public void UnresolvedRelation() => values[6]++;

        public AnalysisSummary CreateSummary(string inputKind, int projectsLoaded, GraphDocument graph, List<string> diagnostics) => new(
            inputKind, projectsLoaded, ProjectsAnalyzed, 0, ProjectsFailed, DocumentsAnalyzed, DocumentsSkipped,
            DocumentsFailed, graph.Nodes.Count, graph.Links.Count, values[5], values[6], diagnostics.Count, 0,
            diagnostics.Count == 0 && ProjectsFailed == 0 && DocumentsFailed == 0 ? "complete" : "partial");
    }
}
