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
	public partial class UGraphView : GraphView {
		#region Fields
		public UIElementGraph graphEditor;
		public MinimapView miniMap;

		public List<UNodeView> nodeViews = new List<UNodeView>();
		public Dictionary<NodeObject, UNodeView> nodeViewsPerNode = new Dictionary<NodeObject, UNodeView>();
		public List<EdgeView> edgeViews = new List<EdgeView>();

		public Dictionary<NodeObject, UNodeView> cachedNodeMap = new Dictionary<NodeObject, UNodeView>();

		/// <summary>
		/// The editor data of the graph
		/// </summary>
		public GraphEditorData graphData => graphEditor.graphData;
		/// <summary>
		/// The graph layout
		/// </summary>
		public GraphLayout graphLayout { get; private set; }
		/// <summary>
		/// The editor window
		/// </summary>
		public uNodeEditor window => graphEditor.window;
		/// <summary>
		/// The current zoom scale
		/// </summary>
		public float zoomScale => graphEditor.zoomScale;
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
			graphViewChanged = OnGraphViewChanged;
			viewTransformChanged = OnViewTransformChanged;
			elementResized = OnElementResized;
			//unserializeAndPaste += (op, data) => {
			//	if(op == "Paste") {
			//		graph.Repaint();
			//		var clickedPos = GetMousePosition(graph.topMousePos);
			//		graph.PasteNode(clickedPos);
			//		graph.Refresh();
			//	}
			//};

			InitializeManipulators();
			RegisterCallback<KeyDownEvent>(OnKeyDown);
			RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
			RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
			this.RegisterRepaintAction(() => {
				if(autoHideNodes && _viewPosition != contentViewContainer.resolvedStyle.translate && uNodeThreadUtility.frame % 2 == 0) {
					_viewPosition = contentViewContainer.resolvedStyle.translate;
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
						//if(generic is UnityEngine.Object && uNodeEditorUtility.IsSceneObject(generic as UnityEngine.Object)) {
						//	DragAndDrop.visualMode = DragAndDropVisualMode.None;
						//	return;
						//}
					}
					//else if(DragAndDrop.objectReferences.Length > 0) {
					//	if(uNodeEditorUtility.IsSceneObject(DragAndDrop.objectReferences[0])) {
					//		DragAndDrop.visualMode = DragAndDropVisualMode.None;
					//		return;
					//	}
					//}
				}
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
		}

		#region Drag Handler
		private void DragHandleObject(Object obj, Vector2 position, Vector2 menuPosition) {
			if(obj is GraphAsset graphAsset) {
				//Create Liked macro from dragable macros.
				if(graphAsset is MacroGraph) {
					var macro = graphAsset as MacroGraph;
					graphEditor.CreateLinkedMacro(macro, position);
					return;
				}
				if(graphAsset is IReflectionType) {
					DragHandleType(ReflectionUtils.GetRuntimeType(graphAsset), position, menuPosition);
					return;
				}
			}
			GenericMenu menu = new GenericMenu();

			var dragData = new DragHandlerDataForGraphElement() {
				draggedValue = obj,
				droppedTarget = graphData.currentCanvas,
				graphEditor = graphEditor,
				mousePositionOnCanvas = position,
				mousePositionOnScreen = menuPosition,
			};
			DragHandlerMenu.Instances.ForEach(handler => {
				if(handler.IsValid(dragData)) {
					menu.AppendMenu(handler.GetMenuItems(dragData));
				}
			});

			if(menu.GetItemCount() == 0) {
				if(!(graphData.graph is IIndependentGraph)) {
					EditorUtility.DisplayDialog("Error", "The c# graph cannot reference project and scene object.", "Ok");
					return;
				}
				else if(!EditorUtility.IsPersistent(obj) && !uNodeEditorUtility.IsSceneObject(graphData.owner)) {
					EditorUtility.DisplayDialog("Error", "The project graph cannot reference scene object.", "Ok");
					return;
				}
				else if(graphData.graph.GetGraphType().IsSubclassOf(typeof(UnityEngine.Object)) == false) {
					EditorUtility.DisplayDialog("Error", "The graph that's not inherited from UnityEngine.Object cannot reference project and scene object.", "Ok");
					return;
				}
			}

			menu.ShowAsContext();
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
				graphEditor.Refresh();
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
				graphEditor.Refresh();
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
					graphEditor.Refresh();
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
					graphEditor.Refresh();
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
					graphEditor.Refresh();
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
				graphEditor.Refresh();
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
					graphEditor.Refresh();
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
				graphEditor.Refresh();
			});
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(NodeMenu menu, Vector2 position) {
			NodeEditorUtility.AddNewNode(graphData, menu.nodeName, menu.type, position, (Node node) => {
				graphEditor.Refresh();
			});
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(INodeItemCommand command, Vector2 position) {
			command.graph = graphEditor;
			command.Setup(position);
			DragAndDrop.SetGenericData("uNode", null);
		}
		#endregion

		private void OnDragPerformEvent(DragPerformEvent evt) {
			if(graphData.CanAddNode == false) {
				return;
			}
			Vector2 mPos = GetMousePosition(evt, out var topMPos);
			var iPOS = graphEditor.window.GetMousePositionForMenu(topMPos);
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

				#region Element
				if(generic is Node) {
					generic = (generic as Node).nodeObject;
				}
				if(generic is UGraphElement) {
					var element = generic as UGraphElement;
					if(element.graphContainer != graphData.graph) {
						EditorUtility.DisplayDialog("Error", $"The graph of the {element.GetType()} must same with the current graph", "Ok");
						return;
					}
					GenericMenu menu = new GenericMenu();
					var dragData = new DragHandlerDataForGraphElement() {
						draggedValue = element,
						droppedTarget = graphData.currentCanvas,
						graphEditor = graphEditor,
						mousePositionOnCanvas = mPos,
						mousePositionOnScreen = iPOS,
					};
					DragHandlerMenu.Instances.ForEach(handler => {
						if(handler.IsValid(dragData)) {
							menu.AppendMenu(handler.GetMenuItems(dragData));
						}
					});
					menu.ShowAsContext();
					DragAndDrop.SetGenericData("uNode", null);
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
				DragHandleObject(dragObject, mPos, iPOS);
			}
			else if(DragAndDrop.objectReferences.Length > 1) {
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
								graphEditor.Refresh();
							}, category));
							ItemSelector w = ItemSelector.ShowWindow(/*editorData.selectedGroup ?? editorData.selectedRoot as UnityEngine.Object ?? editorData.graph*/ null, filter, delegate (MemberData value) {
								value.instance = new MemberData(dOBJ, MemberData.TargetType.Values);
								NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, null, null, mPos, delegate (MultipurposeNode n) {
									n.target = value;
								});
								graphEditor.Refresh();
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
								graphEditor.Refresh();
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
			miniMap?.SetDirty();
			var processor = GraphProcessor;
			var list = new List<ISelectable>();
			list.AddRange(selection.Distinct());
			foreach(var p in processor) {
				if(p.Delete(list)) {
					selection.Clear();
					graphEditor.Refresh();
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
				if(n is not NodeObject node || node.node is TransitionEvent)
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

		public void AddToSelection(IEnumerable<ISelectable> selectables) {
			if(selectables.Any(s => s is BaseNodeView)) {
				foreach(var selected in selectables) {
					if(selected is BaseNodeView) {
						AddToSelection(selected);
					}
				}
			}
			else {
				foreach(var selected in selectables) {
					if(selected != null) {
						AddToSelection(selected);
						break;
					}
				}
			}
		}

		public override void AddToSelection(ISelectable selectable) {
			base.AddToSelection(selectable);
			uNodeThreadUtility.ExecuteOnce(() => {
				bool onlyNodes = this.selection.Count > 1 && this.selection.Any(s => s is BaseNodeView);
				graphEditor.ClearSelection();
				foreach(var selectable in this.selection.ToArray()) {
					if(onlyNodes) {
						if(selectable is not BaseNodeView) {
							base.RemoveFromSelection(selectable);
							continue;
						}
					}
					if(selectable is BaseNodeView) {
						graphEditor.SelectNode((selectable as BaseNodeView).nodeObject, false);
						AutoHideGraphElement.RegisterNodeToIgnore(selectable as NodeView);
					}
					else if(selectable is EdgeView) {
						var edge = (selectable as EdgeView);
						if(graphData.selectedNodes.Any() == false) {
							if(edge.isFlow) {
								if(edge.Output != null) {
									graphEditor.Select(new UPortRef(edge.Output.GetPortValue()));
									return;
								}
							}
							else {
								if(edge.Input != null) {
									graphEditor.Select(new UPortRef(edge.Input.GetPortValue()));
								}
							}
							graphEditor.Select(new UPortRef(edge.Input?.GetPortValue() ?? edge.Output?.GetPortValue()));
						}
					}
					else if(selectable is UNodeView) {
						graphEditor.Select((selectable as UNodeView).GetSelectableObject());
						AutoHideGraphElement.RegisterNodeToIgnore(selectable as NodeView);
					}
				}
			}, "[GRAPH_ADD_SELECTIONS]");
		}

		public override void RemoveFromSelection(ISelectable selectable) {
			base.RemoveFromSelection(selectable);
			if(selectable is BaseNodeView) {
				graphEditor.Unselect((selectable as BaseNodeView).nodeObject);
				AutoHideGraphElement.UnregisterNodeToIgnore(selectable as NodeView);
			}
		}

		public override void ClearSelection() {
			base.ClearSelection();
			graphEditor.ClearSelection();
		}

		GraphViewChange OnGraphViewChanged(GraphViewChange changes) {
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

		private float lastZoomLevel = 1f;
		void OnViewTransformChanged(GraphView view) {
			gridBackground?.MarkDirtyRepaint();
			if(graphEditor != null && hasInitialize) {
				graphData.GetCurrentCanvasData().zoomScale = scale;
				graphData.position = -contentViewContainer.resolvedStyle.translate / scale;
			}
			if(Mathf.Abs(scale - lastZoomLevel) > 0.01f) {
				if(gridBackground != null) {
					if(scale >= 0.5f) {
						gridBackground.style.display = StyleKeyword.Null;
					}
					else {
						gridBackground.style.display = DisplayStyle.None;
					}
				}

				foreach(var node in nodes) {
					if(node is UNodeView v)
						v.OnZoomUpdated(scale);
				}
				lastZoomLevel = scale;
			}
			miniMap?.SetDirty();
		}

		void OnElementResized(VisualElement elem) {

		}

		public override List<GPort> GetCompatiblePorts(GPort startPort, NodeAdapter nodeAdapter) {
			var compatiblePorts = new List<GPort>();
			var startPortView = startPort as PortView;

			compatiblePorts.AddRange(ports.ToList().Select(p => p as PortView).Where(p => {
				if(p == null || !p.enabledSelf || p.direction == startPort.direction || p.isFlow != startPortView.isFlow /*|| !startPortView.IsValidTarget(p)*/)
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
			//if(evt is MouseUpEvent && graphDragger.isActive) {
			//	graphDragger.OnMouseUp(evt as MouseUpEvent);
			//	this.ReleaseMouse();
			//	return;
			//}
			//else 
			if(evt is IMouseEvent) {
				GetMousePosition(evt as IMouseEvent, out var position);
				graphEditor.topMousePos = position;
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
			var screenRect = graphEditor.window.GetMousePositionForMenu(evt.mousePosition);
			var clickedPos = GetMousePosition(evt, out var position);
			graphEditor.topMousePos = position;

			if(evt.target is RegionNodeView) {
				evt.target = this;
			}

			#region Graph
			if(evt.target is GraphView && graphData.CanAddNode) {
				var menus = GraphManipulator.GetAllMenuForGraphCanvas(graphEditor, clickedPos);
				foreach(var m in menus) {
					evt.menu.MenuItems().Add(m.menu);
				}

				#region Graph Commands
				evt.menu.AppendSeparator("");
				var commands = NodeEditorUtility.FindGraphCommands();
				if(commands != null && commands.Count > 0) {
					bool addSeparator = false;
					foreach(var c in commands) {
						c.graphEditor = graphEditor;
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
				if(graphEditor.canvasData.SupportPlaceFit) {
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
						var events = graphEditor.nodes.Where(n => n.node is BaseEventNode);
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
				}
				#endregion

				if(graphEditor.canvasData.SupportMacro && selection.Count > 0 && selection.Any(s => (s is UNodeView nodeView) && !nodeView.isBlock)) {
					evt.menu.AppendAction("Selection to macro", (e) => {
						SelectionToMacro(clickedPos);
					}, DropdownMenuAction.AlwaysEnabled);
				}

				evt.menu.AppendAction("Take Screenshot", (e) => {
					CaptureGraphScreenshot();
				}, DropdownMenuAction.AlwaysEnabled);

#if UNODE_DEV
				evt.menu.AppendAction("Export to JSON", (e) => {
					var data = SerializedGraph.Serialize(graphData.graphData, OdinSerializer.DataFormat.JSON);
					var bytes = OdinSerializer.SerializationUtility.SerializeValue(graphData.graphData, OdinSerializer.DataFormat.JSON, out _);
					Debug.Log(System.Text.Encoding.UTF8.GetString(bytes));
					uNodeEditorUtility.CopyToClipboard(System.Text.Encoding.UTF8.GetString(bytes));
				}, DropdownMenuAction.AlwaysEnabled);
#endif

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
						var pastedNodes = graphEditor.PasteNode(clickedPos);
						ClearSelection();
						graphData.ClearSelection();
						graphData.AddToSelection(pastedNodes);
						graphEditor.SelectionChanged();
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
			if(evt.target is UNodeView) {
				var nodeView = evt.target as UNodeView;
				if(nodeView.targetNode != null) {
					Node node = nodeView.targetNode;
					if(node == null)
						return;

					foreach(var proc in GraphProcessor) {
						var menus = proc.ContextMenuForNode(clickedPos, nodeView);
						if(menus != null) {
							foreach(var menu in menus) {
								if(menu == null) continue;
								evt.menu.MenuItems().Add(menu);
							}
						}
					}

					var allMenu = GraphManipulator.GetAllMenuForNode(graphEditor, clickedPos, node);
					foreach(var m in allMenu) {
						evt.menu.MenuItems().Add(m.menu);
					}

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
							graphEditor.SelectionAddRegion(clickedPos);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					#endregion

					#region Add Breakpoint
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
					#endregion

					#region Node commands
					evt.menu.AppendSeparator("");
					var commands = NodeEditorUtility.FindNodeCommands();
					if(commands != null && commands.Count > 0) {
						bool addSeparator = false;
						foreach(var c in commands) {
							c.graphEditor = graphEditor;
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


					evt.menu.AppendSeparator("");
					if(graphEditor.canvasData.SupportPlaceFit) {
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
					}
					if(selection.Count > 0 && selection.Any(s => (s is UNodeView nodeView) && !nodeView.isBlock)) {

						if(graphEditor.canvasData.SupportMacro) {
							evt.menu.AppendAction("Selection to macro", (e) => {
								SelectionToMacro(clickedPos);
							}, DropdownMenuAction.AlwaysEnabled);
						}

#if UNODE_DEV
						if(selection.Count == 1) {
							evt.menu.AppendAction("Export to JSON", (e) => {
								var bytes = OdinSerializer.SerializationUtility.SerializeValue(selection[0], OdinSerializer.DataFormat.JSON, out _);
								var str = System.Text.Encoding.UTF8.GetString(bytes);
								if(selection[0] is UNodeView view) {
									bytes = OdinSerializer.SerializationUtility.SerializeValue(view.targetNode, OdinSerializer.DataFormat.JSON, out _);
									str += "\n" + System.Text.Encoding.UTF8.GetString(bytes);
								}
								Debug.Log(str);
								uNodeEditorUtility.CopyToClipboard(str);
							}, DropdownMenuAction.AlwaysEnabled);
						}
#endif
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
						graphEditor.Refresh();
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

			#region Edge
			if(evt.target is EdgeView) {
				EdgeView edge = evt.target as EdgeView;
				if(edge is not ConversionEdgeView) {
					evt.menu.AppendAction("Convert to proxy", (e) => {
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Convert to proxy");
						edge.connection.isProxy = true;
						graphEditor.GUIChanged();
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
									c.graph = GraphEditor.openedGraph;
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
								graphEditor.Refresh();
							}, DropdownMenuAction.AlwaysEnabled);
						}
						evt.menu.AppendAction("Reset", (e) => {
							Undo.SetCurrentGroupName("Reset");
							port.owner.RegisterUndo();
							if(p.IsOptional) {
								p.AssignToDefault(MemberData.None);
							}
							else {
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
								p.AssignToDefault(val);
							}
							MarkRepaint(port.owner);
							foreach(var edge in port.GetValidEdges()) {
								if(edge.connection.isValid) {
									MarkRepaint(edge.connection.Output.node);
								}
							}
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
								c.graph = GraphEditor.openedGraph;
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
									graphEditor.Refresh();
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
										c.graph = GraphEditor.openedGraph;
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
								graphEditor.Refresh();
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

		void OnKeyDown(KeyDownEvent e) {
			if(e.keyCode == KeyCode.S) {
				// e.StopPropagation();
			}
			else if(e.altKey && e.keyCode == KeyCode.Space) {
				graphEditor.window.maximized = !graphEditor.window.maximized;
			}
		}

		//This will ensure after the node removed, the conencted nodes will be refreshed.
		void OnNodeRemoved(NodeView view) {
			if(view is UNodeView) {
				var node = view as UNodeView;
				try {
					node.OnNodeRemoved();
				}
				catch(Exception ex) {
					Debug.LogException(ex);
				}
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
			graphEditor.Repaint();
			Vector2 point = graphEditor.window.rootVisualElement.ChangeCoordinatesTo(
				contentViewContainer,
				screenMousePosition);

			INodeBlock blockView = nodeViews.Select(view => view as INodeBlock).FirstOrDefault(view => view != null && view.nodeView.GetPosition().Contains(point));
			if(blockView != null) {
				switch(blockView.blockType) {
					case BlockType.Action:
					case BlockType.CoroutineAction:
						graphEditor.ShowNodeMenu(screenMousePosition, onAddNode: node => {
							node.nodeObject.SetParent(blockView.blocks);
							uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode, UIChangeType.Important);
						}, nodeFilter: NodeFilter.FlowInput);
						break;
					case BlockType.Condition:
						graphEditor.ShowNodeMenu(screenMousePosition, new FilterAttribute(typeof(object)), onAddNode: node => {
							if(node is not MultipurposeNode && node.nodeObject.ReturnType() == typeof(bool)) {
								node.nodeObject.SetParent(blockView.blocks);
							}
							else {
								var nodeType = node.nodeObject.ReturnType();
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
				if(GraphManipulatorUtility.HandleCommand(graphEditor, nameof(GraphManipulator.Command.OpenItemSelector))) {
					return;
				}

				var nodeView = nodeViews.Where(view => !(view is RegionNodeView)).FirstOrDefault(view => view != null && view.GetPosition().Contains(point));
				if(nodeView == null && graphData.CanAddNode) {
					graphEditor.ShowNodeMenu(point);
				}
			}
		}

		public bool HandleShortcut(GraphShortcutType type) {
			var screenMousePosition = Event.current.mousePosition;
			screenMousePosition.y -= 20;
			graphEditor.topMousePos = screenMousePosition;

			Vector2 mousePosition = graphEditor.window.rootVisualElement.ChangeCoordinatesTo(
							contentViewContainer,
							screenMousePosition);

			if(type == GraphShortcutType.AddNode) {
				if(graphData.CanAddNode) {
					if(ContainsPoint(window.rootVisualElement.ChangeCoordinatesTo(this, screenMousePosition))) {
						OnCreateNode(screenMousePosition);
					}
				}
				return true;
			}
			else if(type == GraphShortcutType.AddNodeFromFavorites) {
				if(graphData.CanAddNode) {
					if(ContainsPoint(window.rootVisualElement.ChangeCoordinatesTo(this, screenMousePosition))) {
						graphEditor.ShowFavoriteMenu(mousePosition);
					}
				}
				return true;
			}
			else if(type == GraphShortcutType.OpenCommand) {
				if(graphData.CanAddNode) {
					if(GraphManipulatorUtility.HandleCommand(graphEditor, nameof(GraphManipulator.Command.OpenCommand))) {
						return true;
					}
					if(ContainsPoint(window.rootVisualElement.ChangeCoordinatesTo(this, screenMousePosition))) {
						IEnumerable<string> namespaces = null;
						if(graphData.graph != null) {
							namespaces = graphData.graph.GetUsingNamespaces();
						}
						AutoCompleteWindow.CreateWindow(Vector2.zero, (items) => {
							var nodes = CompletionEvaluator.CompletionsToGraphs(CompletionEvaluator.SimplifyCompletions(items), graphData, mousePosition);
							if(nodes != null && nodes.Count > 0) {
								graphEditor.Refresh();
								return true;
							}
							return false;
						}, new CompletionEvaluator.CompletionSetting() {
							owner = graphData.currentCanvas,
							namespaces = namespaces,
							allowExpression = true,
							allowStatement = true,
							allowSymbolKeyword = true,
						}).ChangePosition(graphEditor.GetMenuPosition());
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
				if(GraphManipulatorUtility.HandleCommand(graphEditor, nameof(GraphManipulator.Command.Paste))) {
					return true;
				}
				if(graphData.CanAddNode) {
					uNodeEditorUtility.RegisterUndo(graphData.owner, "Paste nodes");
					var clickedPos = GetMousePosition(graphEditor.topMousePos);
					var pastedNodes = graphEditor.PasteNode(clickedPos, true);
					ClearSelection();
					graphData.ClearSelection();
					graphData.AddToSelection(pastedNodes);
					graphEditor.SelectionChanged();
				}
				return true;
			}
			else if(type == GraphShortcutType.PasteNodesWithLink) {
				if(GraphManipulatorUtility.HandleCommand(graphEditor, nameof(GraphManipulator.Command.PasteWithLink))) {
					return true;
				}
				if(graphData.CanAddNode) {
					uNodeEditorUtility.RegisterUndo(graphData.owner, "Paste nodes");
					var clickedPos = GetMousePosition(graphEditor.topMousePos);
					var pastedNodes = graphEditor.PasteNode(clickedPos, false);
					ClearSelection();
					graphData.ClearSelection();
					graphData.AddToSelection(pastedNodes);
					graphEditor.SelectionChanged();
				}
				return true;
			}
			else if(type == GraphShortcutType.DuplicateNodes) {
				if(graphData.CanAddNode) {
					uNodeEditorUtility.RegisterUndo(graphData.owner, "Duplicate nodes");
					CopySelectedNodes();
					graphEditor.Repaint();
					var clickedPos = GetMousePosition(graphEditor.topMousePos);
					graphEditor.PasteNode(clickedPos);
					graphEditor.Refresh();
				}
				return true;
			}
			else if(type == GraphShortcutType.DeleteSelectedNodes) {
				DeleteSelectionCallback(AskUser.DontAskUser);
				return true;
			}
			else if(type == GraphShortcutType.SelectAllNodes) {
				ClearSelection();
				AddToSelection(nodeViews.Select(view => view as ISelectable));
				return true;
			}
			else if(type == GraphShortcutType.CreateRegion) {
				if(graphData.CanAddNode) {
					graphEditor.SelectionAddRegion(mousePosition);
				}
				return true;
			}
			else if(type == GraphShortcutType.PlaceFitNodes) {
				if(graphEditor.canvasData.SupportPlaceFit) {
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

			}
			else if(type == GraphShortcutType.Rename) {
				if(graphData.selectedCount == 1) {
					var selected = graphData.selecteds.First();
					if(selected is NodeObject nodeObject) {
						ActionPopupWindow.Show(
							nodeObject.name,
							(ref object obj) => {
								object str = EditorGUILayout.TextField(obj as string);
								if(obj != str) {
									obj = str;
									nodeObject.name = obj as string;
									uNodeGUIUtility.GUIChanged(nodeObject, UIChangeType.Average);
								}
							}).ChangePosition(graphEditor.GetMenuPosition()).headerName = "Rename title";
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

	#region Classes
	public struct GraphDraggedData {
		public UGraphView graphView;
		public GraphEditorData graphData;
		public Vector2 mousePositionOnCanvas;
		public Vector2 mousePositionOnScreen;
	}

	class GridBackground : VisualElement {
		static readonly CustomStyleProperty<float> k_SpacingProperty = new CustomStyleProperty<float>("--spacing");
		static readonly CustomStyleProperty<int> k_ThickLinesProperty = new CustomStyleProperty<int>("--thick-lines");
		static readonly CustomStyleProperty<Color> k_LineColorProperty = new CustomStyleProperty<Color>("--line-color");
		static readonly CustomStyleProperty<Color> k_ThickLineColorProperty = new CustomStyleProperty<Color>("--thick-line-color");

		static float DefaultSpacing => 50f;
		static int DefaultThickLines => 10;

		static Color DefaultLineColor {
			get {
				if(EditorGUIUtility.isProSkin) {
					return new Color(200 / 255f, 200 / 255f, 200 / 255f, 0.05f);
				}

				return new Color(65 / 255f, 65 / 255f, 65 / 255f, 0.07f);
			}
		}

		static Color DefaultThickLineColor {
			get {
				if(EditorGUIUtility.isProSkin) {
					return new Color(200 / 255f, 200 / 255f, 200 / 255f, 0.1f);
				}

				return new Color(65 / 255f, 65 / 255f, 65 / 255f, 0.1f);
			}
		}

		/// <summary>
		/// Spacing between grid lines.
		/// </summary>
		public float Spacing { get; private set; } = DefaultSpacing;

		int ThickLines { get; set; } = DefaultThickLines;

		Color m_LineColor = DefaultLineColor;
		Color LineColor => m_LineColor;

		Color m_ThickLineColor = DefaultThickLineColor;
		Color ThickLineColor => m_ThickLineColor;

		VisualElement m_Container;

		/// <summary>
		/// Initializes a new instance of the <see cref="GridBackground"/> class.
		/// </summary>
		public GridBackground() {
			pickingMode = PickingMode.Ignore;

			this.StretchToParentSize();
			generateVisualContent += GenerateVisualContent;

			RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
		}

		Vector3 Clip(Rect clipRect, Vector3 @in) {
			if(@in.x < clipRect.xMin)
				@in.x = clipRect.xMin;
			if(@in.x > clipRect.xMax)
				@in.x = clipRect.xMax;

			if(@in.y < clipRect.yMin)
				@in.y = clipRect.yMin;
			if(@in.y > clipRect.yMax)
				@in.y = clipRect.yMax;

			return @in;
		}

		void OnCustomStyleResolved(CustomStyleResolvedEvent e) {
			var elementCustomStyle = e.customStyle;
			if(elementCustomStyle.TryGetValue(k_SpacingProperty, out var spacingValue))
				Spacing = spacingValue;

			if(elementCustomStyle.TryGetValue(k_ThickLinesProperty, out var thicklinesValue))
				ThickLines = thicklinesValue;

			if(elementCustomStyle.TryGetValue(k_ThickLineColorProperty, out var thicklineColorValue))
				m_ThickLineColor = thicklineColorValue;

			if(elementCustomStyle.TryGetValue(k_LineColorProperty, out var lineColorValue))
				m_LineColor = lineColorValue;
		}

		void GenerateVisualContent(MeshGenerationContext mgc) {
			var target = parent;

			var graphView = target as GraphView;
			if(graphView == null) {
				throw new InvalidOperationException("GridBackground can only be added to a GraphView");
			}

			m_Container = graphView.contentViewContainer;
			var clientRect = graphView.layout;

			// Since we're always stretch to parent size, we will use (0,0) as (x,y) coordinates
			clientRect.x = 0;
			clientRect.y = 0;

			var containerScale = m_Container.resolvedStyle.scale.value;
			var containerTranslation = m_Container.resolvedStyle.translate;
			var containerPosition = m_Container.layout;

			var painter = mgc.painter2D;

			void Line(Vector2 from, Vector2 to) {
				painter.MoveTo(Clip(clientRect, from));
				painter.LineTo(Clip(clientRect, to));
			}

			// vertical lines
			var from = new Vector3(clientRect.x, clientRect.y, 0.0f);
			var to = new Vector3(clientRect.x, clientRect.height, 0.0f);

			var tx = Matrix4x4.TRS(containerTranslation, Quaternion.identity, Vector3.one);

			from = tx.MultiplyPoint(from);
			to = tx.MultiplyPoint(to);

			from.x += (containerPosition.x * containerScale.x);
			from.y += (containerPosition.y * containerScale.y);
			to.x += (containerPosition.x * containerScale.x);
			to.y += (containerPosition.y * containerScale.y);

			float thickGridLineX = from.x;
			float thickGridLineY = from.y;

			// Update from/to to start at beginning of clientRect
			from.x = (from.x % (Spacing * (containerScale.x)) - (Spacing * (containerScale.x)));
			to.x = from.x;

			from.y = clientRect.y;
			to.y = clientRect.y + clientRect.height;

			painter.BeginPath();
			painter.strokeColor = LineColor;
			while(from.x < clientRect.width) {
				from.x += Spacing * containerScale.x;
				to.x += Spacing * containerScale.x;

				Line(from, to);
			}
			painter.Stroke();

			float thickLineSpacing = (Spacing * ThickLines);
			from.x = to.x = (thickGridLineX % (thickLineSpacing * (containerScale.x)) - (thickLineSpacing * (containerScale.x)));

			painter.BeginPath();
			painter.strokeColor = ThickLineColor;
			while(from.x < clientRect.width + thickLineSpacing) {
				Line(from, to);

				from.x += (Spacing * containerScale.x * ThickLines);
				to.x += (Spacing * containerScale.x * ThickLines);
			}
			painter.Stroke();

			// horizontal lines
			from = new Vector3(clientRect.x, clientRect.y, 0.0f);
			to = new Vector3(clientRect.x + clientRect.width, clientRect.y, 0.0f);

			from.x += (containerPosition.x * containerScale.x);
			from.y += (containerPosition.y * containerScale.y);
			to.x += (containerPosition.x * containerScale.x);
			to.y += (containerPosition.y * containerScale.y);

			from = tx.MultiplyPoint(from);
			to = tx.MultiplyPoint(to);

			from.y = to.y = (from.y % (Spacing * (containerScale.y)) - (Spacing * (containerScale.y)));
			from.x = clientRect.x;
			to.x = clientRect.width;

			painter.BeginPath();
			painter.strokeColor = LineColor;
			while(from.y < clientRect.height) {
				from.y += Spacing * containerScale.y;
				to.y += Spacing * containerScale.y;
				Line(from, to);
			}
			painter.Stroke();

			thickLineSpacing = Spacing * ThickLines;
			from.y = to.y = (thickGridLineY % (thickLineSpacing * (containerScale.y)) - (thickLineSpacing * (containerScale.y)));

			painter.BeginPath();
			painter.strokeColor = ThickLineColor;
			while(from.y < clientRect.height + thickLineSpacing) {
				Line(from, to);

				from.y += Spacing * containerScale.y * ThickLines;
				to.y += Spacing * containerScale.y * ThickLines;
			}
			painter.Stroke();
		}
	}
	#endregion
}