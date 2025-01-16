using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace MaxyGames.UNode.Editors {
	using TypeInfo = Microsoft.CodeAnalysis.TypeInfo;

	public static class RoslynExtensions {
		public static void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right) {
			BinaryExpressionSyntax binaryExpression = (BinaryExpressionSyntax)node;
			left = binaryExpression.Left;
			operatorToken = binaryExpression.OperatorToken;
			right = binaryExpression.Right;
		}

		private static bool AnySymbolIsUserDefinedOperator(SymbolInfo symbolInfo) {
			if(IsUserDefinedOperator(symbolInfo.Symbol)) {
				return true;
			}
			ImmutableArray<ISymbol>.Enumerator enumerator = symbolInfo.CandidateSymbols.GetEnumerator();
			while(enumerator.MoveNext()) {
				if(IsUserDefinedOperator(enumerator.Current)) {
					return true;
				}
			}
			return false;
		}

		private static bool IsUserDefinedOperator(ISymbol symbol) {
			if(symbol is IMethodSymbol methodSymbol) {
				return methodSymbol.MethodKind == MethodKind.UserDefinedOperator;
			}
			return false;
		}

		private static bool IsFloatingPoint(TypeInfo typeInfo) {
			if(!IsFloatingPoint(typeInfo.Type)) {
				return IsFloatingPoint(typeInfo.ConvertedType);
			}
			return true;
		}

		private static bool IsFloatingPoint(ITypeSymbol type) {
			SpecialType? specialType = type?.SpecialType;
			if(specialType.HasValue) {
				SpecialType valueOrDefault = specialType.GetValueOrDefault();
				if((uint)(valueOrDefault - 18) <= 1u) {
					return true;
				}
			}
			return false;
		}

		public static bool IsSafeToChangeAssociativity<TBinaryExpressionSyntax>(TBinaryExpressionSyntax innerBinary, TBinaryExpressionSyntax parentBinary, SemanticModel semanticModel) where TBinaryExpressionSyntax : SyntaxNode {
			if(AnySymbolIsUserDefinedOperator(semanticModel.GetSymbolInfo(innerBinary))) {
				return false;
			}
			TypeInfo innerTypeInfo = semanticModel.GetTypeInfo(innerBinary);
			if(innerTypeInfo.Type != null && innerTypeInfo.ConvertedType != null && !innerTypeInfo.Type.Equals(innerTypeInfo.ConvertedType)) {
				return false;
			}
			if(innerTypeInfo.Type is IDynamicTypeSymbol) {
				return false;
			}
			GetPartsOfBinaryExpression(parentBinary, out var parentBinaryLeft, out _, out var parentBinaryRight);
			if(!object.Equals(semanticModel.GetTypeInfo(parentBinaryLeft).Type, semanticModel.GetTypeInfo(parentBinaryRight).Type)) {
				return false;
			}
			if(!object.Equals(semanticModel.GetTypeInfo(parentBinaryLeft).ConvertedType, semanticModel.GetTypeInfo(parentBinaryRight).ConvertedType)) {
				return false;
			}
			TypeInfo outerTypeInfo = semanticModel.GetTypeInfo(parentBinary);
			if(IsFloatingPoint(innerTypeInfo) || IsFloatingPoint(outerTypeInfo)) {
				return false;
			}
			if(semanticModel.GetOperation(parentBinary) is IBinaryOperation parentBinaryOp && semanticModel.GetOperation(innerBinary) is IBinaryOperation innerBinaryOp && (parentBinaryOp.IsChecked || innerBinaryOp.IsChecked) && (IsArithmetic(parentBinaryOp) || IsArithmetic(innerBinaryOp))) {
				return false;
			}
			return true;
		}

		static bool IsArithmetic(IBinaryOperation op) {
			BinaryOperatorKind operatorKind = op.OperatorKind;
			return operatorKind == BinaryOperatorKind.Add || operatorKind == BinaryOperatorKind.Subtract || operatorKind == BinaryOperatorKind.Multiply || operatorKind == BinaryOperatorKind.Divide;
		}

		public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind) {
			return (node?.Parent).IsKind(kind);
		}

		public static bool IsParentOf(this SyntaxNode node, SyntaxNode childNode) {
			var parent = childNode.Parent;
			while(parent != null) {
				if(node == parent) {
					return true;
				}
				parent = parent.Parent;
			}
			return false;
		}

		public static bool IsKind(this SyntaxNode node, params SyntaxKind[] kind) {
			if(node != null) {
				SyntaxKind syntaxKind = node.Kind();
				foreach(var k in kind) {
					if(syntaxKind == k) {
						return true;
					}
				}
			}
			return false;
		}

		public static bool IsKind<TNode>(this SyntaxNode node, SyntaxKind kind, out TNode result) where TNode : SyntaxNode {
			if(node.IsKind(kind)) {
				result = (TNode)node;
				return true;
			}
			result = null;
			return false;
		}

		public static bool IsParentKind<TNode>(this SyntaxNode node, SyntaxKind kind, out TNode result) where TNode : SyntaxNode {
			if(node.IsParentKind(kind)) {
				result = (TNode)node.Parent;
				return true;
			}
			result = null;
			return false;
		}

		public static bool IsLeftSideOfAnyAssignExpression(this SyntaxNode node) {
			if(node?.Parent is AssignmentExpressionSyntax assignment) {
				return assignment.Left == node;
			}
			return false;
		}

		public static bool IsAnyAssignExpression(this SyntaxNode node) {
			return SyntaxFacts.IsAssignmentExpression(node.Kind());
		}

		public static bool IsAnyLiteralExpression(this ExpressionSyntax expression) {
			return expression is LiteralExpressionSyntax;
		}

		public static OperatorPrecedence GetOperatorPrecedence(this ExpressionSyntax expression) {
			switch(expression.Kind()) {
				case SyntaxKind.InvocationExpression:
				case SyntaxKind.ElementAccessExpression:
				case SyntaxKind.AnonymousMethodExpression:
				case SyntaxKind.ObjectCreationExpression:
				case SyntaxKind.SimpleMemberAccessExpression:
				case SyntaxKind.PointerMemberAccessExpression:
				case SyntaxKind.ConditionalAccessExpression:
				case SyntaxKind.PostIncrementExpression:
				case SyntaxKind.PostDecrementExpression:
				case SyntaxKind.TypeOfExpression:
				case SyntaxKind.SizeOfExpression:
				case SyntaxKind.CheckedExpression:
				case SyntaxKind.UncheckedExpression:
				case SyntaxKind.DefaultExpression:
				case SyntaxKind.SuppressNullableWarningExpression:
					return OperatorPrecedence.Primary;
				case SyntaxKind.CastExpression:
				case SyntaxKind.UnaryPlusExpression:
				case SyntaxKind.UnaryMinusExpression:
				case SyntaxKind.BitwiseNotExpression:
				case SyntaxKind.LogicalNotExpression:
				case SyntaxKind.PreIncrementExpression:
				case SyntaxKind.PreDecrementExpression:
				case SyntaxKind.PointerIndirectionExpression:
				case SyntaxKind.AddressOfExpression:
				case SyntaxKind.AwaitExpression:
					return OperatorPrecedence.Unary;
				case SyntaxKind.RangeExpression:
					return OperatorPrecedence.Range;
				case SyntaxKind.MultiplyExpression:
				case SyntaxKind.DivideExpression:
				case SyntaxKind.ModuloExpression:
					return OperatorPrecedence.Multiplicative;
				case SyntaxKind.AddExpression:
				case SyntaxKind.SubtractExpression:
					return OperatorPrecedence.Additive;
				case SyntaxKind.LeftShiftExpression:
				case SyntaxKind.RightShiftExpression:
					return OperatorPrecedence.Shift;
				case SyntaxKind.IsPatternExpression:
				case SyntaxKind.LessThanExpression:
				case SyntaxKind.LessThanOrEqualExpression:
				case SyntaxKind.GreaterThanExpression:
				case SyntaxKind.GreaterThanOrEqualExpression:
				case SyntaxKind.IsExpression:
				case SyntaxKind.AsExpression:
					return OperatorPrecedence.RelationalAndTypeTesting;
				case SyntaxKind.EqualsExpression:
				case SyntaxKind.NotEqualsExpression:
					return OperatorPrecedence.Equality;
				case SyntaxKind.BitwiseAndExpression:
					return OperatorPrecedence.LogicalAnd;
				case SyntaxKind.ExclusiveOrExpression:
					return OperatorPrecedence.LogicalXor;
				case SyntaxKind.BitwiseOrExpression:
					return OperatorPrecedence.LogicalOr;
				case SyntaxKind.LogicalAndExpression:
					return OperatorPrecedence.ConditionalAnd;
				case SyntaxKind.LogicalOrExpression:
					return OperatorPrecedence.ConditionalOr;
				case SyntaxKind.CoalesceExpression:
					return OperatorPrecedence.NullCoalescing;
				case SyntaxKind.ConditionalExpression:
					return OperatorPrecedence.Conditional;
				case SyntaxKind.SimpleLambdaExpression:
				case SyntaxKind.ParenthesizedLambdaExpression:
				case SyntaxKind.SimpleAssignmentExpression:
				case SyntaxKind.AddAssignmentExpression:
				case SyntaxKind.SubtractAssignmentExpression:
				case SyntaxKind.MultiplyAssignmentExpression:
				case SyntaxKind.DivideAssignmentExpression:
				case SyntaxKind.ModuloAssignmentExpression:
				case SyntaxKind.AndAssignmentExpression:
				case SyntaxKind.ExclusiveOrAssignmentExpression:
				case SyntaxKind.OrAssignmentExpression:
				case SyntaxKind.LeftShiftAssignmentExpression:
				case SyntaxKind.RightShiftAssignmentExpression:
					return OperatorPrecedence.AssignmentAndLambdaExpression;
				case SyntaxKind.SwitchExpression:
					return OperatorPrecedence.Switch;
				default:
					return OperatorPrecedence.None;
			}
		}
	}

	public enum OperatorPrecedence {
		None,
		AssignmentAndLambdaExpression,
		Conditional,
		NullCoalescing,
		ConditionalOr,
		ConditionalAnd,
		LogicalOr,
		LogicalXor,
		LogicalAnd,
		Equality,
		RelationalAndTypeTesting,
		Shift,
		Additive,
		Multiplicative,
		Switch,
		Range,
		Unary,
		Primary
	}
}