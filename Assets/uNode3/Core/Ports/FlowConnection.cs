using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[Serializable]
	public sealed class FlowConnection : Connection {
		[SerializeReference]
		public FlowInput input;
		[SerializeReference]
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
			if(input.node == output.node) {
				if(input.node == null) {
					throw new Exception("Node owner in both input and output is null");
				}
				throw new InvalidOperationException("Cannot connect to self node, this can cause stack overflow");
			}
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