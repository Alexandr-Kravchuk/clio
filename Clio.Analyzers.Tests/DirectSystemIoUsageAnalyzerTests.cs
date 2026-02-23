using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Analyzers.Tests;

/// <summary>
/// Verifies <see cref="DirectSystemIoUsageAnalyzer"/> behavior for direct and abstracted filesystem usage.
/// </summary>
public sealed class DirectSystemIoUsageAnalyzerTests {
	[Test]
	[Description("Reports CLIO003 for direct static System.IO.File usage.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesFileReadAllText_ReturnsClio003Diagnostic() {
		// Arrange
		const string source = """
		                    using System.IO;
		                    public static class Sample {
		                    	public static string Read(string path) {
		                    		return File.ReadAllText(path);
		                    	}
		                    }
		                    """;
		DirectSystemIoUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "CLIO003", because: "direct System.IO.File usage should be flagged");
	}

	[Test]
	[Description("Reports CLIO003 for direct System.IO.Path usage.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesPathCombine_ReturnsClio003Diagnostic() {
		// Arrange
		const string source = """
		                    using System.IO;
		                    public static class Sample {
		                    	public static string Build(string left, string right) {
		                    		return Path.Combine(left, right);
		                    	}
		                    }
		                    """;
		DirectSystemIoUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "CLIO003", because: "direct System.IO.Path usage should be flagged");
	}

	[Test]
	[Description("Reports CLIO003 for direct object creation of System.IO types.")]
	public async Task RunAnalyzerAsync_WhenSourceCreatesSystemIoObjects_ReturnsClio003Diagnostics() {
		// Arrange
		const string source = """
		                    using System.IO;
		                    public static class Sample {
		                    	public static void Build(string path) {
		                    		FileInfo fileInfo = new(path);
		                    		DirectoryInfo directoryInfo = new(path);
		                    		using FileStream stream = new(path, FileMode.OpenOrCreate);
		                    	}
		                    }
		                    """;
		DirectSystemIoUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Count(d => d.Id == "CLIO003")
			.Should()
			.Be(3, because: "each direct System.IO object creation should be flagged");
	}

	[Test]
	[Description("Reports CLIO003 when direct System.IO access is referenced through an alias.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesSystemIoAlias_ReturnsClio003Diagnostic() {
		// Arrange
		const string source = """
		                    using IOFile = System.IO.File;
		                    public static class Sample {
		                    	public static bool Exists(string path) {
		                    		return IOFile.Exists(path);
		                    	}
		                    }
		                    """;
		DirectSystemIoUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "CLIO003", because: "aliases should resolve to the same forbidden type");
	}

	[Test]
	[Description("Does not report CLIO003 for System.IO.Abstractions usage.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesSystemIoAbstractions_ReturnsNoClio003Diagnostic() {
		// Arrange
		const string source = """
		                    using System.IO.Abstractions;
		                    public sealed class Sample {
		                    	public bool Exists(IFileSystem fileSystem, string path) {
		                    		return fileSystem.File.Exists(path);
		                    	}
		                    }
		                    """;
		DirectSystemIoUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "CLIO003", because: "abstraction-based filesystem access is the preferred pattern");
	}

	[Test]
	[Description("Does not report diagnostics when the compilation assembly name indicates test code.")]
	public async Task RunAnalyzerAsync_WhenAssemblyNameContainsTest_ReturnsNoClio003Diagnostic() {
		// Arrange
		const string source = """
		                    using System.IO;
		                    public static class Sample {
		                    	public static bool Exists(string path) {
		                    		return File.Exists(path);
		                    	}
		                    }
		                    """;
		DirectSystemIoUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer, "sample.tests");

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "CLIO003", because: "test assemblies are intentionally excluded from this rule");
	}

	[Test]
	[Description("Reports a single CLIO003 diagnostic for invocation expressions without duplicates.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesFileExists_ReturnsSingleClio003Diagnostic() {
		// Arrange
		const string source = """
		                    using System.IO;
		                    public static class Sample {
		                    	public static bool Exists(string path) {
		                    		return File.Exists(path);
		                    	}
		                    }
		                    """;
		DirectSystemIoUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Count(d => d.Id == "CLIO003")
			.Should()
			.Be(1, because: "invocation-based access should produce one diagnostic per offending expression");
	}
}
