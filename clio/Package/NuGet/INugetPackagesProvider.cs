using System.Collections.Generic;
using System.Threading.Tasks;
using Clio.Project.NuGet;

namespace Clio.Package.NuGet
{
	/// <summary>
	/// Provides NuGet package version metadata from a configured NuGet source.
	/// </summary>
	public interface INugetPackagesProvider
	{
		/// <summary>
		/// Returns latest and stable version info for each package name.
		/// </summary>
		/// <param name="packagesNames">Package identifiers to resolve.</param>
		/// <param name="nugetSourceUrl">NuGet server base URL.</param>
		/// <returns>Resolved package versions.</returns>
		Task<IEnumerable<LastVersionNugetPackages>> GetLastVersionPackages(IEnumerable<string> packagesNames,
			string nugetSourceUrl);

		/// <summary>
		/// Returns latest and stable version info for a single package name.
		/// </summary>
		/// <param name="packageName">Package identifier to resolve.</param>
		/// <param name="nugetSourceUrl">NuGet server base URL.</param>
		/// <returns>Resolved package versions or <see langword="null"/> when not found.</returns>
		Task<LastVersionNugetPackages> GetLastVersionPackages(string packageName, string nugetSourceUrl);
	}
}
