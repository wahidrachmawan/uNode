using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public class ValueOutput : ValuePort {
		private Func<Flow, object> get;
		private Action<Flow, object> set;
		public bool isVariable;

		public ValueOutput(NodeObject node, string id, Type type, PortAccessibility accessibility) : base(node) {
			this.id = id;
			this.type = type;
			if(accessibility.CanGet()) {
				get = DefaultGet;
				canGetValue = () => true;
			}
			if(accessibility.CanSet()) {
				set = DefaultSet;
				canSetValue = () => true;
			}
		}

		public ValueOutput(NodeObject node, string id, Func<Type> type, PortAccessibility accessibility) : base(node) {
			this.id = id;
			this.dynamicType = type;
			if(accessibility.CanGet()) {
				get = DefaultGet;
				canGetValue = () => true;
			}
			if(accessibility.CanSet()) {
				set = DefaultSet;
				canSetValue = () => true;
			}
		}

		internal object DefaultGet(Flow flow) => flow.GetPortData(this);
		internal void DefaultSet(Flow flow, object value) => flow.SetPortData(this, value);

		public IEnumerable<ValueInput> GetConnectedPorts() {
			if(isConnected) {
				foreach(var c in connections) {
					if(c.isValid) {
						yield return c.input;
					}
				}
			}
		}

		public object GetValue(Flow flow) {
			if(get == null)
				throw new GraphException("The get action on the port is null.", node);
			return get(flow);
		}

		public void SetValue(Flow flow, object value) {
			if(get == null)
				throw new GraphException("The get action on the port is null.", node);
			set(flow, value);
		}

		public void AssignGetCallback(Func<Flow, object> get) {
			this.get = get;
		}

		public void AssignSetCallback(Action<Flow, object> set) {
			this.set = set;
		}

		public void Restore(ValueOutput other) {
			if(other.id != id)
				throw new Exception("Cannot restore port because the id is different.");
			name = other.name;
			get = other.get;
			set = other.set;
			_type = other._type;
			dynamicType = other.dynamicType;
		}
	}
}