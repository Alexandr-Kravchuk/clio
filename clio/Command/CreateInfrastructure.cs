using System;
using System.Collections.Generic;
using System.Reflection;
using Clio.Common;
using CommandLine;
using ConsoleTables;

namespace Clio.Command;

[Verb("create-k8-files", Aliases = ["ck8f"], HelpText = "Prepare K8 files for deployment")]
public class CreateInfrastructureOptions{
	#region Properties: Public

	[Option('p', "path", Required = false,
		HelpText = "Path to infrastructure files (default: auto-detected from clio settings)")]
	public string InfrastructurePath { get; set; }

	[Option("pg-limit-memory", Required = false, Default = "4Gi",
		HelpText = "PostgreSQL memory limit (default: 4Gi)")]
	public string PostgresLimitMemory { get; set; }

	[Option("pg-limit-cpu", Required = false, Default = "2",
		HelpText = "PostgreSQL CPU limit (default: 2)")]
	public string PostgresLimitCpu { get; set; }

	[Option("pg-request-memory", Required = false, Default = "2Gi",
		HelpText = "PostgreSQL memory request (default: 2Gi)")]
	public string PostgresRequestMemory { get; set; }

	[Option("pg-request-cpu", Required = false, Default = "1",
		HelpText = "PostgreSQL CPU request (default: 1)")]
	public string PostgresRequestCpu { get; set; }

	[Option("mssql-limit-memory", Required = false, Default = "4Gi",
		HelpText = "MSSQL memory limit (default: 4Gi)")]
	public string MssqlLimitMemory { get; set; }

	[Option("mssql-limit-cpu", Required = false, Default = "2",
		HelpText = "MSSQL CPU limit (default: 2)")]
	public string MssqlLimitCpu { get; set; }

	[Option("mssql-request-memory", Required = false, Default = "2Gi",
		HelpText = "MSSQL memory request (default: 2Gi)")]
	public string MssqlRequestMemory { get; set; }

	[Option("mssql-request-cpu", Required = false, Default = "1",
		HelpText = "MSSQL CPU request (default: 1)")]
	public string MssqlRequestCpu { get; set; }

	#endregion
}

public class CreateInfrastructureCommand(IFileSystem fileSystem, IInfrastructurePathProvider infrastructurePathProvider,
	ILogger logger) : Command<CreateInfrastructureOptions>{
	
	#region Methods: Public

	public override int Execute(CreateInfrastructureOptions options) {
		string to = infrastructurePathProvider.GetInfrastructurePath(options.InfrastructurePath);
		string location = fileSystem.GetFilesInfos(Assembly.GetExecutingAssembly().Location).DirectoryName;
		string from = fileSystem.NormalizeFilePathByPlatform($"{location}/tpl/k8/infrastructure");
		fileSystem.CopyDirectory(from, to, true);

		// Process template files with variable substitution
		Dictionary<string, string> replacements = new() {
			{ "{{PG_LIMIT_MEMORY}}", options.PostgresLimitMemory },
			{ "{{PG_LIMIT_CPU}}", options.PostgresLimitCpu },
			{ "{{PG_REQUEST_MEMORY}}", options.PostgresRequestMemory },
			{ "{{PG_REQUEST_CPU}}", options.PostgresRequestCpu },
			{ "{{MSSQL_LIMIT_MEMORY}}", options.MssqlLimitMemory },
			{ "{{MSSQL_LIMIT_CPU}}", options.MssqlLimitCpu },
			{ "{{MSSQL_REQUEST_MEMORY}}", options.MssqlRequestMemory },
			{ "{{MSSQL_REQUEST_CPU}}", options.MssqlRequestCpu }
		};

		// Process PostgreSQL StatefulSet
		string postgresStatefulSetPath =
			fileSystem.NormalizeFilePathByPlatform($"{to}/postgres/postgres-stateful-set.yaml");
		if (fileSystem.ExistsFile(postgresStatefulSetPath)) {
			string content = fileSystem.ReadAllText(postgresStatefulSetPath);
			foreach (KeyValuePair<string, string> kvp in replacements) {
				content = content.Replace(kvp.Key, kvp.Value);
			}

			fileSystem.WriteAllTextToFile(postgresStatefulSetPath, content);
		}

		// Process MSSQL StatefulSet
		string mssqlStatefulSetPath =
			fileSystem.NormalizeFilePathByPlatform($"{to}/mssql/mssql-stateful-set.yaml");
		if (fileSystem.ExistsFile(mssqlStatefulSetPath)) {
			string content = fileSystem.ReadAllText(mssqlStatefulSetPath);
			foreach (KeyValuePair<string, string> kvp in replacements) {
				content = content.Replace(kvp.Key, kvp.Value);
			}

			fileSystem.WriteAllTextToFile(mssqlStatefulSetPath, content);
		}


		// Display resource configuration
		logger.WriteLine();
		logger.WriteLine("Resource Configuration:");
		logger.WriteLine(
			$"  PostgreSQL: Memory Limit={options.PostgresLimitMemory}, CPU Limit={options.PostgresLimitCpu}");
		logger.WriteLine(
			$"              Memory Request={options.PostgresRequestMemory}, CPU Request={options.PostgresRequestCpu}");
		logger.WriteLine($"  MSSQL:      Memory Limit={options.MssqlLimitMemory}, CPU Limit={options.MssqlLimitCpu}");
		logger.WriteLine(
			$"              Memory Request={options.MssqlRequestMemory}, CPU Request={options.MssqlRequestCpu}");
		logger.WriteLine();

		logger.WriteLine(ConsoleLogger.WrapRed("****************************  IMPORTANT ****************************"));
		logger.WriteLine("All files have been copied to:");
		logger.WriteLine($"\t{to}");
		logger.WriteLine();
		logger.WriteLine(ConsoleLogger.WrapRed("1. Make sure to review files and change values if needed"));
		logger.WriteLine(
			ConsoleLogger.WrapRed(
				"2. If you have more than one cluster configured, make sure to switch to Rancher Desktop"));
		logger.WriteLine();

		logger.WriteLine("Files Include:");
		ConsoleTable t = new("Application", "Version", "Available on");
		t.Rows.Add(["Postgres SQL Server", "latest", "Port: 5432"]);
		t.Rows.Add(["Microsoft SQL Server 2022", "latest developer edition", "Port: 1434"]);
		t.Rows.Add(["Redis Server", "latest", "Port: 6379"]);
		t.Rows.Add(["Email Listener", "1.0.10", "Port: 1090"]);

		logger.PrintTable(t);
		logger.WriteLine();

		logger.WriteLine();
		logger.WriteLine(ConsoleLogger.WrapRed("Clio will not deploy infrastructure automatically"));
		logger.WriteLine();

		logger.WriteLine($"To deploy new infrastructure execute from {to} folder in any terminal:");
		logger.WriteLine(ConsoleLogger.WrapYellow("\tkubectl apply -f infrastructure"));
		logger.WriteLine();
		logger.WriteLine("Use Rancher Desktop to check if infrastructure is deployed correctly");

		return 0;
	}

	#endregion
}
