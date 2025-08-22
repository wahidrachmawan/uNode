using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;
using System.Collections;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public class ContextMenuItem : IComparable<ContextMenuItem> {
		public DropdownMenuItem menu;
		public int order;

		public ContextMenuItem() { }

		public ContextMenuItem(DropdownMenuItem menu, int order = 0) {
			this.menu = menu;
			this.order = order;
		}

		public ContextMenuItem(string actionName, Action<DropdownMenuAction> action, int order = 0) {
			this.menu = new DropdownMenuAction(actionName, action, DropdownMenuAction.AlwaysEnabled);
			this.order = order;
		}

		public static ContextMenuItem CreateAction(string actionName, Action<DropdownMenuAction> action, int order = 0) {
			return new ContextMenuItem(actionName, action, order);
		}

		public static ContextMenuItem CreateSeparator(string subMenuPath = "", int order = 0) {
			return new ContextMenuItem() {
				menu = new DropdownMenuSeparator(subMenuPath),
				order = order,
			};
		}

		public int CompareTo(ContextMenuItem other) {
			return CompareUtility.Compare(order, other.order);
		}

		public static implicit operator ContextMenuItem(DropdownMenuItem menu) {
			return new ContextMenuItem(menu);
		}
	}

	/// <summary>
	/// Provides an abstract base class for manipulating graph-related data and behavior.
	/// </summary>
	/// <remarks>The <see cref="GraphManipulator"/> class defines a framework for handling commands, canvas
	/// features,  and context menus within a graph editor. It includes methods for validating actions, handling commands, 
	/// and creating new graph elements such as variables, properties, and functions.  Derived classes can override its
	/// virtual members to implement specific manipulation logic.</remarks>
	public abstract class GraphManipulator {
		/// <summary>
		/// The list of manipulation command
		/// </summary>
		public static class Command {
			public const string Paste = nameof(Paste);
			public const string PasteWithLink = nameof(PasteWithLink);
			public const string Copy = nameof(Copy);

			public const string CanCopy = nameof(CanCopy);
			public const string CanPaste = nameof(CanPaste);

			public const string OpenItemSelector = nameof(OpenItemSelector);
			public const string OpenCommand = nameof(OpenCommand);
		}

		/// <summary>
		/// The list of build-in canvas feature
		/// </summary>
		public static class Feature {
			public const string SurroundWith = nameof(SurroundWith);
			public const string Macro = nameof(Macro); 
			public const string PlaceFit = nameof(PlaceFit);
			public const string ShowAddNodeContextMenu = nameof(ShowAddNodeContextMenu);
		}

		private uNodeEditor.TabData m_tabData;
		/// <summary>
		/// The tab reference data
		/// </summary>
		public uNodeEditor.TabData tabData {
			get => m_tabData;
			private set {
				m_tabData = value;
				graphData = value.selectedGraphData;
			}
		}

		private GraphEditor m_graphEditor;
		/// <summary>
		/// The graph editor reference
		/// </summary>
		public GraphEditor graphEditor {
			get => m_graphEditor;
			set {
				m_graphEditor = value;
				tabData = value.tabData;
			}
		}

		/// <summary>
		/// The graph editor data reference
		/// </summary>
		public GraphEditorData graphData { get; private set; }
		/// <summary>
		/// The graph reference
		/// </summary>
		public IGraph graph => graphData.graph;

		/// <summary>
		/// The order of manipulator
		/// </summary>
		public virtual int order => 0;

		/// <summary>
		/// Check whether the manipulator is valid for <paramref name="action"/>
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public virtual bool IsValid(string action) => false;

		/// <summary>
		/// Manipulate the command for allow/disallow some command to be executed.
		/// If result is `false` The command will be leaved to be manipulated by others manipulator.
		/// If result is `true` The command will be handled by this manipulator.
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public virtual bool HandleCommand(string command) => false;

		/// <summary>
		/// Finalize manipulate all canvas features
		/// </summary>
		/// <param name="features"></param>
		public virtual void ManipulateCanvasFeatures(HashSet<string> features) { }

		/// <summary>
		/// Get the current canvas features
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<string> GetCanvasFeatures() => null;

		/// <summary>
		/// Callback for create a new variable
		/// </summary>
		/// <param name="mousePosition"></param>
		/// <param name="postAction"></param>
		/// <returns></returns>
		public virtual bool CreateNewVariable(Vector2 mousePosition, Action postAction) => false;
		/// <summary>
		/// Callback for create new property
		/// </summary>
		/// <param name="mousePosition"></param>
		/// <param name="postAction"></param>
		/// <returns></returns>
		public virtual bool CreateNewProperty(Vector2 mousePosition, Action postAction) => false;
		/// <summary>
		/// Callback for create new function
		/// </summary>
		/// <param name="mousePosition"></param>
		/// <param name="postAction"></param>
		/// <returns></returns>
		public virtual bool CreateNewFunction(Vector2 mousePosition, Action postAction) => false;
		/// <summary>
		/// Callback for create new graph
		/// </summary>
		/// <param name="mousePosition"></param>
		/// <param name="postAction"></param>
		/// <returns></returns>
		public virtual bool CreateNewGraph(Vector2 mousePosition, Action postAction) => false;
		/// <summary>
		/// Callback for create new local variable
		/// </summary>
		/// <param name="mousePosition"></param>
		/// <param name="postAction"></param>
		/// <returns></returns>
		public virtual bool CreateNewLocalVariable(Vector2 mousePosition, Action postAction) => false;
		/// <summary>
		/// Callback for create new classes
		/// </summary>
		/// <param name="mousePosition"></param>
		/// <param name="postAction"></param>
		/// <returns></returns>
		public virtual bool CreateNewClass(Vector2 mousePosition, Action postAction) => false;
		public virtual IEnumerable<ContextMenuItem> ContextMenuForGraph(Vector2 mousePosition) => null;
		public virtual IEnumerable<ContextMenuItem> ContextMenuForGraphCanvas(Vector2 mousePosition) => null;
		public virtual IEnumerable<ContextMenuItem> ContextMenuForVariable(Vector2 mousePosition, Variable value) => null;
		public virtual IEnumerable<ContextMenuItem> ContextMenuForProperty(Vector2 mousePosition, Property value) => null;
		public virtual IEnumerable<ContextMenuItem> ContextMenuForEventGraph(Vector2 mousePosition, NodeContainer value) => null;
		public virtual IEnumerable<ContextMenuItem> ContextMenuForFunction(Vector2 mousePosition, Function value) => null;
		public virtual IEnumerable<ContextMenuItem> ContextMenuForNode(Vector2 mousePosition, Node value) => null;

		#region Static
		public static ContextMenuItem[] GetAllMenuForGraph(GraphEditor graphEditor, Vector2 mousePosition) {
			var manipulators = NodeEditorUtility.FindGraphManipulators();
			var pool = StaticListPool<ContextMenuItem>.pool;
			var list = pool.Allocate();
			foreach(var manipulator in manipulators) {
				manipulator.graphEditor = graphEditor;
				if(manipulator.IsValid(nameof(manipulator.ContextMenuForGraph))) {
					var menuItems = manipulator.ContextMenuForGraph(mousePosition);
					if(menuItems != null) {
						foreach(var menu in menuItems) {
							if(menu == null) continue;
							int index = list.FindLastIndex(item => menu.CompareTo(item) == 0);
							if(index < 0) {
								index = list.FindIndex(item => menu.CompareTo(item) > 0);
								if(index > 0) {
									index--;
								}
							}
							else {
								index++;
							}
							if(index >= 0 && list.Count > index) {
								list.Insert(index, menu);
							}
							else if(index == -1 && list.Count > 0) {
								list.Insert(0, menu);
							}
							else {
								list.Add(menu);
							}
						}
					}
				}
			}
			var result = list.ToArray();
			pool.Free(list);
			return result;
		}

		public static ContextMenuItem[] GetAllMenuForGraphCanvas(GraphEditor graphEditor, Vector2 mousePosition) {
			var manipulators = NodeEditorUtility.FindGraphManipulators();
			var pool = StaticListPool<ContextMenuItem>.pool;
			var list = pool.Allocate();
			foreach(var manipulator in manipulators) {
				manipulator.graphEditor = graphEditor;
				if(manipulator.IsValid(nameof(manipulator.ContextMenuForGraphCanvas))) {
					var menuItems = manipulator.ContextMenuForGraphCanvas(mousePosition);
					if(menuItems != null) {
						foreach(var menu in menuItems) {
							if(menu == null) continue;
							int index = list.FindLastIndex(item => menu.CompareTo(item) == 0);
							if(index < 0) {
								index = list.FindIndex(item => menu.CompareTo(item) > 0);
								if(index > 0) {
									index--;
								}
							}
							else {
								index++;
							}
							if(index >= 0 && list.Count > index) {
								list.Insert(index, menu);
							}
							else if(index == -1 && list.Count > 0) {
								list.Insert(0, menu);
							}
							else {
								list.Add(menu);
							}
						}
					}
				}
			}
			var result = list.ToArray();
			pool.Free(list);
			return result;
		}

		public static ContextMenuItem[] GetAllMenuForVariable(GraphEditor graphEditor, Vector2 mousePosition, Variable value) {
			var manipulators = NodeEditorUtility.FindGraphManipulators();
			var pool = StaticListPool<ContextMenuItem>.pool;
			var list = pool.Allocate();
			foreach(var manipulator in manipulators) {
				manipulator.graphEditor = graphEditor;
				if(manipulator.IsValid(nameof(manipulator.ContextMenuForVariable))) {
					var menuItems = manipulator.ContextMenuForVariable(mousePosition, value);
					if(menuItems != null) {
						foreach(var menu in menuItems) {
							if(menu == null) continue;
							int index = list.FindLastIndex(item => menu.CompareTo(item) == 0);
							if(index < 0) {
								index = list.FindIndex(item => menu.CompareTo(item) > 0);
								if(index > 0) {
									index--;
								}
							}
							else {
								index++;
							}
							if(index >= 0 && list.Count > index) {
								list.Insert(index, menu);
							}
							else if(index == -1 && list.Count > 0) {
								list.Insert(0, menu);
							}
							else {
								list.Add(menu);
							}
						}
					}
				}
			}
			var result = list.ToArray();
			pool.Free(list);
			return result;
		}

		public static ContextMenuItem[] GetAllMenuForProperty(GraphEditor graphEditor, Vector2 mousePosition, Property value) {
			var manipulators = NodeEditorUtility.FindGraphManipulators();
			var pool = StaticListPool<ContextMenuItem>.pool;
			var list = pool.Allocate();
			foreach(var manipulator in manipulators) {
				manipulator.graphEditor = graphEditor;
				if(manipulator.IsValid(nameof(manipulator.ContextMenuForProperty))) {
					var menuItems = manipulator.ContextMenuForProperty(mousePosition, value);
					if(menuItems != null) {
						foreach(var menu in menuItems) {
							if(menu == null) continue;
							int index = list.FindLastIndex(item => menu.CompareTo(item) == 0);
							if(index < 0) {
								index = list.FindIndex(item => menu.CompareTo(item) > 0);
								if(index > 0) {
									index--;
								}
							}
							else {
								index++;
							}
							if(index >= 0 && list.Count > index) {
								list.Insert(index, menu);
							}
							else if(index == -1 && list.Count > 0) {
								list.Insert(0, menu);
							}
							else {
								list.Add(menu);
							}
						}
					}
				}
			}
			var result = list.ToArray();
			pool.Free(list);
			return result;
		}

		public static ContextMenuItem[] GetAllMenuForEventGraph(GraphEditor graphEditor, Vector2 mousePosition, NodeContainer value) {
			var manipulators = NodeEditorUtility.FindGraphManipulators();
			var pool = StaticListPool<ContextMenuItem>.pool;
			var list = pool.Allocate();
			foreach(var manipulator in manipulators) {
				manipulator.graphEditor = graphEditor;
				if(manipulator.IsValid(nameof(manipulator.ContextMenuForEventGraph))) {
					var menuItems = manipulator.ContextMenuForEventGraph(mousePosition, value);
					if(menuItems != null) {
						foreach(var menu in menuItems) {
							if(menu == null) continue;
							int index = list.FindLastIndex(item => menu.CompareTo(item) == 0);
							if(index < 0) {
								index = list.FindIndex(item => menu.CompareTo(item) > 0);
								if(index > 0) {
									index--;
								}
							}
							else {
								index++;
							}
							if(index >= 0 && list.Count > index) {
								list.Insert(index, menu);
							}
							else if(index == -1 && list.Count > 0) {
								list.Insert(0, menu);
							}
							else {
								list.Add(menu);
							}
						}
					}
				}
			}
			var result = list.ToArray();
			pool.Free(list);
			return result;
		}

		public static ContextMenuItem[] GetAllMenuForFunction(GraphEditor graphEditor, Vector2 mousePosition, Function value) {
			var manipulators = NodeEditorUtility.FindGraphManipulators();
			var pool = StaticListPool<ContextMenuItem>.pool;
			var list = pool.Allocate();
			foreach(var manipulator in manipulators) {
				manipulator.graphEditor = graphEditor;
				if(manipulator.IsValid(nameof(manipulator.ContextMenuForFunction))) {
					var menuItems = manipulator.ContextMenuForFunction(mousePosition, value);
					if(menuItems != null) {
						foreach(var menu in menuItems) {
							if(menu == null) continue;
							int index = list.FindLastIndex(item => menu.CompareTo(item) == 0);
							if(index < 0) {
								index = list.FindIndex(item => menu.CompareTo(item) > 0);
								if(index > 0) {
									index--;
								}
							}
							else {
								index++;
							}
							if(index >= 0 && list.Count > index) {
								list.Insert(index, menu);
							}
							else if(index == -1 && list.Count > 0) {
								list.Insert(0, menu);
							}
							else {
								list.Add(menu);
							}
						}
					}
				}
			}
			var result = list.ToArray();
			pool.Free(list);
			return result;
		}

		public static ContextMenuItem[] GetAllMenuForNode(GraphEditor graphEditor, Vector2 mousePosition, Node value) {
			var manipulators = NodeEditorUtility.FindGraphManipulators();
			var pool = StaticListPool<ContextMenuItem>.pool;
			var list = pool.Allocate();
			foreach(var manipulator in manipulators) {
				manipulator.graphEditor = graphEditor;
				if(manipulator.IsValid(nameof(manipulator.ContextMenuForNode))) {
					var menuItems = manipulator.ContextMenuForNode(mousePosition, value);
					if(menuItems != null) {
						foreach(var menu in menuItems) {
							if(menu == null) continue;
							int index = list.FindLastIndex(item => menu.CompareTo(item) == 0);
							if(index < 0) {
								index = list.FindIndex(item => menu.CompareTo(item) > 0);
								if(index > 0) {
									index--;
								}
							}
							else {
								index++;
							}
							if(index >= 0 && list.Count > index) {
								list.Insert(index, menu);
							}
							else if(index == -1 && list.Count > 0) {
								list.Insert(0, menu);
							}
							else {
								list.Add(menu);
							}
						}
					}
				}
			}
			var result = list.ToArray();
			pool.Free(list);
			return result;
		}
		#endregion

		/// <summary>
		/// Call this to mark that the graph has been changed
		/// </summary>
		protected void GraphChanged() {
			uNodeGUIUtility.GUIChanged(graph, UIChangeType.Important);
		}

		protected void ShowTypeMenu(Vector2 position, Action<Type> onClick, Type[] generalTypes = null, FilterAttribute filter = null) {
			if(generalTypes == null) {
				generalTypes = new Type[] {
					typeof(string),
					typeof(float),
					typeof(bool),
					typeof(int),
					typeof(Vector2),
					typeof(Vector3),
					typeof(Transform),
					typeof(GameObject),
					//typeof(IRuntimeClass),
					typeof(List<>),
					typeof(Dictionary<,>),
				};
			}
			if(filter == null) {
				filter = FilterAttribute.DefaultTypeFilter;
			}
			var customItems = ItemSelector.MakeCustomTypeItems(generalTypes, "General");
			var window = ItemSelector.ShowType(
				graphData.graph,
				filter, 
				(m) => {
					onClick(m.startType);
				},
				customItems).ChangePosition(position);
			window.displayNoneOption = false;
			window.displayGeneralType = false;
		}
	}

	public static class GraphManipulatorUtility {
		public static bool HandleCommand(GraphEditor graphEditor, string command) {
			var manipulators = NodeEditorUtility.FindGraphManipulators();
			foreach(var manipulator in manipulators) {
				manipulator.graphEditor = graphEditor;
				if(manipulator.IsValid(nameof(manipulator.HandleCommand))) {
					if(manipulator.HandleCommand(command)) {
						return true;
					}
				}
			}
			return false;
		}
	}

	class DefaultGraphManipulator : GraphManipulator {
		public override int order => int.MaxValue;

		public override bool IsValid(string action) {
			return true;
		}

		public override bool CreateNewVariable(Vector2 mousePosition, Action postAction) {
			ShowTypeMenu(mousePosition, type => {
				uNodeEditorUtility.RegisterUndo(graph);
				NodeEditorUtility.AddNewVariable(graphData.graphData.variableContainer, "newVariable", type, variable => {
					if(graph is IClassModifier classModifier) {
						if(classModifier.GetModifier().ReadOnly) {
							variable.modifier.ReadOnly = true;
						}
					}
					postAction?.Invoke();
					uNodeThreadUtility.Queue(() => {
						CustomInspector.Inspect(mousePosition, new GraphEditorData(graphData, variable), true);
					});
				});
			});
			return true;
		}

		public override bool CreateNewProperty(Vector2 mousePosition, Action postAction) {
			GenericMenu menu = new GenericMenu();
			var graph = graphData.graph;
			menu.AddItem(new GUIContent("Add new"), false, () => {
				ShowTypeMenu(mousePosition, type => {
					uNodeEditorUtility.RegisterUndo(graph);
					NodeEditorUtility.AddNewProperty(graphData.graphData.propertyContainer, "newProperty", type, p => {
						if(uNodePreference.preferenceData.newPropertyAccessor == uNodePreference.DefaultAccessor.Private) {
							p.modifier.SetPrivate();
						}
						postAction?.Invoke();
						uNodeThreadUtility.Queue(() => {
							CustomInspector.Inspect(mousePosition, new GraphEditorData(graphData, p), true);
						});
					});
				});
			});
			Type inheritType = null;
			if(graphData.graph is IClassGraph co) {
				inheritType = co.InheritType;
			}
			if(inheritType != null) {
				#region Override
				{
					var properties = inheritType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(delegate (PropertyInfo info) {
						if(info is not IRuntimeMember && graph is not IScriptGraphType)
							return false;
						var method = info.GetMethod ?? info.SetMethod;
						if(!method.IsAbstract && !method.IsVirtual)
							return false;
						if(method.IsStatic)
							return false;
						if(method.IsPrivate)
							return false;
						if(!method.IsPublic && !method.IsFamily)
							return false;
						if(method.IsFamilyAndAssembly)
							return false;
						if(info.IsSpecialName)
							return false;
						if(info.IsDefinedAttribute(typeof(ObsoleteAttribute)))
							return false;
						if(info.GetCustomAttributes(true).Length > 0) {
							if(info.IsDefinedAttribute(typeof(System.Runtime.ConstrainedExecution.ReliabilityContractAttribute)))
								return false;
						}
						return true;
					}).ToArray();
					foreach(var property in properties) {
						bool hasProperty = false;
						if(graph.GetProperty(property.Name)) {
							hasProperty = true;
						}
						var m = property;
						menu.AddItem(new GUIContent("Override Property/" + property.Name), hasProperty, () => {
							if(!hasProperty) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewProperty(graphData.graph.GraphData.propertyContainer, m.Name, m.PropertyType,
									(prop) => {
										var info = m.GetMethod ?? m.SetMethod;
										prop.modifier.Override = true;
										prop.modifier.Private = info.IsPrivate;
										prop.modifier.Public = info.IsPublic;
										prop.modifier.Internal = info.IsAssembly;
										prop.modifier.Protected = info.IsFamily;
										if(info.IsFamilyOrAssembly) {
											prop.modifier.Internal = true;
											prop.modifier.Protected = true;
										}
										if(m.GetMethod != null && m.SetMethod != null) {
											prop.accessor = PropertyAccessorKind.ReadWrite;
											prop.getterModifier.Protected = m.GetMethod.IsFamily;
											prop.setterModifier.Protected = m.SetMethod.IsFamily;
										}
										else if(m.GetMethod != null) {
											prop.accessor = PropertyAccessorKind.ReadOnly;
											prop.getterModifier.Protected = m.GetMethod.IsFamily;
										}
										else if(m.SetMethod != null) {
											prop.accessor = PropertyAccessorKind.WriteOnly;
											prop.setterModifier.Protected = m.SetMethod.IsFamily;
										}
										if(m.GetMethod != null) {
											prop.CreateGetter();
										}
										if(m.SetMethod != null) {
											prop.CreateSetter();
										}
									});
								GraphChanged();
							}
						});
					}
				}
				#endregion

				#region Implement Interfaces
				var interfaceSystem = graphData.graph as IInterfaceSystem;
				if(interfaceSystem != null && interfaceSystem.Interfaces.Count > 0) {
					foreach(var inter in interfaceSystem.Interfaces) {
						if(inter == null || !inter.isFilled)
							continue;
						Type t = inter.type;
						if(t != null) {
							var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							foreach(var property in properties) {
								bool flag = false;
								if(graph.GetProperty(property.Name)) {
									flag = true;
								}

								var m = property;
								menu.AddItem(new GUIContent("Interface " + t.Name + "/" + property.Name), flag, () => {
									if(!flag) {
										uNodeEditorUtility.RegisterUndo(base.graph);
										NodeEditorUtility.AddNewProperty(graphData.graph.GraphData.propertyContainer, m.Name, m.PropertyType, p => {
											if(property.CanRead == false) {
												p.CreateSetter();
											}
											if(property.CanWrite == false) {
												p.CreateGetter();
											}
										});
										GraphChanged();
									}
								});
							}
						}
					}
				}
				#endregion
			}
			menu.ShowAsContext();
			return true;
		}

		public override bool CreateNewGraph(Vector2 mousePosition, Action postAction) {
			var graph = graphData.graph;
			if(graph is IGraphWithEventGraph eventGraph) {
				var supportedEventGraph = eventGraph.SupportedEventGraphs;
				if(supportedEventGraph != null) {
					GenericMenu menu = new GenericMenu();
					foreach(var type in EditorReflectionUtility.GetDefinedTypes<EventGraphAttribute>()) {
						var att = type.GetCustomAttribute<EventGraphAttribute>();
						if(supportedEventGraph.Contains(att.name)) {
							menu.AddItem(new GUIContent("Add new: " + att.name), false, () => {
								var value = ReflectionUtils.CreateInstance(type);
								if(value is NodeContainer container) {
									uNodeEditorUtility.RegisterUndo(graph);
									container.name = att.name;
									container.SetParent(graphData.graphData.eventGraphContainer);
									postAction?.Invoke();
									GraphChanged();
								}
								else {
									throw null;
								}
							});
						}
					}
					if(menu.GetItemCount() > 0) {
						menu.ShowAsContext();
					}
				}
			}
			return true;
		}

		public override bool CreateNewLocalVariable(Vector2 mousePosition, Action postAction) {
			ShowTypeMenu(mousePosition, type => {
				uNodeEditorUtility.RegisterUndo(graph);
				var variable = graphData.selectedRoot.variableContainer.AddVariable("localVariable", type);
				postAction?.Invoke();
				uNodeThreadUtility.Queue(() => {
					CustomInspector.Inspect(mousePosition, new GraphEditorData(graphData, variable), true);
				});
			});
			return true;
		}

		public override bool CreateNewFunction(Vector2 mousePosition, Action postAction) {
			GenericMenu menu = new GenericMenu();
			var graph = graphData.graph;
			menu.AddItem(new GUIContent("Add new"), false, () => {
				uNodeEditorUtility.RegisterUndo(base.graph);
				NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "NewFunction", typeof(void), f => {
					if(uNodePreference.preferenceData.newFunctionAccessor == uNodePreference.DefaultAccessor.Private) {
						f.modifier.SetPrivate();
					}
					uNodeThreadUtility.Queue(() => {
						CustomInspector.Inspect(mousePosition, new GraphEditorData(graphData, f), true);
					});
				});
				GraphChanged();
			});
			menu.AddItem(new GUIContent("Add new coroutine"), false, () => {
				uNodeEditorUtility.RegisterUndo(base.graph);
				NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "NewFunction", typeof(IEnumerator), f => {
					if(uNodePreference.preferenceData.newFunctionAccessor == uNodePreference.DefaultAccessor.Private) {
						f.modifier.SetPrivate();
					}
					uNodeThreadUtility.Queue(() => {
						CustomInspector.Inspect(mousePosition, new GraphEditorData(graphData, f), true);
					});
				});
				GraphChanged();
			});
			Type inheritType = null;
			if(graphData.graph is IClassGraph co) {
				inheritType = co.InheritType;
			}
			if(inheritType != null) {
				#region UnityEvent
				if(typeof(MonoBehaviour).IsAssignableFrom(inheritType)) {
					menu.AddSeparator("");
					{//Start Event
						bool hasFunction = false;
						if(graph.GetFunction("Start", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/Start()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "Start", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//Awake Event
						bool hasFunction = false;
						if(graph.GetFunction("Awake", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/Awake()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "Awake", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnDestroy Event
						bool hasFunction = false;
						if(graph.GetFunction("OnDestroy", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/OnDestroy()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnDestroy", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnDisable Event
						bool hasFunction = false;
						if(graph.GetFunction("OnDisable", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/OnDisable()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnDisable", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnEnable Event
						bool hasFunction = false;
						if(graph.GetFunction("OnEnable", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Behavior/OnEnable()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnEnable", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//Update Event
						bool hasFunction = false;
						if(graph.GetFunction("Update", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Gameloop/Update()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "Update", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//FixedUpdate Event
						bool hasFunction = false;
						if(graph.GetFunction("FixedUpdate", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Gameloop/FixedUpdate()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "FixedUpdate", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//LateUpdate Event
						bool hasFunction = false;
						if(graph.GetFunction("LateUpdate", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Gameloop/LateUpdate()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "LateUpdate", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnGUI Event
						bool hasFunction = false;
						if(graph.GetFunction("OnGUI", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Gameloop/OnGUI()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnGUI", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnAnimatorIK Event
						bool hasFunction = false;
						if(graph.GetFunction("OnAnimatorIK", 0, typeof(int))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Animation/OnAnimatorIK(int)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnAnimatorIK", typeof(void), new string[] { "layerIndex" }, new Type[] { typeof(int) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnAnimatorMove Event
						bool hasFunction = false;
						if(graph.GetFunction("OnAnimatorMove", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Animation/OnAnimatorMove()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnAnimatorMove", typeof(void), f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnApplicationFocus Event
						bool hasFunction = false;
						if(graph.GetFunction("OnApplicationFocus", 0, typeof(bool))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Game Event/OnApplicationFocus(bool)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnApplicationFocus", typeof(void), new string[] { "focusStatus" }, new Type[] { typeof(bool) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnApplicationPause Event
						bool hasFunction = false;
						if(graph.GetFunction("OnApplicationPause", 0, typeof(bool))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Game Event/OnApplicationPause(bool)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnApplicationPause", typeof(void), new string[] { "pauseStatus" }, new Type[] { typeof(bool) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnApplicationQuit Event
						bool hasFunction = false;
						if(graph.GetFunction("OnApplicationQuit", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Game Event/OnApplicationQuit()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnApplicationQuit", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionEnter Event
						bool hasFunction = false;
						if(graph.GetFunction("OnCollisionEnter", 0, typeof(Collision))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionEnter(Collision)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionEnter", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionEnter2D Event
						bool hasFunction = false;
						if(graph.GetFunction("OnCollisionEnter2D", 0, typeof(Collision2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionEnter2D(Collision2D)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionEnter2D", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionExit Event
						bool hasFunction = false;
						if(graph.GetFunction("OnCollisionExit", 0, typeof(Collision))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionExit(Collision)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionExit", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionExit2D Event
						bool hasFunction = false;
						if(graph.GetFunction("OnCollisionExit2D", 0, typeof(Collision2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionExit2D(Collision2D)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionExit2D", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionStay Event
						bool hasFunction = false;
						if(graph.GetFunction("OnCollisionStay", 0, typeof(Collision))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionStay(Collision)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionStay", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnCollisionStay2D Event
						bool hasFunction = false;
						if(graph.GetFunction("OnCollisionStay2D", 0, typeof(Collision2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnCollisionStay2D(Collision2D)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnCollisionStay2D", typeof(void), new string[] { "collisionInfo" }, new Type[] { typeof(Collision2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnParticleCollision Event
						bool hasFunction = false;
						if(graph.GetFunction("OnParticleCollision", 0, typeof(GameObject))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnParticleCollision(GameObject)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnParticleCollision", typeof(void), new string[] { "other" }, new Type[] { typeof(GameObject) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerEnter Event
						bool hasFunction = false;
						if(graph.GetFunction("OnTriggerEnter", 0, typeof(Collider))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerEnter(Collider)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerEnter", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerEnter2D Event
						bool hasFunction = false;
						if(graph.GetFunction("OnTriggerEnter2D", 0, typeof(Collider2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerEnter2D(Collider2D)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerEnter2D", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerExit Event
						bool hasFunction = false;
						if(graph.GetFunction("OnTriggerExit", 0, typeof(Collider))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerExit(Collider)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerExit", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerExit2D Event
						bool hasFunction = false;
						if(graph.GetFunction("OnTriggerExit2D", 0, typeof(Collider2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerExit2D(Collider2D)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerExit2D", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerStay Event
						bool hasFunction = false;
						if(graph.GetFunction("OnTriggerStay", 0, typeof(Collider))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerStay(Collider)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerStay", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTriggerStay2D Event
						bool hasFunction = false;
						if(graph.GetFunction("OnTriggerStay2D", 0, typeof(Collider2D))) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Physics/OnTriggerStay2D(Collider2D)"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTriggerStay2D", typeof(void), new string[] { "colliderInfo" }, new Type[] { typeof(Collider2D) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTransformChildrenChanged Event
						bool hasFunction = false;
						if(graph.GetFunction("OnTransformChildrenChanged", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Transfrom/OnTransformChildrenChanged()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTransformChildrenChanged", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnTransformParentChanged Event
						bool hasFunction = false;
						if(graph.GetFunction("OnTransformParentChanged", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Transfrom/OnTransformParentChanged()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnTransformParentChanged", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseDown Event
						bool hasFunction = false;
						if(graph.GetFunction("OnMouseDown", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseDown()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseDown", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseDrag Event
						bool hasFunction = false;
						if(graph.GetFunction("OnMouseDrag", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseDrag()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseDrag", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseEnter Event
						bool hasFunction = false;
						if(graph.GetFunction("OnMouseEnter", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseEnter()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseEnter", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseExit Event
						bool hasFunction = false;
						if(graph.GetFunction("OnMouseExit", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseExit()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseExit", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseOver Event
						bool hasFunction = false;
						if(graph.GetFunction("OnMouseOver", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseOver()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseOver", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseUp Event
						bool hasFunction = false;
						if(graph.GetFunction("OnMouseUp", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseUp()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseUp", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnMouseUpAsButton Event
						bool hasFunction = false;
						if(graph.GetFunction("OnMouseUpAsButton", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Mouse/OnMouseUpAsButton()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnMouseUpAsButton", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnBecameInvisible Event
						bool hasFunction = false;
						if(graph.GetFunction("OnBecameInvisible", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnBecameInvisible()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnBecameInvisible", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnBecameVisible Event
						bool hasFunction = false;
						if(graph.GetFunction("OnBecameVisible", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnBecameVisible()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnBecameVisible", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnPostRender Event
						bool hasFunction = false;
						if(graph.GetFunction("OnPostRender", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnPostRender()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnPostRender", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnPreCull Event
						bool hasFunction = false;
						if(graph.GetFunction("OnPreCull", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnPreCull()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnPreCull", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnPreRender Event
						bool hasFunction = false;
						if(graph.GetFunction("OnPreRender", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnPreRender()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnPreRender", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnRenderObject Event
						bool hasFunction = false;
						if(graph.GetFunction("OnRenderObject", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnRenderObject()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnRenderObject", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnRenderImage Event
						bool hasFunction = false;
						if(graph.GetFunction("OnRenderImage", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnRenderImage()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnRenderImage", typeof(void), new[] { "src", "dest" }, new[] { typeof(RenderTexture), typeof(RenderTexture) }, action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					{//OnWillRenderObject Event
						bool hasFunction = false;
						if(graph.GetFunction("OnWillRenderObject", 0)) {
							hasFunction = true;
						}
						menu.AddItem(new GUIContent("UnityEvent/Renderer/OnWillRenderObject()"), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnWillRenderObject", typeof(void), action: f => f.modifier.SetPrivate());
								GraphChanged();
							}
						});
					}
					//if(editorData.graph is uNodeClass) 
					{
						{//OnDrawGizmos Event
							bool hasFunction = false;
							if(graph.GetFunction("OnDrawGizmos", 0)) {
								hasFunction = true;
							}
							menu.AddItem(new GUIContent("UnityEvent/Editor/OnDrawGizmos()"), hasFunction, () => {
								if(!hasFunction) {
									uNodeEditorUtility.RegisterUndo(base.graph);
									NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnDrawGizmos", typeof(void), action: f => f.modifier.SetPrivate());
									GraphChanged();
								}
							});
						}
						{//OnDrawGizmosSelected Event
							bool hasFunction = false;
							if(graph.GetFunction("OnDrawGizmosSelected", 0)) {
								hasFunction = true;
							}
							menu.AddItem(new GUIContent("UnityEvent/Editor/OnDrawGizmosSelected()"), hasFunction, () => {
								if(!hasFunction) {
									uNodeEditorUtility.RegisterUndo(base.graph);
									NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnDrawGizmosSelected", typeof(void), action: f => f.modifier.SetPrivate());
									GraphChanged();
								}
							});
						}
						{//OnValidate Event
							bool hasFunction = false;
							if(graph.GetFunction("OnValidate", 0)) {
								hasFunction = true;
							}
							menu.AddItem(new GUIContent("UnityEvent/Editor/OnValidate()"), hasFunction, () => {
								if(!hasFunction) {
									uNodeEditorUtility.RegisterUndo(base.graph);
									NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "OnValidate", typeof(void), action: f => f.modifier.SetPrivate());
									GraphChanged();
								}
							});
						}
						{//Reset Event
							bool hasFunction = false;
							if(graph.GetFunction("Reset", 0)) {
								hasFunction = true;
							}
							menu.AddItem(new GUIContent("UnityEvent/Editor/Reset()"), hasFunction, () => {
								if(!hasFunction) {
									uNodeEditorUtility.RegisterUndo(base.graph);
									NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, "Reset", typeof(void), action: f => f.modifier.SetPrivate());
									GraphChanged();
								}
							});
						}
					}
				}
				#endregion

				#region Override
				{
					MethodInfo[] methods = inheritType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(delegate (MethodInfo info) {
						if(info is not IRuntimeMember && graph is not IScriptGraphType)
							return false;
						if(!info.IsAbstract && !info.IsVirtual)
							return false;
						if(info.IsStatic)
							return false;
						if(info.IsSpecialName)
							return false;
						if(info.IsPrivate)
							return false;
						if(info.IsConstructor)
							return false;
						if(info.Name.StartsWith("get_", StringComparison.Ordinal))
							return false;
						if(info.Name.StartsWith("set_", StringComparison.Ordinal))
							return false;
						if(info.ContainsGenericParameters)
							return false;
						if(!info.IsPublic && !info.IsFamily)
							return false;
						if(info.IsFamilyAndAssembly)
							return false;
						if(info.IsDefinedAttribute(typeof(ObsoleteAttribute)))
							return false;
						if(info.GetCustomAttributes(true).Length > 0) {
							if(info.IsDefinedAttribute(typeof(System.Runtime.ConstrainedExecution.ReliabilityContractAttribute)))
								return false;
						}
						return true;
					}).ToArray();
					foreach(var method in methods) {
						bool hasFunction = false;
						if(graph.GetFunction(method.Name, method.GetGenericArguments().Length, method.GetParameters().Select(item => item.ParameterType).ToArray())) {
							hasFunction = true;
						}
						var m = method;
						menu.AddItem(new GUIContent("Override Function/" + EditorReflectionUtility.GetPrettyMethodName(method)), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, m.Name, m.ReturnType,
									m.GetParameters().Select(item => new ParameterData(item)).ToArray(),
									m.GetGenericArguments().Select(item => item.Name).ToArray(),
									(function) => {
										function.modifier.Override = true;
										function.modifier.Private = m.IsPrivate;
										function.modifier.Public = m.IsPublic;
										function.modifier.Internal = m.IsAssembly;
										function.modifier.Protected = m.IsFamily;
										if(m.IsFamilyOrAssembly) {
											function.modifier.Internal = true;
											function.modifier.Protected = true;
										}
										function.Entry.EnsureRegistered();
										NodeEditorUtility.AddNewNode<NodeBaseCaller>(function, new Vector2(0, 100), baseNode => {
											baseNode.target = MemberData.CreateFromMember(m);
											baseNode.EnsureRegistered();
											if(function.ReturnType() == typeof(void)) {
												function.Entry.exit.ConnectTo(baseNode.enter);
											}
											else {
												NodeEditorUtility.AddNewNode<Nodes.NodeReturn>(function, new Vector2(0, 100), returnNode => {
													baseNode.position = new Rect(-200, 100, 0, 0);
													returnNode.value.ConnectTo(baseNode.output);
													function.Entry.exit.ConnectTo(returnNode.enter);
												});
											}
										});
									});
								GraphChanged();
							}
						});
					}
				}
				#endregion

				#region Hide Function
				if(graph is IScriptGraphType) {
					MethodInfo[] methods = inheritType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(delegate (MethodInfo info) {
						if(info.IsStatic)
							return false;
						if(info.IsPrivate)
							return false;
						if(info.IsAbstract)
							return false;
						if(info.IsConstructor)
							return false;
						if(info.IsSpecialName)
							return false;
						if(info.Name.StartsWith("get_", StringComparison.Ordinal))
							return false;
						if(info.Name.StartsWith("set_", StringComparison.Ordinal))
							return false;
						if(info.ContainsGenericParameters)
							return false;
						if(!info.IsPublic && !info.IsFamily)
							return false;
						if(info.IsDefinedAttribute(typeof(ObsoleteAttribute)))
							return false;
						if(info.GetCustomAttributes(true).Length > 0) {
							if(info.IsDefinedAttribute(typeof(System.Runtime.ConstrainedExecution.ReliabilityContractAttribute)))
								return false;
						}
						return true;
					}).ToArray();
					foreach(var method in methods) {
						bool hasFunction = false;
						if(graph.GetFunction(method.Name, method.GetGenericArguments().Length,
							method.GetParameters()
							.Select(item => item.ParameterType).ToArray())) {
							hasFunction = true;
						}
						var m = method;
						menu.AddItem(new GUIContent("Hide Function/" + EditorReflectionUtility.GetPrettyMethodName(method)), hasFunction, () => {
							if(!hasFunction) {
								uNodeEditorUtility.RegisterUndo(base.graph);
								NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, m.Name, m.ReturnType,
									m.GetParameters().Select(item => new ParameterData(item)).ToArray(),
									m.GetGenericArguments().Select(item => item.Name).ToArray(),
									(function) => {
										function.modifier.New = true;
										function.modifier.Private = m.IsPrivate;
										function.modifier.Public = m.IsPublic;
										function.modifier.Internal = m.IsAssembly;
										function.modifier.Protected = m.IsFamily;
										if(m.IsFamilyOrAssembly) {
											function.modifier.Internal = true;
											function.modifier.Protected = true;
										}
									});
								GraphChanged();
							}
						});
					}
				}
				#endregion

				#region Implement Interfaces
				var interfaceSystem = graphData.graph as IInterfaceSystem;
				if(interfaceSystem != null && interfaceSystem.Interfaces.Count > 0) {
					foreach(var inter in interfaceSystem.Interfaces) {
						if(inter == null || !inter.isFilled)
							continue;
						Type t = inter.type;
						if(t != null) {
							MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(delegate (MethodInfo info) {
								if(info.Name.StartsWith("get_", StringComparison.Ordinal))
									return false;
								if(info.Name.StartsWith("set_", StringComparison.Ordinal))
									return false;
								return true;
							}).ToArray();
							foreach(var method in methods) {
								bool hasFunction = false;
								if(graph.GetFunction(method.Name, method.GetGenericArguments().Length,
									method.GetParameters()
									.Select(item => item.ParameterType).ToArray())) {
									hasFunction = true;
								}

								var m = method;
								menu.AddItem(new GUIContent("Interface " + t.Name + "/" + EditorReflectionUtility.GetPrettyMethodName(method)), hasFunction, () => {
									if(!hasFunction) {
										uNodeEditorUtility.RegisterUndo(base.graph);
										NodeEditorUtility.AddNewFunction(graphData.graph.GraphData.functionContainer, m.Name, m.ReturnType,
											m.GetParameters().Select(item => new ParameterData(item)).ToArray(),
											m.GetGenericArguments().Select(item => item.Name).ToArray());
										GraphChanged();
									}
								});
							}
						}
					}
				}
				#endregion
			}
			menu.ShowAsContext();
			return true;
		}

		public override bool CreateNewClass(Vector2 mousePosition, Action postAction) {
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Class"), false, () => {
				if(tabData.owner is IScriptGraph scriptGraph) {
					var newAsset = ScriptableObject.CreateInstance<ClassScript>();
					if(scriptGraph.TypeList.references.Count > 0) {
						newAsset.name = "newClass";
					}
					uNodeEditorUtility.RegisterUndo(tabData.owner);
					AssetDatabase.AddObjectToAsset(newAsset, tabData.owner);
					scriptGraph.TypeList.AddType(newAsset, scriptGraph);
					AssetDatabase.SaveAssetIfDirty(tabData.owner);
					postAction?.Invoke();
				}
				else {
					throw new InvalidOperationException();
				}
			});
			menu.AddItem(new GUIContent("Struct"), false, () => {
				if(tabData.owner is IScriptGraph scriptGraph) {
					var newAsset = ScriptableObject.CreateInstance<ClassScript>();
					newAsset.inheritType = typeof(ValueType);
					if(scriptGraph.TypeList.references.Count > 0) {
						newAsset.name = "newStruct";
					}
					uNodeEditorUtility.RegisterUndo(tabData.owner);
					AssetDatabase.AddObjectToAsset(newAsset, tabData.owner);
					scriptGraph.TypeList.AddType(newAsset, scriptGraph);
					AssetDatabase.SaveAssetIfDirty(tabData.owner);
				}
				else {
					throw new InvalidOperationException();
				}
				postAction?.Invoke();
			});
			if(tabData.owner is IScriptGraph scriptGraph) {
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Interface"), false, () => {
					var newAsset = ScriptableObject.CreateInstance<InterfaceScript>();
					if(scriptGraph.TypeList.references.Count > 0) {
						newAsset.name = "newInterface";
					}
					uNodeEditorUtility.RegisterUndo(tabData.owner);
					AssetDatabase.AddObjectToAsset(newAsset, tabData.owner);
					scriptGraph.TypeList.AddType(newAsset, scriptGraph);
					AssetDatabase.SaveAssetIfDirty(tabData.owner);
					postAction?.Invoke();
				});
				menu.AddItem(new GUIContent("Enum"), false, () => {
					var newAsset = ScriptableObject.CreateInstance<EnumScript>();
					if(scriptGraph.TypeList.references.Count > 0) {
						newAsset.name = "newEnum";
					}
					uNodeEditorUtility.RegisterUndo(tabData.owner);
					AssetDatabase.AddObjectToAsset(newAsset, tabData.owner);
					scriptGraph.TypeList.AddType(newAsset, scriptGraph);
					AssetDatabase.SaveAssetIfDirty(tabData.owner);
					postAction?.Invoke();
				});
			}
			menu.ShowAsContext();
			return true;
		}

		public override IEnumerable<ContextMenuItem> ContextMenuForVariable(Vector2 mousePosition, Variable variable) {
			if(graph is IGraphWithProperties) {
				yield return new DropdownMenuAction($"Encapsulate variable: '{variable.name}' and use property", evt => {
					if(variable.modifier.isPublic) {
						if(EditorUtility.DisplayDialog("", "Are you sure to encapsulate public variable? when encapsulate public variable, the variable will became private", "Yes", "Cancel") == false) {
							return;
						}
					}
					NodeEditorUtility.AddNewProperty(graphData.graphData.propertyContainer, uNodeUtility.AutoCorrectName(ObjectNames.NicifyVariableName(variable.name)), variable.type, p => {
						p.CreateGetter();
						{
							var root = p.getRoot;
							var returnNode = root.AddChildNode(new Nodes.NodeReturn());
							returnNode.EnsureRegistered();
							returnNode.value.AssignToDefault(MemberData.CreateFromValue(variable));
							root.Entry.exit.ConnectTo(returnNode.enter);
						}
						p.CreateSetter();
						{
							var root = p.setRoot;
							var setNode = root.AddChildNode(new Nodes.NodeSetValue());
							setNode.EnsureRegistered();
							setNode.target.AssignToDefault(MemberData.CreateFromValue(variable));
							setNode.value.AssignToDefault(MemberData.CreateFromValue(new ParameterRef(root, root.parameters[0])));
							root.Entry.exit.ConnectTo(setNode.enter);
						}
						uNodeThreadUtility.Queue(() => {
							CustomInspector.Inspect(mousePosition, new GraphEditorData(graphData, p), true);
						});
					});
				}, DropdownMenuAction.AlwaysEnabled);
			}
			yield break;
		}

		public override IEnumerable<ContextMenuItem> ContextMenuForGraph(Vector2 mousePosition) {
			if(graph != null) {
				if(graph is IClassGraph) {
					yield return new DropdownMenuAction("Show Inheritors", evt => {
						GraphUtility.ShowGraphInheritanceHeirarchy(graph);
					}, DropdownMenuAction.AlwaysEnabled);
				}
				var converters = GraphUtility.FindGraphConverters();
				var current = graph;
				for(int x = 0; x < converters.Count; x++) {
					var converter = converters[x];
					if(!converter.IsValid(graph)) continue;
					yield return new DropdownMenuAction("Convert/" + converter.GetMenuName(current), evt => {
						converter.Convert(current);
						uNodeGUIUtility.GUIChangedMajor(null);
					}, DropdownMenuAction.AlwaysEnabled);
				}
			}
			yield break;
		}

		public override IEnumerable<ContextMenuItem> ContextMenuForGraphCanvas(Vector2 mousePosition) {
			if(graphEditor.canvasData.ShowAddNodeContextMenu) {
				yield return new ContextMenuItem("Add Node", evt => {
					graphEditor.ShowNodeMenu(mousePosition);
				}, int.MinValue);

				yield return new ContextMenuItem("Add Node (Set)", evt => {
					graphEditor.ShowNodeMenu(mousePosition, new FilterAttribute() { SetMember = true, VoidType = false }, (node) => {
						node.EnsureRegistered();
						NodeEditorUtility.AddNewNode(graphData, new Vector2(node.nodeObject.position.x, node.position.y), delegate (Nodes.NodeSetValue n) {
							n.EnsureRegistered();
							NodeEditorUtility.ConnectPort(n.target, node.nodeObject.primaryValueOutput);
							n.value.AssignToDefault(MemberData.Default(n.target.type));
						});
						node.nodeObject.SetPosition(new Vector2(node.position.x - 150, node.position.y - 100));
					});
				}, int.MinValue);
				yield return new ContextMenuItem("Add Node (Favorites)", evt => {
					graphEditor.ShowFavoriteMenu(mousePosition);
				}, int.MinValue);
			}

			if(graphEditor.canvasData.SupportMacro) {
				yield return new ContextMenuItem("Add Linked Macro", evt => {
					var macros = GraphUtility.FindGraphs<MacroGraph>();
					List<ItemSelector.CustomItem> customItems = new List<ItemSelector.CustomItem>();
					foreach(var macro in macros) {
						var m = macro;
						customItems.Add(ItemSelector.CustomItem.Create(
							m.GetGraphName(),
							() => {
								graphEditor.CreateLinkedMacro(m, mousePosition);
							},
							m.category,
							icon: uNodeEditorUtility.GetTypeIcon(m.GetIcon()),
							tooltip: new GUIContent(m.GraphData.comment)));
					}
					ItemSelector.ShowWindow(null, null, null, customItems).ChangePosition(graphEditor.GetMenuPosition()).displayDefaultItem = false;
				}, int.MinValue);
			}


			#region Event & State
			if(graphData.currentCanvas is MainGraphContainer) {
				if(graphData.graph is IStateGraph state && state.CanCreateStateGraph) {
					yield return ContextMenuItem.CreateSeparator(order: int.MinValue);
					yield return new ContextMenuItem("Add State", evt => {
						NodeEditorUtility.AddNewNode<Nodes.StateNode>(graphData,
							"State",
							mousePosition);
						graphEditor.Refresh();
					}, int.MinValue);
					//Add events
					var eventMenus = NodeEditorUtility.FindEventMenu();
					foreach(var menu in eventMenus) {
						if(menu.IsValidScopes(NodeScope.StateGraph)) {
							yield return new ContextMenuItem("Add Event/" + (string.IsNullOrEmpty(menu.category) ? menu.name : menu.category + "/" + menu.name), (e) => {
								NodeEditorUtility.AddNewNode<Node>(graphData, menu.nodeName, menu.type, mousePosition);
								graphEditor.Refresh();
							}, int.MinValue);
						}
					}
				}
				else if(graphData.graph is ICustomMainGraph mainGraph) {
					yield return ContextMenuItem.CreateSeparator(order: int.MinValue);

					var scopes = mainGraph.MainGraphScope.Split(',');

					//Add events
					var eventMenus = NodeEditorUtility.FindEventMenu();
					foreach(var menu in eventMenus) {
						if(menu.IsValidScopes(scopes)) {
							yield return new ContextMenuItem("Add Event/" + (string.IsNullOrEmpty(menu.category) ? menu.name : menu.category + "/" + menu.name), (e) => {
								NodeEditorUtility.AddNewNode<Node>(graphData, menu.nodeName, menu.type, mousePosition);
								graphEditor.Refresh();
							}, int.MinValue);
						}
					}
				}
			}
			else if(graphData.currentCanvas is NodeObject superNode) {

				if(superNode.node is INodeWithEventHandler) {
					yield return ContextMenuItem.CreateSeparator(order: int.MinValue);

					#region Add Event
					var eventMenus = NodeEditorUtility.FindEventMenu();
					foreach(var menu in eventMenus) {
						if(menu.type.IsDefinedAttribute<StateEventAttribute>()) {
							if(superNode.node is IStateTransitionNode stateTransition) {
								if(stateTransition.StateNode is Nodes.AnyStateNode) {
									if(menu.type == typeof(Nodes.StateOnEnterEvent)) continue;
									if(menu.type == typeof(Nodes.StateOnExitEvent)) continue;
								}
							}
							yield return new ContextMenuItem("Add Event/" + (string.IsNullOrEmpty(menu.category) ? menu.name : menu.category + "/" + menu.name), (e) => {
								NodeEditorUtility.AddNewNode<Node>(graphData, menu.nodeName, menu.type, mousePosition);
								graphEditor.Refresh();
							}, int.MinValue);
						}
					}
					#endregion
				}
				if(superNode.node is Nodes.ScriptState) {
					yield return new ContextMenuItem("Add Trigger Transition", evt => {
						NodeEditorUtility.AddNewNode<Nodes.TriggerStateTransition>(graphData,
							"Trigger",
							mousePosition);
						graphEditor.Refresh();
					}, int.MinValue);
				}
			}
			if(graphData.scopes.Contains(StateGraphContainer.Scope)) {
				yield return new ContextMenuItem("Add State", evt => {
					NodeEditorUtility.AddNewNode<Nodes.ScriptState>(graphData,
						"State",
						mousePosition);
					graphEditor.Refresh();
				}, int.MinValue);
				yield return new ContextMenuItem("Add Any State", evt => {
					NodeEditorUtility.AddNewNode<Nodes.AnyStateNode>(graphData,
						"AnyState",
						mousePosition);
					graphEditor.Refresh();
				}, int.MinValue);
				yield return new ContextMenuItem("Add Nested State", evt => {
					NodeEditorUtility.AddNewNode<Nodes.NestedStateNode>(graphData,
						"NestedState",
						mousePosition);
					graphEditor.Refresh();
				}, int.MinValue);
			}
			#endregion

			#region Add Region
			yield return ContextMenuItem.CreateSeparator(order: int.MinValue);
			yield return new ContextMenuItem("Add Region", (e) => {
				graphEditor.SelectionAddRegion(mousePosition);
			}, int.MinValue);
			#endregion

			#region Add Notes
			yield return new ContextMenuItem("Add Note", (e) => {
				Rect rect = new Rect(mousePosition.x, mousePosition.y, 200, 130);
				NodeEditorUtility.AddNewNode<Nodes.StickyNote>(graphData, mousePosition, (node) => {
					node.nodeObject.name = "Title";
					node.nodeObject.comment = "type something here";
					node.position = rect;
				});
				graphEditor.Refresh();
			}, int.MinValue);
			#endregion

			#region Add Await
			if(graphData.selectedRoot is Function) {
				var func = graphData.selectedRoot as Function;
				if(func.modifier.Async) {
					yield return new ContextMenuItem("Add Await", (e) => {
						Rect rect = new Rect(mousePosition.x, mousePosition.y, 200, 130);
						NodeEditorUtility.AddNewNode<Nodes.AwaitNode>(graphData, mousePosition, (node) => {
							node.position = rect;
						});
						graphEditor.Refresh();
					}, int.MinValue);
				}
			}
			#endregion

			#region Return & Jump
			if(graphData.selectedRoot is BaseFunction) {
				yield return new ContextMenuItem("Jump Statement/Add Return", (e) => {
					var selectedNodes = graphData.selectedNodes.ToArray();
					Rect rect = selectedNodes.Length > 0 ? NodeEditorUtility.GetNodeRect(selectedNodes) : new Rect(mousePosition.x, mousePosition.y, 200, 130);
					NodeEditorUtility.AddNewNode<Nodes.NodeReturn>(graphData, mousePosition, (node) => {
						rect.x -= 30;
						rect.y -= 50;
						rect.width += 60;
						rect.height += 70;
						node.position = rect;
					});
					graphEditor.Refresh();
				}, int.MinValue);
				yield return new ContextMenuItem("Jump Statement/Add Break", (e) => {
					var selectedNodes = graphData.selectedNodes.ToArray();
					Rect rect = selectedNodes.Length > 0 ? NodeEditorUtility.GetNodeRect(selectedNodes) : new Rect(mousePosition.x, mousePosition.y, 200, 130);
					NodeEditorUtility.AddNewNode<Nodes.NodeBreak>(graphData, mousePosition, (node) => {
						rect.x -= 30;
						rect.y -= 50;
						rect.width += 60;
						rect.height += 70;
						node.position = rect;
					});
					graphEditor.Refresh();
				}, int.MinValue);
				yield return new ContextMenuItem("Jump Statement/Add Continue", (e) => {
					var selectedNodes = graphData.selectedNodes.ToArray();
					Rect rect = selectedNodes.Length > 0 ? NodeEditorUtility.GetNodeRect(selectedNodes) : new Rect(mousePosition.x, mousePosition.y, 200, 130);
					NodeEditorUtility.AddNewNode<Nodes.NodeContinue>(graphData, mousePosition, (node) => {
						rect.x -= 30;
						rect.y -= 50;
						rect.width += 60;
						rect.height += 70;
						node.position = rect;
					});
					graphEditor.Refresh();
				}, int.MinValue);
			}
			#endregion

		}

		public override IEnumerable<ContextMenuItem> ContextMenuForNode(Vector2 mousePosition, Node node) {
			#region MultipurposeNode
			if(node is MultipurposeNode) {
				MultipurposeNode mNode = node as MultipurposeNode;
				if(mNode.target.isAssigned) {
					if(mNode.target.isDeepTarget) {
						var members = mNode.target.GetMembers(false);
						if(members != null && members.Length > 0) {
							yield return new DropdownMenuAction("Split Nodes", (e) => {
								int choice = EditorUtility.DisplayDialogComplex("", "Did you want to replace the node?", "Yes", "No", "Cancel");
								if(choice != 2) {
									uNodeEditorUtility.RegisterUndo(mNode, "Split nodes: " + mNode.GetTitle());

									List<NodeObject> nodes = new List<NodeObject>();
									var member = mNode.target;
									int index = 0;

									void AssignParameters(MultipurposeNode n) {
										if(n.parameters != null && n.parameters.Count > 0) {
											for(int x = 0; x < n.parameters.Count; x++) {
												var param = mNode.parameters[x + index];
												if(param.input != null) {
													n.parameters[x].input.AssignToDefault(param.input);
												}
												else if(param.output != null) {
													foreach(var port in param.output.GetConnectedPorts().ToArray()) {
														if(port == null) continue;
														n.parameters[x].output.ConnectTo(port);
													}
												}
												else {
													throw null;
												}
											}
											index += n.parameters.Count;
										}
									}

									if(member.IsTargetingUNode) {
										NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, mousePosition, n => {
											var val = member.startItem.GetReferenceValue();
											if(val is Variable) {
												n.target = MemberData.CreateFromValue(val as Variable);
											}
											else if(val is Function) {
												n.target = MemberData.CreateFromValue(val as Function);
											}
											else if(val is Property) {
												n.target = MemberData.CreateFromValue(val as Property);
											}
											n.useOutputParameters = mNode.useOutputParameters;
											n.Register();
											AssignParameters(n);
											nodes.Add(n);
										});
									}
									for(int i = 0; i < members.Length; i++) {
										NodeEditorUtility.AddNewNode<MultipurposeNode>(graphData, mousePosition, n => {
											n.target = MemberData.CreateFromMember(members[i]);
											n.Register();
											AssignParameters(n);
											if(nodes.Count > 0) {
												n.instance?.ConnectTo(nodes.Last().primaryValueOutput);
												if(i == members.Length - 1) {
													if(mNode.exit != null && mNode.exit.GetTargetFlow() != null) {
														n.exit.ConnectTo(mNode.exit.GetTargetFlow());
													}
												}
											}
											else {
												n.instance?.AssignToDefault(mNode.instance);
											}
											nodes.Add(n);
										});
									}
									for(int i = 0; i < nodes.Count; i++) {
										nodes[(nodes.Count - i - 1)].position = node.position;
										nodes[(nodes.Count - i - 1)].position.x -= i * (nodes[i].position.width + 50);
										nodes[(nodes.Count - i - 1)].position.y += i * (nodes[i].position.height + 50);
									}
									if(choice == 0) {
										if(mNode.output != null) {
											foreach(var port in mNode.output.GetConnectedPorts().ToArray()) {
												port.ConnectTo(nodes.Last().primaryValueOutput);
											}
										}
										if(mNode.enter != null) {
											foreach(var port in mNode.enter.GetConnectedPorts().ToArray()) {
												port.ConnectTo(nodes.Last().primaryFlowInput);
											}
										}
										mNode.nodeObject.Destroy();
									}
									graphEditor.Refresh();
								}
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}
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
								yield return new DropdownMenuAction("Overrides/" + EditorReflectionUtility.GetPrettyMethodName(m), (e) => {
									object[] objs = e.userData as object[];
									MultipurposeNode nod = objs[0] as MultipurposeNode;
									MethodInfo method = objs[1] as MethodInfo;
									if(member != m) {
										if(method.IsGenericMethodDefinition) {
											TypeBuilderWindow.Show(graphEditor.topMousePos, graphData.currentCanvas, new FilterAttribute() { UnityReference = false }, delegate (MemberData[] types) {
												uNodeEditorUtility.RegisterUndo(nod.GetUnityObject());
												method = ReflectionUtils.MakeGenericMethod(method, types.Select(i => i.Get<Type>(null)).ToArray());
												MemberData d = new MemberData(method);
												nod.target.CopyFrom(d);
												uNodeGUIUtility.GUIChanged(nod, UIChangeType.Average);
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
									yield return new DropdownMenuAction("Overrides/" + EditorReflectionUtility.GetPrettyConstructorName(m), (e) => {
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
								yield return new DropdownMenuAction("Overrides/" + EditorReflectionUtility.GetPrettyFunctionName(m), (e) => {
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

			yield break;
		}
	}

	class StateGraphManipulator : GraphManipulator {
		public override int order => -100_000;

		public override bool IsValid(string action) {
			return true;
		}

		public override IEnumerable<ContextMenuItem> ContextMenuForNode(Vector2 mousePosition, Node node) {
			#region Transition
			if(node is Nodes.StateNode) {
				yield return ContextMenuItem.CreateSeparator(order: int.MinValue);
				foreach(TransitionMenu menuItem in NodeEditorUtility.FindTransitionMenu()) {
					var menu = menuItem;
					yield return new ContextMenuItem("Add Transition/" + menuItem.path, ((e) => {
						if(node is Nodes.StateNode stateNode) {
							var transition = new NodeObject();
							NodeEditorUtility.AddNewNode<TransitionEvent>(
								stateNode.transitions.container,
								menu.name,
								menu.type,
								new Vector2(stateNode.position.x + (stateNode.position.width / 2), stateNode.position.position.y + (stateNode.position.height / 2) + 50),
								(transition) => {
									graphEditor.Refresh();
								});
						}
					}), int.MinValue);
				}
				yield return ContextMenuItem.CreateSeparator(order: int.MinValue);
			}
			else if(node is IStateNodeWithTransition stateNode) {
				yield return new ContextMenuItem("Add Transition/Empty", ((e) => {
					NodeEditorUtility.AddNewNode<Nodes.StateTransition>(
						stateNode.TransitionContainer, "Transition",
						new Vector2(node.position.x + (node.position.width / 2), node.position.position.y + (node.position.height / 2) + 50),
						(transition) => {
							transition.nodeObject.AddChildNode(new Nodes.TriggerStateTransition());
							//NodeEditorUtility.AddNewNode<Nodes.ScriptState>(node.nodeObject.parent, "State", 
							//	new Vector2(node.position.x + (node.position.width / 2), node.position.position.y + (node.position.height / 2) + 150), 
							//	state => {
							//		state.enter.ConnectTo(transition.exit);
							//	});
							graphEditor.Refresh();
						});
				}), int.MinValue);

				var eventMenus = NodeEditorUtility.FindEventMenu();
				foreach(var menu in eventMenus) {
					if(menu.type == typeof(Nodes.StateOnEnterEvent)) continue;
					if(menu.type == typeof(Nodes.StateOnExitEvent)) continue;
					if(menu.type.IsDefinedAttribute<StateEventAttribute>()) {
						yield return new ContextMenuItem("Add Transition/" + (string.IsNullOrEmpty(menu.category) ? menu.name : menu.category + "/" + menu.name), (e) => {
							NodeEditorUtility.AddNewNode<Nodes.StateTransition>(
								stateNode.TransitionContainer, menu.nodeName,
								new Vector2(node.position.x + (node.position.width / 2), node.position.position.y + (node.position.height / 2) + 50),
								(transition) => {
									transition.nodeObject.name = menu.nodeName;
									var trigger = transition.nodeObject.AddChildNode(new Nodes.TriggerStateTransition());
									trigger.Register();
									NodeEditorUtility.AddNewNode<Node>(transition, menu.nodeName, menu.type, new(0, -100), evt => {
										var output = evt.nodeObject.FlowOutputs.FirstOrDefault();
										if(output != null) {
											output.ConnectTo(trigger.trigger);
										}
									});
									//NodeEditorUtility.AddNewNode<Nodes.ScriptState>(node.nodeObject.parent, "State",
									//	new Vector2(node.position.x + (node.position.width / 2), node.position.position.y + (node.position.height / 2) + 150),
									//	state => {
									//		state.enter.ConnectTo(transition.exit);
									//	});
								});
							graphEditor.Refresh();
						}, int.MinValue);
					}
				}
				yield return ContextMenuItem.CreateSeparator(order: int.MinValue);
			}
			#endregion

			yield break;
		}

		public override bool HandleCommand(string command) {
			if(graphData.scopes.Contains(StateGraphContainer.Scope)) {
				switch(command) {
					case nameof(Command.OpenCommand):
						//Skip
						return true;
					case nameof(Command.OpenItemSelector):

						return true;
				}
			}
			return false;
		}

		public override void ManipulateCanvasFeatures(HashSet<string> features) {
			if(graphData.scopes.Contains(StateGraphContainer.Scope)) {
				features.Remove(nameof(Feature.Macro));
				features.Remove(nameof(Feature.PlaceFit));
				features.Remove(nameof(Feature.SurroundWith));
				features.Remove(nameof(Feature.ShowAddNodeContextMenu));
			}
		}
	}
}