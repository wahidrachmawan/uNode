using System;
using UnityEngine;

namespace MaxyGames.UNode {
	public abstract class EditorTheme : ScriptableObject {
		[SerializeField]
		private string Name;
		public EditorTextSetting textSettings = new EditorTextSetting();

		[HideInInspector]
		public bool expanded = false;

		public abstract System.Type GetGraphType();

		public string ThemeName {
			get {
				if(string.IsNullOrEmpty(Name)) {
					return name;
				}
				return Name;
			}
		}
	}
}