using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public sealed class FlowConnection : Connection {
		public FlowInput input;
		public FlowOutput output;

		public override UPort Input {
			get {
				return input;
			}
			set {
				input = value as FlowInput;
			}
		}
		public override UPort Output {
			get {
				return output;
			}
			set {
				output = value as FlowOutput;
			}
		}

		public FlowConnection(FlowInput input, FlowOutput output) {
			this.input = input;
			this.output = output;
		}

		public static FlowConnection CreateAndConnect(FlowInput input, FlowOutput output) {
			var connection = new FlowConnection(input, output);
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
				input.connections.Add(this);
			}
			if(!output.connections.Contains(this)) {
				output.ClearConnections();
				output.connections.Add(this);
			}
		}

		public override void Disconnect() {
			if(input != null) {
				input.connections.Remove(this);
				input = null;
			}
			if(output != null) {
				output.connections.Remove(this);
				output = null;
			}
		}
	}
}