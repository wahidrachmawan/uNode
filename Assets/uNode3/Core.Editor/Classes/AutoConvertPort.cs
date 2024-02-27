using UnityEngine;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Base class for auto convert port.
	/// </summary>
	public abstract class AutoConvertPort {
		/// <summary>
		/// The port filter, this can be null on validating
		/// </summary>
		public FilterAttribute filter;

		public bool force;

		/// <summary>
		/// The input port ( this should not null when <see cref="canvas"/> is not null )
		/// </summary>
		public ValueInput input;
		/// <summary>
		/// The output port ( this should not null when <see cref="canvas"/> is not null )
		/// </summary>
		public ValueOutput output;

		public System.Type leftType;
		public System.Type rightType;

		/// <summary>
		/// The canvas of graph, this can be null on validating
		/// </summary>
		public UGraphElement canvas;
		public Vector2 position;

		public virtual int order { get { return 0; } }

		public abstract bool IsValid();
		public abstract bool CreateNode(System.Action<Node> action);
	}
}