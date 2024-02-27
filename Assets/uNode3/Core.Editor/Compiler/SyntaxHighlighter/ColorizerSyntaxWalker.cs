using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MaxyGames.UNode.SyntaxHighlighter {
	internal class ColorizerSyntaxWalker : CSharpSyntaxWalker {
		private SemanticModel model;
		private Action<TokenKind, string> writeDelegate;
		public ColorizerSyntaxWalker() : base(SyntaxWalkerDepth.Token) {

		}

		public void DoVisit(SyntaxNode token, SemanticModel model, Action<TokenKind, string> writeDelegate) {
			this.model = model;
			this.writeDelegate = writeDelegate;
			Visit(token);
		}

		public override void VisitUsingDirective(UsingDirectiveSyntax node) {
			base.VisitUsingDirective(node);
		}

		public override void VisitToken(SyntaxToken token) {
			base.VisitLeadingTrivia(token);
			var isProcessed = false;
			if(token.IsKeyword()) {
				writeDelegate(TokenKind.Keyword, token.ValueText);
				isProcessed = true;
			} else {
				switch(token.Kind()) {
					case SyntaxKind.StringLiteralToken:
						writeDelegate(TokenKind.StringLiteral, token.Text);
						isProcessed = true;
						break;
					case SyntaxKind.CharacterLiteralToken:
						writeDelegate(TokenKind.CharacterLiteral, token.Text);
						isProcessed = true;
						break;
					case SyntaxKind.NumericLiteralToken:
						writeDelegate(TokenKind.NumericLiteral, token.Text);
						isProcessed = true;
						break;
					case SyntaxKind.IdentifierToken:
						if(token.Parent is SimpleNameSyntax) {
							var name = (SimpleNameSyntax)token.Parent;
							var symbolInfo = model.GetSymbolInfo(name);
							if(symbolInfo.Symbol != null && symbolInfo.Symbol.Kind != SymbolKind.ErrorType) {
								switch(symbolInfo.Symbol.Kind) {
									case SymbolKind.NamedType:
										writeDelegate(TokenKind.Identifier, token.ValueText);
										isProcessed = true;
										break;
									case SymbolKind.Namespace:
									case SymbolKind.Parameter:
									case SymbolKind.Local:
									case SymbolKind.Field:
									case SymbolKind.Property:
										writeDelegate(TokenKind.None, token.ValueText);
										isProcessed = true;
										break;
									default:
										break;
								}
							}
						} else if(token.Parent is TypeDeclarationSyntax) {
							var name = (TypeDeclarationSyntax)token.Parent;
							var symbol = model.GetDeclaredSymbol(name);
							if(symbol != null && symbol.Kind != SymbolKind.ErrorType) {
								switch(symbol.Kind) {
									case SymbolKind.NamedType:
										writeDelegate(TokenKind.Identifier, token.ValueText);
										isProcessed = true;
										break;
								}
							}
						}
						break;
				}
			}
			if(!isProcessed)
				HandleSpecialCaseIdentifiers(token);
			base.VisitTrailingTrivia(token);
		}

		private void HandleSpecialCaseIdentifiers(SyntaxToken token) {
			switch(token.Kind()) {
				case SyntaxKind.IdentifierToken:
					try {
						if ((token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.Parameter)
						  || (token.Parent.Kind() == SyntaxKind.EnumDeclaration)
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.Attribute)
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.CatchDeclaration)
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.ObjectCreationExpression)
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.ForEachStatement && !(token.GetNextToken().Kind() == SyntaxKind.CloseParenToken))
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Parent.Kind() == SyntaxKind.CaseSwitchLabel && !(token.GetPreviousToken().Kind() == SyntaxKind.DotToken))
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.MethodDeclaration)
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.CastExpression)
						  //e.g. "private static readonly HashSet patternHashSet = new HashSet();" the first HashSet in this case
						  || (token.Parent.Kind() == SyntaxKind.GenericName && token.Parent.Parent.Kind() == SyntaxKind.VariableDeclaration)
						  //e.g. "private static readonly HashSet patternHashSet = new HashSet();" the second HashSet in this case
						  || (token.Parent.Kind() == SyntaxKind.GenericName && token.Parent.Parent.Kind() == SyntaxKind.ObjectCreationExpression)
						  //e.g. "public sealed class BuilderRouteHandler : IRouteHandler" IRouteHandler in this case
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.BaseList)
						  //e.g. "Type baseBuilderType = typeof(BaseBuilder);" BaseBuilder in this case
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Parent.Parent.Kind() == SyntaxKind.TypeOfExpression)
						  // e.g. "private DbProviderFactory dbProviderFactory;" OR "DbConnection connection = dbProviderFactory.CreateConnection();"
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.VariableDeclaration)
						  // e.g. "DbTypes = new Dictionary();" DbType in this case
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.TypeArgumentList)
						  // e.g. "DbTypes.Add("int", DbType.Int32);" DbType in this case
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.SimpleMemberAccessExpression && token.Parent.Parent.Parent.Kind() == SyntaxKind.Argument && !(token.GetPreviousToken().Kind() == SyntaxKind.DotToken || Char.IsLower(token.ValueText[0])))
						  // e.g. "schemaCommand.CommandType = CommandType.Text;" CommandType in this case
						  || (token.Parent.Kind() == SyntaxKind.IdentifierName && token.Parent.Parent.Kind() == SyntaxKind.SimpleMemberAccessExpression && !(token.GetPreviousToken().Kind() == SyntaxKind.DotToken || Char.IsLower(token.ValueText[0])))
						  ) {
							writeDelegate(TokenKind.Identifier, token.ToString());
						} else {
							if (token.ValueText == "HashSet") {

							}
							writeDelegate(TokenKind.None, token.ToString());
						}
					} catch {
						goto default;
					}
					break;
				default:
					writeDelegate(TokenKind.None, token.ToString());
					break;
			}
		}

		public override void VisitTrivia(SyntaxTrivia trivia) {
			switch(trivia.Kind()) {
				case SyntaxKind.MultiLineCommentTrivia:
				case SyntaxKind.SingleLineCommentTrivia:
					writeDelegate(TokenKind.Comment, trivia.ToString());
					break;
				case SyntaxKind.DisabledTextTrivia:
					writeDelegate(TokenKind.DisabledText, trivia.ToString());
					break;
				case SyntaxKind.DocumentationCommentExteriorTrivia:
				case SyntaxKind.EndOfDocumentationCommentToken:
				case SyntaxKind.MultiLineDocumentationCommentTrivia:
				case SyntaxKind.SingleLineDocumentationCommentTrivia:
					writeDelegate(TokenKind.Comment, trivia.ToFullString());
					break;
				case SyntaxKind.RegionDirectiveTrivia:
				case SyntaxKind.EndRegionDirectiveTrivia:
					writeDelegate(TokenKind.Region, trivia.ToString());
					break;
				case SyntaxKind.IfDirectiveTrivia:
				case SyntaxKind.EndIfDirectiveTrivia:
				case SyntaxKind.ElseDirectiveTrivia:
				case SyntaxKind.ElifDirectiveTrivia:
					writeDelegate(TokenKind.None, trivia.ToFullString());
					break;
				case SyntaxKind.DefineDirectiveTrivia:
				case SyntaxKind.PragmaWarningDirectiveTrivia:
				case SyntaxKind.PragmaChecksumDirectiveTrivia:
					writeDelegate(TokenKind.None, trivia.ToFullString());
					break;
				default:
					writeDelegate(TokenKind.None, trivia.ToString());
					break;
			}
			base.VisitTrivia(trivia);
		}
	}
}