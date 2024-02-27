using System;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public abstract class ValueControl : ImmediateModeElement {
		public readonly ControlConfig config;

		public ValueControl(ControlConfig config, bool autoLayout = false) {
			if(config != null && config.filter == null && config.type != null) {
				config.filter = new FilterAttribute(config.type);
			}
			this.config = config;
			EnableInClassList("Layout", autoLayout);
		}

		protected override void ImmediateRepaint() {

		}
	}

	[Serializable]
	public class ControlConfig {
		public UNodeView owner;
		public object value;
		public Type type;
		public FilterAttribute filter;
		public UIChangeType changeType = UIChangeType.None;
		public Action<object> onValueChanged { private get; set; }

		public UnityEngine.Object targetObject => owner.nodeObject.graphContainer as UnityEngine.Object;
		public object targetCanvas => owner.nodeObject ?? owner.nodeObject.graphContainer as object;

		public void OnValueChanged(object value) {
			uNodeEditorUtility.RegisterUndo(owner.nodeObject.GetUnityObject());
			onValueChanged?.Invoke(value);
			this.value = value;
			uNodeGUIUtility.GUIChanged(owner.nodeObject, changeType);
		}
	}
}