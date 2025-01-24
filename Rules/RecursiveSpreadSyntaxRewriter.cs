using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Vibe;

public class RecursiveSpreadSyntaxRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
    {
        return node;
    }
    public override SyntaxNode VisitInitializerExpression(InitializerExpressionSyntax node)
    {
        var propertySources = node.Expressions.Select(expression =>
        {

            // Handle nested object initializers recursively
            if (expression is AssignmentExpressionSyntax assignment &&
                assignment.Right is InitializerExpressionSyntax nestedInitializer)
            {
                var nestedObject = VisitInitializerExpression(nestedInitializer);
                return SyntaxFactory.ParseExpression(
                    $"new KeyValuePair<string, object>(\"{assignment.Left}\", {nestedObject})"
                );
            }
            // Handle nested object initializers recursively
            if (expression is AssignmentExpressionSyntax aassignment &&
                aassignment.Right is ImplicitArrayCreationExpressionSyntax arrayconverter)
            {
                return SyntaxFactory.ParseExpression(
                    $"new KeyValuePair<string, object>(\"{aassignment.Left}\", new {aassignment.Right.ToFullString().Replace(":","=")})"
                );
            }

            // Handle explicit properties with lambdas
            if (expression is AssignmentExpressionSyntax explicitProperty &&
                explicitProperty.Right is LambdaExpressionSyntax lambdaExpression)
            {
                // Determine the lambda's delegate type
                var delegateType = DetermineDelegateType(lambdaExpression);

                // Transform lambda into a casted delegate
                var castedLambda = SyntaxFactory.ParseExpression(
                    $"({delegateType})({lambdaExpression.ToFullString()})"
                );

                return SyntaxFactory.ParseExpression(
                    $"new KeyValuePair<string, object>(\"{explicitProperty.Left}\", {castedLambda})"
                );
            }

            // Handle explicit properties (e.g., id: "1")
            if (expression is AssignmentExpressionSyntax Property)
            {
                return SyntaxFactory.ParseExpression(
                    $"new KeyValuePair<string, object>(\"{Property.Left}\", {Property.Right})"
                );
            }

            // Handle spreads (e.g., ...props)
            if (expression is ImplicitElementAccessSyntax spread &&
                spread.ArgumentList.Arguments.Count == 1 &&
                spread.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax identifier)
            {
                return SyntaxFactory.ParseExpression(identifier.Identifier.Text);
            }

            return null;
        }).Where(e => e != null).ToList();

        // Combine explicit properties and spreads into a dynamic object
        var dynamicObjectCall = SyntaxFactory.ParseExpression(
            $"DynamicObjectHelper.CreateDynamicObject(new object[] {{ {string.Join(", ", propertySources)} }})"
        );

        return dynamicObjectCall;
    }

    private string DetermineDelegateType(LambdaExpressionSyntax lambda)
    {
        // Check if the lambda is asynchronous
        var isAsync = lambda.AsyncKeyword != default;

        // Count parameters to determine the delegate type
        var parameterList = lambda switch
        {
            SimpleLambdaExpressionSyntax simpleLambda => new[] { simpleLambda.Parameter },
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters,
            _ => Enumerable.Empty<ParameterSyntax>()
        };

        var parameters = parameterList.Select(p => p.Type?.ToString() ?? "object").ToArray();

        // Check if the lambda has a return type
        var hasReturnStatement = lambda.Body.DescendantNodes().OfType<ReturnStatementSyntax>().Any();

        if (!hasReturnStatement)
        {
            // If no return, it's an Action (or Task if async)
            return parameters.Length switch
            {
                0 => isAsync ? "Func<Task>" : "Action",
                _ => isAsync ? $"Func<{string.Join(", ", parameters)}, Task>" : $"Action<{string.Join(", ", parameters)}>"
            };
        }

        // If async and has a return type, it's Func<Task<T>>
        if (isAsync)
        {
            return parameters.Length switch
            {
                0 => "Func<Task<object>>",
                1 => $"Func<{parameters[0]}, Task<object>>",
                _ => $"Func<{string.Join(", ", parameters)}, Task<object>>"
            };
        }

        // Otherwise, it's a regular Func
        return parameters.Length switch
        {
            0 => "Func<object>",
            1 => $"Func<{parameters[0]}, object>",
            _ => $"Func<{string.Join(", ", parameters)}, object>"
        };
    }
}
