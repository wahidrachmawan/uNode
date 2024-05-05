using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;
using GPort = UnityEditor.Experimental.GraphView.Port;
using Object = UnityEngine.Object;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors {
	public struct GraphDraggedData {
		public UGraphView graphView;
		public GraphEditorData graphData;
		public Vector2 mousePositionOnCanvas;
		public Vector2 mousePositionOnScreen;
	}

	public partial class UGraphView : GraphView {
		#region Fields
		public UIElementGraph graph;
		public MinimapView miniMap;

		public List<UNodeView> nodeViews = new List<UNodeView>();
		public Dictionary<NodeObject, UNodeView> nodeViewsPerNode = new Dictionary<NodeObject, UNodeView>();
		public List<EdgeView> edgeViews = new List<EdgeView>();

		public Dictionary<NodeObject, UNodeView> cachedNodeMap = new Dictionary<NodeObject, UNodeView>();

		/// <summary>
		/// The editor data of the graph
		/// </summary>
		public GraphEditorData graphData => graph.graphData;
		/// <summary>
		/// The graph layout
		/// </summary>
		public GraphLayout graphLayout { get; private set; }
		/// <summary>
		/// The editor window
		/// </summary>
		public uNodeEditor window => graph.window;
		/// <summary>
		/// The current zoom scale
		/// </summary>
		public float zoomScale => graph.zoomScale;
		/// <summary>
		/// True if the graph is currently reloading.
		/// </summary>
		public bool isLoading { get; private set; }

		public bool isNativeGraph => graphData.graph.IsNativeGraph();

		private GraphDragger graphDragger = new GraphDragger();
		private GridBackground gridBackground;
		private bool autoHideNodes = true, hasInitialize;
		#endregion

		#region Properties
		private static List<UIGraphProcessor> _Processor;
		/// <summary>
		/// The list of available Graph Processor
		/// </summary>
		public static List<UIGraphProcessor> GraphProcessor {
			get {
				if(_Processor == null) {
					_Processor = EditorReflectionUtility.GetListOfType<UIGraphProcessor>();
					_Processor.Sort((x, y) => {
						return CompareUtility.Compare(x.order, y.order);
					});
				}
				return _Processor;
			}
		}
		#endregion

		#region Initialization
		public UGraphView() {
			//Add(new MiniMap());
			graphViewChanged = GraphViewChangedCallback;
			viewTransformChanged = ViewTransformChangedCallback;
			elementResized = ElementResizedCallback;
			//unserializeAndPaste += (op, data) => {
			//	if(op == "Paste") {
			//		graph.Repaint();
			//		var clickedPos = GetMousePosition(graph.topMousePos);
			//		graph.PasteNode(clickedPos);
			//		graph.Refresh();
			//	}
			//};

			InitializeManipulators();
			RegisterCallback<KeyDownEvent>(KeyDownCallback);
			RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
			RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
			this.RegisterRepaintAction(() => {
				if(autoHideNodes && _viewPosition != viewTransform.position && uNodeThreadUtility.frame % 2 == 0) {
					_viewPosition = viewTransform.position;
					AutoHideGraphElement.UpdateVisibility(this);
				}
			});

			SetupZoom(0.05f, 4f);
			this.StretchToParentSize();
		}

		protected virtual void InitializeManipulators() {
			if(uNodeUtility.isOSXPlatform) {
				this.AddManipulator(new ContentDragger());
			}
			else {
				this.AddManipulator(graphDragger);
			}
			// this.AddManipulator(new ClickSelector());
			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(new RectangleSelector());
			this.AddManipulator(new FreehandSelector());
		}
		#endregion

		#region Drag & Drop
		private void OnDragUpdatedEvent(DragUpdatedEvent evt) {
			if(evt.currentTarget == null) {
				return;
			}
			if(DragAndDrop.GetGenericData("uNode") != null ||
				DragAndDrop.visualMode == DragAndDropVisualMode.None &&
				DragAndDrop.objectReferences.Length > 0) {

				if(!uNodeEditorUtility.IsSceneObject(graphData.owner)) {
					if(DragAndDrop.GetGenericData("uNode") != null) {
						var generic = DragAndDrop.GetGenericData("uNode");
						if(generic is IDragableGraphHandler dragable) {
							var mousePosition = GetMousePosition(evt, out var screenPosition);
							if(!dragable.CanAcceptDrag(new GraphDraggedData() {
								graphData = graphData,
								graphView = this,
								mousePositionOnScreen = screenPosition,
								mousePositionOnCanvas = mousePosition,
							})) {
								return;
							}
						}
						if(generic is UnityEngine.Object && uNodeEditorUtility.IsSceneObject(generic as UnityEngine.Object)) {
							DragAndDrop.visualMode = DragAndDropVisualMode.None;
							return;
						}
					}
					else if(DragAndDrop.objectReferences.Length > 0) {
						if(uNodeEditorUtility.IsSceneObject(DragAndDrop.objectReferences[0])) {
							DragAndDrop.visualMode = DragAndDropVisualMode.None;
							return;
						}
					}
				}
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
		}

		#region Drag Handler
		private void DragHandleVariable(Variable variable, Vector2 position) {
			if(variable.graphContainer != graphData.graph) {
				EditorUtility.DisplayDialog("Error", "The graph of the variable must same with the current graph", "Ok");
				return;
			}
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Get"), false, (() => {
				NodeEditorUtility.AddNewNode(graphData, variable.name, null, position, delegate (MultipurposeNode n) {
					var mData = MemberData.CreateFromValue(variable);
					n.target = mData;
					n.Register();
				});
				graph.Refresh();
			}));
			menu.AddItem(new GUIContent("Set"), false, (() => {
				NodeEditorUtility.AddNewNode(graphData, variable.name, null, position, delegate (Nodes.NodeSetValue n) {
					var mData = MemberData.CreateFromValue(variable);
					n.Register();
					n.target.AssignToDefault(mData);
					if(mData.type != null) {
						n.value.AssignToDefault(MemberData.Default(mData.type));
					}
				});
				graph.Refresh();
			}));
			menu.ShowAsContext();
		}

		private void DragHandleObject(Object obj, Vector2 position, Vector2 menuPosition) {
			if(obj is GraphAsset graphAsset) {
				//Create Liked macro from dragable macros.
				if(graphAsset is MacroGraph) {
					var macro = graphAsset as MacroGraph;
					CreateLinkedMacro(macro, position);
					return;
				}
				if(graphAsset is IReflectionType) {
					DragHandleType(ReflectionUtils.GetRuntimeType(graphAsset), position, menuPosition);
					return;
				}
			}
			if(!(graphData.graph is IIndependentGraph)) {
				EditorUtility.DisplayDialog("Error", "The c# graph cannot reference project and scene object.", "Ok");
				return;
			}
			else if(!EditorUtility.IsPersistent(obj) && !uNodeEditorUtility.IsSceneObject(graphData.owner)) {
				EditorUtility.DisplayDialog("Error", "The project graph cannot reference scene object.", "Ok");
				return;
			}
			GenericMenu menu = new GenericMenu();

			#region Dragged Action
			Action<UnityEngine.Object, string> action = (dOBJ, startName) => {
				menu.AddItem(new GUIContent(startName + "Get"), false, () => {
					FilterAttribute filter = new FilterAttribute();
					filter.MaxMethodParam = int.MaxValue;
					filter.VoidType = true;
					filter.Public = true;
					filter.Instance = true;
					filter.Static = false;
					filter.DisplayDefaultStaticType = false;
					var type = dOBJ.GetType();
					if(dOBJ is IRuntimeClass || dOBJ is IReflectionType || dOBJ is IInstancedGraph) {
						type = ReflectionUtils.GetRuntimeType(dOBJ);
					}
					string category = type.PrettyName();
					var customItems = ItemSelector.MakeCustomItems(type, filter, category, ItemSelector.CategoryInherited);
					if(customItems != null) {
						//TODO: fix me
						//if(!(dOBJ is uNodeInterface)) {
						//	customItems.Insert(0, ItemSelector.CustomItem.Create("this", () => {
						//		var value = new MemberData(dOBJ, MemberData.TargetType.Values);
						//		value.startType = type;
						//		NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, null, null, position, delegate (MultipurposeNode n) {
						//			if(n.target == null) {
						//				n.target = new MultipurposeMember();
						//			}
						//			n.target.target = value;
						//			MemberDataUtility.UpdateMultipurposeMember(n.target);
						//		});
						//		graph.Refresh();
						//	}, category));
						//}
						ItemSelector w = ItemSelector.ShowWindow(dOBJ, filter, delegate (MemberData value) {
							//if(dOBJ is uNodeInterface) {
							//	dOBJ = null;//Will make the instance null for graph interface
							//}
							var mData = new MemberData(dOBJ, MemberData.TargetType.Values);
							mData.startType = type;
							value.startType = type;
							value.instance = mData;
							NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, position, delegate (MultipurposeNode n) {
								n.target = value;
							});
							graph.Refresh();
						}, customItems).ChangePosition(menuPosition);
						w.displayDefaultItem = false;
					}
				});
				menu.AddItem(new GUIContent(startName + "Set"), false, () => {
					FilterAttribute filter = new FilterAttribute();
					filter.SetMember = true;
					filter.MaxMethodParam = int.MaxValue;
					//filter.VoidType = true;
					filter.Public = true;
					filter.Instance = true;
					filter.Static = false;
					filter.DisplayDefaultStaticType = false;
					var type = dOBJ.GetType();
					if(dOBJ is IRuntimeClass || dOBJ is IReflectionType || dOBJ is IInstancedGraph) {
						type = ReflectionUtils.GetRuntimeType(dOBJ);
					}
					var customItems = ItemSelector.MakeCustomItems(type, filter, type.PrettyName(), ItemSelector.CategoryInherited);
					if(customItems != null) {
						ItemSelector w = ItemSelector.ShowWindow(dOBJ, filter, delegate (MemberData value) {
							//if(dOBJ is uNodeInterface) {
							//	dOBJ = null;//Will make the instance null for graph interface
							//}
							value.instance = dOBJ;
							value.startType = type;
							NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(graphData, position, (n) => {
								n.target.AssignToDefault(value);
							});
							graph.Refresh();
						}, customItems).ChangePosition(menuPosition);
						w.displayDefaultItem = false;
					}
				});
			};
			#endregion

			action(obj, "");
			if(obj is GameObject) {
				menu.AddSeparator("");
				foreach(var comp in (obj as GameObject).GetComponents<Component>()) {
					action(comp, comp.GetType().Name + "/");
				}
			}
			else if(obj is Component) {
				menu.AddSeparator("");
				foreach(var comp in (obj as Component).GetComponents<Component>()) {
					action(comp, comp.GetType().Name + "/");
				}
			}
			menu.ShowAsContext();
		}

		private void DragHandleProperty(Property property, Vector2 position) {
			if(property.graphContainer != graphData.graph) {
				EditorUtility.DisplayDialog("Error", "The graph of the property must same with the current graph", "Ok");
				return;
			}
			GenericMenu menu = new GenericMenu();
			if(property.CanGetValue()) {
				menu.AddItem(new GUIContent("Get"), false, () => {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, property.name, null, position, delegate (MultipurposeNode n) {
						var mData = MemberData.CreateFromValue(property);
						n.target = mData;
						n.EnsureRegistered();
					});
					graph.Refresh();
				});
			}
			if(property.CanSetValue()) {
				menu.AddItem(new GUIContent("Set"), false, () => {
					NodeEditorUtility.AddNewNode(graphData, property.name, null, position, delegate (Nodes.NodeSetValue n) {
						n.EnsureRegistered();
						var mData = MemberData.CreateFromValue(property);
						n.target.AssignToDefault(mData);
						if(mData.type != null) {
							n.value.AssignToDefault(MemberData.Default(mData.type));
						}
					});
					graph.Refresh();
				});
			}
			menu.ShowAsContext();
		}

		private void DragHandleFunction(Function function, Vector2 position) {
			if(function.graphContainer != graphData.graph) {
				EditorUtility.DisplayDialog("Error", "The graph of the function must same with the current graph", "Ok");
				return;
			}
			if(function.ReturnType().IsCastableTo(typeof(System.Collections.IEnumerator)) && graphData.graph.GetGraphInheritType().IsCastableTo(typeof(MonoBehaviour))) {
				GenericMenu menu = new GenericMenu();
				menu.AddItem(new GUIContent("Invoke"), false, (() => {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, function.name, null, position, (n) => {
						n.target = MemberData.CreateFromValue(function);
					});
					graph.Refresh();
				}));
				menu.AddItem(new GUIContent("Start Coroutine"), false, (() => {
					NodeEditorUtility.AddNewNode(graphData, position, delegate (NodeBaseCaller node) {
						node.target = MemberData.CreateFromMember(typeof(MonoBehaviour).GetMethod(nameof(MonoBehaviour.StartCoroutine), new[] { typeof(System.Collections.IEnumerator) }));
						node.Register();

						NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, function.name, null, new Vector2(position.x - 200, position.y), (n) => {
							n.target = MemberData.CreateFromValue(function);

							node.parameters[0].input.ConnectTo(n.output);
						});
					});
					graph.Refresh();
				}));
				menu.ShowAsContext();
			}
			else {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, function.name, null, position, (n) => {
					n.target = MemberData.CreateFromValue(function);
				});
				graph.Refresh();
			}
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(FieldInfo member, Vector2 position) {
			if(member.IsPrivate) {
				if(!EditorUtility.DisplayDialog("Variable is Private", "The variable you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
			}
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Get"), false, (() => {
				NodeEditorUtility.AddNewNode(graphData, member.Name, null, position, delegate (MultipurposeNode n) {
					var mData = new MemberData(member);
					n.target = new MemberData(member);
				});
				graph.Refresh();
			}));
			menu.AddItem(new GUIContent("Set"), false, (() => {
				NodeEditorUtility.AddNewNode(graphData, member.Name, null, position, delegate (MultipurposeNode n) {
					var mData = new MemberData(member);
					n.target = new MemberData(member);
					n.Register();
					NodeEditorUtility.AddNewNode(graphData, "Set", null, position, delegate (Nodes.NodeSetValue setNode) {
						setNode.Register();
						NodeEditorUtility.ConnectPort(setNode.target, n.output);
						if(mData.type != null) {
							setNode.value.AssignToDefault(MemberData.Default(member.FieldType));
						}
					});
				});
				graph.Refresh();
			}));
			menu.ShowAsContext();
		}

		private void DragHandleMember(PropertyInfo member, Vector2 position) {
			bool nonPublic = false;
			if(member.GetGetMethod(false) == null && member.GetSetMethod(false) == null) {
				if(!EditorUtility.DisplayDialog("Property is Private", "The property you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
				nonPublic = true;
			}
			GenericMenu menu = new GenericMenu();
			if(member.GetGetMethod(nonPublic) != null) {
				menu.AddItem(new GUIContent("Get"), false, (() => {
					NodeEditorUtility.AddNewNode(graphData, member.Name, null, position, delegate (MultipurposeNode n) {
						var mData = new MemberData(member);
						n.target = new MemberData(member);
						n.Register();
					});
					graph.Refresh();
				}));
			}
			if(member.GetSetMethod(nonPublic) != null) {
				menu.AddItem(new GUIContent("Set"), false, (() => {
					NodeEditorUtility.AddNewNode(graphData, member.Name, null, position, delegate (MultipurposeNode n) {
						var mData = new MemberData(member);
						n.target = new MemberData(member);
						n.Register();
						NodeEditorUtility.AddNewNode(graphData, "Set", null, position, delegate (Nodes.NodeSetValue setNode) {
							setNode.Register();
							NodeEditorUtility.ConnectPort(setNode.target, n.output);
							if(mData.type != null) {
								setNode.value.AssignToDefault(MemberData.Default(member.PropertyType));
							}
						});
					});
					graph.Refresh();
				}));
			}
			menu.ShowAsContext();
		}

		private void DragHandleMember(MethodInfo member, Vector2 position, Vector2 screenPosition = default(Vector2)) {
			if(member.IsPrivate) {
				if(!EditorUtility.DisplayDialog("Function is Private", "The function you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
			}
			if(member.ContainsGenericParameters) {
				var args = member.GetGenericArguments();
				TypeItem[] typeItems = new TypeItem[args.Length];
				for(int i = 0; i < args.Length; i++) {
					var fil = new FilterAttribute(args[i].BaseType);
					fil.ToFilterGenericConstraints(args[i]);
					typeItems[i] = new TypeItem(args[i].BaseType, fil);
				}
				if(args.Length == 1) {
					ItemSelector w = null;
					Action<MemberData> action = delegate (MemberData m) {
						if(w != null) {
							w.Close();
						}
						TypeBuilderWindow.Show(Rect.zero, graphData.currentCanvas, typeItems[0].filter, delegate (MemberData[] members) {
							member = ReflectionUtils.MakeGenericMethod(member, members.Select(item => item.startType).ToArray());
							DragHandleMember(member, position, screenPosition);
						}, new TypeItem(m, typeItems[0].filter));
					};
					w = ItemSelector.ShowAsNew(null, typeItems[0].filter, action).ChangePosition(screenPosition.ToScreenPoint());
				}
				else {
					TypeBuilderWindow.Show(screenPosition, graphData.currentCanvas, new FilterAttribute() { OnlyGetType = true }, (members) => {
						member = ReflectionUtils.MakeGenericMethod(member, members.Select(item => item.startType).ToArray());
						DragHandleMember(member, position, screenPosition);
					}, typeItems);
				}
			}
			else {
				NodeEditorUtility.AddNewNode(graphData, member.Name, null, position, delegate (MultipurposeNode n) {
					n.target = new MemberData(member);
					n.Register();
					graph.Refresh();
				});
			}
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(ConstructorInfo ctor, Vector2 position) {
			if(ctor.IsPrivate) {
				if(!EditorUtility.DisplayDialog("Constructor is Private", "The constructor you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
			}
			NodeEditorUtility.AddNewNode(graphData, ctor.Name, null, position, delegate (MultipurposeNode n) {
				n.target = new MemberData(ctor);
				n.Register();
				graph.Refresh();
			});
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleType(Type type, Vector2 position, Vector2 menuPosition) {
			FilterAttribute filter = new FilterAttribute();
			filter.MaxMethodParam = int.MaxValue;
			filter.VoidType = true;
			filter.Public = true;
			filter.Instance = true;
			filter.Static = false;
			filter.DisplayDefaultStaticType = false;
			string category = type.PrettyName();
			var customItems = ItemSelector.MakeCustomItems(type, filter, category, ItemSelector.CategoryInherited);
			if(customItems != null) {
				ItemSelector w = ItemSelector.ShowWindow(null, filter, delegate (MemberData value) {
					NodeEditorUtility.AddNewNode(graphData, null, null, position, delegate (MultipurposeNode n) {
						n.target = value;
						n.Register();
					});
					graph.Refresh();
				}, customItems).ChangePosition(menuPosition);
				w.displayDefaultItem = false;
			}
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(Type member, Vector2 position) {
			if(member.IsNotPublic) {
				if(!EditorUtility.DisplayDialog("Type is Private", "The type you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
			}
			NodeEditorUtility.AddNewNode(graphData, member.Name, null, position, delegate (MultipurposeNode n) {
				n.target = new MemberData(member);
				n.Register();
				graph.Refresh();
			});
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(NodeMenu menu, Vector2 position) {
			NodeEditorUtility.AddNewNode(graphData, menu.nodeName, menu.type, position, (Node node) => {
				graph.Refresh();
			});
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(INodeItemCommand command, Vector2 position) {
			command.graph = graph;
			command.Setup(position);
			DragAndDrop.SetGenericData("uNode", null);
		}
		#endregion

		private void OnDragPerformEvent(DragPerformEvent evt) {
			if(graphData.canAddNode == false) {
				return;
			}
			Vector2 topMPos;
			Vector2 mPos = GetMousePosition(evt, out topMPos);
			if(DragAndDrop.GetGenericData("uNode") != null) {
				var generic = DragAndDrop.GetGenericData("uNode");

				if(generic is IDragableGraphHandler dragable) {
					var mousePosition = GetMousePosition(evt, out var screenPosition);
					var data = new GraphDraggedData() {
						graphData = graphData,
						graphView = this,
						mousePositionOnScreen = screenPosition,
						mousePositionOnCanvas = mousePosition,
					};
					if(dragable.CanAcceptDrag(data)) {
						dragable.AcceptDrag(data);
						return;
					}
				}

				#region Function
				if(generic is Function) {//Drag functions.
					var function = generic as Function;
					DragHandleFunction(function, mPos);
				}
				else
				#endregion

				#region Property
				if(generic is Property) {//Drag property
					var property = generic as Property;
					DragHandleProperty(property, mPos);
				}
				else
				#endregion

				#region Variable
				if(generic is Variable) {//Drag variable.
					var varData = generic as Variable;
					DragHandleVariable(varData, mPos);
				}
				else
				#endregion

				#region Visual Element
				if(generic is VisualElement) {
					//#region Variable
					//if(generic is TreeViews.VariableView) {
					//	var view = generic as TreeViews.VariableView;
					//	var variable = view.variable;
					//	var root = view.owner as uNodeRoot;
					//	if(root != editorData.graph) {
					//		if(uNodeEditorUtility.IsPrefab(root)) {
					//			root = GraphUtility.GetTempGraphObject(root);
					//			if(root == editorData.graph) {
					//				variable = root.GetVariableData(variable.Name);
					//			} else {
					//				if(view.owner is IClassIdentifier) {
					//					var runtimeType = ReflectionUtils.GetRuntimeType(view.owner as uNodeRoot);
					//					var field = runtimeType.GetField(variable.Name);
					//					if(field != null) {
					//						DragHandleMember(field, mPos);
					//					} else {
					//						uNodeEditorUtility.DisplayErrorMessage();
					//					}
					//					return;
					//				}
					//				var type = uNodeEditorUtility.GetFullScriptName(view.owner as uNodeRoot).ToType(false);
					//				if(type != null) {
					//					var field = type.GetField(variable.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
					//					if(field != null) {
					//						if(field.IsPublic) {
					//							DragHandleMember(field, mPos);
					//						} else {
					//							EditorUtility.DisplayDialog("Variable is Private", "Can't access the variable because the variable is not public.", "OK");
					//						}
					//						return;
					//					}
					//				}
					//				EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
					//				return;
					//			}
					//		} else
					//			return;
					//	}
					//	if(variable != null && root != null) {
					//		DragHandleVariable(root, variable, mPos);
					//	}
					//} else
					//#endregion

					//#region Property
					//if(generic is TreeViews.PropertyView) {
					//	var view = generic as TreeViews.PropertyView;
					//	var property = view.property;
					//	var root = property.owner as uNodeRoot;
					//	if(root != editorData.graph) {
					//		if(uNodeEditorUtility.IsPrefab(root)) {
					//			root = GraphUtility.GetTempGraphObject(root);
					//			if(root == editorData.graph) {
					//				property = root.GetPropertyData(property.Name);
					//			} else {
					//				if(property.owner is IClassIdentifier) {
					//					var runtimeType = ReflectionUtils.GetRuntimeType(property.owner as uNodeRoot);
					//					var member = runtimeType.GetProperty(property.Name);
					//					if(member != null) {
					//						DragHandleMember(member, mPos);
					//					} else {
					//						uNodeEditorUtility.DisplayErrorMessage();
					//					}
					//					return;
					//				}
					//				var type = uNodeEditorUtility.GetFullScriptName(property.owner).ToType(false);
					//				if(type != null) {
					//					var member = type.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
					//					if(member != null) {
					//						DragHandleMember(member, mPos);
					//						return;
					//					}
					//				}
					//				EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
					//				return;
					//			}
					//		} else
					//			return;
					//	}
					//	if(property != null && root != null) {
					//		DragHandleProperty(property, mPos);
					//	}
					//} else
					//#endregion

					//#region Function
					//if(generic is TreeViews.FunctionView) {
					//	var view = generic as TreeViews.FunctionView;
					//	var function = view.function;
					//	var root = function.owner as uNodeRoot;
					//	if(root != editorData.graph) {
					//		if(uNodeEditorUtility.IsPrefab(root)) {
					//			root = GraphUtility.GetTempGraphObject(root);
					//			if(root == editorData.graph) {
					//				function = root.GetFunction(function.Name, function.GenericParameters.Count, function.Parameters.Select(p => p.Type).ToArray());
					//			} else {
					//				if(function.owner is IClassIdentifier) {
					//					var runtimeType = ReflectionUtils.GetRuntimeType(function.owner as uNodeRoot);
					//					var member = runtimeType.GetMethod(function.Name, function.Parameters.Select(p => p.Type).ToArray());
					//					if(member != null) {
					//						DragHandleMember(member, mPos);
					//					} else {
					//						uNodeEditorUtility.DisplayErrorMessage();
					//					}
					//					return;
					//				}
					//				var type = uNodeEditorUtility.GetFullScriptName(function.owner).ToType(false);
					//				if(type != null) {
					//					var member = type.GetMethod(function.Name, function.Parameters.Select(p => p.Type).ToArray());
					//					if(member != null) {
					//						if(member.IsPublic) {
					//							DragHandleMember(member, mPos, topMPos);
					//						} else {
					//							EditorUtility.DisplayDialog("Function is Private", "Can't access the function because the function is not public.", "OK");
					//						}
					//						return;
					//					}
					//				}
					//				EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
					//				return;
					//			}
					//		} else
					//			return;
					//	}
					//	if(function != null && root != null) {
					//		DragHandleFunction(function, mPos);
					//	}
					//} else
					//#endregion

					//#region Graph & Macro
					//if(generic is TreeViews.GraphTreeView) {
					//	var view = generic as TreeViews.GraphTreeView;
					//	var root = view.root;
					//	if(root != editorData.graph) {
					//		if(uNodeEditorUtility.IsPrefab(root)) {
					//			if(root is uNodeMacro) {
					//				CreateLinkedMacro(root as uNodeMacro, mPos);
					//				return;
					//			}
					//			root = GraphUtility.GetTempGraphObject(root);
					//			if(root == editorData.graph) {
					//				NodeEditorUtility.AddNewNode(editorData, "this", null, mPos, delegate (MultipurposeNode n) {
					//					n.target.target = new MemberData(root, MemberData.TargetType.SelfTarget);
					//					MemberDataUtility.UpdateMultipurposeMember(n.target);
					//				});
					//				graph.Refresh();
					//			} else {
					//				if(root is IClassIdentifier) {
					//					EditorUtility.DisplayDialog("Error", "Unsupported graph type.", "OK");
					//					return;
					//				}
					//				var type = uNodeEditorUtility.GetFullScriptName(root).ToType(false);
					//				if(type != null) {
					//					DragHandleMember(type, mPos);
					//				} else {
					//					EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
					//				}
					//				return;
					//			}
					//		} else
					//			return;
					//	}
					//}
					//#endregion
				}
				else
				#endregion

				#region MemberInfo
				if(generic is MemberInfo) {
					if(generic is Type) {
						DragHandleMember(generic as Type, mPos);
					}
					else if(generic is FieldInfo) {
						DragHandleMember(generic as FieldInfo, mPos);
					}
					else if(generic is PropertyInfo) {
						DragHandleMember(generic as PropertyInfo, mPos);
					}
					else if(generic is MethodInfo) {
						DragHandleMember(generic as MethodInfo, mPos, topMPos);
					}
					else if(generic is ConstructorInfo) {
						DragHandleMember(generic as ConstructorInfo, mPos);
					}
				}
				#endregion

				#region Menu
				if(generic is NodeMenu) {
					DragHandleMember(generic as NodeMenu, mPos);
				}
				else if(generic is INodeItemCommand) {
					DragHandleMember(generic as INodeItemCommand, mPos);
				}
				#endregion
			}
			else if(DragAndDrop.objectReferences.Length == 1) {//Dragging UnityObject
				var dragObject = DragAndDrop.objectReferences[0];
				var iPOS = graph.window.GetMousePositionForMenu(topMPos);
				DragHandleObject(dragObject, mPos, iPOS);
			}
			else if(DragAndDrop.objectReferences.Length > 1) {
				var iPOS = graph.window.GetMousePositionForMenu(topMPos);
				GenericMenu menu = new GenericMenu();
				foreach(var o in DragAndDrop.objectReferences) {
					menu.AddItem(new GUIContent("Get/" + o.name), false, (dOBJ) => {
						FilterAttribute filter = new FilterAttribute();
						filter.MaxMethodParam = int.MaxValue;
						filter.VoidType = true;
						filter.Public = true;
						filter.Instance = true;
						filter.Static = false;
						filter.DisplayDefaultStaticType = false;
						string category = dOBJ.GetType().PrettyName();
						var customItems = ItemSelector.MakeCustomItems(dOBJ.GetType(), filter, category, ItemSelector.CategoryInherited);
						if(customItems != null) {
							customItems.Insert(0, ItemSelector.CustomItem.Create("this", () => {
								var value = new MemberData(dOBJ, MemberData.TargetType.Values);
								NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, null, null, mPos, delegate (MultipurposeNode n) {
									n.target = value;
								});
								graph.Refresh();
							}, category));
							ItemSelector w = ItemSelector.ShowWindow(/*editorData.selectedGroup ?? editorData.selectedRoot as UnityEngine.Object ?? editorData.graph*/ null, filter, delegate (MemberData value) {
								value.instance = new MemberData(dOBJ, MemberData.TargetType.Values);
								NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, null, null, mPos, delegate (MultipurposeNode n) {
									n.target = value;
								});
								graph.Refresh();
							}, customItems).ChangePosition(iPOS);
							w.displayDefaultItem = false;
						}
					}, o);
					menu.AddItem(new GUIContent("Set/" + o.name), false, (dOBJ) => {
						FilterAttribute filter = new FilterAttribute();
						filter.SetMember = true;
						filter.MaxMethodParam = int.MaxValue;
						//filter.VoidType = true;
						filter.Public = true;
						filter.Instance = true;
						filter.Static = false;
						filter.DisplayDefaultStaticType = false;
						var customItems = ItemSelector.MakeCustomItems(dOBJ.GetType(), filter, dOBJ.GetType().PrettyName(), ItemSelector.CategoryInherited);
						if(customItems != null) {
							ItemSelector w = ItemSelector.ShowWindow(dOBJ as UnityEngine.Object, filter, delegate (MemberData value) {
								value.instance = dOBJ;
								NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(graphData, null, null, mPos, delegate (Nodes.NodeSetValue n) {
									n.target.AssignToDefault(value);
								});
								graph.Refresh();
							}, customItems).ChangePosition(iPOS);
							w.displayDefaultItem = false;
						}
					}, o);
				}
				menu.ShowAsContext();
			}
		}
		#endregion

		#region Callbacks
		protected override bool canPaste => GraphUtility.CopyPaste.IsCopiedNodes;

		public override EventPropagation DeleteSelection() {
			uNodeEditorUtility.RegisterUndo(graphData.owner, "Delete selected elements");
			return DeleteSelection(selection);
		}

		private EventPropagation DeleteSelection(List<ISelectable> selection) {
			var processor = GraphProcessor;
			var list = new List<ISelectable>();
			list.AddRange(selection.Distinct());
			foreach(var p in processor) {
				if(p.Delete(list)) {
					selection.Clear();
					graph.Refresh();
					return EventPropagation.Stop;
				}
			}
			edges.ForEach(edge => {
				if(edge != null && edge is ConversionEdgeView conversionEdge && conversionEdge.isValid && conversionEdge.node != null) {
					if(list.Contains(edge.input.node)) {
						list.Add(edge);
					}
					else if(list.Contains(edge.output.node)) {
						list.Add(edge);
					}
				}
			});
			var nodeToRemove = new List<NodeObject>();
			Action postAction = null;
			foreach(var s in list) {
				if(s is UNodeView) {
					UNodeView view = s as UNodeView;
					if(view.nodeObject != null) {
						nodeToRemove.Add(view.nodeObject);
						postAction += () => {
							OnNodeRemoved(view);
						};
					}
				}
				else if(s is EdgeView) {
					EdgeView view = s as EdgeView;
					if(!view.isValid)
						continue;
					postAction += () => {
						var inPort = view.input as PortView;
						var outPort = view.output as PortView;
						view.Disconnect();
						MarkRepaint(inPort?.owner, outPort?.owner);
					};
				}
			}
			selection.Clear();
			if(postAction != null || nodeToRemove.Count > 0) {
				if(nodeToRemove.Count > 0) {
					foreach(var node in nodeToRemove) {
						node.Destroy();
					}
				}
				postAction?.Invoke();

				foreach(var p in processor) {
					if(p.PostDelete(list)) {
						break;
					}
				}

				if(graphData.graph != null) {
					uNodeGUIUtility.GUIChanged(graphData.graph);
				}
				//graph.Refresh();
				return EventPropagation.Stop;
			}
			return EventPropagation.Continue;
		}

		public void CutSelectedNodes() {
			var nodes = new HashSet<UGraphElement>();
			foreach(var item in selection) {
				if(item is UNodeView) {
					var view = item as UNodeView;
					nodes.Add(view.nodeObject);
					if(view != null) {
						foreach(var p in view.inputPorts) {
							var edges = p.GetValidEdges();
							foreach(var e in edges) {
								if(e is ConversionEdgeView) {
									var ce = e as ConversionEdgeView;
									nodes.Add(ce.node.nodeObject);
								}
							}
						}
						//foreach(var p in view.outputPorts) {
						//	var edges = p.GetValidEdges();
						//	foreach(var e in edges) {
						//		if(e is ConversionEdgeView) {
						//			var ce = e as ConversionEdgeView;
						//			nodes.Add(ce.node);
						//		}
						//	}
						//}
					}
				}
				else {

				}
				
			}
			if(nodes.Count > 0) {
				GraphUtility.CopyPaste.Copy(nodes.ToArray());
				DeleteSelection();
			}
		}

		public void CopySelectedNodes() {
			var nodes = new HashSet<UGraphElement>();
			foreach(var n in graphData.selecteds) {
				if(n is not NodeObject node)
					continue;
				nodes.Add(node);
				var view = GetNodeView(node);
				if(view != null) {
					foreach(var p in view.inputPorts) {
						var edges = p.GetValidEdges();
						foreach(var e in edges) {
							if(e is ConversionEdgeView) {
								var ce = e as ConversionEdgeView;
								nodes.Add(ce.node.nodeObject);
							}
						}
					}
					//foreach(var p in view.outputPorts) {
					//	var edges = p.GetValidEdges();
					//	foreach(var e in edges) {
					//		if(e is ConversionEdgeView) {
					//			var ce = e as ConversionEdgeView;
					//			nodes.Add(ce.node);
					//		}
					//	}
					//}
				}
			}
			GraphUtility.CopyPaste.Copy(nodes.ToArray());
		}

		public override void AddToSelection(ISelectable selectable) {
			base.AddToSelection(selectable);
			if(selectable is BaseNodeView) {
				graph.SelectNode((selectable as BaseNodeView).nodeObject, false);
				AutoHideGraphElement.RegisterNodeToIgnore(selectable as NodeView);
			}
			else if(selectable is EdgeView) {
				var edge = (selectable as EdgeView);
				if(graphData.selectedNodes.Any() == false) {
					if(edge.isFlow) {
						if(edge.Output != null) {
							graph.Select(new UPortRef(edge.Output.GetPortValue()));
							return;
						}
					}
					else {
						if(edge.Input != null) {
							graph.Select(new UPortRef(edge.Input.GetPortValue()));
						}
					}
					graph.Select(new UPortRef(edge.Input?.GetPortValue() ?? edge.Output?.GetPortValue()));
				}
			}
			else if(selectable is TransitionView) {
				graph.Select((selectable as TransitionView).transition);
				AutoHideGraphElement.RegisterNodeToIgnore(selectable as NodeView);
			}
			//else if(selectable is BlockView) {
			//	var block = (selectable as BlockView);
			//	if(block.data != null && block.data.block != null && editorData.selected != editorData.selectedNodes) {
			//		graph.Select(new uNodeEditor.ValueInspector(block.data.block, block.owner.nodeView.targetNode));
			//	}
			//} 
		}

		public override void RemoveFromSelection(ISelectable selectable) {
			base.RemoveFromSelection(selectable);
			if(selectable is BaseNodeView) {
				graph.UnselectNode((selectable as BaseNodeView).nodeObject);
				AutoHideGraphElement.UnregisterNodeToIgnore(selectable as NodeView);
			}
		}

		public override void ClearSelection() {
			base.ClearSelection();
			graph.ClearSelection();
		}

		GraphViewChange GraphViewChangedCallback(GraphViewChange changes) {
			if(changes.elementsToRemove != null) {

				//Handle ourselves the edge and node remove
				changes.elementsToRemove.RemoveAll(e => {
					var edge = e as EdgeView;
					var node = e as BaseNodeView;

					if(edge != null) {
						Disconnect(edge);
						return true;
					}
					else if(node != null) {
						RemoveElement(node);
						return true;
					}
					return false;
				});
			}

			return changes;
		}

		void ViewTransformChangedCallback(GraphView view) {
			if(graph != null && hasInitialize) {
				graphData.GetCurrentCanvasData().zoomScale = scale;
				graphData.position = -viewTransform.position / scale;
			}
		}

		void ElementResizedCallback(VisualElement elem) {

		}

		public override List<GPort> GetCompatiblePorts(GPort startPort, NodeAdapter nodeAdapter) {
			var compatiblePorts = new List<GPort>();
			var startPortView = startPort as PortView;

			compatiblePorts.AddRange(ports.ToList().Select(p => p as PortView).Where(p => {
				if(!p.enabledSelf || p.direction == startPort.direction || p.isFlow != startPortView.isFlow /*|| !startPortView.IsValidTarget(p)*/)
					return false;
				return startPortView.CanConnect(p);
			}));

			return compatiblePorts;
		}

		public List<PortView> GetCompatiblePorts(PortView startPort, NodeAdapter nodeAdapter) {
			var compatiblePorts = new List<PortView>();

			compatiblePorts.AddRange(ports.ToList().Select(p => p as PortView).Where(p => {
				if(p == null || !p.enabledSelf || p.direction == startPort.direction || p.isFlow != startPort.isFlow /*|| !startPortView.IsValidTarget(p)*/)
					return false;
				return startPort.CanConnect(p);
			}));

			return compatiblePorts;
		}

		private Vector3 _viewPosition;
#if UNITY_2023_2_OR_NEWER
		protected override void HandleEventBubbleUp(EventBase evt) { 
#elif UNITY_2022_1_OR_NEWER
		protected override void ExecuteDefaultActionAtTarget(EventBase evt) {
#else
		public override void HandleEvent(EventBase evt) {
#endif
			if(evt is MouseUpEvent && graphDragger.isActive) {
				graphDragger.OnMouseUp(evt as MouseUpEvent);
				this.ReleaseMouse();
				return;
			}
			else if(evt is IMouseEvent) {
				GetMousePosition(evt as IMouseEvent, out var position);
				graph.topMousePos = position;
			}
			if(evt is ContextualMenuPopulateEvent && graphDragger.isActive) {
				evt.StopImmediatePropagation();
				return;
			}
			//if(evt is FocusOutEvent focusOutEvent) {
			//	//For fix not focus bug ( cannot delete node, etc )
			//	if(focusOutEvent.target == this && focusOutEvent.relatedTarget == null) {
			//		uNodeThreadUtility.Queue(Focus);
			//		return;
			//	}
			//}
#if UNITY_2023_2_OR_NEWER
			base.HandleEventBubbleUp(evt);
#elif UNITY_2022_1_OR_NEWER
			base.ExecuteDefaultActionAtTarget(evt);
#else
			base.HandleEvent(evt);
#endif
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
			var screenRect = graph.window.GetMousePositionForMenu(evt.mousePosition);
			var clickedPos = GetMousePosition(evt, out var position);
			graph.topMousePos = position;

			if(evt.target is RegionNodeView) {
				evt.target = this;
			}

			#region Graph
			if(evt.target is GraphView && graphData.canAddNode) {
				#region Add Node
				evt.menu.AppendAction("Add Node", (e) => {
					graph.ShowNodeMenu(clickedPos);
				}, DropdownMenuAction.AlwaysEnabled);
				evt.menu.AppendAction("Add Node (Set)", (e) => {
					graph.ShowNodeMenu(clickedPos, new FilterAttribute() { SetMember = true, VoidType = false }, (node) => {
						node.EnsureRegistered();
						NodeEditorUtility.AddNewNode(graphData, new Vector2(node.nodeObject.position.x, node.position.y), delegate (Nodes.NodeSetValue n) {
							n.EnsureRegistered();
							NodeEditorUtility.ConnectPort(n.target, node.nodeObject.primaryValueOutput);
							n.value.AssignToDefault(MemberData.Default(n.target.type));
						});
						node.nodeObject.SetPosition(new Vector2(node.position.x - 150, node.position.y - 100));
					});
				}, DropdownMenuAction.AlwaysEnabled);
				evt.menu.AppendAction("Add Node (Favorites)", (e) => {
					graph.ShowFavoriteMenu(clickedPos);
				}, DropdownMenuAction.AlwaysEnabled);

				evt.menu.AppendAction("Add Linked Macro", (e) => {
					var macros = GraphUtility.FindGraphs<MacroGraph>();
					List<ItemSelector.CustomItem> customItems = new List<ItemSelector.CustomItem>();
					foreach(var macro in macros) {
						var m = macro;
						customItems.Add(ItemSelector.CustomItem.Create(
							m.GetGraphName(), 
							() => {
								CreateLinkedMacro(m, clickedPos);
							}, 
							m.category,
							icon: uNodeEditorUtility.GetTypeIcon(m.GetIcon()),
							tooltip: new GUIContent(m.GraphData.comment)));
					}
					ItemSelector.ShowWindow(null, null, null, customItems).ChangePosition(graph.GetMenuPosition()).displayDefaultItem = false;
				}, DropdownMenuAction.AlwaysEnabled);
				#endregion

				#region Event & State
				evt.menu.AppendSeparator("");
				if(graphData.currentCanvas is MainGraphContainer) {
					if(graphData.graph is IStateGraph state && state.CanCreateStateGraph) {
						evt.menu.AppendAction("Add State", (e) => {
							NodeEditorUtility.AddNewNode<Nodes.StateNode>(graphData,
								"State",
								clickedPos);
							graph.Refresh();
						}, DropdownMenuAction.AlwaysEnabled);

						//Add events
						var eventMenus = NodeEditorUtility.FindEventMenu();
						foreach(var menu in eventMenus) {
							if(menu.IsValidScopes(NodeScope.StateGraph)) {
								evt.menu.AppendAction("Add Event/" + (string.IsNullOrEmpty(menu.category) ? menu.name : menu.category + "/" + menu.name), (e) => {
									NodeEditorUtility.AddNewNode<Node>(graphData, menu.nodeName, menu.type, clickedPos);
									graph.Refresh();
								}, DropdownMenuAction.AlwaysEnabled);
							}
						}
					}
					else if(graphData.graph is ICustomMainGraph mainGraph) {
						var scopes = mainGraph.MainGraphScope.Split(',');

						//Add events
						var eventMenus = NodeEditorUtility.FindEventMenu();
						foreach(var menu in eventMenus) {
							if(menu.IsValidScopes(scopes)) {
								evt.menu.AppendAction("Add Event/" + (string.IsNullOrEmpty(menu.category) ? menu.name : menu.category + "/" + menu.name), (e) => {
									NodeEditorUtility.AddNewNode<Node>(graphData, menu.nodeName, menu.type, clickedPos);
									graph.Refresh();
								}, DropdownMenuAction.AlwaysEnabled);
							}
						}
					}
				}
				else if(graphData.currentCanvas is NodeObject superNode && superNode.node is Nodes.StateNode) {
					#region Add Event
					var eventMenus = NodeEditorUtility.FindEventMenu();
					foreach(var menu in eventMenus) {
						if(menu.type.IsDefinedAttribute<StateEventAttribute>()) {
							evt.menu.AppendAction("Add Event/" + (string.IsNullOrEmpty(menu.category) ? menu.name : menu.category + "/" + menu.name), (e) => {
								NodeEditorUtility.AddNewNode<Node>(graphData, menu.nodeName, menu.type, clickedPos);
								graph.Refresh();
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}
					#endregion
				}
				evt.menu.AppendSeparator("");
				#endregion

				#region Add Region
				evt.menu.AppendAction("Add Region", (e) => {
					SelectionAddRegion(position);
				}, DropdownMenuAction.AlwaysEnabled);
				#endregion

				#region Add Notes
				evt.menu.AppendAction("Add Note", (e) => {
					Rect rect = new Rect(clickedPos.x, clickedPos.y, 200, 130);
					NodeEditorUtility.AddNewNode<Nodes.StickyNote>(graphData, clickedPos, (node) => {
						node.nodeObject.name = "Title";
						node.nodeObject.comment = "type something here";
						node.position = rect;
					});
					graph.Refresh();
				}, DropdownMenuAction.AlwaysEnabled);
				#endregion

				#region Add Await
				if(graphData.selectedRoot is Function) {
					var func = graphData.selectedRoot as Function;
					if(func.modifier.Async) {
						evt.menu.AppendAction("Add Await", (e) => {
							Rect rect = new Rect(clickedPos.x, clickedPos.y, 200, 130);
							NodeEditorUtility.AddNewNode<Nodes.AwaitNode>(graphData, clickedPos, (node) => {
								node.position = rect;
							});
							graph.Refresh();
						}, DropdownMenuAction.AlwaysEnabled);
					}
				}
				#endregion

				#region Return & Jump
				if(!(graphData.selectedRoot is MainGraphContainer)) {
					evt.menu.AppendAction("Jump Statement/Add Return", (e) => {
						var selectedNodes = graphData.selectedNodes.ToArray();
						Rect rect = selectedNodes.Length > 0 ? NodeEditorUtility.GetNodeRect(selectedNodes) : new Rect(clickedPos.x, clickedPos.y, 200, 130);
						NodeEditorUtility.AddNewNode<Nodes.NodeReturn>(graphData, clickedPos, (node) => {
							rect.x -= 30;
							rect.y -= 50;
							rect.width += 60;
							rect.height += 70;
							node.position = rect;
						});
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Jump Statement/Add Break", (e) => {
						var selectedNodes = graphData.selectedNodes.ToArray();
						Rect rect = selectedNodes.Length > 0 ? NodeEditorUtility.GetNodeRect(selectedNodes) : new Rect(clickedPos.x, clickedPos.y, 200, 130);
						NodeEditorUtility.AddNewNode<Nodes.NodeBreak>(graphData, clickedPos, (node) => {
							rect.x -= 30;
							rect.y -= 50;
							rect.width += 60;
							rect.height += 70;
							node.position = rect;
						});
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Jump Statement/Add Continue", (e) => {
						var selectedNodes = graphData.selectedNodes.ToArray();
						Rect rect = selectedNodes.Length > 0 ? NodeEditorUtility.GetNodeRect(selectedNodes) : new Rect(clickedPos.x, clickedPos.y, 200, 130);
						NodeEditorUtility.AddNewNode<Nodes.NodeContinue>(graphData, clickedPos, (node) => {
							rect.x -= 30;
							rect.y -= 50;
							rect.width += 60;
							rect.height += 70;
							node.position = rect;
						});
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
				}
				#endregion

				#region Goto
				{//Goto
					int index = 0;
					bool hasAddSeparator = false;
					if(graphData.nodes != null && graphData.nodes.Any()) {
						foreach(var n in graphData.nodes) {
							if(n == null)
								continue;
							//if(n is ISuperNode || n is IMacro) {
							//	if(!hasAddSeparator) {
							//		evt.menu.AppendSeparator("");
							//		hasAddSeparator = true;
							//	}
							//	evt.menu.AppendAction("goto/Group/[" + index + "]" + n.gameObject.name, (e) => {
							//		Frame(new Node[] { n });
							//	}, DropdownMenuAction.AlwaysEnabled);
							//	index++;
							//}
							if(n.node is BaseEventNode eventNode) {
								if(!hasAddSeparator) {
									evt.menu.AppendSeparator("");
									hasAddSeparator = true;
								}
								evt.menu.AppendAction("goto/Group/[" + index + "]" + n.GetTitle(), (e) => {
									Frame(new NodeObject[] { n });
								}, DropdownMenuAction.AlwaysEnabled);
								index++;
							}
						}
					}
				}
				#endregion

				#region Place Fit
				if(graphData.selectedGroup != null) {
					//TODO: fix this
					//if(editorData.selectedGroup is ISuperNode) {
					//	ISuperNode superNode = editorData.selectedGroup as ISuperNode;
					//	foreach(var n in superNode.nestedFlowNodes) {
					//		if(n == null)
					//			continue;
					//		UNodeView view;
					//		if(nodeViewsPerNode.TryGetValue(n, out view)) {
					//			evt.menu.AppendAction("Place fit nodes", (e) => {
					//				foreach(var node in superNode.nestedFlowNodes) {
					//					if(node == null)
					//						continue;
					//					UNodeView nView;
					//					if(nodeViewsPerNode.TryGetValue(node, out nView)) {
					//						UIElementUtility.PlaceFitNodes(nView);
					//					}
					//				}
					//			}, DropdownMenuAction.AlwaysEnabled);
					//		}
					//	}
					//}
				}
				else if(graphData.currentCanvas is MainGraphContainer) {
					var events = graph.nodes.Where(n => n.node is BaseEventNode);
					List<UNodeView> views = new List<UNodeView>();
					foreach(var n in events) {
						if(nodeViewsPerNode.TryGetValue(n, out var nView)) {
							views.Add(nView);
						}
					}
					if(views.Count > 0) {
						evt.menu.AppendAction("Place fit nodes", (e) => {
							for(int i = 0; i < views.Count; i++) {
								UIElementUtility.PlaceFitNodes(views[i]);
							}
						}, DropdownMenuAction.AlwaysEnabled);
					}
				}
				else if(graphData.selectedRoot is NodeContainerWithEntry nodeContainerWithEntry && nodeContainerWithEntry.Entry != null) {
					UNodeView view;
					if(nodeViewsPerNode.TryGetValue(nodeContainerWithEntry.Entry, out view)) {
						evt.menu.AppendAction("Place fit nodes", (e) => {
							UIElementUtility.PlaceFitNodes(view);
						}, DropdownMenuAction.AlwaysEnabled);
					}
				}
				#endregion

				if(selection.Count > 0 && selection.Any(s => (s is UNodeView nodeView) && !nodeView.isBlock)) {
					evt.menu.AppendAction("Selection to macro", (e) => {
						SelectionToMacro(clickedPos);
					}, DropdownMenuAction.AlwaysEnabled);
				}

				evt.menu.AppendAction("Take Screenshot", (e) => {
					CaptureGraphScreenshot();
				}, DropdownMenuAction.AlwaysEnabled);

				#region Graph Commands
				evt.menu.AppendSeparator("");
				var commands = NodeEditorUtility.FindGraphCommands();
				if(commands != null && commands.Count > 0) {
					bool addSeparator = false;
					foreach(var c in commands) {
						c.graph = graph;
						c.mousePositionOnCanvas = clickedPos;
						if(c.IsValid()) {
							if(c.name == "") {
								evt.menu.AppendSeparator("");
							}
							else {
								evt.menu.AppendAction(c.name, (e) => {
									c.OnClick(position);
								}, DropdownMenuAction.AlwaysEnabled);
							}
							addSeparator = true;
						}
					}
					if(addSeparator) {
						evt.menu.AppendSeparator("");
					}
				}
				#endregion

				evt.menu.AppendSeparator("");
				if(graphData.selectedNodes.Any()) {
					evt.menu.AppendAction("Copy", (e) => {
						CopySelectedNodes();
					}, DropdownMenuAction.AlwaysEnabled);
				}
				else {
					evt.menu.AppendAction("Copy", null, DropdownMenuAction.AlwaysDisabled);
				}

				if(GraphUtility.CopyPaste.IsCopiedNodes) {
					evt.menu.AppendAction("Paste", (e) => {
						graph.PasteNode(clickedPos);
					}, DropdownMenuAction.AlwaysEnabled);
				}
				else {
					evt.menu.AppendAction("Paste", null, DropdownMenuAction.AlwaysDisabled);
				}
				if(graphData.selectedNodes.Any()) {
					evt.menu.AppendAction("Delete", (e) => {
						DeleteSelectionCallback(AskUser.DontAskUser);
					}, DropdownMenuAction.AlwaysEnabled);
				}
				else {
					evt.menu.AppendAction("Delete", null, DropdownMenuAction.AlwaysDisabled);
				}
			}
			#endregion

			#region Node
			if(evt.target is BaseNodeView) {
				var nodeView = evt.target as BaseNodeView;
				if(nodeView.targetNode != null) {
					Node node = nodeView.targetNode;
					if(node == null)
						return;

					//evt.menu.AppendAction("Inspect...", (e) => {
					//	ActionPopupWindow.ShowWindow(screenRect, () => {
					//		graph.Select(node.nodeObject);
					//		CustomInspector.ShowInspector(graphData);
					//	}, 300, 400);
					//}, DropdownMenuAction.AlwaysEnabled);

					//if(nodeView is INodeBlock) {
					//	INodeBlock blockView = nodeView as INodeBlock;
					//	evt.menu.AppendSeparator("");
					//	switch(blockView.blockType) {
					//		case BlockType.Action:
					//			evt.menu.AppendAction("Add new block", (e) => {
					//				BlockUtility.ShowAddActionMenu(
					//					graph.topMousePos,
					//					(act) => {
					//						blockView.nodeView.RegisterUndo("");
					//						blockView.blocks.AddBlock(act);
					//						blockView.nodeView.MarkRepaint();
					//						uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
					//					},
					//					MemberData.CreateFromValue(editorData.graph));
					//			}, DropdownMenuAction.AlwaysEnabled);
					//			break;
					//		case BlockType.Condition:
					//			evt.menu.AppendAction("Add new block", (e) => {
					//				BlockUtility.ShowAddEventMenu(
					//					graph.topMousePos,
					//					MemberData.CreateFromValue(editorData.graph),
					//					(act) => {
					//						blockView.nodeView.RegisterUndo("");
					//						blockView.blocks.AddBlock(act);
					//						blockView.nodeView.MarkRepaint();
					//						uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
					//					});
					//			}, DropdownMenuAction.AlwaysEnabled);
					//			evt.menu.AppendAction("Add 'OR' block", (e) => {
					//				blockView.nodeView.RegisterUndo("");
					//				blockView.blocks.AddBlock(null, EventActionData.EventType.Or);
					//				blockView.nodeView.MarkRepaint();
					//				uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
					//			}, DropdownMenuAction.AlwaysEnabled);
					//			break;
					//		case BlockType.CoroutineAction:
					//			evt.menu.AppendAction("Add new block", (e) => {
					//				BlockUtility.ShowAddActionMenu(
					//				graph.topMousePos,
					//				(act) => {
					//					blockView.nodeView.RegisterUndo("");
					//					blockView.blocks.AddBlock(act);
					//					blockView.nodeView.MarkRepaint();
					//					uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
					//				},
					//				MemberData.CreateFromValue(editorData.graph),
					//				true);
					//			}, DropdownMenuAction.AlwaysEnabled);
					//			break;
					//	}
					//	evt.menu.AppendSeparator("");
					//	if(blockView.blocks != null && blockView.blocks.blocks != null && blockView.blocks.blocks.Count > 0) {
					//		evt.menu.AppendAction("Expand blocks", (e) => {
					//			blockView.nodeView.RegisterUndo("");
					//			foreach(var b in blockView.blocks.blocks) {
					//				b.expanded = true;
					//			}
					//			blockView.nodeView.MarkRepaint();
					//		}, DropdownMenuAction.AlwaysEnabled);
					//		evt.menu.AppendAction("Colapse blocks", (e) => {
					//			blockView.nodeView.RegisterUndo("");
					//			foreach(var b in blockView.blocks.blocks) {
					//				b.expanded = false;
					//			}
					//			blockView.nodeView.MarkRepaint();
					//		}, DropdownMenuAction.AlwaysEnabled);
					//	}
					//}

					#region Transition
					if(node is Nodes.StateNode) {
						evt.menu.AppendSeparator("");
						foreach(TransitionMenu menuItem in NodeEditorUtility.FindTransitionMenu()) {
							object[] eventObject = new object[]{
								menuItem.type,
								menuItem.name,
							};
							evt.menu.AppendAction("Add Transition/" + menuItem.path, (Action<DropdownMenuAction>)((e) => {
								object[] objToArray = e.userData as object[];
								System.Type type = (System.Type)objToArray[0];
								if(node is Nodes.StateNode stateNode) {
									var transition = new NodeObject();
									NodeEditorUtility.AddNewNode<TransitionEvent>(
										stateNode.transitions.container,
										objToArray[1] as string,
										type,
										new Vector2(stateNode.position.width / 2, (stateNode.position.height / 2) + 50),
										(transition) => {
											MarkRepaint();
										});
								}
							}), DropdownMenuAction.AlwaysEnabled, eventObject);
						}
						evt.menu.AppendSeparator("");
					}
					#endregion

					#region Node commands
					evt.menu.AppendSeparator("");
					var commands = NodeEditorUtility.FindNodeCommands();
					if(commands != null && commands.Count > 0) {
						bool addSeparator = false;
						foreach(var c in commands) {
							c.graph = graph;
							c.mousePositionOnCanvas = clickedPos;
							if(c.IsValidNode(node)) {
								if(c.name == "") {
									evt.menu.AppendSeparator("");
								}
								else {
									evt.menu.AppendAction(c.name, (e) => {
										c.OnClick(node, position);
									}, DropdownMenuAction.AlwaysEnabled);
								}
								addSeparator = true;
							}
						}
						if(addSeparator) {
							evt.menu.AppendSeparator("");
						}
					}
					#endregion

					#region MultipurposeNode
					if(node is MultipurposeNode) {
						MultipurposeNode mNode = node as MultipurposeNode;
						if(mNode.target.isAssigned) {
							if(mNode.target.targetType == MemberData.TargetType.Method) {
								var members = mNode.target.GetMembers(false);
								if(members != null && members.Length == 1) {
									var member = members[members.Length - 1];
									BindingFlags flag = BindingFlags.Public;
									if(mNode.target.isStatic) {
										flag |= BindingFlags.Static;
									}
									else {
										flag |= BindingFlags.Instance;
									}
									var memberName = member.Name;
									var mets = member.ReflectedType.GetMember(memberName, flag);
									List<MethodInfo> methods = new List<MethodInfo>();
									foreach(var m in mets) {
										if(m is MethodInfo) {
											methods.Add(m as MethodInfo);
										}
									}
									foreach(var m in methods) {
										evt.menu.AppendAction("Change Methods/" + EditorReflectionUtility.GetPrettyMethodName(m), (e) => {
											object[] objs = e.userData as object[];
											MultipurposeNode nod = objs[0] as MultipurposeNode;
											MethodInfo method = objs[1] as MethodInfo;
											if(member != m) {
												if(method.IsGenericMethodDefinition) {
													TypeBuilderWindow.Show(graph.topMousePos, graphData.currentCanvas, new FilterAttribute() { UnityReference = false }, delegate (MemberData[] types) {
														uNodeEditorUtility.RegisterUndo(nod.GetUnityObject());
														method = ReflectionUtils.MakeGenericMethod(method, types.Select(i => i.Get<Type>(null)).ToArray());
														MemberData d = new MemberData(method);
														nod.target.CopyFrom(d);
														MarkRepaint(nod);
													}, new TypeItem[method.GetGenericArguments().Length]);
												}
												else {
													uNodeEditorUtility.RegisterUndo(nod.GetUnityObject());
													MemberData d = new MemberData(method);
													nod.target.CopyFrom(d);
													uNodeGUIUtility.GUIChanged(nod, UIChangeType.Important);
												}
											}
										}, (e) => {
											if(member == m) {
												return DropdownMenuAction.Status.Checked;
											}
											return DropdownMenuAction.Status.Normal;
										}, new object[] { node, m });
									}
								}
							}
							else if(mNode.target.targetType == MemberData.TargetType.Constructor) {
								var members = mNode.target.GetMembers(false);
								if(members != null && members.Length == 1) {
									var member = members[members.Length - 1];
									if(member != null) {
										BindingFlags flag = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
										var ctors = member.ReflectedType.GetConstructors(flag);
										foreach(var m in ctors) {
											if(!ReflectionUtils.IsPublicMember(m))
												continue;
											evt.menu.AppendAction("Change Constructors/" + EditorReflectionUtility.GetPrettyConstructorName(m), (e) => {
												object[] objs = e.userData as object[];
												MultipurposeNode nod = objs[0] as MultipurposeNode;
												ConstructorInfo ctor = objs[1] as ConstructorInfo;
												if(member != m) {
													uNodeEditorUtility.RegisterUndo(nod.GetUnityObject());
													MemberData d = new MemberData(ctor);
													nod.target.CopyFrom(d);
													uNodeGUIUtility.GUIChanged(nod, UIChangeType.Important);
												}
											}, (e) => {
												if(member == m) {
													return DropdownMenuAction.Status.Checked;
												}
												return DropdownMenuAction.Status.Normal;
											}, new object[] { node, m });
										}
									}
								}
							}
							else if(mNode.target.targetType == MemberData.TargetType.uNodeFunction) {
								var currMethod = mNode.target.startItem.reference?.ReferenceValue as Function;
								if(currMethod != null) {
									var methods = currMethod.graphContainer.GetFunctions().Where(f => f.name == currMethod.name);
									foreach(var m in methods) {
										evt.menu.AppendAction("Change Functions/" + EditorReflectionUtility.GetPrettyFunctionName(m), (e) => {
											object[] objs = e.userData as object[];
											MultipurposeNode nod = objs[0] as MultipurposeNode;
											Function method = objs[1] as Function;
											uNodeEditorUtility.RegisterUndo(nod.GetUnityObject());
											MemberData d = MemberData.CreateFromValue(method);
											nod.target.CopyFrom(d);
											uNodeGUIUtility.GUIChanged(nod, UIChangeType.Important);
										}, (e) => {
											if(currMethod == m) {
												return DropdownMenuAction.Status.Checked;
											}
											return DropdownMenuAction.Status.Normal;
										}, new object[] { node, m });
									}
								}
							}
						}
					}
					#endregion

					#region References
					{
						if(node is MultipurposeNode) {
							var mNode = node as MultipurposeNode;
							if(mNode.target.isTargeted) {
								UIElementUtility.ShowReferenceMenu(evt, mNode.target);
								evt.menu.AppendSeparator("References/");
							}
						}
						else {
							evt.menu.AppendAction("References/Find Node Usages", (e) => {
								GraphUtility.ShowNodeUsages(node.GetType());
							}, DropdownMenuAction.AlwaysEnabled);
							if(node is LinkedMacroNode) {
								var macro = (node as LinkedMacroNode).macroAsset;
								if(macro != null) {
									evt.menu.AppendAction("References/Find Macro Usages", (e) => {
										GraphUtility.ShowNodeUsages(n => {
											if(n is LinkedMacroNode linked) {
												return linked.macroAsset == macro;
											}
											return false;
										});
									}, DropdownMenuAction.AlwaysEnabled);
								}
							}
							evt.menu.AppendSeparator("References/");
						}
						MonoScript ms = uNodeEditorUtility.GetMonoScript(node);
						if(ms != null) {
							evt.menu.AppendAction("References/Find Script", (e) => {
								EditorGUIUtility.PingObject(ms);
							}, DropdownMenuAction.AlwaysEnabled);
							evt.menu.AppendAction("References/Edit Script", (e) => {
								AssetDatabase.OpenAsset(ms);
							}, DropdownMenuAction.AlwaysEnabled);
						}
						if(nodeView.GetType().IsDefinedAttribute<NodeCustomEditor>()) {
							MonoScript ec = uNodeEditorUtility.GetMonoScript(nodeView);
							if(ec != null) {
								evt.menu.AppendAction("References/Edit Editor Script", (e) => {
									AssetDatabase.OpenAsset(ec);
								}, DropdownMenuAction.AlwaysEnabled);
							}
						}
					}
					#endregion

					#region Add Region
					if(selection.Count > 0 && selection.Any(s => (s is UNodeView nodeView) && !nodeView.isBlock)) {
						evt.menu.AppendAction("Add Region", (e) => {
							SelectionAddRegion(position);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					#endregion

					if(!GraphDebug.Breakpoint.HasBreakpoint(uNodeUtility.GetObjectID(node.GetUnityObject()), node.id)) {
						evt.menu.AppendAction("Add Breakpoint", (e) => {
							GraphDebug.Breakpoint.AddBreakpoint(uNodeUtility.GetObjectID(node.GetUnityObject()), node.id);
							MarkRepaint(node);
							uNodeGUIUtility.GUIChanged(node);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					else {
						evt.menu.AppendAction("Remove Breakpoint", (e) => {
							GraphDebug.Breakpoint.RemoveBreakpoint(uNodeUtility.GetObjectID(node.GetUnityObject()), node.id);
							MarkRepaint(node);
							uNodeGUIUtility.GUIChanged(node);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					evt.menu.AppendSeparator("");
					if(nodeView.inputPorts.Any(p => p.connected && !p.IsProxy()) || nodeView.outputPorts.Any(p => p.connected && !p.IsProxy())) {
						evt.menu.AppendAction("Place fit nodes", (e) => {
							UIElementUtility.PlaceFitNodes(nodeView);
						}, DropdownMenuAction.AlwaysEnabled);
						//if(nodeView.outputPorts.Any(p => p.connected && p.orientation == Orientation.Vertical)) {
						//	evt.menu.AppendAction("Place fit flow nodes", (e) => {
						//		UIElementUtility.PlaceFitNodes(nodeView);
						//	}, DropdownMenuAction.AlwaysEnabled);
						//}
						//if(nodeView.inputPorts.Any(p => p.connected && p.orientation == Orientation.Horizontal && !p.IsProxy()) ||
						//	nodeView.outputPorts.Any(p => p.connected && p.orientation == Orientation.Horizontal && !p.IsProxy())) {
						//	evt.menu.AppendAction("Place fit value nodes", (e) => {
						//		UIElementUtility.PlaceFitNodes(nodeView);
						//	}, DropdownMenuAction.AlwaysEnabled);
						//}
					}
					if(selection.Count > 0 && selection.Any(s => (s is UNodeView nodeView) && !nodeView.isBlock)) {

						evt.menu.AppendAction("Selection to macro", (e) => {
							SelectionToMacro(clickedPos);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					//TODO: fix me
					//{
					//	bool hasConnectedNode = false;
					//	if(node as StateNode) {
					//		StateNode eventNode = node as StateNode;
					//		TransitionEvent[] TE = eventNode.GetTransitions();
					//		foreach(TransitionEvent T in TE) {
					//			if(T.GetTargetNode() != null) {
					//				hasConnectedNode = true;
					//				break;
					//			}
					//		}
					//	}
					//	if(!hasConnectedNode) {
					//		AnalizerUtility.AnalizeObject(node, delegate (object o) {
					//			if(o is MemberData) {
					//				MemberData member = o as MemberData;
					//				if(member.targetType == MemberData.TargetType.FlowNode ||
					//					member.targetType == MemberData.TargetType.ValueNode) {
					//					if(member.isAssigned && member.instance is Node) {
					//						hasConnectedNode = true;
					//						return false;
					//					}
					//				}
					//			}
					//			return false;
					//		});
					//	}
					//	if(hasConnectedNode) {
					//		evt.menu.AppendSeparator("");
					//		evt.menu.AppendAction("Select Connected Node", (e) => {
					//			graph.SelectConnectedNode(node, false, (n) => {
					//				UNodeView view;
					//				if(nodeViewsPerNode.TryGetValue(n, out view)) {
					//					base.AddToSelection(view);
					//				}
					//			});
					//		}, DropdownMenuAction.AlwaysEnabled);
					//		evt.menu.AppendAction("Select All Connected Node", (e) => {
					//			graph.SelectConnectedNode(node, true, (n) => {
					//				UNodeView view;
					//				if(nodeViewsPerNode.TryGetValue(n, out view)) {
					//					base.AddToSelection(view);
					//				}
					//			});
					//		}, DropdownMenuAction.AlwaysEnabled);
					//	}
					//}
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Copy", (e) => {
						GraphUtility.CopyPaste.Copy(node.nodeObject);
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Remove", (e) => {
						uNodeEditorUtility.RegisterUndo(node.GetUnityObject(), "Remove");
						OnNodeRemoved(nodeView);
						node.nodeObject.Destroy();
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);

				}
			}
			#endregion

			#region Block
			//if(evt.target is BlockView) {
			//	BlockView blockView = evt.target as BlockView;
			//	if(blockView.data != null && blockView.data.block != null) {
			//		evt.menu.AppendAction("Edit", (e) => {
			//			FieldsEditorWindow window = FieldsEditorWindow.ShowWindow();
			//			window.titleContent = new GUIContent(blockView.data.block.GetType().Name);
			//			window.targetField = blockView.data.block;
			//			window.targetObject = blockView.ownerNode.targetNode;
			//		}, DropdownMenuAction.AlwaysEnabled);
			//	}
			//	evt.menu.AppendSeparator("");
			//	switch(blockView.owner.blockType) {
			//		case BlockType.Action:
			//			evt.menu.AppendAction("Insert new block above", (e) => {
			//				BlockUtility.ShowAddActionMenu(
			//					graph.topMousePos,
			//					(act) => {
			//						int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
			//						blockView.RegisterUndo("");
			//						blockView.owner.blocks.InsertBlock(index, act);
			//						blockView.ownerNode.MarkRepaint();
			//						uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//					},
			//					MemberData.CreateFromValue(editorData.graph));
			//			}, DropdownMenuAction.AlwaysEnabled);
			//			evt.menu.AppendAction("Insert new block below", (e) => {
			//				BlockUtility.ShowAddActionMenu(
			//					graph.topMousePos,
			//					(act) => {
			//						int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
			//						blockView.RegisterUndo("");
			//						blockView.owner.blocks.InsertBlock(index + 1, act);
			//						blockView.ownerNode.MarkRepaint();
			//						uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//					},
			//					MemberData.CreateFromValue(editorData.graph));
			//			}, DropdownMenuAction.AlwaysEnabled);
			//			break;
			//		case BlockType.Condition:
			//			evt.menu.AppendAction("Insert new block above", (e) => {
			//				BlockUtility.ShowAddEventMenu(
			//					graph.topMousePos,
			//					MemberData.CreateFromValue(editorData.graph),
			//					(act) => {
			//						int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
			//						blockView.RegisterUndo("");
			//						blockView.owner.blocks.InsertBlock(index, act);
			//						blockView.ownerNode.MarkRepaint();
			//						uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//					});
			//			}, DropdownMenuAction.AlwaysEnabled);
			//			evt.menu.AppendAction("Insert new block below", (e) => {
			//				BlockUtility.ShowAddActionMenu(
			//				graph.topMousePos,
			//				(act) => {
			//					int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
			//					blockView.RegisterUndo("");
			//					blockView.owner.blocks.InsertBlock(index + 1, act);
			//					blockView.ownerNode.MarkRepaint();
			//					uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//				},
			//				MemberData.CreateFromValue(editorData.graph),
			//				true);
			//			}, DropdownMenuAction.AlwaysEnabled);
			//			evt.menu.AppendSeparator("");
			//			evt.menu.AppendAction("Insert 'OR' block above", (e) => {
			//				int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
			//				blockView.RegisterUndo("");
			//				blockView.owner.blocks.InsertBlock(index, null, EventActionData.EventType.Or);
			//				blockView.ownerNode.MarkRepaint();
			//				uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//			}, DropdownMenuAction.AlwaysEnabled);
			//			evt.menu.AppendAction("Insert 'OR' block below", (e) => {
			//				int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
			//				blockView.RegisterUndo("");
			//				blockView.owner.blocks.InsertBlock(index + 1, null, EventActionData.EventType.Or);
			//				blockView.ownerNode.MarkRepaint();
			//				uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//			}, DropdownMenuAction.AlwaysEnabled);
			//			break;
			//		case BlockType.CoroutineAction:
			//			evt.menu.AppendAction("Insert new block above", (e) => {
			//				BlockUtility.ShowAddActionMenu(
			//				graph.topMousePos,
			//				(act) => {
			//					int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
			//					blockView.RegisterUndo("");
			//					blockView.owner.blocks.InsertBlock(index, act);
			//					blockView.ownerNode.MarkRepaint();
			//					uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//				},
			//				MemberData.CreateFromValue(editorData.graph),
			//				true);
			//			}, DropdownMenuAction.AlwaysEnabled);
			//			evt.menu.AppendAction("Insert new block below", (e) => {
			//				BlockUtility.ShowAddActionMenu(
			//				graph.topMousePos,
			//				(act) => {
			//					int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
			//					blockView.RegisterUndo("");
			//					blockView.owner.blocks.InsertBlock(index + 1, act);
			//					blockView.ownerNode.MarkRepaint();
			//					uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//				},
			//				MemberData.CreateFromValue(editorData.graph),
			//				true);
			//			}, DropdownMenuAction.AlwaysEnabled);
			//			break;
			//	}
			//	evt.menu.AppendSeparator("");
			//	MonoScript ms = uNodeEditorUtility.GetMonoScript(blockView.data.block);
			//	if(ms != null) {
			//		evt.menu.AppendAction("Find Script", (e) => {
			//			EditorGUIUtility.PingObject(ms);
			//		}, DropdownMenuAction.AlwaysEnabled);
			//		evt.menu.AppendAction("Edit Script", (e) => {
			//			AssetDatabase.OpenAsset(ms);
			//		}, DropdownMenuAction.AlwaysEnabled);
			//	}
			//	var es = uNodeEditorUtility.GetMonoScript(blockView);
			//	if(es != null) {
			//		evt.menu.AppendAction("Edit Editor Script", (e) => {
			//			AssetDatabase.OpenAsset(es);
			//		}, DropdownMenuAction.AlwaysEnabled);
			//	}
			//	evt.menu.AppendSeparator("");
			//	evt.menu.AppendAction("Duplicate", (e) => {
			//		blockView.RegisterUndo("Duplicate");
			//		int idx = blockView.owner.blocks.blocks.IndexOf(blockView.data);
			//		var data = SerializerUtility.Duplicate(blockView.data);
			//		blockView.owner.blocks.blocks.Insert(idx, data);
			//		blockView.ownerNode.MarkRepaint();
			//		uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//	}, DropdownMenuAction.AlwaysEnabled);
			//	evt.menu.AppendAction("Remove", (e) => {
			//		OnNodeRemoved(blockView);
			//		blockView.owner.RemoveBlock(blockView);
			//		uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
			//	}, DropdownMenuAction.AlwaysEnabled);
			//}
			#endregion

			#region Transition
			//if(evt.target is TransitionView) {
			//	TransitionView view = evt.target as TransitionView;

			//	MonoScript ms = uNodeEditorUtility.GetMonoScript(view.transition);
			//	if(ms != null) {
			//		evt.menu.AppendAction("Find Script", (e) => {
			//			EditorGUIUtility.PingObject(ms);
			//		}, DropdownMenuAction.AlwaysEnabled);
			//		evt.menu.AppendAction("Edit Script", (e) => {
			//			AssetDatabase.OpenAsset(ms);
			//		}, DropdownMenuAction.AlwaysEnabled);
			//	}
			//	MonoScript ec = uNodeEditorUtility.GetMonoScript(view);
			//	if(ec != null) {
			//		evt.menu.AppendAction("Edit Editor Script", (e) => {
			//			AssetDatabase.OpenAsset(ec);
			//		}, DropdownMenuAction.AlwaysEnabled);
			//	}
			//	if(!uNodePreference.preferenceData.hideChildObject) {
			//		evt.menu.AppendAction("Find GameObject", (e) => {
			//			EditorGUIUtility.PingObject(view.transition);
			//		}, DropdownMenuAction.AlwaysEnabled);
			//	}
			//	evt.menu.AppendSeparator("");
			//	evt.menu.AppendAction("Remove", (e) => {
			//		OnNodeRemoved(view);
			//		Undo.DestroyObjectImmediate(view.transition);
			//		//NodeEditorUtility.RemoveObject(view.transition.gameObject);
			//		graph.Refresh();
			//	}, DropdownMenuAction.AlwaysEnabled);
			//}
			#endregion

			#region Edge
			if(evt.target is EdgeView) {
				EdgeView edge = evt.target as EdgeView;
				if(edge is not ConversionEdgeView) {
					evt.menu.AppendAction("Convert to proxy", (e) => {
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Convert to proxy");
						edge.connection.isProxy = true;
						graph.GUIChanged();
						edge.GetSenderPort()?.owner.MarkRepaint();
						edge.GetReceiverPort()?.owner.MarkRepaint();
					}, DropdownMenuAction.AlwaysEnabled);
				}
				evt.menu.AppendAction("Remove", (e) => {
					uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove port");
					DeleteSelection(new List<ISelectable>() { edge });
				}, DropdownMenuAction.AlwaysEnabled);
			}
			#endregion

			#region Ports
			if(evt.target is PortView) {
				PortView port = evt.target as PortView;
				if(port.isValue) {//Value port
					if(port.direction == Direction.Input) {//Input port
						var p = port.GetPortValue<ValueInput>();
						if(p.UseDefaultValue) {
							var commands = NodeEditorUtility.FindPortCommands();
							if(commands != null && commands.Count > 0) {
								PortCommandData commandData = new PortCommandData() {
									port = p,
									portName = port.GetName(),
									portType = port.GetPortType(),
									portKind = PortKind.ValueInput,
								};
								foreach(var c in commands) {
									c.graph = NodeGraph.openedGraph;
									c.mousePositionOnCanvas = clickedPos;
									c.filter = port.portData.GetFilter();
									if(c.IsValidPort(port.owner.targetNode, commandData)) {
										evt.menu.AppendAction(c.name, (e) => {
											c.OnClick(port.owner.targetNode, commandData, position);
											MarkRepaint(port.owner.targetNode);
											// c.graph.Refresh();
										}, DropdownMenuAction.AlwaysEnabled);
									}
								}
								evt.menu.AppendSeparator();
							}
						}
						if(port.IsProxy()) {
							evt.menu.AppendAction("Unproxy", (act) => {
								Undo.SetCurrentGroupName("Unproxy");
								port.owner.RegisterUndo();
								foreach(var edge in port.GetValidEdges()) {
									if(edge.connection.isValid) {
										edge.connection.isProxy = false;
									}
								}
								graph.Refresh();
							}, DropdownMenuAction.AlwaysEnabled);
						}
						evt.menu.AppendAction("Reset", (e) => {
							Undo.SetCurrentGroupName("Reset");
							port.owner.RegisterUndo();
							var portFilter = port.GetFilter();
							var type = portFilter.Types.FirstOrDefault() ?? port.GetPortType();
							MemberData val = null;
							if(portFilter.IsValidTypeForValueConstant(type)) {
								if(type != null && ReflectionUtils.CanCreateInstance(type) && !portFilter.SetMember) {
									val = MemberData.CreateValueFromType(type);
								}
								else if(type is RuntimeType) {
									val = MemberData.CreateFromValue(null, type);
								}
							}
							if(val == null) {
								val = MemberData.None;
							}
							MarkRepaint(port.owner);
							foreach(var edge in port.GetValidEdges()) {
								if(edge.connection.isValid) {
									MarkRepaint(edge.connection.Output.node);
								}
							}
							p.AssignToDefault(val);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					else {
						//Output port
						var commands = NodeEditorUtility.FindPortCommands();
						if(commands != null && commands.Count > 0) {
							PortCommandData commandData = new PortCommandData() {
								port = port.GetPortValue(),
								portType = port.portType,
								portName = port.GetName(),
								portKind = PortKind.ValueOutput,
							};
							foreach(var c in commands) {
								c.graph = NodeGraph.openedGraph;
								c.mousePositionOnCanvas = clickedPos;
								c.filter = port.portData.GetFilter();
								if(c.IsValidPort(port.owner.targetNode, commandData)) {
									evt.menu.AppendAction(c.name, (e) => {
										c.OnClick(port.owner.targetNode, commandData, position);
										MarkRepaint(port.owner.targetNode);
										// c.graph.Refresh();
									}, DropdownMenuAction.AlwaysEnabled);
								}
							}
						}
						bool hasConnection = false;
						bool hasProxy = false;
						bool hasUnproxy = false;
						foreach(var edge in port.GetValidEdges()) {
							if(edge.connection != null && edge.connection.isValid) {
								hasConnection = true;
								if(edge.connection.isProxy) {
									hasProxy = true;
								}
								else {
									hasUnproxy = true;
								}
							}
						}
						if(hasConnection) {
							evt.menu.AppendSeparator("");
							if(hasProxy) {
								evt.menu.AppendAction("Unproxy All", (act) => {
									Undo.SetCurrentGroupName("Unproxy All");
									port.owner.RegisterUndo();
									foreach(var edge in port.GetValidEdges()) {
										if(edge.connection != null && edge.connection.isValid) {
											edge.connection.isProxy = false;
										}
									}
									graph.Refresh();
								}, DropdownMenuAction.AlwaysEnabled);
							}
							if(hasUnproxy) {
								evt.menu.AppendAction("Proxy All", (e) => {
									Undo.SetCurrentGroupName("Proxy All");
									port.owner.RegisterUndo();
									foreach(var edge in port.GetValidEdges()) {
										if(edge.connection != null && edge.connection.isValid) {
											edge.connection.isProxy = true;
											MarkRepaint(edge.connection.Input.node);
										}
									}
									MarkRepaint(port.owner);
								}, DropdownMenuAction.AlwaysEnabled);
							}
							evt.menu.AppendAction("Disconnect All", (e) => {
								Undo.SetCurrentGroupName("Disconnect All");
								port.owner.RegisterUndo();
								foreach(var edge in port.GetValidEdges()) {
									if(edge.connection != null && edge.connection.isValid) {
										var input = edge.connection.Input as ValueInput;
										input.AssignToDefault(MemberData.None);
										MarkRepaint(input.node);
									}
								}
								MarkRepaint(port.owner);
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}
				}
				else {//Flow port
					if(port.direction == Direction.Output) {
						var flowOutput = port.GetPortValue<FlowOutput>();
						if(flowOutput != null) {
							Node node = port.owner.targetNode as Node;
							if(node != null) {
								var commands = NodeEditorUtility.FindPortCommands();
								if(commands != null && commands.Count > 0) {
									PortCommandData commandData = new PortCommandData() {
										portName = port.GetName(),
										port = port.GetPortValue(),
										portKind = PortKind.FlowOutput,
									};
									foreach(var c in commands) {
										c.graph = NodeGraph.openedGraph;
										c.mousePositionOnCanvas = clickedPos;
										if(c.IsValidPort(node, commandData)) {
											evt.menu.AppendAction(c.name, (e) => {
												c.OnClick(port.owner.targetNode, commandData, position);
												MarkRepaint(port.owner.targetNode);
												// c.graph.Refresh();
											}, DropdownMenuAction.AlwaysEnabled);
										}
									}
								}
							}
						}
						evt.menu.AppendSeparator();
						if(port.IsProxy()) {
							evt.menu.AppendAction("Unproxy", (act) => {
								port.owner.RegisterUndo("Unproxy");
								foreach(var edge in port.GetEdges()) {
									if(edge.isValid && edge.connection != null && edge.connection.isValid) {
										edge.connection.isProxy = false;
									}
								}
								graph.Refresh();
							}, DropdownMenuAction.AlwaysEnabled);
						}
						evt.menu.AppendAction("Reset", (e) => {
							port.owner.RegisterUndo("Reset");
							port.ResetPortValue();
							foreach(var n in port.GetConnectedNodes()) {
								n?.MarkRepaint();
							}
						}, DropdownMenuAction.AlwaysEnabled);
					}
					else {
						//var commands = NodeEditorUtility.FindPortCommands();
						//if(commands != null && commands.Count > 0) {
						//	PortCommandData commandData = new PortCommandData() {
						//		portType = port.portType,
						//		portName = port.GetName(),
						//		getConnection = port.portData.GetConnection,
						//		portKind = PortKind.FlowInput,
						//	};
						//	foreach(var c in commands) {
						//		c.graph = NodeGraph.openedGraph;
						//		c.mousePositionOnCanvas = clickedPos;
						//		c.filter = port.portData.GetFilter();
						//		if(c.IsValidPort(port.owner.targetNode, commandData)) {
						//			evt.menu.AppendAction(c.name, (e) => {
						//				c.OnClick(port.owner.targetNode, commandData, position);
						//				MarkRepaint(port.owner.targetNode);
						//				// c.graph.Refresh();
						//			}, DropdownMenuAction.AlwaysEnabled);
						//		}
						//	}
						//}
						var edges = port.GetEdges();
						if(edges != null && edges.Count > 0 && edges.Any(e => e.enabledSelf)) {
							evt.menu.AppendAction("Disconnect all", (e) => {
								port.owner.RegisterUndo("Disconnect all");
								foreach(var edge in edges) {
									if(!edge.isValid)
										continue;
									var inPort = edge.input as PortView;
									var outPort = edge.output as PortView;
									if(inPort != null && outPort != null) {
										if(inPort.isValue) {
											inPort.ResetPortValue();
										}
										else if(outPort.isFlow) {
											outPort.ResetPortValue();
										}
										inPort.owner.MarkRepaint();
										outPort.owner.MarkRepaint();
									}
								}
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}
				}
			}
			#endregion
		}

		void KeyDownCallback(KeyDownEvent e) {
			if(e.keyCode == KeyCode.S) {
				// e.StopPropagation();
			}
			else if(e.altKey && e.keyCode == KeyCode.Space) {
				graph.window.maximized = !graph.window.maximized;
			}
		}

		//This will ensure after the node removed, the conencted nodes will be refreshed.
		void OnNodeRemoved(NodeView view) {
			if(view is UNodeView) {
				var node = view as UNodeView;
				foreach(var p in node.inputPorts) {
					if(p == null)
						continue;
					var connections = p.GetConnectedNodes();
					if(connections.Count > 0) {
						MarkRepaint(connections.ToArray());
					}
					foreach(var edge in p.GetEdges()) {
						if(edge.isValid || edge.isGhostEdge) {
							edge.Disconnect();
						}
					}
				}
				foreach(var p in node.outputPorts) {
					if(p == null)
						continue;
					var connections = p.GetConnectedNodes();
					if(connections.Count > 0) {
						MarkRepaint(connections.ToArray());
					}
					foreach(var edge in p.GetEdges()) {
						if(edge.isValid || edge.isGhostEdge) {
							edge.Disconnect();
						}
					}
				}
				if(node.ownerBlock != null) {
					(node.ownerBlock as UNodeView).MarkRepaint();
				}
				node.RemoveFromHierarchy();
				if(nodeViews.Contains(node)) {
					nodeViews.Remove(node);
				}
				nodeViewsPerNode.Remove(node.nodeObject);
			}
			//else if(view is BlockView) {
			//	var block = view as BlockView;
			//	block.ownerNode.MarkRepaint();
			//	foreach(var p in block.portViews) {
			//		if(p == null)
			//			continue;
			//		var connections = p.GetConnectedNodes();
			//		if(connections.Count > 0) {
			//			MarkRepaint(connections.ToArray());
			//		}
			//	}
			//}
		}

		void OnCreateNode(Vector2 screenMousePosition) {
			graph.Repaint();
			Vector2 point = graph.window.rootVisualElement.ChangeCoordinatesTo(
				contentViewContainer,
				screenMousePosition);

			INodeBlock blockView = nodeViews.Select(view => view as INodeBlock).FirstOrDefault(view => view != null && view.nodeView.GetPosition().Contains(point));
			if(blockView != null) {
				switch(blockView.blockType) {
					case BlockType.Action:
					case BlockType.CoroutineAction:
						graph.ShowNodeMenu(screenMousePosition, onAddNode: node => {
							node.nodeObject.SetParent(blockView.blocks);
							uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode, UIChangeType.Important);
						}, nodeFilter: NodeFilter.FlowInput);
						break;
					case BlockType.Condition:
						graph.ShowNodeMenu(screenMousePosition, new FilterAttribute(typeof(object)), onAddNode: node => {
							if(node is not MultipurposeNode && node.ReturnType() == typeof(bool)) {
								node.nodeObject.SetParent(blockView.blocks);
							}
							else {
								var nodeType = node.ReturnType();
								node.position = blockView.nodeView.GetPosition();
								node.nodeObject.position.x -= 200;

								GenericMenu menu = new GenericMenu();
								menu.AddItem(new GUIContent("Is Equal"), false, () => {
									NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(blockView.blocks, Vector2.zero, nod => {
										nod.inputType = nodeType;
										nod.inputA.ConnectTo(node.nodeObject.primaryValueOutput);
										nod.inputB.AssignToDefault(MemberData.CreateFromValue(null, nod.inputType));
									});
								});
								menu.AddItem(new GUIContent("Is Not Equal"), false, () => {
									NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(blockView.blocks, Vector2.zero, nod => {
										nod.inputType = nodeType;
										nod.operatorKind = ComparisonType.NotEqual;
										nod.inputA.ConnectTo(node.nodeObject.primaryValueOutput);
										nod.inputB.AssignToDefault(MemberData.CreateFromValue(null, nod.inputType));
									});
								});
								menu.AddSeparator("");
								menu.AddItem(new GUIContent("Is Value True"), false, () => {
									NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(blockView.blocks, Vector2.zero, nod => {
										node.nodeObject.SetParent(blockView.blocks);
									});
								});
								menu.AddItem(new GUIContent("Is Type Equal"), false, () => {
									NodeEditorUtility.AddNewNode<Nodes.ISNode>(blockView.blocks, Vector2.zero, nod => {
										nod.target.ConnectTo(node.nodeObject.primaryValueOutput);
									}
									);
								});
								menu.AddSeparator("");
								if(uNodeEditorUtility.IsNumericType(nodeType)) {
									menu.AddItem(new GUIContent("Is Greater Than"), false, () => {
										NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(blockView.blocks, Vector2.zero, nod => {
											nod.inputType = nodeType;
											nod.operatorKind = ComparisonType.GreaterThan;
											nod.inputA.ConnectTo(node.nodeObject.primaryValueOutput);
											nod.inputB.AssignToDefault(MemberData.CreateFromValue(null, nod.inputType));
										});
									});
									menu.AddItem(new GUIContent("Is Greater Than Or Equal"), false, () => {
										NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(blockView.blocks, Vector2.zero, nod => {
											nod.inputType = nodeType;
											nod.operatorKind = ComparisonType.GreaterThanOrEqual;
											nod.inputA.ConnectTo(node.nodeObject.primaryValueOutput);
											nod.inputB.AssignToDefault(MemberData.CreateFromValue(null, nod.inputType));
										});
									});
									menu.AddItem(new GUIContent("Is Less Than"), false, () => {
										NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(blockView.blocks, Vector2.zero, nod => {
											nod.inputType = nodeType;
											nod.operatorKind = ComparisonType.LessThan;
											nod.inputA.ConnectTo(node.nodeObject.primaryValueOutput);
											nod.inputB.AssignToDefault(MemberData.CreateFromValue(null, nod.inputType));
										});
									});
									menu.AddItem(new GUIContent("Is Less Than Or Equal"), false, () => {
										NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(blockView.blocks, Vector2.zero, nod => {
											nod.inputType = nodeType;
											nod.operatorKind = ComparisonType.LessThanOrEqual;
											nod.inputA.ConnectTo(node.nodeObject.primaryValueOutput);
											nod.inputB.AssignToDefault(MemberData.CreateFromValue(null, nod.inputType));
										});
									});
								}
								menu.ShowAsContext();
							}
							uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode, UIChangeType.Important);
						}, nodeFilter: NodeFilter.ValueInput);
						break;
				}
			}
			else {
				var nodeView = nodeViews.Where(view => !(view is RegionNodeView)).FirstOrDefault(view => view != null && view.GetPosition().Contains(point));
				if(nodeView == null && graphData.canAddNode) {
					graph.ShowNodeMenu(point);
				}
			}
		}

		public bool HandleShortcut(GraphShortcutType type) {
			var screenMousePosition = Event.current.mousePosition;
			screenMousePosition.y -= 20;
			graph.topMousePos = screenMousePosition;

			if(type == GraphShortcutType.AddNode) {
				if(graphData.canAddNode) {
					if(ContainsPoint(window.rootVisualElement.ChangeCoordinatesTo(this, screenMousePosition))) {
						OnCreateNode(screenMousePosition);
					}
				}
				return true;
			}
			else if(type == GraphShortcutType.OpenCommand) {
				if(graphData.canAddNode) {
					if(ContainsPoint(window.rootVisualElement.ChangeCoordinatesTo(this, screenMousePosition))) {
						Vector2 point = graph.window.rootVisualElement.ChangeCoordinatesTo(
							contentViewContainer,
							screenMousePosition);

						IEnumerable<string> namespaces = null;
						if(graphData.graph != null) {
							namespaces = graphData.graph.GetUsingNamespaces();
						}
						AutoCompleteWindow.CreateWindow(Vector2.zero, (items) => {
							var nodes = CompletionEvaluator.CompletionsToGraphs(CompletionEvaluator.SimplifyCompletions(items), graphData, point);
							if(nodes != null && nodes.Count > 0) {
								graph.Refresh();
								return true;
							}
							return false;
						}, new CompletionEvaluator.CompletionSetting() {
							owner = graphData.currentCanvas,
							namespaces = namespaces,
							allowExpression = true,
							allowStatement = true,
							allowSymbolKeyword = true,
						}).ChangePosition(graph.GetMenuPosition());
					}
				}
				return true;
			}
			else if(type == GraphShortcutType.FrameGraph) {
				FrameSelection();
				return true;
			}
			else if(type == GraphShortcutType.CopySelectedNodes) {
				CopySelectedNodes();
				return true;
			}
			else if(type == GraphShortcutType.CutSelectedNodes) {
				CutSelectedNodes();
				return true;
			}
			else if(type == GraphShortcutType.PasteNodesClean) {
				if(graphData.canAddNode) {
					uNodeEditorUtility.RegisterUndo(graphData.owner, "Paste nodes");
					var clickedPos = GetMousePosition(graph.topMousePos);
					graph.PasteNode(clickedPos, true);
					graph.Refresh();
				}
				return true;
			}
			else if(type == GraphShortcutType.PasteNodesWithLink) {
				if(graphData.canAddNode) {
					uNodeEditorUtility.RegisterUndo(graphData.owner, "Paste nodes");
					var clickedPos = GetMousePosition(graph.topMousePos);
					graph.PasteNode(clickedPos, false);
					graph.Refresh();
				}
				return true;
			}
			else if(type == GraphShortcutType.DuplicateNodes) {
				if(graphData.canAddNode) {
					uNodeEditorUtility.RegisterUndo(graphData.owner, "Duplicate nodes");
					CopySelectedNodes();
					graph.Repaint();
					var clickedPos = GetMousePosition(graph.topMousePos);
					graph.PasteNode(clickedPos);
					graph.Refresh();
				}
				return true;
			}
			else if(type == GraphShortcutType.DeleteSelectedNodes) {
				DeleteSelectionCallback(AskUser.DontAskUser);
				return true;
			}
			else if(type == GraphShortcutType.SelectAllNodes) {
				ClearSelection();
				foreach(var view in nodeViews) {
					AddToSelection(view);
				}
				return true;
			}
			else if(type == GraphShortcutType.CreateRegion) {
				if(graphData.canAddNode) {
					SelectionAddRegion(screenMousePosition);
				}
				return true;
			}
			else if(type == GraphShortcutType.PlaceFitNodes) {
				if(ContainsPoint(window.rootVisualElement.ChangeCoordinatesTo(this, screenMousePosition))) {
					if(graphData.selectedCount == 1) {
						var selected = graphData.selecteds.First() as NodeObject;
						if(selected != null) {
							if(nodeViewsPerNode.TryGetValue(selected, out var view)) {
								UIElementUtility.PlaceFitNodes(view);
							}
						}
					}
					return true;
				}

			}
			else if(type == GraphShortcutType.Rename) {
				if(graphData.selectedCount == 1) {
					var selected = graphData.selecteds.First();
					if(selected is NodeObject nodeObject) {
						ActionPopupWindow.ShowWindow(Vector2.zero, nodeObject.name,
							(ref object obj) => {
								object str = EditorGUILayout.TextField(obj as string);
								if(obj != str) {
									obj = str;
									nodeObject.name = obj as string;
									uNodeGUIUtility.GUIChanged(nodeObject, UIChangeType.Average);
								}
							}).ChangePosition(graph.GetMenuPosition()).headerName = "Rename title";
					}
				}
			}
			return false;
		}

		public override Rect CalculateRectToFitAll(VisualElement container) {
			Rect rectToFit = container.layout;
			bool reachedFirstChild = false;
			foreach(var node in nodeViews) {
				if(node.isHidden) {
					if(!reachedFirstChild) {
						rectToFit = node.hidingRect;
						reachedFirstChild = true;
					}
					else {
						rectToFit = RectUtils.Encompass(rectToFit, node.hidingRect);
					}
				}
				else {
					if(!reachedFirstChild) {
						rectToFit = node.ChangeCoordinatesTo(contentViewContainer, new Rect(0, 0, node.layout.width, node.layout.height));
						reachedFirstChild = true;
					}
					else {
						rectToFit = RectUtils.Encompass(rectToFit, node.ChangeCoordinatesTo(contentViewContainer, new Rect(0, 0, node.layout.width, node.layout.height)));
					}
				}
			}
			//graphElements.ForEach(delegate (GraphElement ge) {
			//	if(ge is NodeView) {
			//		if(!reachedFirstChild) {
			//			rectToFit = ge.ChangeCoordinatesTo(contentViewContainer, new Rect(0, 0, ge.layout.width, ge.layout.height));
			//			reachedFirstChild = true;
			//		} else {
			//			rectToFit = RectUtils.Encompass(rectToFit, ge.ChangeCoordinatesTo(contentViewContainer, new Rect(0, 0, ge.layout.width, ge.layout.height)));
			//		}
			//	}
			//});
			return rectToFit;
		}

		public Rect CalculateVisibleGraphRect() {
			Rect contentRect = contentViewContainer.layout;
			bool reachedFirstChild = false;
			nodeViews.ForEach((node) => {
				Rect nodeRect = node.ChangeCoordinatesTo(contentViewContainer, new Rect(0, 0, node.layout.width, node.layout.height));
				nodeRect.xMin -= 10;
				nodeRect.xMax += 10;
				nodeRect.yMin -= 20;
				nodeRect.yMax += 20;
				if(node.inputPorts.Count > 0) {
					float inputWidth = 0;
					foreach(var p in node.inputPorts) {
						if(p != null && p.isValue && !p.connected) {
							inputWidth = Mathf.Max(p.contentContainer.layout.width, inputWidth);
							break;
						}
					}
					nodeRect.xMin -= inputWidth;
				}
				if(!reachedFirstChild) {
					contentRect = nodeRect;
					reachedFirstChild = true;
				}
				else {
					contentRect = RectUtils.Encompass(contentRect, nodeRect);
				}
			});
			return contentRect;
		}
		#endregion
	}
}