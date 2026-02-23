using System;
using MsFileSystem = System.IO.Abstractions.IFileSystem;

namespace Clio.Common
{
	/// <summary>
	/// Provides infrastructure path resolution for Kubernetes infrastructure files.
	/// </summary>
	public interface IInfrastructurePathProvider
	{
		/// <summary>
		/// Gets the infrastructure path, using the provided custom path if specified,
		/// otherwise returns the default path from application settings.
		/// </summary>
		/// <param name="customPath">Optional custom path to infrastructure files</param>
		/// <returns>The resolved infrastructure path</returns>
		string GetInfrastructurePath(string customPath = null);
	}

	/// <summary>
	/// Default implementation of infrastructure path provider.
	/// </summary>
	public class InfrastructurePathProvider : IInfrastructurePathProvider
	{
		private readonly MsFileSystem _fileSystem;

		/// <summary>
		/// Initializes a new instance of <see cref="InfrastructurePathProvider"/>.
		/// </summary>
		/// <param name="fileSystem">File system abstraction used for cross-platform path composition.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="fileSystem"/> is null.</exception>
		public InfrastructurePathProvider(MsFileSystem fileSystem) {
			_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		}

		/// <summary>
		/// Gets the infrastructure path, using the provided custom path if specified,
		/// otherwise returns the default path from application settings.
		/// </summary>
		/// <param name="customPath">Optional custom path to infrastructure files</param>
		/// <returns>The resolved infrastructure path</returns>
		public string GetInfrastructurePath(string customPath = null)
		{
			if (!string.IsNullOrWhiteSpace(customPath))
			{
				return customPath;
			}
			
			return _fileSystem.Path.Join(SettingsRepository.AppSettingsFolderPath, "infrastructure");
		}
	}
}
