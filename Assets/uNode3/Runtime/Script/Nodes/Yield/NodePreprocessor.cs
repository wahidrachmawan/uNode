using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Preprocessor", icon = typeof(MonoBehaviour), hasFlowInput = true, hasFlowOutput = true)]
	public class NodePreprocessor : FlowNode {
		public List<Data> symbols = new List<Data>();
		[NonSerialized]
		public FlowOutput defaultSymbol;

		[Serializable]
		public class Data {
			public string id = uNodeUtility.GenerateUID();
			public string symbol = "MySymbol";
			[NonSerialized]
			public FlowOutput flow;
		}

		protected override void OnRegister() {
			foreach(var d in symbols) {
				d.flow = FlowOutput(d.id).SetName(d.symbol);
			}
			defaultSymbol = FlowOutput(nameof(defaultSymbol)).SetName("Default");
			base.OnRegister();
		}

		protected override string GenerateFlowCode() {
			return CG.Flow(
				CG.Flow(symbols.Select((symbol, index) =>
					CG.Flow(
						(index == 0 ? "#if " : "#elif ") + symbol.symbol,
						CG.Flow(symbol.flow)
					)
				)).AddLineInEnd().Add(defaultSymbol.isAssigned ? CG.Flow("#else", CG.Flow(defaultSymbol), "#endif") : "#endif"),
				CG.FlowFinish(enter, exit)
			);
		}


		static string[] m_defineSymbols;

		protected override void OnExecuted(Flow flow) {
#if !UNITY_EDITOR
			throw new Exception("Preprocessor node only supported in editor when using reflection, or please compile the graph to c# to fix this.\nError from graph: " + nodeObject.graphContainer.GetFullGraphName());
#else
			static bool IsSymbolDefined(string symbol) {
				if(m_defineSymbols == null) {
					UnityEditor.PlayerSettings.GetScriptingDefineSymbols(
						UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(
							UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup
						),
						out var arr
					);
					var defines = new List<string>(arr);
					defines.Add("UNITY_EDITOR");
#if UNITY_EDITOR_64
					defines.Add("UNITY_EDITOR_64");
#endif
#if UNITY_EDITOR_WIN
					defines.Add("UNITY_EDITOR_WIN");
#endif
#if UNITY_EDITOR_OSX
					defines.Add("UNITY_EDITOR_OSX");
#endif
#if UNITY_EDITOR_LINUX
					defines.Add("UNITY_EDITOR_LINUX");
#endif
#if UNITY_STANDALONE
					defines.Add("UNITY_STANDALONE");
#endif
#if UNITY_STANDALONE_WIN
					defines.Add("UNITY_STANDALONE_WIN");
#endif
#if UNITY_STANDALONE_OSX
					defines.Add("UNITY_STANDALONE_OSX");
#endif
#if UNITY_STANDALONE_LINUX
					defines.Add("UNITY_STANDALONE_LINUX");
#endif
#if UNITY_WSA
					defines.Add("UNITY_WSA");
#endif
#if UNITY_ANDROID
					defines.Add("UNITY_ANDROID");
#endif
#if UNITY_IOS
					defines.Add("UNITY_IOS");
#endif
#if UNITY_TVOS
					defines.Add("UNITY_TVOS");
#endif
#if UNITY_PS4
					defines.Add("UNITY_PS4");
#endif
#if UNITY_PS5
					defines.Add("UNITY_PS5");
#endif
#if UNITY_XBOXONE
					defines.Add("UNITY_XBOXONE");
#endif
#if UNITY_GAMECORE
					defines.Add("UNITY_GAMECORE");
#endif
#if UNITY_GAMECORE_XBOXONE
					defines.Add("UNITY_GAMECORE_XBOXONE");
#endif
#if UNITY_GAMECORE_XBOXSERIES
					defines.Add("UNITY_GAMECORE_XBOXSERIES");
#endif
#if UNITY_SWITCH
					defines.Add("UNITY_SWITCH");
#endif
#if UNITY_WEBGL
					defines.Add("UNITY_WEBGL");
#endif
#if DEVELOPMENT_BUILD
					defines.Add("DEVELOPMENT_BUILD");
#endif
#if ENABLE_IL2CPP
					defines.Add("ENABLE_IL2CPP");
#endif
#if ENABLE_MONO
					defines.Add("ENABLE_MONO");
#endif
#if ENABLE_DOTNET
					defines.Add("ENABLE_DOTNET");
#endif
#if UNITY_PIPELINE_BUILTIN
					defines.Add("UNITY_PIPELINE_BUILTIN");
#endif
#if UNITY_PIPELINE_URP
					defines.Add("UNITY_PIPELINE_URP");
#endif
#if UNITY_PIPELINE_HDRP
					defines.Add("UNITY_PIPELINE_HDRP");
#endif
#if ENABLE_VR
					defines.Add("ENABLE_VR");
#endif
#if UNITY_XR_MANAGEMENT
					defines.Add("UNITY_XR_MANAGEMENT");
#endif
#if UNITY_AR_FOUNDATION
					defines.Add("UNITY_AR_FOUNDATION");
#endif
#if UNITY_OPENXR
					defines.Add("UNITY_OPENXR");
#endif
#if UNITY_OCULUS
					defines.Add("UNITY_OCULUS");
#endif
#if UNITY_OPENVR
					defines.Add("UNITY_OPENVR");
#endif
#if ENABLE_INPUT_SYSTEM
					defines.Add("ENABLE_INPUT_SYSTEM");
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
					defines.Add("ENABLE_LEGACY_INPUT_MANAGER");
#endif
#if ENABLE_PHYSICS
					defines.Add("ENABLE_PHYSICS");
#endif
#if ENABLE_PHYSICS2D
					defines.Add("ENABLE_PHYSICS2D");
#endif
#if ENABLE_AUDIO
					defines.Add("ENABLE_AUDIO");
#endif
#if ENABLE_UNET
					defines.Add("ENABLE_UNET");
#endif
#if ENABLE_NETCODE
					defines.Add("ENABLE_NETCODE");
#endif
#if NETSTANDARD
					defines.Add("NETSTANDARD");
#endif
#if NETSTANDARD2_1
					defines.Add("NETSTANDARD2_1");
#endif
#if NET_STANDARD
					defines.Add("NET_STANDARD");
#endif
#if NET_STANDARD_2_0
					defines.Add("NET_STANDARD_2_0");
#endif
#if NET_STANDARD_2_1
					defines.Add("NET_STANDARD_2_1");
#endif
#if ENABLE_BURST
					defines.Add("ENABLE_BURST");
#endif
#if UNITY_JOBS
					defines.Add("UNITY_JOBS");
#endif
#if UNITY_DOTS
					defines.Add("UNITY_DOTS");
#endif

					m_defineSymbols = defines.ToArray();
					//foreach(var define in m_defineSymbols) {
					//	Debug.Log(define);
					//}
				}
				return m_defineSymbols.Contains(symbol);
			}

			foreach(var d in symbols) {
				if(IsSymbolDefined(d.symbol)) {
					flow.Next(d.flow);
					return;
				}
			}
#endif
		}
	}
}
