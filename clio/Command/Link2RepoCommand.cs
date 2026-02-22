using System;
using Clio.Common;
using CommandLine;

namespace Clio.Command;

[Verb("link-to-repository", Aliases = ["l2r", "link2repo"], HelpText = "Link environment package(s) to repository.")]
internal class Link2RepoOptions{
	#region Properties: Public

	[Option('r', "repoPath", Required = true,
		HelpText = "Path to package repository folder", Default = null)]
	public string RepoPath { get; set; }

	[Option('e', "envPkgPath", Required = true,
		HelpText
			= @"Path to environment package folder ({LOCAL_CREATIO_PATH}Terrasoft.WebApp\Terrasoft.Configuration\Pkg)",
		Default = null)]
	public string EnvPkgPath { get; set; }

	#endregion
}

internal class Link2RepoCommand(RfsEnvironment rfsEnvironment, ILogger logger) : Command<Link2RepoOptions>{

	#region Methods: Public

	public override int Execute(Link2RepoOptions options) {
		try {
			if (OperationSystem.Current.IsWindows) {
				rfsEnvironment.Link2Repo(options.EnvPkgPath, options.RepoPath);
				logger.WriteLine("Done.");
				return 0;
			}

			logger.WriteLine("Clio mklink command is only supported on: 'windows'.");
			return 1;
		}
		catch (Exception e) {
			logger.WriteError(e.Message);
			return 1;
		}
	}

	#endregion
}
