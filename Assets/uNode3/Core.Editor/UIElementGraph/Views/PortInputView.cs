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
	public class PortInputView : VisualElement {
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
		public VisualElement container { get; private set; }
		VisualElement m_Dot;
		EdgeControl m_EdgeControl;

		public PortInputView(ValueInputData data) {
			this.AddStyleSheet("uNodeStyles/NativePortStyle");
			this.AddStyleSheet(UIElementUtility.Theme.portStyle);
			pickingMode = PickingMode.Ignore;
			ClearClassList();
			this.data = data;
			this.ScheduleAction(DoUpdate, 500);
			RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
		}

		void DoUpdate() {
			var color = edgeColor;
			if(data.port.UseDefaultValue) {
				if(container == null) {
					InitializeControl();
				} else if(container.parent == null) {
					Add(m_EdgeControl);
					Add(container);
				}
				m_EdgeControl.inputColor = color;
				m_EdgeControl.outputColor = color;
				if(m_Dot != null) {
					m_Dot.style.backgroundColor = color;
				}
				if(UIElementUtility.Theme.coloredPortBorder) {
					container.style.SetBorderColor(color);
				}
			} else if(container?.parent != null) {
				container.RemoveFromHierarchy();
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

			container = new VisualElement { name = "container" };
			{
				if(this.data != null) {
					m_Control = this.data.InstantiateControl();
					if(m_Control != null) {
						m_Control.AddToClassList("port-control");
						container.Add(m_Control);
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
				container.Add(slotContainer);
			}
			Add(container);
		}

		private void OnCustomStyleResolved(CustomStyleResolvedEvent evt) {
			if(container != null && container.visible) {
				m_EdgeControl.UpdateLayout();
			}
		}

		public float GetPortWidth() {
			if(container == null || container.parent == null)
				return 0;
			return container.layout.width;
		}

		public bool IsControlVisible() {
			return container != null && container.parent != null;
		}
	}
}