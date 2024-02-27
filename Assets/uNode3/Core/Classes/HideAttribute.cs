using System;

namespace MaxyGames.UNode {
	/// <summary>
	/// Make variable hide in inspector.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public sealed class HideAttribute : UnityEngine.PropertyAttribute {
		/// <summary>
		/// The target field name
		/// </summary>
		public string targetField;
		/// <summary>
		/// The value for hide
		/// </summary>
		public object hideValue;

		public bool hideOnSame;
		/// <summary>
		/// Will auto set the target object to default value when hide.
		/// </summary>
		public bool defaultOnHide;
		/// <summary>
		/// The default value to set when hide
		/// </summary>
		public object defaultValue;
		/// <summary>
		/// Are hide for element type
		/// </summary>
		public bool elementType;

		public HideAttribute() {

		}

		public HideAttribute(string targetField, object hideValue, bool hideOnSame = true) {
			this.targetField = targetField;
			this.hideValue = hideValue;
			this.hideOnSame = hideOnSame;
			this.defaultOnHide = true;
			this.elementType = false;
		}

		public HideAttribute(string targetField) {
			this.hideOnSame = true;
			this.targetField = targetField;
			this.defaultOnHide = true;
			this.elementType = false;
		}
	}
}