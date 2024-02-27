using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public class FlowInput : FlowPort {
		[NonSerialized]
		public Action<Flow> action;
		[NonSerialized]
		public Func<Flow, IEnumerator> actionCoroutine;
		[NonSerialized]
		public Action<Flow> actionOnExit;
		[NonSerialized]
		public Action<Flow> actionOnStopped;

		[NonSerialized]
		public Func<bool> isCoroutine;
		[NonSerialized]
		public Func<bool> isSelfCoroutine;

		public bool isPrimaryPort {
			get {
				if(node != null) {
					return this == node.primaryFlowInput;
				}
				return false;
			}
		}

		#region Utility
		public IEnumerable<FlowOutput> GetConnectedPorts() {
			if (isConnected) {
				foreach (var c in connections) {
					if (c.isValid) {
						yield return c.output;
					}
				}
			}
		}
		#endregion

		#region Constructors
		public FlowInput(NodeObject node, string id, Action<Flow> action) : base(node) {
			this.id = id;
			this.action = action;
		}

		public FlowInput(NodeObject node, string id, Func<Flow, IEnumerator> action) : base(node) {
			this.id = id;
			this.actionCoroutine = action;
		}

		public FlowInput(NodeObject node, string id, Action<Flow> action, Func<Flow, IEnumerator> actionCoroutine) : base(node) {
			this.id = id;
			this.action = action;
			this.actionCoroutine = actionCoroutine;
		}
		#endregion

		public void Restore(FlowInput other) {
			if(other.id != id)
				throw new Exception("Cannot restore port because the id is different.");
			name = other.name;
			action = other.action;
		}

		public bool IsCoroutine() {
			if(isCoroutine != null) {
				return isCoroutine();
			}
			return IsSelfCoroutine();
		}

		public bool IsSelfCoroutine() {
			if(isSelfCoroutine != null) {
				return isSelfCoroutine();
			}
			return false;
		}
	}
}