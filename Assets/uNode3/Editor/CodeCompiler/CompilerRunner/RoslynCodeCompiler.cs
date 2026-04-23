using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.Compilation;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace MaxyGames.CompilerBuilder {
	[InitializeOnLoad]
	public static class RoslynCodeCompiler {
		public const string RuntimeConfig = @"{
	""runtimeOptions"": {
		""tfm"": ""net6.0"",
		""rollForward"": ""LatestMinor"",
		""framework"": {
			""name"": ""Microsoft.NETCore.App"",
			""version"": ""6.0.0""
		}
	}
}";
		public const string RunnerExecutablePath = "Library/uNodeRoslynCompiler/Runner.exe";
		public const string RunnerDirectoryPath = "Library/uNodeRoslynCompiler";

		static RoslynCodeCompiler() {
			//EditorApplication.delayCall += () => {
			//	if(!File.Exists(RunnerExecutablePath)) {
			//		Build();
			//	}
			//};
			EditorApplication.quitting -= Close;
			EditorApplication.quitting += Close;
		}

#if UNODE_DEV
		[MenuItem("Tools/uNode - Roslyn/Build Runner", false, 0)]
		static void Build() {
			Directory.CreateDirectory(RunnerDirectoryPath);
			Build(RunnerExecutablePath);
		}

		[MenuItem("Tools/uNode - Roslyn/Run Runner", false, 0)]
		static void Run() {
			Run(CreateCompilerOption());
		}
#endif

		public static void Run(CodeCompiler.CodeCompilerOption option, Action<CodeCompiler.CodeCompilerResult> onComplete = null) {
			if(!File.Exists(RunnerExecutablePath)) {
				Debug.Log("Runner not found, building...");
				Build(RunnerExecutablePath);
			}
			Run(RunnerExecutablePath, option, onComplete);
		}

		static void Build(string outputPath) {
			var codeCompilerName = typeof(MaxyGames.CodeCompiler.CodeCompiler).Assembly.GetName().Name;
			var codeCompilerAssembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(asm => asm.name == codeCompilerName);
			if(codeCompilerAssembly == null) {
				Debug.LogError("CodeCompiler assembly not found");
				return;
			}

			var references = codeCompilerAssembly.allReferences;
			var sourceTrees = CodeCompiler.CodeCompiler.GetSyntaxTreesFromFiles(codeCompilerAssembly.sourceFiles, out _, codeCompilerAssembly.defines);

			var compilation = CSharpCompilation.Create(
				"RoslynRunner",
				sourceTrees,
				references.Select(path => MetadataReference.CreateFromFile(path)),
				new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: OptimizationLevel.Debug)
			);

			var result = compilation.Emit(outputPath);

			if(!result.Success) {
				foreach(var d in result.Diagnostics)
					Debug.LogError(d.ToString());
			}
			CreateConfigFile(outputPath);
			CreateCompilerOptionFile(outputPath);

			Debug.Log("Runner built successfully on: " + outputPath);
		}

		static void Run(string runnerPath, CodeCompiler.CodeCompilerOption option, Action<CodeCompiler.CodeCompilerResult> onComplete = null) {
			if(File.Exists(runnerPath)) {
				string pidPath = Path.Combine(RunnerDirectoryPath, "Runner.pid");

				void RequestCompile() => SendOptionFile(
					Path.Combine(Path.GetDirectoryName(option.OutputPath), Path.ChangeExtension(Path.GetFileName(option.OutputPath), "option")),
					option,
					onComplete ?? OnCompileComplete
				);

				if(File.Exists(pidPath)) {
					int pid = int.Parse(File.ReadAllText(pidPath));
					try {
						var proc = System.Diagnostics.Process.GetProcessById(pid);
						if(!proc.HasExited) {
							// Reconnect to runner
							RequestCompile();
							return;
						}
					}
					catch { }

					File.Delete(pidPath);
				}
				CreateConfigFile(RunnerExecutablePath);
#if UNITY_EDITOR_WIN && !UNODE_DEV && false
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
					FileName = Path.GetFullPath(runnerPath),
					UseShellExecute = false,
					CreateNoWindow = false,
				});
#else
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
					FileName = FindDotnetExecutable(),
					Arguments = $"\"{runnerPath}\"",
#if UNODE_DEV
					UseShellExecute = true,
					CreateNoWindow = false,
#else
					UseShellExecute = false,
					CreateNoWindow = true,
#endif
				});
#endif
				RequestCompile();
			}
			else {
				Debug.LogError($"Runner not found: {runnerPath}");
			}
		}

		private static void CreateConfigFile(string outputPath) {
			HashSet<string> allReferences = new HashSet<string>();

			var dir = Path.GetDirectoryName(typeof(CSharpCompilation).Assembly.Location);
			Directory.EnumerateFiles(dir, "*.dll").ToList().ForEach(path => {
				try {
					allReferences.Add(path);
				}
				catch { }
			});

			{//Find Microsoft.CodeAnalysis.dll in Unity Editor folder and add it to references, since it's not loaded in the current AppDomain
				var editorPath = EditorApplication.applicationContentsPath;
				var roslynPath = Path.Combine(editorPath, "DotNetSdkRoslyn");
				string dllPath = Path.Combine(roslynPath, "Microsoft.CodeAnalysis.dll");
				if(File.Exists(dllPath)) {
					allReferences.Add(Path.GetFullPath(dllPath));
				}
				dllPath = Path.Combine(roslynPath, "Microsoft.CodeAnalysis.CSharp.dll");
				if(File.Exists(dllPath)) {
					allReferences.Add(Path.GetFullPath(dllPath));
				}
			}

			var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach(var assembly in loadedAssemblies) {
				try {
					var location = assembly.Location;
					if(!string.IsNullOrEmpty(location) && File.Exists(location)) {
						if(location.EndsWith("Unity.CodeCompiler.CodeGen.dll")) {
							File.Copy(location, Path.Combine(Path.GetDirectoryName(outputPath), "Unity.CodeCompiler.CodeGen.dll"), true);
							continue;
						}
						allReferences.Add(location);
					}
				}
				catch { }
			}
			//foreach(var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor)) {
			//	var path = Path.GetFullPath(assembly.outputPath);
			//	var name = AssemblyName.GetAssemblyName(path).FullName;
			//	if(uniqueReferences.ContainsKey(name)) {
			//		continue;
			//	}
			//	uniqueReferences.Add(name, path);
			//	foreach(var reference in assembly.allReferences) {
			//		var refPath = Path.GetFullPath(reference);
			//		var refName = AssemblyName.GetAssemblyName(refPath).FullName;
			//		if(!uniqueReferences.ContainsKey(refName)) {
			//			uniqueReferences.Add(refName, refPath);
			//		}
			//	}
			//}

			File.WriteAllLines(Path.Combine(Path.GetDirectoryName(outputPath), "Runner.config"), allReferences);
		}

		static Assembly AssemblyCSharp {
			get {
				if(CachedData.assemblyCSharp == null && CachedData.hasDefaultAssembly == null) {
					var assemblies = CompilationPipeline.GetAssemblies();
					for(int i = 0; i < assemblies.Length; i++) {
						var assembly = assemblies[i];
						if(assembly.name == "Assembly-CSharp") {
							CachedData.assemblyCSharp = assembly;
							CachedData.hasDefaultAssembly = true;
						}
					}
				}
				return CachedData.assemblyCSharp;
			}
		}

		static int id = 0;
		static CodeCompiler.CodeCompilerOption CreateCompilerOption() {
			return CreateCompilerOption(AssemblyCSharp, "RoslynRunner");
		}

		public static CodeCompiler.CodeCompilerOption CreateCompilerOption(Assembly assembly, string assemblyName) {
			var option = new CodeCompiler.CodeCompilerOption() {
				AssemblyName = assemblyName,
				Defines = assembly.defines,
				OutputPath = Path.GetFullPath(Path.Combine(RunnerDirectoryPath, $"Output/Assembly{Interlocked.Increment(ref id)}.dll")),
				OutputResultPath = Path.GetFullPath(Path.Combine(RunnerDirectoryPath, $"Output/Assembly{id}.result")),
				References = assembly.allReferences.Select(path => Path.GetFullPath(path)).ToArray(),
				SourceFiles = assembly.sourceFiles.Select(path => Path.GetFullPath(path)).ToArray(),
				ScriptCompilerOptions = new CodeCompiler.ScriptCompilerOptions() {
					AllowUnsafeCode = assembly.compilerOptions.AllowUnsafeCode,
					AnalyzerConfigPath = assembly.compilerOptions.AnalyzerConfigPath,
					RoslynAnalyzerDllPaths = assembly.compilerOptions.RoslynAnalyzerDllPaths.Select(path => Path.GetFullPath(path)).ToArray(),
					RoslynAdditionalFilePaths = assembly.compilerOptions.RoslynAdditionalFilePaths,
					RoslynAnalyzerRulesetPath = assembly.compilerOptions.RoslynAnalyzerRulesetPath,
					RunILPP = true,
				}
			};
			return option;
		}

		static void CreateCompilerOptionFile(string outputPath) {
			//File.WriteAllText(
			//	Path.Combine(Path.GetDirectoryName(outputPath), "Runner.option"),
			//	CodeCompiler.CodeCompiler.Serialize(CreateCompilerOption())
			//);
			File.WriteAllText(
				Path.Combine(Path.GetDirectoryName(outputPath), "Runner.runtimeconfig.json"),
				RuntimeConfig
			);
		}

		public static void Close() {
			var pidPath = Path.Combine(RunnerDirectoryPath, "Runner.pid");
			if(File.Exists(pidPath)) {
				int pid = int.Parse(File.ReadAllText(pidPath));
				try {
					var proc = System.Diagnostics.Process.GetProcessById(pid);
					proc.Kill();
				}
				catch { }
				File.Delete(pidPath);
			}
		}

		static void OnCompileComplete(CodeCompiler.CodeCompilerResult result) {
			if(result.Success) {
				if(result.ILPPApplied) {
					Debug.Log("Compilation succeeded with ILPP applied");
				}
				else {
					Debug.Log("Compilation succeeded");
				}
			}
			else {
				Debug.LogError("Compilation failed");
				foreach(var error in result.Errors) {
					Debug.LogError(error);
				}
			}
		}

		private static void SendOptionFile(string optionPath, CodeCompiler.CodeCompilerOption option, Action<CodeCompiler.CodeCompilerResult> onComplete) {
			optionPath = Path.GetFullPath(optionPath);
			Directory.CreateDirectory(Path.GetDirectoryName(optionPath));
			File.WriteAllText(optionPath, CodeCompiler.CodeCompiler.Serialize(option));

			if(File.Exists(option.OutputResultPath)) {
				// Ensure old result file is deleted before compilation to prevent reading stale results
				File.Delete(option.OutputResultPath);
			}

			using var pipeClient = new System.IO.Pipes.NamedPipeClientStream(".", CodeCompiler.CodeCompiler.PipeName, System.IO.Pipes.PipeDirection.Out);
			pipeClient.Connect(1000);

			using var writer = new StreamWriter(pipeClient, System.Text.Encoding.UTF8);
			writer.Write(optionPath);
			writer.Flush();

			var times = DateTime.Now.Ticks;
			new Thread(async () => {
				while(pipeClient.IsConnected) {
					await System.Threading.Tasks.Task.Delay(10);
				}
				while(!File.Exists(option.OutputResultPath)) {
					if(DateTime.Now.Ticks - times > 30 * TimeSpan.TicksPerSecond) {
						Debug.LogError("Compilation timed out");
						return;
					}
					await System.Threading.Tasks.Task.Delay(1);
				}
				const int maxRetries = 5;
				const int delayMs = 100;
				for(int i = 0; i < maxRetries; i++) {
					try {
						var result = CodeCompiler.CodeCompiler.Deserialize<CodeCompiler.CodeCompilerResult>(File.ReadAllText(option.OutputResultPath));
						onComplete?.Invoke(result);
						return;
					}
					catch(IOException) {
						if(i == maxRetries - 1)
							throw;
						Thread.Sleep(delayMs);
					}
				}
			}).Start();
		}

		static string dotnetPath;
		static string FindDotnetExecutable() {
			if(dotnetPath == null) {
				dotnetPath = string.Empty;
				if(IsCommandWorking("dotnet")) {
					dotnetPath = "dotnet";
					return dotnetPath;
				}
				string[] knowDotnetPaths;
				if(Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer) {
					knowDotnetPaths = new string[] {
						EditorApplication.applicationContentsPath + "/NetCoreRuntime/dotnet",
						"/usr/local/share/dotnet/dotnet",
						"/usr/local/bin/dotnet",
						"/opt/homebrew/bin/dotnet",
					};
				}
				else if(Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer) {
					knowDotnetPaths = new string[] {
						EditorApplication.applicationContentsPath + "/NetCoreRuntime/dotnet",
						"/usr/local/share/dotnet/dotnet",
						"/usr/bin/dotnet",
						"/usr/local/bin/dotnet",
					};
				}
				else {
					knowDotnetPaths = new string[] {
						EditorApplication.applicationContentsPath + "/NetCoreRuntime/dotnet.exe",
						@"C:\Program Files\dotnet\dotnet.exe",
						@"C:\Program Files (x86)\dotnet\dotnet.exe",
					};
				}
				foreach(var path in knowDotnetPaths) {
					if(File.Exists(path)) {
						if(IsCommandWorking(path)) {
							dotnetPath = path;
							return dotnetPath;
						}
					}
				}
			}
			return dotnetPath;
		}

		static bool IsCommandWorking(string path) {
			try {
				var info = new System.Diagnostics.ProcessStartInfo {
					FileName = path,
					Arguments = "--version",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				using(var process = System.Diagnostics.Process.Start(info)) {
					process.WaitForExit();
					return process.ExitCode == 0;
				}
			}
			catch { }
			return false;
		}

		private class CachedData {
			internal static Assembly assemblyCSharp;
			internal static bool? hasDefaultAssembly;
		}
	}
}