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
		public const string KEY_INFORMATION_HEAD = "@";
		public const string KEY_INFORMATION_TAIL = "#";
		public const string KEY_INFORMATION_REFERENCE = "REF:";

		private static string GetDebugOwner() {
			if(CG.generationState.isStatic || graph is IClassGraph classGraph && classGraph.InheritType == typeof(ValueType)) {
				//In case it is a struct then use a type name instead because a struct is a value copy.
				return $"typeof({generatorData.typeName.Split('.').Last()})";
			}
			return This;
		}

		/// <summary>
		/// Wrap 'input' string with information of 'obj' so uNode can suggest what is the object that generates the code.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static string WrapWithInformation(string input, object obj) {
			if(!string.IsNullOrWhiteSpace(input)) {
				int firstIndex = 0;
				int lastIndex = input.Length;
				for (int i = 0; i < input.Length;i++) {
					if(!char.IsWhiteSpace(input[i])) {
						firstIndex = i;
						break;
					}
				}
				for (int i = input.Length - 1; i > 0; i--) {
					if(!char.IsWhiteSpace(input[i])) {
						lastIndex = i + 1;
						break;
					}
				}
				return input.Add(lastIndex, EndGenerateInformation(obj)).Add(firstIndex, BeginGenerateInformation(obj));
			}
			return null;
			// return input.AddFirst(BeginGenerateInformation(obj)).Add(EndGenerateInformation(obj));
		}

		static string BeginGenerateInformation(object obj) {
			if(obj is UGraphElement) {
				return Comment((obj as UGraphElement).id.ToString().AddFirst(KEY_INFORMATION_HEAD));
			} 
			else if(obj is UnityEngine.Object) {
				return Comment((obj as UnityEngine.Object).GetHashCode().ToString().AddFirst(KEY_INFORMATION_HEAD + KEY_INFORMATION_REFERENCE));
			}
			else if(obj is Node) {
				return Comment((obj as Node).nodeObject.id.ToString().AddFirst(KEY_INFORMATION_HEAD));
			}
			else if(obj is UPort) {
				return Comment((obj as UPort).node.id.ToString().AddFirst(KEY_INFORMATION_HEAD));
			}
			return null;
		}

		static string EndGenerateInformation(object obj) {
			if(obj is UGraphElement) {
				return Comment((obj as UGraphElement).id.ToString().AddFirst(KEY_INFORMATION_TAIL));
			}
			else if (obj is UnityEngine.Object) {
				return Comment((obj as UnityEngine.Object).GetHashCode().ToString().AddFirst(KEY_INFORMATION_TAIL + KEY_INFORMATION_REFERENCE));
			}
			else if(obj is Node) {
				return Comment((obj as Node).nodeObject.id.ToString().AddFirst(KEY_INFORMATION_TAIL));
			}
			else if(obj is UPort) {
				return Comment((obj as UPort).node.id.ToString().AddFirst(KEY_INFORMATION_TAIL));
			}
			return null;
		}

		/// <summary>
		/// Generate debug code.
		/// </summary>
		/// <param name="comp"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public static string Debug(FlowInput port, StateType state) {
			if(IsStateFlow(port))
				//In case it is handled by state graph, return empty string instead since the debugging is handled automaticly
				return null;
			string data = setting.debugPreprocessor ? "\n#if UNITY_EDITOR" : "";
			string s = state == StateType.Success ? "true" : (state == StateType.Failure ? "false" : "null");
			if(port.isPrimaryPort) {
				data += FlowInvoke(typeof(GraphDebug), nameof(GraphDebug.FlowNode),
					GetDebugOwner(),
					Value(graph.GetGraphID()),
					Value(port.node.id),
					s).AddLineInFirst();
			} else {
				data += FlowInvoke(typeof(GraphDebug), nameof(GraphDebug.FlowNode),
					GetDebugOwner(),
					Value(graph.GetGraphID()),
					Value(port.node.id),
					Value(port.id),
					s).AddLineInFirst();
			}
			if(setting.debugPreprocessor)
				data += "#endif".AddLineInFirst();
			return data;
		}

		/// <summary>
		/// Generate debug code.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static string Debug(FlowOutput port) {
			string data = setting.debugPreprocessor ? "\n#if UNITY_EDITOR" : "";
			data += FlowInvoke(typeof(GraphDebug), nameof(GraphDebug.Flow),
				GetDebugOwner(),
				Value(graph.GetGraphID()),
				Value(port.node.id),
				Value(port.id)).AddLineInFirst();
			if(setting.debugPreprocessor)
				data += "#endif".AddLineInFirst();
			return data;
		}

		/// <summary>
		/// Generate debug code.
		/// </summary>
		/// <param name="member"></param>
		/// <param name="value"></param>
		/// <param name="isSet"></param>
		/// <returns></returns>
		public static string Debug(ValueInput port, string value, bool isSet = false) {
			if(value.Contains(Null)) {
				return GenericInvoke(port.ValueType ?? port.type, 
					typeof(GraphDebug), 
					nameof(GraphDebug.Value),
					value,
					GetDebugOwner(),
					Value(graph.GetGraphID()),
					Value(port.node.id),
					Value(port.id),
					Value(isSet));
			}
			var type = port.ValueType ?? port.type;
			if(port.CanSetValue() || type.IsByRef) {
				//Check when variable is struct and it is not readonly.
				if((port.IsVariable && type.IsValueType || type.IsByRef && type.ElementType().IsValueType) && !type.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false)) {
					var ports = StaticHashPool<UPort>.Allocate();

					static void FindAllSourcePort(UPort sourcePort, HashSet<UPort> ports) {
						if(sourcePort != null && ports.Add(sourcePort)) {
							if(sourcePort is ValueOutput valueOutput) {
								var node = valueOutput.node;
								foreach(var p in node.FlowInputs) {
									FindAllSourcePort(p, ports);
								}
								if(node.node is IRerouteNode reroute) {
									FindAllSourcePort(reroute.Input, ports);
								}
							}
							else if(sourcePort is ValueInput valueInput) {
								FindAllSourcePort(valueInput.GetTargetPort(), ports);
							}
							else if(sourcePort is FlowOutput flowOutput) {
								var node = flowOutput.node;
								foreach(var p in node.FlowInputs) {
									FindAllSourcePort(p, ports);
								}
							}
							else if(sourcePort is FlowInput flowInput) {
								foreach(var p in flowInput.GetConnectedPorts()) {
									FindAllSourcePort(p, ports);
								}
							}
						}
					}

					FindAllSourcePort(port, ports);
					foreach(var p in ports) {
						if(p is FlowOutput flowOutput) {
							if(flowOutput.localFunction) {
								StaticHashPool<UPort>.Free(ports);
								//When there's a local function, we skip the debug value as it may cause bug/error when value is mutable.
								return value;
							}
						}
					}
					StaticHashPool<UPort>.Free(ports);

					return Invoke(typeof(GraphDebug), nameof(GraphDebug.Value),
						"ref " + value,
						GetDebugOwner(),
						Value(graph.GetGraphID()),
						Value(port.node.id),
						Value(port.id),
						Value(isSet));
				}
			}
			return Invoke(typeof(GraphDebug), nameof(GraphDebug.Value),
				value,
				GetDebugOwner(),
				Value(graph.GetGraphID()),
				Value(port.node.id),
				Value(port.id),
				Value(isSet));
		}
	}
}