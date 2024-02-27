using UnityEngine;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Base class for all custom port menu.
	/// </summary>
	public abstract class PortMenuCommand : ICustomIcon {
		/// <summary>
		/// The graph editor.
		/// </summary>
		public NodeGraph graph;
		/// <summary>
		/// The mouse position on canvas.
		/// </summary>
		public Vector2 mousePositionOnCanvas;
		/// <summary>
		/// The filter for the port.
		/// </summary>
		public FilterAttribute filter;

		public virtual bool onlyContextMenu => false;

		/// <summary>
		/// The name of the menu.
		/// </summary>
		public abstract string name { get; }
		/// <summary>
		/// The order of the menu, default is 0.
		/// </summary>
		public virtual int order => 0;
		/// <summary>
		/// Callback for do something on menu is clicked.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="value"></param>
		/// <param name="mousePosition"></param>
		public abstract void OnClick(Node source, PortCommandData data, Vector2 mousePosition);
		/// <summary>
		/// Validate if port is valid for this command.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool IsValidPort(Node source, PortCommandData data) {
			return true;
		}

		/// <summary>
		/// The command icon
		/// </summary>
		/// <returns></returns>
		public virtual Texture GetIcon() {
			return null;
		}
	}

	public class PortCommandData {
		/// <summary>
		/// The name of the port.
		/// </summary>
		public string portName;
		/// <summary>
		/// The original port value.
		/// </summary>
		public UPort port;
		/// <summary>
		/// Required for value port.
		/// </summary>
		public System.Type portType;
		/// <summary>
		/// The port kind.
		/// </summary>
		public PortKind portKind;
	}
}