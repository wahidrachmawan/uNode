using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode.Nodes {
	//[NodeMenu("Flow", "State", IsCoroutine = true, order = 1, HideOnFlow = true)]
	public class StateNode : BaseCoroutineNode, ISuperNode, IGraphEventHandler {
		[HideInInspector]
		public TransitionData transitions = new TransitionData();

		public bool CanTrigger(GraphInstance instance) {
			var flow = instance.GetStateData(enter);
			return flow.state == StateType.Running;
		}

		public IEnumerable<NodeObject> nestedFlowNodes => nodeObject.GetObjectsInChildren<NodeObject>(obj => obj.node is BaseEventNode);

		public IEnumerable<TransitionEvent> GetTransitions() {
			return transitions.GetFlowNodes<TransitionEvent>();
		}

		private event System.Action<Flow> m_onEnter;
		public event System.Action<Flow> onEnter {
			add {
				m_onEnter -= value;
				m_onEnter += value;
			}
			remove {
				m_onEnter -= value;
			}
		}
		private event System.Action<Flow> m_onExit;
		public event System.Action<Flow> onExit {
			add {
				m_onExit -= value;
				m_onExit += value;
			}
			remove {
				m_onExit -= value;
			}
		}

		protected override void OnRegister() {
			base.OnRegister();
			transitions.Register(this);
			//enter.Next(new RuntimeFlow(OnExit));
			enter.actionOnExit = OnExit;
			enter.actionOnStopped = OnExit;
		}

		protected override System.Collections.IEnumerator OnExecutedCoroutine(Flow flow) {
			if(m_onEnter != null) {
				m_onEnter(flow);
			}
			foreach(var tr in GetTransitions()) {
				tr.OnEnter(flow);
			}
			while(flow.state == StateType.Running) {
				foreach(var tr in GetTransitions()) {
					tr.OnUpdate(flow);
					if(flow.state != StateType.Running) {
						yield break;
					}
				}
				yield return null;
			}
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

		public override void OnGeneratorInitialize() {
			//Register this node as state node.
			CG.RegisterAsStateFlow(enter);
			var transitions = GetTransitions().ToArray();
			for(int i = 0; i < transitions.Length; i++) {
				TransitionEvent transition = transitions[i];
				transition?.OnGeneratorInitialize();
			}
			CG.SetStateInitialization(enter, () => CG.GeneratePort(enter));
			foreach(BaseEventNode node in nestedFlowNodes) {
				foreach(var flow in node.outputs) {
					var targetFlow = flow.GetTargetFlow();
					if(targetFlow != null) {
						CG.RegisterAsStateFlow(targetFlow);
					}
				}
			}
			CG.RegisterPort(enter, () => {
				string onEnter = null;
				string onUpdate = null;
				string onExit = null;
				nodeObject.ForeachInChildrens(element => {
					if(element is NodeObject nodeObject) {
						foreach(var flow in nodeObject.FlowInputs) {
							if(/*flow.IsSelfCoroutine() &&*/ CG.IsStateFlow(flow)) {
								onExit += CG.StopEvent(flow).AddLineInFirst();
							}
						}
					}
				});
				//onExit += CG.Flow(nestedFlowNodes.Select(n => (n.node as BaseGraphEvent).GenerateStopFlows()).ToArray());
				for(int i = 0; i < transitions.Length; i++) {
					TransitionEvent transition = transitions[i];
					if(transition != null) {
						onEnter += transition.GenerateOnEnterCode().Add("\n", !string.IsNullOrEmpty(onEnter));
						onUpdate += transition.GenerateOnUpdateCode().AddLineInFirst();
						onExit += transition.GenerateOnExitCode().AddLineInFirst();
					}
				}
				foreach(var evt in nodeObject.GetNodesInChildren<BaseGraphEvent>()) {
					if(evt != null) {
						if(evt is StateOnEnterEvent) {
							onEnter += evt.GenerateFlows().AddLineInFirst().Replace("yield ", "");
						} else if(evt is StateOnExitEvent) {
							onExit += evt.GenerateFlows().AddLineInFirst();
						} else {
							CG.SetStateInitialization(evt, CG.Routine(CG.LambdaForEvent(evt.GenerateFlows())));
						}
					}
				}
				CG.SetStateStopAction(enter, onExit);
				return CG.Routine(
					CG.Routine(CG.Lambda(onEnter)),
					CG.Invoke(typeof(Runtime.Routine), nameof(Runtime.Routine.WaitWhile), CG.Lambda(onUpdate.AddLineInEnd() + CG.Return(CG.CompareNodeState(enter, null))))
				);
			});
		}

		protected override bool IsSelfCoroutine() {
			return true;
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

namespace MaxyGames.UNode {
	[Serializable]
	public class TransitionData : BaseNodeContainerData<TransitionContainer> {
		protected override bool IsValidFlowNode(NodeObject node) {
			return node.node is TransitionEvent;
		}
	}

	public class TransitionContainer : UGraphElement {

	}
}