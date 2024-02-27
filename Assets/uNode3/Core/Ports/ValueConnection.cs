using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public sealed class ValueConnection : Connection {
		public ValueInput input;
		public ValueOutput output;

		public override UPort Input {
			get {
				return input;
			}
			set {
				input = value as ValueInput;
			}
		}
		public override UPort Output {
			get {
				return output;
			}
			set {
				output = value as ValueOutput;
			}
		}

		public ValueConnection(ValueInput input, ValueOutput output) {
			this.input = input;
			this.output = output;
		}

		public static ValueConnection CreateAndConnect(ValueInput input, ValueOutput output) {
			var connection = new ValueConnection(input, output);
			connection.Connect();
			return connection;
		}

		public override void Connect() {
			if(input == null)
				throw new NullReferenceException("The input is null");
			if(output == null)
				throw new NullReferenceException("The output is null");
			if(input.node == output.node)
				throw new InvalidOperationException("Cannot connect to self node, this can cause stack overflow");
			if(!input.connections.Contains(this)) {
				input.ClearConnections();
				input.connections.Add(this);
			}
			if(!output.connections.Contains(this)) {
				output.connections.Add(this);
			}
		}

		public override void Disconnect() {
			if(input != null) {
				var type = input.ValueType ?? input.type;
				input.connections.Remove(this);
				if(type != null) {
					input.AssignToDefault(MemberData.CreateValueFromType(type));
				}
			}
			if(output != null) {
				output.connections.Remove(this);
			}
		}

		public object GetValue(Flow flow) {
			if(output != null) {
				try {
					var result = output.GetValue(flow);
#if UNITY_EDITOR
					if(GraphDebug.useDebug && input != null) {
						var node = input.node;
						return GraphDebug.Value(result, flow.target, node.graphContainer.GetGraphID(), node.id, input.id, false);
					}
#endif
					return result;
				} 
				catch(Exception ex) {
					if(ex is GraphException)
						throw;
					throw new GraphException(ex, output.node);
				}
			} else {
				throw new GraphException("Connection is unassigned.", input?.node);
			}
		}

		public void SetValue(Flow flow, object value) {
			if(output != null) {
				try {
#if UNITY_EDITOR
					if(GraphDebug.useDebug && input != null) {
						var node = input.node;
						GraphDebug.Value(value, flow.target, node.graphContainer.GetGraphID(), node.id, input.id, true);
					}
#endif
					output.SetValue(flow, value);
				}
				catch(Exception ex) {
					if(ex is GraphException)
						throw;
					throw new GraphException(ex, output.node);
				}
			} else {
				throw new GraphException("Connection is unassigned.", input?.node);
			}
		}
	}
}