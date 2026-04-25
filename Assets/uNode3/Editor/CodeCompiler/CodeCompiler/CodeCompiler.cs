using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Text;

namespace MaxyGames.CodeCompiler {
	static class Program {
		static readonly string mainPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		static readonly string runnerConfigPath = Path.Combine(mainPath, "Runner.config");
		static readonly string runnerPidPath = Path.Combine(mainPath, "Runner.pid");

		public static void Main(string[] args) {
			if(File.Exists(runnerConfigPath)) {
				File.WriteAllText(runnerPidPath, System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
				static void OnClose() {
					if(File.Exists(runnerPidPath)) {
						File.Delete(runnerPidPath);
					}
				}
				AppDomain.CurrentDomain.ProcessExit += (sender, e) => {
					OnClose();
				};
				Console.CancelKeyPress += (sender, e) => {
					OnClose();
				};
				foreach(var line in File.ReadAllLines(runnerConfigPath)) {
					AssemblyReferences.AddReference(line);
				}
				AppDomain.CurrentDomain.AssemblyResolve += AssemblyReferences.ResolveAssembly;
			}
			else {
				Console.WriteLine("Runner.config not found");
				return;
			}

			if(args.Length > 0) {
				Console.WriteLine("Arguments:");
				foreach(var arg in args) {
					Console.WriteLine(arg);
				}
				if(File.Exists(args[0])) {
					var option = CodeCompiler.GetOption(File.ReadAllText(args[0]));
					{
						var result = CodeCompiler.Run(option);
						Console.WriteLine($"Compilation {(result.Success ? "succeeded" : "failed")}");
						if(!result.Success) {
							Console.WriteLine("Errors:");
							foreach(var error in result.Errors) {
								Console.WriteLine(error);
							}
						}
					}
				}
				else {
					Console.WriteLine($"File not found: {args[0]}");
				}
			}
			else {
				while(true) {
					var pipeServer = new NamedPipeServerStream(CodeCompiler.PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
					Console.WriteLine("Waiting for connection...");
					pipeServer.WaitForConnection();
					Task.Run(async () => {
						try {
							await HandleClientAsync(pipeServer);
						}
						finally {
							pipeServer.Dispose(); 
						}
					});
					//_clientTask.Add(clientTask);
				}
			}
		}

		//private static List<Task> _clientTask = new List<Task>();

		private static async Task HandleClientAsync(NamedPipeServerStream pipeServer) {
			var data = await PipeHelper.ReceiveStringAsync(pipeServer);
			var option = CodeCompiler.GetOption(data);
			Console.WriteLine("Compiling: " + option.AssemblyName);

			var result = CodeCompiler.Run(option);

			await PipeHelper.SendStringAsync(pipeServer, CodeCompiler.Serialize(result));

			Console.WriteLine($"Compilation {(result.Success ? "succeeded" : "failed")}");
			if(!result.Success) {
				Console.WriteLine("Errors:");
				foreach(var error in result.Errors) {
					Console.WriteLine(error);
				}
			}
		}
	}

	static class AssemblyReferences {
		class ReferenceData {
			public AssemblyName name;
			public string path;
			public string fullname;

			private DateTime _lastWriteTime;
			private Assembly _assembly;
			public Assembly assembly {
				get {
					var lastWriteTime = File.GetLastWriteTime(path);
					if(_assembly == null || lastWriteTime != _lastWriteTime) {
						try {
							if(lastWriteTime != _lastWriteTime) {
								Console.WriteLine("() => Resolving assembly: " + fullname);
							}
							else {
								Console.WriteLine("() => Re-resolving assembly: " + fullname);
							}
							_lastWriteTime = lastWriteTime;
							_assembly = Assembly.Load(File.ReadAllBytes(path));
						}
						catch(Exception ex) {
							Console.WriteLine($"Failed to load assembly: {path}");
							Console.WriteLine(ex);
							return null;
						}
					}
					return _assembly;
				}
			}
			public Version version => name.Version;
		}
		static List<ReferenceData> allReferences = new List<ReferenceData>();

		public static IEnumerable<Assembly> GetCodeGenAssembly() {
			for(int i = 0; i < allReferences.Count; i++) {
				var refs = allReferences[i];
				if(refs.name.Name.StartsWith("Unity.") && refs.name.Name.EndsWith("CodeGen")) {
					yield return refs.assembly;
				}
			}
		}

		//static string[] excludedAssemblies = new[] { "Unity.UNodeECS.CodeGen.dll", "Unity.CodeCompiler.CodeGen.dll" };
		public static void AddReference(string path) {
			try {
				if(File.Exists(path) == false) {
					//Console.WriteLine($"Reference file not found: {path}");
					return;
				}
				//for(int i = 0; i < excludedAssemblies.Length; i++) {
				//	if(path.EndsWith(excludedAssemblies[i])) {
				//		//This to make sure we skip the codegen assembly
				//		return;
				//	}
				//}
				var index = allReferences.FindIndex(r => r.path == path);
				if(index >= 0) {
					//Console.WriteLine($"Reference already added: {path}");
					return;
				}
				var assemblyName = AssemblyName.GetAssemblyName(path);
				allReferences.Add(new ReferenceData() {
					name = assemblyName,
					path = path,
					fullname = assemblyName.FullName,
				});
			}
			catch(Exception ex) {
				Console.WriteLine($"Failed to add reference: {path}");
				Console.WriteLine(ex);
			}
		}

		public static Assembly ResolveAssembly(object sender, ResolveEventArgs resolveArgs) {
			var assembly = new AssemblyName(resolveArgs.Name);
			var version = assembly.Version;
			var assemblyName = assembly.Name;

			//Console.WriteLine($"Resolving assembly: {resolveArgs.Name}, Requested by: {resolveArgs.RequestingAssembly}");

			ReferenceData referenceData = null;
			var closestVersion = version;

			foreach(var data in allReferences) {
				if(data.path.EndsWith(assemblyName + ".dll")) {
					try {
						if(referenceData == null) {
							referenceData = data;
							closestVersion = data.version;
						}
						if(data.fullname == resolveArgs.Name) {
							referenceData = data;
							break;
						}
						else {
							if(data.version != null && version != null) {
								if(data.version > version && data.version < closestVersion) {
									referenceData = data;
									closestVersion = data.version;
									//Console.WriteLine($"Closest version: {data.fullname} ({data.version})");
								}
							}
						}
					}
					catch(Exception ex) {
						Console.WriteLine($"Failed to load assembly: {data.path}, Requested by: {resolveArgs.RequestingAssembly}");
						Console.WriteLine(ex);
					}
				}
			}
			if(referenceData != null) {
				//if(assemblyName.StartsWith("Microsoft.CodeAnalysis")) {
				//	Console.WriteLine();
				//	Console.WriteLine($"Loading assembly: {path}");
				//	Console.WriteLine($"Requested assembly: {resolveArgs.Name}");
				//	Console.WriteLine($"Requesting assembly2: {resolveArgs.RequestingAssembly}");
				//	Console.WriteLine($"Found assembly: {fullname}");
				//	Console.WriteLine($"Assembly name: {assemblyName}");
				//	Console.WriteLine();
				//}
				return referenceData.assembly;
			}
			return null;
		}
	}

	public static class CodeCompiler {
		private const bool useSourceGenerator = true;
		public const string PipeName = "UCodeCompilerPipe";

		public static CodeCompilerResult Run(CodeCompilerOption option) {
			CodeCompilerResult result = null;
			try {
				result = Compile(option);
			}
			finally {
				if(string.IsNullOrEmpty(option.OutputResultPath) == false) {
					Directory.CreateDirectory(Path.GetDirectoryName(option.OutputResultPath));
					File.WriteAllText(option.OutputResultPath, Serialize(result ?? new CodeCompilerResult() { Success = false }));
				}
			}
			return result;
		}

		private static CodeCompilerResult Compile(CodeCompilerOption option) {
			if(Path.IsPathRooted(option.OutputPath) == false) {
				throw new Exception($"Output path is not fully qualified: {option.OutputPath}");
			}
			var compilation = CSharpCompilation.Create(
				option.AssemblyName,
				GetSyntaxTreesFromFiles(option.SourceFiles, out var embeddedTexts, option.Defines),
				option.References.Select(path => MetadataReference.CreateFromFile(path)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: option.ScriptCompilerOptions.OptimizationLevel)
			);
			if(useSourceGenerator) {
				List<IIncrementalGenerator> iGenerators = new List<IIncrementalGenerator>();
				List<Assembly> assemblies = new List<Assembly>();
				foreach(var path in option.ScriptCompilerOptions.RoslynAnalyzerDllPaths) {
					AssemblyReferences.AddReference(path);
				}
				foreach(var path in option.ScriptCompilerOptions.RoslynAnalyzerDllPaths) {
					if(path.EndsWith("CodeFixes.dll", StringComparison.Ordinal))
						continue;
					assemblies.Add(Assembly.Load(File.ReadAllBytes(path)));
				}
				static void AppendSourceGenerators(Assembly assembly, ref List<IIncrementalGenerator> incrementalGenerators) {
					try {
						foreach(var type in assembly.GetTypes()) {
							if(type.IsAbstract || type.IsInterface)
								continue;
							if(typeof(ISourceGenerator).IsAssignableFrom(type)) {
								incrementalGenerators.Add((Activator.CreateInstance(type) as ISourceGenerator).AsIncrementalGenerator());
							}
							else if(typeof(IIncrementalGenerator).IsAssignableFrom(type)) {
								incrementalGenerators.Add(Activator.CreateInstance(type) as IIncrementalGenerator);
							}
						}
					}
					catch(ReflectionTypeLoadException ex) {
						Console.WriteLine("--------------------------");
						Console.WriteLine("() => Error loading assembly: " + assembly.ToString());
						foreach(var loaderException in ex.LoaderExceptions) {
							Console.WriteLine("###############");
							if(loaderException is FileNotFoundException fileNotFoundException) {
								Console.WriteLine($"File not found: {fileNotFoundException.FileName}");
							}
							Console.WriteLine(loaderException);
						}
						Console.WriteLine("--------------------------");
					}
					catch(Exception ex) {
						Console.WriteLine("Error: " + assembly.ToString());
						Console.WriteLine(ex);
					}
				}
				foreach(var ass in assemblies) {
					AppendSourceGenerators(ass, ref iGenerators);
				}
				CSharpGeneratorDriver.Create(iGenerators.ToArray()).RunGeneratorsAndUpdateCompilation(compilation, out var compilationUpdated, out _);
				compilation = compilationUpdated as CSharpCompilation;
			}
			using(var assemblyStream = new MemoryStream())
			using(var symbolsStream = new MemoryStream()) {
				EmitResult emitResult;
				if(embeddedTexts != null) {
					var emitOptions = new EmitOptions(
						debugInformationFormat: DebugInformationFormat.PortablePdb,
						pdbFilePath: Path.ChangeExtension(option.OutputPath, "pdb"));
					emitResult = compilation.Emit(
						assemblyStream,
						symbolsStream,
						embeddedTexts: embeddedTexts,
						options: emitOptions);
				}
				else {
					emitResult = compilation.Emit(assemblyStream, symbolsStream);
				}
				CodeCompilerResult result = new CodeCompilerResult() {
					OutputPath = option.OutputPath,
					Success = emitResult.Success,
					Errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToList()
				};
				if(emitResult.Success) {
					string directory = Path.GetDirectoryName(option.OutputPath);

					if(!Directory.Exists(directory)) {
						Directory.CreateDirectory(directory);
					}
					assemblyStream.Seek(0, SeekOrigin.Begin);
					symbolsStream?.Seek(0, SeekOrigin.Begin);
					var rawAssembly = assemblyStream.ToArray();
					var rawPdb = symbolsStream.ToArray();

					if(option.ScriptCompilerOptions.RunILPP) {
						try {
							CodeCompilerILPPRunner.RunILPP(option.OutputPath, ref rawAssembly, ref rawPdb, option.References, option.Defines, AssemblyReferences.GetCodeGenAssembly());
							result.ILPPApplied = true;
						}
						catch(Exception ex) {
							Console.WriteLine($"Error on running ILPP: {option.OutputPath}");
							Console.WriteLine(ex);
						}
					}

					File.WriteAllBytes(option.OutputPath, rawAssembly);
					File.WriteAllBytes(Path.ChangeExtension(option.OutputPath, "pdb"), rawPdb);

					Console.WriteLine($"Assembly written to: {option.OutputPath}");
				}
				return result;
			}
		}

		public static List<SyntaxTree> GetSyntaxTreesFromFiles(IEnumerable<string> paths, out List<EmbeddedText> embeddedTexts, IEnumerable<string> preprocessorSymbols = null) {
			var result = new List<SyntaxTree>();
			embeddedTexts = new List<EmbeddedText>();
			foreach(var path in paths) {
				var script = File.ReadAllText(path);
				var buffer = Encoding.UTF8.GetBytes(script);
				var sourceText = SourceText.From(buffer, buffer.Length, Encoding.UTF8, canBeEmbedded: true);
				var tree = CSharpSyntaxTree.ParseText(
						sourceText,
						options: new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols),
						path: path);
				result.Add(tree);
				embeddedTexts.Add(EmbeddedText.FromSource(path, sourceText));
			}
			return result;
		}

		public static CodeCompilerOption GetOption(string xml) {
			return Deserialize<CodeCompilerOption>(xml);
		}

		public static T Deserialize<T>(string xml) {
			var reader = new StringReader(xml);
			var serializer = new XmlSerializer(typeof(T));
			var result = (T)serializer.Deserialize(reader);
			return result;
		}

		public static string Serialize<T>(T obj) {
			var writer = new StringWriter();
			var serializer = new XmlSerializer(typeof(T));
			serializer.Serialize(writer, obj);
			return writer.ToString();
		}
	}

	public enum RunnerMode {
		Compile,
		Run,
	}

	[Serializable]
	public class RunnerConfig {
		public RunnerMode Mode { get; set; }
		public CodeCompilerOption CompilerOption { get; set; }
		public RunOption RunOption { get; set; }
	}

	[Serializable]
	public class RunOption {
		public string AssemblyPath { get; set; }
		public string TypeName { get; set; }
		public string MethodName { get; set; }
		public object[] Parameters { get; set; } = new object[0];
	}

	[Serializable]
	public class RunResult {
		public object ReturnValue { get; set; }
		public bool Success { get; set; }
		public string ErrorMessage { get; set; }
	}

	[Serializable]
	public class CodeCompilerOption {
		public string OutputPath { get; set; }
		public string OutputResultPath { get; set; }
		public string AssemblyName { get; set; }
		public string[] References { get; set; } = new string[0];
		public string[] SourceFiles { get; set; } = new string[0];
		public string[] Defines { get; set; } = new string[0];
		public ScriptCompilerOptions ScriptCompilerOptions { get; set; } = new ScriptCompilerOptions();
	}

	[Serializable]
	public class CodeCompilerResult {
		public string OutputPath { get; set; }
		public bool Success { get; set; }
		public bool ILPPApplied { get; set; }
		public List<string> Errors { get; set; } = new List<string>();
	}

	[Serializable]
	public class ScriptCompilerOptions {
		/// <summary>
		/// Stores the path to the Roslyn ruleset file.
		/// </summary>
		public string RoslynAnalyzerRulesetPath { get; set; }

		/// <summary>
		/// Stores the paths to the .dll files.
		/// </summary>
		public string[] RoslynAnalyzerDllPaths { get; set; }

		/// <summary>
		/// Stores the paths to the Roslyn Analyzer additional files.
		/// </summary>
		public string[] RoslynAdditionalFilePaths { get; set; }

		/// <summary>
		/// Stores the path to the Roslyn global config file.
		/// </summary>
		public string AnalyzerConfigPath { get; set; }

		/// <summary>
		/// Allow 'unsafe' code when compiling scripts.
		/// </summary>
		public bool AllowUnsafeCode { get; set; }
		public bool RunILPP { get; set; }

		public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.Debug;
	}
}

namespace MaxyGames {
	public static class PipeHelper {
		public static async Task SendStringAsync(Stream pipe, string message) {
			byte[] data = Encoding.UTF8.GetBytes(message);
			byte[] lenBytes = BitConverter.GetBytes(data.Length);
			await pipe.WriteAsync(lenBytes, 0, 4);
			await pipe.WriteAsync(data, 0, data.Length);
			await pipe.FlushAsync();
		}

		public static async Task<string> ReceiveStringAsync(Stream pipe) {
			byte[] lenBuf = new byte[4];
			int read = 0;
			while(read < 4)
				read += await pipe.ReadAsync(lenBuf, read, 4 - read);
			int length = BitConverter.ToInt32(lenBuf, 0);

			byte[] buffer = new byte[length];
			read = 0;
			while(read < length)
				read += await pipe.ReadAsync(buffer, read, length - read);

			return Encoding.UTF8.GetString(buffer);
		}
	}
}