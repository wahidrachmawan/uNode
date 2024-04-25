using MaxyGames.OdinSerializer.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class HLNode : Node {
		[HideInInspector]
		public SerializedType type = typeof(object);

		[NonSerialized]
		public FlowInput enter;
		[NonSerialized]
		public FlowOutput exit;
		[NonSerialized]
		public FlowOutput onSuccess;
		[NonSerialized]
		public FlowOutput onFailure;
		[NonSerialized]
		public ValueOutput @out;

		public class PortData {
			public string name;
			public MemberInfo memberInfo;
			public NodePortAttribute attribute;

			public ValueInput valueInput;
			public ValueOutput valueOutput;
			public FlowInput flowInput;
			public FlowOutput flowOutput;
			public FlowOutputData[] flowOutputs;

		}

		public class FlowOutputData {
			public string name;
			public object value;
			public FlowOutput port;
		}

		[NonSerialized]
		public List<PortData> ports = new List<PortData>();

		private object m_instance;
		private bool hasRegister;

		[System.Runtime.Serialization.OnDeserialized]
		private void OnDeserialized() {
			ports = new List<PortData>();
		}

		protected override void OnRegister() {
			var instanceType = this.type.type;
			if(m_instance == null || m_instance.GetType() != instanceType) {
				if(instanceType != null && ReflectionUtils.CanCreateInstance(instanceType)) {
					m_instance = ReflectionUtils.CreateInstance(instanceType);
				}
				else {
					m_instance = null;
					return;
				}
			}
			if(m_instance is IFlowNode || m_instance is ICoroutineNode) {
				enter = PrimaryFlowInput(nameof(enter), OnExecute);
				exit = PrimaryFlowOutput(nameof(exit));
				enter.isCoroutine = () => enter.IsSelfCoroutine() || exit.IsCoroutine();
				if(m_instance is ICoroutineNode) {
					enter.isSelfCoroutine = () => true;
					enter.actionCoroutine = ExecuteCoroutine;
				}
			}
			else if(m_instance is IStateNode || m_instance is IStateCoroutineNode) {
				enter = PrimaryFlowInput(nameof(enter), OnExecute);
				exit = PrimaryFlowOutput(nameof(exit));
				onSuccess = FlowOutput(nameof(onSuccess));
				onFailure = FlowOutput(nameof(onFailure));
				enter.isCoroutine = () => enter.IsSelfCoroutine() || exit.IsCoroutine() || onSuccess.IsCoroutine() || onFailure.IsCoroutine();
				if(m_instance is IStateCoroutineNode) {
					enter.isSelfCoroutine = () => true;
					enter.actionCoroutine = ExecuteCoroutine;
				}
			}
			if(m_instance is IDataNode) {
				@out = PrimaryValueOutput(nameof(@out));
			}
			ports.Clear();
			if(m_instance is IInstanceNode) {
				var members = ReflectionUtils.GetMembersCached(instanceType);
				foreach(var member in members) {
					RegisterPort(member);
				}
				ValidatePorts();
			}
			else if(m_instance is IStaticNode) {
				var members = ReflectionUtils.GetMembers(instanceType, BindingFlags.Public | BindingFlags.Static);
				foreach(var member in members) {
					RegisterPort(member);
				}
				ValidatePorts();
			}
			else {
				var members = ReflectionUtils.GetFieldsCached(instanceType);
				foreach(var member in members) {
					RegisterPort(member);
				}
			}
		}

		private void ValidatePorts() {
			foreach(var data in ports) {
				if(data.memberInfo is MethodInfo method) {

					if(data.attribute is InputAttribute inputAttribute && inputAttribute.exit != null) {
						var portData = GetFieldData(inputAttribute.exit);
						if(portData == null || portData.flowOutput == null) {
							throw new Exception($"No flow output port with name: {inputAttribute.exit}, please ensure to have flow output port with this name.");
						}
					}

					var parameters = method.GetParameters();
					for(int i = 0; i < parameters.Length; i++) {
						var portData = GetFieldData(parameters[i].Name);
						if(parameters[i].IsOut) {
							if(data.attribute is OutputAttribute) {
								throw new Exception("Cannot use out parameter modifier on Output");
							}
							if(portData == null) {
								throw new Exception($"No {typeof(FlowPortDefinition)} with name: {parameters[i].Name}");
							}
							if(portData.flowOutputs == null) {
								throw new Exception($"The port: {portData.name} is not multi flow output, please specify the multi output type from {typeof(OutputAttribute)} attribute");
							}
						}
						else {
							if(portData == null || portData.valueInput == null) {
								throw new Exception($"No value input port with name: {parameters[i].Name}, please ensure to have value input port with this name.");
							}
						}
					}
				}
			}
		}

		private bool IsSupportedTypeForFlow(Type type) {
			return type == typeof(bool) || type.IsEnum;
		}

		public void RegisterPort(MemberInfo member) {
			if(member.IsDefinedAttribute<NodePortAttribute>() == false)
				return;
			var att = member.GetAttribute<NodePortAttribute>();
			if(member is FieldInfo field) {
				if(field.IsPublic) {
					if(att is InputAttribute inputAttribute) {
						if(field.FieldType == typeof(FlowPortDefinition))
							throw new Exception($"variable with type: {typeof(FlowPortDefinition)} cannot be used for flow input port");
						if(field.FieldType == typeof(ValuePortDefinition) && att.type == null)
							throw new Exception($"Please specify the type of Input port for variable: {field.Name}");

						var port = ValueInput(att.id ?? "field:" + member.Name, att.type ?? field.FieldType).SetName(att.name ?? member.Name);

						if(string.IsNullOrEmpty(att.description) == false) {
							port.SetTooltip(att.description);
						}
						if(inputAttribute.filter != null) {
							port.filter = inputAttribute.filter;
						}

						ports.Add(new PortData() {
							name = field.Name,
							memberInfo = member,
							valueInput = port,
							attribute = att,
						}); ;
					}
					else if(att is OutputAttribute) {
						if(field.FieldType == typeof(FlowPortDefinition)) {
							if(att.type == null) {
								var port = FlowOutput(att.id ?? "field:" + member.Name).SetName(att.name ?? member.Name);

								if(string.IsNullOrEmpty(att.description) == false) {
									port.SetTooltip(att.description);
								}
								if(att.primary) {
									if(nodeObject.primaryFlowOutput != null)
										throw Exception_MultiplePrimaryFlowOutput;
									nodeObject.primaryFlowOutput = port;
								}
								ports.Add(new PortData() {
									name = member.Name,
									memberInfo = member,
									flowOutput = port,
									attribute = att,
								});
							}
							else {
								if(att.type == typeof(bool)) {
									var port = FlowOutput(att.id ?? "field:" + member.Name).SetName(att.name ?? member.Name);

									if(string.IsNullOrEmpty(att.description) == false) {
										port.SetTooltip(att.description);
									}
									if(att.primary) {
										if(nodeObject.primaryFlowOutput != null)
											throw Exception_MultiplePrimaryFlowOutput;
										nodeObject.primaryFlowOutput = port;
									}

									ports.Add(new PortData() {
										name = member.Name,
										memberInfo = member,
										flowOutput = port,
										flowOutputs = new[] {
											new FlowOutputData() {
												name = "True",
												port = port,
												value = true,
											},
										},
										attribute = att,
									});
								}
								else if(att.type.IsEnum) {
									var infos = ReflectionUtils.GetFieldsFromType(att.type, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
									var outputs = new FlowOutputData[infos.Length];
									for(int i = 0; i < infos.Length; i++) {
										var info = infos[i];
										string portnName = null;
										string description = null;
										if(info.IsDefinedAttribute<PortDiscardAttribute>()) {
											continue;
										}
										if(info.IsDefinedAttribute<PortDescriptionAttribute>()) {
											var outputAtt = info.GetAttribute<PortDescriptionAttribute>();
											portnName = outputAtt.name;
											description = outputAtt.description;
										}
										object rawValue = info.GetValueOptimized(null);
										outputs[i] = new FlowOutputData() {
											name = info.Name,
											port = FlowOutput(member.Name + "." + rawValue).SetName(portnName ?? info.Name),
											value = rawValue
										};
										if(description != null) {
											outputs[i].port.SetTooltip(description);
										}
									}
									ports.Add(new PortData() {
										name = member.Name,
										memberInfo = member,
										flowOutputs = outputs,
										attribute = att,
									});
								}
								else {
									throw new Exception($"Not supported type for multi output, the supported type are: {typeof(bool)} and any Enum");
								}
							}
						}
						else {
							if(att.type == null && field.FieldType == typeof(ValuePortDefinition)) {
								throw new Exception($"Please specify the type of Output port for variable: {field.Name}");
							}
							var port = ValueOutput(att.id ?? "field:" + member.Name, att.type ?? field.FieldType).SetName(att.name ?? member.Name);
							port.AssignGetCallback(flow => {
								var instanceData = flow.GetElementData(this);
								return field.GetValueOptimized(instanceData);
							});

							if(string.IsNullOrEmpty(att.description) == false) {
								port.SetTooltip(att.description);
							}
							if(att.primary) {
								if(nodeObject.primaryValueOutput != null)
									throw Exception_MultiplePrimaryValueOutput;
								nodeObject.primaryValueOutput = port;
							}
							ports.Add(new PortData() {
								name = member.Name,
								memberInfo = member,
								valueOutput = port,
								attribute = att,
							});
						}
					}
				}
			}
			else if(member is MethodInfo method) {
				if(method.IsPublic) {
					if(att is InputAttribute inputAttribute) {
						PortData[] datas = null;
						PortData exit = null;
						bool hasOutput = false;
						var port = FlowInput(att.id ?? "method:" + method.Name, flow => {
							if(datas == null) {
								var parameters = method.GetParameters();
								datas = new PortData[parameters.Length];
								for(int i = 0; i < parameters.Length; i++) {
									datas[i] = GetFieldData(parameters[i].Name);
									if(datas[i].flowOutputs != null) {
										hasOutput = true;
									}
								}
								if(inputAttribute.exit != null) {
									exit = GetFieldData(inputAttribute.exit);
								}
							}
							var instanceData = flow.GetElementData(this);
							var values = new object[datas.Length];
							for(int i = 0; i < datas.Length; i++) {
								if(datas[i].valueInput != null)
									values[i] = datas[i].valueInput.GetValue(flow);
							}
							method.InvokeOptimized(instanceData, values);
							if(hasOutput) {
								for(int i = 0; i < datas.Length; i++) {
									if(datas[i].flowOutputs != null) {
										foreach(var data in datas[i].flowOutputs) {
											if(data == null) continue;
											if(object.Equals(data.value, values[i])) {
												flow.Next(data.port);
												break;
											}
										}
									}
									else if(datas[i].valueOutput != null) {
										flow.SetUserData(this, datas[i].valueOutput, values[i]);
									}
								}
							}
							if(exit != null) {
								flow.Next(exit.flowOutput);
							}
						}).SetName(att.name ?? member.Name);

						if(string.IsNullOrEmpty(att.description) == false) {
							port.SetTooltip(att.description);
						}
						if(att.primary) {
							if(nodeObject.primaryFlowInput != null)
								throw Exception_MultiplePrimaryFlowInput;
							nodeObject.primaryFlowInput = port;
						}

						ports.Add(new PortData() {
							name = member.Name,
							memberInfo = member,
							flowInput = port,
							attribute = att,
						});
					}
					else if(att is OutputAttribute) {
						var port = ValueOutput(att.id ?? "method:" + method.Name, att.type ?? method.ReturnType).SetName(att.name ?? member.Name);

						PortData[] datas = null;
						port.AssignGetCallback(flow => {
							if(datas == null) {
								var parameters = method.GetParameters();
								datas = new PortData[parameters.Length];
								for(int i = 0; i < parameters.Length; i++) {
									datas[i] = GetFieldData(parameters[i].Name);
								}
							}
							var instanceData = flow.GetElementData(this);
							var values = new object[datas.Length];
							for(int i = 0; i < datas.Length; i++) {
								if(datas[i].valueInput != null)
									values[i] = datas[i].valueInput.GetValue(flow);
							}
							return method.InvokeOptimized(instanceData, values);
						});

						if(string.IsNullOrEmpty(att.description) == false) {
							port.SetTooltip(att.description);
						}
						if(att.primary) {
							if(nodeObject.primaryValueOutput != null)
								throw Exception_MultiplePrimaryValueOutput;
							nodeObject.primaryValueOutput = port;
						}

						ports.Add(new PortData() {
							name = member.Name,
							memberInfo = member,
							valueOutput = port,
							attribute = att,
						});
					}
				}
			}
			else if(member is PropertyInfo property) {
				if(property.CanRead) {
					var getMethod = property.GetMethod;
					if(att is OutputAttribute) {
						var port = ValueOutput(att.id ?? "member:" + property.Name, att.type ?? getMethod.ReturnType).SetName(att.name ?? member.Name);

						PortData[] datas = null;
						port.AssignGetCallback(flow => {
							if(datas == null) {
								var parameters = getMethod.GetParameters();
								datas = new PortData[parameters.Length];
								for(int i = 0; i < parameters.Length; i++) {
									datas[i] = GetFieldData(parameters[i].Name);
								}
							}
							var instanceData = flow.GetElementData(this);
							var values = new object[datas.Length];
							for(int i = 0; i < datas.Length; i++) {
								if(datas[i].valueInput != null)
									values[i] = datas[i].valueInput.GetValue(flow);
							}
							return getMethod.InvokeOptimized(instanceData, values);
						});

						if(string.IsNullOrEmpty(att.description) == false) {
							port.SetTooltip(att.description);
						}
						if(att.primary) {
							if(nodeObject.primaryValueOutput != null)
								throw Exception_MultiplePrimaryValueOutput;
							nodeObject.primaryValueOutput = port;
						}

						ports.Add(new PortData() {
							name = member.Name,
							memberInfo = member,
							valueOutput = port,
							attribute = att,
						});
					}
				}
			}
		}

		private PortData GetFieldData(string name) {
			for(int i = 0; i < ports.Count; i++) {
				if(ports[i].name == name) {
					return ports[i];
				}
			}
			return null;
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			instance.SetElementData(this, ReflectionUtils.CreateInstance(type));
		}

		#region Code Generation
		public override void OnGeneratorInitialize() {
			if(m_instance == null) return;
			string generatedInstanceName;

			if(m_instance is IStaticNode) {
				generatedInstanceName = CG.Type(m_instance.GetType());
			}
			else {
				var variable = CG.GetOrRegisterUserObject(new VariableData(name, m_instance.GetType(), m_instance) {
					modifier = FieldModifier.PrivateModifier,
				}, this);
				generatedInstanceName = CG.RegisterVariable(variable);
			}

			for(int x = 0; x < ports.Count; x++) {
				var data = ports[x];
				if(data.flowInput != null) {
					if(data.memberInfo is MethodInfo method) {
						FlowOutput exit = null;
						if(data.attribute is InputAttribute inputAttribute && inputAttribute.exit != null) {
							exit = GetFieldData(inputAttribute.exit).flowOutput;
						}
						CG.RegisterPort(data.flowInput, () => {
							var parameters = method.GetParameters();

							List<(string, ParameterInfo, PortData)> outputVariables = new(parameters.Length);
							string[] args = new string[parameters.Length];
							for(int i = 0; i < parameters.Length; i++) {
								if(parameters[i].IsOut) {
									var name = CG.GenerateNewName("tempVar");
									outputVariables.Add((name, parameters[i], GetFieldData(parameters[i].Name)));
									args[i] = "out var " + name;
								}
								else {
									args[i] = CG.GeneratePort(GetFieldData(parameters[i].Name).valueInput);
								}
							}
							List<string> contents = new List<string>(2 + outputVariables.Count) {
								generatedInstanceName.CGFlowInvoke(method.Name, args)
							};
							if(outputVariables.Count > 0) {
								foreach(var (vName, info, data) in outputVariables) {
									if(data.flowOutputs.Length == 1) {
										var item = data.flowOutputs[0];
										if(item.port.hasValidConnections) {
											contents.Add(CG.If(CG.Compare(vName, item.value.CGValue()), CG.GeneratePort(item.port)));
										}
									}
									else {
										List<(string, string)> cases = new List<(string, string)>(data.flowOutputs.Length);
										foreach(var item in data.flowOutputs) {
											if(item == null) continue;
											if(item.port.hasValidConnections) {
												cases.Add((item.value.CGValue(), CG.GeneratePort(item.port)));
											}
										}
										contents.Add(CG.Switch(vName, cases));
									}
								}
							}
							if(exit != null) {
								contents.Add(CG.FlowFinish(data.flowInput, exit));
							}
							return CG.Flow(contents);
						});
					}
					else {
						throw new InvalidOperationException();
					}
				}
				else if(data.valueOutput != null) {
					if(data.memberInfo is MethodInfo method) {
						CG.RegisterPort(data.valueOutput, () => {
							var parameters = method.GetParameters();
							string[] args = new string[parameters.Length];
							for(int i = 0; i < parameters.Length; i++) {
								if(parameters[i].IsOut) {
									throw new InvalidOperationException();
								}
								else {
									args[i] = CG.GeneratePort(GetFieldData(parameters[i].Name).valueInput);
								}
							}
							return generatedInstanceName.CGInvoke(method.Name, args);
						});
					}
					else {
						CG.RegisterPort(data.valueOutput, () => {
							return generatedInstanceName.CGAccess(data.name);
						});
					}
				}
			}

			if(enter != null) {
				CG.RegisterPort(enter, () => {
					string init = GenerateInitializerCode(generatedInstanceName);
					if(m_instance is IFlowNode) {
						return CG.Flow(
							init,
							generatedInstanceName.CGFlowInvoke(nameof(IFlowNode.Execute), CG.This),
							CG.FlowFinish(enter, exit)
						);
					}
					else if(m_instance is IStateNode) {
						if(CG.debugScript || CG.IsStateFlow(enter)) {
							//If debug are on or return state is supported
							return CG.Flow(
								init,
								CG.If(
									generatedInstanceName.CGInvoke(nameof(IStateNode.Execute), CG.This),
									CG.FlowFinish(enter, true, true, false, onSuccess, exit),
									CG.FlowFinish(enter, false, true, false, onFailure, exit)
								)
							);
						}
						return CG.Flow(
							init,
							CG.If(
								generatedInstanceName.CGInvoke(nameof(IStateNode.Execute), CG.This),
								CG.Flow(onSuccess),
								CG.Flow(onFailure)
							),
							CG.FlowFinish(enter, exit)
						);
					}
					else if(m_instance is ICoroutineNode) {
						CG.RegisterCoroutineEvent(
							m_instance,
							() => generatedInstanceName.CGInvoke(nameof(ICoroutineNode.Execute), CG.This), true);
						return CG.Flow(
							init,
							CG.WaitEvent(m_instance),
							CG.FlowFinish(enter, exit)
						);
					}
					else if(m_instance is IStateCoroutineNode) {
						CG.RegisterCoroutineEvent(
							m_instance,
							() => generatedInstanceName.CGInvoke(nameof(IStateCoroutineNode.Execute), CG.This), true);
						if(CG.debugScript || CG.IsStateFlow(enter)) {
							//If debug are on or return state is supported
							return CG.Flow(
								init,
								CG.WaitEvent(m_instance),
								CG.If(
									CG.CompareEventState(m_instance, true),
									CG.FlowFinish(enter, true, true, false, onSuccess, exit),
									CG.FlowFinish(enter, false, true, false, onFailure, exit)
								)
							);
						}
						return CG.Flow(
							init,
							CG.WaitEvent(m_instance),
							CG.If(
								CG.CompareEventState(m_instance, true),
								CG.Flow(onSuccess),
								CG.Flow(onFailure)),
							CG.FlowFinish(enter, exit)
						);
					}
					else {
						throw new Exception("Unsupported type: " + m_instance.GetType());
					}
				});
			}
			if(@out != null) {
				CG.RegisterPort(@out, () => {
					string init = GenerateInitializerCode(generatedInstanceName);
					string invoke = null;
					if(m_instance is DataNode) {
						invoke = generatedInstanceName.CGInvoke(
							nameof(DataNode.GetValue),
							new Type[] { m_instance.GetType() },
							CG.This);
					}
					else if(m_instance.GetType().HasImplementInterface(typeof(IDataNode<>))) {
						invoke = generatedInstanceName.CGInvoke(nameof(IDataNode<bool>.GetValue), CG.This);
					}
					else {
						invoke = generatedInstanceName.CGInvoke(nameof(IDataNode.GetValue), CG.This).CGConvert(m_instance.GetType());
					}
					if(string.IsNullOrEmpty(init)) {
						return invoke;
					}
					else {
						return typeof(uNodeUtility).CGType().CGFlowInvoke(
							nameof(uNodeUtility.RuntimeGetValue),
							CG.Lambda(null, null,
								CG.Flow(
									init,
									CG.Return(invoke)
								))).RemoveLast();
					}
				});
			}
		}

		private string GenerateInitializerCode(string instanceName) {
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			foreach(var init in ports) {
				if(init.valueInput != null && init.valueInput.isAssigned) { //Ensure we are only set the dynamic value
					builder.Append(instanceName.CGAccess(init.name).CGSet(init.valueInput.CGValue()).AddLineInFirst());
				}
			}
			return builder.ToString();
		}
		#endregion

		#region Reflection
		IEnumerator ExecuteCoroutine(Flow flow) {
			InitField(flow);
			IEnumerator iterator;
			if(m_instance is ICoroutineNode) {
				iterator = (m_instance as ICoroutineNode).Execute(flow.target).GetEnumerator();
			}
			else if(m_instance is IStateCoroutineNode) {
				iterator = (m_instance as IStateCoroutineNode).Execute(flow.target).GetEnumerator();
			}
			else {
				throw new InvalidOperationException();
			}
			StateType resultState = StateType.Running;
			object result = null;
			while(iterator.MoveNext()) {
				result = iterator.Current;
				if(result is string) {
					string r = result as string;
					if(r == "Success" || r == "Failure") {
						resultState = r == "Success" ? StateType.Success : StateType.Failure;
						break;
					}
				}
				else if(result is bool) {
					bool r = (bool)result;
					resultState = r ? StateType.Success : StateType.Failure;
					break;
				}
				yield return result;
			}
			if(resultState == StateType.Running && m_instance is IStateCoroutineNode) {
				resultState = StateType.Success;
			}
			if(resultState != StateType.Running) {
				flow.state = resultState;
				switch(resultState) {
					case StateType.Success:
						flow.Next(onSuccess, exit);
						break;
					case StateType.Failure:
						flow.Next(onFailure, exit);
						break;
					default:
						throw new InvalidOperationException();
				}
			}
			else flow.Next(exit);
		}

		private void InitField(Flow flow) {
			if(ports.Count > 0 && m_instance != null) {
				var instanceType = m_instance.GetType();
				foreach(var init in ports) {
					if(init.valueInput.isAssigned) {
						var field = instanceType.GetFieldCached(init.name);
						if(field != null) {
							field.SetValueOptimized(m_instance, init.valueInput.GetValue(flow));
						}
					}
				}
			}
		}

		private void OnExecute(Flow flow) {
			InitField(flow);
			var instance = flow.GetElementData(this);
			if(instance is IFlowNode) {
				(instance as IFlowNode).Execute(flow.target);
				flow.Next(exit);
				return;
			}
			else if(instance is IStateNode) {
				if((instance as IStateNode).Execute(flow.target)) {
					flow.Next(onSuccess, exit);
				}
				else {
					flow.Next(onFailure, exit);
				}
				return;
			}
			else if(instance is ICoroutineNode || instance is IStateCoroutineNode) {
				throw new InvalidOperationException("The node is coroutine but trying to execute without coroutine.");
			}
			else if(instance == null) {
				throw new NullReferenceException("The reflected instance is null");
			}
			throw new InvalidOperationException();
		}

		public override object GetValue(Flow flow) {
			var instance = flow.GetElementData(this);
			if(instance is IDataNode) {
				InitField(flow);
				return (instance as IDataNode).GetValue(flow.target);
			}
			return null;
		}
		#endregion

		public override System.Type ReturnType() {
			if(m_instance is IDataNode) {
				return (m_instance as IDataNode).ReturnType();
			}
			return typeof(object);
		}

		public override string GetTitle() {
			Type instancecType = type.type;
			if(instancecType != null) {
				if(instancecType.IsDefined(typeof(NodeMenu), true)) {
					return (instancecType.GetCustomAttributes(typeof(NodeMenu), true)[0] as NodeMenu).name;
				}
			}
			else {
				return "Missing Type";
			}
			return type.prettyName;
		}

		public override void CheckError(ErrorAnalyzer analizer) {
			base.CheckError(analizer);
			if(type.type == null) {
				analizer.RegisterError(this, "Missing node type: " + type.typeName);
			}
			else if(!ReflectionUtils.CanCreateInstance(type.type)) {
				analizer.RegisterError(this, "Cannot create instance of type: " + type.type);
			}
		}

		public override Type GetNodeIcon() {
			if(m_instance is IIcon) {
				return (m_instance as IIcon).GetIcon();
			}
			return base.GetNodeIcon();
		}
	}
}