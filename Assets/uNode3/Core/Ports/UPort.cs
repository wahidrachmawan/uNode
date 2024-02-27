using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[GraphElement]
	public abstract class UPort : IGraphElement {
		[SerializeField]
		private string _id;
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
		[SerializeField]
		private string _tooltip;
		public string tooltip {
			get => _tooltip;
			set => _tooltip = value;
		}

		public NodeObject node { get; set; }

		public Node GetNode() => node?.node;

		public UPort(NodeObject node) => this.node = node;

		public abstract IEnumerable<Connection> Connections { get; }

		public IEnumerable<Connection> ValidConnections {
			get {
				foreach(var c in Connections) {
					if(c.isValid) {
						yield return c;
					}
				}
			}
		}
		public virtual void ClearConnections() {
			while(Connections.Any()) {
				var c = Connections.FirstOrDefault();
				c.Disconnect();
			}
		}

		public bool isValid => node != null;
		public bool isConnected => Connections.Any();
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

		public override string ToString() {
			if(isValid) {
				return $"Port: ({id}-{name}), node: {node}";
			}
			return base.ToString();
		}
	}
}