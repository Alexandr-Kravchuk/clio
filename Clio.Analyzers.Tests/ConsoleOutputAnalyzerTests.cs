using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Analyzers.Tests;

/// <summary>
/// Verifies <see cref="ConsoleOutputAnalyzer"/> diagnostics for direct console writes.
/// </summary>
public sealed class ConsoleOutputAnalyzerTests {
	[Test]
	[Description("Reports CLIO002 when System.Console.WriteLine is used directly.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesConsoleWriteLine_ReturnsClio002Diagnostic() {
		// Arrange
		const string source = """
		                    using System;
		                    public static class Sample {
		                    	public static void Print() {
		                    		Console.WriteLine("hello");
		                    	}
		                    }
		                    """;
		ConsoleOutputAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "CLIO002",
			because: "direct console writes should be routed through logger abstractions");
	}

	[Test]
	[Description("Reports CLIO002 when System.Console.Write is used directly.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesConsoleWrite_ReturnsClio002Diagnostic() {
		// Arrange
		const string source = """
		                    using System;
		                    public static class Sample {
		                    	public static void Print() {
		                    		Console.Write("hello");
		                    	}
		                    }
		                    """;
		ConsoleOutputAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().ContainSingle(d => d.Id == "CLIO002",
			because: "Console.Write is also disallowed by the analyzer");
	}

	[Test]
	[Description("Does not report CLIO002 in Clio.* ConsoleLogger implementation.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesConsoleInsideConsoleLogger_ReturnsNoClio002Diagnostic() {
		// Arrange
		const string source = """
		                    namespace Clio.Common {
		                    	public sealed class ConsoleLogger {
		                    		public void WriteLine(string value) {
		                    			System.Console.WriteLine(value);
		                    		}
		                    	}
		                    }
		                    """;
		ConsoleOutputAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "CLIO002",
			because: "ConsoleLogger is the expected implementation layer for direct console output");
	}

	[Test]
	[Description("Does not report CLIO002 for test assemblies.")]
	public async Task RunAnalyzerAsync_WhenAssemblyNameContainsTest_ReturnsNoClio002Diagnostic() {
		// Arrange
		const string source = """
		                    using System;
		                    public static class Sample {
		                    	public static void Print() {
		                    		Console.WriteLine("hello");
		                    	}
		                    }
		                    """;
		ConsoleOutputAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer, "sample.tests");

		// Assert
		diagnostics.Should().NotContain(d => d.Id == "CLIO002",
			because: "the analyzer intentionally ignores test assemblies");
	}

	[Test]
	[Description("Reports exactly one CLIO002 diagnostic per direct console invocation.")]
	public async Task RunAnalyzerAsync_WhenSourceUsesSingleConsoleWriteLine_ReturnsSingleClio002Diagnostic() {
		// Arrange
		const string source = """
		                    using System;
		                    public static class Sample {
		                    	public static void Print() {
		                    		Console.WriteLine("hello");
		                    	}
		                    }
		                    """;
		ConsoleOutputAnalyzer analyzer = new();

		// Act
		var diagnostics = await AnalyzerTestRunner.RunAnalyzerAsync(source, analyzer);

		// Assert
		diagnostics.Count(d => d.Id == "CLIO002")
			.Should()
			.Be(1, because: "a single offending invocation should produce one diagnostic");
	}
}
