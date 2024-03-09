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
	public class TransitionView : UNodeView {
		public TransitionEvent transition;

		public PortView input { private set; get; }
		public PortView output { private set; get; }

		public override bool fullReload => true;

		public override void Initialize(UGraphView owner, NodeObject node) {
			nodeObject = node;
			this.transition = node.node as TransitionEvent;
			AddToClassList("transition");
			this.AddStyleSheet("uNodeStyles/NativeNodeStyle");
			this.AddStyleSheet(UIElementUtility.Theme.nodeStyle);
			Initialize(owner);
			node.Register();
			ReloadView();

			border.style.overflow = Overflow.Visible;

			titleIcon.RemoveFromHierarchy();
			m_CollapseButton.RemoveFromHierarchy();

			RegisterCallback<MouseDownEvent>((e) => {
				if(e.button == 0 && e.clickCount == 2) {
					ActionPopupWindow.ShowWindow(owner.GetScreenMousePosition(e), transition.name,
						(ref object obj) => {
							object str = EditorGUILayout.TextField(obj as string);
							if(obj != str) {
								obj = str;
								transition.nodeObject.name = obj as string;
								if(GUI.changed) {
									uNodeGUIUtility.GUIChanged(transition, UIChangeType.Average);
								}
							}
						}).headerName = "Edit name";
					e.StopImmediatePropagation();
				}
			});
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

			transition.position = new Rect(newPos.x - transition.node.position.x, newPos.y - transition.node.position.y, 0, 0);
		}

		public override void Teleport(Rect position) {
			base.SetPosition(position);
			transition.position = new Rect(position.x - transition.node.position.x, position.y - transition.node.position.y, 0, 0);
		}

		public void UpdatePosition() {
			Rect pos = transition.node.position;
			pos.x += transition.position.x;
			pos.y += transition.position.y;
			Teleport(pos);
		}

		public override void ReloadView() {
			base.ReloadView();

			title = transition.GetTitle();

			//titleIcon.image = uNodeEditorUtility.GetTypeIcon(transition.GetNodeIcon());

			UpdatePosition();
			InitializeView();
			RefreshPorts();
		}

		public override void RegisterUndo(string name = "") {
			uNodeEditorUtility.RegisterUndo(graphData.owner, name);
		}

		protected virtual void InitializeView() {
			input = AddInputFlowPort(new FlowInputData(transition.enter));
			input.SetEnabled(false);
			output = AddOutputFlowPort(new FlowOutputData(transition.exit));
			output.DisplayProxyTitle(true);
		}
	}

	public class TransitionBlockView : TransitionView, INodeBlock, IDropTarget {
		public BlockNodeHandler handler;

		public BlockType blockType { get; protected set; }
		public UNodeView nodeView => this;

		public BlockContainer blocks { get; private set; }
		public List<UNodeView> blockViews => m_blockViews;

		protected List<UNodeView> m_blockViews = new List<UNodeView>();

		public override void Initialize(UGraphView owner, NodeObject node) {
			handler = new BlockNodeHandler(this);
			base.Initialize(owner, node);
		}

		protected UNodeView CreateBlock(NodeObject node) {
			if(node == null)
				return null;
			if(owner.cachedNodeMap.TryGetValue(node, out var cachedView)) {
				//Clear cached views
				cachedView.RemoveFromHierarchy();
				owner.nodeViews.Remove(cachedView);
			}

			var viewType = UIElementUtility.GetNodeViewTypeFromType(node.node.GetType());

			if(viewType == null)
				viewType = typeof(BaseNodeView);

			var nodeView = Activator.CreateInstance(viewType) as UNodeView;
			nodeView.ownerBlock = this;
			nodeView.Initialize(owner, node);

			owner.nodeViews.Add(nodeView);
			owner.nodeViewsPerNode[node] = nodeView;
			owner.cachedNodeMap[node] = nodeView;

			return nodeView;
		}

		protected void InitializeBlocks(BlockContainer blocks, BlockType blockType) {
			if(blocks != null) {
				this.blocks = blocks;
				this.blockType = blockType;
				border.SetToNoClipping();
				for(int i = 0; i < blocks.childCount; i++) {
					var obj = blocks.GetChild(i) as NodeObject;
					if(obj == null)
						continue;
					obj.position.position = Vector2.zero;
					var block = CreateBlock(obj);
					if(block == null)
						continue;
					handler.blockElement.Add(block);
					blockViews.Add(block);
				}
			}
		}

		public override void ReloadView() {
			for(int i = 0; i < blockViews.Count; i++) {
				blockViews[i].RemoveFromHierarchy();
			}
			blockViews.Clear();
			base.ReloadView();
			handler.ToggleBlockHint(blockViews.Count == 0);
		}

		public override void InitializeEdge() {
			base.InitializeEdge();
			for(int i = 0; i < blockViews.Count; i++) {
				blockViews[i].InitializeEdge();
			}
		}

		bool IDropTarget.CanAcceptDrop(List<ISelectable> selection) {
			return ((IDropTarget)handler).CanAcceptDrop(selection);
		}

		bool IDropTarget.DragEnter(DragEnterEvent evt, IEnumerable<ISelectable> selection, IDropTarget enteredTarget, ISelection dragSource) {
			return ((IDropTarget)handler).DragEnter(evt, selection, enteredTarget, dragSource);
		}

		bool IDropTarget.DragExited() {
			return ((IDropTarget)handler).DragExited();
		}

		bool IDropTarget.DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource) {
			return ((IDropTarget)handler).DragLeave(evt, selection, leftTarget, dragSource);
		}

		bool IDropTarget.DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource) {
			return ((IDropTarget)handler).DragPerform(evt, selection, dropTarget, dragSource);
		}

		bool IDropTarget.DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource) {
			return ((IDropTarget)handler).DragUpdated(evt, selection, dropTarget, dragSource);
		}
	}
}