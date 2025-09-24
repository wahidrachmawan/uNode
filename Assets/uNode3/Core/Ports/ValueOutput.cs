﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[Serializable]
	public class ValueOutput : ValuePort {
		[NonSerialized]
		private Func<Flow, object> get;
		[NonSerialized]
		private Action<Flow, object> set;
		[NonSerialized]
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
				canGetValue = static () => true;
			}
			if(accessibility.CanSet()) {
				set = DefaultSet;
				canSetValue = static () => true;
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
				throw new GraphException($"The get action on the port is null.\nError from node: {node.GetRichName()}", node);
			return get(flow);
		}

		public void SetValue(Flow flow, object value) {
			if(set == null)
				throw new GraphException($"The set action on the port is null.\nError from node: {node.GetRichName()}", node);
			set(flow, value);
		}

		public void AssignGetCallback(Func<Flow, object> get) {
			this.get = get;
			canGetValue = static () => true;
		}

		public void AssignSetCallback(Action<Flow, object> set) {
			this.set = set;
			canSetValue = static () => true;
		}

		public void Restore(ValueOutput other) {
			if(other.id != id)
				throw new Exception("Cannot restore port because the id is different.");
			name = other.name;
			get = other.get;
			set = other.set;
			canGetValue = other.canGetValue;
			canSetValue = other.canSetValue;
			_type = other._type;
			dynamicType = other.dynamicType;
			//OnChanged = other.OnChanged;
		}
	}
}