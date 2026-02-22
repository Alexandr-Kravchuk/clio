using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Clio.Analyzers;

/// <summary>
///     Reports diagnostics when a type that is registered in DI is instantiated manually with <c>new</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DependencyInjectionManualConstructionAnalyzer : DiagnosticAnalyzer{
	private static readonly DiagnosticDescriptor Rule = new(
		"CLIO001",
		"Avoid manual construction of DI-registered services",
		"Type '{0}' is registered in DI and should be resolved from the container instead of using 'new'",
		"DependencyInjection",
		DiagnosticSeverity.Warning,
		true,
		"Behavior classes registered in dependency injection should not be manually constructed.");

	private static readonly ImmutableHashSet<string> RegistrationMethodNames = ImmutableHashSet.Create(
		"AddSingleton",
		"AddScoped",
		"AddTransient",
		"RegisterType",
		"RegisterInstance",
		"Register");

	#region Properties: Public

	/// <inheritdoc />
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

	#endregion

	#region Methods: Private

	private static void AnalyzeObjectCreation(
		SyntaxNodeAnalysisContext context,
		ImmutableHashSet<INamedTypeSymbol> registeredTypes) {
		ObjectCreationExpressionSyntax objectCreation = (ObjectCreationExpressionSyntax)context.Node;
		if (IsInsideRegistrationInvocation(context.SemanticModel, objectCreation, context.CancellationToken)) {
			return;
		}

		ITypeSymbol? creationType
			= context.SemanticModel.GetTypeInfo(objectCreation.Type, context.CancellationToken).Type;
		if (creationType is not INamedTypeSymbol namedType) {
			return;
		}

		if (namedType.IsRecord) {
			return;
		}

		if (!registeredTypes.Contains(namedType.OriginalDefinition)) {
			return;
		}

		Diagnostic diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation(), namedType.ToDisplayString());
		context.ReportDiagnostic(diagnostic);
	}

	private static ImmutableHashSet<INamedTypeSymbol> CollectRegisteredTypes(
		Compilation compilation,
		CancellationToken cancellationToken) {
		HashSet<INamedTypeSymbol> result = new(SymbolEqualityComparer.Default);

		foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees) {
			cancellationToken.ThrowIfCancellationRequested();
			SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
			SyntaxNode root = syntaxTree.GetRoot(cancellationToken);
			IEnumerable<InvocationExpressionSyntax> invocations
				= root.DescendantNodes().OfType<InvocationExpressionSyntax>();

			foreach (InvocationExpressionSyntax invocation in invocations) {
				cancellationToken.ThrowIfCancellationRequested();

				if (!TryGetRegistrationMethod(semanticModel, invocation, cancellationToken,
						out IMethodSymbol? methodSymbol)
					|| methodSymbol is null) {
					continue;
				}

				foreach (ITypeSymbol type in GetTypesFromInvocation(semanticModel, invocation, methodSymbol,
							 cancellationToken)) {
					if (type is not INamedTypeSymbol namedType) {
						continue;
					}

					if (namedType.TypeKind is TypeKind.Interface or TypeKind.Delegate) {
						continue;
					}

					if (namedType.IsAbstract) {
						continue;
					}

					result.Add(namedType.OriginalDefinition);
				}
			}
		}

		return result.ToImmutableHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
	}

	private static IEnumerable<ITypeSymbol> ExtractObjectCreationTypes(
		SemanticModel semanticModel,
		CSharpSyntaxNode body,
		CancellationToken cancellationToken) {
		IEnumerable<ObjectCreationExpressionSyntax> objectCreations = body is ExpressionSyntax expression
			? expression.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>()
			: body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();

		foreach (ObjectCreationExpressionSyntax creation in objectCreations) {
			ITypeSymbol? type = semanticModel.GetTypeInfo(creation.Type, cancellationToken).Type;
			if (type is not null) {
				yield return type;
			}
		}
	}

	private static IEnumerable<ITypeSymbol> GetTypesFromInvocation(
		SemanticModel semanticModel,
		InvocationExpressionSyntax invocation,
		IMethodSymbol methodSymbol,
		CancellationToken cancellationToken) {
		foreach (ITypeSymbol type in GetTypesFromTypeArguments(methodSymbol)) {
			yield return type;
		}

		foreach (ITypeSymbol type in GetTypesFromTypeOfArguments(semanticModel, invocation, cancellationToken)) {
			yield return type;
		}

		foreach (ITypeSymbol type in GetTypesFromRegistrationFactories(semanticModel, invocation, cancellationToken)) {
			yield return type;
		}
	}

	private static IEnumerable<ITypeSymbol> GetTypesFromRegistrationFactories(
		SemanticModel semanticModel,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken) {
		foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments) {
			if (argument.Expression is ParenthesizedLambdaExpressionSyntax parenthesizedLambda) {
				foreach (ITypeSymbol type in ExtractObjectCreationTypes(semanticModel, parenthesizedLambda.Body,
							 cancellationToken)) {
					yield return type;
				}
			}
			else if (argument.Expression is SimpleLambdaExpressionSyntax simpleLambda) {
				foreach (ITypeSymbol type in ExtractObjectCreationTypes(semanticModel, simpleLambda.Body,
							 cancellationToken)) {
					yield return type;
				}
			}
		}
	}

	private static IEnumerable<ITypeSymbol> GetTypesFromTypeArguments(IMethodSymbol methodSymbol) {
		ImmutableArray<ITypeSymbol> typeArguments = methodSymbol.TypeArguments;
		if (typeArguments.Length == 0) {
			yield break;
		}

		if (methodSymbol.Name == "RegisterType") {
			yield return typeArguments[0];
			yield break;
		}

		if (methodSymbol.Name is "AddSingleton" or "AddScoped" or "AddTransient") {
			if (typeArguments.Length >= 2) {
				yield return typeArguments[1];
				yield break;
			}

			yield return typeArguments[0];
		}
	}

	private static IEnumerable<ITypeSymbol> GetTypesFromTypeOfArguments(
		SemanticModel semanticModel,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken) {
		foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments) {
			if (argument.Expression is not TypeOfExpressionSyntax typeOfExpression) {
				continue;
			}

			ITypeSymbol? type = semanticModel.GetTypeInfo(typeOfExpression.Type, cancellationToken).Type;
			if (type is not null) {
				yield return type;
			}
		}
	}

	private static bool IsInsideRegistrationInvocation(
		SemanticModel semanticModel,
		ObjectCreationExpressionSyntax objectCreation,
		CancellationToken cancellationToken) {
		InvocationExpressionSyntax? invocation
			= objectCreation.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault();
		if (invocation is null) {
			return false;
		}

		return TryGetRegistrationMethod(semanticModel, invocation, cancellationToken, out var _);
	}

	private static bool IsTestAssembly(Compilation compilation) {
		string assemblyName = compilation.AssemblyName ?? string.Empty;
		return assemblyName.IndexOf("test", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool TryGetRegistrationMethod(
		SemanticModel semanticModel,
		InvocationExpressionSyntax invocation,
		CancellationToken cancellationToken,
		out IMethodSymbol? methodSymbol) {
		methodSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
		if (methodSymbol is null) {
			return false;
		}

		if (!RegistrationMethodNames.Contains(methodSymbol.Name)) {
			return false;
		}

		string fullTypeName = methodSymbol.ContainingType.ToDisplayString();
		string ns = methodSymbol.ContainingNamespace.ToDisplayString();

		return fullTypeName.StartsWith("Autofac.", StringComparison.Ordinal)
			   || ns.StartsWith("Autofac", StringComparison.Ordinal)
			   || ns.StartsWith("Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal);
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

			ImmutableHashSet<INamedTypeSymbol> registeredTypes = CollectRegisteredTypes(
				startContext.Compilation,
				startContext.CancellationToken);

			if (registeredTypes.Count == 0) {
				return;
			}

			startContext.RegisterSyntaxNodeAction(
				syntaxContext => AnalyzeObjectCreation(syntaxContext, registeredTypes),
				SyntaxKind.ObjectCreationExpression);
		});
	}

	#endregion
}
