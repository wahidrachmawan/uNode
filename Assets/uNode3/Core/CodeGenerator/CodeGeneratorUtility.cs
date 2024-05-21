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
		/// <summary>
		/// Begin a new block statement ( use this for generating lambda block )
		/// </summary>
		/// <param name="allowYield"></param>
		public static void BeginBlock(bool allowYield = false) {
			generatorData.blockStacks.Add(new BlockStack() {
				allowYield = allowYield
			});
		}

		/// <summary>
		/// End the previous block statment
		/// </summary>
		public static void EndBlock() {
			if(generatorData.blockStacks.Count > 0) {
				generatorData.blockStacks.RemoveAt(generatorData.blockStacks.Count - 1);
			}
		}

		public class Utility {
			public static bool IsEvent(ValueInput port) {
				if(port.UseDefaultValue) {
					return IsEvent(port.defaultValue);
				}
				else if(port.isAssigned) {
					var tNode = port.GetTargetNode();
					if(tNode != null && tNode.node is MultipurposeNode mNode) {
						return IsEvent(mNode.target);
					}
				}
				return false;
			}

			public static bool IsEvent(MemberData member) {
				switch(member.targetType) {
					case MemberData.TargetType.uNodeVariable:
					case MemberData.TargetType.uNodeLocalVariable:
						var variable = member.startItem.GetReferenceValue() as Variable;
						if(variable != null) {
							return variable.modifier.Event;
						}
						break;
					case MemberData.TargetType.Event:
						return true;
					case MemberData.TargetType.NodePort:
						var port = member.startItem.GetReferenceValue() as UPort;
						if(port is ValueInput) {
							return IsEvent(port as ValueInput);
						}
						break;
				}
				return false;
			}

			public static bool IsContainOperatorCode(string value) {
				if(value.Contains("op_")) {
					switch(value) {
						case "op_Addition":
						case "op_Subtraction":
						case "op_Division":
						case "op_Multiply":
						case "op_Modulus":
						case "op_Equality":
						case "op_Inequality":
						case "op_LessThan":
						case "op_GreaterThan":
						case "op_LessThanOrEqual":
						case "op_GreaterThanOrEqual":
						case "op_BitwiseAnd":
						case "op_BitwiseOr":
						case "op_LeftShift":
						case "op_RightShift":
						case "op_ExclusiveOr":
						case "op_UnaryNegation":
						case "op_UnaryPlus":
						case "op_LogicalNot":
						case "op_OnesComplement":
						case "op_Increment":
						case "op_Decrement":
							return true;
					}
				}
				return false;
			}
		}

		#region Grouped
		/// <summary>
		/// Register node as grouped node.
		/// </summary>
		/// <param name="node"></param>
		public static void RegisterAsRegularNode(FlowInput node) {
			if(generationState.state != State.Classes)
				throw new InvalidOperationException("The Register action must be performed on Initialization / " + nameof(NodeObject.OnGeneratorInitialize));
			if(!generatorData.regularNodes.Contains(node)) {
				generatorData.regularNodes.Add(node);
				generatorData.stateNodes.Remove(node);
			}
		}

		/// <summary>
		/// Register value output port
		/// </summary>
		/// <param name="port"></param>
		/// <param name="generator"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public static void RegisterPort(ValueOutput port, Func<string> generator) {
			if (generationState.state != State.Classes)
				throw new InvalidOperationException("The Register action must be performed on Initialization / " + nameof(NodeObject.OnGeneratorInitialize));
			if (port is null) {
				//Skip when port is null.
				return;
			}
			if (generator is null) {
				throw new ArgumentNullException(nameof(generator));
			}
			generatorData.generatorForPorts[port] = generator;
		}

		/// <summary>
		/// Register value input port, normally value input doesn't need to register.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="generator"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public static void RegisterPort(ValueInput port, Func<string> generator) {
			if(generationState.state != State.Classes)
				throw new InvalidOperationException("The Register action must be performed on Initialization / " + nameof(NodeObject.OnGeneratorInitialize));
			if(port is null) {
				//Skip when port is null.
				return;
			}
			if(generator is null) {
				throw new ArgumentNullException(nameof(generator));
			}
			generatorData.generatorForPorts[port] = generator;
		}

		/// <summary>
		/// Register flow input port
		/// </summary>
		/// <param name="port"></param>
		/// <param name="generator"></param>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		public static void RegisterPort(FlowInput port, Func<string> generator) {
			if (generationState.state != State.Classes)
				throw new InvalidOperationException("The Register action must be performed on Initialization / " + nameof(NodeObject.OnGeneratorInitialize));
			if (port is null) {
				//Skip when port is null.
				return;
			}
			if (generator is null) {
				throw new ArgumentNullException(nameof(generator));
			}
			generatorData.generatorForPorts[port] = generator;
		}

		/// <summary>
		/// Register flow port as coroutine state ( non-coroutine state port doesn't need this ).
		/// </summary>
		/// <param name="flow"></param>
		public static void RegisterAsStateFlow(FlowInput flow) {
			if(generationState.state != State.Classes)
				throw new InvalidOperationException("The Register action must be performed on Initialization / " + nameof(NodeObject.OnGeneratorInitialize));
			if(flow == null)
				return;
			generatorData.stateNodes.Add(flow);
			generatorData.regularNodes.Remove(flow);
		}

		/// <summary>
		/// Register flow port as coroutine state ( non-coroutine state port doesn't need this ).
		/// </summary>
		/// <param name="members"></param>
		public static void RegisterAsStateFlow(IEnumerable<FlowInput> flows) {
			if(flows == null)
				return;
			if(generationState.state != State.Classes)
				throw new InvalidOperationException("The Register action must be performed on Initialization / " + nameof(NodeObject.OnGeneratorInitialize));
			foreach(var flow in flows) {
				RegisterAsStateFlow(flow);
			}
		}

		/// <summary>
		/// Register flow port as coroutine state ( non-coroutine state port doesn't need this ).
		/// </summary>
		/// <param name="flow"></param>
		public static void RegisterAsStateFlow(FlowOutput flow) {
			if(generationState.state != State.Classes)
				throw new InvalidOperationException("The Register action must be performed on Initialization / " + nameof(NodeObject.OnGeneratorInitialize));
			if(flow == null || !flow.isAssigned)
				return;
			RegisterAsStateFlow(flow.GetTargetFlow());
		}

		/// <summary>
		/// Register flow port as coroutine state ( non-coroutine state port doesn't need this ).
		/// </summary>
		/// <param name="flows"></param>
		public static void RegisterAsStateFlow(IEnumerable<FlowOutput> flows) {
			if(generationState.state != State.Classes)
				throw new InvalidOperationException("The Register action must be performed on Initialization / " + nameof(NodeObject.OnGeneratorInitialize));
			if(flows == null)
				return;
			foreach(var flow in flows) {
				if(flow != null && flow.isAssigned) {
					RegisterAsStateFlow(flow.GetTargetFlow());
				}
			}
		}

		/// <summary>
		/// Is the node is regular node?
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool IsRegularFlow(FlowInput node) {
			return generatorData.regularNodes.Contains(node);
		}

		/// <summary>
		/// Is the node is state node?
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool IsStateFlow(FlowInput port) {
			if (port == null)
				return false;
			return generatorData.stateNodes.Contains(port);
		}

		/// <summary>
		/// Is the node is in state graph?
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool IsInStateGraph(NodeObject node) {
			return node.GetObjectInParent<MainGraphContainer>() != null;
		}
		#endregion

		#region Variables
		public static bool CanDeclareLocal(ValueOutput outputPort, params FlowOutput[] flows) {
			return CanDeclareLocal(outputPort, flows as IEnumerable<FlowOutput>);
		}

		/// <summary>
		///  if variable from value output port can be declared as local variable.
		/// </summary>
		/// <param name="outputPort"></param>
		/// <param name="flows"></param>
		/// <returns></returns>
		public static bool CanDeclareLocal(ValueOutput outputPort, IEnumerable<FlowOutput> flows) {
			if (outputPort == null)
				throw new ArgumentNullException(nameof(outputPort));
			var from = outputPort.node;
			if(from != null && flows != null && !IsStateFlow(from.primaryFlowInput)) {
				var allFlows = new HashSet<NodeObject>();
				allFlows.Add(from);
				foreach(var flow in flows) {
					if(flow == null)
						continue;
					Nodes.FindAllConnections(flow.GetTargetNode(), ref allFlows);
				}
				var allConnectedNodes = new HashSet<NodeObject>();
				foreach(var con in outputPort.connections) {
					if(con.isValid) {
						allConnectedNodes.Add(con.input.node);
						//Nodes.FindAllConnections(con.input.node, ref allConnectedNodes, false, false, false, true);
						
					}
				}
				foreach(var node in allConnectedNodes) {
					if(!allFlows.Contains(node)) {
						return false;
					}
					else {
						foreach(var flow in node.FlowInputs) {
							if(IsStateFlow(flow)) {
								return false;
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// if variable from value output port can be declared as local variable.
		/// </summary>
		/// <param name="outputPort"></param>
		/// <param name="ports"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static bool CanDeclareLocal(ValueOutput outputPort, IEnumerable<ValueInput> ports) {
			if(outputPort == null)
				throw new ArgumentNullException(nameof(outputPort));
			var from = outputPort.node;
			if(from != null && ports != null && !IsStateFlow(from.primaryFlowInput)) {
				var allFlows = new HashSet<NodeObject>();
				allFlows.Add(from);
				foreach(var flow in ports) {
					if(flow == null)
						continue;
					Nodes.FindAllConnections(flow.GetTargetNode(), ref allFlows);
				}
				var allConnectedNodes = new HashSet<NodeObject>();
				foreach(var con in outputPort.connections) {
					if(con.isValid) {
						allConnectedNodes.Add(con.input.node);
					}
				}
				foreach(var node in allConnectedNodes) {
					if(!allFlows.Contains(node)) {
						return false;
					}
					else {
						foreach(var flow in node.FlowInputs) {
							if(IsStateFlow(flow)) {
								return false;
							}
						}
					}
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// True if variable from value output port can be declared as local variable.
		/// </summary>
		/// <param name="groupNode"></param>
		/// <returns></returns>
		public static bool CanDeclareLocal(ISuperNode superNode) {
			if(superNode != null) {
				var flows = new HashSet<NodeObject>();
				foreach(var n in superNode.nestedFlowNodes) {
					Nodes.FindFlowConnectionAfterCoroutineNode(n, ref flows);
				}
				bool Validate(object obj) {
					if (obj == null) return false;
					if(obj is MemberData member) {
						if(member.isAssigned) {
							if(member.targetType == MemberData.TargetType.uNodeVariable || member.targetType == MemberData.TargetType.uNodeLocalVariable) {
								var varRef = member.startItem.GetReferenceValue() as Variable;
								if(varRef != null && varRef.IsChildOf(superNode as Node)) {
									return true;
								}
							}
							if(!member.isStatic) {
								return Validate(member.instance);
							}
						}
					}
					return false;
				}
				foreach(var node in flows) {
					if(node == null)
						continue;
					foreach (var r in node.serializedData.references) {
						if (r is MemberData member) {
							if(Validate(member)) {
								return true;
							}
						}
					}
				}
				var allConnection = new HashSet<NodeObject>();
				foreach(var n in superNode.nestedFlowNodes) {
					Nodes.FindAllFlowConnection(n, ref allConnection);
				}
				foreach(var node in allConnection) {
					if(node == null)
						continue;
					foreach(var r in node.serializedData.references) {
						if(r is MemberData member) {
							if (Validate(member)) {
								if(node.FlowInputs.Where(p => p.isConnected).SelectMany(p => p.GetConnectedPorts()).Any(p => !flows.Contains(p.node))) {
									return true;
								}
								if (node.primaryFlowInput != null && generatorData.stateNodes.Contains(node.primaryFlowInput)) {
									return true;
								}
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// True if the variable is instanced variable or are declared within the class.
		/// </summary>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static bool IsInstanceVariable(object reference) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(object.ReferenceEquals(vdata.reference, reference)) {
					return vdata.isInstance;
				}
			}
			//throw new Exception("The variable is not registered.");
			return false;
		}

		/// <summary>
		/// True if the variable is local variable or are not declared within the class.
		/// </summary>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static bool IsLocalVariable(object reference) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(vdata.reference == reference) {
					return !vdata.isInstance;
				}
			}
			//throw new Exception("The variable is not registered.");
			return false;
		}
		#endregion

		#region GetAllNode
		/// <summary>
		/// Get all nodes.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<NodeObject> GetAllNode() {
			return generatorData.allNode;
		}

		/// <summary>
		/// Get all nodes in child of parent.
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		public static HashSet<NodeObject> GetAllNode(UGraphElement parent) {
			HashSet<NodeObject> nodes;
			if(generatorData.nodesMap.TryGetValue(parent, out nodes)) {
				return nodes;
			}
			nodes = new HashSet<NodeObject>();
			foreach(var node in GetAllNode()) {
				if(node == null)
					continue;
				if(node.parent == parent) {
					nodes.Add(node);
				}
			}
			generatorData.nodesMap[parent] = nodes;
			return nodes;
		}
		#endregion

		#region ActionData
		[System.NonSerialized]
		private static int coNum = 0;

		private static CoroutineData GetOrRegisterCoroutineEvent(object owner) {
			CoroutineData data;
			if(!generatorData.coroutineEvent.TryGetValue(owner, out data)) {
				data = new CoroutineData();
				data.variableName = "coroutine" + (++coNum).ToString();
				generatorData.coroutineEvent[owner] = data;
			}
			return data;
		}

		private static CoroutineData GetCoroutineEvent(object owner) {
			generatorData.coroutineEvent.TryGetValue(owner, out var data);
			return data;
		}

		/// <summary>
		/// Set state node stop action
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="contents"></param>
		public static void SetStateStopAction(FlowInput owner, string contents) {
			var data = GetOrRegisterCoroutineEvent(owner);
			data.onStop = contents;
		}

		/// <summary>
		/// Set state node action
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="contents"></param>
		public static void SetStateAction(object owner, string contents) {
			var data = GetOrRegisterCoroutineEvent(owner);
			data.contents = contents;
		}

		/// <summary>
		/// Set custom state node initialization / custom action
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="contents"></param>
		public static void SetStateInitialization(FlowInput owner, string contents) {
			var data = GetOrRegisterCoroutineEvent(owner);
			data.customExecution = () => contents;
		}

		/// <summary>
		/// Set custom state node initialization / custom action
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="contents"></param>
		public static void SetStateInitialization(FlowInput owner, Func<string> contents) {
			var data = GetOrRegisterCoroutineEvent(owner);
			data.customExecution = contents;
		}

		/// <summary>
		/// Set custom state node initialization / custom action
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="contents"></param>
		public static void SetStateInitialization(BaseGraphEvent owner, Func<string> contents) {
			var data = GetOrRegisterCoroutineEvent(owner);
			data.customExecution = contents;
		}

		/// <summary>
		/// Set custom state node initialization / custom action
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="contents"></param>
		public static void SetStateInitialization(BaseGraphEvent owner, string contents) {
			var data = GetOrRegisterCoroutineEvent(owner);
			data.customExecution = () => contents;
		}

		/// <summary>
		/// Register a Coroutine Event
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="generator"></param>
		/// <returns></returns>
		public static string RegisterCoroutineEvent(object obj, Func<string> generator, bool customExecution = false) {
			if(CG.generatorData.coroutineEvent.ContainsKey(obj)) {
				return CG.RunEvent(obj);
			} else {
				if(customExecution) {
					var data = GetOrRegisterCoroutineEvent(obj);
					data.customExecution = generator;
				} else {
					CG.SetStateAction(obj, generator());
				}
				return CG.RunEvent(obj);
			}
		}

		/// <summary>
		/// Get the variable name of coroutine event
		/// Note: It will create a new if doesn't exist.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static string GetCoroutineName(object target) {
			if(target != null) {
				return WrapWithInformation(GetOrRegisterCoroutineEvent(target).variableName, target);
			}
			return null;
		}
		#endregion

		#region Others
		/// <summary>
		/// Return true on flow body can be simplify to lambda expression code.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="returnType"></param>
		/// <param name="parameterTypes"></param>
		/// <returns></returns>
		public static bool CanSimplifyToLambda(FlowOutput body, Type returnType, IList<ValueOutput> parameterTypes) {
			if (!body.isAssigned)
				return false;
			var bodyNode = body.GetTargetFlow().node;
			if (bodyNode.node is MultipurposeNode) {
				var node = bodyNode.node as MultipurposeNode;
				if (node.target.isAssigned && !node.exit.isAssigned && node.parameters != null) {
					if (parameterTypes.Count == node.parameters.Count) {
						bool flag = true;
						int x = 0;
						foreach (var p in node.parameters) {
							if (p.output != null || !p.input.isConnected || p.input.GetTargetPort() != parameterTypes[x] || p.input.type != parameterTypes[x].type) {
								flag = false;
								break;
							}
							x++;
						}
						if (flag) {
							if(generatePureScript == false) {
								var members = node.target.GetMembers();
								if(members != null) {
									//Return true if the last member is native member
									return ReflectionUtils.IsNativeMember(members[members.Length - 1]);
								}
							}
							return true;
						}
					}
				}
			}
			else if (bodyNode.node is UNode.Nodes.NodeReturn) {
				var rNode = bodyNode.node as UNode.Nodes.NodeReturn;
				if (rNode.value.isConnected) {
					if (parameterTypes.Count == 1 && parameterTypes[0] == rNode.value.GetTargetPort()) {
						return true;
					}
				}
			}
			return false;
		}


		/// <summary>
		/// Return true on flow body can be simplify to lambda expression code.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="returnType"></param>
		/// <param name="parameterTypes"></param>
		/// <returns></returns>
		public static bool CanSimplifyToLambda(ValueInput input, Type returnType, IList<ValueOutput> parameterTypes) {
			if(!input.isAssigned)
				return false;
			var bodyNode = input.GetTargetNode();
			if(bodyNode != null && bodyNode.node is MultipurposeNode) {
				var node = bodyNode.node as MultipurposeNode;
				if(node.target.isAssigned && (node.exit == null || !node.exit.isAssigned) && node.parameters != null) {
					if(parameterTypes.Count == node.parameters.Count) {
						bool flag = true;
						int x = 0;
						foreach(var p in node.parameters) {
							if(p.output != null || !p.input.isConnected || p.input.GetTargetPort() != parameterTypes[x]) {
								flag = false;
								break;
							}
							x++;
						}
						if(flag) {
							if(generatePureScript == false) {
								var members = node.target.GetMembers();
								if(members != null) {
									//Return true if the last member is native member
									return ReflectionUtils.IsNativeMember(members[members.Length - 1]);
								}
							}
							return true;
						}
					}
				}
			}
			return false;
		}
		#endregion
	}
}