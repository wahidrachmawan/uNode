using UnityEngine;
using System.Collections.Generic;
using MaxyGames.UNode.Nodes;

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
		public abstract void OnClick(DragHandlerData data);
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

	#region Menus
	class DragHandlerMenuForFunction_Invoke : DragHandlerMenu {
		public override string name => "Invoke";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, (d.draggedValue as Function).name, null, d.mousePositionOnCanvas, (n) => {
					n.target = MemberData.CreateFromValue((d.draggedValue as Function));
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Function;
			}
			return false;
		}
	}

	class DragHandlerMenuForFunction_StartCoroutine : DragHandlerMenu {
		public override string name => "Start Coroutine";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			var obj = (data.draggedValue as Function);
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode(d.graphData, d.mousePositionOnCanvas, delegate (NodeBaseCaller node) {
					node.target = MemberData.CreateFromMember(typeof(MonoBehaviour).GetMethod(nameof(MonoBehaviour.StartCoroutine), new[] { typeof(System.Collections.IEnumerator) }));
					node.Register();

					NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, obj.name, null, new Vector2(d.mousePositionOnCanvas.x - 200, d.mousePositionOnCanvas.y), (n) => {
						n.target = MemberData.CreateFromValue(obj);

						node.parameters[0].input.ConnectTo(n.output);
					});
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				if(d.draggedValue is Function function) {
					return function.ReturnType().IsCastableTo(typeof(System.Collections.IEnumerator)) && d.graphData.graph.GetGraphInheritType().IsCastableTo(typeof(MonoBehaviour));
				}
			}
			return false;
		}
	}

	class DragHandlerMenuForProperty_Get : DragHandlerMenu {
		public override string name => "Get";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			var obj = (data.draggedValue as Property);
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
					var mData = MemberData.CreateFromValue(obj);
					n.target = mData;
					n.EnsureRegistered();
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Property prop && prop.CanGetValue();
			}
			return false;
		}
	}

	class DragHandlerMenuForProperty_Set : DragHandlerMenu {
		public override string name => "Set";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			var obj = (data.draggedValue as Property);
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (Nodes.NodeSetValue n) {
					n.EnsureRegistered();
					var mData = MemberData.CreateFromValue(obj);
					n.target.AssignToDefault(mData);
					if(mData.type != null) {
						n.value.AssignToDefault(MemberData.Default(mData.type));
					}
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Property prop && prop.CanSetValue();
			}
			return false;
		}
	}

	class DragHandlerMenuForVariable_Get : DragHandlerMenu {
		public override string name => "Get";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			var obj = (data.draggedValue as Variable);
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode<MultipurposeNode>(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (MultipurposeNode n) {
					var mData = MemberData.CreateFromValue(obj);
					n.target = mData;
					n.EnsureRegistered();
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Variable;
			}
			return false;
		}
	}

	class DragHandlerMenuForVariable_Set : DragHandlerMenu {
		public override string name => "Set";

		public override int order => int.MinValue;

		public override void OnClick(DragHandlerData data) {
			var obj = (data.draggedValue as Variable);
			if(data is DragHandlerDataForGraphElement d) {
				NodeEditorUtility.AddNewNode(d.graphData, obj.name, null, d.mousePositionOnCanvas, delegate (Nodes.NodeSetValue n) {
					n.EnsureRegistered();
					var mData = MemberData.CreateFromValue(obj);
					n.target.AssignToDefault(mData);
					if(mData.type != null) {
						n.value.AssignToDefault(MemberData.Default(mData.type));
					}
				});
				d.graphEditor.Refresh();
			}
		}

		public override bool IsValid(DragHandlerData data) {
			if(data is DragHandlerDataForGraphElement d) {
				return d.draggedValue is Variable;
			}
			return false;
		}
	}

	class DragHandlerMenuFor_CreateDelegate : DragHandlerMenu {
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

	class DragHandlerMenuFor_CreateEvent : DragHandlerMenu {
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

	class DragHandlerMenuFor_CreateEventHook : DragHandlerMenu {
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