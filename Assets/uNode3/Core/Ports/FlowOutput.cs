using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public class FlowOutput : FlowPort {
		public bool localFunction;
		public bool isNextFlow;

		/// <summary>
		/// True on port is connected and valid
		/// </summary>
		public bool isAssigned => isConnected && connections[0].isValid;

		public bool IsCoroutine() {
			if(connections.Count > 0) {
				if(connections.Count > 1) {
					return false;
				}
				var c = connections[0];
				if(c.isValid) {
					return c.input.IsCoroutine();
				}
			}
			return false;
		}

		public bool IsSelfCoroutine() {
			if(connections.Count > 0) {
				if(connections.Count > 1) {
					return false;
				}
				var c = connections[0];
				if(c.isValid) {
					return c.input.IsSelfCoroutine();
				}
			}
			return false;
		}

		public FlowInput GetTargetFlow() {
			var con = GetConnection();
			if(con != null && con.isValid) {
				return con.input;
			}
			return null;
		}

		public NodeObject GetTargetNode() {
			var con = GetConnection();
			if (con != null && con.isValid) {
				return con.input.node;
			}
			return null;
		}

		public FlowConnection GetValidConnection() {
			var con = GetConnection();
			if(con != null && con.isValid) {
				return con;
			}
			return null;
		}

		public FlowConnection GetConnection() {
			if(connections.Count > 0) {
				if(connections.Count > 1) {
					throw new InvalidOperationException("There's many connected ports, the connected port must be 1 as it doens't support multi conenctions.");
				}
				return connections[0];
			}
			return null;
		}

		public StateType GetCurrentState(GraphInstance instance) {
			if(connections.Count > 0) {
				var c = GetConnection();
				if(c.isValid) {
					var targetFlow = instance.GetStateData(c.input);
					return targetFlow.currentState;
				}
			}
			return StateType.Success;
		}

		public FlowOutput(NodeObject node, string id) : base(node) {
			this.id = id;
		}

		public void Restore(FlowOutput other) {
			if(other.id != id)
				throw new Exception("Cannot restore port because the id is different.");
			name = other.name;

			localFunction = other.localFunction;
			isNextFlow = other.isNextFlow;
		}
	}
}