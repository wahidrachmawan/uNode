using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.UNode.Editors {
	public static class PortUtility {

		public static PortView GetPort(UPort port, UGraphView graphView) {
			if(port is ValueInput vi) {
				return GetPort(vi, graphView);
			} else if(port is ValueOutput vo) {
				return GetPort(vo, graphView);
			} else if(port is FlowInput fi) {
				return GetPort(fi, graphView);
			} else if(port is FlowOutput fo) {
				return GetPort(fo, graphView);
			} else {
				throw new NotSupportedException("Unsupported port type: " + port.GetType());
			}
		}

		public static PortView GetPort(ValueInput port, UGraphView graphView) {
			if(port.node != null && graphView.nodeViewsPerNode.TryGetValue(port.node, out var nodeView)) {
				foreach(var p in nodeView.inputPorts) {
					if(p.portData.portValue == port) {
						return p;
					}
				}
			}
			return null;
		}

		public static PortView GetPort(ValueOutput port, UGraphView graphView) {
			if(port.node != null && graphView.nodeViewsPerNode.TryGetValue(port.node, out var nodeView)) {
				foreach(var p in nodeView.outputPorts) {
					if(p.portData.portValue == port) {
						return p;
					}
				}
			}
			return null;
		}

		public static PortView GetPort(FlowInput port, UGraphView graphView) {
			if(port.node != null && graphView.nodeViewsPerNode.TryGetValue(port.node, out var nodeView)) {
				foreach(var p in nodeView.inputPorts) {
					if(p.portData.portValue == port) {
						return p;
					}
				}
			}
			return null;
		}

		public static PortView GetPort(FlowOutput port, UGraphView graphView) {
			if(port.node != null && graphView.nodeViewsPerNode.TryGetValue(port.node, out var nodeView)) {
				foreach(var p in nodeView.outputPorts) {
					if(p.portData.portValue == port) {
						return p;
					}
				}
			}
			return null;
		}
	}
}