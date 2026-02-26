using System.Collections.Generic;
using System.IO;
using System.Linq;
using Clio.Common;
using Clio.Package;

namespace Clio.Workspaces;

/// <summary>
/// Resolves dependencies of external packages by reading descriptor.json files
/// from the external packages folder.
/// </summary>
public class ExternalPackageDependencyResolver : IExternalPackageDependencyResolver
{
	private readonly IFileSystem _fileSystem;
	private readonly IJsonConverter _jsonConverter;
	private readonly ILogger _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExternalPackageDependencyResolver"/> class.
	/// </summary>
	/// <param name="fileSystem">File system abstraction.</param>
	/// <param name="jsonConverter">JSON converter for deserializing descriptor files.</param>
	/// <param name="logger">Logger for diagnostic output.</param>
	public ExternalPackageDependencyResolver(IFileSystem fileSystem, IJsonConverter jsonConverter, ILogger logger) {
		fileSystem.CheckArgumentNull(nameof(fileSystem));
		jsonConverter.CheckArgumentNull(nameof(jsonConverter));
		logger.CheckArgumentNull(nameof(logger));
		_fileSystem = fileSystem;
		_jsonConverter = jsonConverter;
		_logger = logger;
	}

	/// <inheritdoc />
	public IEnumerable<string> ResolveDependencies(
		IEnumerable<string> externalPackages,
		IEnumerable<string> ignorePatterns,
		IEnumerable<string> alreadyIncludedPackages,
		string externalPackagesPath) {
		if (externalPackages == null || string.IsNullOrWhiteSpace(externalPackagesPath)) {
			return Enumerable.Empty<string>();
		}

		List<string> externalList = externalPackages.ToList();
		if (!externalList.Any()) {
			return Enumerable.Empty<string>();
		}

		List<string> ignoreList = ignorePatterns?.ToList() ?? new List<string>();
		HashSet<string> alreadyIncluded = new(alreadyIncludedPackages ?? Enumerable.Empty<string>());
		HashSet<string> externalSet = new(externalList);
		HashSet<string> resolved = new();

		foreach (string packageName in externalList) {
			ResolveDependenciesRecursive(packageName, externalPackagesPath, ignoreList,
				alreadyIncluded, externalSet, resolved);
		}

		if (resolved.Any()) {
			_logger.WriteInfo(
				$"Resolved {resolved.Count} external dependency package(s): {string.Join(", ", resolved)}");
		}

		return resolved;
	}

	private void ResolveDependenciesRecursive(
		string packageName,
		string externalPackagesPath,
		List<string> ignorePatterns,
		HashSet<string> alreadyIncluded,
		HashSet<string> externalSet,
		HashSet<string> resolved) {
		string descriptorPath = Path.Combine(externalPackagesPath, packageName, CreatioPackage.DescriptorName);

		if (!_fileSystem.ExistsFile(descriptorPath)) {
			_logger.WriteWarning($"Descriptor not found for external package '{packageName}': {descriptorPath}");
			return;
		}

		PackageDescriptorDto descriptorDto;
		try {
			descriptorDto = _jsonConverter.DeserializeObjectFromFile<PackageDescriptorDto>(descriptorPath);
		} catch (System.Exception ex) {
			_logger.WriteWarning(
				$"Failed to read descriptor for external package '{packageName}': {ex.Message}");
			return;
		}

		if (descriptorDto?.Descriptor?.DependsOn == null || !descriptorDto.Descriptor.DependsOn.Any()) {
			return;
		}

		foreach (PackageDependency dependency in descriptorDto.Descriptor.DependsOn) {
			string depName = dependency.Name;

			if (string.IsNullOrWhiteSpace(depName)) {
				continue;
			}

			// Skip if already included in workspace Packages
			if (alreadyIncluded.Contains(depName)) {
				continue;
			}

			// Skip if already in external packages list
			if (externalSet.Contains(depName)) {
				continue;
			}

			// Skip if already resolved
			if (resolved.Contains(depName)) {
				continue;
			}

			// Skip if matches ignore patterns
			if (ignorePatterns.Any() && PackageIgnoreMatcher.IsIgnored(depName, ignorePatterns)) {
				_logger.WriteInfo($"Skipping ignored dependency '{depName}' of external package '{packageName}'");
				continue;
			}

			// Check if the dependency folder exists in external packages path
			string depFolderPath = Path.Combine(externalPackagesPath, depName);
			if (!_fileSystem.ExistsDirectory(depFolderPath)) {
				continue;
			}

			resolved.Add(depName);

			// Recursively resolve dependencies of this dependency
			ResolveDependenciesRecursive(depName, externalPackagesPath, ignorePatterns,
				alreadyIncluded, externalSet, resolved);
		}
	}
}
