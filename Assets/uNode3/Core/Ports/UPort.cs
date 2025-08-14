using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[GraphElement]
	[Serializable]
	public abstract class UPort : IGraphElement {
		[SerializeField]
		private string _id;
		/// <summary>
		/// The id of the port
		/// </summary>
		public string id {
			get {
				return _id;
			}
			set {
				_id = value;
			}
		}
		[SerializeField]
		private string _name;
		/// <summary>
		/// The name of the port
		/// </summary>
		public string name {
			get {
				return _name ?? id;
			}
			set {
				_name = value;
				if(_id == null) {
					_id = value;
				}
			}
		}
		[NonSerialized]
		private string _tooltip;
		/// <summary>
		/// The port tooltip
		/// </summary>
		public string tooltip {
			get => _tooltip;
			set => _tooltip = value;
		}

		internal Func<string> m_title;
		/// <summary>
		/// The port title, use for proxy name, and others
		/// </summary>
		public string title {
			get {
				if(m_title == null) {
					return name;
				}
				return m_title.Invoke();
			}
			set {
				if(value is null) {
					m_title = null;
					return;
				}
				string val = value;
				m_title = () => val;
			}
		}

		//For debugging purpose, don't remove this
		internal string DebugDisplay => GraphException.GetMessage(node);

		/// <summary>
		/// The node that own this port
		/// </summary>
		public NodeObject node { get; internal set; }

		/// <summary>
		/// The node that own this port
		/// </summary>
		/// <returns></returns>
		public Node GetNode() => node?.node;

		public UPort(NodeObject node) => this.node = node;

		/// <summary>
		/// Iterate all connection including valid and invalid connections
		/// </summary>
		public abstract IEnumerable<Connection> Connections { get; }

		/// <summary>
		/// Iterate all valid connection
		/// </summary>
		public IEnumerable<Connection> ValidConnections {
			get {
				foreach(var c in Connections) {
					if(c.isValid) {
						yield return c;
					}
				}
			}
		}

		/// <summary>
		/// Iterate all invalid connection
		/// </summary>
		public IEnumerable<Connection> InvalidConnections {
			get {
				foreach(var c in Connections) {
					if(c.isValid == false) {
						yield return c;
					}
				}
			}
		}

		/// <summary>
		/// Clear all connection that's connected with this port
		/// </summary>
		public virtual void ClearConnections() {
			while(Connections.Any()) {
				var c = Connections.FirstOrDefault();
				c?.Disconnect();
			}
		}

		/// <summary>
		/// True if the port is a valid port
		/// </summary>
		public bool isValid => node != null;
		/// <summary>
		/// True if the port is connected
		/// </summary>
		public bool isConnected => Connections.Any();
		/// <summary>
		/// True if the port has a valid connection
		/// </summary>
		public bool hasValidConnections {
			get {
				foreach(var c in Connections) { 
					if(c.isValid) {
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Get pretty name of the port
		/// </summary>
		/// <returns></returns>
		public string GetPrettyName() {
			var str = name;
			if(string.IsNullOrEmpty(str)) {
				if(this is FlowInput) {
					return "Enter";
				} else if(this is FlowOutput) {
					return "Exit";
				} else if(this is ValueInput) {
					return "Input";
				} else if(this is ValueOutput) {
					return "Output";
				}
			}
#if UNITY_EDITOR
			if(!string.IsNullOrEmpty(str)) {
				return UnityEditor.ObjectNames.NicifyVariableName(str);
			}
#endif
			return str;
		}

		/// <summary>
		/// Get rich name of the port
		/// </summary>
		/// <returns></returns>
		public virtual string GetRichName() {
			var str = title;
			if(this is ValueOutput && this.IsPrimaryPort()) {
				if(string.IsNullOrEmpty(str) || str == "Out" || str == "out" || str == "output") {
					return node.GetRichName();
				}
			}
			if(string.IsNullOrEmpty(str)) {
				if(this is FlowInput) {
					return "Enter";
				}
				else if(this is FlowOutput) {
					return "Exit";
				}
				else if(this is ValueInput) {
					return "Input";
				}
				else if(this is ValueOutput) {
					return "Output";
				}
			}
			return str;
		}

		public override string ToString() {
			if(isValid) {
				return $"Port: ({id}-{name}), node: {node}";
			}
			return base.ToString();
		}

		/// <summary>
		/// Called when the port is changed.
		/// </summary>
		[NonSerialized]
		protected Action<UPort> OnChanged;

		public void OnPortChanged() {
			OnChanged?.Invoke(this);
		}

		/// <summary>
		/// Set port changed callback
		/// </summary>
		/// <param name="action"></param>
		public void SetOnChangedCallback(Action action) {
			if(action == null) {
				OnChanged = null;
			}
			OnChanged = (_) => action();
		}

		/// <summary>
		/// Set port changed callback
		/// </summary>
		/// <param name="action"></param>
		public void SetOnChangedCallback(Action<UPort> action) {
			OnChanged = action;
		}
	}
}