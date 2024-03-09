using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Event Hook", inputs = new[] { typeof(Delegate), typeof(UnityEventBase) })]
	public class EventHook : Node {
		public ValueInput target { get; set; }
		public FlowInput register { get; set; }
		public FlowInput unregister { get; set; }
		public FlowOutput body { get; set; }

		public class PortData {
			public string id = uNodeUtility.GenerateUID();
			public string name;
			public SerializedType type = typeof(object);

			[System.NonSerialized]
			public ValueOutput port;
		}
		[HideInInspector]
		public List<PortData> parameters = new List<PortData>();

		private System.Delegate m_Delegate;

		private Type _registeredType;

		protected override void OnRegister() {
			target = ValueInput(nameof(target), typeof(Delegate));
			target.filter = new FilterAttribute(typeof(Delegate), typeof(UnityEventBase)) {
				SetMember = true,
			};
			register = FlowInput(nameof(register), OnRegisters);
			unregister = FlowInput(nameof(unregister), OnUnregister);
			body = FlowOutput(nameof(body));
			if(target.isAssigned) {
				var targetType = target.ValueType;
				if(targetType != null) {
					if(_registeredType != targetType) {
						_registeredType = targetType;
						if(targetType.IsCastableTo(typeof(Delegate))) {
							var types = targetType.GetMethod("Invoke").GetParameters();
							for(int i = 0; i < types.Length; i++) {
								if(parameters.Count > i) {
									parameters[i].name = types[i].Name;
									parameters[i].type = types[i].ParameterType;
								} else {
									parameters.Add(new PortData() {
										name = types[i].Name,
										type = types[i].ParameterType
									});
								}
							}
							while(parameters.Count > types.Length) {
								parameters.RemoveAt(parameters.Count - 1);
							}
						} else if(targetType.IsCastableTo(typeof(UnityEventBase))) {
							var method = targetType.GetMethod("AddListener");
							var types = method.GetParameters()[0].ParameterType.GetGenericArguments();
							for(int i = 0; i < types.Length; i++) {
								if(parameters.Count > i) {
									parameters[i].name = "Parameter " + i.ToString();
									parameters[i].type = types[i];
								} else {
									parameters.Add(new PortData() {
										name = "Parameter " + i.ToString(),
										type = types[i]
									});
								}
							}
							while(parameters.Count > types.Length) {
								parameters.RemoveAt(parameters.Count - 1);
							}
						}
					}
				}
			}
			foreach(var p in parameters) {
				p.port = ValueOutput(p.id, () => p.type, PortAccessibility.ReadWrite).SetName(p.name);
			}
		}

		private void OnRegisters(Flow flow) {
			if(target.isAssigned) {
				object val = target.GetValue(flow);
				if(val == null) {
					val = new MemberData.Event(target.defaultValue.CreateRuntimeEvent(), null);
				}
				if(val is MemberData.Event) {
					MemberData.Event e = val as MemberData.Event;
					if(e.eventInfo != null) {
						if(m_Delegate == null) {
							if(e.eventInfo is RuntimeEvent) {
								var returnType = target.type.GetMethod("Invoke").ReturnType;
								m_Delegate = new MemberData.EventCallback((obj => {
									if(nodeObject == null)
										return null;
									if(obj != null && parameters.Count == obj.Length) {
										for(int i = 0; i < obj.Length; i++) {
											flow.SetPortData(parameters[i].port, obj[i]);
										}
									}
									if(returnType == typeof(void)) {
										flow.TriggerParallel(body);
										return null;
									} else {
										flow.TriggerParallel(body);
										var js = flow.jumpStatement;
										if(js == null || js.jumpType != JumpStatementType.Return) {
											throw new Exception("No return value");
										}
										return js.value;
									}
								}));
							} else {
								var method = e.eventInfo.EventHandlerType.GetMethod("Invoke");
								var type = method.ReturnType;
								if(type == typeof(void)) {
									m_Delegate = CustomDelegate.CreateActionDelegate((obj) => {
										if(nodeObject == null)
											return;
										if(obj != null && parameters.Count == obj.Length) {
											for(int i = 0; i < obj.Length; i++) {
												flow.SetPortData(parameters[i].port, obj[i]);
											}
										}
										flow.TriggerParallel(body);
									}, method.GetParameters().Select(i => i.ParameterType).ToArray());
								} else {
									var types = method.GetParameters().Select(i => i.ParameterType).ToList();
									types.Add(type);
									m_Delegate = CustomDelegate.CreateFuncDelegate((obj) => {
										if(nodeObject == null)
											return null;
										if(obj != null && parameters.Count == obj.Length) {
											for(int i = 0; i < obj.Length; i++) {
												flow.SetPortData(parameters[i].port, obj[i]);
											}
										}
										flow.TriggerParallel(body);
										JumpStatement js = flow.jumpStatement;
										if(js == null || js.jumpType != JumpStatementType.Return) {
											throw new Exception("No return value");
										}
										return js.value;
									}, types.ToArray());
								}
								m_Delegate = ReflectionUtils.ConvertDelegate(m_Delegate, e.eventInfo.EventHandlerType);
							}
						}
						e.eventInfo.AddEventHandler(e.instance, m_Delegate);
					}
				} else if(val is UnityEventBase) {
					var method = val.GetType().GetMethod("AddListener");
					if(m_Delegate == null) {
						var param = method.GetParameters()[0].ParameterType;
						var gType = param.GetGenericArguments();
						m_Delegate = CustomDelegate.CreateActionDelegate((obj) => {
							if(nodeObject == null)
								return;
							if(obj != null && parameters.Count == obj.Length) {
								for(int i = 0; i < obj.Length; i++) {
									flow.SetPortData(parameters[i].port, obj[i]);
								}
							}
							flow.TriggerParallel(body);
						}, gType);
						m_Delegate = System.Delegate.CreateDelegate(param, m_Delegate.Target, m_Delegate.Method);
					}
					method.InvokeOptimized(val, new object[] { m_Delegate });
				} else {
					if(val == null) {
						throw new Exception("The target event is null");
					}
					throw new Exception("Invalid target value: " + val);
				}
			}
		}

		private void OnUnregister(Flow flow) {
			if(m_Delegate != null && target.isAssigned) {
				object val = target.GetValue(flow);
				if(val is MemberData.Event) {
					MemberData.Event e = val as MemberData.Event;
					if(e.eventInfo != null) {
						e.eventInfo.RemoveEventHandler(e.instance, m_Delegate);
					}
				} else if(val is UnityEventBase) {
					var method = val.GetType().GetMethod("RemoveListener");
					method.InvokeOptimized(val, new object[] { m_Delegate });
				}
			}
		}

		public override void OnGeneratorInitialize() {
			foreach(var param in parameters) {
				var vName = CG.RegisterVariable(param.port, param.name);
				CG.RegisterPort(param.port, () => vName);
			}
			CG.RegisterPort(register, () => {
				if(target.ValueType.IsCastableTo(typeof(UnityEventBase))) {
					return target.CGValue().CGInvoke("AddListener", GenerateEventCodes()).AddSemicolon();
				}
				return CG.Set(target, GenerateEventCodes(), SetType.Add, target.type);
			});
			CG.RegisterPort(unregister, () => {
				if(target.ValueType.IsCastableTo(typeof(UnityEventBase))) {
					return target.CGValue().CGInvoke("RemoveListener", GenerateEventCodes()).AddSemicolon();
				}
				return CG.Set(target, GenerateEventCodes(), SetType.Subtract, target.type);
			});
		}

		private string GenerateEventCodes() {
			if(!body.isAssigned) {
				return null;
			}
			Type targetType = target.ValueType;
			if(targetType == null)
				return null;
			Type delegateReturnType;
			if(targetType.IsCastableTo(typeof(Delegate))) {
				var method = targetType.GetMethod("Invoke");
				var targetFlow = body.GetTargetFlow();

				delegateReturnType = method.ReturnType;

				if(targetFlow.isPrimaryPort && CG.CanSimplifyToLambda(body, method.ReturnType, parameters.Select(p => p.port).ToArray())) {
					var targetNode = targetFlow.node.node;
					if(method.ReturnType == typeof(void)) {
						if(targetNode is MultipurposeNode mn) {
							string result = CG.Value(mn.member);
							if(result.EndsWith(")")) {
								int deep = 0;
								for(int i = result.Length - 1; i > 0; i--) {
									var c = result[i];
									if(c == '(') {
										if(deep == 0) {
											result = result.Remove(i);
											break;
										}
										else {
											deep--;
										}
									}
									else if(c == ')' && i != result.Length - 1) {
										deep++;
									}
								}
							}
							return result;
						}
					}
					else {
						if(targetNode is NodeReturn nr && nr.value.GetTargetNode()?.node is MultipurposeNode mn) {
							string result = CG.Value(mn.member);
							if(result.Contains(")")) {
								int deep = 0;
								for(int i = result.Length - 1; i > 0; i--) {
									var c = result[i];
									if(c == '(') {
										if(deep == 0) {
											result = result.Remove(i);
											break;
										}
										else {
											deep--;
										}
									}
									else if(c == ')' && i != result.Length - 1) {
										deep++;
									}
								}
							}
							return result;
						}
					}
				}
			}
			else if(targetType.IsCastableTo(typeof(UnityEventBase))) {
				var method = targetType.GetMethod("AddListener");
				var param = method.GetParameters()[0].ParameterType;
				delegateReturnType = method.ReturnType;

				var targetFlow = body.GetTargetFlow();
				if(targetFlow.isPrimaryPort && CG.CanSimplifyToLambda(body, typeof(void), parameters.Select(p => p.port).ToArray())) {
					var targetnode = targetFlow.node.node;
					if(targetnode is MultipurposeNode mn) {
						string result = CG.Value(mn.member);
						if(result.EndsWith(")")) {
							int deep = 0;
							for(int i = result.Length - 1; i > 0; i--) {
								var c = result[i];
								if(c == '(') {
									if(deep == 0) {
										result = result.Remove(i);
										break;
									}
									else {
										deep--;
									}
								}
								else if(c == ')' && i != result.Length - 1) {
									deep++;
								}
							}
						}
						return result;
					}
				}

			}
			else {
				throw new Exception("Unsupported event to hook:" + target.GetPrettyName());
			}
			//Generate lambda code
			string contents = null;
			List<Type> types = new List<Type>();
			List<string> parameterNames = new List<string>();
			for(int i = 0; i < parameters.Count; i++) {
				var pType = parameters[i];
				if(pType != null) {
					string varName = null;
					if(!CG.CanDeclareLocal(pType.port, body)) {
						varName = CG.GenerateName("tempVar", this);
						var vdata = CG.GetVariableData(pType.port);
						vdata.SetToInstanceVariable();
						contents = CG.Set(vdata.name, varName);
					}
					else {
						varName = CG.GetVariableName(pType.port);
					}
					types.Add(pType.port.type);
					parameterNames.Add(varName);

				}
			}
			contents += CG.Flow(body, false).AddLineInFirst();

			if(register.hasValidConnections && unregister.hasValidConnections) {
				var method = CG.GetUserObject<CG.MData>(this);
				if(method == null) {
					method = CG.generatorData.AddNewGeneratedMethod(
						"EventHookDelegate",
						delegateReturnType,
						types.Select((t, index) => new CG.MPData(parameterNames[index], t)).ToArray());
					method.AddCode(contents);
					//Register the newly method to cached
					CG.RegisterUserObject(method, this);
				}
				return method.name;
			}

			return CG.Lambda(types, parameterNames, contents);
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.EventIcon);
		}
	}
}