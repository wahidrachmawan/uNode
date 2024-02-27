using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using MaxyGames.UNode.Editors;

namespace MaxyGames.UNode.SyntaxHighlighter {
	public static class CSharpSyntaxHighlighter {
		private static Dictionary<TokenKind, string> colorMap;
		private static Assembly[] assemblies;
		private static List<MetadataReference> metadataReferences;

		static void Initialize() {
			if(colorMap == null) {
				colorMap = new Dictionary<TokenKind, string>() {
					{TokenKind.None, "#ffffff" },
					{TokenKind.Keyword, EditorGUIUtility.isProSkin ? "#4470DE" : "#085FE7" },
					{TokenKind.StringLiteral, EditorGUIUtility.isProSkin ? "#D76C28" : "#a31515" },
					{TokenKind.NumericLiteral, EditorGUIUtility.isProSkin ? "#C1C136" : "#CD00FF"},
					{TokenKind.CharacterLiteral , "#d202fe" },
					{TokenKind.Identifier , "#2b91af" },
					{TokenKind.Comment , "#008000" },
					{TokenKind.DisabledText , "#008000" },
					{TokenKind.Region  , "#e0e0e0" },
				};
			}
		}

		private static List<MetadataReference> GetMetadataReferences() {
			if(metadataReferences != null) {
				return metadataReferences;
			}
			List<MetadataReference> references = new List<MetadataReference>();
			if(assemblies == null) {
				assemblies = RoslynUtility.Data.GetAssemblies();
			}
			foreach(var assembly in assemblies) {
				try {
					if(assembly != null && !string.IsNullOrEmpty(assembly.Location)) {
						//Skip AssetStoreTools assembly
						if(assembly.GetName().Name.StartsWith("AssetStoreTools", StringComparison.Ordinal))
							continue;
						references.Add(MetadataReference.CreateFromFile(assembly.Location));
						Thread.Sleep(1);
					}
				} catch { continue; }
			}
			metadataReferences = references;
			return references;
		}

		public static string GetRichTextAsync(string sourceCode) {
			List<string> preprocessorSymbols = new List<string>();
			uNodeThreadUtility.Queue(() => {
				Initialize();
				foreach(var symbol in UnityEditor.EditorUserBuildSettings.activeScriptCompilationDefines) {
					if(symbol.StartsWith("UNITY_EDITOR", StringComparison.Ordinal))
						continue;
					preprocessorSymbols.Add(symbol);
				}
			});
			uNodeThreadUtility.WaitUntilEmpty();
			var tree = CSharpSyntaxTree.ParseText(sourceCode, new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols));
			var compilation = CSharpCompilation.Create("CSharpParser",
				syntaxTrees: new[] { tree }, references: GetMetadataReferences());
			var model = compilation.GetSemanticModel(tree, true);
			if(model == null) {
				//throw new Exception("Unable to analize the script");
				return null;
			}

			//var root = (CompilationUnitSyntax)tree.GetRoot();
			SyntaxNode root = Task.Run(async () => await tree.GetRootAsync()).Result;
			var walker = new ColorizerSyntaxWalker();

			var builder = new StringBuilder();
			walker.DoVisit(root, model, (tk, text) => {
				switch(tk) {
					case TokenKind.None:
						builder.Append(text);
						break;
					case TokenKind.Keyword:
					case TokenKind.Identifier:
						builder.Append("<color=" + colorMap[tk] + ">" + text + "</color>");
						break;
					case TokenKind.StringLiteral:
					case TokenKind.CharacterLiteral:
					case TokenKind.Comment:
					case TokenKind.DisabledText:
					case TokenKind.Region:
						if(!text.Contains("\n")) {
							builder.Append("<color=" + colorMap[tk] + ">" + text + "</color>");
							break;
						}
						string left = "<color=" + colorMap[tk] + ">";
						string right = "</color>"; {
							string[] str = text.Split('\n');
							for(int i = 0; i < str.Length; i++) {
								str[i] = left + str[i] + right;
							}
							builder.Append(string.Join("\n", str));
						}
						break;
					default:
						builder.Append(text);
						break;
				}
			});
			return builder.ToString();
		}

		public static string GetRichText(string sourceCode) {
			Initialize();
			SemanticModel model;
			var root = Editors.RoslynUtility.GetSyntaxTree(sourceCode, out model);
			if(model == null) {
				//throw new Exception("Unable to analize the script");
				return null;
			}
			var walker = new ColorizerSyntaxWalker();

			var builder = new StringBuilder();
			walker.DoVisit(root, model, (tk, text) => {
				switch(tk) {
					case TokenKind.None:
						builder.Append(text);
						break;
					case TokenKind.Keyword:
					case TokenKind.Identifier:
					case TokenKind.NumericLiteral:
						builder.Append("<color=" + colorMap[tk] + ">" + text + "</color>");
						break;
					case TokenKind.StringLiteral:
					case TokenKind.CharacterLiteral:
					case TokenKind.Comment:
					case TokenKind.DisabledText:
					case TokenKind.Region:
						if(!text.Contains("\n")) {
							builder.Append("<color=" + colorMap[tk] + ">" + text + "</color>");
							break;
						}
						string left = "<color=" + colorMap[tk] + ">";
						string right = "</color>"; {
							string[] str = text.Split('\n');
							for(int i = 0; i < str.Length; i++) {
								str[i] = left + str[i] + right;
							}
							builder.Append(string.Join("\n", str));
						}
						break;
					default:
						builder.Append(text);
						break;
				}
			});
			return builder.ToString();
		}
	}

	internal enum TokenKind {
		None,
		Keyword,
		Identifier,
		StringLiteral,
		NumericLiteral,
		CharacterLiteral,
		Comment,
		DisabledText,
		Region
	}
}