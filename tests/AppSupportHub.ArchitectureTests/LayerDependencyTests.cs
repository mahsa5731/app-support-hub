using System.Reflection;
using System.Xml.Linq;

namespace AppSupportHub.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string ApplicationAssemblyName = "AppSupportHub.Application";
    private const string DomainAssemblyName = "AppSupportHub.Domain";
    private const string InfrastructureAssemblyName = "AppSupportHub.Infrastructure";
    private const string RelationalPackageName = "Microsoft.EntityFrameworkCore.Relational";
    private const string TestcontainersPackageName = "Testcontainers.PostgreSql";
    private const string WebAssemblyName = "AppSupportHub.Web";

    [Fact]
    public void DomainDoesNotReferenceOuterLayers()
    {
        AssertDoesNotReference(
            AppSupportHub.Domain.AssemblyReference.Assembly,
            ApplicationAssemblyName,
            InfrastructureAssemblyName,
            WebAssemblyName);
    }

    [Fact]
    public void DomainDoesNotReferenceWebOrPersistenceFrameworks()
    {
        AssertDoesNotReferenceAssemblyPrefixes(
            AppSupportHub.Domain.AssemblyReference.Assembly,
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrWeb()
    {
        AssertDoesNotReference(
            AppSupportHub.Application.AssemblyReference.Assembly,
            InfrastructureAssemblyName,
            WebAssemblyName);
    }

    [Fact]
    public void ApplicationDoesNotReferenceWebOrPersistenceFrameworks()
    {
        AssertDoesNotReferenceAssemblyPrefixes(
            AppSupportHub.Application.AssemblyReference.Assembly,
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Npgsql");
    }

    [Fact]
    public void InfrastructureDoesNotReferenceWeb()
    {
        AssertDoesNotReference(
            AppSupportHub.Infrastructure.AssemblyReference.Assembly,
            WebAssemblyName);
    }

    [Fact]
    public void WebDoesNotDirectlyReferenceDomain()
    {
        AssertDoesNotReference(
            AppSupportHub.Web.AssemblyReference.Assembly,
            DomainAssemblyName);
    }

    [Fact]
    public void ProductionAssemblyReferencesFollowThePermittedGraph()
    {
        Dictionary<Assembly, HashSet<string>> permittedReferences = new()
        {
            [AppSupportHub.Domain.AssemblyReference.Assembly] = [],
            [AppSupportHub.Application.AssemblyReference.Assembly] =
                [DomainAssemblyName],
            [AppSupportHub.Infrastructure.AssemblyReference.Assembly] =
                [ApplicationAssemblyName, DomainAssemblyName],
            [AppSupportHub.Web.AssemblyReference.Assembly] =
                [ApplicationAssemblyName, InfrastructureAssemblyName],
        };

        foreach ((Assembly assembly, HashSet<string> permitted) in permittedReferences)
        {
            IEnumerable<string> productionReferences = GetReferencedAssemblyNames(assembly)
                .Where(IsProductionAssembly);

            foreach (string reference in productionReferences)
            {
                Assert.Contains(reference, permitted);
            }
        }
    }

    [Fact]
    public void ProductionProjectReferenceDeclarationsMatchThePermittedGraph()
    {
        Dictionary<string, string[]> expectedReferences = new(StringComparer.Ordinal)
        {
            [DomainAssemblyName] = [],
            [ApplicationAssemblyName] = [DomainAssemblyName],
            [InfrastructureAssemblyName] = [ApplicationAssemblyName, DomainAssemblyName],
            [WebAssemblyName] = [ApplicationAssemblyName, InfrastructureAssemblyName],
        };

        string solutionRoot = FindSolutionRoot();

        foreach ((string projectName, string[] expected) in expectedReferences)
        {
            string projectPath = Path.Combine(solutionRoot, "src", projectName, $"{projectName}.csproj");
            var projectDocument = XDocument.Load(projectPath);
            string[] actual = projectDocument
                .Descendants("ProjectReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .OfType<string>()
                .Select(path => Path.GetFileNameWithoutExtension(path.Replace('\\', '/')))
                .OfType<string>()
                .Order(StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
        }
    }

    [Fact]
    public void DomainAndApplicationProjectFilesHaveNoPackageReferences()
    {
        string solutionRoot = FindSolutionRoot();

        Assert.Empty(GetPackageReferences(solutionRoot, DomainAssemblyName));
        Assert.Empty(GetPackageReferences(solutionRoot, ApplicationAssemblyName));
    }

    [Fact]
    public void PersistencePackagesAreConfinedToInfrastructureAmongProductionProjects()
    {
        string solutionRoot = FindSolutionRoot();
        string[] productionProjects =
            [DomainAssemblyName, ApplicationAssemblyName, InfrastructureAssemblyName, WebAssemblyName];

        foreach (string projectName in productionProjects)
        {
            var persistencePackages = GetPackageReferences(solutionRoot, projectName)
                .Where(IsPersistencePackage)
                .ToHashSet(StringComparer.Ordinal);

            if (projectName == InfrastructureAssemblyName)
            {
                Assert.Equal(
                    [
                        "Microsoft.EntityFrameworkCore",
                        "Microsoft.EntityFrameworkCore.Design",
                        RelationalPackageName,
                        "Npgsql.EntityFrameworkCore.PostgreSQL",
                    ],
                    persistencePackages.Order(StringComparer.Ordinal));
            }
            else
            {
                Assert.Empty(persistencePackages);
            }
        }

        Assert.Equal(
            "10.0.11",
            GetCentralPackageVersion(solutionRoot, RelationalPackageName));
    }

    [Fact]
    public void TestcontainersPostgreSqlIsReferencedOnlyByIntegrationTests()
    {
        string solutionRoot = FindSolutionRoot();
        Dictionary<string, string> projects = new(StringComparer.Ordinal)
        {
            [DomainAssemblyName] = Path.Combine("src", DomainAssemblyName),
            [ApplicationAssemblyName] = Path.Combine("src", ApplicationAssemblyName),
            [InfrastructureAssemblyName] = Path.Combine("src", InfrastructureAssemblyName),
            [WebAssemblyName] = Path.Combine("src", WebAssemblyName),
            ["AppSupportHub.UnitTests"] = Path.Combine("tests", "AppSupportHub.UnitTests"),
            ["AppSupportHub.IntegrationTests"] = Path.Combine("tests", "AppSupportHub.IntegrationTests"),
            ["AppSupportHub.ArchitectureTests"] = Path.Combine("tests", "AppSupportHub.ArchitectureTests"),
        };

        foreach ((string projectName, string projectDirectory) in projects)
        {
            string projectPath = Path.Combine(
                solutionRoot,
                projectDirectory,
                $"{projectName}.csproj");
            bool referencesTestcontainers = GetPackageReferences(projectPath)
                .Contains(TestcontainersPackageName);

            Assert.Equal(projectName == "AppSupportHub.IntegrationTests", referencesTestcontainers);
        }
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenAssemblyNames)
    {
        HashSet<string> referencedAssemblyNames = GetReferencedAssemblyNames(assembly);

        foreach (string forbiddenAssemblyName in forbiddenAssemblyNames)
        {
            Assert.DoesNotContain(forbiddenAssemblyName, referencedAssemblyNames);
        }
    }

    private static HashSet<string> GetReferencedAssemblyNames(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    private static void AssertDoesNotReferenceAssemblyPrefixes(
        Assembly assembly,
        params string[] forbiddenPrefixes)
    {
        HashSet<string> referencedAssemblyNames = GetReferencedAssemblyNames(assembly);

        Assert.DoesNotContain(
            referencedAssemblyNames,
            reference => forbiddenPrefixes.Any(prefix => reference.StartsWith(
                prefix,
                StringComparison.Ordinal)));
    }

    private static bool IsProductionAssembly(string assemblyName)
    {
        return assemblyName is DomainAssemblyName
            or ApplicationAssemblyName
            or InfrastructureAssemblyName
            or WebAssemblyName;
    }

    private static bool IsPersistencePackage(string packageName)
    {
        return packageName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || packageName.StartsWith("Npgsql", StringComparison.Ordinal);
    }

    private static HashSet<string> GetPackageReferences(string solutionRoot, string projectName)
    {
        string projectPath = Path.Combine(solutionRoot, "src", projectName, $"{projectName}.csproj");
        return GetPackageReferences(projectPath);
    }

    private static HashSet<string> GetPackageReferences(string projectPath)
    {
        return XDocument.Load(projectPath)
            .Descendants("PackageReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string? GetCentralPackageVersion(string solutionRoot, string packageName)
    {
        return XDocument.Load(Path.Combine(solutionRoot, "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .Single(package => package.Attribute("Include")?.Value == packageName)
            .Attribute("Version")
            ?.Value;
    }

    private static string FindSolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AppSupportHub.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the AppSupportHub solution root.");
    }
}
