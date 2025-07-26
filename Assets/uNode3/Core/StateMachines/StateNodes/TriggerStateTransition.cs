using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MaxyGames.UNode.Nodes {
	[NodeMenu("Flow", "Trigger Transition", scope = NodeScope.State, hasFlowInput =true)]
	public class TriggerStateTransition : Node {
		[HideInInspector]
		public UGraphElementRef serializedTransition;
		[NonSerialized]
		public FlowInput trigger;

		[NonSerialized]
		private StateTransition m_transition;
		public StateTransition transition {
			get {
				if(m_transition == null) {
					if(nodeObject.parent is NodeObject parent && parent.node is ScriptState) {
						if(serializedTransition?.reference is NodeObject) {
							var target = serializedTransition.reference as NodeObject;
							if(target.node is StateTransition) {
								m_transition = target.node as StateTransition;
								if(object.ReferenceEquals(transition.StateNode, parent.node) == false
								&& transition.IsExpose == false) {
									m_transition = null;
								}
							}
						}
						return m_transition;
					}
					m_transition = nodeObject.GetNodeInParent<StateTransition>();
				}
				else if(m_transition.nodeObject == null) {
					m_transition = null;
				}
				return m_transition;
			}
			internal set {
				m_transition = value;
			}
		}

		protected override void OnRegister() {
			trigger = PrimaryFlowInput(nameof(trigger), OnTrigger);
		}

		public override string GetTitle() {
			return "Trigger Transition";
		}

		private void OnTrigger(Flow flow) {
			transition.Finish(flow);
		}

		public override void CheckError(ErrorAnalyzer analyzer) {
			base.CheckError(analyzer);
			if(transition == null) {
				if(nodeObject.parent is NodeObject parent && parent.node is ScriptState) {
					analyzer.RegisterError(this, "Please assign the transition to trigger");
					return;
				}
				analyzer.RegisterError(this, "The node is not valid in current context");
			}
		}

		protected override string GenerateFlowCode() {
			return CG.GeneratePort(transition.exit);
		}
	}
}