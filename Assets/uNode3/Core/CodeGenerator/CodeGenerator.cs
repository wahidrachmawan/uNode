using MaxyGames.UNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MaxyGames {
	/// <summary>
	/// Class for Generating C# code from uNode with useful function for generating C# code more easier.
	/// </summary>
	public static partial class CG {
		#region Setup
		private static bool IsCanBeGrouped(FlowInput flow) {
			if(Nodes.IsStackOverflow(flow.node))
				return false;
			return !Nodes.HasStateFlowOutput(flow);
		}

		/// <summary>
		/// Reset the generator settings
		/// </summary>
		public static void ResetGenerator() {
			generatorData = new GData();
			graph = null;
			coNum = 0;
			InitData = new List<string>();
		}

		/// <summary>
		/// Generate new c#
		/// </summary>
		/// <param name="setting"></param>
		/// <returns></returns>
		public static GeneratedData Generate(GeneratorSetting setting) {
			//Wait other generation till finish before generate new script.
			if(isGenerating) {
				if(setting.isAsync) {
					//Wait until other generator is finished
					while(isGenerating) {
						uNodeThreadUtility.WaitOneFrame();
					}
				}
				else {
					//Update thread queue so the other generation will finish
					while(isGenerating) {
						uNodeThreadUtility.Update();
					}
				}
			}
			try {
				//Set max queue for async generation
				ThreadingUtil.SetMaxQueue(setting.maxQueue);
				//Mark is generating to be true so only one can generate the script at the same times.
				isGenerating = true;
				{//Change the global culture information, so that the parsing numeric values always uses dot.
					System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
					customCulture.NumberFormat.NumberDecimalSeparator = ".";
					System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
					if(setting.isAsync) {
						uNodeThreadUtility.Queue(() => {//This is for change culture in main thread.
							System.Globalization.CultureInfo customCulture2 = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
							customCulture2.NumberFormat.NumberDecimalSeparator = ".";
							System.Threading.Thread.CurrentThread.CurrentCulture = customCulture2;
						});
					}
				}
				//This is the current progress
				float progress = 0;
				float classCount = setting.types.Count != 0 ? setting.types.Count / setting.types.Count : 0;

				GeneratedData generatedData = new GeneratedData(setting);

				foreach(var classes in setting.types) {
					ResetGenerator();
					generatorData.setting = setting;
					if(classes == null)
						continue;
					if(classes is IGraph) {
						graph = classes as IGraph;
						graphSystem = ReflectionUtils.GetAttributeFrom<GraphSystemAttribute>(classes);
						if(classes is ITypeWithScriptData typeWithScriptData) {
							typeWithScriptData.ScriptData.unityObjects.Clear();
						}
						generationState.state = State.Classes;

						//class name.
						string className = null;
						int fieldCount = 0;
						int propCount = 0;
						int ctorCount = 0;
						ThreadingUtil.Do(() => {
							className = graph.GetFullGraphName().Split('.').LastOrDefault();
							if(string.IsNullOrEmpty(classes.name) && className == "_" + Mathf.Abs(classes.GetInstanceID())) {
								className = classes.name;
							}
							className = uNodeUtility.AutoCorrectName(className);
							generatorData.typeName = className;

							generatedData.classNames[graph] = className;
							//update progress bar
							setting.updateProgress?.Invoke(progress, "initializing class:" + className);
							//Initialize code gen for classes
							Initialize(out fieldCount, out propCount, out ctorCount);
						});
						if(fieldCount == 0)
							fieldCount = 1;
						if(propCount == 0)
							propCount = 1;
						if(ctorCount == 0)
							ctorCount = 1;
						int nodeCount = generatorData.allNode.Count != 0 ? generatorData.allNode.Count / generatorData.allNode.Count : 1;
						float childFill = ((nodeCount + fieldCount + propCount + ctorCount) / 4F / (classCount)) / 4;
						//Generate functions
						GenerateFunctions((prog, text) => {
							float p = progress + (prog * (childFill));
							if(setting.updateProgress != null)
								setting.updateProgress(p, text);
						});
						progress += childFill;
						//Generate properties
						string properties = GenerateProperties((prog, text) => {
							float p = progress + (prog * (childFill));
							if(setting.updateProgress != null)
								setting.updateProgress(p, text);
						});
						progress += childFill;
						//Generate constructors
						string constructors = GenerateConstructors((prog, text) => {
							float p = progress + (prog * (childFill));
							if(setting.updateProgress != null)
								setting.updateProgress(p, text);
						});
						progress += childFill;
						//Generate variables
						string variables = GenerateVariables((prog, text) => {
							float p = progress + (prog * (childFill));
							if(setting.updateProgress != null)
								setting.updateProgress(p, text);
						});
						progress += childFill;

						generationState.state = State.Classes;

						var classBuilder = new ClassData(classes, graphSystem) {
							name = className,
							variables = variables,
							properties = properties,
							constructors = constructors,
						};
						generatedData.RegisterClass(classes, classBuilder);
						generatorData.classData = classBuilder;

						#region Get / Set Optimizations
						if(setting.runtimeOptimization && (classes is IIndependentGraph)) {
							var variableDatas = generatorData.GetVariables().Where(v => v.isInstance && (v.modifier == null || !v.modifier.isPrivate));
							{//Set Variables
								List<(string, string)> list = new List<(string, string)>();
								foreach(var var in variableDatas) {
									list.Add(
										(
											Value(var.name),
											Flow(
												Set(var.name, Invoke(typeof(uNodeHelper), nameof(uNodeHelper.SetObject), var.name, "value", "op"))
											//GenerateSwitchStatement("op",
											//	cases: new[] {
											//		new KeyValuePair<string[], string>(
											//			new [] {
											//				ParseValue('+'),
											//				ParseValue('-'),
											//				ParseValue('/'),
											//				ParseValue('*'),
											//				ParseValue('%')
											//			},
											//			GenerateSetCode(var.name, GenerateInvokeCode(typeof(uNodeHelper), nameof(uNodeHelper.SetObject), var.name, "value", "op")).RemoveSemicolon()
											//		)
											//	},
											//	@default: GenerateSetCode(var.name, "value")
											//)
											)
										)
									);
								}
								if(list.Count > 0) {
									{
										var method = generatorData.AddMethod(
											nameof(IRuntimeClass.SetVariable),
											typeof(void),
											new[] {
										new MPData("Name", typeof(string)),
										new MPData("value", typeof(object)),
											}
										);
										method.modifier = new FunctionModifier() {
											Public = true,
											Override = true,
										};
										method.code = Flow(
											DoGenerateInvokeCode(nameof(IRuntimeClass.SetVariable), new[] { "Name", "value", Value('=') }).AddSemicolon()
										);
									}
									{
										var method = generatorData.AddMethod(
											nameof(IRuntimeClass.SetVariable),
											typeof(void),
											new[] {
										new MPData("Name", typeof(string)),
										new MPData("value", typeof(object)),
										new MPData("op", typeof(char)),
											}
										);
										method.modifier = new FunctionModifier() {
											Public = true,
											Override = true,
										};
										method.code = Flow(
											Set("value", Invoke(typeof(uNodeHelper), nameof(uNodeHelper.GetActualRuntimeValue), "value")),
											Switch("Name", list, FlowInvoke("base", nameof(IRuntimeClass.SetVariable), "Name", "value", "op"))
										);
									}
								}
							}
							{//Get Variables
								List<(string, string)> list = new List<(string, string)>();
								foreach(var var in variableDatas) {
									list.Add(
										(
											Value(var.name),
											Return(var.name)
										)
									);
								}
								if(list.Count > 0) {
									{
										var method = generatorData.AddMethod(
											nameof(IRuntimeClass.GetVariable),
											typeof(object),
											new[] {
											new MPData("Name", typeof(string)),
											}
										);
										method.modifier = new FunctionModifier() {
											Public = true,
											Override = true,
										};
										method.code = Flow(
											Switch("Name", list),
											Return(Invoke("base", nameof(IRuntimeClass.GetVariable), "Name"))
										);
									}
								}
							}
						}
						#endregion

						generationState.state = State.Function;
						//Generating Event Functions ex: State Machine, Coroutine nodes, etc.
						M_GenerateEventFunction();

						//From here this is the finising parts
						ThreadingUtil.Do(() => {
							if(generatorData.postManipulator != null) {
								generatorData.postManipulator(generatorData);
							}
						});

						var functionData = new StringBuilder();
						var additionalVariables = new StringBuilder();

						#region Events for State Graphs
						if(generatorData.coroutineEvent.Count > 0) {
							generationState.isStatic = false;
							generationState.state = State.Function;

							foreach(var p in generatorData.coroutineEvent) {
								var pair = p;
								if(!string.IsNullOrEmpty(pair.Value.variableName) && pair.Key != null) {
									ThreadingUtil.Queue((() => {
										additionalVariables.Append(
											CG.WrapWithInformation(
												DeclareVariable(
													typeof(Runtime.EventCoroutine),
													pair.Value.variableName,
													New(typeof(Runtime.EventCoroutine)),
													modifier: FieldModifier.PrivateModifier),
												pair.Key).AddLineInFirst()
										);
										string onStopAction = pair.Value.onStop;
										string invokeCode = pair.Value.customExecution == null ?
											KEY_coroutineEventCode.CGInvoke(null, generatorData.GetEventID(pair.Key)) :
											pair.Value.customExecution();
										string genData = null;
										if(!string.IsNullOrEmpty(onStopAction)) {
											genData = pair.Value.variableName.CGFlowInvoke(
												nameof(Runtime.EventCoroutine.Setup),
												Value(classes),
												invokeCode,
												Lambda(onStopAction)
											);
											//if(onStopAction.Contains("yield return")) {
											//	var uid = "stop_" + pair.Value.variableName;
											//	generatorData.InsertCustomUIDMethod("_StopCoroutineEvent", typeof(IEnumerable), uid, onStopAction);
											//	onStopAction = StaticInvoke("_StopCoroutineEvent", GeneratorExtensions.CGValue(uid));
											//	genData = pair.Value.variableName.CGFlowInvoke(
											//		nameof(Runtime.EventCoroutine.Setup),
											//		Value(classes),
											//		invokeCode,
											//		onStopAction
											//	);
											//} else {
											//}
										}
										else {
											genData = pair.Value.variableName.CGFlowInvoke(
												nameof(Runtime.EventCoroutine.Setup),
												Value(classes),
												invokeCode
											);
										}
										if(setting.debugScript && pair.Key is FlowInput port) {
											if(setting.debugPreprocessor)
												genData += "\n#if UNITY_EDITOR".AddLineInFirst();
											genData += DoGenerateInvokeCode(pair.Value + ".Debug", new string[] {
												Value(graph.GetGraphID()), 
												Value(port.node.id),
												port.isPrimaryPort ? Null : Value(port.id)
											}).AddSemicolon().AddLineInFirst();
											if(setting.debugPreprocessor)
												genData += "\n#endif".AddLineInFirst();
										}
										InitData.Add(WrapWithInformation(genData, pair.Key));
									}));
								}
							}
							ThreadingUtil.WaitQueue();

							if(InitData.Count > 0) {//Insert init code into Awake functions.
								string code = "";
								foreach(string s in InitData) {
									code += "\n" + s;
								}
								var method = generatorData.AddMethod("Awake", typeof(void), new Type[0]);
								method.code = code + method.code.AddLineInFirst();
							}
						}
						#endregion

						foreach(MData d in generatorData.methodData) {
							var data = d;
							ThreadingUtil.Queue(() => {
								generationState.isStatic = data.modifier != null && data.modifier.Static;
								if(functionData.Length > 0) {
									functionData.AppendLine();
									functionData.AppendLine();
								}
								functionData.Append(data.GenerateCode());
							});
						}
						ThreadingUtil.WaitQueue();

						if(additionalVariables.Length > 0) {
							classBuilder.variables += additionalVariables.ToString();
						}
						if(functionData.Length > 0) {
							classBuilder.functions += functionData.ToString();
						}
						//generate Nested Type
						if(classes is IGraphWithNestedTypes) {
							//TODO: fix me nested class
							//var nestedType = (classes as INestedClassSystem).NestedClass;
							//if (nestedType) {
							//	generationState.state = State.Classes;
							//	ThreadingUtil.Do(() => {
							//		GameObject targetObj = nestedType.gameObject;
							//		setting.updateProgress?.Invoke(progress, "Generating NestedType...");
							//		isGenerating = false;//This to prevent freeze
							//		var nestedData = Generate(new GeneratorSetting(targetObj, setting));//Start generating nested type
							//		classData += nestedData.FullTypeScript().AddLineInFirst().AddLineInFirst();
							//		//Restore to prev state
							//		isGenerating = true;
							//		generatorData.setting = setting;
							//	});
							//}
						}
						ThreadingUtil.Do(() => {
							if(generatorData.postGeneration != null) {
								generatorData.postGeneration(classBuilder);
							}
						});
					}
					else if(classes is EnumScript) {
						var enumData = classes as EnumScript;
						if(string.IsNullOrEmpty(enumData.ScriptName))
							continue;
						var modifier = new ClassModifier {
							Public = enumData.modifier.Public,
							Private = enumData.modifier.Private,
							Protected = enumData.modifier.Protected,
							Internal = enumData.modifier.Internal
						};

						string EL = null;
						foreach(var e in enumData.enumerators) {
							EL = EL.AddLineInEnd() + e.name + ",";
						}

						var classBuilder = new ClassData(classes, graphSystem) {
							name = enumData.ScriptName,
							modifier = modifier,
							keyword = "enum ",
							variables = EL,
						};
						generatedData.RegisterClass(classes, classBuilder);
						generatorData.classData = classBuilder;
						generatedData.classNames[enumData] = enumData.ScriptName;
					}
					else {
						throw new InvalidOperationException("Unsupported script type: " + classes.GetType());
					}
				}
				if(setting.types.Count == 0) {
					ResetGenerator();
					generatorData.setting = setting;
				}
				ThreadingUtil.Do(() => {
					//Initialize the generated data for futher use
					generatedData.errors = generatorData.errors;
					generatedData.InitOwner();
				});
				RegisterScriptHeader("#pragma warning disable");

				//Build the full script and mark the data to valid.
				generatedData.BuildScript();

				ThreadingUtil.Do(() => {
					//Finish generating scripts
					setting.updateProgress?.Invoke(1, "finish");
					OnSuccessGeneratingGraph?.Invoke(generatedData, setting);
				});
				//Ensure the generator data is clean.
				ResetGenerator();
				isGenerating = false;

				//Return the generated data
				return generatedData;
			}
			finally {
				isGenerating = false;
			}
		}
		#endregion

		#region Private Functions

		private static string GenerateInitializers(IEnumerable<MultipurposeMember.InitializerData> initializers, Type type) {
			return GenerateInitializers(initializers, !type.HasImplementInterface(typeof(ICollection<>)));
		}

		private static string GenerateInitializers(IEnumerable<MultipurposeMember.InitializerData> initializers, bool includeName) {
			string data = null;
			if(includeName) {
				foreach(var init in initializers) {
					if(init.port != null && init.port.isAssigned) {
						data = CG.Flow(data, CG.SetValue(init.name, GeneratePort(init.port)).Add(","));
					}
				}
			}
			else {
				foreach(var init in initializers) {
					if(init.isComplexInitializer) {
						data = CG.Flow(data, CG.WrapBraces(string.Join(", ", init.elementInitializers.Select(e => GeneratePort(e.port)))).Add(","));
					}
					else if(init.port != null && init.port.isAssigned) {
						data = CG.Flow(data, GeneratePort(init.port).Add(","));
					}
				}
			}
			return data;
		}

		private static Type GetCompatibilityType(Type type) {
			if(type is RuntimeType && type is not INativeMember) {
				if(type is IRuntimeType) {
					return (type as IRuntimeType).GetNativeType();
				}
				else if(type.IsCastableTo(typeof(MonoBehaviour))) {
					return typeof(RuntimeBehaviour);
				}
				else if(type.IsCastableTo(typeof(ScriptableObject))) {
					return typeof(BaseRuntimeAsset);
				}
				else if(type.IsInterface) {
					return typeof(IRuntimeClass);
				}
			}
			return type;
		}

		private static void GenerateFunctions(Action<float, string> updateProgress = null) {
			//if((runtimeUNode == null || runtimeUNode.eventNodes.Count == 0) && uNodeObject.Functions.Count == 0)
			//	return;
			float progress = 0;
			float count = 0;
			if(generatorData.allNode.Count > 0) {
				count = generatorData.allNode.Count / generatorData.allNode.Count;
			}
			generationState.state = State.Function;

			#region Generate Nodes
			generationState.isStatic = false;
			for(int i = 0; i < generatorData.allNode.Count; i++) {
				var node = generatorData.allNode[i];
				if(node == null)
					continue;
				ThreadingUtil.Queue(() => {
					if(node != null) {
						if(generatorData.initActionForNodes.TryGetValue(node, out var action)) {
							action();
						}
						//Skip if not flow node
						if(node.primaryFlowInput != null) {
							var func = node.GetObjectInParent<Function>();
							if(func != null) {
								generationState.isStatic = func.modifier.Static;
							}
							if(IsStateFlow(node.primaryFlowInput)) {
								isInUngrouped = true;
							}
							GeneratePort(node.primaryFlowInput);
							if(node.node is IEventGenerator) {
								(node.node as IEventGenerator).GenerateEventCode();
							}
							isInUngrouped = false;
							progress += count;
							if(updateProgress != null)
								updateProgress(progress / generatorData.allNode.Count, "generating node:" + node.name);
						}
						else if(node.node is IEventGenerator) {
							(node.node as IEventGenerator).GenerateEventCode();
							progress += count;
							if(updateProgress != null)
								updateProgress(progress / generatorData.allNode.Count, "generating node:" + node.name);
						}
					}
				});
			}
			ThreadingUtil.WaitQueue();
			generationState.isStatic = false;
			#endregion

			#region Generate Functions
			foreach(var f in graph.GetFunctions().ToArray()) {
				if(f == null)
					return;
				var function = f;
				ThreadingUtil.Queue(() => {
					try {
						generationState.isStatic = function.modifier.Static;
						List<AData> attribute = new List<AData>();
						if(function.attributes != null && function.attributes.Count > 0) {
							foreach(var a in function.attributes) {
								attribute.Add(TryParseAttributeData(a));
							}
						}
						MData mData = generatorData.GetMethodData(
							function.name,
							function.parameters.Select(i => i.type.type).ToArray(),
							function.genericParameters.Length
						);
						if(mData == null) {
							mData = new MData(
								function.name,
								function.returnType,
								function.parameters.Select(i => new MPData(i.name, i.type, i.refKind)).ToArray(),
								function.genericParameters.Select(i => new GPData(i.name, i.typeConstraint.type)).ToArray()
							);
							generatorData.methodData.Add(mData);
						}
						mData.modifier = function.modifier;
						mData.attributes = attribute;
						mData.summary = function.comment;
						mData.owner = function;
						if(function.LocalVariables.Any()) {
							mData.code += M_GenerateLocalVariable(function.LocalVariables).AddLineInFirst();
						}
						if(function.Entry != null && (mData.modifier == null || !mData.modifier.Abstract)) {
							mData.code += GeneratePort(function.Entry.exit).AddLineInFirst();
						}
					}
					catch(Exception ex) {
						UnityEngine.Debug.LogException(ex, graph as UnityEngine.Object);
					}
				});
			}
			ThreadingUtil.WaitQueue();
			#endregion

			#region Generate Event Nodes
			generationState.isStatic = false;
			for(int i = 0; i < generatorData.eventNodes.Count; i++) {
				BaseGraphEvent eventNode = generatorData.eventNodes[i];
				if(eventNode == null)
					continue;
				try {
					ThreadingUtil.Do(() => {
						var flow = eventNode.nodeObject.primaryFlowInput;
						if(flow != null) {
							GeneratePort(flow);
						}
					});
				}
				catch(Exception ex) {
					if(setting != null && setting.isAsync) {

					}
					else {
						if(!generatorData.hasError)
							UnityEngine.Debug.LogError("Error generate code from event: " + eventNode.GetTitle() + "\nFrom graph:" + graph + "\nError:" + ex.ToString(), graph as UnityEngine.Object);
						generatorData.hasError = true;
						throw;
					}
				}
			}
			#endregion
		}

		private static string M_GenerateLocalVariable(IEnumerable<Variable> localVariables) {
			string result = null;
			foreach(var vdata in localVariables) {
				if(IsInstanceVariable(vdata)) {
					continue;
				}
				else if(!vdata.resetOnEnter) {
					M_RegisterVariable(vdata, vdata).modifier.SetPrivate();
					continue;
				}
				//if(vdata.type.isAssigned && vdata.type.targetType == MemberData.TargetType.Type && vdata.value == null && vdata.type.startType != null && vdata.type.startType.IsValueType) {
				//	result += (ParseType(vdata.type) + " " + GetVariableName(vdata) + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
				//	continue;
				//}
				if(vdata.isOpenGeneric) {
					string vType = Type(vdata.type);
					if(vdata.defaultValue != null) {
						result += (vType + " " + GetVariableName(vdata) + $" = default({vType});").AddFirst("\n", !string.IsNullOrEmpty(result));
					}
					else {
						result += (vType + " " + GetVariableName(vdata) + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
					}
					continue;
				}
				else if(vdata.defaultValue == null) {
					var vType = Type(vdata.type);
					result += (vType + " " + GetVariableName(vdata) + $" = default({vType});").AddFirst("\n", !string.IsNullOrEmpty(result));
					continue;
				}
				result += (Type(vdata.type) + " " + GetVariableName(vdata) + " = " + Value(vdata.defaultValue) + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
			}
			return result;
		}

		private static void M_GenerateEventFunction() {
			List<string> CoroutineEventFunc = new List<string>();
			if(generatorData.stateNodes.Count > 0) {
				//Creating ActivateEvent for non grouped node
				for(int i = 0; i < generatorData.stateNodes.Count; i++) {
					var port = generatorData.stateNodes.ElementAt(i);
					if(port == null) //skip on node is not flow node.
						continue;
					var evt = GetCoroutineEvent(port);
					if(evt != null && evt.customExecution == null) {
						ThreadingUtil.Queue(() => {
							isInUngrouped = true;
							string generatedCode = GeneratePort(port);
							isInUngrouped = false;
							if(string.IsNullOrEmpty(generatedCode))
								return;
							if(!setting.fullComment) {
								generatedCode = "\n" + generatedCode;
							}
							var strs = generatedCode.Split('\n');
							int yieldCount = 0;
							int lastYieldIndex = 0;
							int lastContent = 0;
							for(int x = 0; x < strs.Length; x++) {
								if(strs[x].Contains("yield ")) {
									yieldCount++;
									lastYieldIndex = x;
								}
								if(string.IsNullOrWhiteSpace(strs[x]) == false)
									lastContent = x;
							}
							if(yieldCount == 0 || yieldCount == 1 && (lastYieldIndex == strs.Length - 1 || lastYieldIndex == lastContent)) 
							{
								//Automatically set state initialization ( for performance ) if it's supported.
								SetStateInitialization(port, CG.Routine(Lambda(generatedCode.Replace("yield ", "").AddTabAfterNewLine())));
								return;
							}
							string s = "case " + generatorData.GetEventID(port) + ": {" +
								generatedCode.AddTabAfterNewLine(1) + "\n}";
							CoroutineEventFunc.Add(s + "\nbreak;");
						});
					}
				}
				ThreadingUtil.WaitQueue();
			}
			if(generatorData.eventCoroutineData.Count > 0) {
				foreach(var pair in generatorData.eventCoroutineData) {
					string data = "case " + generatorData.GetEventID(pair.Key) + ": {" +
						(pair.Value.AddLineInFirst() + "\nbreak;").AddTabAfterNewLine(1) + "\n}";
					CoroutineEventFunc.Add(data);
				}
			}
			if(CoroutineEventFunc.Count > 0 || generatorData.coroutineEvent.Any(e => e.Value.customExecution == null)) {
				MData method = generatorData.AddMethod(KEY_coroutineEventCode, typeof(IEnumerable), new[] { new MPData("uid", typeof(int)) });
				method.code += "\nswitch(uid) {";
				foreach(string str in CoroutineEventFunc) {
					method.code += ("\n" + str).AddTabAfterNewLine(1);
				}
				foreach(var pair in generatorData.coroutineEvent) {
					if(pair.Value.customExecution != null)
						continue;
					string data = pair.Value.contents.AddFirst("\n");
					if(!string.IsNullOrEmpty(data)) {
						method.code += ("\ncase " + generatorData.GetEventID(pair.Key) + ": {" + data.AddTabAfterNewLine(1) + "\n}\nbreak;").AddTabAfterNewLine();
					}
				}
				method.code += "\n}\nyield break;";
			}
			if(generatorData.customUIDMethods.Count > 0) {
				foreach(var pair in generatorData.customUIDMethods) {
					foreach(var pair2 in pair.Value) {
						MData method = generatorData.AddMethod(pair.Key, pair2.Key, new[] { new MPData("name", typeof(string)) });
						method.code += "\nswitch(name) {";
						foreach(var pair3 in pair2.Value) {
							string data = pair3.Value.AddFirst("\n");
							method.code += ("\ncase \"" + pair3.Key + "\": {" + data.AddTabAfterNewLine(1) + "\n}\nbreak;").AddTabAfterNewLine();
						}
						method.code += "\n}";
						if(pair2.Key == typeof(IEnumerable) || pair2.Key == typeof(IEnumerator)) {
							method.code += "yield break;".AddLineInFirst();
						}
						else if(pair2.Key != typeof(void)) {
							method.code += ("return default(" + Type(pair2.Key) + ");").AddLineInFirst();
						}
					}
				}
			}
		}

		private static string GenerateVariables(Action<float, string> updateProgress = null) {
			if(generatorData.GetVariables().Count == 0)
				return null;
			float progress = 0;
			float count = generatorData.GetVariables().Count / generatorData.GetVariables().Count;
			string result = null;
			generationState.state = State.Classes;
			foreach(VData vdata in generatorData.GetVariables()) {
				if(!vdata.isInstance)
					continue;
				ThreadingUtil.Queue(() => {
					try {
						generationState.isStatic = vdata.IsStatic;
						string str = vdata.GenerateCode().AddFirst("\n", !string.IsNullOrEmpty(result));
						if(includeGraphInformation && vdata.reference is Variable) {
							str = WrapWithInformation(str, vdata.reference);
						}
						result += str;
						progress += count;
						if(updateProgress != null)
							updateProgress(progress / generatorData.GetVariables().Count, "generating variable");
					}
					catch(Exception ex) {
						if(setting != null && setting.isAsync) {
							generatorData.errors.Add(new Exception("Error on generating variable:" + vdata.name + "\nFrom graph:" + graph, ex));
							generatorData.errors.Add(ex);
							//In case async return error commentaries.
							result = "/*Error from variable: " + vdata.name + " */";
							return;
						}
						UnityEngine.Debug.LogError("Error on generating variable:" + vdata.name + "\nFrom graph:" + graph, graph as UnityEngine.Object);
						throw;
					}
				});
			}
			ThreadingUtil.WaitQueue();
			return result;
		}

		private static string GenerateProperties(Action<float, string> updateProgress = null) {
			if(generatorData.properties.Count == 0)
				return null;
			float progress = 0;
			float count = generatorData.properties.Count / generatorData.properties.Count;
			string result = null;
			generationState.state = State.Property;
			foreach(var prop in generatorData.properties) {
				if(prop == null || !prop.obj)
					continue;
				ThreadingUtil.Queue(() => {
					generationState.isStatic = prop.modifier != null && prop.modifier.Static;
					string str = prop.GenerateCode().AddFirst("\n", result != null);
					if(includeGraphInformation && prop.obj != null) {
						str = WrapWithInformation(str, prop.obj);
					}
					result += str;
					progress += count;
					if(updateProgress != null)
						updateProgress(progress / generatorData.properties.Count, "generating property");
				});
			}
			ThreadingUtil.WaitQueue();
			return result;
		}

		private static string GenerateConstructors(Action<float, string> updateProgress = null) {
			if(generatorData.constructors.Count == 0)
				return null;
			float progress = 0;
			float count = generatorData.constructors.Count / generatorData.constructors.Count;
			string result = null;
			generationState.isStatic = false;
			generationState.state = State.Constructor;
			for(int i = 0; i < generatorData.constructors.Count; i++) {
				var ctor = generatorData.constructors[i];
				if(ctor == null || !ctor.obj)
					continue;
				ThreadingUtil.Queue(() => {
					string str = ctor.GenerateCode().AddFirst("\n\n", result != null);
					if(includeGraphInformation && ctor.obj != null) {
						str = WrapWithInformation(str, ctor.obj);
					}
					result += str;
					progress += count;
					if(updateProgress != null)
						updateProgress(progress / generatorData.constructors.Count, "generating constructor");
				});
			}
			ThreadingUtil.WaitQueue();
			return result;
		}
		#endregion

		#region GetCorrectName
		/// <summary>
		/// Function to get correct code for Get correct name in MemberReflection
		/// </summary>
		/// <param name="mData"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		private static string GetCorrectName(
			MemberData mData,
			IList<MultipurposeMember.MParamInfo> parameters = null,
			IList<MultipurposeMember.InitializerData> initializer = null,
			object instance = null,
			Action<string, string> onEnterAndExit = null,
			bool autoConvert = false,
			bool setVariable = false) {

			MemberInfo[] memberInfo = null;
			switch(mData.targetType) {
				case MemberData.TargetType.None:
				case MemberData.TargetType.Null:
				case MemberData.TargetType.Type:
				case MemberData.TargetType.uNodeGenericParameter:
					break;
				default:
					memberInfo = mData.GetMembers(false);
					break;
				case MemberData.TargetType.Self: {
					if(instance == null && mData.instance == null) {
						throw new System.Exception("Variable with self target type can't have null value");
					}
					return Value(instance != null ? instance : mData.instance, setVariable: setVariable);
				}
				case MemberData.TargetType.Values: {
					string initData = null;
					if(initializer != null && mData.startType != null) {
						initData = GenerateInitializers(initializer, mData.startType);
					}
					return Value(mData.Get(null), initData);
				}
				case MemberData.TargetType.uNodeFunction: {
					string data = mData.startName;
					MemberDataUtility.GetItemName(mData.startItem, out var gType, out _);
					if(gType?.Length > 0) {
						data += String.Format("<{0}>", String.Join(", ", gType));
					}
					data += "(";
					{
						var func = mData.startItem.GetReferenceValue() as Function;
						if(func == null) {
							throw new Exception($"Function with name: {mData.startItem.GetActualName()} and id: {mData.startItem.reference.id} is missing");
						}
						for(int i = 0; i < func.parameters.Count; i++) {
							if(i != 0) {
								data += ", ";
							}
							data += M_GenerateParameter(parameters[i]);
						}
					}
					data += ")";
					return data;
				}
			}
			List<string> result;
			if(memberInfo != null) {
				result = new List<string>(memberInfo.Length);
			}
			else {
				result = new List<string>();
			}
			if(memberInfo != null && memberInfo.Length > 0) {
				int accessIndex = 0;
				string enter = null;
				string exit = null;
				for(int i = 0; i < memberInfo.Length; i++) {
					var member = memberInfo[i];
					if(member == null)
						throw new Exception("Incorrect/Unassigned Target");
					string genericData = null;
					if(mData.Items == null || i >= mData.Items.Length)
						break;
					MemberData.ItemData iData = mData.Items[i];
					if(mData.Items.Length > i + 1) {
						iData = mData.Items[i + 1];
					}
					if(iData != null) {
						MemberDataUtility.GetItemName(mData.Items[i + 1],
							out var genericType,
							out var paramsType);
						if(genericType.Length > 0) {
							if(mData.targetType != MemberData.TargetType.uNodeGenericParameter &&
								mData.targetType != MemberData.TargetType.Type) {
								genericData += string.Format("<{0}>", string.Join(", ", genericType)).Replace('+', '.');
							}
							else {
								genericData += string.Format("{0}", string.Join(", ", genericType)).Replace('+', '.');
							}
						}
					}
					bool isRuntime = member is IRuntimeMember && member is not INativeMember;
					if(isRuntime && !(member is IFakeMember)) {
						if(member is RuntimeField) {
							result.Add(GenerateGetRuntimeVariable(member as RuntimeField));
						}
						else if(member is RuntimeProperty) {
							result.Add(GenerateGetRuntimeProperty(member as RuntimeProperty));
						}
						else if(member is RuntimeMethod method) {
							ParameterInfo[] paramInfo = method.GetParameters();
							var datas = new MultipurposeMember.MParamInfo[paramInfo.Length];
							for(int index = 0; index < paramInfo.Length; index++) {
								datas[index] = parameters[accessIndex];
								accessIndex++;
							}
							result.Add(GenerateInvokeRuntimeMethod(method, datas, ref enter, ref exit, autoConvert || generatePureScript == false) + genericData);
						}
						else if(member is RuntimeConstructor ctor) {
							ParameterInfo[] paramInfo = ctor.GetParameters();
							var datas = new MultipurposeMember.MParamInfo[paramInfo.Length];
							for(int index = 0; index < paramInfo.Length; index++) {
								datas[index] = parameters[accessIndex];
								accessIndex++;
							}
							string ctorInit = ParseConstructorInitializer(ctor.DeclaringType, initializer);
							result.Add(GenerateInvokeRuntimeConstructor(ctor, datas, ref enter, ref exit, autoConvert || generatePureScript == false) + ctorInit + genericData);
						}
						else {
							throw new InvalidOperationException("Unsupported Runtime Member: " + member);
						}
					}
					else if(member is MethodInfo) {
						MethodInfo method = member as MethodInfo;
						ParameterInfo[] paramInfo = method.GetParameters();
						string[] parameterDatas;
						if(paramInfo.Length > 0) {
							if(parameters == null) {
								//Throw is parameter is null
								throw new ArgumentNullException(nameof(parameters), "The method does have parameters but there's no given parameter values");
							}
							parameterDatas = new string[paramInfo.Length];
							for(int index = 0; index < paramInfo.Length; index++) {
								var p = parameters[accessIndex];
								if(p.input != null) {
									string pData = null;
									if(paramInfo[index].ParameterType.IsByRef) {
										if(paramInfo[index].IsOut) {
											pData += "out ";
										}
										else if(paramInfo[index].IsIn) {
											//There's nothing todo for In modifier
										}
										else {
											pData += "ref ";
										}
									}
									if(pData != null) {
										if(debugScript && setting.debugValueNode) {
											setting.debugScript = false;
											pData += Value(p);
											setting.debugScript = true;
										}
										else {
											pData += Value(p);
										}
										if(pData == "out null") {//For fix error if the argument value is null on out parameter.
											pData = $"out {Type(paramInfo[index].ParameterType.GetElementType())} _";
										}
									}
									else {
										pData += Value(p);
									}
									parameterDatas[index] = pData;
								}
								else {
									parameterDatas[index] = M_GenerateParameter(p);
								}
								accessIndex++;
							}
						}
						else {
							parameterDatas = Array.Empty<string>();
						}

						//Auto resolve runtime generic method
						if(method is IGenericMethodWithResolver methodWithResolver && ReflectionUtils.IsNativeMember(method) == false) {
							var resolver = methodWithResolver.GetResolver();
							if(resolver != null) {
								resolver.GenerateCode(result, parameterDatas);
							}
							continue;
						}

						if(paramInfo.Length > 0) {
							string data = null;
							for(int index = 0; index < parameterDatas.Length; index++) {
								if(index != 0) {
									data += ", ";
								}
								data += parameterDatas[index];
							}

							if(member.Name == "Item" || member.Name == "get_Item") {
								if(isRuntime && member is IFakeMember) {
									if(generatePureScript && !(ReflectionUtils.GetMemberType(member) is IFakeMember)) {
										//Get indexer and convert to actual type
										result.Add(Convert("[" + data + "]", ReflectionUtils.GetMemberType(member)));
									}
									else if(!generatePureScript) {
										//Get indexer and convert to actual type
										result.Add(Convert("[" + data + "]", ReflectionUtils.GetMemberType(member), true));
									}
									else {
										//Get indexer
										result.Add("[" + data + "]");
									}
								}
								else {
									//Get indexer
									result.Add("[" + data + "]");
								}
							}
							else if(member.Name.StartsWith("set_", StringComparison.Ordinal)) {
								if(member.Name.Equals("set_Item") && method.GetParameters().Length == 2) {
									//Set indexer
									result.Add("[" + parameterDatas[0] + "] = " + parameterDatas[1]);
								}
								else {
									//Set property
									result.Add(member.Name.Replace("set_", "") + " = " + data + genericData);
								}
							}
							else if(member.Name.StartsWith("op_", StringComparison.Ordinal)) {
								#region Operators
								switch(member.Name) {
									default:
										result.Add(member.Name + genericData + "(" + data + ")");
										break;
									case "op_Addition":
										result.Add(parameterDatas[0] + "+" + parameterDatas[1]);
										break;
									case "op_Subtraction":
										result.Add(parameterDatas[0] + "-" + parameterDatas[1]);
										break;
									case "op_Division":
										result.Add(parameterDatas[0] + "/" + parameterDatas[1]);
										break;
									case "op_Multiply":
										result.Add(parameterDatas[0] + "*" + parameterDatas[1]);
										break;
									case "op_Modulus":
										result.Add(parameterDatas[0] + "%" + parameterDatas[1]);
										break;
									case "op_Equality":
										result.Add(parameterDatas[0] + "==" + parameterDatas[1]);
										break;
									case "op_Inequality":
										result.Add(parameterDatas[0] + "!=" + parameterDatas[1]);
										break;
									case "op_LessThan":
										result.Add(parameterDatas[0] + "<" + parameterDatas[1]);
										break;
									case "op_GreaterThan":
										result.Add(parameterDatas[0] + ">" + parameterDatas[1]);
										break;
									case "op_LessThanOrEqual":
										result.Add(parameterDatas[0] + "<=" + parameterDatas[1]);
										break;
									case "op_GreaterThanOrEqual":
										result.Add(parameterDatas[0] + ">=" + parameterDatas[1]);
										break;
									case "op_BitwiseAnd":
										result.Add(parameterDatas[0] + "&" + parameterDatas[1]);
										break;
									case "op_BitwiseOr":
										result.Add(parameterDatas[0] + "|" + parameterDatas[1]);
										break;
									case "op_LeftShift":
										result.Add(parameterDatas[0] + "<<" + parameterDatas[1]);
										break;
									case "op_RightShift":
										result.Add(parameterDatas[0] + ">>" + parameterDatas[1]);
										break;
									case "op_ExclusiveOr":
										result.Add(parameterDatas[0] + "^" + parameterDatas[1]);
										break;
									case "op_UnaryNegation":
										result.Add(parameterDatas[0] + "-" + parameterDatas[1]);
										break;
									case "op_UnaryPlus":
										result.Add(parameterDatas[0] + "+" + parameterDatas[1]);
										break;
									case "op_LogicalNot":
										result.Add(parameterDatas[0] + "!" + parameterDatas[1]);
										break;
									case "op_OnesComplement":
										result.Add(parameterDatas[0] + "~" + parameterDatas[1]);
										break;
									case "op_Increment":
										result.Add(parameterDatas[0] + "++" + parameterDatas[1]);
										break;
									case "op_Decrement":
										result.Add(parameterDatas[0] + "--" + parameterDatas[1]);
										break;
								}
								#endregion

								return string.Join(".", result.ToArray());
							}
							else if(member.Name.StartsWith("Get", StringComparison.Ordinal) &&
								method.GetParameters().Length == 1 &&
								(i > 0 && ReflectionUtils.GetMemberType(memberInfo[i - 1]).IsArray || i == 0 && mData.startType.IsArray)) {

								if(isRuntime && !ReflectionUtils.IsNativeType(method.ReturnType) && generatePureScript) {
									if(result.Count > 0) {
										//Get indexer and convert to actual type
										result[result.Count - 1] = result[result.Count - 1] + Convert("[" + data + "]", ReflectionUtils.GetMemberType(member), true);
									}
									else {
										//Get indexer and convert to actual type
										result.Add(Convert("[" + data + "]", ReflectionUtils.GetMemberType(member), true));
									}
								}
								else {
									if(result.Count > 0) {
										result[result.Count - 1] = result[result.Count - 1] + "[" + data + "]";
									}
									else {
										result.Add("[" + data + "]");
									}
								}
							}
							else if(member.Name.StartsWith("Set", StringComparison.Ordinal) &&
								method.GetParameters().Length == 2 && (i > 0 &&
								ReflectionUtils.GetMemberType(memberInfo[i - 1]).IsArray || i == 0 && mData.startType.IsArray)) {

								result.Add(member.Name.Replace("Set", "[" + parameterDatas[0] + "]") + " = " + parameterDatas[1]);
							}
							else {
								result.Add(member.Name + genericData + "(" + data + ")");
							}
						}
						else if(member.Name.StartsWith("get_", StringComparison.Ordinal)) {
							result.Add(member.Name.Replace("get_", "") + genericData);
						}
						else {
							if(i == memberInfo.Length - 1 && parameters != null && accessIndex < parameters.Count) {
								string data = null;
								for(int x = accessIndex; x < parameters.Count; x++) {
									if(x != accessIndex) {
										data += ", ";
									}
									var p = parameters[x];
									data += M_GenerateParameter(p);
								}
								result.Add(member.Name + genericData + "(" + data + ")");
							}
							else {
								result.Add(member.Name + genericData + "()");
							}
						}
					}
					else if(member is ConstructorInfo) {
						ConstructorInfo ctor = member as ConstructorInfo;
						ParameterInfo[] paramInfo = ctor.GetParameters();
						string ctorInit = ParseConstructorInitializer(ctor.DeclaringType, initializer);
						if(paramInfo.Length > 0) {
							string data = null;
							List<string> dataList = new List<string>();
							for(int index = 0; index < paramInfo.Length; index++) {
								var p = parameters[accessIndex];
								dataList.Add(M_GenerateParameter(p));
								accessIndex++;
							}
							for(int index = 0; index < dataList.Count; index++) {
								if(index != 0) {
									data += ", ";
								}
								data += dataList[index];
							}
							if(ctor.DeclaringType.IsArray) {
								if(result.Count > 0) {
									result.Add("(new " + Type(ctor.DeclaringType.GetElementType()) + "[" + data + "]" + ctorInit + ")");
								}
								else {
									result.Add("new " + Type(ctor.DeclaringType.GetElementType()) + "[" + data + "]" + ctorInit);
								}
							}
							else {
								if(result.Count > 0) {
									result.Add("(new " + Type(ctor.DeclaringType) + "(" + data + ")" + ctorInit + ")");
								}
								else {
									result.Add("new " + Type(ctor.DeclaringType) + "(" + data + ")" + ctorInit);
								}
							}
						}
						else {
							if(i == memberInfo.Length - 1 && parameters != null && accessIndex < parameters.Count) {
								string data = null;
								for(int x = accessIndex; x < parameters.Count; x++) {
									if(x != accessIndex) {
										data += ", ";
									}
									var p = parameters[x];
									data += M_GenerateParameter(p);
								}
								if(result.Count > 0) {
									result.Add("(new " + Type(ctor.DeclaringType) + "(" + data + ")" + ctorInit + ")");
								}
								else {
									result.Add("new " + Type(ctor.DeclaringType) + "(" + data + ")" + ctorInit);
								}
							}
							else {
								if(result.Count > 0) {
									result.Add("(new " + Type(ctor.DeclaringType) + "()" + ctorInit + ")");
								}
								else {
									result.Add("new " + Type(ctor.DeclaringType) + "()" + ctorInit);
								}
							}
						}
					}
					else {
						result.Add(member.Name + genericData);
					}
				}
				if(enter != null && onEnterAndExit != null)
					onEnterAndExit(enter, exit);
			}
			else if(mData.targetType == MemberData.TargetType.Constructor) {
				string ctorInit = ParseConstructorInitializer(mData.startType, initializer);
				result.Add("new " + Type(mData.startType) + "()" + ctorInit);
			}
			if(result.Count > 0) {
				string resultCode = string.Join(".", result.ToArray());
				if(result.Any(i => i.StartsWith("[", StringComparison.Ordinal) || i.StartsWith("(", StringComparison.Ordinal))) {
					resultCode = null;
					for(int i = 0; i < result.Count; i++) {
						resultCode += result[i];
						if(i + 1 != result.Count && !result[i + 1].StartsWith("[", StringComparison.Ordinal) && !result[i + 1].StartsWith("(", StringComparison.Ordinal)) {
							resultCode += ".";
						}
					}
				}
				string startData;
				if(Utility.IsContainOperatorCode(mData.name)) {
					throw new System.Exception("unsupported generating operator code in current context");
				}
				if(mData.targetType == MemberData.TargetType.Constructor) {
					return resultCode;
				}
				startData = ParseStartValue(mData, instance, setVariable: setVariable);
				if(string.IsNullOrEmpty(startData)) {
					return resultCode;
				}
				return startData.Add(".", !resultCode.StartsWith("[", StringComparison.Ordinal) && !resultCode.StartsWith("(", StringComparison.Ordinal)) + resultCode;
			}
			else if(mData.isAssigned) {
				string str = mData.name;
				if(mData.isAssigned) {
					str = null;
					if(mData.targetType == MemberData.TargetType.Constructor) {
						str += "new " + mData.type.PrettyName();
					}
					int accessIndex = 0;
					for(int i = 0; i < mData.Items.Length; i++) {
						if(i != 0 && (mData.targetType != MemberData.TargetType.Constructor)) {
							str += ".";
						}
						if(mData.targetType != MemberData.TargetType.uNodeGenericParameter &&
							mData.targetType != MemberData.TargetType.Type &&
							mData.targetType != MemberData.TargetType.Constructor) {
							str += mData.Items[i].GetActualName();
						}
						MemberData.ItemData iData = mData.Items[i];
						if(iData != null) {
							MemberDataUtility.GetItemName(mData.Items[i],
								out var genericType,
								out var paramsType);
							if(genericType.Length > 0) {
								if(mData.targetType != MemberData.TargetType.uNodeGenericParameter &&
									mData.targetType != MemberData.TargetType.Type) {
									str += string.Format("<{0}>", string.Join(", ", genericType));
								}
								else {
									str += string.Format("{0}", string.Join(", ", genericType));
								}
							}
							if(paramsType.Length > 0 ||
								mData.targetType == MemberData.TargetType.uNodeFunction ||
								mData.targetType == MemberData.TargetType.uNodeConstructor ||
								mData.targetType == MemberData.TargetType.Constructor ||
								mData.targetType == MemberData.TargetType.Method && !mData.isDeepTarget) {
								List<string> dataList = new List<string>();
								var func = mData.startItem.GetReferenceValue() as Function;
								for(int index = 0; index < paramsType.Length; index++) {
									var p = parameters[accessIndex];
									dataList.Add(M_GenerateParameter(p));
									accessIndex++;
								}
								str += string.Format("({0})", string.Join(", ", dataList.ToArray()));
							}
						}
					}
				}
				else if(mData.isAssigned) {
					switch(mData.targetType) {
						case MemberData.TargetType.Constructor:
							return "new " + mData.type.PrettyName() + "()";
					}
				}
				string nextNames = str;
				var strs = nextNames.CGSplitMember();
				if(strs.Count > 0) {
					strs.RemoveAt(0);
				}
				nextNames = string.Join(".", strs.ToArray());
				if(nextNames.StartsWith(".", StringComparison.Ordinal)) {
					nextNames = nextNames.Remove(0, 1);
				}
				str = ParseStartValue(mData, instance, setVariable: setVariable).Add(".", !string.IsNullOrEmpty(nextNames)) + nextNames;
				if(str.IndexOf("get_", StringComparison.Ordinal) >= 0) {
					str = str.Replace("get_", "");
				}
				else if(str.IndexOf("set_", StringComparison.Ordinal) >= 0) {
					str = str.Replace("set_", "");
				}
				//if(str.Contains("Item")) {
				//	str = str.Replace(".Item", "[]");
				//}
				return str;
			}
			return null;
		}

		private static string M_GenerateParameter(MultipurposeMember.MParamInfo parameter) {
			if(parameter.input != null) {
				string result = null;
				if(parameter.IsByRef) {
					if(parameter.IsOut) {
						result += "out ";
					}
					else {
						result += "ref ";
					}
				}
				if(debugScript && setting.debugValueNode) {
					setting.debugScript = false;
					result += Value(parameter);
					setting.debugScript = true;
				}
				else {
					result += Value(parameter);
				}
				return result;
			}
			else {
				if(parameter.output.hasValidConnections) {
					var vData = GetVariableData(parameter.output);
					if(vData == null) {
						throw new Exception($"Unregistered output port: {parameter.output.name} on node: {parameter.output.node.GetTitle()}");
					}
					if(vData.isInstance) {
						return "out " + vData.name;
					}
					else {
						return "out var " + vData.name;
					}
				}
				else {
					return "out _";
				}
			}
		}
		#endregion

		#region Parse Type
		/// <summary>
		/// Function to get correct code for type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Type(Type type) {
			if(type == null)
				return null;
			//if(isGenerating == false) {
			//	//In case it is not generaing any graph
			//	if(type.IsGenericType) {
			//		if(type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
			//			return string.Format("{0}?", Type(Nullable.GetUnderlyingType(type)));
			//		}
			//		else {
			//			return DoParseGenericType(type, type.GetGenericArguments().Select(a => Type(a)));
			//		}
			//	}
			//	else if(type.IsArray) {
			//		return Type(type.GetElementType()) + "[]";
			//	}
			//	return type.FullName.Replace('+', '.');
			//}
			if(generatorData.typesMap.TryGetValue(type, out var typeResult)) {
				return typeResult;
			}
			if(type is RuntimeType && type is not INativeMember) {
				var runtimeType = type as RuntimeType;
				if(!generatePureScript) {
					RegisterUsingNamespace("MaxyGames.UNode");
					if(runtimeType is RuntimeGraphType graphType) {
						if(graphType.target is IClassDefinition classDefinition) {
							return Type(classDefinition.GetModel().ProxyScriptType);
						}
						else if(graphType.IsInterface) {
							return Type(typeof(IRuntimeClass));
						}
						else {
							throw new NotImplementedException();
						}
					}
					else if(type is IFakeMember) {
						string result = DoParseType(type);
						generatorData.typesMap.Add(type, result);
						return result;
					}
					throw new Exception($"Unsupported RuntimeType: {runtimeType.FullName}, {runtimeType.GetType()}");
				}
				else if(setting.fullTypeName) {
					return runtimeType.FullName;
				}
				else if(type is IFakeMember) {
					string result = DoParseType(type);
					generatorData.typesMap.Add(type, result);
					return result;
				}
				if(setting.nameSpace != type.Namespace) {
					RegisterUsingNamespace(type.Namespace);
				}
				return runtimeType.Name;
				//return runtimeType.FullName;
			}
			string str = DoParseType(type);
			generatorData.typesMap.Add(type, str);
			return str;
		}

		private static string DeclareType(Type type) {
			if(ReflectionUtils.IsNativeType(type) == false) {
				if(type is IFakeType) {
					//If it is a fake type and not native type
					type = (type as IFakeType).GetNativeType();
				}
			}
			return Type(type);
		}

		private static string DoParseGenericType(Type type, IEnumerable<string> parameters) {
			string typeName = type.GetGenericTypeDefinition().FullName.Replace('+', '.').Split('`')[0];
			if(/*isGenerating &&*/ !setting.fullTypeName && setting.usingNamespace.Contains(type.Namespace)) {
				string result = typeName.Remove(0, type.Namespace.Length + 1);
				string firstName = result.Split('.')[0];
				bool flag = false;
				generatorData.ValidateTypes(type.Namespace, setting.usingNamespace, t => {
					if(t.IsGenericType && t.GetGenericArguments().Length == type.GetGenericArguments().Length && t.Name.Equals(firstName)) {
						flag = true;
						return true;
					}
					return false;
				});
				if(!flag) {
					typeName = result;
				}
			}
			return string.Format("{0}<{1}>", typeName, string.Join(", ", parameters));
		}

		private static string DoParseType(Type type) {
			if(type.IsGenericType) {
				if(type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
					return string.Format("{0}?", Type(Nullable.GetUnderlyingType(type)));
				}
				else {
					return DoParseGenericType(type, type.GetGenericArguments().Select(a => Type(a)));
				}
			}
			else if(type.IsArray) {
				return Type(type.GetElementType()) + "[]";
			}
			//if(string.IsNullOrEmpty(type.FullName)) {
			//	return type.Name;
			//}
			if(setting.fullTypeName) {
				return type.FullName.Replace('+', '.');
			}
			if(setting.usingNamespace.Contains(type.Namespace)) {
				string result = type.FullName.Replace('+', '.').Remove(0, type.Namespace.Length + 1);
				string firstName = result.Split('.')[0];
				generatorData.ValidateTypes(type.Namespace, setting.usingNamespace, t => {
					if(t.Name.Equals(firstName, StringComparison.Ordinal)) {
						result = type.FullName.Replace('+', '.');
						return true;
					}
					return false;
				});
				return result;
			}
			return type.FullName.Replace('+', '.');
		}

		/// <summary>
		/// Function to get correct code for type
		/// </summary>
		/// <param name="fullTypeName"></param>
		/// <returns></returns>
		public static string ParseType(string fullTypeName) {
			if(!string.IsNullOrEmpty(fullTypeName)) {
				Type type = TypeSerializer.Deserialize(fullTypeName, false);
				if(type != null) {
					return Type(type);
				}
				else {
					if(fullTypeName.Contains("`")) {
						string[] data1 = fullTypeName.Split(new char[] { '`' }, 2);
						if(data1.Length == 2) {
							int deepLevel = 0;
							int step = -1;
							int gLength = 0;
							bool skip = false;
							List<char> listChar = new List<char>();
							List<string> gTypes = new List<string>();
							for(int i = 0; i < data1[1].Length; i++) {
								char c = data1[1][i];
								if(skip) {
									//Continue skip until end of block
									if(c != ']') {
										continue;
									}
									else {
										skip = false;
									}
								}
								if(c == '[') {
									if(deepLevel == 0 && listChar.Count > 0) {
										gLength = int.Parse(string.Join("", new string[] { new string(listChar.ToArray()) }));
									}
									deepLevel++;
									if(deepLevel >= 3) {
										listChar.Add(c);
									}
									else {
										listChar.Clear();
									}
								}
								else if(c == ']') {
									if(deepLevel == 2) {
										//UnityEngine.Debug.Log(string.Join("", listChar.ToArray()));
										var cType = ParseType(string.Join("", new string[] { new string(listChar.ToArray()) }));
										if(cType == null)
											return null;
										//UnityEngine.Debug.Log(cType);
										gTypes.Add(cType);
										//Debug.Log(string.Join("", listChar.ToArray()).Split(',')[0]);
										listChar.Clear();
									}
									else if(deepLevel >= 3) {
										listChar.Add(c);
									}
									else if(deepLevel == 1) {//An array handling
										step++;
									}
									deepLevel--;
								}
								else {
									if(c == ',' && deepLevel == 2) {
										skip = true;
									}
									else {
										listChar.Add(c);
									}
								}
							}
							if(gLength == 0) {
								return fullTypeName;
							}
							//var dType = ParseType(data1[0] + "`" + gLength.ToString());
							//if(dType == null) {//Fallback for fail deserialization
							//	return fullTypeName;
							//}
							var result = data1[0] + "<" + string.Join(", ", gTypes) + ">";
							while(step > 0) {
								result += "[]";
								step--;
							}
							return result;
						}
					}
				}
			}
			return fullTypeName;
		}

		/// <summary>
		/// Function to get correct code for type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Type(MemberData type) {
			if(!object.ReferenceEquals(type, null)) {
				if(type.isAssigned) {
					if(type.targetType == MemberData.TargetType.Type) {
						object o = type.Get(null);
						if(o is Type) {
							return Type(o as Type);
						}
						if(type.Items?.Length > 0) {
							string data = null;
							MemberDataUtility.GetItemName(
								type.startItem,
								out var gType,
								out var pType);
							if(gType.Length > 0) {
								data += String.Format("{0}", String.Join(", ", gType));
							}
							if(data == null) {
								if(type.StartSerializedType.type != null) {
									data = Type(type.StartSerializedType.type);
								}
								else {
									data = ParseType(type.StartSerializedType.typeName);
								}
							}
							return data;
						}
						else if(type.StartSerializedType.type != null) {
							return Type(type.StartSerializedType.type);
						}
						else {
							return ParseType(type.StartSerializedType.typeName);
						}
					}
					else if(type.targetType == MemberData.TargetType.Null) {
						return "null";
					}
					else if(type.targetType == MemberData.TargetType.uNodeGenericParameter) {
						if(type.Items?.Length > 0) {
							string data = null;
							MemberDataUtility.GetItemName(
								type.startItem,
								out var gType,
								out var pType);
							if(gType.Length > 0) {
								data += String.Format("{0}", String.Join(", ", gType));
							}
							return data;
						}
						return type.name;
					}
					else if(type.targetType == MemberData.TargetType.uNodeParameter) {
						return Type(type.type);
					}
					else if(type.targetType == MemberData.TargetType.uNodeType) {
						return Type(type.type);
					}
					else {
						throw new System.Exception("Unsupported target type for parse to type");
					}
				}
				else {
					throw new System.Exception("Unassigned variable");
				}
			}
			return null;
		}
		#endregion

		#region Parse Value
		/// <summary>
		/// Are the member can be generate.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static bool CanParseValue(MemberData member) {
			if(!object.ReferenceEquals(member, null)) {
				if(member.isAssigned) {
					if(member.isStatic) {
						return true;
					}
					else if(graph != null && member.GetInstance() is IGraph g && g == graph) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.Constructor) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.Method) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.Field) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.Property) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.Type) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.Null) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.None) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.Self) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.uNodeParameter) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.uNodeGenericParameter) {
						return true;
					}
					else if(member.targetType == MemberData.TargetType.uNodeFunction) {
						return true;
					}
				}
			}
			return false;
		}

		private static string GenerateGetRuntimeInstance(object instance, RuntimeType runtimeType) {
			RegisterUsingNamespace("MaxyGames.UNode");
			if(generatePureScript) {
				if(instance == null) {
					return DoGenerateInvokeCode(
						nameof(Extensions.ToRuntimeInstance),
						new string[0],
						new string[] { runtimeType.FullName }
					);
				}
				return Value(instance).CGAccess(DoGenerateInvokeCode(
					nameof(Extensions.ToRuntimeInstance),
					new string[0],
					new string[] { runtimeType.FullName })
				);
			}
			else {
				//Type type = typeof(IRuntimeClass);
				//if(runtimeType is RuntimeGraphType graphType) {
				//	if(graphType.target is IClassComponent) {
				//		type = typeof(RuntimeComponent);
				//	} else if(graphType.target is IClassAsset) {
				//		type = typeof(BaseRuntimeAsset);
				//	}
				//} else if(runtimeType is RuntimeGraphInterface) {
				//	type = typeof(IRuntimeClass);
				//}
				if(instance == null) {
					return DoGenerateInvokeCode(
						nameof(Extensions.ToRuntimeInstance),
						new string[] { runtimeType.FullName.AddFirst(KEY_runtimeInterfaceKey, runtimeType.IsInterface).CGValue() }
					);
				}
				return Value(instance).CGAccess(
					DoGenerateInvokeCode(
						nameof(Extensions.ToRuntimeInstance),
						new string[] { runtimeType.FullName.AddFirst(KEY_runtimeInterfaceKey, runtimeType.IsInterface).CGValue() }
					)
				);
			}
		}

		private static string GenerateGetGeneratedComponent(object instance, RuntimeType runtimeType) {
			if(!runtimeType.IsSubclassOf(typeof(Component))) {
				return GenerateGetRuntimeInstance(instance, runtimeType);
			}
			if(generatePureScript) {
				if(instance == null) {
					return DoGenerateInvokeCode(
						nameof(uNodeHelper.GetGeneratedComponent),
						new string[0],
						new string[] { runtimeType.Name }
					);
				}
				return Value(instance).CGAccess(
					DoGenerateInvokeCode(
						nameof(uNodeHelper.GetGeneratedComponent),
						new string[0],
						new string[] { runtimeType.Name }
					)
				);
			}
			else {
				RegisterUsingNamespace("MaxyGames.UNode");
				if(instance == null) {
					return DoGenerateInvokeCode(
						nameof(uNodeHelper.GetGeneratedComponent),
						new string[] { runtimeType.Name.AddFirst(KEY_runtimeInterfaceKey, runtimeType.IsInterface).CGValue()
					});
				}
				return Value(instance).CGFlowInvoke(
					nameof(uNodeHelper.GetGeneratedComponent),
					runtimeType.Name.AddFirst(KEY_runtimeInterfaceKey, runtimeType.IsInterface).CGValue()
				);
			}
		}

		private static string GenerateGetRuntimeVariable(RuntimeField field) {
			if(generatePureScript && !(field.DeclaringType is IFakeMember)) {
				return field.Name;
			}
			else {
				return DoGenerateInvokeCode(nameof(IRuntimeClass.GetVariable), new string[] { field.Name.CGValue() }, new Type[] { field.FieldType });
			}
		}

		private static string GenerateGetRuntimeProperty(RuntimeProperty property) {
			if(generatePureScript && !(property.DeclaringType is IFakeMember)) {
				return property.Name;
			}
			else {
				return DoGenerateInvokeCode(nameof(IRuntimeClass.GetProperty), new string[] { property.Name.CGValue() }, new Type[] { property.PropertyType });
			}
		}

		private static string GenerateInvokeRuntimeConstructor(RuntimeConstructor ctor, MultipurposeMember.MParamInfo[] parameters, ref string enter, ref string exit, bool autoConvert = false) {
			var paramInfo = ctor.GetParameters();
			string data = string.Empty;
			if(paramInfo.Length > 0) {
				List<string> dataList = new List<string>();
				for(int index = 0; index < paramInfo.Length; index++) {
					var p = parameters[index];
					string pData = null;
					if(p.output != null) {
						if(p.output.isConnected) {
							pData = "out " + RegisterLocalVariable(p.name, p.type, null, reference: p.output);
						}
						else {
							pData = "out _";
						}
					}
					else {
						if(paramInfo[index].IsOut) {
							pData += "out ";
						}
						else if(paramInfo[index].IsIn) {
							//There's nothing todo for In modifier
						}
						else if(paramInfo[index].ParameterType.IsByRef) {
							pData += "ref ";
						}
						if(pData != null) {
							bool correct = true;
							if(p.type != null && p.type.IsValueType) {
								//TODO: fix me
								//MemberInfo[] MI = p.GetMembers();
								//if (MI != null && MI.Length > 1 && ReflectionUtils.GetMemberType(MI[MI.Length - 2]).IsValueType) {
								//	string varName = GenerateVariableName("tempVar");
								//	var pVal = Value(p);
								//	pData += varName + "." + pVal.Remove(pVal.IndexOf(ParseStartValue(p)), ParseStartValue(p).Length + 1).CGSplitMember().Last();
								//	if (pVal.LastIndexOf(".") >= 0) {
								//		pVal = pVal.Remove(pVal.LastIndexOf("."));
								//	}
								//	enter += Type(ReflectionUtils.GetMemberType(MI[MI.Length - 2])) + " " + varName + " = " + pVal + ";\n";
								//	exit += pVal + " = " + varName + ";";
								//	correct = false;
								//}
							}
							if(correct) {
								if(debugScript && setting.debugValueNode) {
									setting.debugScript = false;
									pData += Value(p);
									setting.debugScript = true;
								}
								else {
									pData += Value(p);
								}
							}
						}
						else {
							pData += Value(p);
						}
					}
					dataList.Add(pData);
				}
				for(int index = 0; index < dataList.Count; index++) {
					if(index != 0) {
						data += ", ";
					}
					data += dataList[index];
				}
			}
			if(generatePureScript) {
				return New(ctor.DeclaringType, data);
			}
			else {
				RegisterUsingNamespace("MaxyGames.UNode");
				if(paramInfo.Length == 0) {
					if(ctor.owner is RuntimeGraphType runtimeType && runtimeType.target is IClassDefinition definition) {
						return CG.Invoke(definition.GetModel().GetType(), nameof(ClassObjectModel.Create), Value(definition.FullGraphName));
					} else {
						throw new InvalidOperationException($"Unsupported Runtime Type ({ctor.owner}) for creating value at runtime.");
					}
				}
				else {
					throw new InvalidOperationException();
					//string paramValues = MakeArray(typeof(object), data);
					//var paramTypes = paramInfo.Select(p => p.ParameterType).ToArray();
					//var result = DoGenerateInvokeCode(
					//	nameof(BaseRuntimeBehaviour.InvokeFunction),
					//	new string[] {
					//		ctor.Name.CGValue(),
					//		paramTypes.CGValue(),
					//		paramValues });
					//if(autoConvert) {
					//	result = result.CGConvert(ctor.DeclaringType, true);
					//}
					//return result;
				}
			}
		}

		private static string GenerateInvokeRuntimeMethod(RuntimeMethod method, MultipurposeMember.MParamInfo[] parameters, ref string enter, ref string exit, bool autoConvert = false) {
			var paramInfo = method.GetParameters();
			string data = string.Empty;
			if(paramInfo.Length > 0) {
				List<string> dataList = new List<string>();
				for(int index = 0; index < paramInfo.Length; index++) {
					var p = parameters[index];
					string pData = null;
					if(p.output != null) {
						if(p.output.isConnected) {
							pData = "out " + RegisterLocalVariable(p.name, p.type, null, reference: p.output);
						}
						else {
							pData = "out _";
						}
					}
					else {
						if(paramInfo[index].ParameterType.IsByRef) {
							if(paramInfo[index].IsOut) {
								pData += "out ";
							}
							else if(paramInfo[index].IsIn) {
								//There's nothing todo for In modifier
							}
							else {
								pData += "ref ";
							}
						}
						if(pData != null) {
							bool correct = true;
							if(p.type != null && p.type.IsValueType) {
								//TODO: fix me
								//MemberInfo[] MI = p.GetMembers();
								//if (MI != null && MI.Length > 1 && ReflectionUtils.GetMemberType(MI[MI.Length - 2]).IsValueType) {
								//	string varName = GenerateVariableName("tempVar");
								//	var pVal = Value(p);
								//	pData += varName + "." + pVal.Remove(pVal.IndexOf(ParseStartValue(p)), ParseStartValue(p).Length + 1).CGSplitMember().Last();
								//	if (pVal.LastIndexOf(".") >= 0) {
								//		pVal = pVal.Remove(pVal.LastIndexOf("."));
								//	}
								//	enter += Type(ReflectionUtils.GetMemberType(MI[MI.Length - 2])) + " " + varName + " = " + pVal + ";\n";
								//	exit += pVal + " = " + varName + ";";
								//	correct = false;
								//}
							}
							if(correct) {
								if(debugScript && setting.debugValueNode) {
									setting.debugScript = false;
									pData += Value(p);
									setting.debugScript = true;
								}
								else {
									pData += Value(p);
								}
							}
						}
						else {
							pData += Value(p);
						}
					}
					dataList.Add(pData);
				}
				for(int index = 0; index < dataList.Count; index++) {
					if(index != 0) {
						data += ", ";
					}
					data += dataList[index];
				}
			}
			if(generatePureScript) {
				return method.Name + "(" + data + ")";
			}
			else {
				RegisterUsingNamespace("MaxyGames.UNode");
				if(paramInfo.Length == 0) {
					var result = DoGenerateInvokeCode(
						nameof(IRuntimeClass.InvokeFunction),
						new string[] {
							method.Name.CGValue(),
							"null"
						});
					if(autoConvert && method.ReturnType != typeof(void)) {
						result = result.CGConvert(method.ReturnType, true);
					}
					return result;
				}
				else if(paramInfo.Any(p => p.ParameterType is RuntimeType && p.ParameterType is not INativeMember)) {
					var func = (method as IRuntimeMemberWithRef).GetReferenceValue() as Function;
					var graph = (method.owner as IRuntimeMemberWithRef).GetReferenceValue() as IGraph;
					var uid = graph.GetFullGraphName();
					string paramValues = MakeArray(typeof(object), data);
					var result = DoGenerateInvokeCode(
						nameof(Extensions.InvokeFunctionByID),
						new string[] {
							uid.CGValue(),
							func.id.CGValue(),
							paramValues });
					if(autoConvert && method.ReturnType != typeof(void)) {
						result = result.CGConvert(method.ReturnType, true);
					}
					return result;
				}
				else {
					string paramValues = MakeArray(typeof(object), data);
					var paramTypes = paramInfo.Select(p => p.ParameterType).ToArray();
					var result = DoGenerateInvokeCode(
						nameof(IRuntimeClass.InvokeFunction),
						new string[] {
							method.Name.CGValue(),
							paramTypes.CGValue(),
							paramValues });
					if(autoConvert && method.ReturnType != typeof(void)) {
						result = result.CGConvert(method.ReturnType, true);
					}
					return result;
				}
			}
		}

		/// <summary>
		/// Return full name of the member.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static string Nameof(MemberData member) {
			string name = ParseStartValue(member);
			var path = member.Items.Select(n => n.GetActualName()).ToArray();
			for(int i = 1; i < path.Length; i++) {
				name += "." + path[i];
			}
			return name;
		}

		/// <summary>
		/// Return start name of member.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		private static string ParseStartValue(MemberData member, object instance = null, bool setVariable = false) {
			if(!object.ReferenceEquals(member, null)) {
				if(member.isAssigned) {
					if(member.isStatic) {
						var type = member.startType;
						if(type is RuntimeGraphType graphType && graphType.IsSingleton) {
							if(generatePureScript) {
								return typeof(GraphSingleton).CGInvoke(nameof(GraphSingleton.GetInstance), new Type[] { type }, null);
							}
							else {
								return typeof(GraphSingleton).CGInvoke(nameof(GraphSingleton.GetInstance), graphType.FullName.CGValue());
							}
						}
						return Type(type);
					}
					else if(member.targetType == MemberData.TargetType.uNodeParameter) {
						return member.startName;
					}
					else if(member.targetType == MemberData.TargetType.uNodeGenericParameter) {
						return "typeof(" + member.startName + ")";
					}
					else if(member.targetType == MemberData.TargetType.uNodeVariable) {
						var variable = member.startItem.GetReferenceValue() as Variable;
						if(variable != null) {
							return GetVariableName(variable);
						}
						else {
							member.startItem.GetActualName();
						}
					}
					else if(member.targetType == MemberData.TargetType.uNodeProperty) {
						var property = member.startItem.GetReferenceValue() as Property;
						if(property != null) {
							return property.name;
						}
						else {
							member.startItem.GetActualName();
						}
					}
					else if(member.targetType == MemberData.TargetType.uNodeFunction) {
						var function = member.startItem.GetReferenceValue() as Function;
						if(function != null) {
							return function.name;
						}
						else {
							member.startItem.GetActualName();
						}
					}
					else if(member.targetType == MemberData.TargetType.uNodeLocalVariable) {
						var v = member.startItem.GetReferenceValue() as Variable;
						if(v != null) {
							if(isInUngrouped) {
								return RegisterVariable(v);
							}
							return GetVariableName(v);
						}
					}
					else if(instance != null || member.instance != null) {
						var mInstance = instance ?? member.instance;

						if(mInstance is IGraph root && root == graph) {
							if(member.startType is RuntimeType runtimeType && runtimeType is not INativeMember) {
								var runtimeInstance = ReflectionUtils.GetActualTypeFromInstance(mInstance, true);
								if(runtimeType is RuntimeGraphType) {
									if(runtimeInstance != runtimeType) {
										return "this".CGAccess(GenerateGetGeneratedComponent(null, runtimeType));
									}
								}
								else if(runtimeType.IsInterface) {
									if(runtimeType != runtimeInstance) {
										if(runtimeType.GetHashCode() == runtimeInstance.GetHashCode()) {
											if(generatePureScript) {
												return Convert("this", runtimeType);
											}
										}
										else if(!runtimeInstance.IsCastableTo(runtimeType)) {
											return "this".CGAccess(GenerateGetGeneratedComponent(null, runtimeType));
										}
										// if(runtimeInstance == typeof(GameObject) || runtimeInstance.IsCastableTo(typeof(Component))) {
										// } else {
										// 	throw new Exception($"Cannot convert type from: '{runtimeType.FullName}' to '{runtimeInstance.FullName}'");
										// }
									}
								}
								else {
									throw new Exception($"Unsupported RuntimeType: {runtimeType.FullName}");
								}
							}
							//switch(member.targetType) {
							//	case MemberData.TargetType.Constructor:
							//	case MemberData.TargetType.Event:
							//	case MemberData.TargetType.Field:
							//	case MemberData.TargetType.Method:
							//	case MemberData.TargetType.Property:
							//		if(member.startType is RuntimeType) {
							//			return "this";
							//		}
							//		return "base";
							//	default:
							//		return "this";
							//}
							return "this";
						}

						string result = Value(mInstance, setVariable: setVariable);
						if(member.startType is RuntimeType) {
							var runtimeType = member.startType as RuntimeType;
							if(runtimeType is INativeMember) {
								return result;
							}
							else if(runtimeType is RuntimeGraphType) {
								var runtimeInstance = ReflectionUtils.GetActualTypeFromInstance(mInstance, true);
								if(runtimeType != runtimeInstance) {
									return result.CGAccess(GenerateGetGeneratedComponent(null, runtimeType));
								}
							}
							else if(runtimeType.IsInterface) {
								var runtimeInstance = ReflectionUtils.GetActualTypeFromInstance(mInstance, true);
								if(runtimeType != runtimeInstance) {
									if(runtimeType.GetHashCode() == runtimeInstance.GetHashCode()) {
										if(generatePureScript) {
											return Convert("this", runtimeType);
										}
									}
									else if(!runtimeInstance.IsCastableTo(runtimeType)) {
										return result.CGAccess(GenerateGetGeneratedComponent(null, runtimeType));
									}
									// if(runtimeInstance == typeof(GameObject) || runtimeInstance.IsCastableTo(typeof(Component))) {
									// } else {
									// 	throw new Exception($"Cannot convert type from: '{runtimeType.FullName}' to '{runtimeInstance.FullName}'");
									// }
								}
							}
							else if(runtimeType.IsArray) {
								return result;
							}
							else if(runtimeType is GenericFakeType) {
								return result;
							}
							else {
								throw new Exception($"Unsupported RuntimeType: {runtimeType.FullName}, {runtimeType.GetType()}");
							}
						}
						//if(result == "this") {
						//	switch(member.targetType) {
						//		case MemberData.TargetType.Constructor:
						//		case MemberData.TargetType.Event:
						//		case MemberData.TargetType.Field:
						//		case MemberData.TargetType.Method:
						//		case MemberData.TargetType.Property:
						//			if(member.startType is RuntimeType) {
						//				return "this";
						//			}
						//			return "base";
						//	}
						//}
						return result;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Function to generate correct code for ValueGetter
		/// </summary>
		/// <param name="member"></param>
		/// <param name="storeValue"></param>
		/// <returns></returns>
		public static string Value(MultipurposeMember member, Action<string, string> onEnterAndExit = null, bool autoConvert = false, bool setVariable = false) {
			string resultCode = GetCorrectName(member.target, member.parameters, member.initializers, member.instance, onEnterAndExit, autoConvert, setVariable: setVariable);
			if(!string.IsNullOrEmpty(resultCode)) {
				if(Utility.IsContainOperatorCode(member.target.name)) {
					throw new System.Exception("unsupported generating operator code in the current context");
				}
				if((member.target.targetType == MemberData.TargetType.uNodeGenericParameter ||
					member.target.targetType == MemberData.TargetType.Type) && !resultCode.StartsWith("typeof(", StringComparison.Ordinal)) {
					resultCode = "typeof(" + resultCode + ")";
				}
				return resultCode;
			}
			else {
				return Value(member.target, setVariable: setVariable);
			}
		}

		/// <summary>
		/// Function to generate correct code for MemberReflection
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static string Value(MemberData member, IList<MultipurposeMember.MParamInfo> parameters = null, ValueInput instance = null, bool setVariable = false, bool autoConvert = false) {
			if(!object.ReferenceEquals(member, null)) {
				if(member.isTargeted) {
					if(member.targetType == MemberData.TargetType.None || member.targetType == MemberData.TargetType.Type) {
						object o = member.Get(null);
						if(o is Type) {
							return "typeof(" + Type(o as Type) + ")";
						}
					}
					else if(member.targetType == MemberData.TargetType.Null) {
						return "null";
					}
					else if(member.targetType == MemberData.TargetType.uNodeGenericParameter) {
						return "typeof(" + member.name + ")";
					}
					else if(member.targetType == MemberData.TargetType.Constructor) {
						return "new " + Type(member.type) + "()";
					}
					else if(member.targetType == MemberData.TargetType.Values) {
						return Value(member.Get(null));
					}
					else if(member.targetType == MemberData.TargetType.uNodeFunction) {
						//TODO: fix me
						//string data = member.startName;
						//MemberDataUtility.GetItemName(
						//	member.startItem,
						//	out var gType,
						//	out var pType);
						//if(gType.Length > 0) {
						//	data += String.Format("<{0}>", String.Join(", ", gType));
						//}
						//data += "()";
					}
					if(member.isStatic) {
						return GetCorrectName(member, parameters, autoConvert: autoConvert);
						//string result = CSharpGenerator.ParseType(variable.startTypeName);
						//string[] str = GetCorrectName(variable).Split(new char[] { '.' });
						//for(int i = 0; i < str.Length; i++) {
						//	if(i == 0)
						//		continue;
						//	result += "." + str[i];
						//}
						//return result;
					}
					else if(member.IsTargetingVariable) {
						if(!member.isDeepTarget) {
							return ParseStartValue(member, instance, setVariable: setVariable);
						}
						var variable = member.startItem.GetReferenceValue() as Variable;
						if(variable != null) {
							return GetCorrectName(member, parameters, autoConvert: autoConvert, setVariable: setVariable);
						}
						throw new Exception("Variable not found: " + member.startName);
					}
					else {
						var mInstance = instance ?? member.instance;
						if(mInstance == null && !member.IsTargetingGraph) {
							return Null;
						}
						if(member.targetType == MemberData.TargetType.uNodeVariable ||
							member.targetType == MemberData.TargetType.uNodeProperty ||
							member.targetType == MemberData.TargetType.uNodeParameter ||
							member.targetType == MemberData.TargetType.uNodeLocalVariable ||
							member.targetType == MemberData.TargetType.uNodeGenericParameter ||
							member.targetType == MemberData.TargetType.Property ||
							member.targetType == MemberData.TargetType.uNodeFunction ||
							member.targetType == MemberData.TargetType.Field ||
							member.targetType == MemberData.TargetType.Constructor ||
							member.targetType == MemberData.TargetType.Method ||
							member.targetType == MemberData.TargetType.Event) {
							return GetCorrectName(member, parameters, instance: mInstance, autoConvert: autoConvert, setVariable: setVariable);
						}
						//if(graph is uNodeRuntime ||
						//	graph is IClassGraph classGraph && typeof(UnityEngine.Object).IsAssignableFrom(classGraph.InheritType)) {
						//	UnityEngine.Object obj = member.GetInstance() as UnityEngine.Object;
						//	if(obj is Transform && graph.transform == obj) {
						//		return GetCorrectName(member, parameters, autoConvert: autoConvert);
						//	} else if(obj is GameObject && graph.gameObject == obj) {
						//		return GetCorrectName(member, parameters, autoConvert: autoConvert);
						//	}
						//}
						if(graph == mInstance) {
							return Value(mInstance, autoConvert: autoConvert);
						}
						return GetCorrectName(member, parameters, instance: mInstance, autoConvert: autoConvert, setVariable: setVariable);
					}
					throw new Exception("Unsupported target reference: " + member.GetInstance().GetType());
				}
				else {
					throw new Exception("The value is un-assigned");
				}
			}
			else {
				throw new ArgumentNullException(nameof(member));
			}
		}

		/// <summary>
		/// Function to generate code for any object
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="initializer"></param>
		/// <returns></returns>
		public static string Value(object obj, string initializer = null, bool autoConvert = false, bool setVariable = false) {
			if(object.ReferenceEquals(obj, null))
				return "null";
			if(obj is Type) {
				return "typeof(" + Type(obj as Type) + ")";
			}
			else if(obj is MemberData) {
				return Value(obj as MemberData, setVariable: setVariable, autoConvert: autoConvert);
			}
			else if(obj is ValueInput) {
				var port = obj as ValueInput;
				return GeneratePort(port, setVariable: setVariable, autoConvert: autoConvert);
			}
			else if(obj is ValueOutput) {
				var port = obj as ValueOutput;
				return GeneratePort(port);
			}
			else if(obj is MultipurposeMember) {
				string header = null;
				string footer = null;

				if(setVariable) {
					var oldState = generationState.contextState;
					//Mark the current context state for set a value
					generationState.contextState = ContextState.Set;

					//Generate the code
					var rezult = Value(obj as MultipurposeMember, (x, y) => {
						if(!string.IsNullOrEmpty(x)) {
							header += x.AddLineInEnd();
						}
						if(!string.IsNullOrEmpty(y)) {
							footer += y.AddLineInFirst();
						}
					}, autoConvert);

					//Restore the previous context state
					generationState.contextState = oldState;

					//Return the result
					return header + rezult + footer;
				}
				else {
					//Generate the code
					var rezult = Value(obj as MultipurposeMember, (x, y) => {
						if(!string.IsNullOrEmpty(x)) {
							header += x.AddLineInEnd();
						}
						if(!string.IsNullOrEmpty(y)) {
							footer += y.AddLineInFirst();
						}
					}, autoConvert);
					//Return the result
					return header + rezult + footer;
				}
			}
			else if(obj is MultipurposeMember.MParamInfo) {
				var param = obj as MultipurposeMember.MParamInfo;
				if(param.input != null) {
					return GeneratePort(param.input, setVariable: setVariable, autoConvert: autoConvert);
				}
				else {
					throw null;
				}
			}
			else if(obj is UnityEngine.Object) {
				UnityEngine.Object o = obj as UnityEngine.Object;
				if(object.ReferenceEquals(o, graph)) {
					return "this";
				}
				if(o != null) {
					if(generationState.isStatic || generationState.state == State.Classes) {
						return "null";
					}
					if(graph is IClassGraph classGraph) {
						//TODO: fix me
						//Type inherithType = classGraph.InheritType;
						//if (inherithType.IsCastableTo(typeof(GameObject)) || inherithType.IsCastableTo(typeof(Component))) {
						//	if (o is GameObject) {
						//		GameObject g = o as GameObject;
						//		if (g == graph.gameObject) {
						//			return "this.gameObject";
						//		}
						//	}
						//	else if (o is Transform) {
						//		Transform g = o as Transform;
						//		if (g == graph.transform) {
						//			return "this.transform";
						//		}
						//	}
						//	else if (o is Component) {
						//		Component c = o as Component;
						//		if (c.gameObject == graph.gameObject) {
						//			return "this.GetComponent<" + Type(c.GetType()) + ">()";
						//		}
						//	}
						//}
					}
					if(!generatorData.unityVariableMap.ContainsKey(o)) {
						Type objType = o.GetType();
						if(o is ClassAsset asset) {
							if(generatePureScript) {
								objType = (asset.target as IReflectionType).ReflectionType;
							}
							else {
								objType = typeof(BaseRuntimeAsset);
							}
						}
						//else if(o is uNodeSpawner comp) {

						//}
						if(graph is ITypeWithScriptData scriptData) {
							string varName = RegisterVariable(new VariableData() { name = "objectVariable", type = objType, modifier = new FieldModifier() { Public = true } });
							generatorData.unityVariableMap.Add(o, varName);
							scriptData.ScriptData.unityObjects.Add(new GeneratedScriptData.ObjectData() {
								name = varName,
								value = o,
							});
						}
					}
					return generatorData.unityVariableMap[o];
				}
				return "null";
			}
			else if(obj is LayerMask) {
                return CG.Convert(CG.Value(((LayerMask)obj).value), typeof(LayerMask));
            }
			else if(obj is ObjectValueData) {
				return Value((obj as ObjectValueData).value);
			}
			else if(obj is ParameterValueData) {
				return Value((obj as ParameterValueData).value);
			}
			else if(obj is ConstructorValueData) {
				var val = obj as ConstructorValueData;
				Type t = val.type;
				if(t != null) {
					string pVal = null;
					if(val.parameters != null) {
						for(int i = 0; i < val.parameters.Length; i++) {
							string p = Value(val.parameters[i]);
							if(!string.IsNullOrEmpty(pVal)) {
								pVal += ", ";
							}
							pVal += p;
						}
					}
					string data = "new " + Type(t) + "(" + pVal + ")";
					if(!string.IsNullOrEmpty(initializer)) {
						data += " {" + initializer + "}";
					}
					return data;
				}
				return "null";
			}
			else if(obj is BaseValueData) {
				throw new System.Exception("Unsupported Value Data:" + obj.GetType());
			}
			else if(obj is Variable) {
				return GetVariableName(obj as Variable);
			}
			else if(obj is StringWrapper) {
				return (obj as StringWrapper).value;
			}
			Type type = obj.GetType();
			if(type.IsValueType || type == typeof(string)) {
				if(obj is string) {
					return StringHelper.StringLiteralCode(obj.ToString());
				}
				else if(obj is float) {
					return obj.ToString().Replace(',', '.') + "F";
				}
				else if(obj is int) {
					return obj.ToString();
				}
				else if(obj is uint) {
					return obj.ToString() + "U";
				}
				else if(obj is short) {
					return "(" + Type(typeof(short)) + ")" + obj.ToString();
				}
				else if(obj is ushort) {
					return "(" + Type(typeof(ushort)) + ")" + obj.ToString();
				}
				else if(obj is long) {
					return obj.ToString() + "L";
				}
				else if(obj is ulong) {
					return obj.ToString() + "UL";
				}
				else if(obj is byte) {
					return "(" + Type(typeof(byte)) + ")" + obj.ToString();
				}
				else if(obj is sbyte) {
					return "(" + Type(typeof(sbyte)) + ")" + obj.ToString();
				}
				else if(obj is double) {
					return obj.ToString().Replace(',', '.') + "D";
				}
				else if(obj is decimal) {
					return obj.ToString().Replace(',', '.') + "M";
				}
				else if(obj is bool) {
					return obj.ToString().ToLower();
				}
				else if(obj is char) {
					return "'" + obj.ToString() + "'";
				}
				else if(obj is Enum) {
					return Type(obj.GetType()) + "." + obj.ToString();
				}
				else if(obj is Vector2) {
					var val = (Vector2)obj;
					if(string.IsNullOrEmpty(initializer)) {
						if(val == Vector2.zero) {
							return Type(typeof(Vector2)) + ".zero";
						}
						if(val == Vector2.up) {
							return Type(typeof(Vector2)) + ".up";
						}
						if(val == Vector2.down) {
							return Type(typeof(Vector2)) + ".down";
						}
						if(val == Vector2.left) {
							return Type(typeof(Vector2)) + ".left";
						}
						if(val == Vector2.right) {
							return Type(typeof(Vector2)) + ".right";
						}
						if(val == Vector2.one) {
							return Type(typeof(Vector2)) + ".one";
						}
						return "new " + Type(typeof(Vector2)) + "(" + val.x + "f, " + val.y + "f)";
					}
				}
				else if(obj is Vector3) {
					if(string.IsNullOrEmpty(initializer)) {
						var val = (Vector3)obj;
						if(val == Vector3.zero) {
							return Type(typeof(Vector3)) + ".zero";
						}
						else if(val == Vector3.up) {
							return Type(typeof(Vector3)) + ".up";
						}
						else if(val == Vector3.down) {
							return Type(typeof(Vector3)) + ".down";
						}
						else if(val == Vector3.left) {
							return Type(typeof(Vector3)) + ".left";
						}
						else if(val == Vector3.right) {
							return Type(typeof(Vector3)) + ".right";
						}
						else if(val == Vector3.one) {
							return Type(typeof(Vector3)) + ".one";
						}
						else if(val == Vector3.forward) {
							return Type(typeof(Vector3)) + ".forward";
						}
						else if(val == Vector3.back) {
							return Type(typeof(Vector3)) + ".back";
						}
						return "new " + Type(typeof(Vector3)) + "(" + val.x + "f, " + val.y + "f, " + val.z + "f)";
					}
				}
				else if(obj is Vector4) {
					if(string.IsNullOrEmpty(initializer)) {
						var val = (Vector4)obj;
						if(val == Vector4.zero) {
							return Type(typeof(Vector4)) + ".zero";
						}
						else if(val == Vector4.one) {
							return Type(typeof(Vector4)) + ".one";
						}
					}
				}
				else if(obj is Color) {
					if(string.IsNullOrEmpty(initializer)) {
						var val = (Color)obj;
						if(val == Color.white) {
							return Type(typeof(Color)) + ".white";
						}
						else if(val == Color.black) {
							return Type(typeof(Color)) + ".black";
						}
						else if(val == Color.blue) {
							return Type(typeof(Color)) + ".blue";
						}
						else if(val == Color.clear) {
							return Type(typeof(Color)) + ".clear";
						}
						else if(val == Color.cyan) {
							return Type(typeof(Color)) + ".cyan";
						}
						else if(val == Color.gray) {
							return Type(typeof(Color)) + ".gray";
						}
						else if(val == Color.green) {
							return Type(typeof(Color)) + ".green";
						}
						else if(val == Color.magenta) {
							return Type(typeof(Color)) + ".magenta";
						}
						else if(val == Color.red) {
							return Type(typeof(Color)) + ".red";
						}
						else if(val == Color.yellow) {
							return Type(typeof(Color)) + ".yellow";
						}
					}
				}
				else if(obj is Rect) {
					if(string.IsNullOrEmpty(initializer)) {
						var val = (Rect)obj;
						if(val == Rect.zero) {
							return Type(typeof(Rect)) + ".zero";
						}
						else {
							return New(typeof(Rect), Value(val.x), Value(val.y), Value(val.width), Value(val.height));
						}
					}
				}
			}
			else if(type.IsGenericType) {
				string elementObject = "";
				if(obj is IDictionary) {
					IDictionary dic = obj as IDictionary;
					if(dic != null && dic.Count > 0) {
						elementObject = " { ";
						int index = 0;
						foreach(DictionaryEntry o in dic) {
							if(index != 0) {
								elementObject += ", ";
							}
							elementObject += "{ " + Value(o.Key) + ", " + Value(o.Value) + " }";
							index++;
						}
						elementObject += " }";
					}
				}
				else if(obj is ICollection) {
					ICollection col = obj as ICollection;
					if(col != null && col.Count > 0) {
						elementObject = " { ";
						int index = 0;
						foreach(object o in col) {
							if(index != 0) {
								elementObject += ", ";
							}
							if(o is DictionaryEntry) {
								elementObject += "{ " + Value(((DictionaryEntry)o).Key) + ", " + Value(((DictionaryEntry)o).Value) + " }";
							}
							else {
								elementObject += Value(o);
							}
							index++;
						}
						elementObject += initializer + " }";
					}
				}
				else {
					IEnumerable val = obj as IEnumerable;
					if(val != null) {
						elementObject = " { ";
						int index = 0;
						foreach(object o in val) {
							if(index != 0) {
								elementObject += ", ";
							}
							if(o is DictionaryEntry) {
								elementObject += "{ " + Value(((DictionaryEntry)o).Key) + ", " + Value(((DictionaryEntry)o).Value) + " }";
							}
							else {
								elementObject += Value(o);
							}
							index++;
						}
						elementObject += initializer + " }";
						if(index == 0) {
							return "new " + Type(type) + "()";
						}
					}
				}
				return "new " + Type(type) + "()" + elementObject;
			}
			else if(type.IsArray) {
				string elementObject = "[0]";
				Array array = obj as Array;
				if(array != null && array.Length > 0) {
					int index = 0;
					elementObject = "[" + //array.Length + 
						"] {";
					foreach(object o in array) {
						if(index != 0) {
							elementObject += ",";
						}
						elementObject += " " + Value(o);
						index++;
					}
					elementObject += initializer + " }";
				}
				return "new " + Type(type.GetElementType()) + elementObject;
			}
			else if(obj is IEnumerable) {
				string elementObject = "";
				IEnumerable val = obj as IEnumerable;
				if(val != null) {
					elementObject = " { ";
					int index = 0;
					foreach(object o in val) {
						if(index != 0) {
							elementObject += ", ";
						}
						elementObject += Value(o);
						index++;
					}
					elementObject += initializer + " }";
					if(index == 0) {
						return "new " + Type(type) + "()";
					}
				}
				return "new " + Type(type) + "()" + elementObject;
			}
			if(ReflectionUtils.IsNullOrDefault(obj, type)) {
				if(!type.IsValueType && obj == null) {
					return "null";
				}
				else {
					string data = "new " + Type(type) + "()";
					if(!string.IsNullOrEmpty(initializer)) {
						data += " {" + initializer + "}";
					}
					return data;
				}
			}
			else if(obj is Gradient || obj is Keyframe || obj is AnimationCurve) {
				if(string.IsNullOrEmpty(initializer)) {
					return DoGenerateValueCode(obj, type, initializer, includeProperty: true);
				}
				else {
					return New(obj.GetType(), null, new[] { initializer });
				}
			}
			else {
				return DoGenerateValueCode(obj, type, initializer);
			}
		}

		private static string DoGenerateValueCode(object value, Type type, string initializer = null, bool includeField = true, bool includeProperty = false) {
			if(type.IsValueType || ReflectionUtils.GetDefaultConstructor(type) != null) {
				object clone = ReflectionUtils.CreateInstance(type);
				List<string> initializers = new List<string>();
				if(!string.IsNullOrEmpty(initializer)) {
					initializers.Add(initializer);
				}
				if(includeField) {
					FieldInfo[] fields = ReflectionUtils.GetFields(value);
					foreach(FieldInfo field in fields) {
						if(field.IsInitOnly)
							continue;//Skip if the field is `read-only`
						object fieldObj = field.GetValueOptimized(value);
						if(field.FieldType.IsValueType) {
							object cloneObj = field.GetValueOptimized(clone);
							if(cloneObj.Equals(fieldObj))
								continue;
						}
						initializers.Add(CG.SetValue(field.Name, CG.Value(fieldObj)));
					}
				}
				if(includeProperty) {
					PropertyInfo[] properties = ReflectionUtils.GetProperties(value);
					foreach(PropertyInfo property in properties) {
						if(property.CanRead && property.CanWrite && !property.IsDefinedAttribute(typeof(ObsoleteAttribute))) {
							object fieldObj = property.GetValueOptimized(value);
							if(property.PropertyType.IsValueType) {
								object cloneObj = property.GetValueOptimized(clone);
								if(cloneObj.Equals(fieldObj))
									continue;
							}
							initializers.Add(CG.SetValue(property.Name, CG.Value(fieldObj)));
						}
					}
				}
				return CG.New(type, null, initializers);
			}
			return value.ToString();
		}

		/// <summary>
		/// Parse Constructor initializer.
		/// </summary>
		/// <param name="initializer"></param>
		/// <returns></returns>
		private static string ParseConstructorInitializer(Type type, IList<MultipurposeMember.InitializerData> initializer) {
			if(initializer != null) {
				string ctorInit = GenerateInitializers(initializer, type);
				if(!string.IsNullOrEmpty(ctorInit)) {
					ctorInit = " { " + ctorInit + "} ";
				}
				return ctorInit;
			}
			return null;
		}

		/// <summary>
		/// Function for Convert AttributeData to AData
		/// </summary>
		/// <param name="attribute"></param>
		/// <returns></returns>
		private static AData TryParseAttributeData(AttributeData attribute) {
			if(attribute != null && attribute.attributeType != null) {
				AData data = new AData();
				if(attribute.constructor != null && attribute.type != null) {
					data.attributeType = attribute.constructor.type;
					ConstructorValueData ctor = attribute.constructor;
					Type t = ctor.type;
					if(t != null) {
						if(ctor.parameters != null) {
							data.attributeParameters = ctor.parameters.Select(p => Value(p)).ToArray();
						}
						data.attributeType = t;
						if(ctor.initializer != null && ctor.initializer.Length > 0) {
							if(data.namedParameters == null) {
								data.namedParameters = new Dictionary<string, string>();
							}
							foreach(var param in ctor.initializer) {
								data.namedParameters.Add(param.name, Value(param.value));
							}
						}
					}
				}
				else {
					data.attributeType = attribute.attributeType.type;
				}
				return data;
			}
			return null;
		}
		#endregion

		#region Variable Functions
		/// <summary>
		/// Get variable data of variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static VData GetVariableData(object reference) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(object.ReferenceEquals(vdata.reference, reference)) {
					return vdata;
				}
			}
			return null;
		}

		public static string GetVariableNameByReference(object reference) {
			var data = GetVariableData(reference);
			if(data != null) {
				return data.name;
			}
			return null;
		}

		#region AddVariable
		/// <summary>
		/// Register new using namespaces
		/// </summary>
		/// <param name="nameSpace"></param>
		/// <returns></returns>
		public static bool RegisterUsingNamespace(string nameSpace) {
			if(string.IsNullOrEmpty(nameSpace)) return false;
			return setting.usingNamespace.Add(nameSpace);
		}

		/// <summary>
		/// Register new script header like define symbol, pragma symbol or script copyright
		/// </summary>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static bool RegisterScriptHeader(string contents) {
			return setting.scriptHeaders.Add(contents);
		}

		/// <summary>
		/// Register variable for port.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="isLocal"></param>
		/// <returns></returns>
		public static string RegisterVariable(ValueOutput port, string name = null, Type type = null, bool isLocal = true) {
			if(port is null) {
				throw new ArgumentNullException(nameof(port), "Error on trying to register port variable because the port is null.");
			}

			if(isLocal) {
				return RegisterLocalVariable(name ?? (!string.IsNullOrEmpty(port.name) ? port.name : "local_var"), type ?? port.type, reference: port);
			}
			else {
				return RegisterPrivateVariable(name ?? (!string.IsNullOrEmpty(port.name) ? port.name : "m_variable"), type ?? port.type, reference: port);
			}
		}

		public static string RegisterVariable(VariableData variable, bool isInstance = true, bool autoCorrection = true) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(object.ReferenceEquals(vdata.reference, variable)) {
					if(isInstance)
						vdata.isInstance = true;
					return vdata.name;
				}
			}
			var result = new VData(variable.name, variable.type, isInstance, autoCorrection) {
				modifier = variable.modifier,
				reference = variable,
				defaultValue = variable.value,
			};
			generatorData.AddVariable(result);
			return result.name;
		}

		public static string RegisterVariable(Variable variable, bool isInstance = true, bool autoCorrection = true) {
			return M_RegisterVariable(variable, variable, isInstance, autoCorrection).name;
		}

		/// <summary>
		/// Register a new local variable that's not auto declared within the class.
		/// Note: you need to declare the variable manually.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string RegisterLocalVariable(string name, Type type, object value = null, object reference = null) {
			if(reference != null) {
				foreach(VData vdata in generatorData.GetVariables()) {
					if(reference == vdata.reference) {
						vdata.isInstance = false;
						return name;
					}
				}
			}
			var data = new VData(name, type, isInstance: false) {
				reference = reference,
				modifier = new FieldModifier() {
					Public = false,
					Private = true,
				},
				defaultValue = value,
			};
			generatorData.AddVariable(data);
			return data.name;
		}

		/// <summary>
		/// Register a new private variable that's declared within the class.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string RegisterPrivateVariable(string name, Type type, object value = null, object reference = null) {
			if(reference != null) {
				foreach(VData vdata in generatorData.GetVariables()) {
					if(reference == vdata.reference) {
						vdata.isInstance = true;
						if(generationState.isStatic) {
							vdata.modifier.Static = true;
						}
						return name;
					}
				}
			}
			var data = new VData(name, type, isInstance: true) {
				reference = reference,
				modifier = new FieldModifier() {
					Public = false,
					Private = true,
					Static = generationState.isStatic,
				},
				defaultValue = value,
			};
			generatorData.AddVariable(data);
			return data.name;
		}

		private static VData M_RegisterVariable(object reference, Variable variable, bool isInstance = true, bool autoCorrection = true) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(object.ReferenceEquals(vdata.reference, reference)) {
					if(isInstance)
						vdata.isInstance = true;
					return vdata;
				}
			}
			var result = new VData(variable.name, variable.type, isInstance, autoCorrection) {
				modifier = variable.modifier,
				reference = variable,
				defaultValue = variable.defaultValue,
			};
			generatorData.AddVariable(result);
			return result;
		}

		public static void RegisterVariableAlias(string variableName, Variable variable, object owner) {
			generatorData.AddVariableAlias(variableName, variable, owner);
		}

		public static Variable GetVariableAlias(string variableName, object owner) {
			return generatorData.GetVariableAlias(variableName, owner);
		}

		/// <summary>
		/// Register node to the generators.
		/// Note: call only from RegisterPort
		/// </summary>
		/// <param name="nodeComponent"></param>
		public static void RegisterNode(NodeObject nodeComponent) {
			if(!generatorData.allNode.Contains(nodeComponent)) {
				generatorData.allNode.Add(nodeComponent);
			}
		}

		/// <summary>
		/// Register pre node generation process, this is normally called after initialization but before node generation.
		/// Note: call only from RegisterPort.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="action"></param>
		public static void RegisterNodeSetup(NodeObject owner, Action action) {
			Action act;
			generatorData.initActionForNodes.TryGetValue(owner, out act);
			act += action;
			generatorData.initActionForNodes[owner] = act;
		}

		/// <summary>
		/// Register post initialization action.
		/// </summary>
		/// <param name="action"></param>
		public static void RegisterPostInitialization(Action action) {
			generatorData.postInitialization += action;
		}

		/// <summary>
		/// Register post generation action.
		/// This will be called after succesfull generating the current processed classes.
		/// </summary>
		/// <param name="action"></param>
		public static void RegisterPostGeneration(Action<ClassData> action) {
			generatorData.postGeneration += action;
		}

		/// <summary>
		/// Register post class manipulator.
		/// This will be called one step before succesfull generating the current processed classes.
		/// Used for adding/modifying code before successfull of generation.
		/// </summary>
		/// <param name="action"></param>
		public static void RegisterPostClassManipulator(Action<GData> action) {
			generatorData.postManipulator += action;
		}
		#endregion

		#region Declare Variables
		/// <summary>
		/// Create variable declaration code.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <param name="modifier"></param>
		/// <returns></returns>
		public static string DeclareVariable(Type type,
			string name,
			string value = null,
			FieldModifier modifier = null,
			IEnumerable<string> attributes = null) {
			string M = null;
			if(modifier != null) {
				M = modifier.GenerateCode();
			}
			string T;
			if(type != null) {
				T = Type(type);
			}
			else {
				T = KeywordVar;
			}
			if(attributes != null && attributes.Any()) {
				if(string.IsNullOrEmpty(value)) {
					return CG.Flow(
						CG.Flow(attributes),
						M + T + " " + name + ";"
					);
				}
				else {
					return CG.Flow(
						CG.Flow(attributes),
						M + T + " " + name + " = " + value + ";"
					);
				}
			}
			if(string.IsNullOrEmpty(value)) {
				return M + T + " " + name + ";";
			}
			return M + T + " " + name + " = " + value + ";";
		}

		/// <summary>
		/// Create variable declaration code.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string DeclareVariable(string name, string value = null) {
			if(string.IsNullOrEmpty(name)) {
				return null;
			}
			if(string.IsNullOrEmpty(value)) {
				return KeywordVar + " " + name + ";";
			}
			return KeywordVar + " " + name + " = " + value + ";";
		}

		/// <summary>
		/// Create variable declaration code.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="value"></param>
		/// <param name="flows"></param>
		/// <returns></returns>
		public static string DeclareVariable(ValueOutput port, string value, params FlowOutput[] flows) {
			return DeclareVariable(port, null, value, flows);
		}

		/// <summary>
		/// Create variable declaration code.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <param name="flows"></param>
		/// <returns></returns>
		public static string DeclareVariable(ValueOutput port, Type type, string value, params FlowOutput[] flows) {
			if(CG.CanDeclareLocal(port, flows)) {
				if(type == null) {
					return CG.DeclareVariable(CG.GeneratePort(port), value);
				}
				else {
					return CG.DeclareVariable(type, CG.GeneratePort(port), value);
				}
			}
			else {
				var vdata = CG.GetVariableData(port);

				var container = port.node.GetObjectInParent<NodeContainer>();
				if(container != null && container is BaseFunction) {
					CG.RegisterPostClassManipulator(data => {
						var mdata = data.GetMethodData(container as BaseFunction);
						if(mdata != null) {
							mdata.AddCode(CG.DeclareVariable(vdata.type, vdata.name, "default"), int.MinValue);
						}
					});
				}
				else {
					vdata.SetToInstanceVariable();
				}
				return CG.Set(vdata.name, value);
			}
		}
		#endregion

		#endregion

		#region InsertMethod
		public static void InsertCodeToFunction(string functionName, Type returnType, string code, int priority = 0) {
			var mData = generatorData.GetMethodData(functionName);
			if(mData == null) {
				mData = generatorData.AddMethod(functionName, returnType);
			}
			mData.AddCode(code, priority);
		}

		public static void InsertCodeToFunction(string functionName, Type returnType, Type[] parameterTypes, string code, int priority = 0) {
			var mData = generatorData.GetMethodData(functionName, parameterTypes.Select((item) => item).ToArray());
			if(mData == null) {
				mData = generatorData.AddMethod(functionName, returnType, parameterTypes.Select((item) => item).ToArray());
			}
			mData.AddCode(code, priority);
		}
		#endregion
	}
}