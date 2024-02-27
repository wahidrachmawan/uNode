using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public abstract class Connection : IDisposable {
		public bool isProxy;

		public abstract UPort Input { get; set; }
		public abstract UPort Output { get; set; }
		public abstract void Connect();
		public abstract void Disconnect();

		public bool isValid => Input != null && Output != null && Input.isValid && Output.isValid;

		void IDisposable.Dispose() {
			Disconnect();
		}

		#region Factory
		/// <summary>
		/// Create a connection of input and output ports without connect it.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		public static Connection Create(UPort input, UPort output) {
			if(input is null) {
				throw new ArgumentNullException(nameof(input));
			}

			if(output is null) {
				throw new ArgumentNullException(nameof(output));
			}
			//Check in case the input / output is wrong
			if(input is ValueOutput && output is ValueInput || input is FlowOutput && output is FlowInput) {
				var tIn = input;
				var tOut = output;
				input = tOut;
				output = tIn;
			}
			if(input is ValueInput && output is ValueOutput) {
				return new ValueConnection(input as ValueInput, output as ValueOutput);
			} else if(input is FlowInput && output is FlowOutput) {
				return new FlowConnection(input as FlowInput, output as FlowOutput);
			}
			throw new InvalidOperationException($"Cannot connect input: {input.GetType()} to output: {output.GetType()}");
		}

		/// <summary>
		/// Create and Connect the input and output ports
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		public static Connection CreateAndConnect(UPort input, UPort output) {
			var result = Create(input, output);
			result.Connect();
			return result;
		}

		/// <summary>
		/// Create and Connect the input and output ports
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		public static Connection Connect(UPort input, UPort output) {
			return CreateAndConnect(input, output);
		}
		#endregion
	}
}