using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode {
	[CreateAssetMenu(fileName ="Macro", menuName ="uNode/Macro")]
	public class MacroGraph : GraphAsset, IMacroGraph, IGraphWithVariables {
		public string category = "Macro";
		[SerializeField, HideInInspector]
		internal List<string> usingNamespaces = new List<string>() { "UnityEngine", "System.Collections.Generic" };

		public IEnumerable<MacroPortNode> inputFlows => RuntimeGraphUtility.GetMacroInputFlows(GraphData);
		public IEnumerable<MacroPortNode> inputValues => RuntimeGraphUtility.GetMacroInputValues(GraphData);
		public IEnumerable<MacroPortNode> outputFlows => RuntimeGraphUtility.GetMacroOutputFlows(GraphData);
		public IEnumerable<MacroPortNode> outputValues => RuntimeGraphUtility.GetMacroOutputValues(GraphData);

		[SerializeField, HideInInspector]
		private bool hasCoroutineNodes;

		public bool HasCoroutineNode => hasCoroutineNodes;
		public string Namespace => "MaxyGames.Generated";
		List<string> IUsingNamespace.UsingNamespaces { get => usingNamespaces; }
	}
}