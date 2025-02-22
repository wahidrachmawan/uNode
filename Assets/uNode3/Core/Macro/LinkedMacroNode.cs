using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace MaxyGames.UNode.Nodes {
	public class LinkedMacroNode : Node, ILinkedMacro, ILocalVariableSystem, IRefreshable, IGeneratorPrePostInitializer {
		[AllowAssetReference]
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
				bool needSetPrimary = true;
				foreach(var p in macroAsset.InputFlows) { 
					var macroPort = p;
					var port = FlowInput(macroPort.id.ToString(), flow => {
						var data = flow.GetElementData<LinkedData>(this);
						if(data == null) {
							//In case it is live editing.
							OnRuntimeInitialize(flow);
							data = flow.GetElementData<LinkedData>(this);
						}
						if(data.flowCallback.TryGetValue(macroPort.id, out var exit)) {
							flow.Next(exit);
						}
						else {
							throw new GraphException("Port not found", this);
						}
					});
					//port.actionOnExit = flow => {
					//	if(flow is StateFlow) {
					//		var data = flow.GetElementData<LinkedData>(this);
					//		if(data == null) {
					//			//In case it is live editing.
					//			data = flow.GetElementData<LinkedData>(this);
					//		}
					//		if(data.flowCallback.TryGetValue(macroPort.id, out var exit)) {
					//			flow.state = exit.GetCurrentState(flow);
					//		}
					//	}
					//};
					inputFlows.Add(port.SetName(macroPort.GetTitle()));
					if(needSetPrimary) {
						needSetPrimary = false;
						nodeObject.primaryFlowInput = inputFlows[0];
					}
				}
				needSetPrimary = true;
				//Initialize Flow Outputs
				foreach(var p in macroAsset.OutputFlows) {
					var macroPort = p;
					outputFlows.Add(
						FlowOutput(macroPort.id.ToString()).SetName(macroPort.GetTitle())
					);
					if(needSetPrimary) {
						needSetPrimary = false;
						nodeObject.primaryFlowOutput = outputFlows[0];
					}
				}
				//Initialize Value Inputs
				foreach(var p in macroAsset.InputValues) {
					var macroPort = p;
					inputValues.Add(
						ValueInput(macroPort.id.ToString(), macroPort.ReturnType()).SetName(macroPort.GetTitle())
					);
				}
				needSetPrimary = true;
				//Initialize Value Outputs
				foreach(var p in macroAsset.OutputValues) {
					var macroPort = p;
					var port = ValueOutput(macroPort.id.ToString(), macroPort.ReturnType(), PortAccessibility.ReadWrite).SetName(macroPort.GetTitle());
					port.AssignGetCallback(instance => {
						var data = instance.GetElementData<LinkedData>(this);
						if(data == null) {
							//In case it is live editing.
							OnRuntimeInitialize(instance);
							data = instance.GetElementData<LinkedData>(this);
						}
						if(data.valueCallback.TryGetValue(macroPort.id, out var input)) {
							return input.GetValue(instance);
						}
						else {
							throw new GraphException("Port not found", this);
						}
					});
					port.AssignSetCallback((instance, value) => {
						var data = instance.GetElementData<LinkedData>(this);
						if(data == null) {
							//In case it is live editing.
							OnRuntimeInitialize(instance);
							data = instance.GetElementData<LinkedData>(this);
						}
						if(data.valueCallback.TryGetValue(macroPort.id, out var input)) {
							input.SetValue(instance, value);
						}
						else {
							throw new GraphException("Port not found", this);
						}
					});
					outputValues.Add(port);
					if(needSetPrimary) {
						needSetPrimary = false;
						nodeObject.primaryValueOutput = outputValues[0];
					}
				}
			}
			else {
				nodeObject.RestorePreviousPort();
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

			graph.SetAsLinkedGraph(this);
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

		public IMacroGraph LinkedMacro => macroAsset;

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
			if(macroAsset == null)
				throw new Exception("Macro asset is not assigned");
			var runner = RuntimeGraphUtility.GetOrCreateGraphRunner(macroAsset, this);
			var graph = runner.GraphData;

			graph.SetAsLinkedGraph(this);
			graph.ForeachInChildrens(element => {
				if(element is NodeObject nodeObject) {
					if(nodeObject.node is MacroPortNode) {
						CG.RegisterEntry(nodeObject);
					}
				}
			}, true);

			foreach(var port in inputFlows) {
				CG.RegisterAsRegularNode(port);
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
					CG.RegisterPort(port, () => CG.Flow(linkedPort.exit));
					//if(linkedPort.exit != null && linkedPort.exit.isAssigned) {
					//	if(linkedPort.exit.IsSelfCoroutine() && CG.IsStateFlow(linkedPort.exit.GetTargetFlow())) {
					//		//CG.RegisterAsStateFlow(port);
					//		//CG.RegisterAsStateFlow(linkedPort.exit.GetTargetFlow());
					//		//CG.SetStateInitialization(port, () => {
					//		//	var target = linkedPort.exit.GetTargetFlow();
					//		//	if(target == null)
					//		//		return null;
					//		//	return CG.GetEvent(linkedPort.exit);
					//		//});
					//	}
					//}
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