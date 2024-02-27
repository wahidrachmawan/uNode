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
	public class PortInputView : GraphElement {
		public Color edgeColor {
			get {
				if(data != null && data.portView != null) {
					return data.portView.portColor;
				}
				return Color.white;
			}
		}

		public ValueInputData data { get; private set; }

		VisualElement m_Control;
		VisualElement m_Container;
		VisualElement m_Dot;
		EdgeControl m_EdgeControl;

		public PortInputView(ValueInputData data) {
			this.AddStyleSheet("uNodeStyles/NativePortStyle");
			this.AddStyleSheet(UIElementUtility.Theme.portStyle);
			pickingMode = PickingMode.Ignore;
			ClearClassList();
			this.data = data;
			this.ScheduleAction(DoUpdate, 500);
			data?.owner?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		private void OnGeometryChanged(GeometryChangedEvent evt) {
			// Check if dimension has changed
			if(evt.oldRect.size == evt.newRect.size)
				return;
			UpdatePosition();
		}

		public void UpdatePosition() {
			if(data == null || parent == null)
				return;
			this.SetPosition(new Vector2(0, data.portView.ChangeCoordinatesTo(parent, Vector2.zero).y));
		}

		private bool hasUpdatePosition = false;

		void DoUpdate() {
			//if(data.owner.IsFaded())
			//	return;
			if(!hasUpdatePosition) {
				hasUpdatePosition = true;
				UpdatePosition();
			}
			var color = edgeColor;
			if(data.port.UseDefaultValue) {
				if(m_Container == null) {
					InitializeControl();
				} else if(m_Container.parent == null) {
					Add(m_EdgeControl);
					Add(m_Container);
				}
				m_EdgeControl.inputColor = color;
				m_EdgeControl.outputColor = color;
				m_Dot.style.backgroundColor = color;
				if(UIElementUtility.Theme.coloredPortBorder) {
					m_Container.style.SetBorderColor(color);
				}
			} else if(m_Container?.parent != null) {
				m_Container.RemoveFromHierarchy();
				m_EdgeControl.RemoveFromHierarchy();
			}
		}

		private void InitializeControl() {
			m_EdgeControl = new EdgeControl {
				from = new Vector2(412f - 23f, 11.5f),
				to = new Vector2(412f, 11.5f),
				edgeWidth = 2,
				pickingMode = PickingMode.Ignore,
			};
			Add(m_EdgeControl);

			m_Container = new VisualElement { name = "container" };
			{
				if(this.data != null) {
					m_Control = this.data.InstantiateControl();
					if(m_Control != null) {
						m_Control.AddToClassList("port-control");
						m_Container.Add(m_Control);
					}
				}

				m_Dot = new VisualElement { name = "dot" };
				m_Dot.style.backgroundColor = edgeColor;
				var slotElement = new VisualElement { name = "slot" };
				{
					slotElement.Add(m_Dot);
				}
				var slotContainer = new VisualElement() { name = "slotContainer" };
				{
					slotContainer.Add(slotElement);
				}
				m_Container.Add(slotContainer);
			}
			Add(m_Container);
		}

		protected override void OnCustomStyleResolved(ICustomStyle style) {
			base.OnCustomStyleResolved(style);
			if(m_Container != null && m_Container.visible) {
				m_EdgeControl.UpdateLayout();
			}
		}

		public float GetPortWidth() {
			if(m_Container == null || m_Container.parent == null)
				return 0;
			return m_Container.layout.width;
		}

		public bool IsControlVisible() {
			return m_Container != null && m_Container.parent != null;
		}
	}
}