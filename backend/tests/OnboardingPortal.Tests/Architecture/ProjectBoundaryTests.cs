using OnboardingPortal.Tests.TestSupport;

namespace OnboardingPortal.Tests.Architecture;

/// <summary>Enforces the dependency rule in docs/architecture/overview.md §6.</summary>
public sealed class ProjectBoundaryTests
{
    private static readonly Dictionary<string, string[]> _allowedProjectReferences = new()
    {
        ["OnboardingPortal.Domain"] = [],
        ["OnboardingPortal.Application"] = ["OnboardingPortal.Domain"],
        ["OnboardingPortal.Infrastructure"] = ["OnboardingPortal.Application", "OnboardingPortal.Domain"],
        ["OnboardingPortal.Api"] = ["OnboardingPortal.Application", "OnboardingPortal.Domain", "OnboardingPortal.Infrastructure"],
        ["OnboardingPortal.Worker"] = ["OnboardingPortal.Application", "OnboardingPortal.Domain", "OnboardingPortal.Infrastructure"],
    };

    public static TheoryData<string> SourceProjects => new(SourceTree.ProjectNames());

    [Fact]
    public void SourceProjects_EveryProject_HasADependencyRule()
    {
        var projects = SourceTree.ProjectNames();

        Assert.Equal(_allowedProjectReferences.Keys.Order(StringComparer.Ordinal), projects);
    }

    [Theory]
    [MemberData(nameof(SourceProjects))]
    public void ProjectReferences_OfEachProject_FollowTheDependencyRule(string projectName)
    {
        var references = SourceTree.ProjectReferences(projectName);

        var forbidden = references.Except(_allowedProjectReferences[projectName]);
        Assert.Empty(forbidden);
    }

    [Fact]
    public void PackageReferences_OfDomain_AreNone()
    {
        var packages = SourceTree.PackageReferences("OnboardingPortal.Domain");

        Assert.Empty(packages);
    }

    [Fact]
    public void PackageReferences_OfApplication_AreOnlyEntityFrameworkCore()
    {
        var packages = SourceTree.PackageReferences("OnboardingPortal.Application");

        Assert.All(packages, package => Assert.Equal("Microsoft.EntityFrameworkCore", package));
    }
}
