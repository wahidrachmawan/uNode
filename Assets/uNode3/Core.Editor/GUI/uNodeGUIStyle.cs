using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.UNode.Editors {
	public static class uNodeGUIStyle {
		public static readonly GUIStyle itemStatic;
		public static readonly GUIStyle itemNormal;
		public static readonly GUIStyle itemBackground;
		public static readonly GUIStyle itemBackground2;
		public static readonly Texture2D errorIcon;
		public static readonly Texture2D warningIcon;
		public static readonly Texture2D messageIcon;

		public static readonly GUIStyle insertionStyle;

		public static readonly GUIStyle itemSelect = new GUIStyle("flow varPin in");
		public static readonly GUIStyle itemNext = new GUIStyle("AC RightArrow");

		static uNodeGUIStyle() {
			itemNormal = EditorStyles.label;
			itemNormal.richText = true;
			itemStatic = new GUIStyle(EditorStyles.label);
			itemStatic.fontStyle = FontStyle.Bold;
			itemStatic.richText = true;
			itemBackground = new GUIStyle("CN EntryBackEven");
			itemBackground2 = new GUIStyle("CN EntryBackOdd");
			errorIcon = EditorGUIUtility.FindTexture("d_console.erroricon.sml");
			warningIcon = EditorGUIUtility.FindTexture("d_console.warnicon.sml");
			messageIcon = EditorGUIUtility.FindTexture("d_console.infoicon.sml");

			insertionStyle = (GUIStyle)"PR Insertion";
			insertionStyle.overflow = new RectOffset(4, 0, 0, 0);
		}

		private static GUIStyle _headerStyle;
		public static GUIStyle headerStyle {
			get {
				if(_headerStyle == null) {
					_headerStyle = new GUIStyle(EditorStyles.label);
					_headerStyle.overflow = ((GUIStyle)"RL Header").overflow;
					_headerStyle.border = ((GUIStyle)"RL Header").border;
					_headerStyle.padding.left += 5;
					_headerStyle.margin.top += 1;
					_headerStyle.margin.bottom += 2;
					_headerStyle.overflow.top += 1;
					_headerStyle.overflow.bottom += 2;
					_headerStyle.normal.background = ((GUIStyle)"RL Header").normal.background;
					_headerStyle.active.background = ((GUIStyle)"RL Header").active.background;
				}
				return _headerStyle;
			}
		}

		public static Texture2D favoriteIconOff {
			get {
				return Resources.Load<Texture2D>("Icons/favorites-silver");
			}
		}

		public static Texture2D favoriteIconOn {
			get {
				return Resources.Load<Texture2D>("Icons/favorites");
			}
		}


		public static GUIStyle backgroundStyle {
			get {
				return "RL Background";
			}
		}

		public static GUIStyle footerStyle {
			get {
				return "RL Footer";
			}
		}

		static Texture2D _selectedTexture;
		public static Texture2D selectedTexture {
			get {
				if(_selectedTexture == null) {
					_selectedTexture = uNodeEditorUtility.MakeTexture(1, 1, new Color(0.24f, 0.49f, 0.91f));
				}
				return _selectedTexture;
			}
		}

		public static Texture2D GetVisiblilityTexture(bool visible) {
			if(visible) {
				return EditorGUIUtility.isProSkin ?
					EditorGUIUtility.FindTexture("d_animationvisibilitytoggleon") : EditorGUIUtility.FindTexture("animationvisibilitytoggleon");
			} else {
				return EditorGUIUtility.isProSkin ?
					EditorGUIUtility.FindTexture("d_animationvisibilitytoggleoff") : EditorGUIUtility.FindTexture("animationvisibilitytoggleoff");
			}
		}

		static GUIStyle m_RichLabel;
		/// <summary>
		/// Rich label style with word wrapped.
		/// </summary>
		public static GUIStyle RichLabel {
			get {
				if(m_RichLabel == null) {
					m_RichLabel = new GUIStyle(GUI.skin.label);
					m_RichLabel.richText = true;
					m_RichLabel.wordWrap = true;
				}

				return m_RichLabel;
			}
		}

		static GUIStyle m_RichLabel2;
		/// <summary>
		/// Rich label style without word wrapped.
		/// </summary>
		public static GUIStyle RichLabel2 {
			get {
				if(m_RichLabel2 == null) {
					m_RichLabel2 = new GUIStyle(GUI.skin.label);
					m_RichLabel2.richText = true;
				}

				return m_RichLabel2;
			}
		}

		static GUIStyle m_CenterRichLabel;
		/// <summary>
		/// Rich label style without word wrapped.
		/// </summary>
		public static GUIStyle CenterRichLabel {
			get {
				if(m_CenterRichLabel == null) {
					m_CenterRichLabel = new GUIStyle(GUI.skin.label);
					m_CenterRichLabel.richText = true;
					m_CenterRichLabel.alignment = TextAnchor.MiddleCenter;
				}

				return m_CenterRichLabel;
			}
		}

		public static GUIStyle LeftMiniButton {
			get {
				return ButtonRichText;
			}
		}

		static GUIStyle m_MiniButtonRichText;
		public static GUIStyle ButtonRichText {
			get {
				if(m_MiniButtonRichText == null) {
					m_MiniButtonRichText = new GUIStyle(EditorStyles.miniButton);
					m_MiniButtonRichText.richText = true;
					//m_MiniButtonRichText.wordWrap = true;
					m_MiniButtonRichText.alignment = TextAnchor.MiddleLeft;
				}

				return m_MiniButtonRichText;
			}
		}

		static GUIStyle m_FoldoutBold;
		public static GUIStyle FoldoutBold {
			get {
				if(m_FoldoutBold == null) {
					m_FoldoutBold = new GUIStyle(EditorStyles.foldout);
					m_FoldoutBold.fontStyle = FontStyle.Bold;
				}

				return m_FoldoutBold;
			}
		}

		static GUIStyle _whiteBoldLabel;
		public static GUIStyle whiteBoldLabel {
			get {
				if(_whiteBoldLabel == null) {
					_whiteBoldLabel = new GUIStyle(EditorStyles.whiteLabel);
					_whiteBoldLabel.fontStyle = FontStyle.Bold;
					_whiteBoldLabel.normal.textColor = Color.white;
					_whiteBoldLabel.active.textColor = Color.white;
				}
				return _whiteBoldLabel;
			}
		}

		static GUIStyle _whiteLabel;
		public static GUIStyle whiteLabel {
			get {
				if(_whiteLabel == null) {
					_whiteLabel = new GUIStyle(EditorStyles.label);
					_whiteLabel.normal.textColor = Color.white;
					_whiteLabel.active.textColor = Color.white;
				}
				return _whiteLabel;
			}
		}

		static GUIStyle _popup;
		public static GUIStyle popupStyle {
			get {
				if(_popup == null) {
					_popup = new GUIStyle(EditorStyles.popup);
					_popup.richText = true;
				}
				return _popup;
			}
		}

		static GUIStyle _labelStyle;
		public static GUIStyle labelStyle {
			get {
				if(_labelStyle == null) {
					_labelStyle = new GUIStyle(EditorStyles.label);
					_labelStyle.margin.bottom = 0;
					_labelStyle.margin.top = 0;
				}
				return _labelStyle;
			}
		}

		public static GUIStyle objectField {
			get {
#if UNITY_2019_4_OR_NEWER
				return "ObjectFieldButton";
#else
					return EditorStyles.objectField;
#endif
			}
		}
	}
}