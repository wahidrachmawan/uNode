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

	/// <summary>
	/// Utility class for compiling scripts using Roslyn.
	/// </summary>
	public static class RoslynUtility {
		public static IList<Assembly> assemblies;

		#region Data
		public static class Data {
			public static Func<bool> useSourceGenerators;
			public static Func<Assembly[]> GetAssemblies;
			public static Func<CompilationMethod> compilationMethod;
			public static string tempAssemblyPath;
		}

		/// <summary>
		/// Compiles a collection of C# script source codes into an assembly using the Roslyn compiler.
		/// Generates a random assembly name and returns a CompileResult encapsulating
		/// the compiled assembly bytes, debug symbols (if available), and any compilation errors.
		/// </summary>
		/// <param name="scripts">An enumerable collection of C# script source strings to compile.</param>
		/// <returns>A CompileResult object representing the outcome of the compilation process.</returns>
		public static CompileResult CompileScript(IEnumerable<string> scripts) {
			return CompileScript(CachedData.randomAssemblyName, scripts);
		}

		/// <summary>
		/// Compiles the provided scripts into an assembly with the specified name.
		/// </summary>
		/// <param name="assemblyName">The name of the assembly to be created.</param>
		/// <param name="scripts">A collection of script source code strings to be compiled.  Cannot be <see langword="null"/>.</param>
		/// <returns>A <see cref="CompileResult"/> representing the result of the compilation,  including the compiled assembly and any
		/// diagnostic information.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="scripts"/> is <see langword="null"/>.</exception>
		public static CompileResult CompileScript(string assemblyName, IEnumerable<string> scripts) {
			if(scripts == null) {
				throw new ArgumentNullException(nameof(scripts));
			}
			var trees = GetSyntaxTrees(scripts);
			return DoCompile(assemblyName, trees);
		}

		/// <summary>
		/// Compiles the specified source files into an assembly.
		/// </summary>
		/// <param name="files">A collection of file paths representing the source files to compile.  Each file must contain valid source code.</param>
		/// <returns>A <see cref="CompileResult"/> object containing the results of the compilation,  including any errors or warnings
		/// encountered.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="files"/> is <see langword="null"/>.</exception>
		public static CompileResult CompileFiles(IEnumerable<string> files) {
			return CompileFiles(CachedData.randomAssemblyName, files);
		}

		/// <summary>
		/// Compiles the specified source files into an assembly with the given name.
		/// </summary>
		/// <param name="assemblyName">The name of the assembly to be created.</param>
		/// <param name="files">A collection of file paths representing the source files to compile.  Each file must contain valid source code.</param>
		/// <returns>A <see cref="CompileResult"/> object containing the results of the compilation,  including any errors or warnings
		/// encountered.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="files"/> is <see langword="null"/>.</exception>
		public static CompileResult CompileFiles(string assemblyName, IEnumerable<string> files) {
			if(files == null) {
				throw new ArgumentNullException(nameof(files));
			}
			var trees = GetSyntaxTreesFromFiles(files, out var embeddedTexts);
			return DoCompile(assemblyName, trees, embeddedTexts);
		}

		/// <summary>
		/// Compiles the provided C# scripts into an assembly and optionally saves it to the specified path.
		/// </summary>
		/// <remarks>This method compiles the provided scripts into a single assembly. If <paramref name="savePath"/>
		/// is specified, the assembly is saved to the given path. The <paramref name="loadAssembly"/> parameter determines
		/// whether the compiled assembly is loaded into memory after compilation.</remarks>
		/// <param name="assemblyName">The name of the assembly to be created. This value is used as the assembly's identifier.</param>
		/// <param name="scripts">A collection of C# script strings to be compiled. Cannot be <see langword="null"/>.</param>
		/// <param name="savePath">The file path where the compiled assembly will be saved. If <see langword="null"/> or empty, the assembly will not
		/// be saved to disk.</param>
		/// <param name="loadAssembly">A value indicating whether the compiled assembly should be loaded into the current application domain.</param>
		/// <returns>A <see cref="CompileResult"/> object containing the results of the compilation, including any errors or the
		/// compiled assembly.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="scripts"/> is <see langword="null"/>.</exception>
		public static CompileResult CompileScriptAndSave(string assemblyName, IEnumerable<string> scripts, string savePath, bool loadAssembly) {
			if(scripts == null) {
				throw new ArgumentNullException(nameof(scripts));
			}
			var trees = GetSyntaxTrees(scripts);
			return DoCompileAndSave(assemblyName, trees, savePath, loadAssembly: loadAssembly);
		}

		/// <summary>
		/// Compiles the specified source files into an assembly and optionally saves it to the specified path.
		/// </summary>
		/// <remarks>This method compiles the provided source files into an assembly using the specified assembly
		/// name. If <paramref name="savePath"/> is provided, the compiled assembly will be saved to the specified location.
		/// If <paramref name="loadAssembly"/> is <see langword="true"/>, the compiled assembly will also be loaded into the
		/// current application domain.</remarks>
		/// <param name="assemblyName">The name of the assembly to be created.</param>
		/// <param name="files">A collection of file paths containing the source code to compile. Cannot be <see langword="null"/>.</param>
		/// <param name="savePath">The file path where the compiled assembly will be saved. If <see langword="null"/> or empty, the assembly will not
		/// be saved to disk.</param>
		/// <param name="loadAssembly">A value indicating whether the compiled assembly should be loaded into the current application domain.</param>
		/// <returns>A <see cref="CompileResult"/> object containing the results of the compilation, including any diagnostics and the
		/// compiled assembly, if applicable.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="files"/> is <see langword="null"/>.</exception>
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

		public static UnityEditor.Compilation.Assembly AssemblyCSharp {
			get {
				if(CachedData.assemblyCSharp == null && CachedData.hasDefaultAssembly == null) {
					CachedData.hasDefaultAssembly = false;
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
							sourceGenerators.Add((ReflectionUtils.CreateInstance(type) as IIncrementalGenerator).AsSourceGenerator());
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
			}
			else {
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
				CSharpGeneratorDriver.Create(sourceGenerators).RunGeneratorsAndUpdateCompilation(compilation, out var compilationUpdated, out _);
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
				}
				else {
					emitResult = compilation.Emit(assemblyStream);
				}
				if(emitResult.Success) {
					assemblyStream.Seek(0, SeekOrigin.Begin);
					symbolsStream?.Seek(0, SeekOrigin.Begin);
					if(useDebug) {
						result.rawAssembly = assemblyStream.ToArray();
						result.rawPdb = symbolsStream.ToArray();
					}
					else {
						result.rawAssembly = assemblyStream.ToArray();
					}
				}
				else {
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

		public class FileAdditionalText : AdditionalText {
			private readonly string _path;
			private readonly SourceText _text;

			public FileAdditionalText(string path) {
				_path = path;
				_text = SourceText.From(File.ReadAllText(path), System.Text.Encoding.UTF8);
			}

			public FileAdditionalText(string path, string text) {
				_path = path;
				_text = SourceText.From(text, System.Text.Encoding.UTF8);
			}

			public override string Path => _path;
			public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
		}

		private static CompileResult DoCompileAndSave(
			string assemblyName,
			IEnumerable<Syntax> syntaxTrees,
			string assemblyPath,
			List<EmbeddedText> embeddedTexts = null,
			bool loadAssembly = true) {
			CompileResult result = new CompileResult();
			var option = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug);
			var compilation = CSharpCompilation.Create(
				assemblyName,
				syntaxTrees: syntaxTrees,
				references: GetMetadataReferences(),
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug));
			
			if(Data.useSourceGenerators()) {
				GetRoslynGenerators(out var sourceGenerators, out _);
				CSharpGeneratorDriver.Create(sourceGenerators).RunGeneratorsAndUpdateCompilation(compilation, out var compilationUpdated, out _);
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
				}
				else {
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
						if(!string.IsNullOrEmpty(assemblyPath)) {
							var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
							File.Open(assemblyPath, FileMode.OpenOrCreate).Close();
							File.Open(pdbPath, FileMode.OpenOrCreate).Close();
							File.WriteAllBytes(assemblyPath, assemblyStream.ToArray());
							File.WriteAllBytes(pdbPath, symbolsStream.ToArray());
						}
					}
					else {
						result.rawAssembly = assemblyStream.ToArray();
						if(loadAssembly)
							result.LoadAssembly();
						if(!string.IsNullOrEmpty(assemblyPath)) {
							File.Open(assemblyPath, FileMode.OpenOrCreate).Close();
							File.WriteAllBytes(assemblyPath, assemblyStream.ToArray());
						}
					}
				}
				else {
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

		/// <summary>
		/// Parses the provided C# script and returns its syntax tree as a <see cref="CompilationUnitSyntax"/>.
		/// </summary>
		/// <remarks>This method uses the default C# parsing options, including any preprocessor symbols defined by
		/// the <c>GetPreprocessorSymbols</c> method.</remarks>
		/// <param name="script">The C# script to parse. Cannot be <see langword="null"/>.</param>
		/// <returns>The root node of the syntax tree, represented as a <see cref="CompilationUnitSyntax"/>.</returns>
		/// <exception cref="NullReferenceException">Thrown if <paramref name="script"/> is <see langword="null"/>.</exception>
		public static CompilationUnitSyntax GetSyntaxTree(string script) {
			if(script == null) {
				throw new NullReferenceException("Can't parse, Scripts is null");
			}
			var tree = CSharpSyntaxTree.ParseText(script, new CSharpParseOptions(preprocessorSymbols: GetPreprocessorSymbols()));
			return (CompilationUnitSyntax)tree.GetRoot();
		}

		/// <summary>
		/// Parses the provided C# script into a syntax tree and retrieves its semantic model.
		/// </summary>
		/// <remarks>This method creates a new compilation using the provided script and optional references. The
		/// returned <see cref="CompilationUnitSyntax"/> represents the root of the syntax tree for the script, and the
		/// <paramref name="model"/> provides semantic information for the tree.</remarks>
		/// <param name="script">The C# script to parse. This parameter cannot be <see langword="null"/>.</param>
		/// <param name="model">When this method returns, contains the <see cref="SemanticModel"/> associated with the parsed syntax tree.</param>
		/// <param name="references">An optional collection of additional syntax trees to include in the compilation. If <see langword="null"/>, no
		/// additional syntax trees are included.</param>
		/// <returns>The root node of the parsed syntax tree as a <see cref="CompilationUnitSyntax"/>.</returns>
		/// <exception cref="NullReferenceException">Thrown if <paramref name="script"/> is <see langword="null"/>.</exception>
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

		/// <summary>
		/// Retrieves the line number in the source file where the specified member is declared.
		/// </summary>
		/// <remarks>This method parses the source file specified by <paramref name="path"/> to locate the declaration
		/// of the member represented by <paramref name="info"/>. It supports various member types, including types, fields,
		/// events, properties, methods, and constructors. If the member is not found in the file, the method returns
		/// -1.</remarks>
		/// <param name="info">The <see cref="MemberInfo"/> object representing the member whose line number is to be retrieved.</param>
		/// <param name="path">The full path to the source file containing the member's declaration.</param>
		/// <returns>The 1-based line number in the source file where the member is declared, or -1 if the member cannot be found.</returns>
		public static int GetLineForMember(MemberInfo info, string path) {
			var syntaxTree = GetSyntaxTree(File.ReadAllText(path), out var model);
			switch(info.MemberType) {
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType: {
					foreach(var syntax in syntaxTree.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()) {
						if(GetMemberInfoFromSyntax(syntax, model) == info) {
							return syntax.GetLocation().GetLineSpan().Span.Start.Line + 1;
						}
					}
					break;
				}
				case MemberTypes.Field: {
					foreach(var syntax in syntaxTree.DescendantNodesAndSelf().OfType<VariableDeclaratorSyntax>()) {
						if(GetMemberInfoFromSyntax(syntax, model) == info) {
							return syntax.GetLocation().GetLineSpan().Span.Start.Line + 1;
						}
					}
					break;
				}
				case MemberTypes.Event: {
					foreach(var syntax in syntaxTree.DescendantNodesAndSelf().OfType<EventDeclarationSyntax>()) {
						if(GetMemberInfoFromSyntax(syntax, model) == info) {
							return syntax.GetLocation().GetLineSpan().Span.Start.Line + 1;
						}
					}
					break;
				}
				case MemberTypes.Property: {
					foreach(var syntax in syntaxTree.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>()) {
						if(GetMemberInfoFromSyntax(syntax, model) == info) {
							return syntax.GetLocation().GetLineSpan().Span.Start.Line + 1;
						}
					}
					break;
				}
				case MemberTypes.Method: {
					foreach(var syntax in syntaxTree.DescendantNodesAndSelf().OfType<MethodDeclarationSyntax>()) {
						if(GetMemberInfoFromSyntax(syntax, model) == info) {
							return syntax.GetLocation().GetLineSpan().Span.Start.Line + 1;
						}
					}
					break;
				}
				case MemberTypes.Constructor: {
					foreach(var syntax in syntaxTree.DescendantNodesAndSelf().OfType<ConstructorDeclarationSyntax>()) {
						if(GetMemberInfoFromSyntax(syntax, model) == info) {
							return syntax.GetLocation().GetLineSpan().Span.Start.Line + 1;
						}
					}
					break;
				}
			}
			return -1;
		}

		private static Type GetMemberInfoFromSyntax(TypeDeclarationSyntax syntax, SemanticModel model) {
			var symbol = model.GetDeclaredSymbol(syntax);
			if(symbol != null) {
				var nativeType = GetTypeFromTypeSymbol(symbol);
				if(nativeType != null) {
					return nativeType;
				}
			}
			return null;
		}

		private static EventInfo GetMemberInfoFromSyntax(EventDeclarationSyntax syntax, SemanticModel model) {
			var symbol = model.GetDeclaredSymbol(syntax);
			if(symbol != null) {
				var nativeType = GetTypeFromTypeSymbol(symbol.ContainingType);
				if(nativeType != null) {
					var member = nativeType.GetEvent(symbol.Name);
					if(member != null) {
						return member;
					}
				}
			}
			return null;
		}

		private static PropertyInfo GetMemberInfoFromSyntax(PropertyDeclarationSyntax syntax, SemanticModel model) {
			var symbol = model.GetDeclaredSymbol(syntax);
			if(symbol != null) {
				var nativeType = GetTypeFromTypeSymbol(symbol.ContainingType);
				if(nativeType != null) {
					var member = nativeType.GetProperty(symbol.Name);
					if(member != null) {
						return member;
					}
				}
			}
			return null;
		}

		private static FieldInfo GetMemberInfoFromSyntax(VariableDeclaratorSyntax syntax, SemanticModel model) {
			var symbol = model.GetDeclaredSymbol(syntax);
			if(symbol != null) {
				var nativeType = GetTypeFromTypeSymbol(symbol.ContainingType);
				if(nativeType != null) {
					var member = nativeType.GetField(symbol.Name);
					if(member != null) {
						return member;
					}
				}
			}
			return null;
		}

		private static MethodInfo GetMemberInfoFromSyntax(MethodDeclarationSyntax syntax, SemanticModel model) {
			var symbol = model.GetDeclaredSymbol(syntax);
			if(symbol != null) {
				var nativeType = GetTypeFromTypeSymbol(symbol.ContainingType);
				if(nativeType != null) {
					var members = nativeType.GetMember(symbol.Name);
					foreach(var member in members) {
						if(member is MethodInfo method) {
							if(IsValidMember(method, symbol)) {
								return method;
							}
						}
					}
				}
			}
			return null;
		}

		private static ConstructorInfo GetMemberInfoFromSyntax(ConstructorDeclarationSyntax syntax, SemanticModel model) {
			var symbol = model.GetDeclaredSymbol(syntax);
			if(symbol != null) {
				var nativeType = GetTypeFromTypeSymbol(symbol.ContainingType);
				if(nativeType != null) {
					var members = nativeType.GetConstructors();
					foreach(var member in members) {
						if(IsValidMember(member, symbol)) {
							return member;
						}
					}
				}
			}
			return null;
		}

		private static bool IsValidMember(ConstructorInfo ctor, IMethodSymbol symbol) {
			if(ctor.IsGenericMethod && symbol.IsGenericMethod == false)
				return false;
			var mparam = ctor.GetParameters();
			var sparam = symbol.Parameters;
			if(mparam.Length != sparam.Length)
				return false;
			for(int i = 0; i < mparam.Length; i++) {
				if(mparam[i].ParameterType != GetTypeFromTypeSymbol(sparam[i].Type)) {
					return false;
				}
			}
			//var mgparam = ctor.GetGenericArguments();
			//var sgparam = symbol.TypeArguments;
			//if(mgparam.Length != sgparam.Length)
			//	return false;
			return true;
		}

		private static bool IsValidType(Type type, ITypeSymbol typeSymbol) {
			if(type == null && typeSymbol == null) return true;
			if(type == null || typeKeywords == null) return false;
			if(type.IsGenericParameter && type.IsConstructedGenericType == false) {
				if(typeSymbol.TypeKind != TypeKind.TypeParameter) {
					return false;
				}
				if(type.Name != typeSymbol.Name) {
					return false;
				}
			}
			else if(type != GetTypeFromTypeSymbol(typeSymbol)) {
				return false;
			}
			return true;
		}

		private static bool IsValidMember(MethodInfo method, IMethodSymbol symbol) {
			if(method.Name != symbol.Name)
				return false;
			if(method.IsGenericMethod && symbol.IsGenericMethod == false)
				return false;
			if(IsValidType(method.ReturnType, symbol.ReturnType) == false)
				return false;
			var mparam = method.GetParameters();
			var sparam = symbol.Parameters;
			if(mparam.Length != sparam.Length)
				return false;
			for(int i = 0; i < mparam.Length; i++) {
				if(IsValidType(mparam[i].ParameterType, sparam[i].Type) == false) {
					return false;
				}
			}
			var mgparam = method.GetGenericArguments();
			var sgparam = symbol.TypeArguments;
			if(mgparam.Length != sgparam.Length)
				return false;
			return true;
		}


		internal static Dictionary<string, Type> typeKeywords = new Dictionary<string, Type> {
			{ "bool", typeof(Boolean) },
			{ "byte", typeof(Byte) },
			{ "char", typeof(Char) },
			{ "decimal", typeof(Decimal) },
			{ "double", typeof(Double) },
			{ "float", typeof(Single) },
			{ "int", typeof(Int32) },
			{ "long", typeof(Int64) },
			{ "sbyte", typeof(SByte) },
			{ "short", typeof(Int16) },
			{ "string", typeof(string) },
			{ "uint", typeof(UInt32) },
			{ "ulong", typeof(UInt64) },
			{ "ushort", typeof(UInt16) },
			{ "void", typeof(void) },
			{ "object", typeof(object) },
		};

		public static readonly SymbolDisplayFormat genericDisplayFormat = new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted, SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces, SymbolDisplayGenericsOptions.None);

		public static Type GetTypeFromTypeName(string name) {
			Type type = null;
			List<string> paramList = new List<string>();
			string str = string.Empty;
			string param = string.Empty;
			int deep = 0;
			for(int i = 0; i < name.Length; i++) {
				var c = name[i];
				if(c == ' ')
					continue;
				if(c == '<') {
					deep++;
				}
				else if(c == '>') {
					deep--;
					if(deep == 0) {
						paramList.Add(param);
						param = string.Empty;

						type = (str + "`" + paramList.Count).ToType(false);
						if(type == null)
							return null;
						str = string.Empty;
						if(type.IsGenericType) {
							var pTypes = paramList.Select(p => GetTypeFromTypeName(p)).ToArray();
							if(pTypes.Any(p => p == null))
								return null;
							type = type.MakeGenericType(pTypes);
						}
						paramList.Clear();
						i++;
						continue;
					}
				}
				else if(c == ',' && deep == 1) {
					paramList.Add(param);
					param = string.Empty;
				}
				else if(deep > 0) {
					param += c;
				}
				else {
					str += c;
				}
			}
			if(type != null) {
				return (type.FullName + "." + str).ToType(false);
			}
			else {
				if(typeKeywords.TryGetValue(str, out var t)) {
					return t;
				}
				return str.ToType(false);
			}
		}

		private static Type GetTypeFromTypeSymbol(ITypeSymbol typeSymbol, bool isByRef = false) {
			if(typeSymbol == null) {
				throw new ArgumentNullException("typeSymbol");
			}

			if(typeSymbol.Kind == SymbolKind.ArrayType) {
				var arraySymbol = typeSymbol as IArrayTypeSymbol;
				var elementType = GetTypeFromTypeSymbol(arraySymbol.ElementType);
				Type t = arraySymbol.Rank == 1 ? elementType.MakeArrayType() : elementType.MakeArrayType(arraySymbol.Rank);
				if(isByRef) {
					t = t.MakeByRefType();
				}
				return t;
			}
			if(typeSymbol.SpecialType != SpecialType.None) {
				switch(typeSymbol.SpecialType) {
					case SpecialType.System_Boolean:
						return !isByRef ? typeof(bool) : typeof(bool).MakeByRefType();
					case SpecialType.System_Byte:
						return !isByRef ? typeof(byte) : typeof(byte).MakeByRefType();
					case SpecialType.System_Char:
						return !isByRef ? typeof(char) : typeof(char).MakeByRefType();
					case SpecialType.System_Decimal:
						return !isByRef ? typeof(decimal) : typeof(decimal).MakeByRefType();
					case SpecialType.System_Double:
						return !isByRef ? typeof(double) : typeof(double).MakeByRefType();
					case SpecialType.System_Int16:
						return !isByRef ? typeof(short) : typeof(short).MakeByRefType();
					case SpecialType.System_Int32:
						return !isByRef ? typeof(int) : typeof(int).MakeByRefType();
					case SpecialType.System_Int64:
						return !isByRef ? typeof(long) : typeof(long).MakeByRefType();
					case SpecialType.System_Object:
						return !isByRef ? typeof(object) : typeof(object).MakeByRefType();
					case SpecialType.System_SByte:
						return !isByRef ? typeof(sbyte) : typeof(sbyte).MakeByRefType();
					case SpecialType.System_Single:
						return !isByRef ? typeof(float) : typeof(float).MakeByRefType();
					case SpecialType.System_String:
						return !isByRef ? typeof(string) : typeof(string).MakeByRefType();
					case SpecialType.System_UInt16:
						return !isByRef ? typeof(ushort) : typeof(ushort).MakeByRefType();
					case SpecialType.System_UInt32:
						return !isByRef ? typeof(uint) : typeof(uint).MakeByRefType();
					case SpecialType.System_UInt64:
						return !isByRef ? typeof(ulong) : typeof(ulong).MakeByRefType();
					case SpecialType.System_Void:
						return !isByRef ? typeof(void) : typeof(void).MakeByRefType();
					case SpecialType.System_ValueType:
						return !isByRef ? typeof(ValueType) : typeof(ValueType).MakeByRefType();
					case SpecialType.System_Collections_Generic_ICollection_T:
						return !isByRef ? typeof(ICollection<>) : typeof(ICollection<>).MakeByRefType();
					case SpecialType.System_Collections_Generic_IEnumerable_T:
						return !isByRef ? typeof(IEnumerable<>) : typeof(IEnumerable<>).MakeByRefType();
					case SpecialType.System_Collections_Generic_IEnumerator_T:
						return !isByRef ? typeof(IEnumerator<>) : typeof(IEnumerator<>).MakeByRefType();
					case SpecialType.System_Collections_Generic_IList_T:
						return !isByRef ? typeof(IList<>) : typeof(IList<>).MakeByRefType();
					case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
						return !isByRef ? typeof(IReadOnlyCollection<>) : typeof(IReadOnlyCollection<>).MakeByRefType();
					case SpecialType.System_Collections_Generic_IReadOnlyList_T:
						return !isByRef ? typeof(IReadOnlyList<>) : typeof(IReadOnlyList<>).MakeByRefType();
					case SpecialType.System_Collections_IEnumerable:
						return !isByRef ? typeof(IEnumerable) : typeof(IEnumerable).MakeByRefType();
					case SpecialType.System_Collections_IEnumerator:
						return !isByRef ? typeof(IEnumerator) : typeof(IEnumerator).MakeByRefType();
					case SpecialType.System_DateTime:
						return !isByRef ? typeof(DateTime) : typeof(DateTime).MakeByRefType();
					case SpecialType.System_Delegate:
						return !isByRef ? typeof(Delegate) : typeof(Delegate).MakeByRefType();
					case SpecialType.System_Enum:
						return !isByRef ? typeof(Enum) : typeof(Enum).MakeByRefType();
					case SpecialType.System_IAsyncResult:
						return !isByRef ? typeof(IAsyncResult) : typeof(IAsyncResult).MakeByRefType();
					case SpecialType.System_IDisposable:
						return !isByRef ? typeof(IDisposable) : typeof(IDisposable).MakeByRefType();
					case SpecialType.System_IntPtr:
						return !isByRef ? typeof(IntPtr) : typeof(IntPtr).MakeByRefType();
					case SpecialType.System_MulticastDelegate:
						return !isByRef ? typeof(MulticastDelegate) : typeof(MulticastDelegate).MakeByRefType();
					case SpecialType.System_Nullable_T:
						return !isByRef ? typeof(Nullable<>) : typeof(Nullable<>).MakeByRefType();
					case SpecialType.System_RuntimeArgumentHandle:
						return !isByRef ? typeof(RuntimeArgumentHandle) : typeof(RuntimeArgumentHandle).MakeByRefType();
					case SpecialType.System_RuntimeFieldHandle:
						return !isByRef ? typeof(RuntimeFieldHandle) : typeof(RuntimeFieldHandle).MakeByRefType();
					case SpecialType.System_RuntimeMethodHandle:
						return !isByRef ? typeof(RuntimeMethodHandle) : typeof(RuntimeMethodHandle).MakeByRefType();
					case SpecialType.System_RuntimeTypeHandle:
						return !isByRef ? typeof(RuntimeTypeHandle) : typeof(RuntimeTypeHandle).MakeByRefType();
				}
			}
			if(typeSymbol is INamedTypeSymbol) {
				INamedTypeSymbol namedTypeSymbol = typeSymbol as INamedTypeSymbol;
				if(namedTypeSymbol.IsGenericType) {
					Type genericType = namedTypeSymbol.ConstructUnboundGenericType().ToDisplayString(genericDisplayFormat).Add("`" + namedTypeSymbol.TypeArguments.Length).ToType(false);
					if(genericType == null) {//Retry to parse type using different method.
						if(namedTypeSymbol != namedTypeSymbol.OriginalDefinition) {
							var baseType = GetTypeFromTypeSymbol(namedTypeSymbol.OriginalDefinition);
							if(baseType != null) {
								genericType = baseType.MakeGenericType(namedTypeSymbol.TypeArguments.Select(item => GetTypeFromTypeSymbol(item)).ToArray());
								if(isByRef) {
									genericType = genericType.MakeByRefType();
								}
								return genericType;
							}
							else {
								baseType = GetTypeFromTypeName(namedTypeSymbol.ToString());
								if(baseType != null) {
									if(isByRef) {
										baseType = baseType.MakeByRefType();
									}
									return baseType;
								}
								throw new System.Exception("Failed to deserialize type: " + namedTypeSymbol.ToString());
							}
						}
						else {
							return SerializedType.None;
						}
					}
					List<SerializedType> types = new List<SerializedType>();
					foreach(var arg in namedTypeSymbol.TypeArguments) {
						types.Add(GetTypeFromTypeSymbol(arg));
					}
					if(types.Any(item => item.typeKind == SerializedTypeKind.GenericParameter)) {
						//TODO: add support for generic parameter
						throw new NotImplementedException();
						//var member = new MemberData() {
						//	instance = types[0].instance,
						//	targetType = MemberData.TargetType.uNodeGenericParameter,
						//};
						//var typeDatas = MemberDataUtility.MakeTypeDatas(types);
						//member.Items = new MemberData.ItemData[]{
						//	new MemberData.ItemData() {
						//		genericArguments = new TypeData[] { new TypeData() {
						//				name = genericType.FullName,
						//				parameters = typeDatas.ToArray()
						//			}
						//		}
						//	}
						//};
						//return member;
					}
					var gTypes = types.Select(item => item.type);
					if(gTypes.All(item => item != null)) {
						Type t = genericType.MakeGenericType(gTypes.ToArray());
						if(isByRef) {
							t = t.MakeByRefType();
						}
						return t;
					}
					return !isByRef ? genericType : genericType.MakeByRefType();
				}
			}
			//Check if type is nested type
			if(typeSymbol.ContainingType != null) {
				//if(typeSymbol is ITypeParameterSymbol) {
				//	Debug.Log("A");
				//}
				return (GetTypeFromTypeSymbol(typeSymbol.ContainingType).FullName + "+" + typeSymbol.Name + (isByRef ? "&" : "")).ToType();
			}
			Type type = TypeSerializer.Deserialize(typeSymbol.ToString() + (isByRef ? "&" : ""), false);
			//If type not found, try to find it manually.
			if(type == null) {
				//if(usingNamespaces != null) {
				//	string nm = typeSymbol.ToString() + (isByRef ? "&" : "");
				//	foreach(var n in usingNamespaces) {
				//		type = TypeSerializer.Deserialize(n + "." + nm, false);
				//		if(type != null)
				//			break;
				//	}
				//	if(type == null) {
				//		foreach(var n in usingNamespaces) {
				//			type = TypeSerializer.Deserialize(n + "." + nm + "Attribute", false);
				//			if(type != null)
				//				break;
				//		}
				//	}
				//}
				if(type == null) {
					throw new Exception("The type name : " + typeSymbol.ToString() + (isByRef ? "&" : "") + " not found");
				}
			}
			return type;
		}
	}
}