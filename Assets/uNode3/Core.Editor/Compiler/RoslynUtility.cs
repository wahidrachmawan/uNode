using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Syntax = Microsoft.CodeAnalysis.SyntaxTree;
using UnityEditor.Compilation;
using Assembly = System.Reflection.Assembly;
using System.Threading;

namespace MaxyGames.UNode.Editors {
	public class CompileResult {
		public byte[] rawAssembly;
		public byte[] rawPdb;

		public bool isSuccess => rawAssembly != null && rawAssembly.Length > 0;

		public IEnumerable<CompileError> errors;

		public void LogErrors() {
			if(errors != null) {
				foreach(var error in errors) {
					if(error.isWarning) {
						Debug.LogWarning(error.errorMessage);
					}
					else {
						Debug.LogError(error.errorMessage);
					}
				}
			}
		}

		private Assembly _assembly;
		public Assembly LoadAssembly() {
			if(_assembly == null) {
				if(rawAssembly != null) {
					if(rawPdb != null) {
						_assembly = Assembly.Load(rawAssembly, rawPdb);
					}
					else {
						_assembly = Assembly.Load(rawAssembly);
					}
				}
			}
			return _assembly;
		}

		public string GetErrorMessage() {
			if(errors != null) {
				System.Text.StringBuilder builder = new System.Text.StringBuilder();
				foreach(var error in errors) {
					if(!error.isWarning) {
						builder.AppendLine(error.errorMessage);
						builder.AppendLine();
					}
				}
				return builder.ToString();
			}
			return string.Empty;
		}

		public class CompileError {
			public string fileName;
			public bool isWarning;
			public string errorText;
			public string errorNumber;
			public int errorLine;
			public int errorColumn;

			public string errorMessage {
				get {
					if(string.IsNullOrEmpty(fileName)) {
						return $"({errorNumber}): {errorText}\nin line: {errorLine}:{errorColumn}";
					}
					string path = Directory.GetCurrentDirectory();
					if(fileName.StartsWith(path)) {
						return $"({errorNumber}): {errorText}\nin line: {errorLine}:{errorColumn} (at {fileName.Remove(0, path.Length + 1).Replace("\\", "/")}:{errorLine})";
					}
					return $"({errorNumber}): {errorText}\nin line: {errorLine}:{errorColumn}\nin file: {fileName}";
				}
			}
		}
	}

	public static class RoslynUtility {
		public static IList<Assembly> assemblies;

		#region Data
		public static class Data {
			public static Func<bool> useSourceGenerators;
			public static Func<Assembly[]> GetAssemblies;
			public static Func<CompilationMethod> compilationMethod;
			public static string tempAssemblyPath;
		}
		#endregion

		#region Compiles
		public static CompileResult CompileScript(IEnumerable<string> scripts) {
			return CompileScript(CachedData.randomAssemblyName, scripts);
		}

		public static CompileResult CompileScript(string assemblyName, IEnumerable<string> scripts) {
			if(scripts == null) {
				throw new ArgumentNullException(nameof(scripts));
			}
			var trees = GetSyntaxTrees(scripts);
			return DoCompile(assemblyName, trees);
		}

		public static CompileResult CompileFiles(IEnumerable<string> files) {
			return CompileFiles(CachedData.randomAssemblyName, files);
		}

		public static CompileResult CompileFiles(string assemblyName, IEnumerable<string> files) {
			if(files == null) {
				throw new ArgumentNullException(nameof(files));
			}
			var trees = GetSyntaxTreesFromFiles(files, out var embeddedTexts);
			return DoCompile(assemblyName, trees, embeddedTexts);
		}

		public static CompileResult CompileScriptAndSave(string assemblyName, IEnumerable<string> scripts, string savePath, bool loadAssembly) {
			if(scripts == null) {
				throw new ArgumentNullException(nameof(scripts));
			}
			var trees = GetSyntaxTrees(scripts);
			return DoCompileAndSave(assemblyName, trees, savePath, loadAssembly: loadAssembly);
		}

		public static CompileResult CompileFilesAndSave(string assemblyName, IEnumerable<string> files, string savePath, bool loadAssembly) {
			if(files == null) {
				throw new ArgumentNullException(nameof(files));
			}
			var trees = GetSyntaxTreesFromFiles(files, out var embeddedTexts);
			return DoCompileAndSave(assemblyName, trees, savePath, embeddedTexts, loadAssembly: loadAssembly);
		}
		#endregion

		#region Private Functions
		static class CachedData {
			public static bool? hasDefaultAssembly;
			public static UnityEditor.Compilation.Assembly assemblyCSharp;

			public static ISourceGenerator[] sourceGenerators;
			public static IIncrementalGenerator[] incrementalGenerators;

			static int _index;
			internal static string randomAssemblyName => Path.GetRandomFileName() + (++_index).ToString();
		}
		static UnityEditor.Compilation.Assembly AssemblyCSharp {
			get {
				if(CachedData.assemblyCSharp == null && CachedData.hasDefaultAssembly == null) {
					CachedData .hasDefaultAssembly = false;
					uNodeThreadUtility.RunOnMainThread(() => {
						var assemblies = CompilationPipeline.GetAssemblies();
						for(int i = 0; i < assemblies.Length; i++) {
							var assembly = assemblies[i];
							if(assembly.name == "Assembly-CSharp") {
								CachedData.assemblyCSharp = assembly;
								CachedData.hasDefaultAssembly = true;
							}
						}
					});
				}
				return CachedData.assemblyCSharp;
			}
		}

		private static void AppendSourceGenerators(Assembly assembly, ref List<ISourceGenerator> sourceGenerators, ref List<IIncrementalGenerator> incrementalGenerators) {
			try {
				foreach(var type in assembly.GetTypes()) {
					if(typeof(ISourceGenerator).IsAssignableFrom(type)) {
						if(ReflectionUtils.CanCreateInstance(type)) {
							sourceGenerators.Add(ReflectionUtils.CreateInstance(type) as ISourceGenerator);
						}
					}
					else if(typeof(IIncrementalGenerator).IsAssignableFrom(type)) {
						if(ReflectionUtils.CanCreateInstance(type)) {
							incrementalGenerators.Add(ReflectionUtils.CreateInstance(type) as IIncrementalGenerator);
						}
					}
				}
			}
#if UNODE_DEBUG
			catch(Exception ex) {
				Debug.LogError(assembly.ToString());
				Debug.LogException(ex);
			}
#else
			catch { }
#endif
		}

		private static void GetRoslynGenerators(out ISourceGenerator[] sourceGenerators, out IIncrementalGenerator[] incrementalGenerators) {
			//if(SourceGenData.sourceGenerators != null) {
			//	sourceGenerators = SourceGenData.sourceGenerators;
			//	incrementalGenerators = SourceGenData.incrementalGenerators;
			//}
			if(AssemblyCSharp != null) {
				var assembly = AssemblyCSharp;
				var paths = assembly.compilerOptions.RoslynAnalyzerDllPaths.ToHashSet();

				List<ISourceGenerator> sGenerators = new List<ISourceGenerator>();
				List<IIncrementalGenerator> iGenerators = new List<IIncrementalGenerator>();
				List<Assembly> assemblies = new List<Assembly>();
				foreach(var path in paths) {
					assemblies.Add(Assembly.Load(File.ReadAllBytes(path)));
				}
				foreach(var ass in assemblies) {
					AppendSourceGenerators(ass, ref sGenerators, ref iGenerators);
					ReflectionUtils.RegisterPrivateLoadedAssembly(ass);
				}
				CachedData.sourceGenerators = sourceGenerators = sGenerators.ToArray();
				CachedData.incrementalGenerators = incrementalGenerators = iGenerators.ToArray();
			}
			else {
				sourceGenerators = Array.Empty<ISourceGenerator>();
				incrementalGenerators = Array.Empty<IIncrementalGenerator>();
			}
		}

		private static List<MetadataReference> GetDefaultReferences() {
			if(AssemblyCSharp != null) {
				List<MetadataReference> references = new List<MetadataReference>();
				foreach(var reference in AssemblyCSharp.allReferences) {
					references.Add(MetadataReference.CreateFromFile(reference));
				}
				references.Add(MetadataReference.CreateFromFile(AssemblyCSharp.outputPath));
				return references;
			}
			return null;
		}

		private static List<MetadataReference> GetMetadataReferences() {
			if(assemblies != null) {
				List<MetadataReference> result = new List<MetadataReference>();
				foreach(var assembly in assemblies) {
					try {
						if(assembly != null && !string.IsNullOrEmpty(assembly.Location)) {
							//Skip AssetStoreTools assembly
							if(assembly.GetName().Name.StartsWith("AssetStoreTools", StringComparison.Ordinal))
								continue;
							result.Add(MetadataReference.CreateFromFile(assembly.Location));
						}
					}
					catch { continue; }
				}
				return result;
			}
			var defaultReferences = GetDefaultReferences();
			if(defaultReferences != null) {
				return defaultReferences;
			}
			List<MetadataReference> references = new List<MetadataReference>();
			foreach(var assembly in Data.GetAssemblies()) {
				try {
					if(assembly != null && !string.IsNullOrEmpty(assembly.Location)) {
						//Skip AssetStoreTools assembly
						if(assembly.GetName().Name.StartsWith("AssetStoreTools", StringComparison.Ordinal))
							continue;
						references.Add(MetadataReference.CreateFromFile(assembly.Location));
					}
				}
				catch { continue; }
			}
			if(Data.compilationMethod() == CompilationMethod.Roslyn) {
				if(File.Exists(Data.tempAssemblyPath)) {
					references.Add(MetadataReference.CreateFromFile(Data.tempAssemblyPath));
				}
			}
			return references;
		}

		private static List<string> GetPreprocessorSymbols() {
			List<string> preprocessorSymbols = new List<string>();
			if(uNodeUtility.IsInMainThread) {
				foreach(var symbol in UnityEditor.EditorUserBuildSettings.activeScriptCompilationDefines) {
					if(symbol.StartsWith("UNITY_EDITOR", StringComparison.Ordinal))
						continue;
					preprocessorSymbols.Add(symbol);
				}
			} else {
				uNodeThreadUtility.QueueAndWait(() => {
					foreach(var symbol in UnityEditor.EditorUserBuildSettings.activeScriptCompilationDefines) {
						if(symbol.StartsWith("UNITY_EDITOR", StringComparison.Ordinal))
							continue;
						preprocessorSymbols.Add(symbol);
					}
				});
			}
			return preprocessorSymbols;
		}

		public static List<Syntax> GetSyntaxTrees(IEnumerable<string> scripts) {
			var result = new List<Syntax>();
			foreach(var script in scripts) {
				var tree = CSharpSyntaxTree.ParseText(script, new CSharpParseOptions(preprocessorSymbols: GetPreprocessorSymbols()));
				result.Add(tree);
			}
			return result;
		}

		public static List<Syntax> GetSyntaxTreesFromFiles(IEnumerable<string> paths, out List<EmbeddedText> embeddedTexts) {
			return GetSyntaxTreesFromFiles(paths, out embeddedTexts, GetPreprocessorSymbols());
		}

		public static List<Syntax> GetSyntaxTreesFromFiles(IEnumerable<string> paths, out List<EmbeddedText> embeddedTexts, IEnumerable<string> preprocessorSymbols = null) {
			var result = new List<Syntax>();
			embeddedTexts = new List<EmbeddedText>();
			foreach(var path in paths) {
				var script = File.ReadAllText(path);
				var buffer = System.Text.Encoding.UTF8.GetBytes(script);
				var sourceText = SourceText.From(buffer, buffer.Length, System.Text.Encoding.UTF8, canBeEmbedded: true);
				//var tree = CSharpSyntaxTree.ParseText(
				//	text: script,
				//	options: new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols),
				//	path: path);
				var tree = CSharpSyntaxTree.ParseText(
						sourceText,
						options: new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols),
						path: path);
				result.Add(tree);
				embeddedTexts.Add(EmbeddedText.FromSource(path, sourceText));
			}
			return result;
		}

		private static CompileResult DoCompile(
			string assemblyName,
			IEnumerable<Syntax> syntaxTrees,
			List<EmbeddedText> embeddedTexts = null
			) {
			CompileResult result = new CompileResult();
			var compilation = CSharpCompilation.Create(
				assemblyName,
				syntaxTrees: syntaxTrees,
				references: GetMetadataReferences(),
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug));

			if(Data.useSourceGenerators()) {
				GetRoslynGenerators(out var sourceGenerators, out _);
				var generatorDriver = CSharpGeneratorDriver.Create(sourceGenerators).RunGeneratorsAndUpdateCompilation(compilation, out var compilationUpdated, out _);
				compilation = compilationUpdated as CSharpCompilation;
			}
			using(var assemblyStream = new MemoryStream())
			using(var symbolsStream = new MemoryStream()) {
				bool useDebug = false;
				EmitResult emitResult;
				if(embeddedTexts != null) {
					useDebug = true;
					var emitOptions = new EmitOptions(
						debugInformationFormat: DebugInformationFormat.PortablePdb,
						pdbFilePath: Path.ChangeExtension(assemblyName, "pdb"));
					emitResult = compilation.Emit(
						assemblyStream,
						symbolsStream,
						embeddedTexts: embeddedTexts,
						options: emitOptions);
				} else {
					emitResult = compilation.Emit(assemblyStream);
				}
				if(emitResult.Success) {
					assemblyStream.Seek(0, SeekOrigin.Begin);
					symbolsStream?.Seek(0, SeekOrigin.Begin);
					if(useDebug) {
						result.rawAssembly = assemblyStream.ToArray();
						result.rawPdb = symbolsStream.ToArray();
					} else {
						result.rawAssembly = assemblyStream.ToArray();
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
					result.errors = compileErrors;
				}
			}
			return result;
		}

		private static CompileResult DoCompileAndSave(
			string assemblyName,
			IEnumerable<Syntax> syntaxTrees,
			string assemblyPath,
			List<EmbeddedText> embeddedTexts = null,
			bool loadAssembly = true) {
			CompileResult result = new CompileResult();
			var compilation = CSharpCompilation.Create(
				assemblyName,
				syntaxTrees: syntaxTrees,
				references: GetMetadataReferences(),
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug));

			if(Data.useSourceGenerators()) {
				GetRoslynGenerators(out var sourceGenerators, out _);
				var generatorDriver = CSharpGeneratorDriver.Create(sourceGenerators).RunGeneratorsAndUpdateCompilation(compilation, out var compilationUpdated, out _);
				compilation = compilationUpdated as CSharpCompilation;
			}
			using(var assemblyStream = new MemoryStream())
			using(var symbolsStream = new MemoryStream()) {
				bool useDebug = false;
				EmitResult emitResult;
				if(embeddedTexts != null) {
					useDebug = true;
					var emitOptions = new EmitOptions(
						debugInformationFormat: DebugInformationFormat.PortablePdb,
						pdbFilePath: Path.ChangeExtension(assemblyName, "pdb"));
					emitResult = compilation.Emit(
						assemblyStream,
						symbolsStream,
						embeddedTexts: embeddedTexts,
						options: emitOptions);
				} else {
					emitResult = compilation.Emit(assemblyStream);
				}
				if(emitResult.Success) {
					assemblyStream.Seek(0, SeekOrigin.Begin);
					symbolsStream?.Seek(0, SeekOrigin.Begin);
					if(useDebug) {
						result.rawAssembly = assemblyStream.ToArray();
						result.rawPdb = symbolsStream.ToArray();
						if(loadAssembly)
							result.LoadAssembly();
						var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
						File.Open(assemblyPath, FileMode.OpenOrCreate).Close();
						File.Open(pdbPath, FileMode.OpenOrCreate).Close();
						File.WriteAllBytes(assemblyPath, assemblyStream.ToArray());
						File.WriteAllBytes(pdbPath, symbolsStream.ToArray());
					} else {
						result.rawAssembly = assemblyStream.ToArray();
						if(loadAssembly)
							result.LoadAssembly();
						File.Open(assemblyPath, FileMode.OpenOrCreate).Close();
						File.WriteAllBytes(assemblyPath, assemblyStream.ToArray());
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
					result.errors = compileErrors;
				}
			}
			return result;
		}
#endregion

		public static CompilationUnitSyntax GetSyntaxTree(string script) {
			if(script == null) {
				throw new NullReferenceException("Can't parse, Scripts is null");
			}
			var tree = CSharpSyntaxTree.ParseText(script, new CSharpParseOptions(preprocessorSymbols: GetPreprocessorSymbols()));
			return (CompilationUnitSyntax)tree.GetRoot();
		}

		public static CompilationUnitSyntax GetSyntaxTree(string script, out SemanticModel model, IEnumerable<Syntax> references = null) {
			if(script == null) {
				throw new NullReferenceException("Can't parse, Scripts is null");
			}
			var tree = CSharpSyntaxTree.ParseText(script, new CSharpParseOptions(preprocessorSymbols: GetPreprocessorSymbols()));
			List<Syntax> trees = new List<Syntax>() { tree };
			if(references != null) {
				trees.AddRange(references);
			}
			var compilation = CSharpCompilation.Create("CSharpParser", syntaxTrees: trees, references: GetMetadataReferences());
			model = compilation.GetSemanticModel(tree, true);
			return (CompilationUnitSyntax)tree.GetRoot();
		}
	}
}