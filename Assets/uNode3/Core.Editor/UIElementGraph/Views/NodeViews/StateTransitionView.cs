using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	public class StateTransitionView : UNodeView {
		public Nodes.StateTransition transition;

		public PortView inputPort;
		public PortView outputPort;

		public override bool fullReload => true;

		class TransitionEdgeListener : IEdgeConnectorListener {
			public static TransitionEdgeListener Default { get; private set; } = new TransitionEdgeListener();

			public void OnDrop(GraphView graphView, Edge edge) {
				var edgeView = edge as TransitionEdgeView;
				var graph = graphView as UGraphView;
				graph.Connect(edgeView, true);
			}

			public void OnDropOutsidePort(Edge edge, Vector2 position) {

			}
		}

		public override void Initialize(UGraphView owner, NodeObject node) {
			nodeObject = node;
			this.transition = node.node as Nodes.StateTransition;
			AddToClassList("state-node");
			AddToClassList("state-transition");
			this.AddStyleSheet("uNodeStyles/NativeNodeStyle");
			this.AddStyleSheet(UIElementUtility.Theme.nodeStyle);
			Initialize(owner);
			node.Register();
			ReloadView();

			border.style.overflow = Overflow.Visible;

			titleIcon.RemoveFromHierarchy();
			m_CollapseButton.RemoveFromHierarchy();
		}

		public override void SetPosition(Rect newPos) {
			// if(targetNode != null && newPos != targetNode.editorRect && uNodePreference.GetPreference().snapNode) {
			// 	var preference = uNodePreference.GetPreference();
			// 	float range = preference.snapRange;
			// 	newPos.x = NodeEditorUtility.SnapTo(newPos.x, range);
			// 	newPos.y = NodeEditorUtility.SnapTo(newPos.y, range);
			// 	if(preference.snapToPin && owner.selection.Count == 1) {
			// 		var connectedPort = inputPorts.Where((p) => p.connected).ToList();
			// 		for(int i = 0; i < connectedPort.Count; i++) {
			// 			if(connectedPort[i].orientation != Orientation.Vertical) continue;
			// 			var edges = connectedPort[i].GetEdges();
			// 			foreach(var e in edges) {
			// 				if(e != null) {
			// 					float distanceToPort = e.edgeControl.to.x - e.edgeControl.from.x;
			// 					if(Mathf.Abs(distanceToPort) <= preference.snapToPinRange && Mathf.Abs(newPos.x - layout.x) <= preference.snapToPinRange) {
			// 						newPos.x = layout.x - distanceToPort;
			// 						break;
			// 					}
			// 				}
			// 			}
			// 		}
			// 	}
			// }
			base.SetPosition(newPos);

			transition.position = newPos;
		}

		public override void Teleport(Rect position) {
			base.SetPosition(position);
			transition.position = position;
		}

		public void UpdatePosition() {
			Teleport(transition.position);
		}

		public override void ReloadView() {
			base.ReloadView();

			try {
				title = transition.GetTitle();
				//if(titleIcon != null)
				//	titleIcon.image = uNodeEditorUtility.GetTypeIcon(nodeObject.GetNodeIcon());
			}
			catch(Exception ex) {
				title = "Error ???";
				Debug.LogException(ex);
			}

			UpdatePosition();

			{
				inputPort = AddInputFlowPort(new FlowInputData(transition.enter));
				inputPort.SetEdgeConnector<TransitionEdgeView>(TransitionEdgeListener.Default);
				inputPort.pickingMode = PickingMode.Ignore;
				inputPort.SetEnabled(false);
				outputPort = AddOutputFlowPort(new FlowOutputData(transition.exit));
				outputPort.SetEdgeConnector<TransitionEdgeView>(TransitionEdgeListener.Default);
				outputPort.pickingMode = PickingMode.Ignore;
				outputPort.SetEnabled(false);
			}

			RefreshPorts();
		}
	}
}