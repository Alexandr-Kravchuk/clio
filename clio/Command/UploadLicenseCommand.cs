using System;
using Clio.Common;
using Clio.WebApplication;
using CommandLine;

namespace Clio.Command;

#region Class: UploadLicenseCommandOptions

[Verb("upload-license", Aliases = ["license", "loadlicense", "load-license"],
	HelpText = "Load license to selected environment")]
public class UploadLicenseCommandOptions : EnvironmentOptions{
	#region Properties: Public

	[Value(0, MetaName = "FilePath", Required = false, HelpText = "License file path")]
	public string FilePath { get; set; }

	#endregion
}

#endregion

#region Class: UploadLicenseCommand

public class UploadLicenseCommand : Command<UploadLicenseCommandOptions>{
	#region Fields: Private

	private readonly IApplication _application;
	private readonly ILogger _logger;

	#endregion

	#region Constructors: Public

	public UploadLicenseCommand(IApplication application, ILogger logger) {
		application.CheckArgumentNull(nameof(application));
		logger.CheckArgumentNull(nameof(logger));
		_application = application;
		_logger = logger;
	}

	#endregion

	#region Methods: Public

	public override int Execute(UploadLicenseCommandOptions options) {
		try {
			_application.LoadLicense(options.FilePath);
			_logger.WriteLine("Done");
			return 0;
		}
		catch (Exception e) {
			_logger.WriteLine(e.Message);
			return 1;
		}
	}

	#endregion
}

#endregion
