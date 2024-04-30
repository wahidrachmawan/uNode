using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;

namespace MaxyGames.UNode.Editors {
	public class MinimapView : GraphElement, IElementResizable {
		//public Label label;
		private Dragger m_Dragger;
		private ResizableElement m_Resizer;
		private UIElementEditorTheme.MinimapType m_MinimapType;
		private Rect m_ViewportRect;
		private Rect m_ContentRect;
		private Rect m_ContentRectLocal;
		private bool m_Anchored;
		private UGraphView m_GraphView;

		private static Vector3[] s_CachedRect = new Vector3[4];

		private int paddingTop => (int)resolvedStyle.paddingTop;
		private int paddingLeft => (int)resolvedStyle.paddingLeft;
		private int paddingRight => (int)resolvedStyle.paddingRight;
		private int paddingBottom => (int)resolvedStyle.paddingBottom;

		private UIElementEditorTheme.MinimapType minimapType {
			get {
				return m_MinimapType;
			}
			set {
				if(m_MinimapType != value) {
					m_MinimapType = value;
					if(m_MinimapType != UIElementEditorTheme.MinimapType.Floating) {
						capabilities &= ~Capabilities.Movable;
						//ResetPositionProperties();
						AddToClassList("anchored");
					} else {
						capabilities |= Capabilities.Movable;
						RemoveFromClassList("anchored");
					}
					m_Resizer.SetEnabled(true);
					Resize();
				}
			}
		}

		private UGraphView graphView {
			get {
				if(m_GraphView == null) {
					m_GraphView = GetFirstAncestorOfType<UGraphView>();
				}
				return m_GraphView;
			}
		}

		public MinimapView() {
			capabilities = Capabilities.Movable;
			m_Dragger = new Dragger {
				clampToParentEdges = true
			};
			this.AddManipulator(m_Dragger);
			//label = new Label("Minimap");
			//Add(label);
			//label.RegisterCallback<MouseDownEvent>(EatMouseDown);
			m_Resizer = new ResizableElement();
			m_Resizer.SetResizerWidth(4);
			Add(m_Resizer);
			RegisterCallback<MouseDownEvent>(OnMouseDown);
			this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
			this.AddStyleSheet("uNodeStyles/Minimap");
			minimapType = UIElementUtility.Theme.minimapType;
			var rect = UIElementUtility.Theme.minimapPosition;
			style.left = rect.x;
			style.top = rect.y;
			style.width = rect.width;
			style.height = rect.height;
			this.RegisterRepaintAction(DrawContent);
		}

		public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
			if(minimapType == UIElementEditorTheme.MinimapType.Floating) {
				evt.menu.AppendAction("Anchor", (act) => {
					minimapType = UIElementEditorTheme.MinimapType.Anchored;
					UIElementUtility.Theme.minimapType = minimapType;
					EditorUtility.SetDirty(UIElementUtility.Theme);
					evt.StopPropagation();
				}, DropdownMenuAction.AlwaysEnabled);
			} else {
				evt.menu.AppendAction("Make floating", (act) => {
					minimapType = UIElementEditorTheme.MinimapType.Floating;
					UIElementUtility.Theme.minimapType = minimapType;
					EditorUtility.SetDirty(UIElementUtility.Theme);
					evt.StopPropagation();
				}, DropdownMenuAction.AlwaysEnabled);
			}
			evt.menu.AppendSeparator();
			if(minimapType != UIElementEditorTheme.MinimapType.UpperLeft) {
				evt.menu.AppendAction("Snap to/Upper Left", (act) => {
					minimapType = UIElementEditorTheme.MinimapType.UpperLeft;
					UIElementUtility.Theme.minimapType = minimapType;
					EditorUtility.SetDirty(UIElementUtility.Theme);
					evt.StopPropagation();
				}, DropdownMenuAction.AlwaysEnabled);
			}
			if(minimapType != UIElementEditorTheme.MinimapType.UpperRight) {
				evt.menu.AppendAction("Snap to/Upper Right", (act) => {
					minimapType = UIElementEditorTheme.MinimapType.UpperRight;
					UIElementUtility.Theme.minimapType = minimapType;
					EditorUtility.SetDirty(UIElementUtility.Theme);
					evt.StopPropagation();
				}, DropdownMenuAction.AlwaysEnabled);
			}
			if(minimapType != UIElementEditorTheme.MinimapType.BottomLeft) {
				evt.menu.AppendAction("Snap to/Bottom Left", (act) => {
					minimapType = UIElementEditorTheme.MinimapType.BottomLeft;
					UIElementUtility.Theme.minimapType = minimapType;
					EditorUtility.SetDirty(UIElementUtility.Theme);
					evt.StopPropagation();
				}, DropdownMenuAction.AlwaysEnabled);
			}
			if(minimapType != UIElementEditorTheme.MinimapType.BottomRight) {
				evt.menu.AppendAction("Snap to/Bottom Right", (act) => {
					minimapType = UIElementEditorTheme.MinimapType.BottomRight;
					UIElementUtility.Theme.minimapType = minimapType;
					EditorUtility.SetDirty(UIElementUtility.Theme);
					evt.StopPropagation();
				}, DropdownMenuAction.AlwaysEnabled);
			}
			evt.menu.AppendSeparator("");
			evt.menu.AppendAction("Hide minimap", (act) => {
				UIElementUtility.Theme.enableMinimap = false;
				uNodeEditorUtility.MarkDirty(UIElementUtility.Theme);
				RemoveFromHierarchy();
			}, DropdownMenuAction.AlwaysEnabled);
		}

		public void OnResized() {
			Resize();
			if(m_MinimapType == UIElementEditorTheme.MinimapType.Floating || m_MinimapType == UIElementEditorTheme.MinimapType.Anchored) {
				UIElementUtility.Theme.minimapPosition = new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
			}
			EditorUtility.SetDirty(UIElementUtility.Theme);
		}

		private void Resize() {
			//style.left = layout.x;
			//style.top = layout.y;
			//style.width = layout.width;
			//style.height = layout.height;

			//if(parent != null) {
			//	if(resolvedStyle.left + resolvedStyle.width > parent.layout.x + parent.layout.width) {
			//		Rect layout = this.layout;
			//		layout.x -= resolvedStyle.left + resolvedStyle.width - (parent.layout.x + parent.layout.width);
			//		this.SetLayout(layout);
			//	}
			//	if(resolvedStyle.top + resolvedStyle.height > parent.layout.y + parent.layout.height) {
			//		Rect layout2 = layout;
			//		layout2.y -= resolvedStyle.top + resolvedStyle.height - (parent.layout.y + parent.layout.height);
			//		this.SetLayout(layout2);
			//	}
			//	Rect layout3 = this.layout;
			//	layout3.width = resolvedStyle.width;
			//	layout3.height = resolvedStyle.height;
			//	layout3.x = Mathf.Max(parent.layout.x, layout3.x);
			//	layout3.y = Mathf.Max(parent.layout.y, layout3.y);
			//	this.SetLayout(layout3);
			//}
		}

		private static void ChangeToMiniMapCoords(ref Rect rect, float factor, Vector3 translation) {
			rect.width *= factor;
			rect.height *= factor;
			rect.x *= factor;
			rect.y *= factor;
			rect.x += translation.x;
			rect.y += translation.y;
		}

		private void CalculateRects(VisualElement container) {
			m_ContentRect = graphView.CalculateRectToFitAll(container);
			m_ContentRectLocal = m_ContentRect;
			Matrix4x4 inverse = container.worldTransform.inverse;
			Vector4 column = inverse.GetColumn(3);
			Vector2 vector = new Vector2(inverse.m00, inverse.m11);
			m_ViewportRect = new Rect(0, 0, parent.layout.width, parent.layout.height);
			m_ViewportRect.x += column.x;
			m_ViewportRect.y += column.y;
			m_ViewportRect.x += parent.worldBound.x * vector.x;
			m_ViewportRect.y += parent.worldBound.y * vector.y;
			m_ViewportRect.width *= vector.x;
			m_ViewportRect.height *= vector.y;
			Matrix4x4 worldTransform = container.worldTransform;
			float m = worldTransform.m00;
			Rect rect = RectUtils.Encompass(m_ContentRect, m_ViewportRect);
			float factor = (layout.width - paddingLeft - paddingRight) / rect.width;
			ChangeToMiniMapCoords(ref rect, factor, Vector3.zero);
			Vector3 translation = new Vector3(paddingLeft - rect.x, paddingTop - rect.y);
			ChangeToMiniMapCoords(ref m_ViewportRect, factor, translation);
			ChangeToMiniMapCoords(ref m_ContentRect, factor, translation);
			if(rect.height > layout.height - paddingTop - paddingBottom) {
				float num = (layout.height - paddingTop - paddingBottom) / rect.height;
				float num2 = (layout.width - paddingLeft - paddingRight - rect.width * num) / 2f;
				float num3 = paddingTop - (rect.y + translation.y) * num;
				m_ContentRect.width *= num;
				m_ContentRect.height *= num;
				m_ContentRect.x *= num;
				m_ContentRect.y *= num;
				m_ContentRect.x += num2;
				m_ContentRect.y += num3;
				m_ViewportRect.width *= num;
				m_ViewportRect.height *= num;
				m_ViewportRect.x *= num;
				m_ViewportRect.y *= num;
				m_ViewportRect.x += num2;
				m_ViewportRect.y += num3;
			}
		}

		private Rect CalculateElementRect(UNodeView elem) {
			Rect result = elem.ChangeCoordinatesTo(graphView.contentViewContainer, new Rect(0, 0, elem.layout.width, elem.layout.height));
			if(elem.isHidden) {
				result = new Rect(elem.hidingRect.x, elem.hidingRect.y, elem.hidingRect.width, elem.hidingRect.height);
			}
			result.x = m_ContentRect.x + (result.x - m_ContentRectLocal.x) * m_ContentRect.width / m_ContentRectLocal.width;
			result.y = m_ContentRect.y + (result.y - m_ContentRectLocal.y) * m_ContentRect.height / m_ContentRectLocal.height;
			result.width *= m_ContentRect.width / m_ContentRectLocal.width;
			result.height *= m_ContentRect.height / m_ContentRectLocal.height;
			int num = 2;
			int num2 = 0;
			float num3 = layout.width - 2f;
			float num4 = layout.height - 2f;
			if(result.x < num) {
				if(result.x < num - result.width) {
					return new Rect(0f, 0f, 0f, 0f);
				}
				result.width -= num - result.x;
				result.x = num;
			}
			if(result.x + result.width >= num3) {
				if(result.x >= num3) {
					return new Rect(0f, 0f, 0f, 0f);
				}
				result.width -= result.x + result.width - num3;
			}
			if(result.y < (num2 + paddingTop)) {
				if(result.y < (num2 + paddingTop) - result.height) {
					return new Rect(0f, 0f, 0f, 0f);
				}
				result.height -= (num2 + paddingTop) - result.y;
				result.y = num2 + paddingTop;
			}
			if(result.y + result.height >= num4) {
				if(result.y >= num4) {
					return new Rect(0f, 0f, 0f, 0f);
				}
				result.height -= result.y + result.height - num4;
			}
			return result;
		}

		private void DrawContent() {
			var graphView = this.graphView;
			VisualElement contentViewContainer = graphView.contentViewContainer;
			UnityEngine.Profiling.Profiler.BeginSample("Draw minimap content");
			CalculateRects(contentViewContainer);
			Color color = Handles.color;
			var editorData = UIElementUtility.Theme;
			foreach(var elem in graphView.nodeViews) {
				if(elem == null || elem.isBlock)
					continue;
				var c = elem.elementTypeColor;
				c.a = editorData.minimapOpacity;
				Rect rect = CalculateElementRect(elem);
				Handles.color = c;
				s_CachedRect[0].Set(rect.xMin, rect.yMin, 0f);
				s_CachedRect[1].Set(rect.xMax, rect.yMin, 0f);
				s_CachedRect[2].Set(rect.xMax, rect.yMax, 0f);
				s_CachedRect[3].Set(rect.xMin, rect.yMax, 0f);
				Handles.DrawSolidRectangleWithOutline(s_CachedRect, c, c);
				if(elem.selected) {
					DrawRectangleOutline(rect, editorData.minimapSelectedNodeColor);
				}
			}
			DrawRectangleOutline(m_ViewportRect, editorData.minimapViewportColor);
			Handles.color = color;
			{//Layout
				switch(minimapType) {
					case UIElementEditorTheme.MinimapType.BottomLeft:
						style.left = 0;
						style.top = parent.layout.height - layout.height;
						break;
					case UIElementEditorTheme.MinimapType.BottomRight:
						style.left = parent.layout.width - layout.width;
						style.top = parent.layout.height - layout.height;
						break;
					case UIElementEditorTheme.MinimapType.UpperLeft:
						style.left = 0;
						style.top = 0;
						break;
					case UIElementEditorTheme.MinimapType.UpperRight:
						style.left = parent.layout.width - layout.width;
						style.top = 0;
						break;
				}
				if(parent.layout.width != 0 && layout.x + layout.width > parent.layout.width) {
					style.left = parent.layout.width - layout.width;
				}
				if(parent.layout.height != 0 && layout.y + layout.height > parent.layout.height) {
					style.top = parent.layout.height - layout.height;
				}
				if(layout.x < 0) {
					style.left = 0;
				}
				if(layout.y < 0) {
					style.top = 0;
				}
				if(m_MinimapType == UIElementEditorTheme.MinimapType.Floating || m_MinimapType == UIElementEditorTheme.MinimapType.Anchored) {
					if(UIElementUtility.Theme.minimapPosition != layout) {
						UIElementUtility.Theme.minimapPosition = layout;
						EditorUtility.SetDirty(UIElementUtility.Theme);
					}
				}
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		private void DrawRectangleOutline(Rect rect, Color color) {
			Color color2 = Handles.color;
			Handles.color = color;
			Handles.DrawPolyLine(new Vector3(rect.x, rect.y, 0f), new Vector3(rect.x + rect.width, rect.y, 0f), new Vector3(rect.x + rect.width, rect.y + rect.height, 0f), new Vector3(rect.x, rect.y + rect.height, 0f), new Vector3(rect.x, rect.y, 0f));
			Handles.color = color2;
		}

		private void EatMouseDown(MouseDownEvent e) {
			if(e.button == 0 && (capabilities & Capabilities.Movable) == 0) {
				e.StopPropagation();
			}
		}

		private void OnMouseDown(MouseDownEvent e) {
			if(e.button != 0) return;
			GraphView gView = graphView;
			CalculateRects(gView.contentViewContainer);
			Vector2 mousePosition = e.localMousePosition;

			UNodeView closestNode = null;
			foreach(var elem in graphView.nodeViews) {
				if(elem == null || elem.isBlock || elem.IsSelectable() == false)
					continue;
				if(CalculateElementRect(elem).Contains(mousePosition)) {
					closestNode = elem;
					break;
				}
			}

			if(closestNode == null) {
				float closestDistance = int.MaxValue;
				foreach(var elem in graphView.nodeViews) {
					if(elem == null || elem.isBlock || elem.IsSelectable() == false)
						continue;
					var distance = GetDistanceToRect(CalculateElementRect(elem), mousePosition);
					if(distance < closestDistance) {
						closestDistance = distance;
						closestNode = elem;
					}
				}
				if(closestDistance > 5) {
					closestNode = null;
				}
			}
			if(closestNode != null) {
				gView.ClearSelection();
				gView.AddToSelection(closestNode);
				gView.FrameSelection();
				e.StopPropagation();
			}

			EatMouseDown(e);
		}

		private float GetDistanceToRect(Rect rect, Vector2 point) {
			Vector2 halfSize = rect.size / 2f;
			point -= rect.center;
			Vector2 cwed = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - halfSize;
			float outsideDistance = new Vector2(Mathf.Max(cwed.x, 0f), Mathf.Max(cwed.y, 0f)).magnitude;
			float insideDistance = Mathf.Min(0f, Mathf.Max(cwed.x, cwed.y));
			return outsideDistance + insideDistance;
		}

		public void OnStartResize() {

		}
	}
}