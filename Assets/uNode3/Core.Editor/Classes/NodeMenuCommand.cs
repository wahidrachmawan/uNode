using UnityEngine;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Base class for all custom node command.
	/// </summary>
	public abstract class NodeMenuCommand {
		/// <summary>
		/// The target graph editor canvas
		/// </summary>
		public GraphEditor graphEditor;
		/// <summary>
		/// The position mouse on the graph canvas
		/// </summary>
		public Vector2 mousePositionOnCanvas;

		/// <summary>
		/// The graph data to the currently edited canvas
		/// </summary>
		public GraphEditorData graphData {
			get {
				return graphEditor.graphData;
			}
		}

		/// <summary>
		/// The name of the command
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
		public abstract void OnClick(Node source, Vector2 mousePosition);
		/// <summary>
		/// Is the command is valid for show?
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public virtual bool IsValidNode(Node source) {
			return true;
		}
	}

	/// <summary>
	/// Base class for all custom input port items.
	/// </summary>
	public abstract class CustomInputPortItem {
		/// <summary>
		/// The target graph editor canvas
		/// </summary>
		public GraphEditor graphEditor;
		/// <summary>
		/// The position mouse on the graph canvas
		/// </summary>
		public Vector2 mousePositionOnCanvas;

		/// <summary>
		/// The graph data to the currently edited canvas
		/// </summary>
		public GraphEditorData graphData {
			get {
				return graphEditor.graphData;
			}
		}

		public virtual int order { get { return 0; } }

		public virtual bool IsValidPort(ValueOutput source) {
			return true;
		}

		public virtual bool IsValidPort(ValueOutput source, PortAccessibility accessibility) {
			return accessibility != PortAccessibility.WriteOnly && IsValidPort(source);
		}

		public abstract System.Collections.Generic.IList<ItemSelector.CustomItem> GetItems(ValueOutput source);
	}

	/// <summary>
	/// Base class for all custom graph command.
	/// </summary>
	public abstract class GraphMenuCommand {
		/// <summary>
		/// The target graph editor canvas
		/// </summary>
		public GraphEditor graphEditor;
		/// <summary>
		/// The position mouse on the graph canvas
		/// </summary>
		public Vector2 mousePositionOnCanvas;

		/// <summary>
		/// The graph data to the currently edited canvas
		/// </summary>
		public GraphEditorData graphData {
			get {
				return graphEditor.graphData;
			}
		}

		/// <summary>
		/// The name of the command
		/// </summary>
		public abstract string name { get; }
		/// <summary>
		/// The order of the command
		/// </summary>
		public virtual int order { get { return 0; } }
		/// <summary>
		/// Callback when the command is clicked
		/// </summary>
		/// <param name="mousePosition"></param>
		public abstract void OnClick(Vector2 mousePosition);
		/// <summary>
		/// Is the command is valid for show?
		/// </summary>
		/// <returns></returns>
		public virtual bool IsValid() {
			return true;
		}
	}
}