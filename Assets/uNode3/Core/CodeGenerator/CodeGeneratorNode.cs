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
		#region Generate Node
		public static string GeneratePort(ValueInput port, bool setVariable = false, bool autoConvert = false) {
			try {
				if(generatorData.generatorForPorts.TryGetValue(port, out var gen)) {
					if(debugScript && setting.debugValueNode) {
						var oldState = generationState.contextState;
						//Mark the current context state for set a value
						generationState.contextState = ContextState.Set;
						//Disable the debug script for fix error for compatibility generation mode
						setting.debugScript = false;

						var result = gen();

						//Restore the debug script
						setting.debugScript = true;
						//Restore the previous context state
						generationState.contextState = oldState;
						//Return the result
						return result;
					}
					else {
						var oldState = generationState.contextState;
						//Mark the current context state for set a value
						generationState.contextState = ContextState.Set;

						//Generate the ports
						var result = gen();

						//Restore the previous context state
						generationState.contextState = oldState;
						//Return the result
						return result;
					}
				}
				if(port.UseDefaultValue) {
					return Value(port.defaultValue, setVariable: setVariable, autoConvert: autoConvert);
				}
				else {
					if(!port.isConnected) {
						throw new GraphException($"Unassigned port: {port.name} on node: {port.node.GetTitle()}", port.node);
					}
					var tPort = port.GetTargetPort();
					if(setVariable) {
						if(tPort.node.node is MultipurposeNode multipurposeNode) {
							if(generatorData.generatorForPorts.TryGetValue(tPort, out var generator)) {
								if(debugScript && setting.debugValueNode) {
									var oldState = generationState.contextState;
									//Mark the current context state for set a value
									generationState.contextState = ContextState.Set;
									//Disable the debug script for fix error for compatibility generation mode
									setting.debugScript = false;

									var result = generator();

									//Restore the debug script
									setting.debugScript = true;
									//Restore the previous context state
									generationState.contextState = oldState;
									//Return the result
									return result;
								}
								else {
									return generator();
								}
							}
						}
						if(debugScript && setting.debugValueNode) {
							var oldState = generationState.contextState;
							//Mark the current context state for set a value
							generationState.contextState = ContextState.Set;
							//Disable the debug script for fix error for compatibility generation mode
							setting.debugScript = false;

							//Generate the ports
							var result = GeneratePort(tPort);

							//Restore the debug script
							setting.debugScript = true;
							//Restore the previous context state
							generationState.contextState = oldState;
							//Return the result
							return result;
						}
						else {
							var oldState = generationState.contextState;
							//Mark the current context state for set a value
							generationState.contextState = ContextState.Set;

							//Generate the ports
							var result = GeneratePort(tPort);

							//Restore the previous context state
							generationState.contextState = oldState;
							//Return the result
							return result;
						}
					}
					if(debugScript && setting.debugValueNode) {
						return Debug(port, GeneratePort(tPort), setVariable);

						//if (!generatorData.debugMemberMap.ContainsKey(port)) {
						//	generatorData.debugMemberMap.Add(port, new KeyValuePair<int, string>(generatorData.newDebugMapID,
						//		Debug(port, "debugValue").AddLineInFirst() +
						//		("return debugValue;").AddLineInFirst()
						//	));
						//}
						//return KEY_debugGetValueCode + "(" + generatorData.debugMemberMap[port].Key + ", " + GeneratePort(tPort) + ")";
					}
					return GeneratePort(tPort);
				}
			}
			catch(Exception ex) {
				if(ex is GraphException)
					throw;
				throw new GraphException(ex, port.node);
			}
		}

		public static string GeneratePort(FlowOutput port) {
			return CG.Flow(port);
		}

		public static string GeneratePort(FlowInput port) {
			if(port == null || !port.isValid)
				return null;
			if(!isInUngrouped && generatorData.generatedData.ContainsKey(port)) {
				return generatorData.generatedData[port];
			}
			if(generationState.state == State.Classes) {
				throw new Exception("Forbidden to generate port code because it's still in Initialization");
			}
			string data;
			try {
				if(!generatorData.generatorForPorts.TryGetValue(port, out var generator)) {
					throw new GraphException($"The node: {port.node.GetTitle()} with id: {port.node.id}, has unregistered port named: {port.name}", port.node);
				}
				data = generator();
				if(setting.fullComment && !string.IsNullOrEmpty(data)) {
					data = data.Insert(0, $"//node: {port.node.GetTitle()}| port: {port.name} | Type: {port.node.GetType()}".AddLineInEnd());
				}
				data = data.AddLineInFirst();
				if(port.isPrimaryPort && !string.IsNullOrEmpty(port.node.comment)) {//Generate Commentaries for nodes
					string[] str = port.node.comment.Split('\n');
					for(int i = str.Length - 1; i >= 0; i--) {
						data = data.AddFirst(str[i].AddFirst("//").AddLineInFirst());
					}
				}
				if(includeGraphInformation) {
					data = WrapWithInformation(data, port.node);
				}
			}
			catch(Exception ex) {
				var node = port.node;
				if(!generatorData.hasError) {
					if(setting != null && setting.isAsync) {
						generatorData.errors.Add(
							new GraphException(
								"Error from node:" + node.name + " |Type:" + node.node.GetType() +
								"\nFrom graph:" + node.graphContainer.GetGraphName(),
								ex, node));
						//In case async return error commentaries.
						return WrapWithInformation($"/*Error from node: {node.name} with id: {node.id} */", node);
					}
					UnityEngine.Debug.LogError(
						"Error from node:" + node.name + " |Type:" + node.node.GetType() +
						"\nRef: " + GraphException.GetMessage(node) +
						"\nFrom graph:" + node.graphContainer.GetGraphName() +
						"\nError:" + ex.ToString(), node.graphContainer as UnityEngine.Object);
				}
				generatorData.hasError = true;
				throw;
			}
			//if(string.IsNullOrEmpty(data)) {
			//	Debug.Log("Node not generated data", target);
			//}
			if(!isInUngrouped)
				generatorData.generatedData.Add(port, data);
			return data;
		}

		public static string GeneratePort(ValueOutput port) {
			if(port is null) {
				throw new ArgumentNullException(nameof(port));
			}
			if(!port.isValid)
				return null;
			if(generationState.state == State.Classes) {
				throw new Exception("Forbidden to generate port code because it's still in Initialization");
			}
			string data;
			try {
				if(!generatorData.generatorForPorts.TryGetValue(port, out var generator)) {
					throw new GraphException($"The node: {port.node.GetTitle()} with id: {port.node.id}, has unregistered port named: {port.name}", port.node);
				}
				data = generator();
				if(setting.debugScript && setting.debugValueNode) {
					if(typeof(Delegate).IsAssignableFrom(port.type)) {
						data = New(port.type, data);
					}
				}
				if(includeGraphInformation) {
					data = WrapWithInformation(data, port.node);
				}
			}
			catch(Exception ex) {
				var node = port.node;
				if(!generatorData.hasError) {
					if(setting != null && setting.isAsync) {
						generatorData.errors.Add(
							new GraphException(
								"Error from node:" + node.name + " |Type:" + node.node.GetType() +
								"\nFrom graph:" + node.graphContainer.GetGraphName(),
								ex, node));
						//In case async return error commentaries (See uNode Console).
						//uNodeDebug.LogError("Error from node:" + target.gameObject.name + " |Type:" + target.GetType(), target);
						return WrapWithInformation($"/*Error from node: {node.name} with id: {node.id} */", node);
					}
					UnityEngine.Debug.LogError(
						"Error from node:" + node.name + " |Type:" + node.node.GetType() +
						"\nRef: " + GraphException.GetMessage(node) +
						"\nFrom graph:" + node.graphContainer.GetGraphName() +
						"\nError:" + ex.ToString(), node.graphContainer as UnityEngine.Object);
				}
				generatorData.hasError = true;
				throw;
			}
			return data;
		}
		#endregion

		#region Node Functions
		/// <summary>
		/// Get state code from coroutine event
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static string GetNodeState(FlowInput target) {
			if(target == null)
				throw new ArgumentNullException(nameof(target));
			if(!generatorData.stateNodes.Contains(target)) {
				throw new GraphException($"Forbidden to generate state code because the node: {target.node.GetTitle()} is not registered as State Node.\nEnsure to register it using {nameof(CG)}.{nameof(CG.RegisterAsStateFlow)}", target.node);
			}
			return GetCoroutineName(target) + ".state";
		}

		/// <summary>
		/// Compare node state, The compared node will automatic placed to new generated function.
		/// </summary>
		/// <param name="target">The target node to compare</param>
		/// <param name="state">The compare state</param>
		/// <returns></returns>
		public static string CompareNodeState(FlowInput target, bool? state, bool invert = false) {
			if(target == null)
				throw new ArgumentNullException(nameof(target));
			string s = GetCoroutineName(target);
			if(!string.IsNullOrEmpty(s)) {
				string result = s.CGAccess(state == null ? "IsRunning" : state.Value ? "IsSuccess" : "IsFailure");
				if(invert) {
					result = result.CGNot();
				}
				if(!generatorData.stateNodes.Contains(target)) {
					throw new GraphException($"Forbidden to generate state code because the node: {target.node.GetTitle()} is not registered as State Node.\nEnsure to register it using {nameof(CG)}.{nameof(CG.RegisterAsStateFlow)}", target.node);
				}
				return result;
			}
			return null;
		}

		/// <summary>
		/// Get is finished event from coroutine event
		/// </summary>
		/// <param name="target"></param>
		/// <param name="invert"></param>
		/// <returns></returns>
		public static string CompareNodeIsFinished(FlowInput target, bool invert = false) {
			string invertCode = "";
			if(invert) {
				invertCode = "!";
			}
			return invertCode + GetCoroutineName(target).CGAccess("IsFinished");
		}
		#endregion

		#region Event Functions
		/// <summary>
		/// Compare Event State
		/// </summary>
		/// <param name="target"></param>
		/// <param name="state"></param>
		/// <param name="invert"></param>
		/// <returns></returns>
		public static string CompareEventState(object target, bool? state, bool invert = false) {
			if(target is FlowInput) {
				return CompareNodeState(target as FlowInput, state, invert);
			}
			string s = GetCoroutineName(target);
			if(!string.IsNullOrEmpty(s)) {
				string result = s.CGAccess(state == null ? "IsRunning" : state.Value ? "IsSuccess" : "IsFailure");
				if(invert) {
					result = result.CGNot();
				}
				return result;
			}
			return null;
		}

		/// <summary>
		/// Get wait event code for coroutine event
		/// </summary>
		/// <param name="target"></param>
		/// <param name="invokeTarget"></param>
		/// <returns></returns>
		public static string WaitEvent(FlowInput target, bool invokeTarget = true) {
			string result;
			if(invokeTarget) {
				result = GetInvokeNodeCode(target);
				if(!string.IsNullOrEmpty(result)) {
					result = "yield return " + result;
				}
			}
			else {
				result = "yield return " + GetCoroutineName(target) + ".coroutine;";
			}
			return result;
		}

		/// <summary>
		/// Get wait event code for coroutine event
		/// </summary>
		/// <param name="target"></param>
		/// <param name="invokeTarget"></param>
		/// <returns></returns>
		public static string WaitEvent(object target, bool invokeTarget = true) {
			if(target is FlowInput) {
				return WaitEvent(target as FlowInput, invokeTarget);
			}
			string result;
			if(invokeTarget) {
				result = RunEvent(target);
				if(!string.IsNullOrEmpty(result)) {
					result = "yield return " + result;
				}
			}
			else {
				result = "yield return " + GetCoroutineName(target) + ".coroutine;";
			}
			return result;
		}

		/// <summary>
		/// Stop coroutine event
		/// </summary>
		/// <param name="target"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public static string StopEvent(FlowInput target, bool? state = null) {
			if(target == null) {
				throw new ArgumentNullException("target");
			}
			if(state == null) {
				return GetCoroutineName(target) + ".Stop();";
			}
			else if(state.Value) {
				return GetCoroutineName(target) + ".Stop(true);";
			}
			else {
				return GetCoroutineName(target) + ".Stop(false);";
			}
		}

		/// <summary>
		/// Run coroutine event
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static string RunEvent(object target) {
			return GetCoroutineName(target) + ".Run();";
		}

		public static string GetEvent(FlowOutput port) {
			if(!port.isAssigned)
				return null;
			var target = port.GetTargetFlow();
			if(target == null)
				return null;
			string debug = null;
			if(setting.debugScript) {
				debug = Debug(port).AddLineInEnd();
			}
			if(IsStateFlow(target)) {
				if(debug != null) {
					return Invoke(typeof(Runtime.EventCoroutine), nameof(Runtime.EventCoroutine.Create), Value(graph), CG.RoutineEvent(Lambda(debug + Return(GetCoroutineName(target)))));
				}
				return GetCoroutineName(target);
			}
			return Invoke(typeof(Runtime.EventCoroutine), nameof(Runtime.EventCoroutine.Create), Value(graph), CG.Routine(LambdaForEvent(debug + GeneratePort(target))));
		}

		public static string GetEventAndRun(FlowOutput port) {
			return GetEvent(port).Add(".Run()");
		}

		public static string ReturnEvent(FlowOutput port) {
			return CG.Return(GetEvent(port).Add(".Run()"));
		}
		#endregion

		#region GetFinishCode
		/// <summary>
		/// Generate finish code for node.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="isSuccess"></param>
		/// <param name="flowConnection"></param>
		/// <returns></returns>
		public static string FlowFinish(FlowInput input, params FlowOutput[] flowConnection) {
			return FlowFinish(input, true, false, false, flowConnection);
		}

		/// <summary>
		/// Generate finish code for node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="isSuccess"></param>
		/// <param name="flowConnection"></param>
		/// <returns></returns>
		public static string FlowFinish(FlowInput input, bool isSuccess, params FlowOutput[] flowConnection) {
			return FlowFinish(input, isSuccess, true, false, flowConnection);
		}

		/// <summary>
		/// Generate finish code for node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="isSuccess"></param>
		/// <param name="alwaysHaveReturnValue"></param>
		/// <param name="flowConnection"></param>
		/// <returns></returns>
		public static string FlowFinish(FlowInput input, bool isSuccess, bool alwaysHaveReturnValue, params FlowOutput[] flowConnection) {
			return FlowFinish(input, isSuccess, alwaysHaveReturnValue, false, flowConnection);
		}

		/// <summary>
		/// Generate finish code for node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="isSuccess"></param>
		/// <param name="alwaysHaveReturnValue"></param>
		/// <param name="breakCoroutine"></param>
		/// <param name="flowConnection"></param>
		/// <returns></returns>
		public static string FlowFinish(FlowInput input, bool isSuccess, bool alwaysHaveReturnValue = true, bool breakCoroutine = false, params FlowOutput[] flowConnection) {
			string result = null;
			if(isSuccess) {
				string success = null;
				if(setting.debugScript && !input.IsSelfCoroutine()) {
					success += Debug(input, StateType.Success).AddFirst("\n", !string.IsNullOrEmpty(success));
				}
				result = success;
				if(alwaysHaveReturnValue) {
					result += GetReturnValue(input, true, breakCoroutine).AddFirst("\n", result);
				}
				else {
					if(breakCoroutine && input.IsSelfCoroutine()) {
						result = success.Add("\n") + "yield break;";
					}
					else {
						result = success;
					}
				}
			}
			else {
				string failure = null;
				if(setting.debugScript && !input.IsSelfCoroutine()) {
					failure += Debug(input, StateType.Failure).AddFirst("\n", !string.IsNullOrEmpty(failure));
				}
				result = failure;
				if(alwaysHaveReturnValue) {
					result += GetReturnValue(input, false, breakCoroutine).AddFirst("\n", result);
				}
				else {
					if(breakCoroutine && input.IsSelfCoroutine()) {
						result = failure.Add("\n") + "yield break;";
					}
					else {
						result = failure;
					}
				}
			}
			result = result.AddLineInFirst();
			if(flowConnection != null && flowConnection.Length > 0) {
				string flow = null;
				foreach(var f in flowConnection) {
					if(f == null || !f.isAssigned)
						continue;
					flow += Flow(f).AddLineInEnd();
				}
				if(!string.IsNullOrEmpty(flow)) {
					if(result == null)
						result = string.Empty;
					result = result.Insert(0, flow);
				}
			}
			return result;
		}

		/// <summary>
		/// Generate finish code for transition.
		/// </summary>
		/// <param name="transition"></param>
		/// <param name="state"></param>
		/// <returns></returns>
		public static string FlowTransitionFinish(TransitionEvent transition, bool? state = true) {
			if(transition != null) {
				string result = StopEvent(transition.node.enter, state);
				if(debugScript) {
					result += Debug(transition.node, transition);
				}
				return result + Flow(transition.exit, false).AddLineInFirst();
			}
			throw new ArgumentNullException(nameof(transition));
		}
		#endregion

		#region GetInvokeNodeCode
		/// <summary>
		/// Get invoke node code.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="forcedNotGrouped"></param>
		/// <returns></returns>
		public static string GetInvokeNodeCode(FlowInput port, bool forcedNotGrouped = false) {
			if(port == null)
				throw new ArgumentNullException(nameof(port));
			if(!forcedNotGrouped && !IsStateFlow(port)) {
				return GeneratePort(port);
			}
			if(forcedNotGrouped && !generatorData.stateNodes.Contains(port)) {
				throw new GraphException($"Forbidden to generate state code because the node: ({port.node.GetTitle()}-{port.GetType()}) is not registered as State Node.\nEnsure to register it using {nameof(CG)}.{nameof(CG.RegisterAsStateFlow)}", port.node);
			}
			return RunEvent(port);
		}
		#endregion

		#region GenerateFlowCode
		/// <summary>
		/// Function for generating code for flow node.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="from"></param>
		/// <param name="waitTarget"></param>
		/// <returns></returns>
		public static string GenerateFlowCode(FlowInput target, bool waitTarget = true) {
			if(target == null)
				throw new ArgumentNullException("target");
			if(!IsInStateGraph(target.node)) {
				return GeneratePort(target);
			}
			if(waitTarget/* && from && target.IsCoroutine() && from.IsCoroutine()*/) {
				return "yield return " + RunEvent(target);
			}
			if(!isInUngrouped && generatorData.regularNodes.Contains(target)) {
				return GeneratePort(target);
			}
			return RunEvent(target);
		}

		/// <summary>
		/// Function for generating code for flow port.
		/// </summary>
		/// <param name="port">The flow port</param>
		/// <param name="from">The node which flow member comes from</param>
		/// <param name="waitTarget">If true, will generate wait code on coroutine member.</param>
		/// <returns></returns>
		public static string Flow(FlowOutput port, bool waitTarget = true) {
			if(port == null)
				return null;
			if(!port.isAssigned)
				return null;
			var target = port.GetTargetFlow();
			if(target == null)
				return null;
			string debug = null;
			if(setting.debugScript) {
				debug = Debug(port).AddLineInEnd();
			}
			if(!isInUngrouped && !IsInStateGraph(target.node) && !IsStateFlow(target)) {
				return debug + GeneratePort(target);
			}
			if(isInUngrouped || allowYieldStatement && IsStateFlow(target)) {
				if(!generatorData.stateNodes.Contains(target)) {
					return debug + GeneratePort(target);
				}
				if(!allowYieldStatement) {
					throw new Exception("The current block doesn't allow coroutines / yield statements");
				}
				if(waitTarget) {
					return debug + "yield return " + RunEvent(target);
				}
				else {
					return debug + RunEvent(target);
				}
			}
			if(!isInUngrouped && generatorData.regularNodes.Contains(target)) {
				return debug + GeneratePort(target);
			}
			if(!generatorData.stateNodes.Contains(target)) {
				if(!allowYieldStatement && target.IsSelfCoroutine()) {
					throw new Exception("The current block doesn't allow coroutines / yield statements");
				}
				throw new GraphException($"Forbidden to generate state code for port: {port.name} because it is not registered as State port.\nEnsure to register it using {nameof(CG)}.{nameof(CG.RegisterAsStateFlow)}\nFrom node: {target.node.GetTitle()}", target.node);
			}
			return debug + RunEvent(target);
		}

		/// <summary>
		/// Function for generating code for flow member.
		/// </summary>
		/// <param name="flowMembers"></param>
		/// <param name="from"></param>
		/// <param name="waitTarget"></param>
		/// <returns></returns>
		public static string GenerateFlowCode(IList<FlowOutput> flowMembers, bool waitTarget = true) {
			string data = null;
			if(flowMembers != null) {
				for(int i = 0; i < flowMembers.Count; i++) {
					data += Flow(flowMembers[i], waitTarget).AddLineInFirst();
				}
			}
			return data;
		}

		/// <summary>
		/// Function for generating code for flow member.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="waitTarget"></param>
		/// <param name="flowMembers"></param>
		/// <returns></returns>
		public static string GenerateFlowCode(bool waitTarget, params FlowOutput[] flowMembers) {
			string data = null;
			if(flowMembers != null) {
				for(int i = 0; i < flowMembers.Length; i++) {
					data += Flow(flowMembers[i], waitTarget).AddLineInFirst();
				}
			}
			return data;
		}
		#endregion

		#region GetReturnValue
		/// <summary>
		/// Get return value code for node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="value"></param>
		/// <param name="breakCoroutine"></param>
		/// <returns></returns>
		public static string GetReturnValue(FlowInput input, bool breakCoroutine = false) {
			if(isInUngrouped) {
				string result = YieldReturn(GetNodeState(input));
				if(breakCoroutine) {
					result += "\nyield break;";
				}
				return result;
			}
			return null;
		}

		/// <summary>
		/// Get return value code for node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="value"></param>
		/// <param name="breakCoroutine"></param>
		/// <returns></returns>
		public static string GetReturnValue(FlowInput input, bool value, bool breakCoroutine = false) {
			if(input.isConnected && isInUngrouped) {
				string result = YieldReturn(value.CGValue());
				if(breakCoroutine) {
					result += "\nyield break;";
				}
				return result;
			}
			return null;
		}
		#endregion
	}
}