using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using System.Collections;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors.Analyzer {
	class DefaultGraphAnalyzer : GraphAnalyzer {
		public override int order => int.MinValue;

		public override bool IsValidAnalyzerForGraph(Type graphType) => true;

		public override bool IsValidAnalyzerForNode(Type nodeType) => true;

		public override bool IsValidAnalyzerForElement(Type elementType) => true;

		public override void CheckGraphErrors(ErrorAnalyzer analyzer, IGraph graph) {
			var graphData = graph.GraphData;
			if(graphData == null) return;
			if(graph is IUsingNamespace) {
				var namespaces = EditorReflectionUtility.GetNamespaces();
				foreach(var ns in graph.GetUsingNamespaces()) {
					if(!namespaces.Contains(ns)) {
						if(graph is INamespace nsGraph && nsGraph.Namespace != ns) {
							analyzer.RegisterError(graphData, $@"Using Namespace: '{ns}' was not found.");
						}
					}
				}
			}
			else if(graph is IScriptGraphType scriptGraphType) {
				var namespaces = EditorReflectionUtility.GetNamespaces();
				foreach(var ns in graph.GetUsingNamespaces()) {
					if(!namespaces.Contains(ns)) {
						if(scriptGraphType.ScriptTypeData.scriptGraph.Namespace != ns) {
							analyzer.RegisterError(graphData, $@"Using Namespace: '{ns}' was not found.");
						}
					}
				}
			}
			var inheritType = graph.GetGraphInheritType();
			if(graph is IClassModifier) {
				var modifier = (graph as IClassModifier).GetModifier();
				if(modifier.ReadOnly) {
					if(graph is IClassGraph classGraph) {
						if(classGraph.IsStruct == false) {
							analyzer.RegisterError(graphData, $@"Readonly modifier is only supported for struct.");
						}
						else {
							foreach(var variable in classGraph.GetVariables()) {
								if(variable.modifier.ReadOnly == false) {
									analyzer.RegisterError(variable, $@"Variable from readonly struct must be readonly.");
								}
							}
						}
					}

				}
				if(modifier.Static) {
					//Analize for static graph
					if(graph is IClassGraph classGraph) {
						if(classGraph.InheritType != null && classGraph.InheritType != typeof(object)) {
							analyzer.RegisterError(graphData, "Static modifier cannot use inheritance or must inherit from System.Object");
						}
					}
					if(graph is IGraphWithVariables) {
						var variables = graph.GetVariables();
						foreach(var element in variables) {
							if(element.modifier.Static == false) {
								analyzer.RegisterError(element, "Static graph cannot have instance variables");
							}
						}
					}
					if(graph is IGraphWithProperties) {
						var functions = graph.GetProperties();
						foreach(var element in functions) {
							if(element.modifier.Static == false) {
								analyzer.RegisterError(element, "Static graph cannot have instance properties");
							}
						}
					}
					if(graph is IGraphWithFunctions) {
						var functions = graph.GetFunctions();
						foreach(var element in functions) {
							if(element.modifier.Static == false) {
								analyzer.RegisterError(element, "Static graph cannot have instance functions");
							}
						}
					}
					if(graph is IGraphWithConstructors) {
						var ctors = graph.GetConstructors();
						foreach(var element in ctors) {
							if(element.modifier.Static == false) {
								analyzer.RegisterError(element, "Static graph cannot have instance constructors");
							}
							if(element.parameters.Count > 0) {
								analyzer.RegisterError(element, "Static constructor cannot have parameters");
							}
						}
					}
				}
			}
			if(inheritType != null && graph is IGraphWithFunctions) {
				var functions = graph.GetFunctions();
				foreach(var function in functions) {
					var returnType = function.ReturnType();
					if(returnType != typeof(void)) {
						bool isCoroutine = returnType.IsCastableTo(typeof(IEnumerable)) || returnType.IsCastableTo(typeof(IEnumerator));
						bool hasReturn = false;

						var nodes = CG.Nodes.FindAllConnections(function.Entry, includeFlowOutput: true);
						foreach(var node in nodes) {
							if(node.node is NodeReturn) {
								hasReturn = true;
								break;
							}
							else if(isCoroutine) {
								foreach(var port in node.FlowInputs) {
									if(port.IsCoroutine()) {
										hasReturn = true;
										break;
									}
								}
								if(hasReturn) break;
							}
						}
						if(hasReturn == false) {
							if(isCoroutine) {
								analyzer.RegisterError(function.Entry, $@"Function: {function.name} need to have return or yield return node.");
							}
							else {
								analyzer.RegisterError(function.Entry, $@"Function: {function.name} need to have return node.");
							}
						}
					}
					if(function.modifier.Static) {
						//Analizer for static function
						if(function.attributes.Any(a => a.attributeType == typeof(System.Runtime.CompilerServices.ExtensionAttribute))) {
							if(function.parameters.Count == 0) {
								analyzer.RegisterError(function, $@"Function: {function.name} need at least one parameter to mark as extension.");
							}
						}
					}
					else {
						var baseMember = inheritType.GetMethod(function.name, MemberData.flags, null, function.ParameterTypes, null);
						if(function.modifier.Override) {
							if(baseMember == null) {
								analyzer.RegisterError(function, $@"Function: {function.name} has no suitable function found to override.");
							}
							else if(baseMember.IsVirtual == false) {
								analyzer.RegisterError(function, $@"Function: {function.name} is unable to overriden because the base function is not virtual/abstract.");
							}
						}
						else {
							if(baseMember != null && baseMember.IsVirtual) {
								analyzer.RegisterError(function, $@"Function: {function.name} is virtual/overridable but this function is not use override modifier this lead to incorrect behavior please use override modifier or rename this function.", () => {
									function.modifier.SetOverride();
								});
							}
						}
					}
				}
			}
			if(inheritType != null && graph is IGraphWithProperties) {
				var properties = graph.GetProperties();
				foreach(var property in properties) {
					if(property.modifier.Override) {
						var member = inheritType.GetProperty(property.name, MemberData.flags);
						if(member == null) {
							analyzer.RegisterError(property, $@"Property: {property.name} has no suitable property found to override.");
						}
						else {
							//TODO: check for property error because of override
						}
					}
				}
			}
			if(graph is IInterfaceSystem) {
				var ifaceSystem = graph as IInterfaceSystem;
				var ifaces = ifaceSystem.Interfaces;
				if(ifaces != null) {
					foreach(var iface in ifaces) {
						if(!iface.isAssigned)
							continue;
						var type = iface.type;
						if(type == null)
							continue;
						var methods = type.GetMethods();
						for(int i=0;i< methods.Length;i++) {
							var member = methods[i];
							if(member.Name.StartsWith("get_", StringComparison.Ordinal) || member.Name.StartsWith("set_", StringComparison.Ordinal)) {
								continue;
							}
							if(!graph.GetFunction(
								member.Name,
								member.GetGenericArguments().Length,
								member.GetParameters().Select(item => item.ParameterType).ToArray())) {
								analyzer.RegisterError(graphData, 
									$@"The graph does not implement interface method: '{type.PrettyName()}' type: '{EditorReflectionUtility.GetPrettyMethodName(member)}'",
									() => {
										uNodeEditorUtility.RegisterUndo(graph);
										NodeEditorUtility.AddNewFunction(graph.GraphData.functionContainer, member.Name, member.ReturnType,
										member.GetParameters().Select(item => new ParameterData(item)).ToArray(),
										member.GetGenericArguments().Select(item => item.Name).ToArray());
										uNodeGUIUtility.GUIChanged(graph, UIChangeType.Important);
									});
							}
						}
						var properties = type.GetProperties();
						for(int i=0;i< properties.Length;i++) {
							var member = properties[i];
							if(!graph.GetProperty(member.Name)) {
								analyzer.RegisterError(graphData,
									$@"The graph does not implement interface property: '{type.PrettyName()}' type: '{member.PropertyType.PrettyName()}'",
									() => {
										uNodeEditorUtility.RegisterUndo(graph);
										NodeEditorUtility.AddNewProperty(graph.GraphData.propertyContainer, member.Name, member.PropertyType);
										uNodeGUIUtility.GUIChanged(graph, UIChangeType.Important);
									});
							}
						}
					}
				}
			}
		}

		public override void CheckElementErrors(ErrorAnalyzer analizer, UGraphElement element) {
			if(element is IErrorCheck errorCheck) {
				try {
					errorCheck.CheckError(analizer);
				}
				catch(Exception exception) {
					analizer.RegisterError(element, exception.Message);
				}
			}
			if(element is NodeContainer container) {
				var nodes = container.GetObjectsInChildren<NodeObject>(true);
				foreach(var node in nodes) {
					if(node.FlowInputs.Any(item => item.isConnected) && node.FlowOutputs.Any(item => item.isConnected)) {
						if(CG.Nodes.IsStackOverflow(node)) {
							analizer.RegisterError(node, "This node has circular reference flows which is not valid in current context");
						}
					}
					if(node.ValueInputs.Any(item => item.isConnected) && node.ValueInputs.Any(item => item.isConnected)) {
						if(IsValueRecusive(node)) {
							analizer.RegisterError(node, "This node has circular reference values which will cause stack overflow");
						}
					}
				}
			}
		}

		private static bool IsValueRecusive(NodeObject original, NodeObject current = null, HashSet<NodeObject> prevs = null) {
			if(current == null)
				current = original;
			if(prevs == null)
				prevs = new HashSet<NodeObject>();
			if(prevs.Contains(current)) {
				return false;
			}
			prevs.Add(current);
			for(int i = 0; i < current.ValueInputs.Count; i++) {
				if(current.ValueInputs[i].isConnected) {
					var tPort = current.ValueInputs[i].GetTargetPort();
					if(tPort.isVariable == false) {
						var tNode = tPort.node;
						if(tNode == original || IsValueRecusive(original, tNode, prevs)) {
							return true;
						}
					}
				}
			}
			return false;
		}

		//private static bool IsRecusive(NodeObject original, bool skipCoroutine, NodeObject current = null, HashSet<NodeObject> prevs = null) {
		//	if(current == null)
		//		current = original;
		//	if(prevs == null)
		//		prevs = new HashSet<NodeObject>();
		//	if(prevs.Contains(current)) {
		//		return false;
		//	}
		//	prevs.Add(current);
		//	for(int i = 0; i < current.FlowOutputs.Count; i++) {
		//		if(current.FlowOutputs[i].isConnected) {
		//			if(skipCoroutine && current.FlowOutputs[i].IsSelfCoroutine() == true) {
		//				continue;
		//			}
		//			var flow = current.FlowOutputs[i].GetTargetNode();
		//			if(flow == original || IsRecusive(original, skipCoroutine, flow, prevs)) {
		//				return true;
		//			}
		//		}
		//	}
		//	return false;
		//}

		public override void CheckNodeErrors(ErrorAnalyzer analizer, Node node) {
			node.CheckError(analizer);
		}
	}
}