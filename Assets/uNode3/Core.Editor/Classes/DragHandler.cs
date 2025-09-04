using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;
using UnityEngine.UIElements;
using UnityEditor;
using System.Linq;
using System.Reflection;
using UnityEngine.Events;

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

				bool valid = true;
				if(uNodeEditorUtility.IsSceneObject(obj)) {
					if(!(d.graphData.graph is IIndependentGraph)) {
						valid = false;
					}
					else if(!EditorUtility.IsPersistent(obj) && !uNodeEditorUtility.IsSceneObject(d.graphData.owner)) {
						valid = false;
					}
					else if(d.graphData.graph.GetGraphType().IsSubclassOf(typeof(UnityEngine.Object)) == false) {
						valid = false;
					}
				}

				IEnumerable<DropdownMenuItem> action(UnityEngine.Object obj, string startName) {
					yield return new DropdownMenuAction(startName + "Get", evt => {
						FilterAttribute filter = new FilterAttribute();
						filter.MaxMethodParam = int.MaxValue;
						filter.VoidType = true;
						filter.Public = true;
						filter.Instance = true;
						filter.Static = false;
						filter.DisplayDefaultStaticType = false;
						var type = obj.GetType();
						if(obj is IRuntimeClass || obj is IReflectionType || obj is IInstancedGraph) {
							type = ReflectionUtils.GetRuntimeType(obj);
						}
						string category = type.PrettyName();
						var customItems = ItemSelector.MakeCustomItems(type, filter, category, ItemSelector.CategoryInherited);
						if(customItems != null) {
							if(type.IsInterface == false && valid) {
								customItems.Insert(0, ItemSelector.CustomItem.Create("this", () => {
									var value = new MemberData(obj, MemberData.TargetType.Values);
									value.startType = type;
									NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
										n.target = MemberData.CreateFromValue(obj);
									});
									d.graphEditor.Refresh();
								}, category));
							}
							ItemSelector w = ItemSelector.ShowWindow(obj, filter, delegate (MemberData value) {
								if(!valid) {
									ShowInvalidReference(obj, d.graphData);
								}
								if(type.IsInterface || !valid) {
									obj = null;
								}
								var mData = new MemberData(obj, MemberData.TargetType.Values);
								mData.startType = type;
								value.startType = type;
								value.instance = mData;
								NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
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
						var type = obj.GetType();
						if(obj is IRuntimeClass || obj is IReflectionType || obj is IInstancedGraph) {
							type = ReflectionUtils.GetRuntimeType(obj);
						}
						var customItems = ItemSelector.MakeCustomItems(type, filter, type.PrettyName(), ItemSelector.CategoryInherited);
						if(customItems != null) {
							ItemSelector w = ItemSelector.ShowWindow(obj, filter, delegate (MemberData value) {
								if(!valid) {
									ShowInvalidReference(obj, d.graphData);
								}
								if(type.IsInterface || !valid) {
									obj = null;
								}
								value.instance = obj;
								value.startType = type;
								NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode mNode) {
									mNode.target = value;
									mNode.Register();
									NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(d.graphData, d.mousePositionOnCanvas, (n) => {
										n.target.AssignToDefault(mNode.output);
									});
								});
								d.graphEditor.Refresh();
							}, customItems).ChangePosition(d.mousePositionOnScreen);
							w.displayDefaultItem = false;
						}
					}, DropdownMenuAction.AlwaysEnabled);

					if(d.graphData.graph is IGraphWithVariables && obj.GetType() != typeof(MonoScript)) {
						yield return new DropdownMenuAction(startName + "Create variable with type: " + obj.GetType().PrettyName(true), evt => {
							string name = "newVariable";
							if(obj is GameObject gameObject) {
								name = uNodeUtility.AutoCorrectName(gameObject.name);
							}
							else if(obj is Component component) {
								name = uNodeUtility.AutoCorrectName(component.gameObject.name);
							}
							if(name.Length > 1)
								name = char.ToLower(name[0]) + name.Substring(1);
							var variable = d.graphData.graphData.variableContainer.AddVariable(name, obj.GetType());
							if(valid) {
								variable.defaultValue = obj;
							}
							NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, d.mousePositionOnCanvas, (n) => {
								n.target = MemberData.CreateFromValue(variable);
							});
							d.graphEditor.Refresh();
							if(!valid) {
								ShowInvalidReference(obj, d.graphData);
							}
						}, DropdownMenuAction.AlwaysEnabled);
					}

					var members = obj.GetType().GetMembers().Where(m => m.MemberType.HasFlags(MemberTypes.Field | MemberTypes.Event | MemberTypes.Property) && ReflectionUtils.CanGetMember(m));

					foreach(var m in members) {
						var info = m;
						var mType = ReflectionUtils.GetMemberType(m);
						if(mType.IsSubclassOf(typeof(System.Delegate)) || mType.IsCastableTo(typeof(UnityEventBase))) {
							yield return new DropdownMenuAction(startName + "Members/" + info.Name + " - Create callback", evt => {
								NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (MultipurposeNode mNode) {
									mNode.target = MemberData.CreateFromMember(info);
									mNode.Register();
									if(mType.IsSubclassOf(typeof(System.Delegate))) {
										NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(d.graphData, d.mousePositionOnCanvas + new Vector2(100, 0), (n) => {
											n.setType = SetType.Add;
											n.target.AssignToDefault(mNode.output);
											NodeEditorUtility.AddNewNode<Nodes.NodeLambda>(d.graphData, d.mousePositionOnCanvas + new Vector2(0, 100), (lambda) => {
												lambda.delegateType = mType;
												lambda.Register();
												lambda.output.ConnectTo(n.value);
											});
										});
									}
									else if(mType.IsCastableTo(typeof(UnityEventBase))) {
										var listener = mType.GetMethod(nameof(UnityEvent.AddListener));
										if(listener != null) {
											var dType = listener.GetParameters()[0].ParameterType;
											NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, d.mousePositionOnCanvas + new Vector2(100, 0), (n) => {
												n.target = MemberData.CreateFromMember(listener);
												n.Register();
												n.instance.ConnectTo(mNode.output);
												NodeEditorUtility.AddNewNode<Nodes.NodeLambda>(d.graphData, d.mousePositionOnCanvas + new Vector2(0, 100), (lambda) => {
													lambda.delegateType = mType;
													lambda.Register();
													lambda.output.ConnectTo(n.parameters[0].input);
												});
											});
										}
									}
								});
								d.graphEditor.Refresh();
								if(!valid) {
									ShowInvalidReference(obj, d.graphData);
								}
							}, DropdownMenuAction.AlwaysEnabled);

							if(d.graphData.currentCanvas is MainGraphContainer && d.graphData.graph is IStateGraph) {
								yield return new DropdownMenuAction(startName + "Members/" + info.Name + " - Create event listener", evt => {
									NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (CSharpEventListener node) {
										node.target = MemberData.CreateFromMember(info);
										node.Register();
										if(valid) {
											node.instance.AssignToDefault(MemberData.CreateFromValue(obj));
										}
									});
									d.graphEditor.Refresh();
									if(!valid) {
										ShowInvalidReference(obj, d.graphData);
									}
								}, DropdownMenuAction.AlwaysEnabled);
							}
						}
					}
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
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is UnityEngine.Object;
			}
			return false;
		}

		void ShowInvalidReference(UnityEngine.Object obj, GraphEditorData graphData) {
			if(!(graphData.graph is IIndependentGraph)) {
				EditorUtility.DisplayDialog("", "The c# graph cannot reference project and scene object, the reference will be null.", "Ok");
				return;
			}
			else if(!EditorUtility.IsPersistent(obj) && !uNodeEditorUtility.IsSceneObject(graphData.owner)) {
				EditorUtility.DisplayDialog("", "The project graph cannot reference scene object, the reference will be null.", "Ok");
				return;
			}
			else if(graphData.graph.GetGraphType().IsSubclassOf(typeof(UnityEngine.Object)) == false) {
				EditorUtility.DisplayDialog("", "The graph that's not inherited from UnityEngine.Object cannot reference project and scene object, the reference will be null.", "Ok");
				return;
			}
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
				if(d.graphData.currentCanvas is MainGraphContainer) {
					if(d.graphData.graph is not IStateGraph) {
						return false;
					}
				}
				else if(d.graphData.currentCanvas is NodeObject nodeObject) {
					if(nodeObject.node is not INodeWithEventHandler) {
						return false;
					}
				}
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

	class DragHandlerMenuForStateMachine : DragHandlerMenu {
		public override int order => int.MinValue;

		public override IEnumerable<DropdownMenuItem> GetMenuItems(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				var obj = d.draggedValue as StateGraphContainer;

				IEnumerable<DropdownMenuItem> DoAction(UGraphElement obj, string path = "") {
					yield return new DropdownMenuAction($"{path}Get State Machine", evt => {
						NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (GetStateMachineNode n) {
							n.kind = GetStateMachineNode.Kind.StateMachine;
							n.reference = obj;
							n.EnsureRegistered();
						});
						d.graphEditor.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					foreach(var state in obj.GetNodesInChildren<IStateNodeWithTransition>()) {
						if(state is AnyStateNode) continue;
						var title = (state as Node).GetTitle();
						yield return new DropdownMenuAction($"{path}{title}/Set State", evt => {
							NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (GetStateMachineNode n) {
								n.kind = GetStateMachineNode.Kind.SetState;
								n.reference = obj;
								n.stateReference = (state as Node).nodeObject;
								n.EnsureRegistered();
							});
							d.graphEditor.Refresh();
						}, DropdownMenuAction.AlwaysEnabled);
						yield return new DropdownMenuAction($"{path}{title}/Get State Is Active", evt => {
							NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (GetStateMachineNode n) {
								n.kind = GetStateMachineNode.Kind.GetState;
								n.reference = obj;
								n.stateReference = (state as Node).nodeObject;
								n.EnsureRegistered();
							});
							d.graphEditor.Refresh();
						}, DropdownMenuAction.AlwaysEnabled);
						if(state is NestedStateNode nested) {
							foreach(var v in DoAction(nested, title + "/")) {
								yield return v;
							}
						}
					}
				}
				foreach(var v in DoAction(obj)) {
					yield return v;
				}
			}
			yield break;
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is StateGraphContainer;
			}
			return false;
		}
	}
	#endregion
}