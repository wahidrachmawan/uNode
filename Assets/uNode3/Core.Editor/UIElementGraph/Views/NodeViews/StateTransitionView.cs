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

		public override void Initialize(UGraphView owner, NodeObject node) {
			nodeObject = node;
			this.transition = node.node as Nodes.StateTransition;
			this.AddStyleSheet("uNodeStyles/NativeNodeStyle");
			this.AddStyleSheet(UIElementUtility.Theme.nodeStyle);
			Initialize(owner);
			node.Register();
			ReloadView();

			border.style.overflow = Overflow.Visible;

			titleIcon.RemoveFromHierarchy();
			m_CollapseButton.RemoveFromHierarchy();

			this.ExecuteAndScheduleAction(DoUpdate, 500);
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
				inputPort.SetEdgeConnector<TransitionEdgeView>();
				inputPort.pickingMode = PickingMode.Ignore;
				inputPort.SetEnabled(false);
				outputPort = AddOutputFlowPort(new FlowOutputData(transition.exit));
				outputPort.SetEdgeConnector<TransitionEdgeView>();
				outputPort.pickingMode = PickingMode.Ignore;
				//outputPort.SetEnabled(false);
			}
			titleContainer.RegisterCallback<MouseDownEvent>(e => {
				if(e.button == 0 && e.clickCount == 2) {
					owner.graphEditor.graphData.currentCanvas = targetNode.nodeObject;
					owner.graphEditor.Refresh();
					owner.graphEditor.UpdatePosition();
				}
			});

			RefreshPorts();
		}
	}
}