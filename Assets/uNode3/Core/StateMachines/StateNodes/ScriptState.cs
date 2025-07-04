using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	public interface IScriptState {
		public event System.Action<Flow> OnEnterState;
		public event System.Action<Flow> OnExitState;
	}

	public class ScriptState : Node, IScriptState, ISuperNode, IGraphEventHandler, IStateNodeWithTransition {
		[HideInInspector]
		public StateTranstionData transitions = new StateTranstionData();


		private static readonly string[] m_styles = new[] { "state-node", "state-script" };
		public override string[] Styles => m_styles;
		public UGraphElement TransitionContainer => transitions.container;

		public bool CanTrigger(GraphInstance instance) {
			var state = instance.GetUserData(this) as StateMachines.IState;
			return state.IsActive;
		}

		public IEnumerable<NodeObject> nestedFlowNodes => nodeObject.GetObjectsInChildren<NodeObject>(obj => obj.node is BaseEventNode);

		string ISuperNode.SupportedScope => NodeScope.State + "|" + NodeScope.FlowGraph;

		public IEnumerable<StateTransition> GetTransitions() {
			return transitions.GetFlowNodes<StateTransition>();
		}

		private event System.Action<Flow> m_onEnter;
		public event System.Action<Flow> OnEnterState {
			add {
				m_onEnter -= value;
				m_onEnter += value;
			}
			remove {
				m_onEnter -= value;
			}
		}
		private event System.Action<Flow> m_onExit;
		public event System.Action<Flow> OnExitState {
			add {
				m_onExit -= value;
				m_onExit += value;
			}
			remove {
				m_onExit -= value;
			}
		}

		[NonSerialized]
		public FlowInput enter;

		protected override void OnRegister() {
			if(transitions == null) transitions = new();
			transitions.Register(this);
			enter = PrimaryFlowInput(nameof(enter), flow => throw new Exception("State can be changed only from transition"));
			//enter.Next(new RuntimeFlow(OnExit));
		}

		public override void OnRuntimeInitialize(GraphInstance instance) {
			base.OnRuntimeInitialize(instance);
			var state = new StateMachines.State(
				onEnter: () => {
#if UNITY_EDITOR
					if(GraphDebug.useDebug) {
						GraphDebug.FlowNode(instance.target, nodeObject.graphContainer.GetGraphID(), id, null);
					}
#endif
					m_onEnter?.Invoke(instance.defaultFlow);
				},
				onExit: () => {
#if UNITY_EDITOR
					if(GraphDebug.useDebug) {
						GraphDebug.FlowNode(instance.target, nodeObject.graphContainer.GetGraphID(), id, true);
					}
#endif
					m_onExit?.Invoke(instance.defaultFlow);
				});
			instance.SetUserData(this, state);
		}

		public void OnExit(Flow flow) {
			foreach(var element in nodeObject.GetObjectsInChildren(true)) {
				if(element is NodeObject node && node.node is not BaseEventNode) {
					foreach(var port in node.FlowInputs) {
						flow.instance.StopState(port);
					}
				}
			}
			foreach(BaseEventNode node in nestedFlowNodes) {
				node.Stop(flow.instance);
			}
			if(m_onExit != null) {
				m_onExit(flow);
			}
			foreach(var tr in GetTransitions()) {
				tr.OnExit(flow);
			}
		}

		public override string GetTitle() {
			return name;
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public bool AllowCoroutine() {
			return true;
		}
	}

}