using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode {
	public enum EditorThemeVisualMode {
		Dark,
		Light,
	}

	public abstract class EditorTheme : ScriptableObject {
		[SerializeField]
		private string Name;
		public EditorTextSetting textSettings = new EditorTextSetting();
		public EditorThemeTypeSettings typeSettings = new EditorThemeTypeSettings();
		public EditorThemeVisualMode visual;

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