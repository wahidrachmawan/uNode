using System;
using UnityEngine;

namespace MaxyGames.UNode {
	/// <summary>
	/// Attribute for show fields in node window.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class FieldDrawerAttribute : Attribute {
		public GUIContent label = GUIContent.none;

		public FieldDrawerAttribute() { }

		public FieldDrawerAttribute(string label, string tooltip = null) {
			this.label = new GUIContent(label, tooltip);
		}
	}
}