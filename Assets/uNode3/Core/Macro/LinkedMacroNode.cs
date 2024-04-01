using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace MaxyGames.UNode.Nodes {
	public class LinkedMacroNode : Node, IMacro, ILocalVariableSystem, IRefreshable, IGeneratorPrePostInitializer {
		public MacroGraph macroAsset;

		[HideInInspector]
		public List<Variable> variables = new List<Variable>();

		public List<FlowInput> inputFlows { get; set; }
		public List<ValueInput> inputValues { get; set; }
		public List<FlowOutput> outputFlows { get; set; }
		public List<ValueOutput> outputValues { get; set; }

		public IEnumerable<Variable> LocalVariables => variables;

		#region Init
		public LinkedMacroNode() {
			Init();
		}

		[System.Runtime.Serialization.OnDeserialized]
		private void Init() {
			inputFlows = new List<FlowInput>();
			inputValues = new List<ValueInput>();
			outputFlows = new List<FlowOutput>();
			outputValues = new List<ValueOutput>();
		}

		private int version;
		#endregion

		private class LinkedData {
			public Dictionary<int, FlowOutput> flowCallback = new Dictionary<int, FlowOutput>();
			public Dictionary<int, ValueInput> valueCallback = new Dictionary<int, ValueInput>();
		}

		protected override void OnRegister() {
			if(macroAsset != null) {
				version = macroAsset.GraphData.version;
				//Clean the data.
				inputFlows.Clear();
				inputValues.Clear();
				outputFlows.Clear();
				outputValues.Clear();
				//Initialize Flow Inputs
				foreach(var p in macroAsset.inputFlows) { 
					var macroPort = p;
					inputFlows.Add(
						FlowInput(macroPort.id.ToString(), flow => {
							var data = flow.GetElementData<LinkedData>(this);
							if(data.flowCallback.TryGetValue(macroPort.id, out var exit)) {
								flow.Next(exit);
							}
							else {
								throw new GraphException("Port not found", this);
							}
						}).SetName(macroPort.GetTitle())
					);
				}
				//Initialize Flow Outputs
				foreach(var p in macroAsset.outputFlows) {
					var macroPort = p;
					outputFlows.Add(
						FlowOutput(macroPort.id.ToString()).SetName(macroPort.GetTitle())
					);
				}
				//Initialize Value Inputs
				foreach(var p in macroAsset.inputValues) {
					var macroPort = p;
					inputValues.Add(
						ValueInput(macroPort.id.ToString(), macroPort.ReturnType()).SetName(macroPort.GetTitle())
					);
				}
				//Initialize Value Outputs
				foreach(var p in macroAsset.outputValues) {
					var macroPort = p;
					var port = ValueOutput(macroPort.id.ToString(), macroPort.ReturnType(), PortAccessibility.ReadWrite).SetName(macroPort.GetTitle());
					port.AssignGetCallback(instance => {
						var data = instance.GetElementData<LinkedData>(this);
						if(data.valueCallback.TryGetValue(macroPort.id, out var input)) {
							return input.GetValue(instance);
						}
						else {
							throw new GraphException("Port not found", this);
						}
					});
					port.AssignSetCallback((instance, value) => {
						var data = instance.GetElementData<LinkedData>(this);
						if(data.valueCallback.TryGetValue(macroPort.id, out var input)) {
							input.SetValue(instance, value);
						}
						else {
							throw new GraphException("Port not found", this);
						}
					});
					outputValues.Add(port);
				}
			}
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			if(macroAsset == null)
				throw new Exception("Missing macro asset");
			if(version != macroAsset.GraphData.version) {
				//Re-register the port in case it is out of date
				Register();
			}
			//Create a key for macro
			var key = new Tuple<object, object>(this, instance);
			//This is required to make sure that the `key` is not disposed
			instance.SetUserData(this, key);
			var runner = RuntimeGraphUtility.GetOrCreateGraphRunner(macroAsset, key);
			var graph = runner.GraphData;
			//Debug.Log(graph.version + " <> " + macroAsset.GraphData.version);
			graph.ForeachInChildrens(element => {
				element.OnRuntimeInitialize(instance);
			}, true);
			var elementData = new LinkedData();
			int index = 0;
			//Initialize Flow Inputs
			foreach(var p in RuntimeGraphUtility.GetMacroInputFlows(graph)) {
				var linkedPort = p;
				elementData.flowCallback.Add(linkedPort.id, linkedPort.exit);
				index++;
			}
			//Initialize Flow Outputs
			index = 0;
			foreach(var p in RuntimeGraphUtility.GetMacroOutputFlows(graph)) {
				var linkedPort = p;
				instance.SetElementData(linkedPort, outputFlows[index]);
				index++;
			}
			//Initialize Value Inputs
			index = 0;
			foreach(var p in RuntimeGraphUtility.GetMacroInputValues(graph)) {
				var linkedPort = p;
				instance.SetElementData(linkedPort, inputValues[index]);
				index++;
			}
			//Initialize Value Outputs
			index = 0;
			foreach(var p in RuntimeGraphUtility.GetMacroOutputValues(graph)) {
				var linkedPort = p;
				var port = outputValues[index];
				elementData.valueCallback.Add(linkedPort.id, linkedPort.input);
				index++;
			}
			instance.SetElementData(this, elementData);
		}

		IEnumerable<MacroPortNode> IMacro.InputFlows => (macroAsset as IMacro)?.InputFlows;
		IEnumerable<MacroPortNode> IMacro.InputValues => (macroAsset as IMacro)?.InputValues;
		IEnumerable<MacroPortNode> IMacro.OutputFlows => (macroAsset as IMacro)?.OutputFlows;
		IEnumerable<MacroPortNode> IMacro.OutputValues => (macroAsset as IMacro)?.OutputValues;

		public void Refresh() {
			if(macroAsset != null) {
				//for(int i = 0; i < variables.Count; i++) {
				//	bool flag = false;
				//	foreach(var mVar in macroAsset.GetVariables()) {
				//		if(mVar.id == variables[i].id) {
				//			variables[i].name = mVar.name;
				//			variables[i].type = mVar.type;
				//			flag = true;
				//		}
				//	}
				//	if(!flag) {
				//		variables.RemoveAt(i);
				//		i--;
				//	}
				//}
			}
		}

		public override System.Type GetNodeIcon() {
			if(macroAsset != null) {
				return macroAsset.GetIcon();
			}
			return typeof(TypeIcons.StateIcon);
		}

		public override string GetTitle() {
			if(macroAsset != null) {
				return macroAsset.GetGraphName();
			}
			return name;
		}

		public bool IsSelfCoroutine() {
			if(macroAsset != null) {
				return macroAsset.HasCoroutineNode;
			}
			return false;
		}

		void IGeneratorPrePostInitializer.OnPreInitializer() {
			var runner = RuntimeGraphUtility.GetOrCreateGraphRunner(macroAsset, this);
			var graph = runner.GraphData;

			graph.ForeachInChildrens(element => {
				if(element is NodeObject nodeObject) {
					if(nodeObject.node is MacroPortNode) {
						CG.RegisterEntry(nodeObject);
					}
				}
			}, true);

			foreach(var port in inputFlows) {
				foreach(var con in port.connections) {
					if(con.isValid == false) continue;
					CG.RegisterEntry(con.output.node);
				}
			}
			foreach(var port in outputFlows) {
				foreach(var con in port.connections) {
					if(con.isValid == false) continue;
					CG.RegisterEntry(con.input.node);
				}
			}
			foreach(var port in inputValues) {
				foreach(var con in port.connections) {
					if(con.isValid == false) continue;
					CG.RegisterEntry(con.output.node);
				}
			}
			foreach(var port in outputValues) {
				foreach(var con in port.connections) {
					if(con.isValid == false) continue;
					CG.RegisterEntry(con.input.node);
				}
			}

			CG.RegisterNestedGraph(graph);
			CG.RegisterPostInitialization(() => {
				//Initialize Flow Inputs
				int index = 0;
				foreach(var p in RuntimeGraphUtility.GetMacroInputFlows(graph)) {
					var linkedPort = p;
					var port = inputFlows[index];
					CG.RegisterPort(port, () => CG.GeneratePort(linkedPort.exit));
					index++;
				}
				//Initialize Flow Outputs
				index = 0;
				foreach(var p in RuntimeGraphUtility.GetMacroOutputFlows(graph)) {
					var linkedPort = p;
					var port = outputFlows[index];
					CG.RegisterPort(linkedPort.enter, () => CG.GeneratePort(port));
					index++;
				}
				//Initialize Value Inputs
				index = 0;
				foreach(var p in RuntimeGraphUtility.GetMacroInputValues(graph)) {
					var linkedPort = p;
					var port = inputValues[index];
					CG.RegisterPort(linkedPort.output, () => CG.GeneratePort(port));
					index++;
				}
				//Initialize Value Outputs
				index = 0;
				foreach(var p in RuntimeGraphUtility.GetMacroOutputValues(graph)) {
					var linkedPort = p;
					var port = outputValues[index];
					CG.RegisterPort(port, () => CG.GeneratePort(linkedPort.input));
					index++;
				}
				foreach(var variable in graph.variableContainer.collections) {
					variable.modifier.SetPrivate();
					CG.RegisterVariable(variable);
				}
			});


			CG.RegisterPostGeneration(_ => {
				RuntimeGraphUtility.DestroyGraphRunner(runner);
			});
		}

		void IGeneratorPrePostInitializer.OnPostInitializer() { }
	}
}