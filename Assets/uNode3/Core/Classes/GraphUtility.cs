using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections;
using System.Linq;

namespace MaxyGames.UNode {
	public static class GraphUtility {
		#region Place Fits
		public class PlaceFit {
			class PlaceFitData {
				public NodeObject node;

				public List<PlaceFitData> inputs = new();
				public List<PlaceFitData> flows = new();

				public List<NodeObject> GetNodes() {
					var list = new List<NodeObject>() { node };
					foreach(var data in inputs) {
						list.AddRange(data.GetNodes());
					}
					foreach(var data in flows) {
						list.AddRange(data.GetNodes());
					}
					return list;
				}
			}

			private static Vector2 flowSpacing = new Vector2(20, 45);
			private static Vector2 valueSpacing = new Vector2(25, 25);

			public static NodeObject[] PlaceFitNodes(NodeObject node) {
				var nodes = CG.Nodes.FindAllConnections(node, true, true, false, false);
				foreach(var n in nodes) {
					if(n.position.width == 0) {
						n.position.width = 200;
					}
					if(n.position.height == 0) {
						n.position.height = 100;
					}
				}
				var exceptionNodes = CG.Nodes.FindAllConnections(node, false, false, true, false);
				exceptionNodes.Remove(node);
				foreach(var n in exceptionNodes) {
					if(nodes.Contains(n)) {
						nodes.Remove(n);
					}
				}

				var data = CreateData(node, out var datas);
				DoPlaceFit(data);
				return datas.Select(d => d.node).ToArray();
			}

			private static Rect GetNodeRect(IList<NodeObject> nodes) {
				Rect rect = Rect.zero;
				if(nodes.Count > 0) {
					rect = nodes[0].position;
				}
				foreach(var data in nodes) {
					rect = Encompass(rect, data.position);
				}
				return rect;
			}

			private static Rect Encompass(Rect a, Rect b) {
				Rect result = default(Rect);
				result.xMin = Math.Min(a.xMin, b.xMin);
				result.yMin = Math.Min(a.yMin, b.yMin);
				result.xMax = Math.Max(a.xMax, b.xMax);
				result.yMax = Math.Max(a.yMax, b.yMax);
				return result;
			}

			private static void TeleportNodes(IList<NodeObject> nodes, Vector2 position, bool fromCenter = false) {
				if(fromCenter) {
					Vector2 center = Vector2.zero;
					foreach(var node in nodes) {
						center.x += node.position.x;
						center.y += node.position.y;
					}
					center /= nodes.Count;
					foreach(var node in nodes) {
						node.position.x = (node.position.x - center.x) + position.x;
						node.position.y = (node.position.y - center.y) + position.y;
					}
				}
				else {
					Vector2 pos = Vector2.zero;
					if(nodes.Count > 0) {
						pos = nodes[0].position.position;
					}
					foreach(var node in nodes) {
						var p = node.position;
						if(pos.x > p.x) {
							pos.x = p.x;
						}
						if(pos.y > p.y) {
							pos.y = p.y;
						}
					}
					foreach(var node in nodes) {
						node.position.x = (node.position.x - pos.x) + position.x;
						node.position.y = (node.position.y - pos.y) + position.y;
					}
				}
			}

			private static void DoPlaceFit(PlaceFitData tree) {
				if(tree.inputs.Count > 0) {
					var parentPos = tree.node.position;
					List<NodeObject> listNodes = new List<NodeObject>();
					float offset = 0;
					foreach(var childTree in tree.inputs) {
						DoPlaceFit(childTree);
						var nodes = childTree.GetNodes();
						var totalRect = GetNodeRect(nodes);

						TeleportNodes(nodes, new Vector2(parentPos.x - totalRect.width - valueSpacing.x, parentPos.y + offset), false);
						offset += totalRect.height + valueSpacing.y;
						listNodes.AddRange(nodes);
					}
					if(tree.inputs.Count == 1) {
						var rect = GetNodeRect(listNodes);
						var sourcePosition = GetNodeRect(new[] { tree.inputs[0].node });
						MoveNodes(listNodes, new Vector2(0, -(sourcePosition.y - rect.y) + (parentPos.height - sourcePosition.height) / 2));
					}
					else {
						MoveNodes(listNodes, new Vector2(0, ((parentPos.height - GetNodeRect(listNodes).height) / 2)));
					}
				}
				if(tree.flows.Count > 0) {
					var parentPos = tree.node.position;
					float parentY = parentPos.y + parentPos.height;
					{
						List<NodeObject> nodeViews = new List<NodeObject>();
						if(tree.inputs.Count > 0) {
							foreach(var childTree in tree.inputs) {
								nodeViews.AddRange(childTree.GetNodes());
							}
						}
						if(nodeViews.Count > 0) {
							foreach(var n in nodeViews) {
								var rect = GetNodeRect(new[] { n });
								if(rect.y + rect.height > parentY) {
									parentY = rect.y + rect.height;
								}
							}
						}
					}
					if(tree.flows.Count > 0) {
						List<NodeObject> listNodes = new List<NodeObject>();
						float offset = 0;
						foreach(var childTree in tree.flows) {
							DoPlaceFit(childTree);
							var nodes = childTree.GetNodes();
							var totalRect = GetNodeRect(nodes);
							var dist = Mathf.Abs(GetNodeRect(nodes).width - totalRect.width);

							TeleportNodes(nodes, new Vector2(parentPos.x + offset + dist, parentY + flowSpacing.y), false);
							offset += totalRect.width + flowSpacing.x + dist;
							listNodes.AddRange(nodes);
						}
						if(tree.flows.Count == 1) {
							var rect = GetNodeRect(listNodes);
							var sourcePosition = GetNodeRect(new[] { tree.flows[0].node });
							//var parentPosition = GetNodeRect(tree.node);
							//TeleportNodes(listNodes, new Vector2(parentPosition.x - (sourcePosition.x - rect.x) - ((sourcePosition.width - parentPos.width) / 2), rect.y), false);
							MoveNodes(listNodes, new Vector2(-(sourcePosition.x - rect.x) + (parentPos.width - sourcePosition.width) / 2, 0));
						}
						else {
							var rect = GetNodeRect(listNodes);
							//MoveNodes(listNodes, new Vector2(((parentPos.width - rect.width) / 2), 0));

							var sourcePosition = GetNodeRect(new[] { tree.node });
							var startFlowPosition = GetNodeRect(new[] { tree.flows[0].node });
							var endFlowPosition = GetNodeRect(new[] { tree.flows[tree.flows.Count - 1].node });

							TeleportNodes(listNodes, new Vector2(sourcePosition.x - (startFlowPosition.x - rect.x) - ((endFlowPosition.x - startFlowPosition.x) / 2), rect.y));
						}
					}
				}
			}

			private static PlaceFitData CreateData(NodeObject node, out List<PlaceFitData> datas) {
				HashSet<NodeObject> visited = new HashSet<NodeObject>();
				var flowNodes = CG.Nodes.FindAllConnections(node, true, false, false, false);
				var valueNodes = CG.Nodes.FindAllConnections(node, false, true, false, false);
				var data = new PlaceFitData() {
					node = node,
				};
				List<PlaceFitData> allDatas = new List<PlaceFitData> {
					data
				};
				datas = allDatas;
				visited.Add(node);

				List<PlaceFitData> flowData = new List<PlaceFitData>();

				void RecursiveForFlow(PlaceFitData data, HashSet<NodeObject> included, HashSet<NodeObject> visited) {
					foreach(var port in data.node.FlowOutputs) {
						var targets = port.connections.Where(c => c.isProxy == false).Select(c => c.input.node);
						foreach(var target in targets) {
							if(included.Contains(target) && visited.Add(target)) {
								var childData = new PlaceFitData() {
									node = target,
								};
								data.flows.Add(childData);
								flowData.Add(childData);
								allDatas.Add(childData);
								RecursiveForFlow(childData, included, visited);
							}
						}
					}
				}

				void RecursiveForValue(PlaceFitData data, HashSet<NodeObject> included, HashSet<NodeObject> visited) {
					foreach(var port in data.node.ValueInputs) {
						var targets = port.connections.Where(c => c.isProxy == false).Select(c => c.output.node);
						foreach(var target in targets) {
							var other = target;
							while(other.node is INodeAsEdge) {
								var t = other.ValueInputs.FirstOrDefault(p => p.isValid && p.GetTargetNode() != null);
								if(t != null) {
									other = t.GetTargetNode();
								}
								else {
									other = null;
									break;
								}
							}

							if(other != null && included.Contains(other) && visited.Add(other)) {
								var childData = new PlaceFitData() {
									node = other,
								};
								data.inputs.Add(childData);
								allDatas.Add(childData);
								RecursiveForValue(childData, included, visited);
							}
						}
					}
				}
				RecursiveForFlow(data, flowNodes, visited);
				RecursiveForValue(data, valueNodes, visited);

				foreach(var flow in flowData) {
					var included = CG.Nodes.FindAllConnections(flow.node, false, true, false, false);
					RecursiveForValue(flow, included, visited);
				}
				foreach(var n in visited) {
					if(n.position.width == 0) {
						n.position.width = 200;
					}
					if(n.position.height == 0) {
						n.position.height = 100;
					}
				}
				return data;
			}
		}
		#endregion

		#region MoveNodes
		/// <summary>
		/// Move the node to position
		/// </summary>
		/// <param name="position"></param>
		/// <param name="nodes"></param>
		public static void MoveNodes(Vector2 position, params NodeObject[] nodes) {
			if(nodes.Length == 0)
				throw new ArgumentNullException();
			MoveNodes(nodes, position);
		}

		/// <summary>
		/// Move the node to position
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="position"></param>
		public static void MoveNodes(IEnumerable<NodeObject> nodes, Vector2 position) {
			foreach(var node in nodes) {
				node.position.x += position.x;
				node.position.y += position.y;
			}
		}
		#endregion

		#region TeleportNodes
		/// <summary>
		/// Teleport the node to position
		/// </summary>
		/// <param name="position"></param>
		/// <param name="nodes"></param>
		public static void TeleportNodes(Vector2 position, params NodeObject[] nodes) {
			TeleportNodes(nodes, position);
		}

		/// <summary>
		/// Teleport the node to position
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="position"></param>
		public static void TeleportNodes(IList<NodeObject> nodes, Vector2 position) {
			Vector2 center = Vector2.zero;
			foreach(var node in nodes) {
				center.x += node.position.x;
				center.y += node.position.y;
			}
			center /= nodes.Count;
			foreach(var node in nodes) {
				node.position.x = (node.position.x - center.x) + position.x;
				node.position.y = (node.position.y - center.y) + position.y;
			}
		}
		#endregion

		#region GetNodeRect
		/// <summary>
		/// Get the node Rect
		/// </summary>
		/// <param name="node"></param>
		/// <param name="position"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public static Rect GetNodeRect(NodeObject node, Vector2 position, Vector2 size = new Vector2()) {
			return new Rect(node.position.x + position.x, (node.position.y + position.y) - 17, size.x, size.y);
		}

		/// <summary>
		/// Get the node Rect
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static Rect GetNodeRect(params NodeObject[] nodes) {
			return GetNodeRect(nodes.ToList());
		}

		/// <summary>
		/// Get the node Rect
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static Rect GetNodeRect(IList<NodeObject> nodes) {
			if(nodes == null || nodes.Count == 0)
				return Rect.zero;
			if(nodes.Count == 1) {
				return nodes[0].position;
			}
			Rect rect = nodes[0].position;
			foreach(var node in nodes) {
				if(rect.width < node.position.x + node.position.width) {
					rect.width = node.position.x + node.position.width;
				}
				if(rect.height < node.position.y + node.position.height) {
					rect.height = node.position.y + node.position.height;
				}
				if(rect.x > node.position.x) {
					rect.x = node.position.x;
				}
				if(rect.y > node.position.y) {
					rect.y = node.position.y;
				}
			}
			rect.width -= rect.x;
			rect.height -= rect.y;
			return rect;
		}

		public static List<NodeObject> GetNodeFromRect(Rect rect, IList<NodeObject> nodes) {
			List<NodeObject> list = new List<NodeObject>();
			foreach(var n in nodes) {
				if(n != null && rect.Overlaps(n.position)) {
					list.Add(n);
				}
			}
			return list;
		}
		#endregion

		#region Others
		public static bool IsParentOf(UGraphElement parent, UGraphElement child) {
			var current = child.parent;
			while(current != null) {
				if(parent == current) {
					return true;
				}
				current = current.parent;
			}
			return false;
		}
		#endregion
	}
}