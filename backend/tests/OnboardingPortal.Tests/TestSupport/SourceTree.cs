using System.Xml.Linq;

namespace OnboardingPortal.Tests.TestSupport;

/// <summary>Reads the project files under <c>backend/src</c>, found from the test output folder.</summary>
public static class SourceTree
{
    private static readonly Lazy<string> _sourceFolder = new(FindSourceFolder);

    public static IReadOnlyList<string> ProjectNames() =>
        Directory.GetDirectories(_sourceFolder.Value)
            .Where(folder => File.Exists(ProjectFilePath(Path.GetFileName(folder))))
            .Select(folder => Path.GetFileName(folder))
            .Order(StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<string> ProjectReferences(string projectName) =>
        ReadIncludes(projectName, "ProjectReference")
            .Select(path => Path.GetFileNameWithoutExtension(path.Replace('\\', '/')))
            .ToList();

    public static IReadOnlyList<string> PackageReferences(string projectName) =>
        ReadIncludes(projectName, "PackageReference");

    private static List<string> ReadIncludes(string projectName, string itemName) =>
        XDocument.Load(ProjectFilePath(projectName))
            .Descendants(itemName)
            .Select(item => (string?)item.Attribute("Include"))
            .OfType<string>()
            .ToList();

    private static string ProjectFilePath(string projectName) =>
        Path.Combine(_sourceFolder.Value, projectName, projectName + ".csproj");

    private static string FindSourceFolder()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "OnboardingPortal.slnx")))
            {
                return Path.Combine(folder.FullName, "src");
            }
        }

        throw new InvalidOperationException("OnboardingPortal.slnx not found above " + AppContext.BaseDirectory);
    }
}
