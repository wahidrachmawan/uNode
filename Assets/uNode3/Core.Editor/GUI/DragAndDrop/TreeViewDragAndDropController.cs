using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors.UI {
	internal abstract class BaseReorderableDragAndDropController : ICollectionDragAndDropController {
		protected readonly BaseTreeView m_View;

		protected List<int> m_SelectedIndices;

		public BaseReorderableDragAndDropController(BaseTreeView view) {
			m_View = view;
			enableReordering = true;
		}

		public bool enableReordering { get; set; }

		public virtual bool CanStartDrag(IEnumerable<ITreeViewItemElement> itemIndices) {
			return enableReordering;
		}

		public virtual StartDragArgs SetupDragAndDrop(IEnumerable<ITreeViewItemElement> itemIndices, bool skipText = false) {
			m_SelectedIndices ??= new List<int>();
			m_SelectedIndices.Clear();

			Dictionary<string, object> genericDatas = new Dictionary<string, object>();
			IEnumerable<Object> genericReferences = Enumerable.Empty<Object>();

			var title = string.Empty;
			if(itemIndices != null) {
				foreach(var item in itemIndices) {
					var index = item.index;

					m_SelectedIndices.Add(index);

					if(skipText)
						continue;

					if(string.IsNullOrEmpty(title)) {
						var label = m_View.GetRootElementForIndex(index)?.Q<Label>();
						title = label != null ? label.text : $"Item {index}";
					}
					else {
						title = "<Multiple>";
						skipText = true;
					}
					var dragDatas = item.GetDragGenericData();
					if(dragDatas != null) {
						foreach(var (key, value) in dragDatas) {
							genericDatas[key] = value;
						}
					}
					var dragReferences = item.GetDraggedReferences();
					if(dragReferences != null) {
						genericReferences = genericReferences.Concat(dragReferences);
					}
				}
			}

			m_SelectedIndices.Sort();

			var result = new StartDragArgs(title, m_View);
			foreach(var (key, value) in genericDatas) {
				result.SetGenericData(key, value);
			}
			result.SetUnityObjectReferences(genericReferences.Distinct());
			return result;
		}

		public abstract DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args);
		public abstract void OnDrop(IListDragAndDropArgs args);
	}

	internal class TreeViewReorderableDragAndDropController : BaseReorderableDragAndDropController {
		protected readonly BaseTreeView m_TreeView;

		public TreeViewReorderableDragAndDropController(BaseTreeView view) : base(view) {
			m_TreeView = view;
			enableReordering = true;
		}

		public override DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args) {
			if(!enableReordering)
				return DragVisualMode.Rejected;

			return args.dragAndDropData.userData == m_TreeView ? DragVisualMode.Move : DragVisualMode.Rejected;
		}

		public override void OnDrop(IListDragAndDropArgs args) {
			var insertAtId = m_TreeView.GetIdForIndex(args.insertAtIndex);
			var insertAtParentId = m_TreeView.GetParentIdForIndex(args.insertAtIndex);
			var insertAtChildIndex = m_TreeView.viewController.GetChildIndexForId(insertAtId);

			if(args.dragAndDropPosition == DragAndDropPosition.OverItem || (insertAtId == -1 && insertAtParentId == -1 && insertAtChildIndex == -1)) {
				for(var i = 0; i < m_SelectedIndices.Count; i++) {
					var index = m_SelectedIndices[i];
					var id = m_TreeView.GetIdForIndex(index);

					var parentId = insertAtId;
					var childIndex = -1;
					m_TreeView.viewController.Move(id, parentId, childIndex, false);
				}
			}
			else {
				for(var i = m_SelectedIndices.Count - 1; i >= 0; --i) {
					var index = m_SelectedIndices[i];
					var id = m_TreeView.GetIdForIndex(index);

					var parentId = insertAtParentId;
					var childIndex = insertAtChildIndex;
					m_TreeView.viewController.Move(id, parentId, childIndex, false);
				}
			}

			m_TreeView.viewController.RebuildTree();
			m_TreeView.RefreshItems();
		}
	}



	internal class TreeViewCustomDragAndDropController : BaseReorderableDragAndDropController {
		protected readonly BaseTreeView m_TreeView;

		public delegate void ReorderDelegate(int id, int parentId, int childIndex);

		public ReorderDelegate reorderCallback;

		public TreeViewCustomDragAndDropController(BaseTreeView view, ReorderDelegate reorderCallback) : base(view) {
			m_TreeView = view;
			enableReordering = true;
			this.reorderCallback = reorderCallback;
		}

		public override DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args) {
			if(!enableReordering)
				return DragVisualMode.Rejected;

			return args.dragAndDropData.userData == m_TreeView ? DragVisualMode.Move : DragVisualMode.Rejected;
		}

		public override void OnDrop(IListDragAndDropArgs args) {
			var insertAtId = m_TreeView.GetIdForIndex(args.insertAtIndex);
			var insertAtParentId = m_TreeView.GetParentIdForIndex(args.insertAtIndex);
			var insertAtChildIndex = m_TreeView.viewController.GetChildIndexForId(insertAtId);

			if(args.dragAndDropPosition == DragAndDropPosition.OverItem || (insertAtId == -1 && insertAtParentId == -1 && insertAtChildIndex == -1)) {
				for(var i = 0; i < m_SelectedIndices.Count; i++) {
					var index = m_SelectedIndices[i];
					var id = m_TreeView.GetIdForIndex(index);

					var parentId = insertAtId;
					var childIndex = -1;
					m_TreeView.viewController.Move(id, parentId, childIndex, false);
					reorderCallback?.Invoke(id, parentId, childIndex);
				}
			}
			else {
				for(var i = m_SelectedIndices.Count - 1; i >= 0; --i) {
					var index = m_SelectedIndices[i];
					var id = m_TreeView.GetIdForIndex(index);

					var parentId = insertAtParentId;
					var childIndex = insertAtChildIndex;
					m_TreeView.viewController.Move(id, parentId, childIndex, false);
					reorderCallback?.Invoke(id, parentId, childIndex);
				}
			}

			m_TreeView.viewController.RebuildTree();
			m_TreeView.RefreshItems();
		}
	}

	internal class TreeViewUGraphElementDragAndDropController : BaseReorderableDragAndDropController {
		protected readonly BaseTreeView m_TreeView;
		protected readonly UGraphElementRef m_RootElement;

		public TreeViewUGraphElementDragAndDropController(BaseTreeView view, UGraphElement rootElement) : base(view) {
			m_TreeView = view;
			m_RootElement = new UGraphElementRef(rootElement);
			enableReordering = true;
		}

		public override DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args) {
			if(!enableReordering)
				return DragVisualMode.Rejected;

			return args.dragAndDropData.userData == m_TreeView ? DragVisualMode.Move : DragVisualMode.Rejected;
		}

		public override void OnDrop(IListDragAndDropArgs args) {
			var insertAtId = m_TreeView.GetIdForIndex(args.insertAtIndex);
			var insertAtParentId = m_TreeView.GetParentIdForIndex(args.insertAtIndex);
			var insertAtChildIndex = m_TreeView.viewController.GetChildIndexForId(insertAtId);

			if(args.dragAndDropPosition == DragAndDropPosition.OverItem || (insertAtId == -1 && insertAtParentId == -1 && insertAtChildIndex == -1)) {
				for(var i = 0; i < m_SelectedIndices.Count; i++) {
					var index = m_SelectedIndices[i];
					var id = m_TreeView.GetIdForIndex(index);

					var parentId = insertAtId;
					var childIndex = -1;
					m_TreeView.viewController.Move(id, parentId, childIndex, false);
					{
						if(m_RootElement.UnityObject != null) {
							uNodeEditorUtility.RegisterUndo(m_RootElement.UnityObject, "reorder");
						}
						var selectedElement = m_TreeView.GetItemDataForId<UGraphElement>(id);
						var parentElement = m_TreeView.GetItemDataForId<UGraphElement>(parentId);
						if(parentElement != null) {
							selectedElement.SetParent(parentElement);
						}
						else {
							var rootID = m_TreeView.GetItemDataForId<UGraphElement>(m_TreeView.GetRootIds().First()).parent.id;
							selectedElement.ForeachInParents(p => {
								if(p.id == rootID) {
									var droppedElement = m_TreeView.GetItemDataForId<UGraphElement>(insertAtId);
									selectedElement.SetParent(p);
									selectedElement.SetSiblingIndex(p.childCount);
								}
							});
						}
					}
				}
			}
			else {
				for(var i = m_SelectedIndices.Count - 1; i >= 0; --i) {
					var index = m_SelectedIndices[i];
					var id = m_TreeView.GetIdForIndex(index);

					var parentId = insertAtParentId;
					var childIndex = insertAtChildIndex;
					m_TreeView.viewController.Move(id, parentId, childIndex, false);


					{
						if(m_RootElement.UnityObject != null) {
							uNodeEditorUtility.RegisterUndo(m_RootElement.UnityObject, "reorder");
						}
						var selectedElement = m_TreeView.GetItemDataForId<UGraphElement>(id);
						var parentElement = m_TreeView.GetItemDataForId<UGraphElement>(parentId);
						if(parentElement != null) {
							var droppedElement = m_TreeView.GetItemDataForId<UGraphElement>(insertAtId);
							selectedElement.SetParent(parentElement);
							selectedElement.PlaceBehind(droppedElement);
							//selectedElement.SetSiblingIndex(childIndex);
						} else {
							var rootID = m_TreeView.GetItemDataForId<UGraphElement>(insertAtId).parent.id;
							selectedElement.ForeachInParents(p => {
								if(p.id == rootID) {
									var droppedElement = m_TreeView.GetItemDataForId<UGraphElement>(insertAtId);
									selectedElement.SetParent(p);
									selectedElement.PlaceBehind(droppedElement);
									//selectedElement.SetSiblingIndex(childIndex);
								}
							});
						}
					}
				}
			}

			m_TreeView.viewController.RebuildTree();
			m_TreeView.RefreshItems();
		}
	}
}