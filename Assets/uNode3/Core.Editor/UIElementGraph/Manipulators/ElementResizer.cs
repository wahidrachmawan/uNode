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

namespace MaxyGames.UNode.Editors {
	class ResizableElementFactory : UxmlFactory<ResizableElement> { }

	public class ResizableElement : VisualElement {
		public ResizableElement() : this("uxml/Resizable") {
			pickingMode = PickingMode.Ignore;
		}

		public ResizableElement(string uiFile) {
			var tpl = Resources.Load<VisualTreeAsset>(uiFile);
			var sheet = Resources.Load<StyleSheet>("uNodeStyles/Resizable");
			styleSheets.Add(sheet);

			tpl.CloneTree(this);

			foreach(Resizer value in System.Enum.GetValues(typeof(Resizer))) {
				VisualElement resizer = this.Q(value.ToString().ToLower() + "-resize");
				if(resizer != null)
					resizer.AddManipulator(new ElementResizer(this, value));
				m_Resizers[value] = resizer;
			}

			foreach(Resizer vertical in new[] { Resizer.Top, Resizer.Bottom })
				foreach(Resizer horizontal in new[] { Resizer.Left, Resizer.Right }) {
					VisualElement resizer = this.Q(vertical.ToString().ToLower() + "-" + horizontal.ToString().ToLower() + "-resize");
					if(resizer != null)
						resizer.AddManipulator(new ElementResizer(this, vertical | horizontal));
					m_Resizers[vertical | horizontal] = resizer;
				}
		}

		public void SetResizerWidth(float width) {
			foreach(var child in Children()) {
				if(!child.name.StartsWith("#middle")) {
					child.style.width = width;
				}
				foreach(var c in child.Children()) {
					if(!c.name.StartsWith(child.name)) {
						c.style.height = width;
					}
				}
			}
		}

		public enum Resizer {
			Top = 1 << 0,
			Bottom = 1 << 1,
			Left = 1 << 2,
			Right = 1 << 3,
		}

		Dictionary<Resizer, VisualElement> m_Resizers = new Dictionary<Resizer, VisualElement>();
	}

	public class ElementResizer : Manipulator {
		public readonly ResizableElement.Resizer direction;

		public readonly VisualElement resizedElement;

		public ElementResizer(VisualElement resizedElement, ResizableElement.Resizer direction) {
			this.direction = direction;
			this.resizedElement = resizedElement;
		}

		protected override void RegisterCallbacksOnTarget() {
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
			target.RegisterCallback<PointerDownEvent>(OnMouseDown);
			target.RegisterCallback<PointerUpEvent>(OnMouseUp);
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
			target.UnregisterCallback<PointerDownEvent>(OnMouseDown);
			target.UnregisterCallback<PointerUpEvent>(OnMouseUp);
		}

		Vector2 m_StartMouse;
		Vector2 m_StartSize;

		Vector2 m_MinSize;
		Vector2 m_MaxSize;

		Vector2 m_StartPosition;

		bool m_DragStarted = false;

		void OnMouseDown(MouseDownEvent e) {
			if(e.button == 0 && e.clickCount == 1) {
				VisualElement resizedTarget = resizedElement.parent;
				if(resizedTarget != null) {
					VisualElement resizedBase = resizedTarget.parent;
					if(resizedBase != null) {
						target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
						e.StopPropagation();
						target.CaptureMouse();
						m_StartMouse = resizedBase.WorldToLocal(e.mousePosition);
						m_StartSize = new Vector2(resizedTarget.resolvedStyle.width, resizedTarget.resolvedStyle.height);
						m_StartPosition = new Vector2(resizedTarget.resolvedStyle.left, resizedTarget.resolvedStyle.top);

						bool minWidthDefined = resizedTarget.resolvedStyle.minWidth != StyleKeyword.Auto;
						bool maxWidthDefined = resizedTarget.resolvedStyle.maxWidth != StyleKeyword.None;
						bool minHeightDefined = resizedTarget.resolvedStyle.minHeight != StyleKeyword.Auto;
						bool maxHeightDefined = resizedTarget.resolvedStyle.maxHeight != StyleKeyword.None;
						m_MinSize = new Vector2(
							minWidthDefined ? resizedTarget.resolvedStyle.minWidth.value : Mathf.NegativeInfinity,
							minHeightDefined ? resizedTarget.resolvedStyle.minHeight.value : Mathf.NegativeInfinity);
						m_MaxSize = new Vector2(
							maxWidthDefined ? resizedTarget.resolvedStyle.maxWidth.value : Mathf.Infinity,
							maxHeightDefined ? resizedTarget.resolvedStyle.maxHeight.value : Mathf.Infinity);

						m_DragStarted = false;
					}
				}
			}
		}

		void OnMouseMove(MouseMoveEvent e) {
			VisualElement resizedTarget = resizedElement.parent;
			VisualElement resizedBase = resizedTarget.parent;
			Vector2 mousePos = resizedBase.WorldToLocal(e.mousePosition);
			if(!m_DragStarted) {
				if(resizedTarget is IElementResizable) {
					(resizedTarget as IElementResizable).OnStartResize();
				}
				m_DragStarted = true;
			}

			if((direction & ResizableElement.Resizer.Right) != 0) {
				resizedTarget.style.width = Mathf.Min(m_MaxSize.x, Mathf.Max(m_MinSize.x, m_StartSize.x + mousePos.x - m_StartMouse.x));
			} else if((direction & ResizableElement.Resizer.Left) != 0) {
				float delta = mousePos.x - m_StartMouse.x;

				if(m_StartSize.x - delta < m_MinSize.x) {
					delta = -m_MinSize.x + m_StartSize.x;
				} else if(m_StartSize.x - delta > m_MaxSize.x) {
					delta = -m_MaxSize.x + m_StartSize.x;
				}

				resizedTarget.style.left = delta + m_StartPosition.x;
				resizedTarget.style.width = -delta + m_StartSize.x;
			}
			if((direction & ResizableElement.Resizer.Bottom) != 0) {
				resizedTarget.style.height = Mathf.Min(m_MaxSize.y, Mathf.Max(m_MinSize.y, m_StartSize.y + mousePos.y - m_StartMouse.y));
			} else if((direction & ResizableElement.Resizer.Top) != 0) {
				float delta = mousePos.y - m_StartMouse.y;

				if(m_StartSize.y - delta < m_MinSize.y) {
					delta = -m_MinSize.y + m_StartSize.y;
				} else if(m_StartSize.y - delta > m_MaxSize.y) {
					delta = -m_MaxSize.y + m_StartSize.y;
				}
				resizedTarget.style.top = delta + m_StartPosition.y;
				resizedTarget.style.height = -delta + m_StartSize.y;
			}
			e.StopPropagation();
		}

		void OnMouseUp(MouseUpEvent e) {
			if(e.button == 0) {
				VisualElement resizedTarget = resizedElement.parent;
				if(resizedTarget.style.width != m_StartSize.x || resizedTarget.style.height != m_StartSize.y) {
					if(resizedTarget is IElementResizable) {
						(resizedTarget as IElementResizable).OnResized();
					}
				}
				target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
				target.ReleaseMouse();
				e.StopPropagation();
			}
		}

		void OnMouseDown(PointerDownEvent e) {
			if(e.button == 0 && e.clickCount == 1) {
				VisualElement resizedTarget = resizedElement.parent;
				if(resizedTarget != null) {
					VisualElement resizedBase = resizedTarget.parent;
					if(resizedBase != null) {
						target.RegisterCallback<PointerMoveEvent>(OnMouseMove);
						e.StopPropagation();
						target.CaptureMouse();
						m_StartMouse = resizedBase.WorldToLocal(e.position);
						m_StartSize = new Vector2(resizedTarget.resolvedStyle.width, resizedTarget.resolvedStyle.height);
						m_StartPosition = new Vector2(resizedTarget.resolvedStyle.left, resizedTarget.resolvedStyle.top);

						bool minWidthDefined = resizedTarget.resolvedStyle.minWidth != StyleKeyword.Auto;
						bool maxWidthDefined = resizedTarget.resolvedStyle.maxWidth != StyleKeyword.None;
						bool minHeightDefined = resizedTarget.resolvedStyle.minHeight != StyleKeyword.Auto;
						bool maxHeightDefined = resizedTarget.resolvedStyle.maxHeight != StyleKeyword.None;
						m_MinSize = new Vector2(
							minWidthDefined ? resizedTarget.resolvedStyle.minWidth.value : Mathf.NegativeInfinity,
							minHeightDefined ? resizedTarget.resolvedStyle.minHeight.value : Mathf.NegativeInfinity);
						m_MaxSize = new Vector2(
							maxWidthDefined ? resizedTarget.resolvedStyle.maxWidth.value : Mathf.Infinity,
							maxHeightDefined ? resizedTarget.resolvedStyle.maxHeight.value : Mathf.Infinity);

						m_DragStarted = false;
					}
				}
			}
		}

		void OnMouseMove(PointerMoveEvent e) {
			VisualElement resizedTarget = resizedElement.parent;
			VisualElement resizedBase = resizedTarget.parent;
			Vector2 mousePos = resizedBase.WorldToLocal(e.position);
			if(!m_DragStarted) {
				if(resizedTarget is IElementResizable) {
					(resizedTarget as IElementResizable).OnStartResize();
				}
				m_DragStarted = true;
			}

			if((direction & ResizableElement.Resizer.Right) != 0) {
				resizedTarget.style.width = Mathf.Min(m_MaxSize.x, Mathf.Max(m_MinSize.x, m_StartSize.x + mousePos.x - m_StartMouse.x));
			}
			else if((direction & ResizableElement.Resizer.Left) != 0) {
				float delta = mousePos.x - m_StartMouse.x;

				if(m_StartSize.x - delta < m_MinSize.x) {
					delta = -m_MinSize.x + m_StartSize.x;
				}
				else if(m_StartSize.x - delta > m_MaxSize.x) {
					delta = -m_MaxSize.x + m_StartSize.x;
				}

				resizedTarget.style.left = delta + m_StartPosition.x;
				resizedTarget.style.width = -delta + m_StartSize.x;
			}
			if((direction & ResizableElement.Resizer.Bottom) != 0) {
				resizedTarget.style.height = Mathf.Min(m_MaxSize.y, Mathf.Max(m_MinSize.y, m_StartSize.y + mousePos.y - m_StartMouse.y));
			}
			else if((direction & ResizableElement.Resizer.Top) != 0) {
				float delta = mousePos.y - m_StartMouse.y;

				if(m_StartSize.y - delta < m_MinSize.y) {
					delta = -m_MinSize.y + m_StartSize.y;
				}
				else if(m_StartSize.y - delta > m_MaxSize.y) {
					delta = -m_MaxSize.y + m_StartSize.y;
				}
				resizedTarget.style.top = delta + m_StartPosition.y;
				resizedTarget.style.height = -delta + m_StartSize.y;
			}
			e.StopPropagation();
		}

		void OnMouseUp(PointerUpEvent e) {
			if(e.button == 0) {
				VisualElement resizedTarget = resizedElement.parent;
				if(resizedTarget.style.width != m_StartSize.x || resizedTarget.style.height != m_StartSize.y) {
					if(resizedTarget is IElementResizable) {
						(resizedTarget as IElementResizable).OnResized();
					}
				}
				target.UnregisterCallback<PointerMoveEvent>(OnMouseMove);
				target.ReleaseMouse();
				e.StopPropagation();
			}
		}
	}
}