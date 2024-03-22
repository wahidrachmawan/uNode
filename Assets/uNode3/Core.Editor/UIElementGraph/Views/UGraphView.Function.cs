using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public partial class UGraphView {
		private class AsyncLoadingData {
			public System.Diagnostics.Stopwatch watch;
			public int maxMilis = 5;
			public Action<string, string, float> displayProgressCallback;
			public bool isStopped { get; private set; }

			public void DisplayProgress(string title, string info, float progress) {
				displayProgressCallback?.Invoke(title, info, progress);
			}

			public bool needWait => isStopped || watch.ElapsedMilliseconds > maxMilis;

			public void Stop() {
				isStopped = true;
			}
		}

		public void Initialize(UIElementGraph graph) {
			this.graph = graph;

			ToggleMinimap(UIElementUtility.Theme.enableMinimap);
			ToogleGrid(uNodePreference.GetPreference().showGrid);
			UpdatePosition();
			MarkRepaint(false);
			hasInitialize = true;
		}

		IEnumerator InitializeNodeViews(AsyncLoadingData data, bool reloadExistingNodes) {
			//graph.RefreshEventNodes();
			// if(!reloadExistingNodes && _needReloadedViews.Count > 0) {
			// 	reloadExistingNodes = true;
			// }

			void RecursiveAddBlockNodes(INodeBlock nodeBlock) {
				foreach(var view in nodeBlock.blockViews) {
					if(!nodeViewsPerNode.ContainsKey(view.nodeObject)) {
						nodeViews.Add(view);
						nodeViewsPerNode[view.nodeObject] = view;
						cachedNodeMap[view.nodeObject] = view;
					}
					if(view is INodeBlock nestedBlock) {
						RecursiveAddBlockNodes(nestedBlock);
					}
				}
			}

			var nodes = graph.graphData.nodes.ToArray();
			float count = nodes.Length;
			float currentCount = 0;
			foreach(var node in nodes) {
				if(node != null) {
					try {
						var view = RepaintNode(node, reloadExistingNodes);
						if(view is INodeBlock) {
							RecursiveAddBlockNodes(view as INodeBlock);
						}
					}
					catch(Exception ex) {
						Debug.LogException(ex);
					}
					if(data.needWait) {
						data.DisplayProgress("Loading graph", "Initialize Node", currentCount / count);
						data.watch.Restart();
						yield return null;
					}
					currentCount++;
				}
			}
			//if(graph.regions != null) {
			//	foreach(var region in graph.regions.ToArray()) {
			//		if(region != null) {
			//			try {
			//				RepaintNode(data, region, reloadExistingNodes);
			//			}
			//			catch(Exception ex) {
			//				Debug.LogException(ex, region.owner);
			//				uNodeDebug.LogException(ex, region);
			//			}
			//			if(data.needWait) {
			//				data.DisplayProgress("Loading graph", "Initialize Node", 1);
			//				data.watch.Restart();
			//				yield return null;
			//			}
			//		}
			//	}
			//}
			_needReloadedViews.Clear();
		}

		IEnumerator InitializeEdgeViews(AsyncLoadingData data) {
			HashSet<Connection> connections = new HashSet<Connection>();
			foreach(var nodeView in nodeViews) {
				foreach(var port in nodeView.nodeObject.FlowOutputs) {
					foreach(var c in port.connections) {
						connections.Add(c);
					}
				}
				foreach(var port in nodeView.nodeObject.ValueInputs) {
					foreach(var c in port.connections) {
						connections.Add(c);
					}
				}
				//foreach(var c in nodeView.nodeObject.Connections) {
				//	if(c.isValid) {
				//		connections.Add(c);
				//	}
				//}
			}
			float count = connections.Count;
			float currentCount = 0;
			foreach(var connection in connections) {
				var input = PortUtility.GetPort(connection.Input, this);
				var output = PortUtility.GetPort(connection.Output, this);
				if(input == null || output == null)
					continue;
				var edgeData = new EdgeData(connection, input, output);
				EdgeView edgeView = null;
				foreach(var p in GraphProcessor) {
					edgeView = p.InitializeEdge(this, edgeData);
					if(edgeView != null) {
						if(edgeView.input != null && edgeView.output != null) {
							Connect(edgeView, false);
						}
						break;
					}
				}
				if(data != null && data.needWait) {
					data.DisplayProgress("Loading graph", "Initialize Edges", currentCount / count);
					data.watch.Restart();
					yield return null;
				}
				currentCount++;
			}
			foreach(var nodeView in nodeViews) {
				nodeView.InitializeEdge();
			}
		}

		#region Repaint
		UNodeView RepaintNode(NodeObject node, bool fullReload) {
			foreach(var p in GraphProcessor) {
				var v = p.RepaintNode(this, node, fullReload);
				if(v != null) {
					return v;
				}
			}
			if(cachedNodeMap.TryGetValue(node, out var view) && view != null && view.owner == this) {
				try {
					if(!nodeViewsPerNode.ContainsKey(node)) {
						//Ensure to add element when the node is not in the current scope
						AddElement(view);
						nodeViews.Add(view);
						nodeViewsPerNode[node] = view;
					}
					if(view is IRefreshable) {
						(view as IRefreshable).Refresh();
					}
					if(fullReload || _needReloadedViews.Contains(view) || view.autoReload) {
						view.ReloadView();
						view.MarkDirtyRepaint();
					} else {
						view.expanded = view.nodeObject.nodeExpanded;
					}
					return view;
				} catch(Exception ex) {
					Debug.LogException(ex);
					if(!nodeViewsPerNode.ContainsKey(node)) {
						//Add a view using default settings
						return AddNodeView(node);
					}
				}
			} else {
				return AddNodeView(node);
			}
			return null;
		}

		public bool requiredReload { get; private set; }
		protected bool _fullReload = true;
		protected UGraphElement _reloadedGraphScope;
		protected Action executeAfterReload;
		protected uNodeAsyncOperation repaintProgress;
		private int reloadID;

		public void MarkRepaint() {
			MarkRepaint(false);
		}

		public void FullReload() {
			MarkRepaint(true);
		}

		public void MarkRepaint(bool fullReload) {
			if(fullReload) {
				_fullReload = true;
			}
			if(!requiredReload) {
				requiredReload = true;
				repaintProgress?.Stop();
				var isUndoRedo = uNodeUtility.undoRedoPerformed;
				uNodeThreadUtility.ExecuteOnce(() => {
					int currID = ++reloadID;
					requiredReload = false;
					autoHideNodes = false;
					AutoHideGraphElement.ResetVisibility(this);
					float currentZoom = graph.zoomScale;
					SetZoomScale(1, true);
					contentViewContainer.SetOpacity(0);
					DisplayProgressBar("Loading graph", "", 0);

					var watch = new System.Diagnostics.Stopwatch();
					watch.Start();
					var data = new AsyncLoadingData() {
						watch = watch,
						maxMilis = uNodePreference.preferenceData.maxReloadMilis,
						displayProgressCallback = DisplayProgressBar,
					};
					if(uNodeUtility.isPlaying) {
						data.maxMilis = Mathf.Max(data.maxMilis, 150);
					}
					var taskProgress = uNodeThreadUtility.Task(ReloadView(data), 
						onFinished: () => {
							isLoading = true;
							watch.Stop();
							SetZoomScale(currentZoom, false);
							contentViewContainer.SetOpacity(1);
							ClearProgressBar();
							uNodeThreadUtility.ExecuteAfter(1, () => {
								executeAfterReload?.Invoke();
								executeAfterReload = null;
								uNodeThreadUtility.ExecuteAfter(1, () => {
									if(isUndoRedo && Selection.activeObject is CustomInspector customInspector) {
										var nodes = customInspector.editorData.selectedNodes.ToArray();
										if(nodes.Length > 0) {
											foreach(var node in nodes) {
												var view = GetNodeView(node);
												if(view != null) {
													base.AddToSelection(view);
												}
											}
										}
									}
									autoHideNodes = true;
									AutoHideGraphElement.UpdateVisibility(this);
									if(reloadID == currID)
										isLoading = false;
								});
							});
						}, 
						onStopped: () => {
							reloadID++;
							data.Stop();
						});
					repaintProgress = taskProgress;
				}, typeof(UGraphView));
			}
		}

		protected bool needReloadNodes;
		protected HashSet<UNodeView> _needReloadedViews = new HashSet<UNodeView>();

		public void MarkRepaint(IEnumerable<UNodeView> views) {
			if(views == null)
				return;
			if(requiredReload) {
				needReloadNodes = false;
				// _needReloadedViews.Clear();
				foreach(var v in views) {
					_needReloadedViews.Add(v);
				}
				return;
			}
			if(!needReloadNodes) {
				needReloadNodes = true;
				uNodeThreadUtility.Queue(() => {
					if(needReloadNodes && !requiredReload) {
						Repaint(_needReloadedViews.ToArray());
						_needReloadedViews.Clear();
					}
					needReloadNodes = false;
				});
			}
			foreach(var v in views) {
				_needReloadedViews.Add(v);
			}
		}

		public void MarkRepaint(params UNodeView[] views) {
			MarkRepaint(views as IEnumerable<UNodeView>);
		}

		public void MarkRepaint(NodeObject node) {
			if(node == null)
				return;
			UNodeView view;
			if(nodeViewsPerNode.TryGetValue(node, out view)) {
				MarkRepaint(view);
			}
		}

		public void MarkRepaintEdges() {
			MarkRepaint(new UNodeView[0]);
		}

		void Repaint(params UNodeView[] views) {
			for(int i = 0; i < views.Length; i++) {
				if(views[i] == null || views[i].nodeObject == null)
					continue;
				views[i].ReloadView();
			}
			//Update edges.
			RemoveEdges();
			InitializeEdgeViews(null).MoveNext();
		}

		void Repaint(params NodeObject[] nodes) {
			for(int i = 0; i < nodes.Length; i++) {
				if(nodeViewsPerNode.TryGetValue(nodes[i], out var view) && view != null && view.nodeObject != null) {
					view.ReloadView();
				}
			}
			//Update edges.
			RemoveEdges();
			InitializeEdgeViews(null).MoveNext();
		}
		#endregion

		#region ReloadView
		IEnumerator ReloadView(AsyncLoadingData data) {
			isLoading = true;
			graphLayout = graphData.graph != null ? graphData.graph.GraphData.graphLayout : GraphLayout.Vertical;
			if(graphLayout == GraphLayout.Vertical) {
				EnableInClassList("vertical-graph", true);
				EnableInClassList("horizontal-graph", false);
			} else {
				EnableInClassList("vertical-graph", false);
				EnableInClassList("horizontal-graph", true);
			}
			if(uNodePreference.GetPreference().forceReloadGraph) {
				_fullReload = true;
				cachedNodeMap.Clear();
			} else if(_reloadedGraphScope != graphData.currentCanvas) {
				_reloadedGraphScope = graphData.currentCanvas;
				if(!_fullReload) {
					//Ensure to remove all node views when the graph scope is different
					RemoveNodeViews(false);
				}
			}
			//Ensure we get the up to date datas
			graphData.Refresh();
			//Remove all edges
			RemoveEdges();
			if(_fullReload) {
				// Remove everything node view
				RemoveNodeViews(true);
			} else {
				foreach(var nodeView in nodeViews) {
					if(nodeView.fullReload) {
						if(!nodeView.isBlock)
							RemoveElement(nodeView);
						cachedNodeMap.Remove(nodeView.nodeObject);
						nodeViewsPerNode.Remove(nodeView.nodeObject);
					}
				}
				nodeViews.RemoveAll(n => n.fullReload);
			}
			// re-add with new up to date datas
			var initNodes = InitializeNodeViews(data, _fullReload);
			while(initNodes.MoveNext()) {
				yield return null;
			}
			data.DisplayProgress("Loading graph", "Initialize Edges", 1);
			var initEdges = InitializeEdgeViews(data);
			while(initEdges.MoveNext()) {
				yield return null;
			}

			ToggleMinimap(UIElementUtility.Theme.enableMinimap);
			ToogleGrid(uNodePreference.GetPreference().showGrid);
			//Mark full reload to false for more faster reload after full reload
			_fullReload = false;
			yield break;
		}
		#endregion

		#region Utility
		private PropertyInfo _keepPixelCacheProperty;
		protected void SetPixelCachedOnBoundChanged(bool value) {
			if(panel != null) {
				if(_keepPixelCacheProperty == null) {
					_keepPixelCacheProperty = panel.GetType().GetProperty("keepPixelCacheOnWorldBoundChange", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				}
				if(_keepPixelCacheProperty != null) {
					_keepPixelCacheProperty.SetValueOptimized(panel, value);
				}
			}
		}

		void RemoveNodeViews(bool includingCache) {
			foreach(var nodeView in nodeViews) {
				if(!nodeView.isBlock)
					RemoveElement(nodeView);
				if(includingCache)
					cachedNodeMap.Remove(nodeView.nodeObject);
			}
			nodeViews.Clear();
			nodeViewsPerNode.Clear();
		}

		internal void RemoveView(UNodeView view) {
			if(view == null) return;
			RemoveElement(view);
			cachedNodeMap.Remove(view.nodeObject);
			nodeViewsPerNode.Remove(view.nodeObject);
			nodeViews.Remove(view);
		}

		public void RemoveEdges() {
			foreach(var edge in edgeViews)
				RemoveElement(edge);
			edgeViews.Clear();
		}
		#endregion
	}
}
