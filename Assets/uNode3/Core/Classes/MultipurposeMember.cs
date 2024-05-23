using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MaxyGames.UNode {
	/// <summary>
	/// Allow you to invoking method or get field, property or even create new object instance and much more.
	/// This class can invoke method, constructor or event with parameter.
	/// </summary>
	public class MultipurposeMember {
		public class MInfo {
			public MemberInfo info;
			public List<MParamInfo> parameters = new List<MParamInfo>();
		}

		public class MParamInfo {
			public RefKind refKind;

			public bool IsByRef => refKind != RefKind.None;
			public bool IsOut => refKind == RefKind.Out;
			public bool IsIn => refKind == RefKind.In;

			public string name {
				get {
					if(info != null) {
						return info.Name;
					}
					else if(input != null) {
						return input.name;
					}
					else {
						return output.name;
					}
				}
			}
			public bool useOutput => output != null;

			[NonSerialized]
			public ValueInput input;
			//The output only available for parameter with `out` modifier.
			[NonSerialized]
			public ValueOutput output;

			[NonSerialized]
			public ParameterInfo info;

			public Type type {
				get {
					if(info != null) {
						return info.ParameterType;
					}
					else if(input != null) {
						return input.type;
					}
					else {
						return output.type;
					}
				}
			}
		}
		public class InitializerData {
			public string id = uNodeUtility.GenerateUID();
			public string name;
			public SerializedType type;

			public ComplexElementInitializer[] elementInitializers;

			public ValueInput port { get; set; }

			public bool isComplexInitializer => elementInitializers != null;

			public IEnumerable<ValueInput> ports {
				get {
					if(elementInitializers != null) {
						foreach(var element in elementInitializers) {
							yield return element.port;
						}
					}
					yield return port;
				}
			}
		}

		public class ComplexElementInitializer {
			public string name;
			public SerializedType type;

			public ValueInput port { get; set; }
		}

		public MemberData target = MemberData.None;
		public List<InitializerData> initializers = new List<InitializerData>();

		public MemberData.TargetType targetType => target.targetType;
		public bool isAssigned => target.isAssigned;

		[NonSerialized]
		private Node node;

		public ValueInput instance { get; private set; }
		public MInfo[] datas { get; private set; }
		public bool hasRegister { get; private set; }

		public List<MParamInfo> parameters { get; private set; }

		public void OnGeneratorInitialize(params FlowOutput[] flows) {
			if(parameters != null) {
				CG.RegisterPostInitialization(() => {
					foreach(var p in parameters) {
						if(p.output != null && p.output.isConnected) {
							if(CG.CanDeclareLocal(p.output, flows)) {
								string name = CG.RegisterVariable(p.output, isLocal: true);
								CG.RegisterPort(p.output, () => name);
							}
							else {
								string name = CG.RegisterVariable(p.output, isLocal: false);
								CG.RegisterPort(p.output, () => name);
							}
						}
					}
				});
			}
		}

		public void Register(Node node, string pathID = null, Action createOutPort = null, Action createFlowPort = null, bool preferOutputForParameters = false, bool createInstancePort = true) {
			if(target == null || !target.isAssigned)
				return;
			this.node = node;
			hasRegister = true;
			switch(target.targetType) {
				case MemberData.TargetType.Null:
					createOutPort?.Invoke();
					break;
				case MemberData.TargetType.Self:
					createOutPort?.Invoke();
					break;
				default:
					var members = target.GetMembers(false);
					if(members == null || members.Length == 0) {
						if(target.CanGetValue() || target.CanSetValue()) {
							createOutPort?.Invoke();
						}
						if(target.targetType.HasFlags(MemberData.TargetType.Constructor | MemberData.TargetType.Method | MemberData.TargetType.uNodeFunction)) {
							createFlowPort?.Invoke();
							if(target.targetType == MemberData.TargetType.uNodeFunction) {
								var reference = target.startItem.GetReferenceValue() as Function;
								if(reference != null) {
									parameters = new List<MParamInfo>();
									datas = new MInfo[] {
										new MInfo()
									};
									for(int i = 0; i < reference.parameters.Count; i++) {
										var p = reference.parameters[i];
										var pdata = new MParamInfo() {
											info = null,
											refKind = p.refKind,
										};
										if(preferOutputForParameters && pdata.IsOut) {
											pdata.output = Node.Utilities.ValueOutput(
												node,
												pathID + "-" + i + "-" + 0, p.Type,
												PortAccessibility.ReadWrite).SetName(p.name);
										}
										else {
											pdata.input = Node.Utilities.ValueInput(node, pathID + "-" + i + "-" + 0, p.Type, out var isNew).SetName(p.name);
											if(isNew) {
												pdata.input.AssignToDefault(MemberData.CreateValueFromType(p.Type));
											}
											if(pdata.refKind == RefKind.Out) {
												pdata.input.filter = new FilterAttribute() {
													SetMember = true,
												};
											}
										}
										datas[0].parameters.Add(pdata);
										parameters.Add(pdata);
									}
								}
							}
						}
						return;
					}
					var lastMember = members[members.Length - 1];
					var returnType = ReflectionUtils.GetMemberType(lastMember);
					if(returnType != typeof(void)) {
						createOutPort?.Invoke();
					}
					if(target.targetType == MemberData.TargetType.uNodeFunction || lastMember is MethodBase) {
						createFlowPort?.Invoke();
					}
					if(createInstancePort && !target.isStatic && target.IsTargetingReflection) {
						instance = Node.Utilities.ValueInput(node, pathID + "-" + nameof(instance), target.startType).SetName("instance");
						instance.canSetValue = () => {
							if(instance.UseDefaultValue) {
								return instance.defaultValue.CanSetValue();
							}
							else {
								var other = instance.GetTargetPort();
								if(other == null)
									return false;
								return other.CanSetValue();
							}
						};
					}
					parameters = new List<MParamInfo>();
					datas = new MInfo[members.Length];
					for(int i = 0; i < members.Length; i++) {
						var m = members[i];
						var data = new MInfo();
						data.info = m;
						if(m is MethodBase method) {
							var parameterInfos = method.GetParameters();
							for(int x = 0; x < parameterInfos.Length; x++) {
								var p = parameterInfos[x];
								var pdata = new MParamInfo() {
									info = p,
								};
								if(p.IsOut) {
									pdata.refKind = RefKind.Out;
								}
								else if(p.IsIn) {
									pdata.refKind = RefKind.In;
								}
								else if(p.ParameterType.IsByRef) {
									pdata.refKind = RefKind.Ref;
								}
								if(preferOutputForParameters && pdata.IsOut) {
									pdata.output = Node.Utilities.ValueOutput(
										node,
										pathID + "-" + i + "-" + x, p.ParameterType.GetElementType(),
										PortAccessibility.ReadWrite).SetName(p.Name);
								}
								else {
									pdata.input = Node.Utilities.ValueInput(
										node,
										pathID + "-" + i + "-" + x,
										p.ParameterType, out var isNew).SetName(p.Name);
									if(isNew) {
										pdata.input.AssignToDefault(MemberData.CreateValueFromType(p.ParameterType));
									}
									if(pdata.refKind == RefKind.Out) {
										pdata.input.filter = new FilterAttribute() {
											SetMember = true,
										};
									}
								}
								data.parameters.Add(pdata);
								parameters.Add(pdata);
							}
						}
						datas[i] = data;
					}
					break;
			}
			if(initializers == null)
				initializers = new List<InitializerData>();
			for(int i = 0; i < initializers.Count; i++) {
				var init = initializers[i];
				if(init.isComplexInitializer) {
					for(int x = 0; x < init.elementInitializers.Length; x++) {
						var element = init.elementInitializers[x];
						element.port = Node.Utilities.ValueInput(node, pathID + "-init." + init.id + "#" + x, element.type.type).SetName(element.name + " " + i);
					}
				}
				else {
					init.port = Node.Utilities.ValueInput(node, pathID + "-init." + init.id, init.type.type).SetName(init.name);
				}
			}
		}

		public void CheckErrors(UGraphElement element, ErrorAnalyzer analizer) {
			if(instance != null) {
				analizer.CheckPort(instance);
			}
			analizer.CheckValue(target, "", element);
			if(parameters != null) {
				foreach(var p in parameters) {
					if(p.input != null) {
						analizer.CheckPort(p.input);
					}
				}
				var members = target.GetMembers(false);
				if(members != null) {
					for(int i = 0; i < members.Length; i++) {
						if(members[i] is IGenericMethodWithResolver genericMethodWithResolver && ReflectionUtils.IsNativeMember(members[i]) == false) {
							var resolver = genericMethodWithResolver.GetResolver();
							if(resolver == null || resolver is GenericMethodResolver.Default) {
								analizer.RegisterWarning(element, $"No resolver for member: {(members[i] as INativeMethod).GetNativeMethod().GetGenericMethodDefinition()}, this can lead to incorrect or different behavior when running between reflection and native c#");
							}
						}
					}
				}
			}
		}

		public Type ReturnType() {
			if(target != null && target.isAssigned) {
				return target.type;
			}
			return typeof(object);
		}

		public object[] GetParameterValues(Flow flow) {
			var paramsValue = new object[parameters.Count];
			for(int i = 0; i < paramsValue.Length; i++) {
				if(parameters[i].input != null) {
					paramsValue[i] = parameters[i].input.GetValue(flow);
				}
				else {
					//The parameter is using output, so leave the parameter value to null.
					paramsValue[i] = null;
				}
			}
			return paramsValue;
		}

		public object GetValue(Flow flow) {
			object obj = null;
			if(target.isAssigned) {
				if(instance != null) {
					var targetInstance = instance.GetValue(flow);
					if(targetInstance == null) {
						throw new GraphException("The value of 'Instance' port is null", instance.node);
					}
					target.startTarget = targetInstance;
				}
				if(parameters?.Count > 0) {
					object[] paramsValue = null;
					if(!target.HasRefOrOut) {
						if(parameters.Count > 0) {
							paramsValue = new object[parameters.Count];
							for(int i = 0; i < paramsValue.Length; i++) {
								paramsValue[i] = parameters[i].input.GetValue(flow);
							}
#if UNITY_EDITOR
							//For easier logging.
							if(node != null &&
								target.targetType == MemberData.TargetType.Method &&
								target.startType == typeof(UnityEngine.Debug) &&
								target.methodInfo != null && target.methodInfo.Name.StartsWith("Log", System.StringComparison.Ordinal)) {
								if(paramsValue[0] != null) {
									paramsValue[0] = GraphException.GetMessage(paramsValue[0].ToString(), node, flow.target);
								}
								else {
									paramsValue[0] = GraphException.GetMessage("null", node, flow.target);
								}
							}
#endif
						}
						obj = target.Invoke(flow, paramsValue);
					}
					else {
						if(parameters.Count > 0) {
							paramsValue = new object[parameters.Count];
							for(int i = 0; i < paramsValue.Length; i++) {
								if(parameters[i].input != null) {
									paramsValue[i] = parameters[i].input.GetValue(flow);
								}
								else {
									//The parameter is using output, so leave the parameter value to null.
									paramsValue[i] = null;
								}
							}
						}
						obj = target.Invoke(flow, paramsValue);
						if(paramsValue != null) {
							for(int i = 0; i < paramsValue.Length; i++) {
								if(parameters[i].IsByRef) {
									if(parameters[i].input != null) {
										//Set the input port, if we're using input port.
										parameters[i].input.SetValue(flow, paramsValue[i]);
									}
									else {
										//In case use output, we set the output value instead.
										flow.SetPortData(parameters[i].output, paramsValue[i]);
									}
								}
							}
						}
					}
				}
				else {
					obj = target.Get(flow);
				}
				if(obj != null && initializers != null) {
					if(target.targetType == MemberData.TargetType.Values || target.targetType == MemberData.TargetType.Constructor) {
						var t = target.type ?? obj.GetType();
						if(obj is System.Collections.IList) {
							if(t.IsArray) {
								System.Array array = obj as System.Array;
								for(int i = 0; i < initializers.Count; i++) {
									var param = initializers[i];
									if(param == null)
										continue;
									array.SetValue(param.port.GetValue(flow), i);
								}
							}
							else {
								System.Collections.IList list = obj as System.Collections.IList;
								foreach(var param in initializers) {
									if(param == null)
										continue;
									list.Add(param.port.GetValue(flow));
								}
							}
						}
						else {
							foreach(var param in initializers) {
								if(param == null)
									continue;
								if(param.isComplexInitializer) {
									var method = t.GetMemberCached("Add") as MethodInfo;
									if(method != null) {
										method.InvokeOptimized(obj, param.elementInitializers.Select(e => e.port.GetValue(flow)).ToArray());
									}
								}
								else {
									var members = t.GetMember(param.name);
									if(members.Length == 0)
										continue;
									foreach(var member in members) {
										if(member is System.Reflection.FieldInfo) {
											var field = member as System.Reflection.FieldInfo;
											field.SetValueOptimized(obj, param.port.GetValue(flow));
											break;
										}
										else if(member is System.Reflection.PropertyInfo) {
											var prop = member as System.Reflection.PropertyInfo;
											prop.SetValueOptimized(obj, param.port.GetValue(flow));
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			return obj;
		}

		public void SetValue(Flow flow, object value) {
			if(instance != null) {
				var targetInstance = instance.GetValue(flow);
				if(targetInstance == null) {
					throw new Exception("The value of 'Instance' port is null");
				}
				target.startTarget = targetInstance;
			}
			if(parameters != null && parameters.Count > 0) {
				target.GetMembers();
				object[] paramsValue = null;
				if(!target.HasRefOrOut) {
					if(parameters.Count > 0) {
						paramsValue = new object[parameters.Count];
						for(int i = 0; i < paramsValue.Length; i++) {
							paramsValue[i] = parameters[i].input.GetValue(flow);
						}
					}
					target.Set(flow, value, paramsValue);
				}
				else {
					if(parameters.Count > 0) {
						paramsValue = new object[parameters.Count];
						for(int i = 0; i < paramsValue.Length; i++) {
							if(parameters[i].input != null) {
								paramsValue[i] = parameters[i].input.GetValue(flow);
							}
							else {
								//The parameter is using output, so leave the parameter value to null.
								paramsValue[i] = null;
							}
						}
					}
					target.Set(flow, value, paramsValue);
					if(paramsValue != null) {
						for(int i = 0; i < paramsValue.Length; i++) {
							if(parameters[i].IsByRef) {
								if(parameters[i].input != null) {
									//Set the input port, if we're using input port.
									parameters[i].input.SetValue(flow, paramsValue[i]);
								}
								else {
									//In case use output, we set the output value instead.
									flow.SetPortData(parameters[i].output, paramsValue[i]);
								}
							}
						}
					}
				}
			}
			else {
				target.Set(flow, value);
			}
		}

		public bool CanSetValue() {
			if(target != null && target.isAssigned) {
				return target.CanSetValue();
			}
			return false;
		}

		public bool CanGetValue() {
			if(target != null && target.isAssigned) {
				return target.CanGetValue();
			}
			return false;
		}
	}
}