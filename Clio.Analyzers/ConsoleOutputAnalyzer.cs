using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Clio.Analyzers;

/// <summary>
/// Reports diagnostics when code writes output through <see cref="System.Console"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConsoleOutputAnalyzer : DiagnosticAnalyzer {
	private static readonly DiagnosticDescriptor Rule = new(
		"CLIO002",
		"Avoid direct Console output",
		"Use ConsoleLogger/ILogger instead of Console.{0}",
		"Logging",
		DiagnosticSeverity.Warning,
		true,
		"Prefer centralized logging abstraction instead of direct Console output.");

	private static readonly ImmutableHashSet<string> ConsoleOutputMethods = ImmutableHashSet.Create(
		"Write",
		"WriteLine");

	#region Properties: Public

	/// <inheritdoc />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	#endregion

	#region Methods: Private

	private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
		InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
		IMethodSymbol? methodSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken)
			.Symbol as IMethodSymbol;
		if (methodSymbol is null) {
			return;
		}

		if (!ConsoleOutputMethods.Contains(methodSymbol.Name)) {
			return;
		}

		INamedTypeSymbol containingType = methodSymbol.ContainingType;
		if (containingType is null || containingType.ToDisplayString() != "System.Console") {
			return;
		}

		INamedTypeSymbol? containingClass = context.ContainingSymbol?.ContainingType;
		if (containingClass?.Name == "ConsoleLogger"
			&& containingClass.ContainingNamespace.ToDisplayString().StartsWith("Clio", StringComparison.Ordinal)) {
			return;
		}

		Diagnostic diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), methodSymbol.Name);
		context.ReportDiagnostic(diagnostic);
	}

	private static bool IsTestAssembly(Compilation compilation) {
		string assemblyName = compilation.AssemblyName ?? string.Empty;
		return assemblyName.IndexOf("test", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	#endregion

	#region Methods: Public

	/// <inheritdoc />
	public override void Initialize(AnalysisContext context) {
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(startContext => {
			if (IsTestAssembly(startContext.Compilation)) {
				return;
			}

			startContext.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
		});
	}

	#endregion
}
