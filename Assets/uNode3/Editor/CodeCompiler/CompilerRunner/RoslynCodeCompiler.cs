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
		const string RuntimeConfig = @"{
	""runtimeOptions"": {
		""tfm"": ""net6.0"",
		""rollForward"": ""LatestMinor"",
		""framework"": {
			""name"": ""Microsoft.NETCore.App"",
			""version"": ""6.0.0""
		}
	}
}";
		public const string RunnerExecutablePath = "Library/uNodeRoslynCompiler/Runner.dll";
		public const string RunnerDirectoryPath = "Library/uNodeRoslynCompiler";
		static string RunnerInProjectPath => AssetDatabase.GUIDToAssetPath("1bf56dea8541bb44389a93fd8de8d808");

		static RoslynCodeCompiler() {
			EditorApplication.quitting -= CloseCodeCompiler;
			EditorApplication.quitting += CloseCodeCompiler;
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

		/// <summary>
		/// Runs the code compiler with the specified options, building the runner executable if necessary.
		/// </summary>
		/// <param name="option">The options to use for the code compilation.</param>
		/// <param name="onComplete">An optional callback invoked with the compilation result.</param>
		public static void Run(CodeCompiler.CodeCompilerOption option, Action<CodeCompiler.CodeCompilerResult> onComplete = null) {
			//if(!File.Exists(RunnerExecutablePath)) {
			//	Debug.Log("Runner not found, building...");
			//	Build(RunnerExecutablePath);
			//}
			EnsureCompilerHasBuild();
			Run(RunnerExecutablePath, option, onComplete);
		}

		static void Build(string outputPath) {
			var codeCompilerName = typeof(MaxyGames.CodeCompiler.CodeCompiler).Assembly.GetName().Name;
			var codeCompilerAssembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(asm => asm.name == codeCompilerName);
			Build(outputPath, codeCompilerAssembly);
		}

		static void Build(string outputPath, Assembly codeCompilerAssembly) {
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

#if UNODE_DEV
			var assetPath = RunnerInProjectPath;
			if(File.Exists(assetPath) && File.Exists(outputPath)) {
				//Update the main runner in project for ease
				File.Copy(outputPath, assetPath, true);
			}

			Debug.Log("Runner built successfully on: " + outputPath);
			EditorApplication.delayCall += static () => {
				AssetDatabase.Refresh();
			};
#endif
		}

		static void Run(string runnerPath, CodeCompiler.CodeCompilerOption option, Action<CodeCompiler.CodeCompilerResult> onComplete = null) {
			if(File.Exists(runnerPath)) {
				string pidPath = Path.Combine(RunnerDirectoryPath, "Runner.pid");

				void RequestCompile() => SendData(
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

				if(Directory.Exists(Path.Combine(RunnerDirectoryPath, "Output"))) {
					try {
						// Clean up old output to prevent confusion
						Directory.Delete(Path.Combine(RunnerDirectoryPath, "Output"), true);
					}
					catch { }
				}

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

		private static string[] m_RoslynPaths;
		private static string[] RoslynPaths {
			get {
				if(m_RoslynPaths == null) {
					m_RoslynPaths = new string[2];
					//Find Microsoft.CodeAnalysis.dll in Unity Editor folder and add it to references, since it's not loaded in the current AppDomain
					var editorPath = EditorApplication.applicationContentsPath;
					var roslynPath = Path.Combine(editorPath, "DotNetSdkRoslyn");
					string dllPath = Path.Combine(roslynPath, "Microsoft.CodeAnalysis.dll");
					if(File.Exists(dllPath)) {
						m_RoslynPaths[0] = Path.GetFullPath(dllPath);
					}
					dllPath = Path.Combine(roslynPath, "Microsoft.CodeAnalysis.CSharp.dll");
					if(File.Exists(dllPath)) {
						m_RoslynPaths[1] = Path.GetFullPath(dllPath);
					}
				}
				return m_RoslynPaths;
			}
		}

		private static void CreateConfigFile(string outputPath) {
			HashSet<string> allReferences = new HashSet<string>();

			//var dir = Path.GetDirectoryName(typeof(CSharpCompilation).Assembly.Location);
			//Directory.EnumerateFiles(dir, "*.dll").ToList().ForEach(path => {
			//	try {
			//		allReferences.Add(path);
			//	}
			//	catch { }
			//});

			{//Find Microsoft.CodeAnalysis.dll in Unity Editor folder and add it to references, since it's not loaded in the current AppDomain
				foreach(var path in RoslynPaths) {
					allReferences.Add(path);
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
						if(location.Contains("Microsoft.CodeAnalysis")) {
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
#if UNODE_DEV
				OutputResultPath = Path.GetFullPath(Path.Combine(RunnerDirectoryPath, $"Output/Assembly{id}.result")),
#endif
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

		#region SendData
		private static void SendData(CodeCompiler.CodeCompilerOption option, Action<CodeCompiler.CodeCompilerResult> onComplete) {
			new Thread(() => SendDataAsync(option, onComplete)).Start();
		}

		private static async void SendDataAsync(CodeCompiler.CodeCompilerOption option, Action<CodeCompiler.CodeCompilerResult> onComplete) {
//#if UNODE_DEV
//			Debug.Log("Sending compilation request to runner...");
//			var stopwatch = new System.Diagnostics.Stopwatch();
//			stopwatch.Start();
//#endif
			try {
				if(string.IsNullOrEmpty(option.OutputResultPath) == false && File.Exists(option.OutputResultPath)) {
					// Ensure old result file is deleted before compilation to prevent reading stale results
					File.Delete(option.OutputResultPath);
				}
				using var pipeClient = new System.IO.Pipes.NamedPipeClientStream(".", CodeCompiler.CodeCompiler.PipeName, System.IO.Pipes.PipeDirection.InOut);
				pipeClient.Connect(5000);

				await PipeHelper.SendStringAsync(pipeClient, CodeCompiler.CodeCompiler.Serialize(option));

				var resultString = await PipeHelper.ReceiveStringAsync(pipeClient);
				var result = CodeCompiler.CodeCompiler.Deserialize<CodeCompiler.CodeCompilerResult>(resultString);
				onComplete?.Invoke(result);
			}
			catch(Exception ex) {
				Debug.LogException(ex);
			}
//#if UNODE_DEV
//			Debug.Log("Elapsed time: " + stopwatch.ElapsedMilliseconds + " ms");
//#endif
		}
		#endregion

		#region Dotnet
		static string dotnetPath;
		static string FindDotnetExecutable() {
			if(dotnetPath == null) {
				dotnetPath = string.Empty;
				if(IsDotNetAvailable() || IsCommandWorking("dotnet")) {
					dotnetPath = "dotnet";
					return dotnetPath;
				}
				string[] knowDotnetPaths;
				string unityDotnet;
				if(Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer) {
					knowDotnetPaths = new string[] {
						unityDotnet = EditorApplication.applicationContentsPath + "/NetCoreRuntime/dotnet",
						"/usr/local/share/dotnet/dotnet",
						"/usr/local/bin/dotnet",
						"/opt/homebrew/bin/dotnet",
					};
				}
				else if(Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer) {
					knowDotnetPaths = new string[] {
						unityDotnet = EditorApplication.applicationContentsPath + "/NetCoreRuntime/dotnet",
						"/usr/local/share/dotnet/dotnet",
						"/usr/bin/dotnet",
						"/usr/local/bin/dotnet",
					};
				}
				else {
					knowDotnetPaths = new string[] {
						unityDotnet = EditorApplication.applicationContentsPath + "/NetCoreRuntime/dotnet.exe",
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
				if(string.IsNullOrEmpty(dotnetPath)) {
					bool isNet6Installed = DotNetRuntimeDirectoryChecker.IsNet60RuntimeInstalled();
					if(isNet6Installed == false) {
						const string dotNet60DownloadUrl = "https://dotnet.microsoft.com/en-us/download/dotnet/6.0";
						string errorMessage = $"❌ .NET 6.0 Runtime is NOT installed!\n\n" +
									"uNode Compiler requires .NET 6.0 Runtime to run.\n\n" +
									$"Please download and install it from:\n{dotNet60DownloadUrl}";
						Debug.LogError(errorMessage);
						Debug.Log($"📥 Please Install .NET 6.0 Runtime, here's the link to download: {dotNet60DownloadUrl}");
					}
					else {
						dotnetPath = unityDotnet;
					}
				}
			}
			return dotnetPath;
		}

		public static class DotNetRuntimeDirectoryChecker {
			private static string GetDotnetSharedPath() {
				if(Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer) {
					return "/usr/local/share/dotnet/shared";
				}
				else if(Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer) {
					return "/usr/share/dotnet/shared";
				}
				else {
					return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "shared");
				}
			}

			/// <summary>
			/// Checks if Microsoft.NETCore.App version 6.0.x is installed by checking the directory structure.
			/// </summary>
			public static bool IsNet60RuntimeInstalled() {
				string sharedPath = GetDotnetSharedPath();
				string runtimePath = Path.Combine(sharedPath, "Microsoft.NETCore.App");

				if(!Directory.Exists(runtimePath))
					return false;

				return Directory.GetDirectories(runtimePath).Any(dir => Path.GetFileName(dir).StartsWith("6."));
			}
		}

		static bool IsDotNetAvailable() {
			string dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
			string path = Environment.GetEnvironmentVariable("PATH");

			bool hasDotnetRoot = !string.IsNullOrEmpty(dotnetRoot) &&
								 System.IO.Directory.Exists(dotnetRoot);

			bool hasDotnetInPath = !string.IsNullOrEmpty(path) &&
								   path.Split(';').Any(p => p.Contains("dotnet"));

			return hasDotnetRoot || hasDotnetInPath;
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
		#endregion

		#region Callback
		static void CloseCodeCompiler() {
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
					Debug.LogError(error.message);
				}
			}
		}
		#endregion

		static void EnsureCompilerHasBuild() {
			if(SessionState.GetBool("uNode_RoslynCodeCompilerInitialized", false) == false || File.Exists(RunnerExecutablePath) == false) {
#if UNODE_DEV
				Debug.Log("Building compiler runner...");
#endif
				var path = RunnerInProjectPath;
				if(string.IsNullOrEmpty(path) == false && File.Exists(path)) {
					try {
						Directory.CreateDirectory(Path.GetDirectoryName(RunnerExecutablePath));
						File.Copy(path, Path.Combine(Path.GetDirectoryName(RunnerExecutablePath), "Runner.dll"), true);
						CreateConfigFile(RunnerExecutablePath);
						CreateCompilerOptionFile(RunnerExecutablePath);
						Debug.Log(path);
					}
					catch { }
				}
				else {
					Build(RunnerExecutablePath);
				}
				SessionState.SetBool("uNode_RoslynCodeCompilerInitialized", true);

				//var codeCompilerName = typeof(MaxyGames.CodeCompiler.CodeCompiler).Assembly.GetName().Name;
				//var codeCompilerAssembly = CompilationPipeline.GetAssemblies(AssembliesType.Editor).FirstOrDefault(asm => asm.name == codeCompilerName);
				//if(codeCompilerAssembly != null) {
				//	new Thread(() => {
				//		Build(RunnerExecutablePath, codeCompilerAssembly);
				//		EditorApplication.delayCall += () => {
				//			SessionState.SetBool("uNode_RoslynCodeCompilerInitialized", true);
				//			//if(File.Exists(RunnerExecutablePath)) {
				//			//	Debug.Log("Roslyn Code Compiler initialized successfully.");
				//			//}
				//			//else {
				//			//	Debug.LogError("Failed to initialize Roslyn Code Compiler.");
				//			//}
				//		};
				//	}).Start();
				//}
			}
		}

		private class CachedData {
			internal static Assembly assemblyCSharp;
			internal static bool? hasDefaultAssembly;
		}
	}
}