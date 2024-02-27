using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace MaxyGames.UNode.Editors {
	public static class CSharpDocGenerator {
		const string documentationDirectory = "uNode3Data/XML_Documentation/";

		[MenuItem("Tools/uNode/Update/Script Documentation", false, 100000)]
		public static void GenerateDocumentation() {
			if(uNodeUtility.IsProVersion == false) {
				uNodeEditorUtility.DisplayRequiredProVersion();
				return;
			}
			try {
				if(Directory.Exists(documentationDirectory)) {
					Directory.Delete(documentationDirectory, true);
				}
				var assemblies = CompilationPipeline.GetAssemblies();
				for(int i = 0; i < assemblies.Length; i++) {
					var assembly = assemblies[i];
					EditorUtility.DisplayProgressBar($"Generating Documentation - {i + 1} of {assemblies.Length}", $"Generating {assembly.name} documentation with {assembly.sourceFiles.Length} scripts.", (float)i / (float)assemblies.Length);
					GenerateDocumentation(assembly);
				}
				XmlDoc.ReloadDocInBackground();
			}
			finally {
				EditorProgressBar.ClearProgressBar();
				EditorUtility.ClearProgressBar();
			}
		}

		private static void GenerateDocumentation(UnityEditor.Compilation.Assembly assembly) {
			//CompileResult result = new CompileResult();
			try {
				List<MetadataReference> references = new List<MetadataReference>();
				foreach(var reference in assembly.allReferences) {
					references.Add(MetadataReference.CreateFromFile(reference));
				}
				List<string> preprocessorSymbols = new List<string>();
				foreach(var symbol in assembly.defines) {
					//if(symbol.StartsWith("UNITY_EDITOR", StringComparison.Ordinal))
					//	continue;
					preprocessorSymbols.Add(symbol);
				}
				List<EmbeddedText> embeddedTexts = new List<EmbeddedText>();
				var syntaxTrees = new List<Microsoft.CodeAnalysis.SyntaxTree>();
				for(int i = 0; i < assembly.sourceFiles.Length; i++) {
					var path = assembly.sourceFiles[i];
					var script = File.ReadAllText(path);
					var buffer = System.Text.Encoding.UTF8.GetBytes(script);
					var sourceText = SourceText.From(buffer, buffer.Length, System.Text.Encoding.UTF8, canBeEmbedded: true);
					var tree = CSharpSyntaxTree.ParseText(
						text: sourceText,
						options: new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols),
						path: path);
					syntaxTrees.Add(CSharpSyntaxTree.Create(tree.GetRoot() as CSharpSyntaxNode, null, path, System.Text.Encoding.UTF8));
					embeddedTexts.Add(EmbeddedText.FromSource(path, tree.GetText()));
				}
				var compilation = CSharpCompilation.Create(
					assembly.name,
					syntaxTrees: syntaxTrees,
					references: references,
					options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug, allowUnsafe: assembly.compilerOptions.AllowUnsafeCode));
				using(var assemblyStream = new MemoryStream())
				using(var symbolsStream = new MemoryStream())
				using(var docStream = new MemoryStream()) {
					var emitOptions = new EmitOptions(
						debugInformationFormat: DebugInformationFormat.PortablePdb,
						pdbFilePath: Path.ChangeExtension(assembly.name, "pdb"));
					var emitResult = compilation.Emit(
						assemblyStream,
						symbolsStream,
						xmlDocumentationStream: docStream,
						embeddedTexts: embeddedTexts,
						options: emitOptions);
					if(emitResult.Success) {
						//assemblyStream.Seek(0, SeekOrigin.Begin);
						//symbolsStream?.Seek(0, SeekOrigin.Begin);
						//if(useDebug) {
						//	result.assembly = Assembly.Load(assemblyStream.ToArray(), symbolsStream.ToArray());
						//} else {
						//	result.assembly = Assembly.Load(assemblyStream.ToArray());
						//}
						Directory.CreateDirectory(documentationDirectory);
						var savePath = documentationDirectory + assembly.name + ".xml";
						using(var fileStream = new FileStream(savePath, FileMode.OpenOrCreate)) {
							docStream.WriteTo(fileStream);
						}
					} else {
						var failures = emitResult.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);
						List<CompileResult.CompileError> compileErrors = new List<CompileResult.CompileError>();
						foreach(var d in failures) {
							//Debug.LogError($"{d.Id} - {d.GetMessage()}");
							string errorMessage = d.GetMessage();
							int column = 0;
							int line = 0;
							string fileName = string.Empty;
							if(d.Location != null && d.Location.IsInSource && (d.Location.GetLineSpan().IsValid || d.Location.GetMappedLineSpan().IsValid)) {
								var span = d.Location.GetMappedLineSpan().IsValid ? d.Location.GetMappedLineSpan() : d.Location.GetLineSpan();
								line = span.Span.Start.Line + 1;
								column = span.Span.Start.Character + 1;
								fileName = d.Location.SourceTree?.FilePath;
								if(d.Location.IsInSource) {
									errorMessage += " | source script: " + d.Location.SourceTree.ToString().Substring(d.Location.SourceSpan.Start, d.Location.SourceSpan.Length);
								}
							}
							compileErrors.Add(new CompileResult.CompileError() {
								errorColumn = column,
								errorLine = line,
								fileName = fileName,
								errorNumber = d.Id,
								isWarning = d.Severity == DiagnosticSeverity.Warning,
								errorText = errorMessage
							});
						}
						string error = null;
						foreach(var e in compileErrors) {
							error = error.AddLineInEnd() + e.errorMessage;
						}
						Debug.LogError("Error from assembly: " + assembly.name + "\n" + error);
					}
				}
			}
			catch(Exception ex) {
				Debug.LogError(ex);
			}
		}
	}

	class DocSyntaxVisitor : CSharpSyntaxVisitor {

	}

	class DocSymbolVisitor : SymbolVisitor {
		public override void VisitNamespace(INamespaceSymbol symbol) {
			foreach(var child in symbol.GetMembers()) {
				child.Accept(this);
			}
		}

		public override void VisitNamedType(INamedTypeSymbol symbol) {
			foreach(var child in symbol.GetMembers()) {
				child.Accept(this);
			}
		}

		public override void VisitMethod(IMethodSymbol symbol) {
			Debug.Log(symbol);
		}
	}
}