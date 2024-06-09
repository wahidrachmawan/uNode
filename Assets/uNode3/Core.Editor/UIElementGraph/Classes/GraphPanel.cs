using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MaxyGames.UNode.Editors.UI;
using System.Collections;

namespace MaxyGames.UNode.Editors {
	internal class GraphPanel : VisualElement, IDisposable {
		ScrollView scroll;
		UIElementGraph graphEditor;

		private GraphEditorData graphData {
			get {
				return graphEditor.graphData;
			}
		}
		private IGraph desiredGraph {
			get {
				var result = graphData.graph;
				while(result is IInstancedGraph instanced) {
					var graph = instanced.OriginalGraph;
					if(graph == instanced || graph == null)
						return graphData.graph;
					result = graph;
				}
				return result;
			}
		}

		private static string ussClassNameActive = "active";
		private static string ussClassNameSelected = "selected";

		private uNodeEditor.TabData tabData => graphEditor.tabData;

		VisualElement classContainer, variableContainer, propertyContainer, functionContainer, eventContainer, constructorContainer, namespaceElement, interfaceContainer, enumContainer, localContainer, nestedContainer;
		ClickableElement namespaceButton, classAddButton, functionAddButton;
		Dictionary<object, ClickableElement> contentMap = new Dictionary<object, ClickableElement>();

		[SerializeField]
		bool showClasses = true, showVariables = true, showProperties = true, showFunctions = true, showEvents = true, showConstructors = true, showInterfaces = true, showEnums = true, showLocal = true, showNested = true;

		private TreeView treeView;

		public GraphPanel(UIElementGraph graph) {
			try {
				this.graphEditor = graph;
				this.StretchToParentSize();
				this.AddStyleSheet("uNodeStyles/NativePanelStyle");
				this.AddStyleSheet(UIElementUtility.Theme.graphPanelStyle);
				scroll = new ScrollView(ScrollViewMode.Vertical) {
					name = "scroll-view",
				};
				//scroll.Add(explorer);
				scroll.StretchToParentSize();
				Add(scroll);
				InitializeView();
				uNodeEditor.onChanged += ReloadView;
				uNodeEditor.onSelectionChanged += OnSelectionChanged;
				ReloadView();

			}
			catch(Exception ex) {
				Debug.LogException(ex);
			}
		}

		void InitializeView() {
			VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>("uxml/GraphPanel");
			visualTreeAsset.CloneTree(scroll.contentContainer);
			//{//Namespace
			//	namespaceElement = new VisualElement() { name = "namespace" };
			//	namespaceElement.style.flexDirection = FlexDirection.Row;
			//	namespaceElement.Add(new Label("Namespace") { name = "label" });
			//	namespaceButton = new ClickableElement("") { name = "value" };
			//	namespaceButton.onClick = () => {
			//		//var mPos = (namespaceButton.clickedEvent.currentTarget as VisualElement).GetScreenMousePosition((namespaceButton.clickedEvent as IMouseEvent).localMousePosition, graph.window);
			//		//if (editorData.graphData == null && !(editorData.graph is IIndependentGraph)) {
			//		//	editorData.owner.AddComponent<uNodeData>();
			//		//}
			//		//OnNamespaceClicked(mPos, editorData.graphData);
			//	};
			//	namespaceElement.Add(namespaceButton);
			//	scroll.contentContainer.Insert(0, namespaceElement);
			//}
			{//Classes
				VisualElement header = scroll.Q("class");
				header.SetDisplay(false);
				classContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(MonoScript));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var plusElement = new ClickableElement("+") {
					name = "title-button-add",
				};
				plusElement.onClick = (evt) => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.GetLocalMousePosition(), graphEditor.window);
					var manipulators = NodeEditorUtility.FindGraphManipulators();
					foreach(var manipulator in manipulators) {
						manipulator.tabData = tabData;
						if(manipulator.IsValid(nameof(manipulator.CreateNewClass)) && manipulator.CreateNewClass(mPos, ReloadView)) {
							break;
						}
					}
				};
				icon.parent.Add(plusElement);
				classAddButton = plusElement;
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showClasses = evt.newValue;
					ReloadView();
				});
			}
			{//Variable
				VisualElement header = scroll.Q("variable");
				variableContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FieldIcon));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var plusElement = new ClickableElement("+") {
					name = "title-button-add",
				};
				plusElement.onClick = (evt) => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.GetLocalMousePosition(), graphEditor.window);
					var manipulators = NodeEditorUtility.FindGraphManipulators();
					foreach(var manipulator in manipulators) {
						manipulator.tabData = tabData;
						if(manipulator.IsValid(nameof(manipulator.CreateNewVariable)) && manipulator.CreateNewVariable(mPos, ReloadView)) {
							break;
						}
					}
				};
				icon.parent.Add(plusElement);
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showVariables = evt.newValue;
					ReloadView();
				});
			}
			{//Property
				VisualElement header = scroll.Q("property");
				propertyContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.PropertyIcon));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var plusElement = new ClickableElement("+") {
					name = "title-button-add",
				};
				plusElement.onClick = (evt) => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.GetLocalMousePosition(), graphEditor.window);
					var manipulators = NodeEditorUtility.FindGraphManipulators();
					foreach(var manipulator in manipulators) {
						manipulator.tabData = tabData;
						if(manipulator.IsValid(nameof(manipulator.CreateNewProperty)) && manipulator.CreateNewProperty(mPos, ReloadView)) {
							break;
						}
					}
				};
				icon.parent.Add(plusElement);
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showProperties = evt.newValue;
					ReloadView();
				});
			}
			{//Events
				VisualElement header = scroll.Q("event");
				eventContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.EventIcon));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showEvents = evt.newValue;
					ReloadView();
				});
			}
			{//Function
				VisualElement header = scroll.Q("function");
				functionContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.MethodIcon));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var plusElement = new ClickableElement("+") {
					name = "title-button-add",
				};
				plusElement.onClick = (evt) => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.GetLocalMousePosition(), graphEditor.window);
					var manipulators = NodeEditorUtility.FindGraphManipulators();
					foreach(var manipulator in manipulators) {
						manipulator.tabData = tabData;
						if(manipulator.IsValid(nameof(manipulator.CreateNewFunction)) && manipulator.CreateNewFunction(mPos, ReloadView)) {
							break;
						}
					}
				};
				functionAddButton = plusElement;
				icon.parent.Add(plusElement);
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showFunctions = evt.newValue;
					ReloadView();
				});
			}
			{//Constructor
				VisualElement header = scroll.Q("constructor");
				header.SetDisplay(false);
				constructorContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.MethodIcon));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var plusElement = new ClickableElement("+") {
					name = "title-button-add",
				};
				plusElement.onClick = (_) => {
					if(graphData.graph != null)
						CreateNewConstructor(graphData.graph.GraphData.constructorContainer);
				};
				icon.parent.Add(plusElement);
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showConstructors = evt.newValue;
					ReloadView();
				});
			}
			{//Local Variable
				VisualElement header = scroll.Q("localvariable");
				localContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.LocalVariableIcon));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var plusElement = new ClickableElement("+") {
					name = "title-button-add",
				};
				plusElement.onClick = (evt) => {
					if(graphData.selectedRoot != null) {
						var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.GetLocalMousePosition(), graphEditor.window);
						var manipulators = NodeEditorUtility.FindGraphManipulators();
						foreach(var manipulator in manipulators) {
							manipulator.tabData = tabData;
							if(manipulator.IsValid(nameof(manipulator.CreateNewLocalVariable)) && manipulator.CreateNewLocalVariable(mPos, ReloadView)) {
								break;
							}
						}
					}
				};
				icon.parent.Add(plusElement);
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showLocal = evt.newValue;
					ReloadView();
				});
			}
			{//Nested Types
				VisualElement header = scroll.Q("nestedtypes");
				header.SetDisplay(false);
				nestedContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.GraphIcon));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var plusElement = new ClickableElement("+") {
					name = "title-button-add",
				};
				plusElement.onClick = (_) => {
					plusElement.menu = new DropdownMenu();
					CreateNestedTypesMenu(plusElement.menu, graphData.graph as IGraphWithNestedTypes);
				};
				icon.parent.Add(plusElement);
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showNested = evt.newValue;
					ReloadView();
				});
			}
			{//Interfaces
				VisualElement header = scroll.Q("interface");
				header.SetDisplay(false);
				interfaceContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.InterfaceIcon));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var plusElement = new ClickableElement("+") {
					name = "title-button-add",
				};
				plusElement.onClick = (_) => {

					ReloadView();
				};
				icon.parent.Add(plusElement);
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showInterfaces = evt.newValue;
					ReloadView();
				});
			}
			{//Enums
				VisualElement header = scroll.Q("enum");
				header.SetDisplay(false);
				enumContainer = header.Q("contents");
				var icon = header.Q("title-icon") as Image;
				icon.image = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.EnumIcon));
				icon.parent.Add(new VisualElement() { name = "spacer" });
				var plusElement = new ClickableElement("+") {
					name = "title-button-add",
				};
				plusElement.onClick = (_) => {

					ReloadView();
				};
				icon.parent.Add(plusElement);
				var toggle = header.Q("expanded") as Foldout;
				toggle.RegisterValueChangedCallback(evt => {
					showEnums = evt.newValue;
					ReloadView();
				});
			}
		}

		public void Dispose() {
			uNodeEditor.onChanged -= ReloadView;
			uNodeEditor.onSelectionChanged -= OnSelectionChanged;
		}

		void OnSelectionChanged(GraphEditorData graphData) {
			UpdateView();
		}

		public void UpdateView() {
			foreach(var element in scroll.Query<PanelElement>().Build()) {
				UpdateView(element);
			}
		}

		private void UpdateView(PanelElement panel) {
			var element = panel.value;
			if(element == null)
				return;
			var visualElement = panel?.parent?.parent;
			if(visualElement == null)
				return;
			visualElement.EnableInClassList(ussClassNameSelected, object.ReferenceEquals(element, graphData.selecteds.FirstOrDefault()));
			if(element is UnityEngine.Object) {
				visualElement.EnableInClassList(ussClassNameActive, object.ReferenceEquals(element, graphData.owner));
			}
			else if(element is NodeContainer) {
				visualElement.EnableInClassList(ussClassNameActive, object.ReferenceEquals(element, graphData.selectedRoot));
			}
			else {
				visualElement.EnableInClassList(ussClassNameActive, false);
			}
			//else if(pair.Key is Property) {
			//	var obj = pair.Key as Property;
			//	if(!obj.AutoProperty) {
			//		pair.Value.EnableInClassList("active", obj.setRoot != null && obj.setRoot == graphData.selectedRoot || obj.getRoot != null && obj.getRoot == graphData.selectedRoot);
			//	}
			//}
			//else if(pair.Key is IScriptGraphType) {
			//	var obj = pair.Key as IScriptGraphType;
			//	pair.Value.EnableInClassList("active", object.ReferenceEquals(obj, tabData.selectedGraphData.owner));
			//}
		}

		private bool _markedRepaint;
		public void MarkRepaint() {
			if(!_markedRepaint) {
				_markedRepaint = true;
				uNodeThreadUtility.ExecuteAfterCondition(
					() => EditorWindow.focusedWindow == uNodeEditor.window,
					() => {
						_markedRepaint = false;
						ReloadView();
					});
			}
		}

		void SetupPanelElement(UGraphElement graphElement, PanelElement content) {
			content.value = graphElement;
			//Cleanup
			content.onClick = null;
			content.GetDragGenericData = null;
			//content.RemoveFromClassList(ussClassNameActive);
			//content.RemoveFromClassList(ussClassNameSelected);
			content.removeAction = null;
			content.RemoveFromClassList("private-modifier");

			var data = new PanelData();
			content.userData = data;
			content.label.text = graphElement.name;

			uNodeThreadUtility.Queue(() => {
				UpdateView(content);
			});

			content.onClick = (_) => {
				graphEditor.window.ChangeEditorSelection(graphElement);
			};

			if(graphElement is Variable variable) {
				Texture icon = uNodeEditorUtility.GetTypeIcon(variable.type);
				content.ShowIcon(icon);
				if(!variable.modifier.isPublic) {
					content.AddToClassList("private-modifier");
				}
				content.GetDragGenericData = () => {
					var result = new Dictionary<string, object>();
					result["uNode"] = variable;
					result["uNode-Target"] = graphData.graph;
					return result;
				};
				var current = variable;
				//Show Inpect on double click
				data.clickEvent = evt => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
					if(evt.button == 0) {
						if(evt.clickCount == 2 || evt.altKey) {
							var tempName = current.name;
							GraphUtility.RefactorVariableName(mPos, current, () => {
								graphData.Refresh();
								ReloadView();
							});
						}
						else if(evt.shiftKey) {
							ActionPopupWindow.ShowWindow(Vector2.zero, () => {
								CustomInspector.ShowInspector(new GraphEditorData(graphData, current));
							}, 300, 300).ChangePosition(mPos);
						}
					}
				};
				data.contextMenuEvent = evt => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
					evt.menu.AppendAction("Rename", act => {
						GraphUtility.RefactorVariableName(mPos, current, () => {
							graphData.Refresh();
							ReloadView();
						});
					});
					evt.menu.AppendAction("Inspect...", act => {
						ActionPopupWindow.ShowWindow(Vector2.zero, () => {
							CustomInspector.ShowInspector(new GraphEditorData(graphData, current));
						}, 300, 300).ChangePosition(mPos);
					});
					evt.menu.AppendAction("Create Group", act => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Group Variable: " + variable.name);
						var group = current.parent.AddChild(new UGroupElement() { name = "New Group" });
						group.PlaceBehind(current);
						current.SetParent(group);
						ReloadView();
					});
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Find All References", act => {
						GraphUtility.ShowVariableUsages(current);
					});
					//TODO: move to local variable
					//evt.menu.AppendAction("Move To Local Variable", act => {
					//RefactorUtility.MoveVariableToLocalVariable(current.Name, editorData.graph);
					//uNodeGUIUtility.GUIChanged(editorData.graph);
					//});

					var manipulators = NodeEditorUtility.FindGraphManipulators();
					foreach(var manipulator in manipulators) {
						manipulator.tabData = tabData;
						if(manipulator.IsValid(nameof(manipulator.ContextMenuForVariable))) {
							var menuItems = manipulator.ContextMenuForVariable(mPos, variable);
							if(menuItems != null) {
								foreach(var menu in menuItems) {
									if(menu == null) continue;
									evt.menu.MenuItems().Add(menu);
								}
							}
						}
					}
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Move Up", act => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Up Variable: " + variable.name);
						GraphUtility.ReorderMoveUp<VariableContainer>(variable);
						ReloadView();
					});
					evt.menu.AppendAction("Move Down", act => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Up Variable: " + variable.name);
						GraphUtility.ReorderMoveDown<VariableContainer>(variable);
						ReloadView();
					});
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Duplicate", act => {
						if(graphData.owner != null)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Duplicate Variable: " + current.name);
						GraphUtility.CopyPaste.Duplicate(current);
						ReloadView();
					});
					evt.menu.AppendAction("Remove", act => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Variable: " + variable.name);
						variable.Destroy();
						ReloadView();
					});
				};
				content.removeAction = () => {
					if(graphData.owner)
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Variable: " + current.name);
					current.Destroy();
					ReloadView();
				};
				//contentMap[variable] = content;
			}
			else if(graphElement is Property property) {
				Texture icon = uNodeEditorUtility.GetTypeIcon(property.ReturnType());
				content.ShowIcon(icon);
				if(!property.modifier.isPublic) {
					content.AddToClassList("private-modifier");
				}
				content.GetDragGenericData = () => {
					var result = new Dictionary<string, object>();
					result["uNode"] = property;
					result["uNode-Target"] = graphData.graph;
					return result;
				};
				var current = property;

				data.clickEvent = evt => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
					if(evt.button == 0 && (evt.clickCount == 2 || evt.altKey)) {
						GraphUtility.RefactorPropertyName(mPos, current, () => {
							graphData.Refresh();
							ReloadView();
						});
					}
					else if(evt.button == 0 && evt.shiftKey) {
						ActionPopupWindow.ShowWindow(Vector2.zero, () => {
							CustomInspector.ShowInspector(new GraphEditorData(graphData, current));
						}, width: 300, height: 300).ChangePosition(mPos);
					}
				};
				data.contextMenuEvent = evt => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
					evt.menu.AppendAction("Rename", act => {
						GraphUtility.RefactorPropertyName(mPos, current, () => {
							graphData.Refresh();
							ReloadView();
						});
					});
					evt.menu.AppendAction("Inspect...", act => {
						ActionPopupWindow.ShowWindow(Vector2.zero, () => {
							CustomInspector.ShowInspector(new GraphEditorData(graphData, current));
						}, 300, 300).ChangePosition(mPos);
					});
					evt.menu.AppendAction("Create Group", act => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Group Property: " + current.name);
						var group = current.parent.AddChild(new UGroupElement() { name = "New Group" });
						group.PlaceBehind(current);
						current.SetParent(group);
						ReloadView();
					});
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Find All References", act => {
						GraphUtility.ShowPropertyUsages(current);
					});
					var manipulators = NodeEditorUtility.FindGraphManipulators();
					foreach(var manipulator in manipulators) {
						manipulator.tabData = tabData;
						if(manipulator.IsValid(nameof(manipulator.ContextMenuForProperty))) {
							var menuItems = manipulator.ContextMenuForProperty(mPos, property);
							if(menuItems != null) {
								foreach(var menu in menuItems) {
									if(menu == null) continue;
									evt.menu.MenuItems().Add(menu);
								}
							}
						}
					}
					evt.menu.AppendSeparator("");
					if(current.getRoot == null && current.setRoot == null) {
						evt.menu.AppendAction("Add Getter and Setter", act => {
							uNodeEditorUtility.RegisterUndo(graphData.owner);
							NodeEditorUtility.AddNewObject<Function>("Setter", current, func => {
								func.parameters = new List<ParameterData>() { new ParameterData("value", current.ReturnType()) };
								current.setRoot = func;
							});
							NodeEditorUtility.AddNewObject<Function>("Getter", current, func => {
								func.returnType = current.ReturnType();
								current.getRoot = func;
								NodeEditorUtility.AddNewNode<Nodes.NodeReturn>(func, new Vector2(0, 100), (node) => {
									current.getRoot.Entry.EnsureRegistered();
									node.enter.ConnectTo(current.getRoot.Entry.exit);
									if(ReflectionUtils.CanCreateInstance(current.ReturnType())) {
										node.value.AssignToDefault(MemberData.CreateFromValue(ReflectionUtils.CreateInstance(current.ReturnType())));
									}
								});
							});
							graphEditor.Refresh();
						});
					}
					if(current.setRoot == null) {
						evt.menu.AppendAction("Add Setter", act => {
							NodeEditorUtility.AddNewObject<Function>("Setter", current, func => {
								func.parameters = new List<ParameterData>() { new ParameterData("value", current.ReturnType()) };
								current.setRoot = func;
								//		NodeEditorUtility.AddNewObject<Nodes.NodeAction>(editorData.graph, "Entry", func.transform, (node) => {
								//			func.startNode = node;
								//		});
							});
							graphEditor.Refresh();
						});
					}
					if(current.getRoot == null) {
						evt.menu.AppendAction("Add Getter", act => {
							NodeEditorUtility.AddNewObject<Function>("Getter", current, func => {
								func.returnType = current.ReturnType();
								current.getRoot = func;
								NodeEditorUtility.AddNewNode<Nodes.NodeReturn>(func, new Vector2(0, 100), (node) => {
									current.getRoot.Entry.EnsureRegistered();
									node.enter.ConnectTo(current.getRoot.Entry.exit);
									if(ReflectionUtils.CanCreateInstance(current.ReturnType())) {
										node.value.AssignToDefault(MemberData.CreateFromValue(ReflectionUtils.CreateInstance(current.ReturnType())));
									}
								});
							});
							graphEditor.Refresh();
						});
					}
					if(current.getRoot && current.setRoot) {
						evt.menu.AppendAction("Remove Getter and Setter", act => {
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Getter and Setter");
							current.getRoot.Destroy();
							current.setRoot.Destroy();
							graphEditor.Refresh();
						});
					}
					if(current.setRoot) {
						evt.menu.AppendAction("Remove Setter", act => {
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Setter");
							current.setRoot.Destroy();
							graphEditor.Refresh();
						});
					}
					if(current.getRoot) {
						evt.menu.AppendAction("Remove Getter", act => {
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Getter");
							current.getRoot.Destroy();
							graphEditor.Refresh();
						});
					}
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Move Up", act => {
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Up Property: " + current.name);
						GraphUtility.ReorderMoveUp<PropertyContainer>(current);
						graphEditor.Refresh();
					});
					evt.menu.AppendAction("Move Down", act => {
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Down Property: " + current.name);
						GraphUtility.ReorderMoveDown<PropertyContainer>(current);
						graphEditor.Refresh();
					});
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Duplicate", act => {
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Duplicate Property: " + current.name);
						GraphUtility.CopyPaste.Duplicate(current);
						ReloadView();
					});
					evt.menu.AppendAction("Remove", act => {
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Property: " + current.name);
						current.Destroy();
						graphEditor.Refresh();
					});
				};
				content.removeAction = () => {
					if(graphData.owner)
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Property: " + current.name);
					current.Destroy();
					ReloadView();
					graphEditor.Refresh();
				};
			}
			else if(graphElement is Function function) {
				Texture icon = uNodeEditorUtility.GetTypeIcon(function.returnType);
				content.ShowIcon(icon);
				content.onClick = (_) => {
					graphEditor.window.ChangeEditorSelection(function);
					if(graphData.selectedRoot != function || graphData.selectedGroup != null) {
						graphData.currentCanvas = function;
						graphEditor.SelectionChanged();
						graphEditor.UpdatePosition();
						graphEditor.Refresh();
					}
				};
				if(!function.modifier.isPublic) {
					content.AddToClassList("private-modifier");
				}
				content.GetDragGenericData = () => {
					var result = new Dictionary<string, object>();
					result["uNode"] = function;
					result["uNode-Target"] = graphData.graph;
					return result;
				};
				var current = function;
				data.clickEvent = evt => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
					if(evt.button == 0 && (evt.clickCount == 2 || evt.altKey)) {
						//Rename Function
						GraphUtility.RefactorFunctionName(mPos, current, () => {
							graphData.Refresh();
							ReloadView();
						});
					}
					else if(evt.button == 0 && evt.shiftKey) {
						ActionPopupWindow.ShowWindow(Vector2.zero, () => {
							CustomInspector.ShowInspector(new GraphEditorData(graphData, current));
						}, 300, 300).ChangePosition(mPos);
					}
				};
				data.contextMenuEvent = evt => {
					var mPos = UIElementUtility.GetScreenMousePosition(evt.currentTarget as VisualElement, evt.localMousePosition, graphEditor.window);
					evt.menu.AppendAction("Rename", (act) => {
						GraphUtility.RefactorFunctionName(mPos, current, () => {
							graphData.Refresh();
							ReloadView();
						});
					});
					evt.menu.AppendAction("Inspect...", (act) => {
						ActionPopupWindow.ShowWindow(Vector2.zero, () => {
							CustomInspector.ShowInspector(new GraphEditorData(graphData, current));
						}, 300, 300).ChangePosition(mPos);
					});
					evt.menu.AppendAction("Create Group", act => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Group Function: " + current.name);
						var group = current.parent.AddChild(new UGroupElement() { name = "New Group" });
						group.PlaceBehind(current);
						current.SetParent(group);
						ReloadView();
					});
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Find All References", act => {
						GraphUtility.ShowFunctionUsages(current);
					});
					var manipulators = NodeEditorUtility.FindGraphManipulators();
					foreach(var manipulator in manipulators) {
						manipulator.tabData = tabData;
						if(manipulator.IsValid(nameof(manipulator.ContextMenuForFunction))) {
							var menuItems = manipulator.ContextMenuForFunction(mPos, function);
							if(menuItems != null) {
								foreach(var menu in menuItems) {
									if(menu == null) continue;
									evt.menu.MenuItems().Add(menu);
								}
							}
						}
					}
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Move Up", (act) => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Up Function: " + current.name);
						GraphUtility.ReorderMoveUp<FunctionContainer>(current);
						ReloadView();
					});
					evt.menu.AppendAction("Move Down", (act) => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Down Function: " + current.name);
						GraphUtility.ReorderMoveDown<FunctionContainer>(current);
						ReloadView();
					});
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Duplicate", act => {
						if(graphData.owner != null)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Duplicate Function: " + current.name);
						GraphUtility.CopyPaste.Duplicate(current);
						ReloadView();
					});
					evt.menu.AppendAction("Remove", (act) => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Function: " + current.name);
						current.Destroy();
						ReloadView();
					});
				};
				content.removeAction = () => {
					if(graphData.owner)
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Function: " + current.name);
					current.Destroy();
					ReloadView();
					graphEditor.Refresh();
				};
			}
			else if(graphElement is Constructor ctor) {
				content.label.text = "ctor(" + string.Join(", ", ctor.parameters.Select(p => p.type.prettyName).ToArray()) + ")";
				Texture icon = uNodeEditorUtility.GetTypeIcon(typeof(void));
				content.ShowIcon(icon);
				content.onClick = (_) => {
					graphEditor.window.ChangeEditorSelection(ctor);
					if(graphData.selectedRoot != ctor || graphData.selectedGroup != null) {
						graphData.currentCanvas = ctor;
						graphEditor.SelectionChanged();
						graphEditor.UpdatePosition();
						graphEditor.Refresh();
					}
				};
				content.GetDragGenericData = () => {
					var result = new Dictionary<string, object>();
					result["uNode"] = ctor;
					result["uNode-Target"] = graphData.graph;
					return result;
				};
				var current = ctor;

				data.clickEvent = evt => {
					var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
					if(evt.button == 0 && (evt.clickCount == 2 || evt.shiftKey)) {
						ActionPopupWindow.ShowWindow(Vector2.zero, () => {
							CustomInspector.ShowInspector(new GraphEditorData(graphData, current));
						}, 300, 300).ChangePosition(mPos);
					}
				};
				data.contextMenuEvent = evt => {
					var mPos = UIElementUtility.GetScreenMousePosition(evt.currentTarget as VisualElement, evt.localMousePosition, graphEditor.window);
					evt.menu.AppendAction("Inspect...", (act) => {
						ActionPopupWindow.ShowWindow(Vector2.zero, () => {
							CustomInspector.ShowInspector(new GraphEditorData(graphData, current));
						}, 300, 300).ChangePosition(mPos);
					});
					evt.menu.AppendAction("Create Group", act => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Group Constructor: " + current.name);
						var group = current.parent.AddChild(new UGroupElement() { name = "New Group" });
						group.PlaceBehind(current);
						current.SetParent(group);
						ReloadView();
					});
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Move Up", (act) => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Up Constructor: " + current.name);
						GraphUtility.ReorderMoveUp<ConstructorContainer>(current);
						ReloadView();
					});
					evt.menu.AppendAction("Move Down", (act) => {
						if(graphData.owner) {
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Down Constructor: " + current.name);
						}
						GraphUtility.ReorderMoveDown<ConstructorContainer>(current);
						ReloadView();
					});
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Duplicate", act => {
						if(graphData.owner != null)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Duplicate Constructor: " + current.name);
						GraphUtility.CopyPaste.Duplicate(current);
						ReloadView();
					});
					evt.menu.AppendAction("Remove", (act) => {
						if(graphData.owner)
							uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Constructor: " + current.name);
						current.Destroy();
						ReloadView();
					});
				};
				content.removeAction = () => {
					if(graphData.owner)
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Constructor: " + current.name);
					current.Destroy();
					ReloadView();
					graphEditor.Refresh();
				};
			}
			else if(graphElement is MainGraphContainer mainContainer) {
				if(desiredGraph is IMacroGraph || desiredGraph is IStateGraph state && state.CanCreateStateGraph || desiredGraph is ICustomMainGraph) {
					content.label.text = $"[{graphData.mainGraphTitle}]";
					Texture icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.StateIcon));
					content.ShowIcon(icon);
					content.onClick = (_) => {
						graphData.ClearSelection();
						graphData.currentCanvas = null;
						graphEditor.SelectionChanged();
						graphEditor.Refresh();
						graphEditor.UpdatePosition();
					};
				}
				else {
					content.label.text = $"[MAIN]";
					Texture icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.StateIcon));
					content.ShowIcon(icon);
				}
			}
			else if(graphElement is UGroupElement) {
				content.ShowIcon(uNodeEditorUtility.GetTypeIcon((graphElement as UGroupElement).GetIcon()));
				content.onClick = (_) => {
					graphEditor.window.ChangeEditorSelection(graphElement);
				};
				content.removeAction = () => {
					if(graphElement.childCount > 0) {
						int option = EditorUtility.DisplayDialogComplex("Group contains sub item", "There's a sub item in group, do you want to also remove it?", "Yes", "Only remove group", "Cancel");
						if(option == 1) {
							if(graphData.owner)
								uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Group: " + graphElement.name);
							while(graphElement.childCount > 0) {
								var child = graphElement.GetChild(graphElement.childCount - 1);
								child.SetParent(graphElement.parent);
								child.PlaceInFront(graphElement);
							}
							graphElement.Destroy();
							ReloadView();
							graphEditor.Refresh();
							return;
						}
						else if(option == 2) {
							return;
						}
					}
					if(graphData.owner)
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Group: " + graphElement.name);
					graphElement.Destroy();
					ReloadView();
				};
			}
			else {
				Texture icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FolderIcon));
				content.ShowIcon(icon);
				content.onClick = (_) => {
					graphEditor.window.ChangeEditorSelection(graphElement);
				};
				content.removeAction = () => {
					if(graphData.owner)
						uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Element: " + graphElement.name);
					graphElement.Destroy();
					ReloadView();
				};
			}
		}

		void DrawElements(UGraphElement container, VisualElement viewContainer) {
			TreeView treeView = viewContainer.Q<TreeView>();
			if(treeView == null) {
				treeView = new TreeView(
					makeItem: () => {
						var content = new PanelElement("") { name = "content" };
						content.StretchToParentSize();
						content.RegisterCallback<MouseDownEvent>(evt => {
							var data = content.userData as PanelData;
							if(data != null) {
								data.clickEvent?.Invoke(evt);
							}
						});
						content.AddManipulator(new ContextualMenuManipulator(evt => {
							var data = content.userData as PanelData;
							if(data != null) {
								data.contextMenuEvent?.Invoke(evt);
							}
						}));
						return content;
					},
					bindItem: (ve, index) => {
						var element = ve as PanelElement;
						element.index = index;
						var graphElement = treeView.GetItemDataForIndex<UGraphElement>(index);
						SetupPanelElement(graphElement, element);
						var toggle = element.parent.parent.Q<Toggle>();
						if(toggle != null) {
							var data = toggle[0].userData as EventCallback<ChangeEvent<bool>>;
							if(data == null) {
								data = evt => {
									graphElement.expanded = evt.newValue;
								};
								toggle[0].userData = data;
							}
							else {
								toggle.UnregisterValueChangedCallback(data);
							}
							toggle.RegisterValueChangedCallback(data);
						}
					});
				//To make sure we are handling selection manually
				treeView.selectionType = SelectionType.None;
				//Setup the item height
				treeView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
				treeView.fixedItemHeight = 20;
				//Create the dragger handler
				var dragger = new TreeViewDragger(treeView);
				dragger.dragAndDropController = new TreeViewUGraphElementDragAndDropController(treeView, container);
				//Add the tree view to container
				viewContainer.Add(treeView);
			}
			bool ShowChildElements(UGraphElement element) {
				if(element is VariableContainer) {
					return true;
				}
				else if(element is PropertyContainer) {
					return true;
				}
				else if(element is FunctionContainer) {
					return true;
				}
				else if(element is ConstructorContainer) {
					return true;
				}
				else if(element is UGroupElement) {
					return true;
				}
				else if(element is Property) {
					return true;
				}
				return false;
			}
			List<TreeViewItemData<UGraphElement>> GetElements(UGraphElement parent) {
				if(ShowChildElements(parent)) {
					var roots = new List<TreeViewItemData<UGraphElement>>(parent.childCount);
					foreach(var child in parent) {
						if(child is MainGraphContainer) {
							if(desiredGraph is IMacroGraph || desiredGraph is IStateGraph state && state.CanCreateStateGraph || desiredGraph is ICustomMainGraph) {
								roots.Add(new TreeViewItemData<UGraphElement>(child.id, child, GetElements(child)));
							}
							continue;

						}
						roots.Add(new TreeViewItemData<UGraphElement>(child.id, child, GetElements(child)));
					}
					return roots;
				}
				else return null;
			}
			void SetExpands(UGraphElement parent, TreeView treeView) {
				var roots = new List<TreeViewItemData<UGraphElement>>(parent.childCount);
				foreach(var child in parent) {
					//if(treeView.GetRootElementForId(child.id) != null) {
					//}
					if(child.expanded)
						treeView.ExpandItem(child.id);
					SetExpands(child, treeView);
				}
			}
			var items = GetElements(container);
			if(items.Count > 0) {
				treeView.ShowElement();
				treeView.SetRootItems(items);
				treeView.Rebuild();
				SetExpands(container, treeView);
			}
			else {
				treeView.HideElement();
			}
		}

		class PanelData {
			public Action<MouseDownEvent> clickEvent;
			public Action<ContextualMenuPopulateEvent> contextMenuEvent;
		}

		void ReloadView() {
			contentMap.Clear();
			//{//Namespace
			//	if(editorData.graphData != null || !(editorData.graph is IIndependentGraph)) {
			//		if(editorData.graphData != null) {
			//			namespaceButton.label.text = editorData.graph.Namespace;
			//		}
			//		namespaceElement.SetDisplay(true);
			//	} else {
			//		namespaceElement.SetDisplay(false);
			//	}
			//}
			{//Classes
				classContainer.parent.SetDisplay(true);
				for(int i = 0; i < classContainer.childCount; i++) {
					classContainer[i].RemoveFromHierarchy();
					i--;
				}
				if(showClasses) {
					TreeView treeView = classContainer.Q<TreeView>();
					if(treeView == null) {
						treeView = new TreeView(
							makeItem: () => {
								var content = new PanelElement("") { name = "content" };
								content.StretchToParentSize();
								content.RegisterCallback<MouseDownEvent>(evt => {
									var data = content.userData as PanelData;
									if(data != null) {
										data.clickEvent?.Invoke(evt);
									}
								});
								content.AddManipulator(new ContextualMenuManipulator(evt => {
									var data = content.userData as PanelData;
									if(data != null) {
										data.contextMenuEvent?.Invoke(evt);
									}
								}));
								return content;
							},
							bindItem: (ve, index) => {
								var data = new PanelData();
								var unityObject = treeView.GetItemDataForIndex<UnityEngine.Object>(index);
								if(unityObject == null)
									return;
								var element = ve as PanelElement;
								element.index = index;
								element.value = unityObject;
								element.userData = data;

								uNodeThreadUtility.Queue(() => {
									UpdateView(element);
								});

								Texture icon = uNodeEditorUtility.GetTypeIcon(unityObject);
								element.ShowIcon(icon);

								string displayName = unityObject.name;
								if(unityObject is IGraph graph) {
									displayName = graph.GetGraphName();
								}
								else if(string.IsNullOrEmpty(displayName)) {
									displayName = tabData.owner.name;
								}
								element.label.text = displayName;

								element.GetDragGenericData = () => {
									var result = new Dictionary<string, object>();
									result["uNode"] = unityObject;
									result["uNode-Target"] = graphData.graph;
									return result;
								};
								element.GetDragReferences = () => {
									return new[] { unityObject };
								};

								element.onClick = (_) => {
									if(unityObject is IGraph graph) {
										if(graphData.graph != graph) {
											uNodeEditor.Open(graph);
										}
										graphEditor.window.ChangeEditorSelection(null);
									}
									else if(unityObject is EnumScript enumScript) {
										uNodeEditor.Open(enumScript);
										graphEditor.window.ChangeEditorSelection(null);
									}
									else {
										throw new NotImplementedException();
									}
									graphEditor.window.ChangeEditorSelection(new UnityObjectReference(unityObject));
								};

								data.clickEvent = (evt) => {
									var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
									if(evt.button == 0 && (evt.clickCount == 2 || evt.shiftKey)) {
										ActionPopupWindow.ShowWindow(Vector2.zero, () => {
											CustomInspector.ShowInspector(new GraphEditorData(unityObject));
										}, 300, 300).ChangePosition(mPos);
									}
								};
								data.contextMenuEvent = (evt) => {
									var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
									evt.menu.AppendAction("Inspect...", act => {
										ActionPopupWindow.ShowWindow(Vector2.zero, () => {
											CustomInspector.ShowInspector(new GraphEditorData(unityObject));
										}, 300, 300).ChangePosition(mPos);
									});
									if(unityObject is IReflectionType) {
										var rType = (unityObject as IReflectionType).ReflectionType;
										if(rType != null) {
											evt.menu.AppendAction("Find All References", act => {
												GraphUtility.ShowMemberUsages(rType);
											});
										}
									}
									evt.menu.AppendSeparator("");
									if(tabData.owner is IScriptGraph scriptGraph) {
										if(unityObject is IScriptGraphType) {
											evt.menu.AppendAction("Duplicate", act => {
												var clone = UnityEngine.Object.Instantiate(unityObject);
												clone.name = unityObject.name + "_clone";
												scriptGraph.TypeList.AddType(clone as IScriptGraphType, scriptGraph);
												AssetDatabase.AddObjectToAsset(clone, scriptGraph as UnityEngine.Object);
												AssetDatabase.SaveAssetIfDirty(scriptGraph as UnityEngine.Object);
												ReloadView();
											});
											evt.menu.AppendAction("Remove", act => {
												Undo.SetCurrentGroupName("Remove classes: " + unityObject);
												Undo.RegisterFullObjectHierarchyUndo(scriptGraph as UnityEngine.Object, "");
												Undo.DestroyObjectImmediate(unityObject);
												scriptGraph.TypeList.RemoveType(unityObject as IScriptGraphType);
												ReloadView();
											});
										}
									}
									if(unityObject is IGraphWithConstructors graph) {
										evt.menu.AppendSeparator("");
										evt.menu.AppendAction("New Constructor", act => {
											CreateNewConstructor(graph.GraphData.constructorContainer);
										});
									}
									//TODO: support for nested type
									//if(unityObject is IGraphWithNestedTypes nestedTypes) {
									//	evt.menu.AppendSeparator("");
									//	evt.menu.AppendAction("New Nested Types", null, DropdownMenuAction.AlwaysDisabled);
									//	evt.menu.AppendSeparator("");
									//	CreateNestedTypesMenu(evt.menu, root as INestedClassSystem);
									//}
									var manipulators = NodeEditorUtility.FindGraphManipulators();
									foreach(var manipulator in manipulators) {
										manipulator.tabData = tabData;
										if(manipulator.IsValid(nameof(manipulator.ContextMenuForGraph))) {
											var menuItems = manipulator.ContextMenuForGraph(mPos);
											if(menuItems != null) {
												foreach(var menu in menuItems) {
													if(menu == null) continue;
													evt.menu.MenuItems().Add(menu);
												}
											}
										}
									}
								};

								//TODO: add support for nested types
								//var toggle = element.parent.parent.Q<Toggle>();
								//if(toggle != null) {
								//	var data = toggle[0].userData as EventCallback<ChangeEvent<bool>>;
								//	if(data == null) {
								//		data = evt => {
								//			graphElement.expanded = evt.newValue;
								//		};
								//		toggle[0].userData = data;
								//	}
								//	else {
								//		toggle.UnregisterValueChangedCallback(data);
								//	}
								//	toggle.RegisterValueChangedCallback(data);
								//}
							});
						//To make sure we are handling selection manually
						treeView.selectionType = SelectionType.None;
						//Setup the item height
						treeView.fixedItemHeight = 20;
						treeView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
						if(tabData.owner is IScriptGraph) {
							var scriptGraph = tabData.owner as IScriptGraph;
							//Create the dragger handler
							var dragger = new TreeViewDragger(treeView);
							dragger.dragAndDropController = new TreeViewCustomDragAndDropController(treeView, (id, parentId, childIndex) => {
								Undo.RegisterCompleteObjectUndo(tabData.owner, "reorder");

								var source = treeView.GetItemDataForId<UnityEngine.Object>(id);
								if(parentId < 0) {
									var references = scriptGraph.TypeList.references;
									if(childIndex < 0) {
										references.Remove(source);
										references.Add(source);
									}
									else {
										var sourceIndex = references.IndexOf(source);
										if(sourceIndex < childIndex) {
											references.Remove(source);
											references.Insert(childIndex - 1, source);
										}
										else if(sourceIndex > childIndex) {
											references.Remove(source);
											references.Insert(childIndex, source);
										}
									}
								}
								else {
									//TODO: reorder for nested graph
									//var parent = treeView.GetItemDataForId<UnityEngine.Object>(parentId);

								}
								ReloadView();
							});
						}
						else {
							var dragger = new TreeViewDragger(treeView);
							dragger.dragAndDropController = new TreeViewCustomDragAndDropController(treeView, (id, parentId, childIndex) => {

							});
						}
						//Add the tree view to container
						classContainer.Add(treeView);
					}

					classAddButton.SetDisplay(tabData.owner is IScriptGraph);
					if(tabData.owner is IScriptGraph) {

						var scriptGraph = tabData.owner as IScriptGraph;
						var types = scriptGraph.TypeList.references;
						var items = new List<TreeViewItemData<UnityEngine.Object>>(types.Count);
						foreach(var cls in types) {
							items.Add(new TreeViewItemData<UnityEngine.Object>(cls.GetHashCode(), cls, null));
						}
						if(items.Count > 0) {
							treeView.ShowElement();
							treeView.SetRootItems(items);
							treeView.Rebuild();
							//SetExpands(container, treeView);
						}
						else {
							treeView.HideElement();
						}
					}
					else if(treeView != null && tabData != null && tabData.owner != null) {
						treeView.ShowElement();
						treeView.SetRootItems(
							new List<TreeViewItemData<UnityEngine.Object>>() {
									new TreeViewItemData<UnityEngine.Object>(
										tabData.owner.GetHashCode(),
										tabData.owner)
							}
						);
						treeView.Rebuild();
					}
				}
				else {
					classContainer.parent.SetDisplay(false);
				}
			}
			var graphSystem = GraphUtility.GetGraphSystem(graphData.graph as UnityEngine.Object);
			//Variables
			if(graphData.graph != null && graphData.graph is IGraphWithVariables) {
				variableContainer.parent.SetDisplay(true);
				if(showVariables) {
					var container = graphData.graphData.variableContainer;
					DrawElements(container, variableContainer);
				}
				else {
					for(int i = 0; i < variableContainer.childCount; i++) {
						variableContainer[i].RemoveFromHierarchy();
						i--;
					}
				}
			}
			else {
				variableContainer.parent.SetDisplay(false);
			}
			//Properties
			if(graphData.graph != null && (graphData.graph is IGraphWithProperties || graphData.graph.GetProperties().Any())) {
				propertyContainer.parent.SetDisplay(true);
				if(showProperties) {
					var container = graphData.graphData.propertyContainer;
					DrawElements(container, propertyContainer);
				}
				else {
					for(int i = 0; i < propertyContainer.childCount; i++) {
						propertyContainer[i].RemoveFromHierarchy();
						i--;
					}
				}
			}
			else {
				propertyContainer.parent.SetDisplay(false);
			}

			//Functions
			if(graphData.graph != null && (graphData.graph is IGraphWithFunctions || graphData.graph is IMacroGraph || graphData.graph is ICustomMainGraph)) {
				functionContainer.parent.SetDisplay(true);
				if(showFunctions) {
					if(graphData.graph is IMacroGraph) {
						functionAddButton.SetDisplay(false);
					}
					else {
						functionAddButton.SetDisplay(true);
					}
					var container = graphData.graphData.functionContainer;
					DrawElements(container, functionContainer);
				}
				else {
					for(int i = 0; i < functionContainer.childCount; i++) {
						functionContainer[i].RemoveFromHierarchy();
						i--;
					}
				}
			}
			else {
				functionContainer.parent.SetDisplay(false);
			}
			//Events
			if(desiredGraph is IStateGraph stateGraph && stateGraph.CanCreateStateGraph) {
				eventContainer.parent.SetDisplay(true);
				if(showEvents && graphData.graphData.mainGraphContainer.GetNodeInChildren<BaseEventNode>() != null && graphData.isInMainGraph) {
					var events = graphData.graphData.mainGraphContainer.GetNodesInChildren<BaseEventNode>().ToArray();

					TreeView treeView = eventContainer.Q<TreeView>();
					if(treeView == null) {
						treeView = new TreeView(
							makeItem: () => {
								var content = new PanelElement("") { name = "content" };
								content.StretchToParentSize();
								content.RegisterCallback<MouseDownEvent>(evt => {
									var data = content.userData as PanelData;
									if(data != null) {
										data.clickEvent?.Invoke(evt);
									}
								});
								content.AddManipulator(new ContextualMenuManipulator(evt => {
									var data = content.userData as PanelData;
									if(data != null) {
										data.contextMenuEvent?.Invoke(evt);
									}
								}));
								return content;
							},
							bindItem: (ve, index) => {
								var data = new PanelData();
								var eventNode = treeView.GetItemDataForIndex<BaseEventNode>(index);
								var element = ve as PanelElement;
								element.index = index;
								element.value = eventNode;
								element.userData = data;

								uNodeThreadUtility.Queue(() => {
									UpdateView(element);
								});

								Texture icon = uNodeEditorUtility.GetTypeIcon(eventNode.GetNodeIcon());
								element.ShowIcon(icon);

								element.removeAction = () => {
									uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Node: " + eventNode.GetTitle());
									eventNode.nodeObject.Destroy();
									graphEditor.Refresh();
								};

								element.label.text = eventNode.GetTitle();

								element.onClick = (_) => {
									graphEditor.Highlight(eventNode);
								};

								data.clickEvent = (evt) => {
									var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
									if(evt.button == 0 && (evt.clickCount == 2 || evt.shiftKey)) {
										ActionPopupWindow.ShowWindow(Vector2.zero, () => {
											CustomInspector.ShowInspector(new GraphEditorData(eventNode));
										}, 300, 300).ChangePosition(mPos);
									}
								};
								data.contextMenuEvent = (evt) => {
									var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graphEditor.window);
									evt.menu.AppendAction("Inspect...", act => {
										ActionPopupWindow.ShowWindow(Vector2.zero, () => {
											CustomInspector.ShowInspector(new GraphEditorData(eventNode));
										}, 300, 300).ChangePosition(mPos);
									});
									evt.menu.AppendSeparator("");

									evt.menu.AppendAction("Move Up", (act) => {
										if(index > 0) {
											uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Up Event: " + eventNode.GetTitle());
											eventNode.nodeObject.PlaceBehind(treeView.GetItemDataForIndex<BaseEventNode>(index - 1));
											graphEditor.Refresh();
											ReloadView();
										}
									});
									evt.menu.AppendAction("Move Down", (act) => {
										if(index + 1 < events.Length) {
											uNodeEditorUtility.RegisterUndo(graphData.owner, "Move Down Event: " + eventNode.GetTitle());
											eventNode.nodeObject.PlaceInFront(treeView.GetItemDataForIndex<BaseEventNode>(index + 1));
											graphEditor.Refresh();
											ReloadView();
										}
									});
									evt.menu.AppendSeparator("");
									evt.menu.AppendAction("Remove", (act) => {
										uNodeEditorUtility.RegisterUndo(graphData.owner, "Remove Event: " + eventNode.GetTitle());
										eventNode.nodeObject.Destroy();
										graphEditor.Refresh();
									});
									//TODO: context menu for event
									//var manipulators = NodeEditorUtility.FindGraphManipulators();
									//foreach(var manipulator in manipulators) {
									//	manipulator.tabData = tabData;
									//	if(manipulator.IsValid(nameof(manipulator.ContextMenuForGraph))) {
									//		var menuItems = manipulator.ContextMenuForGraph(mPos);
									//		if(menuItems != null) {
									//			foreach(var menu in menuItems) {
									//				if(menu == null) continue;
									//				evt.menu.MenuItems().Add(menu);
									//			}
									//		}
									//	}
									//}
								};
							});
						//To make sure we are handling selection manually
						treeView.selectionType = SelectionType.None;
						//Setup the item height
						treeView.fixedItemHeight = 20;
						treeView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
						//TODO: implement reordering for events
						//if(tabData.owner is IScriptGraph) {
						//	var scriptGraph = tabData.owner as IScriptGraph;
						//	//Create the dragger handler
						//	var dragger = new TreeViewDragger(treeView);
						//	dragger.dragAndDropController = new TreeViewCustomDragAndDropController(treeView, (id, parentId, childIndex) => {
						//		Undo.RegisterCompleteObjectUndo(tabData.owner, "reorder");

						//		var source = treeView.GetItemDataForId<UnityEngine.Object>(id);
						//		if(parentId < 0) {
						//			var references = scriptGraph.TypeList.references;
						//			if(childIndex < 0) {
						//				references.Remove(source);
						//				references.Add(source);
						//			}
						//			else {
						//				var sourceIndex = references.IndexOf(source);
						//				if(sourceIndex < childIndex) {
						//					references.Remove(source);
						//					references.Insert(childIndex - 1, source);
						//				}
						//				else if(sourceIndex > childIndex) {
						//					references.Remove(source);
						//					references.Insert(childIndex, source);
						//				}
						//			}
						//		}
						//		else {
						//			//TODO: reorder for nested graph
						//			//var parent = treeView.GetItemDataForId<UnityEngine.Object>(parentId);

						//		}
						//		ReloadView();
						//	});
						//}
						//Add the tree view to container
						eventContainer.Add(treeView);
					}
					{

						var items = new List<TreeViewItemData<BaseEventNode>>(events.Length);
						foreach(var cls in events) {
							items.Add(new TreeViewItemData<BaseEventNode>(cls.id, cls, null));
						}
						if(items.Count > 0) {
							treeView.ShowElement();
							treeView.SetRootItems(items);
							treeView.Rebuild();
							//SetExpands(container, treeView);
						}
						else {
							treeView.HideElement();
						}
					}
				}
				else {
					eventContainer.parent.SetDisplay(false);
				}
			}
			else {
				eventContainer.parent.SetDisplay(false);
			}
			//Constructor
			if(graphData.graph != null && (graphData.graph is IGraphWithConstructors && graphData.graphData.constructorContainer.childCount > 0)) {
				constructorContainer.parent.SetDisplay(true);
				if(showConstructors) {
					var container = graphData.graphData.constructorContainer;
					DrawElements(container, constructorContainer);
				}
				else {
					for(int i = 0; i < constructorContainer.childCount; i++) {
						constructorContainer[i].RemoveFromHierarchy();
						i--;
					}
				}
			}
			else {
				constructorContainer.parent.SetDisplay(false);
			}
			//Local Variable
			if(graphData.graph != null && graphData.selectedRoot != null && !(graphData.selectedRoot is MainGraphContainer)) {
				localContainer.parent.SetDisplay(true);
				if(showLocal) {
					var container = graphData.selectedRoot.variableContainer;
					DrawElements(container, localContainer);
				}
				else {
					for(int i = 0; i < localContainer.childCount; i++) {
						localContainer[i].RemoveFromHierarchy();
						i--;
					}
				}
			}
			else {
				localContainer.parent.SetDisplay(false);
			}
			//Nested Types
			//if(editorData.graph && editorData.graph is INestedClassSystem) {
			//	nestedContainer.parent.SetDisplay(true);
			//	for(int i = 0; i < nestedContainer.childCount; i++) {
			//		nestedContainer[i].RemoveFromHierarchy();
			//		i--;
			//	}
			//	var nestedSystem = editorData.graph as INestedClassSystem;
			//	var nestedTypes = nestedSystem.NestedClass;
			//	if(showInterfaces && nestedTypes) {
			//		var types = nestedTypes?.GetComponents<uNodeRoot>();
			//		if(types.Length > 0) {
			//			foreach(var root in types) {
			//				var content = new PanelElement(root.DisplayName, () => {
			//					uNodeEditor.Open(root);
			//				}) {
			//					name = "content",
			//					onStartDrag = () => {
			//						DragAndDrop.SetGenericData("uNode", root);
			//						DragAndDrop.SetGenericData("uNode-Target", root);
			//					},
			//				};
			//				content.ShowIcon(uNodeEditorUtility.GetTypeIcon(root));
			//				nestedContainer.Add(content);
			//				contentMap[root] = content;
			//			}
			//		}
			//		if(nestedTypes.enums?.Length > 0) {
			//			foreach(var data in nestedTypes.enums) {
			//				var content = new PanelElement(data.name, () => {
			//					uNodeEditor.Open(nestedTypes);
			//				}) {
			//					name = "content",
			//				};
			//				content.ShowIcon(uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.EnumIcon)));
			//				nestedContainer.Add(content);
			//				contentMap[data] = content;
			//			}
			//		}
			//		if(nestedTypes.interfaces?.Length > 0) {
			//			foreach(var data in nestedTypes.interfaces) {
			//				var content = new PanelElement(data.name, () => {
			//					uNodeEditor.Open(nestedTypes);
			//				}) {
			//					name = "content",
			//				};
			//				content.ShowIcon(uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.InterfaceIcon)));
			//				nestedContainer.Add(content);
			//				contentMap[data] = content;
			//			}
			//		}
			//		if(nestedTypes.delegates?.Length > 0) {
			//			foreach(var data in nestedTypes.delegates) {
			//				var content = new PanelElement(data.name, () => {
			//					uNodeEditor.Open(nestedTypes);
			//				}) {
			//					name = "content",
			//				};
			//				content.ShowIcon(uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.DelegateIcon)));
			//				nestedContainer.Add(content);
			//				contentMap[data] = content;
			//			}
			//		}
			//	} else {
			//		nestedContainer.parent.SetDisplay(false);
			//	}
			//} else {
			//	nestedContainer.parent.SetDisplay(false);
			//}
			//Interfaces
			//if(editorData.graphData != null && editorData.graphData.interfaces.Length > 0) {
			//	interfaceContainer.parent.SetDisplay(true);
			//	for(int i = 0; i < interfaceContainer.childCount; i++) {
			//		interfaceContainer[i].RemoveFromHierarchy();
			//		i--;
			//	}
			//	if(showInterfaces) {
			//		foreach(var iface in editorData.graphData.interfaces) {
			//			var content = new PanelElement(iface.name, () => {
			//				graph.window.ChangeEditorSelection(iface);
			//			}) {
			//				name = "content",
			//				onStartDrag = () => {
			//					DragAndDrop.SetGenericData("uNode", iface);
			//					DragAndDrop.SetGenericData("uNode-Target", editorData.graphData);
			//				},
			//			};
			//			var current = iface;
			//			content.AddManipulator(new ContextualMenuManipulator(evt => {
			//				var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graph.window);
			//				evt.menu.AppendAction("Rename", act => {
			//					ShowRenameAction(mPos, current.name, (string str) => {
			//						uNodeEditorUtility.RegisterUndo(editorData.graphData, "Rename Interface");
			//						current.name = str;
			//					});
			//				});
			//				evt.menu.AppendAction("Remove", act => {
			//					uNodeEditorUtility.RegisterUndo(editorData.graphData, "Remove Interface");
			//					ArrayUtility.Remove(ref editorData.graphData.interfaces, current);
			//					ReloadView();
			//				});
			//			}));
			//			interfaceContainer.Add(content);
			//			contentMap[iface] = content;
			//		}
			//	}
			//} else {
			//	interfaceContainer.parent.SetDisplay(false);
			//}
			//Enums
			//if(editorData.graphData != null && editorData.graphData.enums.Length > 0) {
			//	enumContainer.parent.SetDisplay(true);
			//	for(int i = 0; i < enumContainer.childCount; i++) {
			//		enumContainer[i].RemoveFromHierarchy();
			//		i--;
			//	}
			//	if(showEnums) {
			//		foreach(var e in editorData.graphData.enums) {
			//			var content = new PanelElement(e.name, () => {
			//				graph.window.ChangeEditorSelection(e);
			//			}) {
			//				name = "content",
			//				onStartDrag = () => {
			//					DragAndDrop.SetGenericData("uNode", e);
			//					DragAndDrop.SetGenericData("uNode-Target", editorData.graphData);
			//				},
			//			};
			//			var current = e;
			//			content.AddManipulator(new ContextualMenuManipulator(evt => {
			//				var mPos = (evt.currentTarget as VisualElement).GetScreenMousePosition(evt.localMousePosition, graph.window);
			//				evt.menu.AppendAction("Rename", act => {
			//					ShowRenameAction(mPos, current.name, (string str) => {
			//						uNodeEditorUtility.RegisterUndo(editorData.graphData, "Rename Enum");
			//						current.name = str;
			//					});
			//				});
			//				evt.menu.AppendAction("Remove", act => {
			//					uNodeEditorUtility.RegisterUndo(editorData.graphData, "Remove Enum");
			//					ArrayUtility.Remove(ref editorData.graphData.enums, current);
			//					ReloadView();
			//				});
			//			}));
			//			enumContainer.Add(content);
			//			contentMap[e] = content;
			//		}
			//	}
			//} else {
			//	enumContainer.parent.SetDisplay(false);
			//}
			UpdateView();
			var scrollOffset = scroll.scrollOffset;
			uNodeThreadUtility.ExecuteOnce(() => {
				if(scroll != null && scroll.scrollOffset == Vector2.zero) {
					scroll.scrollOffset = scrollOffset;
				}
			}, "[GRAPH-PANEL-UpdateScroller]");
		}

		#region Other
		private void CreateNestedTypesMenu(DropdownMenu menu, IGraphWithNestedTypes nestedSystem) {
			//var nestedTypes = nestedSystem.NestedClass;
			//menu.AppendAction("Class", act => {

			//});
			//menu.AppendAction("Struct", act => {

			//});
			//menu.AppendAction("Interfaces", act => {

			//});
			//menu.AppendAction("Enums", act => {

			//});
		}

		//private void OnNamespaceClicked(Vector2 position, uNodeData data) {
		//	ActionPopupWindow.ShowWindow(Vector2.zero, data.Namespace,
		//		delegate (ref object obj) {
		//			obj = EditorGUILayout.TextField("Namespace", obj as string);
		//		}, null, delegate (ref object obj) {
		//			if(GUILayout.Button("Apply") || Event.current.keyCode == KeyCode.Return) {
		//				Undo.RegisterCompleteObjectUndo(data, "Rename namespace");
		//				data.Namespace = obj as string;
		//				ActionPopupWindow.CloseLast();
		//				ReloadView();
		//			}
		//		}).ChangePosition(position).headerName = "Rename Namespace";
		//}

		private void CreateNewConstructor(ConstructorContainer root) {
			string fName = "NewConstructor";
			if(root.collections != null) {
				int index = 0;
				while(true) {
					index++;
					bool found = false;
					foreach(var f in root.collections) {
						if(f != null && f.name.Equals(fName)) {
							found = true;
							break;
						}
					}
					if(found) {
						fName = "NewConstructor" + index;
					}
					else {
						break;
					}
				}
			}
			int num = 0;
			if(root.collections != null) {
				while(true) {
					bool same = false;
					foreach(var f in root.collections) {
						if(f != null && f.parameters.Count == num) {
							num++;
							same = true;
							break;
						}
					}
					if(!same)
						break;
				}
			}
			List<ParameterData> parameters = new List<ParameterData>(num);
			for(int i = 0; i < parameters.Count; i++) {
				parameters.Add(new ParameterData("p" + (i + 1), typeof(object)));
			}
			NodeEditorUtility.AddNewConstructor(root, fName, (ctor) => {
				ctor.parameters = parameters;
			});
		}

		internal bool HandleShortcut(GraphShortcutType type) {
			if(type == GraphShortcutType.Rename) {
				if(graphData.selectedCount == 1) {
					var selected = graphData.selecteds.First();
					if(selected is Variable) {
						GraphUtility.RefactorVariableName(Event.current.mousePosition, selected as Variable, () => {
							graphData.Refresh();
							ReloadView();
						});
						return true;
					}
					else if(selected is Property) {
						GraphUtility.RefactorPropertyName(Event.current.mousePosition, selected as Property, () => {
							graphData.Refresh();
							ReloadView();
						});
						return true;
					}
					else if(selected is Function) {
						GraphUtility.RefactorFunctionName(Event.current.mousePosition, selected as Function, () => {
							graphData.Refresh();
							ReloadView();
						});
						return true;
					}
					else if(selected is IGraph) {
						//TODO: add support for renaming graph with shortcut
						return true;
					}
				}
			}
			return false;
		}
		#endregion
	}

	internal class PanelElement : ClickableElement, ITreeViewItemElement {
		public object value;

		private ClickableElement removeElement;

		public PanelElement(string text) : base(text) {
			Init();
		}

		public PanelElement(string text, Action onClick) : base(text, onClick) {
			Init();
		}

		void Init() {
			this.RemoveManipulator(clickable);
			this.AddManipulator(new LeftMouseClickable(evt => {
				if(onClick != null && !evt.shiftKey) {
					onClick(evt);
				}
			}) { stopPropagationOnClick = false });
		}

		public Func<Dictionary<string, object>> GetDragGenericData;
		public Func<IEnumerable<UnityEngine.Object>> GetDragReferences;
		public Action removeAction {
			set {
				if(value != null) {
					if(removeElement == null) {//Remove button
						this.Add(new VisualElement() { name = "spacer" });
						removeElement = new ClickableElement("-") {
							name = "content-button-remove",
						};
						this.Add(removeElement);
					}
					removeElement.onClick = (_) => value.Invoke();
				}
				else if(removeElement != null) {
					removeElement.RemoveFromHierarchy();
					removeElement = null;
				}
			}
		}

		public int index { get; set; }

		public bool CanDragInsideParent() {
			if(value is UGraphElement element && element.parent != null) {
				return element.parent is not Property;
			}
			return true;
		}

		public bool CanHaveChilds() {
			return value is UGroupElement;
		}

		public bool CanDrag() {
			if(value is Function function) {
				return function.parent is not Property;
			}
			return true;
		}

		Dictionary<string, object> ITreeViewItemElement.GetDragGenericData() {
			return GetDragGenericData?.Invoke();
		}

		IEnumerable<UnityEngine.Object> ITreeViewItemElement.GetDraggedReferences() {
			return GetDragReferences?.Invoke();
		}
	}
}