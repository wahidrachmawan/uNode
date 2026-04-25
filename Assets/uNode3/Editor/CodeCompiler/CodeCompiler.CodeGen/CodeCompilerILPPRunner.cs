using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace MaxyGames.CodeCompiler {
	public class CodeCompilerILPPRunner {
		public static void RunILPP(string path, ref byte[] rawAssembly, ref byte[] rawPdb, string[] references, string[] defines, IEnumerable<Assembly> codeGenAssemblies) {
			if(rawAssembly == null) {
				rawAssembly = File.ReadAllBytes(path);
				rawPdb = File.ReadAllBytes(Path.ChangeExtension(path, ".pdb"));
			}
			var compiledAssembly = new uNodeCodeCompiledAssembly(rawAssembly, rawPdb, path, references, defines);

			var ilpp = new List<ILPostProcessor>();
			foreach(var assembly in codeGenAssemblies) {
				var name = assembly.GetName().Name;
				//Console.WriteLine("Checking assembly: " + name);
				if(name.StartsWith("Unity.") && name.EndsWith("CodeGen")) {
					foreach(var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && (typeof(ILPostProcessor).IsAssignableFrom(t)))) {
						ilpp.Add((ILPostProcessor)Activator.CreateInstance(type));
					}
				}
			}

			void Process(ILPostProcessor v) {
				try {
					if(v.WillProcess(compiledAssembly)) {
						Console.WriteLine("\n\nProcessing ILPP: " + v.GetType().FullName);

						var iLPostProcessResult = v.Process(compiledAssembly);

						if(iLPostProcessResult == null) {
							Console.WriteLine("Result is null");
							return;
						}
						if(iLPostProcessResult.InMemoryAssembly != null) {
							if(iLPostProcessResult.InMemoryAssembly.PeData == null) {
								Console.WriteLine("ILPostProcessor produced an assembly without PE data");
							}
							compiledAssembly.InMemoryAssembly = iLPostProcessResult.InMemoryAssembly;
						}
						else {
							Console.WriteLine("Result InMemoryAssembly is null");
						}
						if(iLPostProcessResult.Diagnostics?.Count > 0) {
							Console.WriteLine("----------DIAGONISTICS-------------");
							foreach(var diag in iLPostProcessResult.Diagnostics) {
								Console.WriteLine($"[{diag.DiagnosticType}]{diag.MessageData}\n\tOn file: {diag.File} ({diag.Line}-{diag.Column})");
							}
						}
					}
				}
				catch {
					Console.WriteLine("Error on processing ILPP: " + v.GetType().FullName);
				}
			}

			ILPostProcessor burstILPP = null;
			//Environment.SetEnvironmentVariable("UNITY_BURST_DEBUG", "3");
			foreach(var v in ilpp) {
				if(v.GetType().Name == "BurstILPostProcessor") {
					burstILPP = v;
					continue;
				}
				//if(v.GetType().Name == "EntitiesILPostProcessors") continue;
				Process(v);
			}
			if(burstILPP != null) {
				Process(burstILPP);
			}
			rawAssembly = compiledAssembly.InMemoryAssembly.PeData;
			rawPdb = compiledAssembly.InMemoryAssembly.PdbData;
		}

		public static void RunILPostProcessor(string path, out byte[] rawAssembly, out byte[] rawPdb, string[] references, string[] defines, IEnumerable<Assembly> codeGenAssemblies) {
			rawAssembly = null;
			rawPdb = null;
			if(!File.Exists(path)) {
				Console.WriteLine("Assembly not found: " + path);
				return;
			}

			var rawAssembly1 = File.ReadAllBytes(path);
			var rawPdb1 = File.ReadAllBytes(Path.ChangeExtension(path, ".pdb"));
			RunILPP(path, ref rawAssembly1, ref rawPdb1, references, defines, codeGenAssemblies);
			rawAssembly = rawAssembly1;
			rawPdb = rawPdb1;
		}
	}

	public class uNodeCodeCompiledAssembly : ICompiledAssembly {
		public InMemoryAssembly InMemoryAssembly { get; set; }
		public string Name { get; }
		public string[] References { get; }
		public string[] Defines { get; }

		public uNodeCodeCompiledAssembly(byte[] pe, byte[] pdb, string name, string[] refs, string[] defs) {
			InMemoryAssembly = new InMemoryAssembly(pe, pdb);
			Name = name;
			References = refs;
			Defines = defs;
		}
	}
}
