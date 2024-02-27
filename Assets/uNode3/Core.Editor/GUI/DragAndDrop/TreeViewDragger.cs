using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors.UI {
    internal class TreeViewDragger : DragEventsProcessor {
        internal struct DragPosition : IEquatable<DragPosition> {
            public int insertAtIndex;

            public ITreeViewItemElement element;
            public VisualElement rootContent {
                get {
                    VisualElement parent = element.element?.parent;
                    while(parent != null) {
                        if(parent.ClassListContains(BaseTreeView.itemContentContainerUssClassName)) {
                            return parent;
                        }
                        parent = parent.parent;
                    }
                    return null;
                }
            }
            public DragAndDropPosition dragAndDropPosition;

            public bool Equals(DragPosition other) {
                return insertAtIndex == other.insertAtIndex
                    && Equals(element, other.element)
                    && dragAndDropPosition == other.dragAndDropPosition;
            }

            public override bool Equals(object obj) {
                return obj is DragPosition && Equals((DragPosition)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    var hashCode = insertAtIndex;
                    hashCode = (hashCode * 397) ^ (element != null ? element.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int)dragAndDropPosition;
                    return hashCode;
                }
            }
        }

        private DragPosition m_LastDragPosition;

        private VisualElement m_DragHoverBar;

        private const int k_AutoScrollAreaSize = 5;
        private const int k_BetweenElementsAreaSize = 5;
        private const int k_PanSpeed = 20;
        private const int k_DragHoverBarHeight = 2;

        protected BaseTreeView targetListView {
            get { return m_Target as BaseTreeView; }
        }

        protected ScrollView targetScrollView {
            get { return targetListView.Q<ScrollView>(); }
        }

        public ICollectionDragAndDropController dragAndDropController { get; set; }

        public TreeViewDragger(BaseTreeView listView) : base(listView) {
            dragAndDropController = new TreeViewReorderableDragAndDropController(listView);
        }

        protected override bool CanStartDrag(Vector3 pointerPosition) {
            if(dragAndDropController == null)
                return false;

            if(!targetScrollView.contentContainer.worldBound.Contains(pointerPosition))
                return false;

			if(targetListView.selectedIndices.Count() > 1)
				return false;

			var recycledItem = GetRecycledItem(pointerPosition);
            return recycledItem != null && dragAndDropController.CanStartDrag(new[] { recycledItem });
        }

        protected internal override StartDragArgs StartDrag(Vector3 pointerPosition) {
            if(targetListView.selectedIndices.Count() > 1)
                return default;

            var recycledItem = GetRecycledItem(pointerPosition);
            if(recycledItem == null || recycledItem.CanDrag() == false)
                return default;

            return dragAndDropController.SetupDragAndDrop(new[] { recycledItem });
        }

        protected internal override DragVisualMode UpdateDrag(Vector3 pointerPosition) {
            var dragPosition = new DragPosition();
            var visualMode = GetVisualMode(pointerPosition, ref dragPosition);
            if(visualMode == DragVisualMode.Rejected)
                ClearDragAndDropUI();
            else
                ApplyDragAndDropUI(dragPosition);

            return visualMode;
        }

        private DragVisualMode GetVisualMode(Vector3 pointerPosition, ref DragPosition dragPosition) {
            if(dragAndDropController == null)
                return DragVisualMode.Rejected;

            HandleDragAndScroll(pointerPosition);
            if(!TryGetDragPosition(pointerPosition, ref dragPosition))
                return DragVisualMode.Rejected;

            var args = MakeDragAndDropArgs(dragPosition);
            return dragAndDropController.HandleDragAndDrop(args);
        }

        protected internal override void OnDrop(Vector3 pointerPosition) {
            var dragPosition = new DragPosition();
            if(!TryGetDragPosition(pointerPosition, ref dragPosition))
                return;

            var args = MakeDragAndDropArgs(dragPosition);
            if(dragAndDropController.HandleDragAndDrop(args) != DragVisualMode.Rejected)
                dragAndDropController.OnDrop(args);
        }

        // Internal for tests.
        internal void HandleDragAndScroll(Vector2 pointerPosition) {
            var scrollUp = pointerPosition.y < targetScrollView.worldBound.yMin + k_AutoScrollAreaSize;
            var scrollDown = pointerPosition.y > targetScrollView.worldBound.yMax - k_AutoScrollAreaSize;
            if(scrollUp || scrollDown) {
                var offset = targetScrollView.scrollOffset + (scrollUp ? Vector2.down : Vector2.up) * k_PanSpeed;
                offset.y = Mathf.Clamp(offset.y, 0f, Mathf.Max(0, targetScrollView.contentContainer.worldBound.height - targetScrollView.contentViewport.worldBound.height));
                targetScrollView.scrollOffset = offset;
            }
        }

        protected void ApplyDragAndDropUI(DragPosition dragPosition) {
            if(m_LastDragPosition.Equals(dragPosition))
                return;

            if(m_DragHoverBar == null) {
                m_DragHoverBar = new VisualElement();
                m_DragHoverBar.AddToClassList(BaseVerticalCollectionView.dragHoverBarUssClassName);
                m_DragHoverBar.style.width = targetListView.localBound.width;
                m_DragHoverBar.style.visibility = Visibility.Hidden;
                m_DragHoverBar.pickingMode = PickingMode.Ignore;

                targetListView.RegisterCallback<GeometryChangedEvent>(e => {
                    m_DragHoverBar.style.width = targetListView.localBound.width;
                });
                targetScrollView.contentViewport.Add(m_DragHoverBar);
            }

            ClearDragAndDropUI();
            m_LastDragPosition = dragPosition;
            switch(dragPosition.dragAndDropPosition) {
                case DragAndDropPosition.OverItem:
                    dragPosition.rootContent.AddToClassList(BaseVerticalCollectionView.itemDragHoverUssClassName);
                    break;
                case DragAndDropPosition.BetweenItems:
                    if(dragPosition.insertAtIndex == 0)
                        PlaceHoverBarAt(0);
                    else {
                        var item = targetListView.GetRootElementForIndex(dragPosition.insertAtIndex - 1);
                        item ??= targetListView.GetRootElementForIndex(dragPosition.insertAtIndex);
                        PlaceHoverBarAtElement(item);
                    }

                    break;
                case DragAndDropPosition.OutsideItems:
                    var recycledItem = targetListView.GetRootElementForIndex(targetListView.itemsSource.Count - 1);
                    if(recycledItem != null)
                        PlaceHoverBarAtElement(recycledItem);
                    //else if(targetListView.sourceIncludesArraySize && targetListView.itemsSource.Count > 0)
                    //    PlaceHoverBarAtElement(targetListView.GetRecycledItemFromIndex(0).rootElement);
                    else
                        PlaceHoverBarAt(0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dragPosition.dragAndDropPosition),
                        dragPosition.dragAndDropPosition,
                        $"Unsupported {nameof(dragPosition.dragAndDropPosition)} value");
            }
        }

        protected virtual bool TryGetDragPosition(Vector2 pointerPosition, ref DragPosition dragPosition) {
            var itemDragable = GetRecycledItem(pointerPosition);
            var dragableElement = itemDragable?.element;
            if(itemDragable != null && dragableElement != null) {
                // Skip array size item
                //if(targetListView.sourceIncludesArraySize && recycledItem.index == 0) {
                //    dragPosition.insertAtIndex = recycledItem.index + 1;
                //    dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
                //    return true;
                //}
                if(itemDragable.CanDragInsideParent() == false) {
                    return false;
				}

                //Below an item
                if(dragableElement.worldBound.yMax - pointerPosition.y < k_BetweenElementsAreaSize) {
                    dragPosition.insertAtIndex = itemDragable.index + 1;
                    dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
                    return true;
                }

                //Upon an item
                if(pointerPosition.y - dragableElement.worldBound.yMin > k_BetweenElementsAreaSize && itemDragable.CanHaveChilds()) {
                    var scrollOffset = targetScrollView.scrollOffset;
                    targetScrollView.ScrollTo(dragableElement);
                    if(scrollOffset != targetScrollView.scrollOffset) {
                        return TryGetDragPosition(pointerPosition, ref dragPosition);
                    }

                    dragPosition.element = itemDragable;
                    dragPosition.insertAtIndex = itemDragable.index;
                    dragPosition.dragAndDropPosition = DragAndDropPosition.OverItem;
                    return true;
                }

                dragPosition.insertAtIndex = itemDragable.index;
                dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
                return true;
            }

            if(!targetListView.worldBound.Contains(pointerPosition))
                return false;

            dragPosition.dragAndDropPosition = DragAndDropPosition.OutsideItems;
            if(pointerPosition.y >= targetScrollView.contentContainer.worldBound.yMax)
                dragPosition.insertAtIndex = targetListView.itemsSource.Count;
            else
                dragPosition.insertAtIndex = 0;

            return true;
        }

        private ListDragAndDropArgs MakeDragAndDropArgs(DragPosition dragPosition) {
            object target = null;
            var recycledItem = dragPosition.element;
            if(recycledItem != null)
                target = targetListView.viewController.GetItemForIndex(recycledItem.index);

            return new ListDragAndDropArgs {
                target = target,
                insertAtIndex = dragPosition.insertAtIndex,
                dragAndDropPosition = dragPosition.dragAndDropPosition,
                dragAndDropData = DragAndDropUtility.dragAndDrop.data,
            };
        }

        private void PlaceHoverBarAtElement(VisualElement element) {
            var contentViewport = targetScrollView.contentViewport;
            var elementBounds = contentViewport.WorldToLocal(element.worldBound);
            PlaceHoverBarAt(Mathf.Min(elementBounds.yMax, contentViewport.localBound.yMax - k_DragHoverBarHeight));
        }

        private void PlaceHoverBarAt(float top) {
            m_DragHoverBar.style.top = top;
            m_DragHoverBar.style.visibility = Visibility.Visible;
        }

        protected override void ClearDragAndDropUI() {
            m_LastDragPosition = new DragPosition();
            foreach(var item in targetListView.Query(classes: BaseTreeView.itemContentContainerUssClassName).Build()) {
                item.RemoveFromClassList(BaseVerticalCollectionView.itemDragHoverUssClassName);
            }

            if(m_DragHoverBar != null)
                m_DragHoverBar.style.visibility = Visibility.Hidden;
        }

        protected ITreeViewItemElement GetRecycledItem(Vector3 pointerPosition) {
            foreach(var element in targetListView.Query(classes: BaseTreeView.itemUssClassName).Build()) {
                if(element.worldBound.Contains(pointerPosition))
                    return element.Q(classes: BaseTreeView.itemContentContainerUssClassName)[0] as ITreeViewItemElement;
            }

            return null;
        }
    }
}