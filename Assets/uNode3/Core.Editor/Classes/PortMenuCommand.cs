using UnityEngine;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Base class for all custom port menu.
	/// </summary>
	public abstract class PortMenuCommand : IIcon {
		/// <summary>
		/// The graph editor.
		/// </summary>
		public GraphEditor graph;
		/// <summary>
		/// The mouse position on canvas.
		/// </summary>
		public Vector2 mousePositionOnCanvas;
		/// <summary>
		/// The filter for the port.
		/// </summary>
		public FilterAttribute filter;

		/// <summary>
		/// Gets a value indicating whether only the context menu is available.
		/// </summary>
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
		public virtual System.Type GetIcon() {
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
		/// The filter for port
		/// </summary>
		public FilterAttribute filter;
		/// <summary>
		/// The port kind.
		/// </summary>
		public PortKind portKind;

		/// <summary>
		/// Gets the actual type of the port as determined by the filter.
		/// </summary>
		/// <returns>The resolved port type.</returns>
		public System.Type GetActualPortType() => filter?.GetActualType(portType) ?? portType;
	}
}