using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	//[NodeMenu("Flow", "Macro")]
	public class MacroNode : Node, IMacro, ISuperNode, IGeneratorPrePostInitializer {
		private List<MacroPortNode> inputFlows;
		private List<MacroPortNode> inputValues;
		private List<MacroPortNode> outputFlows;
		private List<MacroPortNode> outputValues;

		public IEnumerable<NodeObject> nestedFlowNodes {
			get {
				Refresh();
				foreach(var n in inputFlows) {
					yield return n.nodeObject;
				}
			}
		}

		IEnumerable<MacroPortNode> IMacro.InputFlows => inputFlows;
		IEnumerable<MacroPortNode> IMacro.InputValues => inputValues;
		IEnumerable<MacroPortNode> IMacro.OutputFlows => outputFlows;
		IEnumerable<MacroPortNode> IMacro.OutputValues => outputValues;

		protected override void OnRegister() {
			Refresh();
			//Initialize Flow Inputs
			for(int i = 0; i < inputFlows.Count; i++) {
				var macroPort = inputFlows[i];
				FlowInput port = null;
				port = FlowInput(macroPort.id.ToString(), (flow) => flow.Next(macroPort.exit)).SetName(macroPort.GetTitle());
				macroPort.enter = port;
			}
			//Initialize Flow Outputs
			for(int i = 0; i < outputFlows.Count; i++) {
				var macroPort = outputFlows[i];
				macroPort.exit = FlowOutput(macroPort.id.ToString()).SetName(macroPort.GetTitle());
			}
			//Initialize Value Inputs
			for(int i = 0; i < inputValues.Count; i++) {
				var macroPort = inputValues[i];
				macroPort.input = ValueInput(macroPort.id.ToString(), macroPort.ReturnType()).SetName(macroPort.GetTitle());
			}
			//Initialize Value Outputs
			for(int i = 0; i < outputValues.Count; i++) {
				var macroPort = outputValues[i];
				macroPort.output = ValueOutput(macroPort.id.ToString(), macroPort.ReturnType(), PortAccessibility.ReadWrite).SetName(macroPort.GetTitle());
				macroPort.output.AssignGetCallback(macroPort.nodeObject.GetPrimaryValue);
				macroPort.output.AssignSetCallback(macroPort.nodeObject.SetPrimaryValue);
			}
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			//Initialize Flow Outputs
			int index = 0;
			foreach(var p in outputFlows) {
				var linkedPort = p;
				instance.SetElementData(linkedPort, outputFlows[index].exit);
				index++;
			}
			//Initialize Value Inputs
			index = 0;
			foreach(var p in inputValues) {
				var linkedPort = p;
				instance.SetElementData(linkedPort, inputValues[index].input);
				index++;
			}
			//Initialize Value Outputs
			index = 0;
			foreach(var p in outputValues) {
				var linkedPort = p;
				instance.SetElementData(linkedPort, outputValues[index].input);
				index++;
			}
		}

		#region Variable Initialization
		public MacroNode() {
			InitPrivateVariables();
		}

		[System.Runtime.Serialization.OnDeserialized]
		private void InitPrivateVariables() {
			inputFlows = new List<MacroPortNode>();
			inputValues = new List<MacroPortNode>();
			outputFlows = new List<MacroPortNode>();
			outputValues = new List<MacroPortNode>();
		}
		#endregion

		public void Refresh() {
			inputFlows.Clear();
			inputValues.Clear();
			outputFlows.Clear();
			outputValues.Clear();

			foreach(var element in nodeObject) {
				if(element is NodeObject obj) {
					var node = obj.node;
					if(node is MacroPortNode) {
						MacroPortNode macro = node as MacroPortNode;
						switch(macro.kind) {
							case PortKind.FlowInput:
								inputFlows.Add(macro);
								break;
							case PortKind.FlowOutput:
								outputFlows.Add(macro);
								break;
							case PortKind.ValueInput:
								inputValues.Add(macro);
								break;
							case PortKind.ValueOutput:
								outputValues.Add(macro);
								break;
						}
					}
				}
			}
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public override string GetTitle() {
			return name;
		}

		public bool AllowCoroutine() {
			var pComp = nodeObject.parent;
			if(pComp is NodeContainer container) {
				return container.AllowCoroutine();
			} else if(pComp is NodeObject nodeObject && nodeObject.node is ISuperNode) {
				return (nodeObject.node as ISuperNode).AllowCoroutine();
			}
			return false;
		}

		public bool IsCoroutine() {
			foreach(var n in inputFlows) {
				if(n != null && n.IsCoroutine()) {
					return true;
				}
			}
			return false;
		}

		void IGeneratorPrePostInitializer.OnPreInitializer() {
			//TODO: fix me
			//foreach(VariableData variable in variables) {
			//	variable.modifier.SetPrivate();
			//	CG.RegisterVariable(variable);
			//}
			foreach(var p in inputFlows) {
				CG.RegisterEntry(p);
			}
			CG.RegisterPostInitialization(() => {
				//Initialize Flow Inputs
				foreach(var p in inputFlows) {
					var port = p;
					CG.RegisterPort(port.enter, () => CG.GeneratePort(port.exit));
				}
				//Initialize Flow Outputs
				foreach(var p in outputFlows) {
					var port = p;
					CG.RegisterPort(port.enter, () => CG.GeneratePort(port.exit));
				}
				//Initialize Value Inputs
				foreach(var p in inputValues) {
					var port = p;
					CG.RegisterPort(port.output, () => CG.GeneratePort(port.input));
				}
				//Initialize Value Outputs
				foreach(var p in outputValues) {
					var port = p;
					CG.RegisterPort(port.output, () => CG.GeneratePort(port.input));
				}
				//foreach(var variable in variableContainer.collections) {
				//	variable.modifier.SetPrivate();
				//	CG.RegisterVariable(variable);
				//}
			});
		}

		void IGeneratorPrePostInitializer.OnPostInitializer() {
		}
	}
}