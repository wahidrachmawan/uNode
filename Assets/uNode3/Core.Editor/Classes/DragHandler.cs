using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine.UIElements;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	public class DragHandlerData {
		/// <summary>
		/// The value that's dragged
		/// </summary>
		public object draggedValue;
		/// <summary>
		/// The object that's dropped
		/// </summary>
		public object droppedTarget;
	}

	public class DragHandlerDataForGraphElement : DragHandlerData {
		/// <summary>
		/// The target graph editor canvas
		/// </summary>
		public GraphEditor graphEditor;
		/// <summary>
		/// The position mouse on the graph canvas
		/// </summary>
		public Vector2 mousePositionOnCanvas;
		/// <summary>
		/// The position mouse in screen
		/// </summary>
		public Vector2 mousePositionOnScreen;

		/// <summary>
		/// The graph data to the currently edited canvas
		/// </summary>
		public GraphEditorData graphData {
			get {
				return graphEditor.graphData;
			}
		}
	}

	/// <summary>
	/// Base class for drag handler.
	/// </summary>
	public abstract class DragHandler {
		/// <summary>
		/// The name of the handler
		/// </summary>
		public abstract string name { get; }
		/// <summary>
		/// The order of the command
		/// </summary>
		public virtual int order { get { return 0; } }
		/// <summary>
		/// Callback when the command is clicked
		/// </summary>
		/// <param name="source"></param>
		/// <param name="mousePosition"></param>
		public abstract void OnAcceptDrag(DragHandlerData data);
		/// <summary>
		/// Is the handler is valid for execute/show?
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public virtual bool IsValid(DragHandlerData data) {
			return true;
		}

		public static List<DragHandler> m_instances;
		public static List<DragHandler> Instances {
			get {
				if(m_instances == null) {
					m_instances = EditorReflectionUtility.GetListOfType<DragHandler>();
				}
				return m_instances;
			}
		}
	}

	/// <summary>
	/// Base class for drag handler for menu.
	/// </summary>
	public abstract class DragHandlerMenu {
		/// <summary>
		/// The order of the command
		/// </summary>
		public virtual int order { get { return 0; } }
		/// <summary>
		/// Get the list of menu items
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public abstract IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data);
		/// <summary>
		/// Is the handler is valid for execute/show?
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public abstract bool IsValid(DragHandlerData data);

		public static List<DragHandlerMenu> m_instances;
		public static List<DragHandlerMenu> Instances {
			get {
				if(m_instances == null) {
					m_instances = EditorReflectionUtility.GetListOfType<DragHandlerMenu>();
				}
				return m_instances;
			}
		}
	}

	/// <summary>
	/// Drag handler for single menu item
	/// </summary>
	public abstract class DragHandleMenuAction : DragHandlerMenu {
		public abstract string name { get; }

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			yield return new DropdownMenuAction(name, evt => {
				OnClick(data);
			}, DropdownMenuAction.AlwaysEnabled);
		}

		public abstract void OnClick(DragHandlerData data);
	}

	#region Menus
	class DragHandlerMenuForFunction : DragHandlerMenu {
		public override int order => int.MinValue;

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				var obj = d.draggedValue as Function;
				yield return new DropdownMenuAction("Invoke", evt => {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, obj.name, null, d.mousePositionOnCanvas, (n) => {
						n.target = MemberData.CreateFromValue(obj);
					});
					d.graphEditor.Refresh();
				}, DropdownMenuAction.AlwaysEnabled);

				if(obj.ReturnType().IsCastableTo(typeof(System.Collections.IEnumerator)) && d.graphData.graph.GetGraphInheritType().IsCastableTo(typeof(MonoBehaviour))) {
					yield return new DropdownMenuAction("Start Coroutine", evt => {
						NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (NodeBaseCaller node) {
							node.target = MemberData.CreateFromMember(typeof(MonoBehaviour).GetMethod(nameof(MonoBehaviour.StartCoroutine), new[] { typeof(System.Collections.IEnumerator) }));
							node.Register();

							NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, obj.name, null, new Vector2(d.mousePositionOnCanvas.x - 200, d.mousePositionOnCanvas.y), (n) => {
								n.target = MemberData.CreateFromValue(obj);

								node.parameters[0].input.ConnectTo(n.output);
							});
						});
						d.graphEditor.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
				}
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Function;
			}
			return false;
		}
	}

	class DragHandlerMenuForProperty : DragHandlerMenu {
		public override int order => int.MinValue;

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				var obj = d.draggedValue as Property;
				if(obj.CanGetValue()) {
					yield return new DropdownMenuAction("Get", evt => {
						NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
							var mData = MemberData.CreateFromValue(obj);
							n.target = mData;
							n.EnsureRegistered();
						});
						d.graphEditor.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
				}
				if(obj.CanSetValue()) {
					yield return new DropdownMenuAction("Set", evt => {
						NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (Nodes.NodeSetValue n) {
							n.EnsureRegistered();
							var mData = MemberData.CreateFromValue(obj);
							n.target.AssignToDefault(mData);
							if(mData.type != null) {
								n.value.AssignToDefault(MemberData.Default(mData.type));
							}
						});
						d.graphEditor.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
				}
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Property prop && (prop.CanGetValue() || prop.CanSetValue());
			}
			return false;
		}
	}

	class DragHandlerMenuForVariable : DragHandlerMenu {
		public override int order => int.MinValue;

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				var obj = d.draggedValue as Variable;
				yield return new DropdownMenuAction("Get", evt => {
					NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
						var mData = MemberData.CreateFromValue(obj);
						n.target = mData;
						n.EnsureRegistered();
					});
					d.graphEditor.Refresh();
				}, DropdownMenuAction.AlwaysEnabled);
				yield return new DropdownMenuAction("Set", evt => {
					NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (Nodes.NodeSetValue n) {
						n.EnsureRegistered();
						var mData = MemberData.CreateFromValue(obj);
						n.target.AssignToDefault(mData);
						if(mData.type != null) {
							n.value.AssignToDefault(MemberData.Default(mData.type));
						}
					});
					d.graphEditor.Refresh();
				}, DropdownMenuAction.AlwaysEnabled);
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Variable;
			}
			return false;
		}
	}

	class DragHandlerMenuForUnityObject : DragHandlerMenu {
		public override int order => int.MinValue;

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				var obj = d.draggedValue as UnityEngine.Object;

				IEnumerable<DropdownMenuItem> action(UnityEngine.Object dOBJ, string startName) {
					yield return new DropdownMenuAction(startName + "Get", evt => {
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
							if(type.IsInterface == false) {
								customItems.Insert(0, ItemSelector.CustomItem.Create("this", () => {
									var value = new MemberData(dOBJ, MemberData.TargetType.Values);
									value.startType = type;
									NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
										n.target = MemberData.CreateFromValue(dOBJ);
									});
									d.graphEditor.Refresh();
								}, category));
							}
							ItemSelector w = ItemSelector.ShowWindow(dOBJ, filter, delegate (MemberData value) {
								if(type.IsInterface) {
									dOBJ = null;//Will make the instance null for graph interface
								}
								var mData = new MemberData(dOBJ, MemberData.TargetType.Values);
								mData.startType = type;
								value.startType = type;
								value.instance = mData;
								NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
									n.target = value;
								});
								d.graphEditor.Refresh();
							}, customItems).ChangePosition(d.mousePositionOnScreen);
							w.displayDefaultItem = false;
						}
					}, DropdownMenuAction.AlwaysEnabled);

					yield return new DropdownMenuAction(startName + "Set", evt => {
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
								NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(d.graphData, d.mousePositionOnCanvas, (n) => {
									n.target.AssignToDefault(value);
								});
								d.graphEditor.Refresh();
							}, customItems).ChangePosition(d.mousePositionOnScreen);
							w.displayDefaultItem = false;
						}
					}, DropdownMenuAction.AlwaysEnabled);
				}
				{
					foreach(var menu in action(obj, "")) {
						yield return menu;
					}
				}
				if(obj is GameObject) {
					yield return new DropdownMenuSeparator("");
					foreach(var comp in (obj as GameObject).GetComponents<Component>()) {
						foreach(var menu in action(comp, comp.GetType().Name + "/")) {
							yield return menu;
						}
					}
				}
				else if(obj is Component) {
					yield return new DropdownMenuSeparator("");
					foreach(var comp in (obj as Component).GetComponents<Component>()) {
						if(comp == obj) continue;
						foreach(var menu in action(comp, comp.GetType().Name + "/")) {
							yield return menu;
						}
					}
				}

				if(d.graphData.graph is IGraphWithVariables && obj.GetType() != typeof(MonoScript)) {
					DropdownMenuAction GetMenu(UnityEngine.Object obj, string subMenu) {
						return new DropdownMenuAction(subMenu + "Create variable with type: " + obj.GetType().PrettyName(true), evt => {
							var variable = d.graphData.graphData.variableContainer.AddVariable("newVariable", obj.GetType());
							if(uNodeEditorUtility.IsSceneObject(obj) == false) {
								variable.defaultValue = obj;
							}
							NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, obj.name, null, d.mousePositionOnCanvas, (n) => {
								n.target = MemberData.CreateFromValue(variable);
							});
							d.graphEditor.Refresh();
						}, DropdownMenuAction.AlwaysEnabled);
					}
					yield return GetMenu(obj, "");
					if(obj is Component comp) {
						yield return GetMenu(comp.gameObject, typeof(GameObject).Name + "/");
						foreach(var c in comp.GetComponents<Component>()) {
							if(c == obj) continue;
							yield return GetMenu(c, c.GetType().Name + "/");
						}
					}
					else if(obj is GameObject go) {
						foreach(var c in go.GetComponents<Component>()) {
							if(c == obj) continue;
							yield return GetMenu(c, c.GetType().Name + "/");
						}
					}
				}
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is UnityEngine.Object;
			}
			return false;
		}
	}

	class DragHandlerMenuFor_CreateDelegate : DragHandleMenuAction {
		public override string name => "Create Delegate";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			var obj = (data.draggedValue as Function);
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (NodeDelegateFunction node) {
					node.member.target = MemberData.CreateFromValue(obj);
					node.Register();
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				if(d.draggedValue is Function) {
					return true;
				}
			}
			return false;
		}
	}

	class DragHandlerMenuFor_CreateEvent : DragHandleMenuAction {
		public override string name => "Create Event Listener";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (CSharpEventListener node) {
					if(data.draggedValue is Variable variable) {
						var member = variable.GetMemberInfo();
						if(member != null) {
							node.target = MemberData.CreateFromMember(member);
							node.Register();
							node.instance.AssignToDefault(MemberData.This(d.graphData.graph));
						}
					}
					else if(data.draggedValue is Property property) {
						var member = property.GetMemberInfo();
						if(member != null) {
							node.target = MemberData.CreateFromMember(member);
							node.Register();
							node.instance.AssignToDefault(MemberData.This(d.graphData.graph));
						}
					}
					node.Register();
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				if(d.graphData.currentCanvas is not MainGraphContainer || d.graphData.graph is not IStateGraph || d.graphData.graph is not IReflectionType)
					return false;
				if(d.draggedValue is Variable variable) {
					return variable.type.IsSubclassOf(typeof(System.Delegate));
				}
				if(d.draggedValue is Property property) {
					return property.ReturnType().IsSubclassOf(typeof(System.Delegate));
				}
			}
			return false;
		}
	}

	class DragHandlerMenuFor_CreateEventHook : DragHandleMenuAction {
		public override string name => "Create Event Hook";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (EventHook node) {
					if(data.draggedValue is Variable variable) {
						node.target.AssignToDefault(MemberData.CreateFromValue(variable));
					}
					else if(data.draggedValue is Property property) {
						node.target.AssignToDefault(MemberData.CreateFromValue(property));
					}
					else if(data.draggedValue is Function function) {
						NodeEditorUtility.AddNewNode(d.graphData, new(d.mousePositionOnCanvas.x - 100, d.mousePositionOnCanvas.y - 100), (MultipurposeNode tNode) => {
							tNode.target = MemberData.CreateFromValue(function);
							tNode.Register();
							node.target.ConnectTo(tNode.output);
						});
					}
					node.Register();
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				if(d.draggedValue is Variable variable) {
					return variable.type.IsSubclassOf(typeof(System.Delegate));
				}
				if(d.draggedValue is Property property) {
					return property.ReturnType().IsSubclassOf(typeof(System.Delegate));
				}
				if(d.draggedValue is Function function) {
					return function.ReturnType().IsSubclassOf(typeof(System.Delegate));
				}
			}
			return false;
		}
	}
	#endregion
}