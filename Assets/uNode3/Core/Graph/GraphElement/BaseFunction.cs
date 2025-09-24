﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode {
	public abstract class BaseFunction : NodeContainerWithEntry, IParameterSystem, ILocalVariableSystem, IGeneratorPrePostInitializer {
		[HideInInspector]
		public List<ParameterData> parameters = new List<ParameterData>();

		public IEnumerable<Variable> LocalVariables => variableContainer.collections;

		public Type[] ParameterTypes {
			get {
				return parameters.Select(p => p.isByRef ? p.Type.MakeByRefType() : p.Type).ToArray();
			}
		}

		public bool HasRefOrOut {
			get {
				for(int i = 0; i < parameters.Count; i++) {
					if(parameters[i].isByRef)
						return true;
				}
				return false;
			}
		}

		public IList<ParameterData> Parameters {
			get {
				return parameters;
			}
		}

		public new FunctionEntryNode Entry => entryObject?.node as FunctionEntryNode ?? base.Entry as FunctionEntryNode;

		public abstract Type ReturnType();

		public override void RegisterEntry(BaseEntryNode node) {
			for(int i = 0; i < parameters.Count; i++) {
				var param = parameters[i];
				var port = Node.Utilities.ValueOutput(node, param.id, () => param.Type, PortAccessibility.ReadWrite).SetName(param.name);
				port.AssignGetCallback((flow) => flow.GetLocalData(null, param));
				if(param.refKind != RefKind.In) {
					// If the parameter is not an 'in' or readonly parameter, assign a set callback
					port.AssignSetCallback((flow, value) => flow.SetLocalData(null, param, value));
				}
				if(string.IsNullOrEmpty(param.summary) == false) {
					port.SetTooltip(param.summary);
				}
			}
		}

		/// <summary>
		/// Pre initialize for code gen
		/// </summary>
		public virtual void OnPreInitializer() {
			Entry.EnsureRegistered();
			for(int i = 0; i < Entry.nodeObject.ValueOutputs.Count; i++) {
				var port = Entry.nodeObject.ValueOutputs[i];
				if(!port.isConnected) continue;
				for(int x = 0; x < parameters.Count; x++) {
					if(parameters[x].id == port.id) {
						var name = parameters[x].name;
						CG.RegisterPort(port, () => name);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Post initialize for code gen
		/// </summary>
		public virtual void OnPostInitializer() { }
	}
}