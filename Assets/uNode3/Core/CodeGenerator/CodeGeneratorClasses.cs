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
		static class Cached {
			static Dictionary<string, List<Type>> _namespaceTypes;
			public static Dictionary<string, List<Type>> namespaceTypes {
				get {
					if(_namespaceTypes == null) {
						Init();
					}
					return _namespaceTypes;
				}
			}
			//static List<Type> _types;
			//public static List<Type> types {
			//	get {
			//		if(_types == null) {
			//			Init();
			//		}
			//		return _types;
			//	}
			//}
			static void Init() {
				var assemblies = ReflectionUtils.GetStaticAssemblies();
				//_types = new List<Type>();
				_namespaceTypes = new Dictionary<string, List<Type>>();
				for(int i = 0; i < assemblies.Length; i++) {
					try {
						var typeList = ReflectionUtils.GetAssemblyTypes(assemblies[i]);
						//_types.AddRange(typeList);
						for(int x = 0; x < typeList.Length; x++) {
							if(!_namespaceTypes.TryGetValue(typeList[x].Namespace, out var values)) {
								values = new List<Type>(32);
								_namespaceTypes[typeList[x].Namespace] = values;
							}
							_namespaceTypes[typeList[x].Namespace].Add(typeList[x]);
						}
					}
					catch { }
				}
			}
		}

		public sealed class StringWrapper {
			public string value;

			public StringWrapper(string value) {
				this.value = value;
			}
		}

		public enum State {
			None,
			Function,
			Classes,
			Constructor,
			Property,
		}

		public enum ContextState {
			None,
			Set,
		}

		public sealed class GeneratorState {
			public bool isStatic;
			public ContextState contextState = ContextState.None;

			public State m_state = State.Classes;
			public State state {
				get => m_state;
				internal set {
					m_state = value;
					//Reset other value every time state is changed.
					contextState = ContextState.None;
				}
			}

			//private List<UPort> ports = new List<UPort>(64);
		}

		public class CoroutineData {
			public string variableName;
			public string onStop;
			public string contents;
			public Func<string> customExecution;

			public override string ToString() {
				return variableName;
			}
		}

		internal class BlockStack {
			internal bool allowYield;
			// internal Type returnType;
		}

		public class GData {
			public GeneratorSetting setting;
			public GeneratorState state = new GeneratorState();
			public bool hasError = false;

			/// <summary>
			/// Note: this is filled after successful generating the class ( use post generation for use ).
			/// </summary>
			public ClassData classData { get; internal set; }

			/// <summary>
			/// The type name of currently generating code.
			/// </summary>
			public string typeName {
				get;
				internal set;
			}

			public void ValidateTypes(string Namespace, HashSet<string> usingNamespace, Func<Type, bool> func) {
				//var watch = new System.Diagnostics.Stopwatch();
				//watch.Start();

				if(Namespace != string.Empty) {//Global
					if(Cached.namespaceTypes.TryGetValue(string.Empty, out var types)) {
						foreach(var t in types) {
							if(t.IsPublic && func(t)) {
								return;
							}
						}
					}
				}
				foreach(var ns in usingNamespace) {
					if(ns == Namespace) continue;
					if(Cached.namespaceTypes.TryGetValue(ns, out var types)) {
						foreach(var t in types) {
							if(t.IsPublic && func(t)) {
								return;
							}
						}
					}
				}
				//for(int i = 0; i < Cached.types.Count; i++) {
				//	var type = Cached.types[i];
				//	if(type.IsPublic && Namespace != type.Namespace && (usingNamespace.Contains(type.Namespace) || type.Namespace == null)) {
				//		if(func(type)) {
				//			return;
				//		}
				//	}
				//}

				//UnityEngine.Debug.Log(watch.ElapsedMilliseconds);
			}

			internal BlockStack currentBlock => blockStacks.Count > 0 ? blockStacks[blockStacks.Count - 1] : null;

			internal List<BlockStack> blockStacks = new List<BlockStack>();

			private List<VData> variables = new List<VData>(16);
			public List<PData> properties = new List<PData>(16);
			public List<CData> constructors = new List<CData>();
			public List<MData> methodData = new List<MData>(16);

			public List<BaseGraphEvent> eventNodes = new List<BaseGraphEvent>();
			public List<NodeObject> allNode = new List<NodeObject>();
			public HashSet<FlowInput> regularNodes = new HashSet<FlowInput>();
			public HashSet<FlowInput> stateNodes = new HashSet<FlowInput>();
			public HashSet<NodeObject> connectedNodes = new HashSet<NodeObject>();

			public List<Exception> errors = new List<Exception>();

			public HashSet<NodeObject> registeredFlowNodes = new HashSet<NodeObject>();

			public Dictionary<UPort, Func<string>> generatorForPorts = new Dictionary<UPort, Func<string>>();
			public Dictionary<Type, string> typesMap = new Dictionary<Type, string>(32) {
				{ typeof(string), "string" },
				{ typeof(bool), "bool" },
				{ typeof(float), "float" },
				{ typeof(int), "int" },
				{ typeof(short), "short" },
				{ typeof(long), "long" },
				{ typeof(double), "double" },
				{ typeof(decimal), "decimal" },
				{ typeof(byte), "byte" },
				{ typeof(uint), "uint" },
				{ typeof(ulong), "ulong" },
				{ typeof(ushort), "ushort" },
				{ typeof(char), "char" },
				{ typeof(sbyte), "sbyte" },
				{ typeof(void), "void" },
				{ typeof(object), "object" },
			};
			public Dictionary<NodeObject, HashSet<NodeObject>> FlowConnectedTo = new Dictionary<NodeObject, HashSet<NodeObject>>();
			public Dictionary<UPort, string> generatedData = new Dictionary<UPort, string>(32);
			public Dictionary<NodeObject, string> eventCoroutineData = new Dictionary<NodeObject, string>();
			public Dictionary<object, string> eventIDMap = new Dictionary<object, string>();
			public Dictionary<NodeObject, string> methodName = new Dictionary<NodeObject, string>();

			internal Dictionary<NodeObject, bool> stackOverflowMap = new Dictionary<NodeObject, bool>();
			internal Dictionary<object, CoroutineData> coroutineEvent = new Dictionary<object, CoroutineData>();

			int _debugMemberMapID;
			internal int newDebugMapID {
				get {
					return ++_debugMemberMapID;
				}
			}

			internal Dictionary<UGraphElement, HashSet<NodeObject>> nodesMap = new Dictionary<UGraphElement, HashSet<NodeObject>>();

			public Dictionary<object, object> userObjectMap = new Dictionary<object, object>();
			public Dictionary<object, Dictionary<string, string>> variableNamesMap = new Dictionary<object, Dictionary<string, string>>();

			internal Dictionary<string, Dictionary<Type, Dictionary<string, string>>> customUIDMethods = new Dictionary<string, Dictionary<Type, Dictionary<string, string>>>();

			public Dictionary<UnityEngine.Object, string> unityVariableMap = new Dictionary<UnityEngine.Object, string>();

			public Dictionary<string, int> VarNames = new Dictionary<string, int>();
			private Dictionary<string, int> generatedNames = new Dictionary<string, int>();
			private Dictionary<string, int> generatedMethodNames = new Dictionary<string, int>();

			public Action<ClassData> postGeneration;
			public Action<GData> postManipulator;
			public Action postInitialization;
			public Dictionary<NodeObject, Action> initActionForNodes = new Dictionary<NodeObject, Action>();
			public Dictionary<object, HashSet<int>> initializedUserObject = new Dictionary<object, HashSet<int>>();

			public Dictionary<object, Dictionary<string, Variable>> variableAliases = new Dictionary<object, Dictionary<string, Variable>>();

			private int generatedMethodCount;

			public MData AddNewGeneratedMethod(string uid, Type type, MPData[] parameters = null) {
				if(string.IsNullOrEmpty(uid)) {
					uid = "Generated" + (++generatedMethodCount);
				}
				return AddMethod("M_" + uid, type, parameters);
			}

			public MData AddNewGeneratedMethod(string uid, Type type, Type[] parameters) {
				if(string.IsNullOrEmpty(uid)) {
					uid = "Generated" + (++generatedMethodCount);
				}
				return AddMethod("M_" + uid, type, parameters);
			}

			public void AddVariable(VData variable) {
				variables.Add(variable);
			}

			public void AddVariableAlias(string name, Variable variable, object owner) {
				Dictionary<string, Variable> map;
				if(!variableAliases.TryGetValue(owner, out map)) {
					map = new Dictionary<string, Variable>();
					variableAliases[owner] = map;
				}
				map[name] = variable;
			}

			public Variable GetVariableAlias(string name, object owner) {
				Dictionary<string, Variable> map;
				if(variableAliases.TryGetValue(owner, out map)) {
					Variable variable;
					if(map.TryGetValue(name, out variable)) {
						return variable;
					}
				}
				return null;
			}

			public void AddEventCoroutineData(NodeObject comp, string contents) {
				eventCoroutineData[comp] = contents;
			}

			public List<VData> GetVariables() {
				return variables;
			}

			public string GenerateName(string startName = "variable") {
				if(string.IsNullOrEmpty(startName)) {
					startName = "variable";
				}
				startName = uNodeUtility.AutoCorrectName(startName);
				if(generatedNames.ContainsKey(startName)) {
					string name;
					while(true) {
						name = startName + (++generatedNames[startName]).ToString();
						if(!generatedNames.ContainsKey(name)) {
							break;
						}
					}
					return name;
				} else {
					string name = startName;
					if(generatedNames.ContainsKey(name)) {
						while(true) {
							name = startName + (++generatedNames[startName]).ToString();
							if(!generatedNames.ContainsKey(name)) {
								break;
							}
						}
					}
					generatedNames.Add(name, 0);
					return name;
				}
			}

			/// <summary>
			/// Function for generating correctly method name
			/// </summary>
			/// <param name="startName"></param>
			/// <returns></returns>
			public string GenerateMethodName(string startName = "Method") {
				if(string.IsNullOrEmpty(startName)) {
					startName = "Method";
				}
				startName = uNodeUtility.AutoCorrectName(startName);
				if(generatedMethodNames.ContainsKey(startName)) {
					string name;
					while(true) {
						name = startName + (++generatedMethodNames[startName]).ToString();
						if(!generatedMethodNames.ContainsKey(name)) {
							break;
						}
					}
					return name;
				} else {
					string name = startName;
					if(generatedMethodNames.ContainsKey(name)) {
						while(true) {
							name = startName + (++generatedMethodNames[startName]).ToString();
							if(!generatedMethodNames.ContainsKey(name)) {
								break;
							}
						}
					}
					generatedMethodNames.Add(name, 0);
					return name;
				}
			}

			public string GetEventID(object target) {
				if(target == null)
					throw new System.ArgumentNullException("target cannot be null");
				if(target is UnityEngine.Object || !target.GetType().IsValueType) {
					if(!eventIDMap.TryGetValue(target, out var id)) {
						id = eventIDMap.Count.ToString();
						eventIDMap[target] = id;
					}
					return id;
				}
				throw new Exception("Unsupported value for event.\nType:" + target.GetType().FullName);
			}

			public string GetMethodName(BaseGraphEvent method) {
				if(method == null)
					throw new System.Exception("method can't null");
				if(methodName.ContainsKey(method)) {
					return methodName[method];
				}
				string name = generatorData.GenerateMethodName(method.GetTitle());
				methodName.Add(method, name);
				return name;
			}

			public PData GetPropertyData(string name) {
				return properties.FirstOrDefault(p => p.name == name);
			}

			public MData GetMethodData(BaseFunction function) {
				foreach(MData m in methodData) {
					if(m.owner == function) {
						return m;
					}
				}
				return GetMethodData(function.name, function.ParameterTypes.Select(p => p).ToArray());
			}

			/// <summary>
			/// Get Correct Method Data.
			/// </summary>
			/// <param name="methodName"></param>
			/// <param name="returnType"></param>
			/// <param name="parametersType"></param>
			/// <returns></returns>
			public MData GetMethodData(string methodName, IList<Type> parametersType = null, int genericParameterLength = -1) {
				if(parametersType == null || parametersType.Count == 0) {
					foreach(MData m in methodData) {
						if(m.name == methodName && (parametersType == null || (m.parameters == null || m.parameters.Count == 0))) {
							if(genericParameterLength >= 0 && 
								(m.genericParameters == null ? 0 : m.genericParameters.Count) != genericParameterLength) {
								continue;
							}
							return m;
						}
					}
				} else {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.parameters != null && m.parameters.Count == parametersType.Count) {
							bool correct = true;
							for(int i = 0; i < m.parameters.Count; i++) {
								if(m.parameters[i].type != parametersType[i]) {
									correct = false;
									break;
								}
							}
							if(correct) {
								if(genericParameterLength >= 0 && 
									(m.genericParameters == null ? 0 : m.genericParameters.Count) != genericParameterLength) {
									continue;
								}
								return m;
							}
						}
					}
				}
				return null;
			}

			public void InsertMethodCode(string methodName, string code, int priority = 0) {
				foreach(MData m in methodData) {
					if(m.name == methodName) {
						m.AddCode(code, priority);
						return;
					}
				}
				throw new System.Exception("No Method data found to insert code");
			}

			public MData AddMethod(string methodName, Type returnType, params Type[] parametersType) {
				return AddMethod(methodName, returnType, parametersType as IList<Type>);
			}

			public MData AddMethod(string methodName, Type returnType, IList<Type> parametersType) {
				if(string.IsNullOrEmpty(methodName) || returnType == null)
					throw new System.Exception("Method name or return type can't null");
				if(parametersType == null || parametersType.Count == 0) {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType) {
							return m;
						}
					}
				} else {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType && m.parameters != null && m.parameters.Count == parametersType.Count) {
							bool correct = true;
							for(int i = 0; i < m.parameters.Count; i++) {
								if(m.parameters[i].type != parametersType[i]) {
									correct = false;
									break;
								}
							}
							if(correct) {
								return m;
							}
						}
					}
				}
				MData mData = new MData(methodName, returnType, parametersType);
				methodData.Add(mData);
				return mData;
			}

			public MData AddMethod(string methodName, Type returnType, MPData[] parametersType) {
				if(string.IsNullOrEmpty(methodName) || returnType == null)
					throw new System.Exception("Method name or return type can't null");
				if(parametersType == null || parametersType.Length == 0) {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType) {
							return m;
						}
					}
				} else {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType && m.parameters != null && m.parameters.Count == parametersType.Length) {
							bool correct = true;
							for(int i = 0; i < m.parameters.Count; i++) {
								if(m.parameters[i].type != parametersType[i].type) {
									correct = false;
									break;
								}
							}
							if(correct) {
								return m;
							}
						}
					}
				}
				MData mData = new MData(methodName, returnType, parametersType);
				methodData.Add(mData);
				return mData;
			}

			public MData AddMethod(string methodName, Type returnType, MPData[] parametersType, GPData[] genericParameters) {
				if(string.IsNullOrEmpty(methodName) || returnType == null)
					throw new System.Exception("Method name or return type can't null");
				if(parametersType == null || parametersType.Length == 0) {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType &&
							(m.genericParameters == null || genericParameters == null || m.genericParameters.Count == genericParameters.Length)) {
							return m;
						}
					}
				} else {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType &&
							m.parameters != null && m.parameters.Count == parametersType.Length &&
							(m.genericParameters == null || genericParameters == null || m.genericParameters.Count == genericParameters.Length)) {
							bool correct = true;
							for(int i = 0; i < m.parameters.Count; i++) {
								if(m.parameters[i].type != parametersType[i].type) {
									correct = false;
									break;
								}
							}
							if(correct) {
								return m;
							}
						}
					}
				}
				MData mData = new MData(methodName, returnType, parametersType, genericParameters);
				methodData.Add(mData);
				return mData;
			}

			public void InsertCustomUIDMethod(string methodName, Type returnType, string ID, string contents) {
				Dictionary<Type, Dictionary<string, string>> map;
				if(!customUIDMethods.TryGetValue(methodName, out map)) {
					map = new Dictionary<Type, Dictionary<string, string>>();
					customUIDMethods[methodName] = map;
				}
				Dictionary<string, string> map2;
				if(!map.TryGetValue(returnType, out map2)) {
					map2 = new Dictionary<string, string>();
					map[returnType] = map2;
				}
				map2[ID] = contents;
			}

			public void InsertMethodCode(string methodName, Type returnType, string code, params Type[] parametersType) {
				if(parametersType == null || parametersType.Length == 0) {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType) {
							m.code += code;
							return;
						}
					}
				} else {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType && m.parameters != null && m.parameters.Count == parametersType.Length) {
							bool correct = true;
							for(int i = 0; i < m.parameters.Count; i++) {
								if(m.parameters[i].type != parametersType[i]) {
									correct = false;
									break;
								}
							}
							if(correct) {
								m.code += code;
								return;
							}
						}
					}
				}
				MData mData = new MData(methodName, returnType, parametersType) { code = code };
				methodData.Add(mData);
			}

			public void InsertMethodCode(string methodName, Type returnType, string code, params MPData[] parameters) {
				if(parameters == null || parameters.Length == 0) {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType) {
							m.code += code;
							return;
						}
					}
				} else {
					foreach(MData m in methodData) {
						if(m.name == methodName && m.type == returnType && m.parameters != null && m.parameters.Count == parameters.Length) {
							bool correct = true;
							for(int i = 0; i < m.parameters.Count; i++) {
								if(m.parameters[i].type != parameters[i].type) {
									correct = false;
									break;
								}
							}
							if(correct) {
								m.code += code;
								return;
							}
						}
					}
				}
				MData mData = new MData(methodName, returnType, parameters) { code = code };
				methodData.Add(mData);
			}
		}

        public class GeneratorSetting {
			private string _filename;
			public string fileName {
				get {
					if(string.IsNullOrEmpty(_filename)) {
						return (scriptGraph as UnityEngine.Object)?.name ?? (types.First()).name;
					}
					return _filename;
				}
				set {
					_filename = value;
				}
			}
			public string nameSpace;
			public IScriptGraph scriptGraph;
			public ICollection<UnityEngine.Object> types;

			public HashSet<string> usingNamespace;
			public HashSet<string> scriptHeaders = new HashSet<string>();
			public bool fullTypeName;
			public bool fullComment = false;
			public bool runtimeOptimization = false;

			public GenerationKind generationMode = GenerationKind.Default;

			public bool debugScript;
			public bool debugValueNode;
			public bool debugPreprocessor = false;
			public bool includeGraphInformation = true;
			[NonSerialized]
			public int debugID;

			public bool isPreview;
			public bool isAsync;
			public int maxQueue = 1;

			public int GetSettingUID() {
				string str = string.Empty;
				str += generationMode.ToString();
				if(fullTypeName) {
					str += nameof(fullTypeName);
				}
				if(fullComment) {
					str += nameof(fullComment);
				}
				if(runtimeOptimization) {
					str += nameof(runtimeOptimization);
				}
				if(debugScript) {
					str += nameof(debugScript);
				}
				if(debugValueNode) {
					str += nameof(debugValueNode);
				}
				return uNodeUtility.GetHashCode(str);
			}

			public Action<float, string> updateProgress;

			//TODO: fix me
			//public GeneratorSetting(uNodeInterface ifaceAsset) {
			//	if(ifaceAsset == null) {
			//		throw new ArgumentNullException(nameof(ifaceAsset));
			//	}
			//	graphs = new uNodeRoot[0];
			//	fileName = ifaceAsset.name;
			//	nameSpace = string.IsNullOrEmpty(ifaceAsset.@namespace) ? RuntimeType.RuntimeNamespace : ifaceAsset.@namespace;
			//	usingNamespace = ifaceAsset.usingNamespaces.ToHashSet();
			//	interfaces = new InterfaceData[] { new InterfaceData() {
			//		name = ifaceAsset.name,
			//		summary = ifaceAsset.summary,
			//		modifiers = ifaceAsset.modifiers,
			//		functions = ifaceAsset.functions,
			//		properties = ifaceAsset.properties,
			//	} };
			//}


			public GeneratorSetting(UnityEngine.Object source) {
				if(source is IScriptGraph) {
					Init(source as IScriptGraph);
				} else if(source is IGraph) {
					Init(source as IGraph);
				}
			}

			public GeneratorSetting(IGraph graph) {
				Init(graph);
			}

			public GeneratorSetting(IScriptGraph scriptGraph) {
				Init(scriptGraph);
			}

			private void Init(IGraph graph) {
				this.types = new List<UnityEngine.Object>() { graph as UnityEngine.Object };
				if (types != null) {
					if (graph is INamespaceSystem namespaceSystem) {
						nameSpace = namespaceSystem.Namespace;
						usingNamespace = new HashSet<string>(namespaceSystem.UsingNamespaces);
					} else {
						nameSpace = RuntimeType.RuntimeNamespace;
						usingNamespace = new HashSet<string>() { "UnityEngine", "System.Collections", "System.Collections.Generic" };
					}
					if (graph is ITypeWithScriptData scriptData) {
						debugScript = scriptData.ScriptData.debug;
						debugValueNode = scriptData.ScriptData.debugValueNode;
					}
				}
			}

			private void Init(IScriptGraph scriptGraph) {
				if (scriptGraph == null) {
					throw new ArgumentNullException(nameof(scriptGraph));
				}
				this.scriptGraph = scriptGraph;
				this.types = scriptGraph.TypeList.references.ToArray();
				nameSpace = scriptGraph.Namespace;
				usingNamespace = new HashSet<string>(scriptGraph.UsingNamespaces);
				debugScript = scriptGraph.ScriptData.debug;
				debugValueNode = scriptGraph.ScriptData.debugValueNode;
			}
		}

		static class ThreadingUtil {
			private static List<Action> actions = new List<Action>();
			private static int maxQueue = 1;

			public static void Do(Action action) {
				if(setting.isAsync) {
					uNodeThreadUtility.QueueOnFrame(() => {
						action();
					});
					uNodeThreadUtility.WaitUntilEmpty();
				} else {
					action();
				}
			}

			public static void WaitOneFrame() {
				if(setting.isAsync) {
					uNodeThreadUtility.WaitOneFrame();
				}
			}

			public static void WaitQueue() {
				if(setting.isAsync) {
					if(actions.Count > 0) {
						List<Action> list = new List<Action>(actions);
						uNodeThreadUtility.QueueOnFrame(() => {
							foreach(var a in list) {
								if(a != null) {
									a();
								}
							}
						});
						actions.Clear();
					}
					uNodeThreadUtility.WaitUntilEmpty();
				}
			}

			public static void Queue(Action action) {
				if(setting.isAsync) {
					if(maxQueue > 1) {
						if(actions.Count < maxQueue) {
							actions.Add(action);
						} else {
							List<Action> list = new List<Action>(actions);
							uNodeThreadUtility.QueueOnFrame(() => {
								foreach(var a in list) {
									if(a != null) {
										a();
									}
								}
							});
							actions.Clear();
						}
					} else {
						uNodeThreadUtility.QueueOnFrame(() => {
							action();
						});
					}
				} else {
					action();
				}
			}

			public static void SetMaxQueue(int max) {
				maxQueue = max;
			}
		}

		/// <summary>
		/// Used for store Attribute Data
		/// </summary>
		public class AData {
			public Type attributeType;
			public IEnumerable<string> attributeParameters;
			public Dictionary<string, string> namedParameters;

			public string GenerateCode() {
				string parameters = null;
				if(attributeParameters != null) {
					foreach(var str in attributeParameters) {
						if(string.IsNullOrEmpty(str))
							continue;
						if(!string.IsNullOrEmpty(parameters)) {
							parameters += ", ";
						}
						parameters += str;
					}
				}
				string namedParameters = null;
				if(this.namedParameters != null) {
					foreach(var pain in this.namedParameters) {
						if(string.IsNullOrEmpty(pain.Value))
							continue;
						if(!string.IsNullOrEmpty(namedParameters)) {
							namedParameters += ", ";
						}
						namedParameters += pain.Key + " = " + pain.Value;
					}
				}
				string attName = Type(this.attributeType);
				if(attName.EndsWith("Attribute")) {
					attName = attName.RemoveLast(9);
				}
				string result;
				if(string.IsNullOrEmpty(parameters)) {
					if(string.IsNullOrEmpty(namedParameters)) {
						result = "[" + attName + "]";
					}
					else {
						result = "[" + attName + "(" + namedParameters + ")]";
					}
				}
				else {
					result = "[" + attName + "(" + parameters + namedParameters.AddFirst(", ", !string.IsNullOrEmpty(parameters)) + ")]";
				}
				return result;
			}

			public AData() { }

			public AData(Type attributeType, params string[] attributeParameters) {
				this.attributeType = attributeType;
				this.attributeParameters = attributeParameters;
			}

			public AData(Type attributeType, IEnumerable<string> attributeParameters, Dictionary<string, string> namedParameters) {
				this.attributeType = attributeType;
				this.attributeParameters = attributeParameters;
				this.namedParameters = namedParameters;
			}
		}

		/// <summary>
		/// Used for store class or struct data
		/// </summary>
		public class ClassData {
			public string name;
			public string keyword = "class ";

			public string summary;
			public ClassModifier modifier;
			public Type inheritFrom;
			public HashSet<Type> implementedInterfaces = new HashSet<Type>();
			public List<GPData> genericParameters = new List<GPData>();
			public List<AData> attributes = new List<AData>();

			public string variables;
			public string properties;
			public string functions;
			public string constructors;
			public string nestedTypes;

			#region Constructors
			public ClassData() { }

			public ClassData(object target, GraphSystemAttribute graphSystem = null) {
				Setup(target, graphSystem);
			}

			public ClassData(string name, Type inheritFrom = null, ClassModifier modifier = null) {
				this.name = name;
				this.inheritFrom = inheritFrom;
				if(modifier != null) {
					this.modifier = modifier;
				}
			}
			#endregion

			public string GenerateCode(object owner = null) {
				var builder = new StringBuilder();
				builder.Append(GenerateClassSummary().AddLineInEnd());
				builder.Append(GenerateClassAttributes().AddLineInEnd());

				string genericParameters = null;
				string whereClause = null;
				if(this.genericParameters.Count > 0) {
					var gData = this.genericParameters;
					if(gData != null && gData.Count > 0) {
						genericParameters += "<";
						for(int i = 0; i < gData.Count; i++) {
							if(i != 0)
								genericParameters += ", ";
							genericParameters += gData[i].name;
						}
						genericParameters += ">";
					}
					if(gData != null && gData.Count > 0) {
						for(int i = 0; i < gData.Count; i++) {
							if(!string.IsNullOrEmpty(gData[i].type) &&
								!"object".Equals(gData[i].type) &&
								!"System.Object".Equals(gData[i].type)) {
								whereClause += " where " + gData[i].name + " : " +
									ParseType(gData[i].type);
							}
						}
					}
				}

				builder.Append(modifier?.GenerateCode() + keyword + name + genericParameters);
				if(inheritFrom != null) {
					builder.Append(" : ");
					if(generatePureScript == false && inheritFrom is RuntimeType) {
						builder.Append(inheritFrom.FullName);
					}
					else {
						builder.Append(Type(inheritFrom));
					}
				}
				if(implementedInterfaces.Count > 0) {
					if(inheritFrom == null) {
						builder.Append(" : ");
					}
					else {
						builder.Append(", ");
					}
					string interfaceName = null;
					foreach(var iface in implementedInterfaces) { 
						if(iface == null)
							continue;
						if(!string.IsNullOrEmpty(interfaceName)) {
							interfaceName += ", ";
						}
						interfaceName += Type(iface);
					}
					builder.Append(interfaceName);
				}
				if(!string.IsNullOrEmpty(whereClause)) {
					builder.Append(" : ");
					builder.Append(whereClause);
				}

				builder.Append(" {");

				var builder2 = new StringBuilder();
				if(!string.IsNullOrEmpty(variables)) {
					builder2.AppendLine();
					builder2.Append(variables);
					builder2.AppendLine();
				}
				if(!string.IsNullOrEmpty(properties)) {
					builder2.AppendLine();
					builder2.Append(properties);
					builder2.AppendLine();
				}
				if(!string.IsNullOrEmpty(constructors)) {
					builder2.AppendLine();
					builder2.Append(constructors);
					builder2.AppendLine();
				}
				if(!string.IsNullOrEmpty(functions)) {
					builder2.AppendLine();
					builder2.Append(functions);
					builder2.AppendLine();
				}
				if(!string.IsNullOrEmpty(nestedTypes)) {
					builder2.AppendLine();
					builder2.Append(nestedTypes);
					builder2.AppendLine();
				}
				if(owner == null) {
					builder.Append(builder2.ToString().AddTabAfterNewLine(1, false));
				} else {
					builder.Append(CG.WrapWithInformation(builder2.ToString().AddTabAfterNewLine(1, false), owner));
				}

				builder.Append("}");
				return builder.ToString();
			}

			#region Utilities
			public void RegisterAttribute(Type type, params string[] parameters) {
				attributes.Add(new AData(type, parameters));
			}

			public void RegisterVariable(string contents) {
				if(string.IsNullOrEmpty(variables)) {
					variables = contents;
				}
				else {
					variables += contents.AddFirst("\n");
				}
			}

			public void RegisterProperty(string contents) {
				if(string.IsNullOrEmpty(properties)) {
					properties = contents;
				}
				else {
					properties += contents.AddFirst("\n\n");
				}
			}

			public void RegisterFunction(string contents) {
				if(string.IsNullOrEmpty(functions)) {
					functions = contents;
				}
				else {
					functions += contents.AddFirst("\n\n");
				}
			}

			public void RegisterConstructor(string contents) {
				if(string.IsNullOrEmpty(constructors)) {
					constructors = contents;
				}
				else {
					constructors += contents.AddFirst("\n\n");
				}
			}

			public void RegisterNestedType(string contents) {
				if(string.IsNullOrEmpty(nestedTypes)) {
					nestedTypes = contents;
				}
				else {
					nestedTypes += contents.AddFirst("\n\n");
				}
			}

			public void SetTypeToClass() {
				keyword = "class ";
			}

			public void SetTypeToStruct() {
				keyword = "struct ";
			}

			public void SetTypeToInterface() {
				keyword = "interface ";
			}

			public void SetToPartial() {
				if(modifier == null)
					modifier = new ClassModifier();
				modifier.Partial = true;
			}
			#endregion

			#region Private Functions
			private void Setup(object target, GraphSystemAttribute graphSystem = null) {
				if(target is IGraph graph) {
					summary = graph.GraphData.comment;
				}
				if(target is IAttributeSystem) {
					foreach(var attribute in (target as IAttributeSystem).Attributes) {
						if(attribute == null)
							continue;
						AData aData = TryParseAttributeData(attribute);
						if(aData != null) {
							attributes.Add(aData);
						}
					}
				}
				if(target is IClassModifier) {
					modifier = (target as IClassModifier).GetModifier();
				}
				else if(target is IInterfaceModifier) {
					var interfaceModifier = (target as IInterfaceModifier).GetModifier();
					modifier = new ClassModifier {
						Public = interfaceModifier.Public,
						Private = interfaceModifier.Private,
						Protected = interfaceModifier.Protected,
						Internal = interfaceModifier.Internal
					};
				}
				else {
					modifier = new ClassModifier();
				}
				if(target is IInterfaceSystem) {//Implementing interfaces
					var interfaces = (target as IInterfaceSystem).Interfaces.Where(item => item != null && item.type != null).Select(item => item.type);
					if(interfaces != null) {
						implementedInterfaces.Clear();
						foreach(var iface in interfaces) {
							implementedInterfaces.Add(iface);
						}
					}
				}
				if(target is IGenericParameterSystem && (graphSystem == null || graphSystem.supportGeneric)) {//Implementing generic parameters
					var gData = (target as IGenericParameterSystem).GenericParameters.Select(i => new GPData(i.name, i.typeConstraint.type)).ToList();
					if(gData != null) {
						this.genericParameters = gData;
					}
				}
				Type InheritedType;
				if(graphSystem != null && graphSystem.inherithFrom != null) {
					InheritedType = graphSystem.inherithFrom;
				}
				else if(target is IClassDefinition) {
					InheritedType = (target as IClassDefinition).GetModel().ScriptInheritType;
				}
				else if(target is IClassGraph) {
					InheritedType = (target as IClassGraph).InheritType;
					if(InheritedType == typeof(ValueType)) {
						//In case it is struct
						InheritedType = null;
						SetTypeToStruct();
					}
					else if(InheritedType == null) {
						//In case it is interface
						SetTypeToInterface();
					}
				}
				else if(target is IScriptInterface) {
					InheritedType = null;
					SetTypeToInterface();
				}
				else {
					InheritedType = null;
				}
				if(InheritedType != null && InheritedType != typeof(object)) {
					inheritFrom = InheritedType;
				}
			}

			private string GenerateClassAttributes() {
				return string.Join('\n', attributes.Select(a => a.GenerateCode()));
			}

			private string GenerateClassSummary() {
				if(!string.IsNullOrEmpty(summary)) {
					return "/// <summary>".AddLineInEnd() +
						"/// " + summary.Replace("\n", "\n" + "/// ").AddLineInEnd() +
						"/// </summary>";
				}
				return null;
			}
			#endregion
		}

		/// <summary>
		/// Used for store Variable Data
		/// </summary>
		public class VData {
			/// <summary>
			/// The name of variable
			/// </summary>
			public string name;
			/// <summary>
			/// The summary of variable.
			/// </summary>
			public string summary;
			/// <summary>
			/// The object reference of variable.
			/// </summary>
			public object reference;

			private Type _type;
			/// <summary>
			/// The variable type.
			/// </summary>
			public Type type {
				get {
					if(_type == null) {
						if(reference is Variable) {
							_type = (reference as Variable).type;
						} else if(reference is VariableData) {
							_type = (reference as VariableData).type;
						}
					}
					return _type;
				}
				set {
					_type = value;
				}
			}
			/// <summary>
			/// The default value
			/// </summary>
			public object defaultValue;
			/// <summary>
			/// Is the variable is instance or owned by classes.
			/// </summary>
			public bool isInstance;
			/// <summary>
			/// The variable modifiers
			/// </summary>
			public FieldModifier modifier;
			/// <summary>
			/// The variable attributes.
			/// </summary>
			public IList<AData> attributes;

			public void SetToInstanceVariable() {
				isInstance = true;
				if(CG.generationState.isStatic) {
					if(modifier == null) {
						modifier = new FieldModifier() {
							Public = false,
						};
					}
					modifier.Static = true;
				}
			}
			public void SetToLocalVariable() => isInstance = false;

			#region Constructor
			public VData(
				string name,
				Type type,
				bool isInstance = true,
				bool autoCorrection = true) {

				if(autoCorrection) {
					this.name = GenerateNewName(name);
				} else {
					this.name = name;
				}
				this.type = type;
				this.isInstance = isInstance;
			}
			#endregion

			public string GenerateCode() {
				string result = null;
				if(attributes != null) {
					foreach(AData att in attributes) {
						string code = att.GenerateCode();
						if(!string.IsNullOrEmpty(code)) {
							result += code.AddFirst("\n", !string.IsNullOrEmpty(result));
						}
					}
				}
				string m = null;
				if(modifier != null) {
					m = modifier.GenerateCode();
				}
				bool isGeneric = false;
				string vType;
				if(reference is Variable) {
					vType = Type((reference as Variable).type);
					isGeneric = (reference as Variable).isOpenGeneric;
				} else {
					vType = Type(type);
				}
				if(ReflectionUtils.IsNativeType(type) == false) {
					if(type is IFakeType) {
						//If it is a fake type and not native type
						vType = Type((type as IFakeType).GetNativeType());
					}
					//don't generate default value when the type is not native (CLR) type
					defaultValue = null;
				}
				if(type == null || defaultValue is IInstancedGraph) {
					defaultValue = null;
				}
				if(isGeneric) {
					if(defaultValue != null) {
						result += (m + vType + " " + name + " = default(" + vType + ");").AddFirst("\n", !string.IsNullOrEmpty(result));
					} else {
						result += (m + vType + " " + name + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
					}
				} else {
					if(!ReflectionUtils.IsNullOrDefault(defaultValue) && !(graph is IClassGraph classGraph && classGraph.InheritType == typeof(ValueType))) {
						if(defaultValue is IGraph obj && obj != graph) {
							result += (m + vType + " " + name + " = " + Value(defaultValue) + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
						} else {
							result += (m + vType + " " + name + " = " + Value(defaultValue) + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
						}
					} else {
						result += (m + vType + " " + name + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
					}
				}
				if(!string.IsNullOrEmpty(summary)) {
					result = "/// <summary>".AddLineInEnd() +
						"/// " + summary.Replace("\n", "\n" + "/// ").AddLineInEnd() +
						"/// </summary>" +
						result.AddLineInFirst();
				}
				return result;
			}

			public bool IsStatic {
				get {
					return modifier != null && modifier.Static;
				}
			}

			public override string ToString() {
				if(!string.IsNullOrEmpty(name)) {
					return name;
				}
				return base.ToString();
			}
		}

		/// <summary>
		/// Used for store Constructor Data
		/// </summary>
		public class CData {
			public string name {
				get {
					return obj.graphContainer.GetGraphName();
				}
			}
			public string summary;
			public Constructor obj;
			public ConstructorModifier modifier;

			public CData(Constructor constructor) {
				this.obj = constructor;
				this.summary = constructor.comment;
			}

			public string GenerateCode() {
				string result = null;
				string m = null;
				if(modifier != null) {
					m = modifier.GenerateCode();
				}
				string code = GeneratePort(obj.Entry.nodeObject.primaryFlowOutput);
				string parameters = null;
				if(obj.parameters != null && obj.parameters.Count > 0) {
					int index = 0;
					var parametersData = obj.parameters.Select(i => new MPData(i.name, i.type, i.refKind));
					foreach(MPData data in parametersData) {
						if(index != 0) {
							parameters += ", ";
						}
						parameters += data.GenerateCode();
						index++;
					}
				}
				string localVar = M_GenerateLocalVariable(obj.LocalVariables).AddLineInFirst().AddTabAfterNewLine();
				result += (m + name + "(" + parameters + ") {" + localVar.Add("\n", string.IsNullOrEmpty(code)) + code.AddTabAfterNewLine().AddLineInEnd() + "}").AddFirst("\n", !string.IsNullOrEmpty(result));
				if(!string.IsNullOrEmpty(summary)) {
					result = "/// <summary>".AddLineInEnd() +
						"/// " + summary.Replace("\n", "\n" + "/// ").AddLineInEnd() +
						"/// </summary>" +
						result.AddLineInFirst();
				}
				return result;
			}
		}

		/// <summary>
		/// Used for store Property Data
		/// </summary>
		public class PData {
			public string name {
				get {
					return obj.name;
				}
			}
			public string summary;
			public Property obj;
			public PropertyModifier modifier;
			public IList<AData> attributes;

			public PData(Property property) {
				obj = property;
				summary = property.comment;
			}

			public PData(Property property, IList<AData> attributes) {
				obj = property;
				summary = property.comment;
				this.attributes = attributes;
			}

			public string GenerateCode() {
				string result = null;
				foreach(AData a in attributes) {
					string code = a.GenerateCode();
					if(!string.IsNullOrEmpty(code)) {
						result += code.AddFirst("\n", !string.IsNullOrEmpty(result));
					}
				}
				bool autoProperty = obj.AutoProperty;
				if(autoProperty) {
					if(obj.fieldAttributes != null) {
						foreach(var att in obj.fieldAttributes) {
							var a = TryParseAttributeData(att);
							string code = a.GenerateCode();
							if(!string.IsNullOrEmpty(code)) {
								result += code.Insert(1, "field: ").AddFirst("\n", !string.IsNullOrEmpty(result));
							}
						}
					}
				}
				string m = null;
				if(modifier != null) {
					m = modifier.GenerateCode();
				}
				string p = null;
				if(autoProperty) {
					p += "{\n";
					string getter;
					if(obj.CanGetValue()) {
						getter = CG.Flow(obj.getterAttributes.Select(a => TryParseAttributeData(a).GenerateCode()).ToArray()).AddLineInEnd() +
							"get;".AddFirst(obj.getterModifier.GenerateCode(), !obj.getterModifier.isPublic);
					}
					else {
						getter = null;
					}
					string setter;
					if(obj.CanSetValue()) {
						setter = CG.Flow(obj.setterAttributes.Select(a => TryParseAttributeData(a).GenerateCode()).ToArray()).AddLineInEnd() +
						"set;".AddFirst(obj.setterModifier.GenerateCode(), !obj.setterModifier.isPublic);
					}
					else {
						setter = null;
					}
					p += CG.Flow(getter, setter).AddTabAfterNewLine() + "\n}";
				} else {
					p += "{\n";
					if(obj.CanGetValue()) {
						var str = "get {\n";
						if(!obj.getterModifier.isPublic) {
							str = str.Insert(0, obj.getterModifier.GenerateCode());
						}
						if(obj.getRoot.LocalVariables.Any()) {
							string lv = M_GenerateLocalVariable(obj.getRoot.LocalVariables);
							str += lv.AddLineInFirst().AddTabAfterNewLine();
						}
						str += GeneratePort(obj.getRoot.Entry.nodeObject.primaryFlowOutput).AddTabAfterNewLine();
						str += "\n}";
                        if(obj.getRoot.attributes.Count > 0) {
                            str = CG.Flow(obj.getRoot.attributes.Select(a => TryParseAttributeData(a).GenerateCode())).AddLineInEnd().Add(str);
                        }
                        p += str.AddTabAfterNewLine() + "\n";
					}
					if(obj.CanSetValue()) {
						var str = "set {\n";
						if(!obj.setterModifier.isPublic) {
							str = str.Insert(0, obj.setterModifier.GenerateCode());
						}
						if(obj.setRoot.LocalVariables.Any()) {
							string lv = M_GenerateLocalVariable(obj.setRoot.LocalVariables);
							str += lv.AddLineInFirst().AddTabAfterNewLine();
						}
						str += GeneratePort(obj.setRoot.Entry.nodeObject.primaryFlowOutput).AddTabAfterNewLine();
						str += "\n}";
                        if(obj.setRoot.attributes.Count > 0) {
                            str = CG.Flow(obj.setRoot.attributes.Select(a => TryParseAttributeData(a).GenerateCode())).AddLineInEnd().Add(str);
                        }
                        p += str.AddTabAfterNewLine() + "\n";
					}
					p += "}";
				}
				result += (m + DeclareType(obj.ReturnType()) + " " + name + " " + p).AddFirst("\n", !string.IsNullOrEmpty(result));
				if(!string.IsNullOrEmpty(summary)) {
					result = "/// <summary>".AddLineInEnd() +
						"/// " + summary.Replace("\n", "\n" + "/// ").AddLineInEnd() +
						"/// </summary>" +
						result.AddLineInFirst();
				}
				return result;
			}
		}

		/// <summary>
		/// Used for store Parameter Data
		/// </summary>
		public class MPData {
			public string name;
			public Type type;
			public RefKind refKind;
			public List<AData> attributes;

			public void RegisterAttribute(Type type, params string[] parameters) {
				if(attributes == null)
					attributes = new List<AData>();
				attributes.Add(new AData(type, parameters));
			}

			public string GenerateCode() {
				string result = null;
				if(attributes != null) {
					foreach(var att in attributes) {
						result += att.GenerateCode().Add(" ");
					}
				}
				switch(refKind) {
					case RefKind.In:
						result += "in ";
						break;
					case RefKind.Out:
						result += "out ";
						break;
					case RefKind.Ref:
						result += "ref ";
						break;
				}
				result += DeclareType(type) + " ";
				if(string.IsNullOrEmpty(name)) {
					result += CG.GenerateNewName("parameter");
				}
				else {
					result += name;
				}
				return result;
			}

			public MPData(string name, Type type, RefKind refKind = RefKind.None) {
				this.name = name;
				this.type = type;
				this.refKind = refKind;
			}
		}

		/// <summary>
		/// Used for store Generic Parameter Data
		/// </summary>
		public class GPData {
			public string name;
			public string type;

			public GPData() { }
			public GPData(string name) {
				this.name = name;
			}
			public GPData(string name, Type type) {
				this.name = name;
				if(type != typeof(object)) {
					this.type = type.FullName;
				}
			}
			public GPData(string name, string type) {
				this.name = name;
				if(type != typeof(object).FullName) {
					this.type = type;
				}
			}
		}

		/// <summary>
		/// Used for store Method Data
		/// </summary>
		public class MData {
			public string name;
			public Type type;
			public IList<MPData> parameters;
			public IList<GPData> genericParameters;
			public List<AData> attributes;
			public string code;
			public FunctionModifier modifier;
			public string summary;

			public UGraphElement owner;

			private List<(int priority, string code)> codeList = new List<(int, string)>();
			private HashSet<float> ownerUID = new HashSet<float>();

			public string GenerateCode() {
				string result = null;
				bool isExtension = false;
				if(attributes != null && attributes.Count > 0) {
					foreach(AData attribute in attributes) {
						if(attribute.attributeType == typeof(System.Runtime.CompilerServices.ExtensionAttribute)) {
							isExtension = true;
							continue;
						}
						string a = attribute.GenerateCode();
						if(!string.IsNullOrEmpty(a)) {
							result += a.AddLineInEnd();
						}
					}
				}
				if(graph is IIndependentGraph) {
					if(name == "Awake") {
						//Ensure to change the Awake to OnAwake for IndependentGraph
						name = "OnAwake";
						if(modifier == null)
							modifier = new FunctionModifier();
						else
							modifier = SerializerUtility.Duplicate(modifier);
						modifier.SetPublic();
						modifier.Override = true;
					} else if(name == "OnEnable") {
						//Ensure to change the OnEnable to OnEnabled for IndependentGraph
						name = "OnBehaviourEnable";
						if(modifier == null)
							modifier = new FunctionModifier();
						else
							modifier = SerializerUtility.Duplicate(modifier);
						modifier.SetPublic();
						modifier.Override = true;
					}
				}
				if(modifier != null)
					result += modifier.GenerateCode();
				result += DeclareType(type) + " " + name;
				if(genericParameters != null && genericParameters.Count > 0) {
					result += "<";
					for(int i = 0; i < genericParameters.Count; i++) {
						if(i != 0)
							result += ", ";
						result += genericParameters[i].name;
					}
					result += ">";
				}
				result += "(";
				if(parameters != null) {
					int index = 0;
					foreach(MPData data in parameters) {
						if(index != 0) {
							result += ", ";
						}
						else if(isExtension) {
							result += "this ";
						}
						result += data.GenerateCode();
						index++;
					}
				}
				string genericData = null;
				if(genericParameters != null && genericParameters.Count > 0) {
					for(int i = 0; i < genericParameters.Count; i++) {
						if(!string.IsNullOrEmpty(genericParameters[i].type) &&
							!"object".Equals(genericParameters[i].type) &&
							!"System.Object".Equals(genericParameters[i].type)) {
							genericData += "where " + genericParameters[i].name + " : " +
								ParseType(genericParameters[i].type) + " ";
						}
					}
				}
				if(modifier != null && modifier.Abstract) {
					result += ");";
					if(owner != null && includeGraphInformation) {
						result = WrapWithInformation(result, owner);
					}
					return result;
				}
				if(!string.IsNullOrEmpty(summary)) {
					result = "/// <summary>".AddLineInEnd() +
						"/// " + summary.Replace("\n", "\n" + "/// ").AddLineInEnd() +
						"/// </summary>" +
						result.AddLineInFirst();
				}
				var codeList = this.codeList.ToList();
				codeList.Insert(0, (0, code));
				codeList.Sort((x, y) => CompareUtility.Compare(x.priority, y.priority));
				string contents = null;
				foreach(var pair in codeList) {
					contents += pair.code.AddFirst("\n");
				}
				if(owner != null && (
					owner.graphContainer is IScriptInterface && string.IsNullOrEmpty(contents) || 
					owner.graphContainer is IReflectionType typeContainer && typeContainer.ReflectionType.IsInterface)) 
				{
					//In case it is interface
					return result + ") " + genericData + ";";
				}
				result += ") " + genericData + "{" + contents.AddTabAfterNewLine(1);
				result += "\n}";
				if(owner != null && includeGraphInformation) {
					result = WrapWithInformation(result, owner);
				}
				return result;
			}

			#region Constructors
			public MData(string name, Type returnType) {
				this.code = "";
				this.name = name;
				this.type = returnType;
			}

			public MData(string name, Type returnType, IList<Type> parametersType) {
				this.code = "";
				this.name = name;
				this.type = returnType;
				if(parametersType != null && parametersType.Count > 0) {
					List<MPData> pData = new List<MPData>();
					for(int i = 0; i < parametersType.Count; i++) {
						pData.Add(new MPData("parameter" + i, parametersType[i]));
					}
					this.parameters = pData;
				}
			}

			public MData(string name, Type returnType, IList<Type> parametersType, IList<Type> genericParameters = null) {
				this.code = "";
				this.name = name;
				this.type = returnType;
				if(parametersType != null && parametersType.Count > 0) {
					List<MPData> pData = new List<MPData>();
					for(int i = 0; i < parametersType.Count; i++) {
						pData.Add(new MPData("parameter" + i, parametersType[i]));
					}
					this.parameters = pData;
				}
				if(genericParameters != null) {
					this.genericParameters = genericParameters.Select(i => new GPData(Type(i))).ToList();
				}
			}

			public MData(string name, Type returnType, IList<MPData> parameters, IList<GPData> genericParameters = null) {
				this.code = "";
				this.name = name;
				this.type = returnType;
				this.parameters = parameters;
				if(genericParameters != null) {
					this.genericParameters = genericParameters;
				}
			}

			public MData(string name, Type returnType, string code, IList<MPData> parameters, IList<GPData> genericParameters = null) {
				this.name = name;
				this.type = returnType;
				this.code = code;
				this.parameters = parameters;
				if(genericParameters != null) {
					this.genericParameters = genericParameters;
				}
			}
			#endregion

			#region Function
			public void RegisterAttribute(Type type, params string[] parameters) {
				if(attributes == null)
					attributes = new List<AData>();
				attributes.Add(new AData(type, parameters));
			}

			public void RegisterAttribute(AData data) {
				if(attributes == null)
					attributes = new List<AData>();
				attributes.Add(data);
			}

			public MData SetToPublic() {
				if(modifier == null)
					modifier = new FunctionModifier();
				modifier.SetPublic();
				return this;
			}

			public MData SetToPrivate() {
				if(modifier == null)
					modifier = new FunctionModifier();
				modifier.SetPrivate();
				return this;
			}

			public void AddCodeForEvent(string code) {
				AddCode(code, -100);
			}

			public void AddCode(string code, int priority = 0) {
				codeList.Add((priority, code));
			}

			public void AddCode(string code, UnityEngine.Object owner, int priority = 0) {
				AddCode(code, owner.GetInstanceID(), priority);
			}

			public void AddCode(string code, float ownerID, int priority) {
				if(ownerUID.Contains(ownerID)) {
					return;
				}
				ownerUID.Add(ownerID);
				codeList.Add((priority, code));
			}

			public void ClearCode() {
				codeList.Clear();
				ownerUID.Clear();
			}
			#endregion
		}

		public class GeneratedData {
			public Dictionary<object, string> classNames = new Dictionary<object, string>();
			public string fileName {
				get {
					if(graphOwner != null) {
						return graphOwner.name;
					}
					return setting.fileName;
				}
			}
			public string Namespace => setting.nameSpace;

			public UnityEngine.Object graphOwner;
			public UnityEngine.Object[] types;
			public List<Exception> errors;

			public int graphUID => uNodeUtility.GetObjectID(graphOwner);

			public bool hasError => errors != null && errors.Count > 0;

			public bool isValid => classes.Count > 0 && !hasError;

			public event Func<string, string> postScriptModifier;

			private GeneratorSetting setting;
			private Dictionary<object, ClassData> classes = new Dictionary<object, ClassData>();
			private StringBuilder scriptBuilder;

			public GeneratedData(GeneratorSetting setting) {
				this.setting = setting;
			}

			public void BuildScript() {
				scriptBuilder = new StringBuilder();
				foreach(var (owner, builder) in classes) {
					var classResult = builder.GenerateCode(owner);
					if(!string.IsNullOrEmpty(classResult)) {
						scriptBuilder.Append(classResult.AddLineInEnd());
					}
				}
			}

			public void RegisterClass(object owner, ClassData builder) {
				classes[owner] = builder;
			}
			
			public void InitOwner() {
				if(graphOwner == null) {
					if(setting.scriptGraph != null) {
						graphOwner = setting.scriptGraph as UnityEngine.Object;
					} else {
						var obj = setting.types.Where(g => g != null).Select(g => g).FirstOrDefault();
						graphOwner = obj;
					}
				}
				types = setting.types.ToArray();
			}

			public int GetSettingUID() {
				return setting.GetSettingUID();
			}

			/// <summary>
			/// Generate Script
			/// </summary>
			/// <returns></returns>
			public string ToScript() {
				if(setting.includeGraphInformation) {
					string str = DoToScript();
					CollectGraphInformations(str, out str);
					return str;
				}
				return DoToScript();
			}

			/// <summary>
			/// Generate Script
			/// </summary>
			/// <returns></returns>
			public string ToScript(out List<ScriptInformation> informations) {
				if(setting.includeGraphInformation) {
					string str = DoToScript();
					informations = CollectGraphInformations(str, out str);
					return str;
				}
				informations = null;
				return DoToScript();
			}

			/// <summary>
			/// Generate Script
			/// </summary>
			/// <returns></returns>
			public string ToScript(out List<ScriptInformation> informations, bool polishInformation) {
				if(setting.includeGraphInformation) {
					string str = DoToScript();
					informations = CollectGraphInformations(str, out str);
					if(polishInformation) {
						PolishInformations(informations);
					}
					return str;
				}
				informations = null;
				return DoToScript();
			}

			/// <summary>
			/// Polish informations for persistance ID so it can be saved to file for future use.
			/// </summary>
			/// <param name="informations"></param>
			public void PolishInformations(List<ScriptInformation> informations) {
				foreach(var info in informations) { 
					if(int.TryParse(info.id, out var id)) {
						info.ghostID = info.id;
						info.id = id.ToString();
					}
				}
			} 

			public string ToRawScript() {
				return DoToScript();
			}
			
			private string DoToScript() {
				if(scriptBuilder == null)
					throw new Exception($"Plaese ensure to call {nameof(BuildScript)} first.");
				string script = scriptBuilder.ToString();
				StringBuilder builder = new StringBuilder();
				if(setting.scriptHeaders != null) {
					foreach(var header in setting.scriptHeaders) {
						builder.AppendLine(header);
					}
				}
				if(setting.usingNamespace != null) {
					foreach(var ns in setting.usingNamespace) {
						if(string.IsNullOrEmpty(ns)) continue;
						builder.AppendLine("using " + ns + ";");
					}
					builder.AppendLine("");
				}
				if(!string.IsNullOrEmpty(setting.nameSpace)) {
					builder.AppendLine("namespace " + setting.nameSpace + " {");
					builder.Append(script.AddTabAfterNewLine(1, false));
					builder.Append("\n}");
				} else {
					builder.Append(script);
				}

				StringBuilder result = new StringBuilder();
				result.AppendLine(builder.ToString());
				if(postScriptModifier != null) {
					return postScriptModifier(result.ToString());
				}
				return result.ToString();
			}

			private class GraphInformationToken {
				public string value;
				public int line;
				public int column;

				public bool isStart => value.StartsWith("/*" + KEY_INFORMATION_HEAD, StringComparison.Ordinal);
				public bool isEnd => value.StartsWith("/*" + KEY_INFORMATION_TAIL, StringComparison.Ordinal);

				private string id;

				public string GetID() {
					if(id == null) {
						id = value.RemoveLast(2).Remove(0, 3);
					}
					return id;
				}
			}

			public List<ScriptInformation> CollectGraphInformations(string input, out string output) {
				var strs = input.Split('\n').ToList();
				var information = new List<GraphInformationToken>();
				int addedInformation = 0;
				for (int x = 0; x < strs.Count;x++) {
					string str = strs[x];
					addedInformation = 0;
					string match = null;
					int index = -1;
					for (int y = 0; y < str.Length;y++) {
						char c = str[y];
						match += c;
						if (0 > index) {
							if (c == '/') {
								match = null;
								match += c;
							} else if (match.Length == 3) {
								if (match == "/*" + KEY_INFORMATION_HEAD || match == "/*" + KEY_INFORMATION_TAIL) {
									index = y - 2;
								}
							}
						} else {
							if(c == '/' && match.EndsWith("*/")) {
								addedInformation++;
								information.Add(new GraphInformationToken() {
									value = match,
									line = x,
									column = index,
								});
								str = str.Remove(index, match.Length);
								strs[x] = str;
								if(string.IsNullOrWhiteSpace(str)) {
									for (int i = 1; i - 1 < addedInformation; i++) {
										information[information.Count - i].column = 0;
									}
									strs.RemoveAt(x);
									x--;
									break;
								} else {
									// if(index + 2 > str.Length) {
									// 	information[information.Count - 1].line++;
									// 	information[information.Count - 1].column = 0;
									// }
									y = index - 1;
									match = null;
									index = -1;
								}
							}
						}
					}
				}
				List<ScriptInformation> referenceInformations = new List<ScriptInformation>();
				List<ScriptInformation> infos = new List<ScriptInformation>();
				for (int x = 0; x < information.Count;x++) {
					if(information[x].isEnd) continue;
					var startID = information[x].GetID();
					int deep = 0;
					for (int y = x + 1; y < information.Count;y++) {
						var endID = information[y].GetID();
						if(startID == endID) {
							if(information[y].isStart) {
								deep++;
							} else if(information[y].isEnd) {
								deep--;
								if(0 > deep) {
									if (startID.StartsWith(KEY_INFORMATION_REFERENCE)) {
										referenceInformations.Add(new ScriptInformation() {
											id = startID.Substring(KEY_INFORMATION_REFERENCE.Length),
											startLine = information[x].line,
											startColumn = information[x].column,
											endLine = information[y].line,
											endColumn = information[y].column,
										});
									}
									else {
										infos.Add(new ScriptInformation() {
											id = startID,
											startLine = information[x].line,
											startColumn = information[x].column,
											endLine = information[y].line,
											endColumn = information[y].column,
										});
									}
									information.RemoveAt(y);
									break;
								}
							}
						}
					}
				}
				output = string.Join("\n", strs);
				if(referenceInformations.Count > 0) {
					referenceInformations.Sort((x, y) => {
						return CompareUtility.Compare(x.columnRange, y.columnRange);
					});
					foreach(var info in infos) {
						foreach(var refInfo in referenceInformations) {
							//if(info.columnRange > refInfo.columnRange) continue;
							if (info.startLine < refInfo.startLine) continue;
							if (info.endLine > refInfo.endLine) continue;
							if (info.startLine == refInfo.startLine && info.startColumn < refInfo.startColumn) continue;
							if (info.endLine == refInfo.endLine && info.endColumn > refInfo.endColumn) continue;
							info.ownerID = int.Parse(refInfo.id);
							break;
						}
					}
				}
				// Debug.Log(input);
				// foreach(var info in infos) {
				// 	Debug.Log($"{info.id} on line {info.startLine} : {info.startColumn} - {info.endLine} : {info.endColumn}");
				// }
				// var lines = input.Split('\n');
				// for (int i = 0;i<lines.Length;i++) {
				// 	Debug.Log($"line {i + 1} : {lines[i]}");
				// }
				return infos;
			}

			/// <summary>
			/// Generate Full Script without any namespace
			/// </summary>
			/// <returns></returns>
			public string FullTypeScript() {
				string script = scriptBuilder.ToString();
				return script;
			}
		}
	}
}