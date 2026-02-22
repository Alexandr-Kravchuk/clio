using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Clio.Common;
using Clio.Project.NuGet;

namespace Clio.Package.NuGet;

#region Class: NugetPackagesProvider

public class NugetPackagesProvider(HttpClient httpClient, ILogger logger) : INugetPackagesProvider{
	#region Class: Nested

	private record NugetPackageVersionsResponse(
		[property: JsonPropertyName("versions")]
		List<string> Versions);

	#endregion

	#region Methods: Private

	private static LastVersionNugetPackages FindLastVersionNugetPackages(AllVersionsNugetPackages packages) {
		return packages != null
			? new LastVersionNugetPackages(packages.Name, packages.Last, packages.Stable)
			: null;
	}

	private async Task<AllVersionsNugetPackages>
		FindAllVersionsNugetPackages(string packageName, string nugetSourceUrl) {
		List<string> allVersionsNugetPackage = await GetPackageVersionsAsync(packageName, nugetSourceUrl);
		IEnumerable<NugetPackage> packages = allVersionsNugetPackage
											 .Select(version =>
												 new NugetPackage(packageName, PackageVersion.ParseVersion(version)))
											 .ToImmutableList();
		return packages.Any()
			? new AllVersionsNugetPackages(packageName, packages)
			: null;
	}

	private async Task<List<string>> GetPackageVersionsAsync(string packageName, string nugetServer) {
		nugetServer = string.IsNullOrEmpty(nugetServer) ? "https://api.nuget.org" : nugetServer;
		string nugetApiUrl = $"{nugetServer}/v3-flatcontainer/{packageName.ToLower()}/index.json";
		List<string> versions = [];

		try {
			// Send GET request to NuGet API
			HttpResponseMessage response = await httpClient.GetAsync(nugetApiUrl);
			response.EnsureSuccessStatusCode();

			// Parse the response
			string jsonResponse = await response.Content.ReadAsStringAsync();
			NugetPackageVersionsResponse packageData =
				JsonSerializer.Deserialize<NugetPackageVersionsResponse>(jsonResponse);
			if (packageData?.Versions is { Count: > 0 }) {
				versions.AddRange(packageData.Versions);
			}
			else {
				logger.WriteWarning($"No versions found for package: {packageName}");
			}
		}
		catch (Exception ex) {
			logger.WriteError($"Error fetching package versions: {ex.Message}");
		}

		return versions;
	}

	#endregion

	#region Methods: Public

	public async Task<IEnumerable<LastVersionNugetPackages>> GetLastVersionPackages(IEnumerable<string> packagesNames,
		string nugetSourceUrl) {
		packagesNames.CheckArgumentNull(nameof(packagesNames));
		Task<AllVersionsNugetPackages>[] tasks = packagesNames
												 .Select(pkgName =>
													 FindAllVersionsNugetPackages(pkgName, nugetSourceUrl))
												 .ToArray();
		AllVersionsNugetPackages[] allPackages = await Task.WhenAll(tasks);
		return allPackages
				.Select(FindLastVersionNugetPackages)
				.Where(pkg => pkg != null);
	}

	public async Task<LastVersionNugetPackages> GetLastVersionPackages(string packageName, string nugetSourceUrl) {
		packageName.CheckArgumentNullOrWhiteSpace(nameof(packageName));
		IEnumerable<LastVersionNugetPackages> packages = await GetLastVersionPackages([packageName], nugetSourceUrl);
		return packages.FirstOrDefault();
	}

	#endregion
}

#endregion
