using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Analyzers.Tests;

/// <summary>
/// Verifies <see cref="DirectProcessUsageAnalyzer"/> behavior for direct process usage.
/// </summary>
public sealed class DirectProcessUsageAnalyzerTests {
	[Test]
	[Description("Reports CLIO004 when System.Diagnostics.Process.Start is used directly.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesProcessStart_ReturnsClio004Diagnostic() {
		// Arrange
		const string source = """
		                    using System.Diagnostics;
		                    public static class Sample {
		                    	public static void Run() {
		                    		Process.Start("dotnet", "--info");
		                    	}
		                    }
		                    """;
		DirectProcessUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "CLIO004",
			because: "direct process start should be routed through IProcessExecutor");
	}

	[Test]
	[Description("Reports CLIO004 for direct Process object creation.")]
	public async Task RunAnalyzerAsync_WhenSourceCreatesProcess_ReturnsClio004Diagnostic() {
		// Arrange
		const string source = """
		                    using System.Diagnostics;
		                    public static class Sample {
		                    	public static void Run() {
		                    		Process process = new();
		                    	}
		                    }
		                    """;
		DirectProcessUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "CLIO004",
			because: "direct process creation bypasses IProcessExecutor");
	}

	[Test]
	[Description("Reports CLIO004 for direct ProcessStartInfo creation.")]
	public async Task RunAnalyzerAsync_WhenSourceCreatesProcessStartInfo_ReturnsClio004Diagnostic() {
		// Arrange
		const string source = """
		                    using System.Diagnostics;
		                    public static class Sample {
		                    	public static void Run() {
		                    		ProcessStartInfo startInfo = new("dotnet", "--info");
		                    	}
		                    }
		                    """;
		DirectProcessUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "CLIO004",
			because: "direct ProcessStartInfo construction bypasses IProcessExecutor");
	}

	[Test]
	[Description("Reports CLIO004 when Process is accessed through an alias.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesProcessAlias_ReturnsClio004Diagnostic() {
		// Arrange
		const string source = """
		                    using DiagProcess = System.Diagnostics.Process;
		                    public static class Sample {
		                    	public static void Run() {
		                    		DiagProcess.Start("dotnet", "--info");
		                    	}
		                    }
		                    """;
		DirectProcessUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "CLIO004",
			because: "aliases should still resolve to forbidden process APIs");
	}

	[Test]
	[Description("Does not report CLIO004 for test assemblies.")]
	public async Task RunAnalyzerAsync_WhenAssemblyNameContainsTest_ReturnsNoClio004Diagnostic() {
		// Arrange
		const string source = """
		                    using System.Diagnostics;
		                    public static class Sample {
		                    	public static void Run() {
		                    		Process.Start("dotnet", "--info");
		                    	}
		                    }
		                    """;
		DirectProcessUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer, "sample.tests");

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "CLIO004",
			because: "test assemblies are intentionally excluded from CLIO analyzers");
	}

	[Test]
	[Description("Reports one CLIO004 diagnostic for one direct Process.Start invocation.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesSingleProcessStart_ReturnsSingleClio004Diagnostic() {
		// Arrange
		const string source = """
		                    using System.Diagnostics;
		                    public static class Sample {
		                    	public static void Run() {
		                    		Process.Start("dotnet", "--info");
		                    	}
		                    }
		                    """;
		DirectProcessUsageAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Count(d => d.Id == "CLIO004")
			.Should()
			.Be(1, because: "the analyzer should avoid duplicate diagnostics for the same invocation expression");
	}
}
