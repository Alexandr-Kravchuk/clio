using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Clio.Common;
using CommandLine;

namespace Clio.Command;

[Verb("open-k8-files", Aliases = ["cfg-k8f", "cfg-k8s", "cfg-k8"], HelpText = "Open folder K8 files for deployment")]
public class OpenInfrastructureOptions{ }

public class OpenInfrastructureCommand : Command<OpenInfrastructureOptions>{
	#region Fields: Private

	private readonly IInfrastructurePathProvider _infrastructurePathProvider;

	#endregion

	#region Constructors: Public

	public OpenInfrastructureCommand(IInfrastructurePathProvider infrastructurePathProvider) {
		_infrastructurePathProvider = infrastructurePathProvider;
	}

	#endregion

	#region Methods: Public

	public override int Execute(OpenInfrastructureOptions options) {
		string infrsatructureCfgFilesFolder = _infrastructurePathProvider.GetInfrastructurePath();
		try {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				Process.Start("explorer.exe", infrsatructureCfgFilesFolder);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				Process.Start("open", infrsatructureCfgFilesFolder);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				Process.Start("xdg-open", infrsatructureCfgFilesFolder);
			}
			else {
				Console.WriteLine($"Unsupported platform: {RuntimeInformation.OSDescription}");
				return 1;
			}

			return 0;
		}
		catch (Exception e) {
			Console.WriteLine($"Failed to open folder: {e.Message}");
			Console.WriteLine($"Folder path: {infrsatructureCfgFilesFolder}");
			return 1;
		}
	}

	#endregion
}
