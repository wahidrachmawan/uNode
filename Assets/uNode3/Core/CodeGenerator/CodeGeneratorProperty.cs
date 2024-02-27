using MaxyGames.UNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MaxyGames {
	public static partial class CG {
        #region Constant
		private const string KEY_coroutineEventCode = "_ExecuteCoroutineEvent";
		private const string KEY_debugGetValueCode = "_DebugGetValue";
		private const string KEY_runtimeInterfaceKey = "i:";
		#endregion

		#region Events
		public static event Action<GeneratedData, GeneratorSetting> OnSuccessGeneratingGraph;
		#endregion

		#region Properties
		/// <summary>
		/// The generator data.
		/// </summary>
		public static GData generatorData { get; private set; }
		/// <summary>
		/// The generation state.
		/// </summary>
		public static GeneratorState generationState => generatorData.state;
		/// <summary>
		/// The generator setting.
		/// </summary>
		public static GeneratorSetting setting => generatorData.setting;

		/// <summary>
		/// True if currently generating script with debug.
		/// </summary>
		public static bool debugScript => setting.debugScript;
		/// <summary>
		/// When true, the Generated Data should contain graph informations.
		/// </summary>
		public static bool includeGraphInformation => setting.includeGraphInformation;

		/// <summary>
		/// True if currently is generating script.
		/// </summary>
		public static bool isGenerating { get; private set; }
		/// <summary>
		/// True if currently is generating ungrouped nodes.
		/// </summary>
		public static bool isInUngrouped { get; private set; }
		/// <summary>
		/// True on current function allowing yield statement to be generated
		/// </summary>
		public static bool allowYieldStatement => generatorData.currentBlock == null || generatorData.currentBlock.allowYield;

		/// <summary>
		/// The target uNode.
		/// </summary>
		public static IGraph graph { get; private set; }
		/// <summary>
		/// The target State Graph.
		/// </summary>
		public static bool hasMainGraph => graph is IStateGraph || graph is ICustomMainGraph;
		/// <summary>
		/// Is generating with the pure script mode?
		/// </summary>
		/// <value></value>
		public static bool generatePureScript {
			get{
				switch(setting.generationMode) {
					case GenerationKind.Performance:
						 return true;
					case GenerationKind.Compatibility:
						return false;
				}
				if(graphSystem != null) {
					switch(graphSystem.generationKind) {
						case GenerationKind.Performance:
							return true;
						case GenerationKind.Compatibility:
							return false;
					}
				}
				return true;
			}
		}
		
		/// <summary>
		/// The target graph system.
		/// </summary>
		/// <typeparam name="GraphSystemAttribute"></typeparam>
		/// <returns></returns>
		public static GraphSystemAttribute graphSystem { get; private set; }
        #endregion
	}
}