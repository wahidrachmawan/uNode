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
			foreach(var v in ilpp) {
				//if(v.GetType().Name == "BurstILPostProcessor") continue;
				if(v.WillProcess(compiledAssembly)) {
					var iLPostProcessResult = v.Process(compiledAssembly);
					if(iLPostProcessResult == null) {
						continue;
					}
					if(iLPostProcessResult.InMemoryAssembly != null) {
						if(iLPostProcessResult.InMemoryAssembly.PeData == null) {
							Console.WriteLine("ILPostProcessor produced an assembly without PE data");
						}
						compiledAssembly.InMemoryAssembly = iLPostProcessResult.InMemoryAssembly;
					}
				}
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
