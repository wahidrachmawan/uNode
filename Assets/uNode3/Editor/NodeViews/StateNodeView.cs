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
	[NodeCustomEditor(typeof(StateNode))]
	public class StateNodeView : BaseNodeView {
		//To ensure the node always reload when the graph changed
		public override bool autoReload => true;

		private List<UNodeView> transitionViews = new List<UNodeView>();

		protected override void InitializeView() {
			base.InitializeView();
			BuildTransitions();
		}

		protected void BuildTransitions() {
			foreach(var view in transitionViews) {
				owner.RemoveView(view);
			}
			transitionViews.Clear();
			StateNode node = targetNode as StateNode;
			var transitions = node.GetTransitions();
			int index = 0;
			foreach(var tr in transitions) {
				var viewType = UIElementUtility.GetNodeViewTypeFromType(tr.GetType());
				if(viewType == null) {
					viewType = typeof(TransitionView);
				}
				var transition = owner.AddNodeView(tr.nodeObject, typeof(TransitionView));
				transitionViews.Add(transition);
				var flow = new FlowOutput(node, "[Transition]" + index).SetName("");
				var port = AddOutputFlowPort(new FlowOutputData(flow) { userData = transition });
				port.SetEnabled(false);
				index++;
			}
		}

		public override void InitializeEdge() {
			foreach(var port in outputPorts) {
				if(port.isFlow && !port.enabledSelf) {
					TransitionView transition = port.portData.userData as TransitionView;
					if(transition != null) {
						var edge = new EdgeView(new EdgeData(Connection.Create(transition.transition.enter, port.GetPortValue()), transition.input, port));
						owner.Connect(edge, false);
						edge.SetEnabled(false);
					}
				}
			}
		}
	}
}