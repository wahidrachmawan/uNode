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
}