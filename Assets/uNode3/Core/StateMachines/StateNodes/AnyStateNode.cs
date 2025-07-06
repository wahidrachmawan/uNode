using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public class AnyStateNode : Node, IStateNodeWithTransition, IGeneratorPrePostInitializer {
		[HideInInspector]
		public StateTranstionData transitions = new StateTranstionData();

		private static readonly string[] m_styles = new[] { "state-node", "state-any" };
		public override string[] Styles => m_styles;
		public UGraphElement TransitionContainer => transitions.container;

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
			var fsm = instance.GetUserData(nodeObject.parent) as StateMachines.IStateMachine;
			state.FSM = fsm;
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

		//public override void OnGeneratorInitialize() {
		//	base.OnGeneratorInitialize();
		//	var state = CG.RegisterPrivateVariable("m_anyState", typeof(StateMachines.AnyState), null, this);
		//	var fsm = CG.GetVariableNameByReference(nodeObject.parent);
		//	if(fsm != null) {
		//		var transition = GetTransitions().ToArray();
		//		foreach(var tr in transition) {
		//			tr.OnGeneratorInitialize();
		//		}

		//		string code = CG.Flow(
		//			CG.Set(state, CG.New(typeof(StateMachines.AnyState))),
		//			fsm.CGFlowInvoke(nameof(StateMachines.IStateMachine.RegisterAnyState), state)
		//		);
		//		CG.generatorData.InsertMethodCode("Awake", code);
		//	}
		//}

		void IGeneratorPrePostInitializer.OnPreInitializer() {
			foreach(var tr in GetTransitions()) {
				CG.RegisterEntry(tr);
			}
		}
	}
}