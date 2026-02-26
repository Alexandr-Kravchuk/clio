namespace Clio.Workspaces
{
	using System;
	using Newtonsoft.Json;
	using System.Collections.Generic;

	#region Class: WorkspaceSettings

	public class WorkspaceSettings
	{

		#region Properties: Public

	public IList<string> Packages { get; set; } = new List<string>();
	public Version ApplicationVersion { get; set; }

	/// <summary>
	/// List of package names and masks to be ignored during workspace operations.
	/// </summary>
	public IList<string> IgnorePackages { get; set; } = new List<string>();

	/// <summary>
	/// List of external package names whose dependencies should be included in publish-app output.
	/// External packages are not pushed or restored from the server.
	/// </summary>
	public IList<string> ExternalPackages { get; set; } = new List<string>();

		#endregion

	}

	#endregion

}