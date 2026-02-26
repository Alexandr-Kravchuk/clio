using System.Collections.Generic;

namespace Clio.Workspaces;

/// <summary>
/// Resolves dependencies of external packages from descriptor.json files.
/// </summary>
public interface IExternalPackageDependencyResolver
{
	/// <summary>
	/// Resolves dependencies for external packages by reading their descriptor.json files,
	/// filtering out ignored and already-included packages.
	/// </summary>
	/// <param name="externalPackages">Names of external packages to resolve dependencies for.</param>
	/// <param name="ignorePatterns">Ignore patterns from IgnorePackages setting.</param>
	/// <param name="alreadyIncludedPackages">Packages already included (to avoid duplicates).</param>
	/// <param name="externalPackagesPath">Path to the folder containing external packages.</param>
	/// <returns>List of dependency package names to include.</returns>
	IEnumerable<string> ResolveDependencies(
		IEnumerable<string> externalPackages,
		IEnumerable<string> ignorePatterns,
		IEnumerable<string> alreadyIncludedPackages,
		string externalPackagesPath);
}
