using System.Reflection;
using System.Xml.Linq;

namespace AppSupportHub.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string ApplicationAssemblyName = "AppSupportHub.Application";
    private const string DomainAssemblyName = "AppSupportHub.Domain";
    private const string InfrastructureAssemblyName = "AppSupportHub.Infrastructure";
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
    public void ApplicationDoesNotReferenceInfrastructureOrWeb()
    {
        AssertDoesNotReference(
            AppSupportHub.Application.AssemblyReference.Assembly,
            InfrastructureAssemblyName,
            WebAssemblyName);
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

    private static bool IsProductionAssembly(string assemblyName)
    {
        return assemblyName is DomainAssemblyName
            or ApplicationAssemblyName
            or InfrastructureAssemblyName
            or WebAssemblyName;
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
