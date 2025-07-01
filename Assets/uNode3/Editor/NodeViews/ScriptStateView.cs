using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;
using MaxyGames.UNode.Nodes;

namespace MaxyGames.UNode.Editors {
	[NodeCustomEditor(typeof(ScriptState))]
	public class ScriptStateView : BaseNodeView {
		//To ensure the node always reload when the graph changed
		public override bool autoReload => true;

		private List<UNodeView> transitionViews = new List<UNodeView>();

		protected override void InitializeView() {
			base.InitializeView();
			AddToClassList("state-node");
			BuildTransitions();
		}

		protected override void InitializeDefaultPorts() {
			var port = AddInputFlowPort(new FlowInputData((nodeObject.node as ScriptState).enter));
			port.SetEdgeConnector<TransitionEdgeView>();
			port.pickingMode = PickingMode.Ignore;
		}

		protected void BuildTransitions() {
			foreach(var view in transitionViews) {
				owner.RemoveView(view);
			}
			transitionViews.Clear();
			var node = targetNode as ScriptState;
			var transitions = node.GetTransitions();
			int index = 0;
			foreach(var tr in transitions) {
				var viewType = UIElementUtility.GetNodeViewTypeFromType(tr.GetType());
				if(viewType == null) {
					viewType = typeof(StateTransitionView);
				}
				var transition = owner.AddNodeView(tr.nodeObject, typeof(StateTransitionView));
				transitionViews.Add(transition);
				var flow = new FlowOutput(node, "[Transition]" + index).SetName("");
				var port = AddOutputFlowPort(new FlowOutputData(flow) { userData = transition });
				port.pickingMode = PickingMode.Ignore;
				port.SetEnabled(false);
				index++;
			}
		}

		public override void InitializeEdge() {
			foreach(var port in outputPorts) {
				if(port.isFlow && !port.enabledSelf) {
					StateTransitionView transition = port.portData.userData as StateTransitionView;
					if(transition != null) {
						var edge = new TransitionEdgeView();
						edge.showArrow = false;
						edge.input = transition.inputPort;
						edge.output = port;
						owner.Connect(edge, false);
						edge.SetEnabled(false);
					}
				}
			}
		}
	}
}