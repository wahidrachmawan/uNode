using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public class GraphCreatorWindow : EditorWindow {
		private GraphCreator graphCreator;
		private Vector2 scrollPos;

		#region Window
		private static GraphCreatorWindow window;

		[MenuItem("Tools/uNode/Create New Graph", false, 1)]
		public static GraphCreatorWindow ShowWindow() {
			window = GetWindow<GraphCreatorWindow>(true);
			window.minSize = new Vector2(300, 250);
			window.titleContent = new GUIContent("Create New Graph");
			window.Show();
			return window;
		}

		public static GraphCreatorWindow ShowWindow(Type graphType) {
			window = ShowWindow();
			if(graphType == typeof(ClassDefinition)) {
				window.graphCreator = FindGraphCreators().First(g => g is ClassDefinitionCreator);
			}
			return window;
		}
		#endregion

		void OnGUI() {
			if (graphCreator == null) {
				graphCreator = FindGraphCreators().FirstOrDefault(item => item is ClassDefinitionCreator);
				if(graphCreator == null) {
					graphCreator = FindGraphCreators().FirstOrDefault();
				}
				if (graphCreator == null) {
					EditorGUILayout.HelpBox("No Graph Creator found", MessageType.Error);
					return;
				}
			}
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			var rect = EditorGUI.PrefixLabel(uNodeGUIUtility.GetRect(), new GUIContent("Graph"));
			if (GUI.Button(rect, new GUIContent(graphCreator.menuName), EditorStyles.popup)) {
				var creators = FindGraphCreators();
				GenericMenu menu = new GenericMenu();
				for (int i = 0; i < creators.Count; i++) {
					var creator = creators[i];
					menu.AddItem(new GUIContent(creator.menuName), graphCreator == creator, () => {
						graphCreator = creator;
					});
				}
				menu.ShowAsContext();
				Event.current.Use();
			}
			graphCreator.OnGUI();
			EditorGUILayout.EndScrollView();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Save")) {
				var obj = graphCreator.CreateAsset();
				string startPath = "Assets";
				var guids = Selection.assetGUIDs;
				foreach(var guid in guids) {
					startPath = AssetDatabase.GUIDToAssetPath(guid);
					if(!AssetDatabase.IsValidFolder(startPath)) {
						var pts = startPath.Split('/').ToArray();
						startPath = string.Join("/", pts, 0, pts.Length - 1);
					}
					break;
				}
				if(obj is GameObject gameObject) {
					string path = EditorUtility.SaveFilePanelInProject("Create new graph asset",
						gameObject.name + ".prefab",
						"prefab",
						"Please enter a file name to save the graph to",
						startPath);
					if (path.Length != 0) {
						PrefabUtility.SaveAsPrefabAsset(gameObject, path);
						Close();
					}
					DestroyImmediate(gameObject);
				} else if(obj is ScriptableObject asset) {
					string path = EditorUtility.SaveFilePanelInProject("Create new graph asset",
							asset.name + ".asset",
							"asset",
							"Please enter a file name to save the graph to",
							startPath);
					if (path.Length != 0) {
						AssetDatabase.CreateAsset(asset, path);
						if(obj is IScriptGraph scriptGraph) {
							foreach(var subAsset in scriptGraph.TypeList.references) {
								if(subAsset != null) {
									AssetDatabase.AddObjectToAsset(subAsset, obj);
								}
							}
						}
						AssetDatabase.SaveAssets();
						Close();
						EditorGUIUtility.PingObject(obj);
						if(asset is IGraph) {
							uNodeEditor.Open(asset as IGraph);
						}
						else if(asset is IScriptGraph) {
							uNodeEditor.Open(asset as IScriptGraph);
						}
						else if(asset is IScriptGraphType) {
							uNodeEditor.Open(asset as IScriptGraphType);
						}
					}
				} else {
					throw new InvalidOperationException();
				}
			}
		}

		private static List<GraphCreator> _graphCreators;
		public static List<GraphCreator> FindGraphCreators() {
			if (_graphCreators == null) {
				_graphCreators = EditorReflectionUtility.GetListOfType<GraphCreator>();
				_graphCreators.Sort((x, y) => {
					return CompareUtility.Compare(x.menuName, x.order, y.menuName, y.order);
				});
			}
			return _graphCreators;
		}
	}

	/// <summary>
	/// Class for easy create graph with Graph Creator window
	/// </summary>
	public abstract class GraphCreator {
		public abstract string menuName { get; }
		public virtual int order => 0;
		public virtual bool IsNativeGraph => false;
		public abstract void OnGUI();
		public abstract Object CreateAsset();

		#region Fields
		protected string graphNamespaces;
		protected List<string> graphUsingNamespaces = new List<string>() {
			"UnityEngine",
			"System.Collections.Generic",
		};
		protected List<SerializedType> graphInterfaces = new List<SerializedType>();
		protected Texture2D graphIcon;
		protected List<UnityEventType> graphUnityEvents = new List<UnityEventType>() {
			UnityEventType.Start,
			UnityEventType.Update,
		};
		protected List<MemberInfo> graphOverrideMembers = new List<MemberInfo>();
		protected SerializedType graphInheritFrom = typeof(object);
		protected FilterAttribute graphInheritFilter = FilterAttribute.DefaultInheritFilter;

		protected List<VariableData> graphVariables = new List<VariableData>();

		protected GraphLayout graphLayout = GraphLayout.Vertical;
		#endregion

		#region Enums
		public enum UnityEventType {
			Awake,
			Start,
			Update,
			FixedUpdate,
			LateUpdate,
			OnAnimatorIK,
			OnAnimatorMove,
			OnApplicationFocus,
			OnApplicationPause,
			OnApplicationQuit,
			OnBecameInvisible,
			OnBecameVisible,
			OnCollisionEnter,
			OnCollisionEnter2D,
			OnCollisionExit,
			OnCollisionExit2D,
			OnCollisionStay,
			OnCollisionStay2D,
			OnDestroy,
			OnDisable,
			OnEnable,
			OnGUI,
			OnMouseDown,
			OnMouseDrag,
			OnMouseEnter,
			OnMouseExit,
			OnMouseOver,
			OnMouseUp,
			OnMouseUpAsButton,
			OnPostRender,
			OnPreCull,
			OnPreRender,
			OnRenderObject,
			OnTransformChildrenChanged,
			OnTransformParentChanged,
			OnTriggerEnter,
			OnTriggerEnter2D,
			OnTriggerExit,
			OnTriggerExit2D,
			OnTriggerStay,
			OnTriggerStay2D,
			OnWillRenderObject,
		}
		#endregion

		#region Functions
		protected void CreateOverrideMembers(IGraph graph) {
			foreach(var member in graphOverrideMembers) {
				if(member is MethodInfo method) {
					var func = CreateObject<Function>(method.Name, graph.GraphData.functionContainer, (val) => {
						val.name = method.Name;
						val.returnType = method.ReturnType;
						val.parameters = method.GetParameters().Select(p => new ParameterData(p.Name, p.ParameterType) { value = p.DefaultValue }).ToList();
						val.genericParameters = method.GetGenericArguments().Select(p => new GenericParameterData(p.Name)).ToArray();
					});
				} else if(member is PropertyInfo property) {
					var prop = CreateObject<Property>(property.Name, graph.GraphData.propertyContainer, (val) => {
						val.name = property.Name;
						val.type = property.PropertyType;
					});
					var getMethod = property.GetGetMethod(true);
					var setMethod = property.GetSetMethod(true);
					if(getMethod != null) {
						var func = CreateObject<Function>("Getter", prop, (val) => {
							val.returnType = getMethod.ReturnType;
							val.parameters = getMethod.GetParameters().Select(p => new ParameterData(p.Name, p.ParameterType) { value = p.DefaultValue }).ToList();
							val.genericParameters = getMethod.GetGenericArguments().Select(p => new GenericParameterData(p.Name)).ToArray();
						});
						func.Entry.EnsureRegistered();
						CreateNode<Nodes.NodeReturn>("Return", func, returnNode => {
							returnNode.EnsureRegistered();
							func.Entry.exit.ConnectTo(returnNode.enter);
							returnNode.value.AssignToDefault(MemberData.Default(property.PropertyType));
						});
						prop.getRoot = func;
					}
					if(setMethod != null) {
						var func = CreateObject<Function>("Setter", prop, (val) => {
							val.returnType = setMethod.ReturnType;
							val.parameters = setMethod.GetParameters().Select(p => new ParameterData(p.Name, p.ParameterType) { value = p.DefaultValue }).ToList();
							val.genericParameters = setMethod.GetGenericArguments().Select(p => new GenericParameterData(p.Name)).ToArray();
						});
						prop.setRoot = func;
					}
				}
			}
		}

		protected void CreateUnityEvents(IGraph graph) {
			foreach(var evt in graphUnityEvents) {
				var func = CreateObject<Function>(evt.ToString(), graph.GraphData.functionContainer, null);
				func.modifier.SetPrivate();
				switch(evt) {
					case UnityEventType.OnAnimatorIK:
						func.parameters = new List<ParameterData>() {
							new ParameterData("parameter", typeof(int))
						};
						break;
					case UnityEventType.OnApplicationFocus:
					case UnityEventType.OnApplicationPause:
						func.parameters = new List<ParameterData>() {
							new ParameterData("parameter", typeof(bool))
						};
						break;
					case UnityEventType.OnCollisionEnter:
					case UnityEventType.OnCollisionExit:
					case UnityEventType.OnCollisionStay:
						func.parameters = new List<ParameterData>() {
							new ParameterData("collision", typeof(Collision))
						};
						break;
					case UnityEventType.OnCollisionEnter2D:
					case UnityEventType.OnCollisionExit2D:
					case UnityEventType.OnCollisionStay2D:
						func.parameters = new List<ParameterData>() {
							new ParameterData("collision", typeof(Collision2D))
						};
						break;
					case UnityEventType.OnTriggerEnter:
					case UnityEventType.OnTriggerExit:
					case UnityEventType.OnTriggerStay:
						func.parameters = new List<ParameterData>() {
							new ParameterData("collider", typeof(Collider))
						};
						break;
					case UnityEventType.OnTriggerEnter2D:
					case UnityEventType.OnTriggerExit2D:
					case UnityEventType.OnTriggerStay2D:
						func.parameters = new List<ParameterData>() {
							new ParameterData("collider", typeof(Collider2D))
						};
						break;
				}
			}
		}

		protected T CreateObject<T>(string name, UGraphElement parent, Action<T> action) where T : UGraphElement, new() {
			var value = new T();
			value.name = name;
			parent.AddChild(value);
			action?.Invoke(value);
			return value;
		}

		protected T CreateNode<T>(string name, UGraphElement parent, Action<T> action) where T : Node, new() {
			var node = new T();
			var element = CreateObject<NodeObject>(name, parent, (nodeObj) => {
				nodeObj.node = node;
				action?.Invoke(node);
			});
			return node;
		}
		#endregion

		#region GUI Functions
		protected void DrawNamespaces(string label = "Namespace") {
			graphNamespaces = EditorGUILayout.TextField(label, graphNamespaces);
		}

		protected void DrawUsingNamespaces(string label = "Using Namespaces") {
			uNodeGUI.DrawNamespace(label, graphUsingNamespaces, null, (val) => {
				graphUsingNamespaces = val.ToList();
			});
		}

		protected void DrawGraphLayout(string label = "Graph Layout") {
			graphLayout = (GraphLayout)EditorGUILayout.EnumPopup(label, graphLayout);
		}

		protected void DrawGraphIcon(string label = "Icon") {
			graphIcon = EditorGUI.ObjectField(uNodeGUIUtility.GetRect(), label, graphIcon, typeof(Texture2D), false) as Texture2D;
		}

		protected void DrawUnityEvent(string label = "Unity Events") {
			uNodeGUI.DrawCustomList(
				graphUnityEvents,
				label,
				drawElement: (position, index, element) => {
					EditorGUI.LabelField(position, element.ToString());
				},
				add: (pos) => {
					GenericMenu menu = new GenericMenu();
					var values = Enum.GetValues(typeof(UnityEventType)) as UnityEventType[];
					for (int i = 0; i < values.Length; i++) {
						var value = values[i];
						if (graphUnityEvents.Contains(value)) continue;
						menu.AddItem(new GUIContent(value.ToString()), false, () => {
							graphUnityEvents.Add(value);
						});
					}
					menu.ShowAsContext();
				},
				remove: (index) => {
					graphUnityEvents.RemoveAt(index);
				}
			);
		}

		protected void DrawOverrideMembers(string label = "Override Members") {
			Type type = graphInheritFrom.type;
			if(type == null)
				return;
			uNodeGUI.DrawCustomList(
				graphOverrideMembers,
				label,
				drawElement: (position, index, element) => {
					EditorGUI.LabelField(position, NodeBrowser.GetPrettyMemberName(element));
				},
				add: (pos) => {
					var members = EditorReflectionUtility.GetOverrideMembers(type);
					GenericMenu menu = new GenericMenu();
					for(int i = 0; i < members.Count; i++) {
						var member = members[i];
						if(member is PropertyInfo) {
							menu.AddItem(new GUIContent("Properties/" + NodeBrowser.GetPrettyMemberName(member)), graphOverrideMembers.Contains(member), () => {
								graphOverrideMembers.Add(member);
							});
						} else {
							menu.AddItem(new GUIContent("Methods/" + NodeBrowser.GetPrettyMemberName(member)), graphOverrideMembers.Contains(member), () => {
								graphOverrideMembers.Add(member);
							});
						}
					}
					menu.ShowAsContext();
				},
				remove: (index) => {
					graphOverrideMembers.RemoveAt(index);
				}
			);
		}

		protected void DrawInheritFrom(string label = "Inherit From") {
			uNodeGUIUtility.DrawTypeDrawer(uNodeGUIUtility.GetRect(), graphInheritFrom, new GUIContent(label), (type) => {
				graphInheritFrom = type;
			}, graphInheritFilter);
		}

		protected void DrawInterfaces(string label = "Interfaces") {
			uNodeGUI.DrawCustomList(
				graphInterfaces,
				label,
				drawElement: (position, index, element) => {
					EditorGUI.LabelField(position, new GUIContent(element.prettyName, uNodeEditorUtility.GetTypeIcon(element)));
				},
				add: (pos) => {
					bool isNativeType = IsNativeGraph;
					var filter = new FilterAttribute() {
						OnlyGetType = true,
						ArrayManipulator = false,
						//UnityReference = false,
						ValidateType = (type) => {
							if(isNativeType != ReflectionUtils.IsNativeType(type)) {
								return false;
							}
							return type.IsInterface;
						}
					};
					ItemSelector.ShowType(null, filter, (member) => {
						graphInterfaces.Add(member.startType);
					}).ChangePosition(pos.ToScreenRect());
				},
				remove: (index) => {
					graphInterfaces.RemoveAt(index);
				}
			);
		}
		#endregion
	}

	public class MacroGraphCreator : GraphCreator {
		public override string menuName => "Macro";

		protected virtual MacroGraph CreateMacroGraph() {
			var macroGraph = ScriptableObject.CreateInstance<MacroGraph>();
			macroGraph.usingNamespaces = graphUsingNamespaces;
			return macroGraph;
		}

		public override bool IsNativeGraph => false;

		public override Object CreateAsset() {
			return CreateMacroGraph();
		}

		public override void OnGUI() {
			DrawUsingNamespaces();
		}
	}

	public abstract class ClassGraphCreator : GraphCreator {
		protected virtual ScriptGraph CreateScriptGraph() {
			var scriptGraph = ScriptableObject.CreateInstance<ScriptGraph>();
			scriptGraph.Namespace = graphNamespaces;
			scriptGraph.UsingNamespaces = graphUsingNamespaces;
			var type = CreateScriptGraphType();
			if(type != null) {
				scriptGraph.TypeList.AddType(type, scriptGraph);
			}
			return scriptGraph;
		}

		protected abstract IScriptGraphType CreateScriptGraphType();

		public override bool IsNativeGraph => true;

		public override Object CreateAsset() {
			return CreateScriptGraph();
		}

		public override void OnGUI() {
			DrawNamespaces();
			DrawUsingNamespaces();
		}
	}

	class ClassScriptGraphCreator : ClassGraphCreator {
		public override string menuName => "C# Script/Class";

		public override void OnGUI() {
			DrawInheritFrom();
			base.OnGUI();
			DrawGraphLayout();
			DrawOverrideMembers();
		}

		protected override IScriptGraphType CreateScriptGraphType() {
			var graph = ScriptableObject.CreateInstance<ClassScript>();
			graph.inheritType = new SerializedType(graphInheritFrom);
			graph.GraphData.graphLayout = graphLayout;
			CreateOverrideMembers(graph);
			return graph;
		}
	}

	class StructGraphCreator : ClassGraphCreator {
		public override string menuName => "C# Script/Struct";

		public override void OnGUI() {
			base.OnGUI();
			DrawGraphLayout();
		}

		protected override IScriptGraphType CreateScriptGraphType() {
			var graph = ScriptableObject.CreateInstance<ClassScript>();
			graph.inheritType = typeof(ValueType);
			graph.GraphData.graphLayout = graphLayout;
			return graph;
		}
	}

	class MonobehaviourScriptCreator : ClassGraphCreator {
		public override string menuName => "C# Script/MonoBehaviour";

		public MonobehaviourScriptCreator() {
			graphInheritFrom = typeof(MonoBehaviour);
			graphInheritFilter.Types.Add(typeof(MonoBehaviour));
		}

		protected override IScriptGraphType CreateScriptGraphType() {
			var graph = ScriptableObject.CreateInstance<ClassScript>();
			graph.inheritType = new SerializedType(graphInheritFrom);
			graph.GraphData.graphLayout = graphLayout;
			CreateUnityEvents(graph);
			CreateOverrideMembers(graph);
			return graph;
		}

		public override void OnGUI() {
			DrawInheritFrom();
			base.OnGUI();
			DrawGraphLayout();
			DrawUnityEvent();
			DrawOverrideMembers();
		}
	}

	class ScriptableObjectScriptCreator : ClassGraphCreator {
		public override string menuName => "C# Script/ScriptableObject";

		public ScriptableObjectScriptCreator() {
			graphInheritFrom = typeof(ScriptableObject);
			graphInheritFilter.Types.Add(typeof(ScriptableObject));
		}

		protected override IScriptGraphType CreateScriptGraphType() {
			var graph = ScriptableObject.CreateInstance<ClassScript>();
			graph.inheritType = new SerializedType(graphInheritFrom);
			graph.GraphData.graphLayout = graphLayout;
			CreateOverrideMembers(graph);
			return graph;
		}

		public override void OnGUI() {
			DrawInheritFrom();
			base.OnGUI();
			DrawGraphLayout();
			DrawOverrideMembers();
		}
	}

	class EnumScriptCreator : ClassGraphCreator {
		public override string menuName => "C# Script/Enum";

		string enumName = "";
		List<EnumScript.Enumerator> enumeratorList = new List<EnumScript.Enumerator>();

		public EnumScriptCreator() {

		}

		protected override IScriptGraphType CreateScriptGraphType() {
			var graph = ScriptableObject.CreateInstance<EnumScript>();
			graph.name = enumName;
			graph.enumerators = enumeratorList;
			return graph;
		}

		public override void OnGUI() {
			base.OnGUI();
			enumName = EditorGUILayout.TextField("Name", enumName);
			uNodeGUI.DrawCustomList(
				enumeratorList,
				"Enumerator List",
				drawElement: (position, index, element) => {
					element.name = EditorGUI.TextField(position, element.name);
				},
				add: (pos) => {
					enumeratorList.Add(new EnumScript.Enumerator());
				},
				remove: (index) => {
					enumeratorList.RemoveAt(index);
				});
		}
	}

	class InterfaceScriptCreator : ClassGraphCreator {
		public override string menuName => "C# Script/Interface";

		public InterfaceScriptCreator() {
		}

		protected override IScriptGraphType CreateScriptGraphType() {
			var graph = ScriptableObject.CreateInstance<InterfaceScript>();
			return graph;
		}

		public override void OnGUI() {
			base.OnGUI();
			DrawGraphLayout();
		}
	}

	class ClassDefinitionCreator : GraphCreator {
		public override string menuName => "Runtime Graph/Class Definition";

		private ClassDefinitionModel model = new ClassComponentModel();

		protected virtual ClassDefinition CreateGraph() {
			return ScriptableObject.CreateInstance<ClassDefinition>();
		}

		public override Object CreateAsset() {
			var graph = CreateGraph();
			graph.icon = graphIcon;
			graph.@namespace = graphNamespaces;
			graph.UsingNamespaces.Clear();
			graph.UsingNamespaces.AddRange(graphUsingNamespaces);
			graph.GraphData.graphLayout = graphLayout;
			graph.model = model;
			if(model.InheritType.IsCastableTo(typeof(MonoBehaviour))) {
				CreateUnityEvents(graph);
			}
			return graph;
		}

		public override void OnGUI() {
			DrawGraphIcon();
			uNodeGUI.DrawClassDefinitionModel(model, m => model = m);
			DrawNamespaces();
			DrawUsingNamespaces();
			if(model.InheritType.IsCastableTo(typeof(MonoBehaviour))) {
				DrawUnityEvent();
			}
			DrawGraphLayout();
		}
	}

	class GraphSingletonCreator : GraphCreator {
		public override string menuName => "Runtime Graph/Singleton";

		private GraphSingleton CreateGraph() {
			return ScriptableObject.CreateInstance<GraphSingleton>();
		}

		public override Object CreateAsset() {
			var graph = CreateGraph();
			graph.icon = graphIcon;
			graph.@namespace = graphNamespaces;
			graph.UsingNamespaces.Clear();
			graph.UsingNamespaces.AddRange(graphUsingNamespaces);
			graph.GraphData.graphLayout = graphLayout;
			return graph;
		}

		public override void OnGUI() {
			DrawGraphIcon();
			DrawNamespaces();
			DrawUsingNamespaces();
			DrawGraphLayout();
		}
	}

	class GraphInterfaceCreator : GraphCreator {
		public override string menuName => "Runtime Graph/Graph Interface";

		private GraphInterface CreateGraph() {
			return ScriptableObject.CreateInstance<GraphInterface>();
		}

		public override Object CreateAsset() {
			var graph = CreateGraph();
			graph.icon = graphIcon;
			graph.@namespace = graphNamespaces;
			graph.UsingNamespaces.Clear();
			graph.UsingNamespaces.AddRange(graphUsingNamespaces);
			graph.GraphData.graphLayout = graphLayout;
			return graph;
		}

		public override void OnGUI() {
			DrawGraphIcon();
			DrawNamespaces();
			DrawUsingNamespaces();
			DrawGraphLayout();
			EditorGUILayout.HelpBox("Graph Interface only supported for Runtime Graphs", MessageType.Info);
		}
	}

	//TODO: fix me
	//class CustomEditorCreator : GraphCreator {
	//	[Filter(typeof(Object), ArrayManipulator =false, OnlyGetType =true)]
	//	public MemberData editorType = MemberData.CreateFromType(typeof(Object));
	//	public override string menuName => "C# Script/Editor/Custom Editor";

	//	public CustomEditorCreator() {
	//		graphInheritFrom = MemberData.CreateFromType(typeof(Editor));
	//		graphInheritFilter.Types.Add(typeof(Editor));
	//		graphUsingNamespaces = new List<string>() {
	//			"UnityEngine",
	//			"UnityEditor",
	//			"System.Collections.Generic",
	//		};
	//		graphOverrideMembers = new List<MemberInfo>() {
	//			typeof(Editor).GetMethod(nameof(Editor.OnInspectorGUI))
	//		};
	//	}

	//	protected virtual uNodeClass CreateClassAsset() {
	//		GameObject gameObject = new GameObject("new_graph");
	//		return gameObject.AddComponent<uNodeClass>();
	//	}

	//	public override Object CreateAsset() {
	//		var graph = CreateClassAsset();
	//		var data = graph.gameObject.AddComponent<uNodeData>();
	//		data.Namespace = graphNamespaces;
	//		data.generatorSettings.usingNamespace = graphUsingNamespaces.ToArray();
	//		CreateOverrideMembers(graph);
	//		graph.Attributes = new AttributeData[] {
	//			new AttributeData() {
	//				type = MemberData.CreateFromType(typeof(CustomEditor)),
	//				value = new ValueData() {
	//					typeData = MemberData.CreateFromType(typeof(CustomEditor)),
	//					Value = new ConstructorValueData() {
	//						typeData = MemberData.CreateFromType(typeof(CustomEditor)),
	//						parameters = new[] {
	//							new ParameterValueData() {
	//								name = "inspectedType",
	//								typeData = MemberData.CreateFromType(typeof(Type)),
	//								value = editorType.Get<Type>()
	//							}
	//						}
	//					},
	//				},
	//			}
	//		};
	//		return graph;
	//	}

	//	public override void OnGUI() {
	//		uNodeGUIUtility.EditValueLayouted(nameof(editorType), this);
	//		DrawInheritFrom();
	//		DrawNamespaces();
	//		DrawUsingNamespaces();
	//		DrawOverrideMembers();
	//		DrawGraphLayout();
	//	}
	//}

	//class EditorWindowCreator : GraphCreator {
	//	string menuItem = "Tools/My Window";
	//	public override string menuName => "C# Script/Editor/Editor Window";

	//	public EditorWindowCreator() {
	//		graphInheritFrom = MemberData.CreateFromType(typeof(EditorWindow));
	//		graphInheritFilter.Types.Add(typeof(EditorWindow));
	//		graphUsingNamespaces = new List<string>() {
	//			"UnityEngine",
	//			"UnityEditor",
	//			"System.Collections.Generic",
	//		};
	//	}

	//	protected virtual uNodeClass CreateClassAsset() {
	//		GameObject gameObject = new GameObject("new_graph");
	//		return gameObject.AddComponent<uNodeClass>();
	//	}

	//	public override Object CreateAsset() {
	//		var graph = CreateClassAsset();
	//		var data = graph.gameObject.AddComponent<uNodeData>();
	//		data.Namespace = graphNamespaces;
	//		data.generatorSettings.usingNamespace = graphUsingNamespaces.ToArray();
	//		CreateComponent<uNodeFunction>("ShowWindow", GetRootTransfrom(graph), val => {
	//			val.Name = "ShowWindow";
	//			val.attributes = new AttributeData[] {
	//				new AttributeData() {
	//					type = MemberData.CreateFromType(typeof(MenuItem)),
	//					value = new ValueData() {
	//						typeData = MemberData.CreateFromType(typeof(MenuItem)),
	//						value = new ConstructorValueData(new MenuItem(menuItem)),
	//					},
	//				}
	//			};
	//		});
	//		return graph;
	//	}

	//	public override void OnGUI() {
	//		menuItem = EditorGUILayout.TextField(new GUIContent("Menu"), menuItem);
	//		DrawInheritFrom();
	//		DrawNamespaces();
	//		DrawUsingNamespaces();
	//	}
	//}
}