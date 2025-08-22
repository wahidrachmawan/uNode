using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class AnyStateNode : Node, ISuperNode, INodeWithUpdateEvent, IStateNodeWithTransition, INodeWithEventHandler, IGeneratorPrePostInitializer {
		[HideInInspector]
		public StateTranstionData transitions = new StateTranstionData();

		private static readonly string[] m_styles = new[] { "state-node", "state-any" };
		public override string[] Styles => m_styles;
		public UGraphElement TransitionContainer => transitions.container;

		public IEnumerable<NodeObject> NestedFlowNodes => nodeObject.GetObjectsInChildren<NodeObject>(obj => obj.node is BaseEventNode);
		string ISuperNode.SupportedScope => NodeScope.State + "|" + NodeScope.FlowGraph + "|" + NodeScope.Coroutine;

		private event System.Action<Flow> m_onUpdate;
		public event System.Action<Flow> OnUpdateCallback {
			add {
				m_onUpdate -= value;
				m_onUpdate += value;
			}
			remove {
				m_onUpdate -= value;
			}
		}

		public bool CanTrigger(GraphInstance instance) {
			var state = instance.GetUserData(nodeObject.parent) as StateMachines.IState;
			return state == null || state.IsActive;
		}

		public IEnumerable<StateTransition> GetTransitions() {
			return transitions.GetFlowNodes<StateTransition>();
		}

		protected override void OnRegister() {
			if(transitions == null) transitions = new();
			transitions.Register(this);
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var state = new StateMachines.AnyState();
			state.onUpdate = () => {
				m_onUpdate?.Invoke(instance.defaultFlow);
			};
			var fsm = instance.GetUserData(nodeObject.parent) as StateMachines.IStateMachine;
			state.FSM = fsm;
			fsm.RegisterAnyState(state);
			instance.SetUserData(this, state);
		}

		public override string GetTitle() {
			return "Any";
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public bool AllowCoroutine() {
			return false;
		}

		public override void OnGeneratorInitialize() {
			base.OnGeneratorInitialize();

			CG.RegisterNodeSetup(this, () => {
				string onUpdate = null;
				foreach(var evt in nodeObject.GetNodesInChildren<BaseGraphEvent>()) {
					if(evt != null) {
						if(evt is StateOnUpdateEvent) {
							onUpdate += evt.GenerateFlows().AddLineInFirst();
						}
					}
				}
				if(onUpdate != null) {
					var state = CG.GetVariableNameByReference(nodeObject.parent);
					if(nodeObject.parent is StateGraphContainer) {
						CG.InsertCodeToFunction("Awake", CG.WrapWithInformation(
							state.CGFlowInvoke(
								nameof(StateMachines.StateMachine.RegisterAnyState), 
								CG.New(
									typeof(StateMachines.AnyState), 
									null, 
									new string[] { CG.SetValue(nameof(StateMachines.State.onUpdate), CG.Lambda(onUpdate)) }
								))
						, this));
					}
					else {
						CG.InsertCodeToFunction("Awake", CG.WrapWithInformation(
							state.CGAccess(nameof(StateMachines.State.onUpdate)).CGSet(CG.Lambda(onUpdate))
						, this));
					}
				}
			});
		}

		void IGeneratorPrePostInitializer.OnPreInitializer() {
			foreach(var tr in GetTransitions()) {
				CG.RegisterDependency(tr);
			}
		}

		string INodeWithEventHandler.GenerateTriggerCode(string contents) {
			if(nodeObject.parent is NodeObject parent) {
				if(parent.node is INodeWithEventHandler) {
					var state = CG.GetVariableNameByReference(nodeObject.parent);
					if(state == null)
						return CG.WrapWithInformation(contents, this);
					return CG.WrapWithInformation(CG.If(state.CGAccess(nameof(StateMachines.IState.IsActive)), contents), this);
				}
			}
			else if(nodeObject.parent is StateGraphContainer container) {
				var state = CG.GetVariableNameByReference(nodeObject.parent);
				if(state != null)
					return CG.WrapWithInformation(CG.If(state.CGAccess(nameof(StateMachines.IStateMachine.IsActive)), contents), this);
			}
			return CG.WrapWithInformation(contents, this);
		}
	}
}