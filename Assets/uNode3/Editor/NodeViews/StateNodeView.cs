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

		public override void SetPosition(Rect newPos) {
			// if(newPos != targetNode.editorRect && uNodePreference.GetPreference().snapNode) {
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
			float xPos = newPos.x - targetNode.position.x;
			float yPos = newPos.y - targetNode.position.y;

			Teleport(newPos);

			if(xPos != 0 || yPos != 0) {
				foreach(var port in outputPorts) {
					if(port.isFlow && !port.enabledSelf) {
						TransitionView transition = port.portData.userData as TransitionView;
						if(transition != null) {
							Rect rect = transition.transition.position;
							rect.x += targetNode.position.x;
							rect.y += targetNode.position.y;
							transition.Teleport(rect);
						}
					}
				}
			}
		}
	}
}