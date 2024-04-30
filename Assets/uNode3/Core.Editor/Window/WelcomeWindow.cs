using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	public class WelcomeWindow : EditorWindow {
		[MenuItem("Tools/uNode/Welcome", false, 5000000)]
		public static void ShowWindow() {
			WelcomeWindow window = GetWindow<WelcomeWindow>(true);
			window.minSize = new Vector2(500, 450);
			window.maxSize = window.minSize;
			window.titleContent = new GUIContent("Welcome to uNode");
			window.Show();
		}

		public static bool IsShowOnStartup {
			get {
				return EditorPrefs.GetBool("unode_welcome", true);
			}
		}

		private Texture logoIcon;
		private Texture gettingStartedIcon;
		private Texture downloadIcon;
		private Texture aboutIcon;
		private Texture addOnsIcon;
		private Texture addOnsAddIcon;
		private Texture youtubeIcon;
		private Texture discordIcon;
		private Texture forumIcon;
		private bool showAtStartup;
		private GUIStyle labelWithWordWrap;
		private string page = "welcome";

		private void OnEnable() {
			logoIcon = Resources.Load<Texture>("uNodeVS_Logo");
			downloadIcon = Resources.Load<Texture>("Icons/download");
			gettingStartedIcon = Resources.Load<Texture>("Icons/books");
			aboutIcon = Resources.Load<Texture>("Icons/about");
			addOnsIcon = Resources.Load<Texture>("Icons/plugin");
			addOnsAddIcon = Resources.Load<Texture>("Icons/plugin_add");
			youtubeIcon = Resources.Load<Texture>("Icons/youtube");
			discordIcon = Resources.Load<Texture>("Icons/discord");
			forumIcon = Resources.Load<Texture>("Icons/comments");
			showAtStartup = EditorPrefs.GetBool("unode_welcome", true);
		}

		private void OnGUI() {
			{//Init styles
				if(labelWithWordWrap == null) {
					labelWithWordWrap = new GUIStyle(EditorStyles.label) { wordWrap = true };
				}
			}
			GUI.Box(new Rect(-1, 0, position.width + 2, 130), "");
			GUI.DrawTexture(new Rect((500 / 2) - (212 / 2), 2, 212, 120), logoIcon);
			GUILayout.Space(140);

			switch(page) {
				case "welcome":
					DrawLink(gettingStartedIcon,
						"Getting Started",
						"Links to samples, tutorials, forums etc.",
						() => {
							page = "learn";
						});
					DrawLink(addOnsIcon,
						"Add-Ons",
						"Extend uNode with these powerful add-ons.",
						() => {
							page = "addons";
						});
					DrawLink(aboutIcon,
						"About",
						"Show uNode about window.",
						() => {
							About.ShowWindow();
						});
					break;
				case "learn":
					DrawLink(downloadIcon,
						"Samples",
						"Download sample scenes and complete projects.",
						() => {
							Application.OpenURL("http://maxygames.com/docs/unode-tutorial/examples/");
						});
					DrawLink(youtubeIcon,
						"Tutorials",
						"Watch tutorials on the uNode YouTube channel.",
						() => {
							Application.OpenURL("https://www.youtube.com/channel/UCDZo-bRai7LUgJZysBnQeQQ/featured?view_as=subscriber");
						});
					DrawLink(gettingStartedIcon,
						"Docs",
						"Browse the online manual.",
						() => {
							Application.OpenURL("http://maxygames.com/docs/");
						});
					DrawLink(discordIcon,
						"Discord Server",
						"Join the uNode discord server and talk with others.",
						() => {
							Application.OpenURL("https://discord.gg/8ufevvN");
						});
					DrawLink(forumIcon,
						"Forums",
						"Join the uNode community!",
						() => {
							Application.OpenURL("https://forum.unity.com/threads/released-unode-visual-scripting-support-c-import-export.500676/");
						});
					break;
				case "addons":
					DrawLink(addOnsAddIcon,
						"C# Parser",
						"Convert c# script into uNode.",
						() => {
							uNodeEditorUtility.DisplayMessage("", "C# Parser by default is included in uNode Pro version.");
							//AssetDatabase.ImportPackage(AssetDatabase.GUIDToAssetPath("a823b7cd525c21a46b2d5ba4f6476bc1"), true);
						});
					DrawLink(addOnsAddIcon,
						"DOTS Support",
						"uNode support for DOTS Project ( Unity ECS ).",
						() => {
							Application.OpenURL("https://github.com/wahidrachmawan/uNode3-DOTS");
						});
					DrawLink(addOnsAddIcon,
						"Unity Visual Scripting Converter",
						"Convert Unity Visual Scripting flow graph into uNode graph.",
						() => {
							uNodeEditorUtility.DisplayMessage("", "UVS Converter is still in WIP.");
							//AssetDatabase.ImportPackage(AssetDatabase.GUIDToAssetPath("1d7300bd2a8544746bfce671a0be2be3"), true);
						});
					break;
			}

			{//Bottom
				GUILayout.FlexibleSpace();
				GUILayout.BeginHorizontal();
				if(page != "welcome") {
					if(GUILayout.Button("Back")) {
						page = "welcome";
					}
				}
				GUILayout.FlexibleSpace();
				bool showWelcomeScreen = GUILayout.Toggle(showAtStartup, "Show At Startup");
				if(showWelcomeScreen != showAtStartup) {
					showAtStartup = showWelcomeScreen;
					EditorPrefs.SetBool("unode_welcome", showAtStartup);
				}
				GUILayout.Space(10);
				GUILayout.EndHorizontal();
			}
		}

		private void DrawLink(Texture texture, string heading, string body, System.Action action) {
			GUILayout.BeginHorizontal();

			GUILayout.Space(60);
			GUILayout.Box(texture, GUIStyle.none, GUILayout.MaxWidth(32), GUILayout.MaxHeight(32));
			GUILayout.Space(5);

			GUILayout.BeginVertical();
			GUILayout.Space(1);
			GUILayout.Label(heading, EditorStyles.boldLabel);
			GUILayout.Label(body, labelWithWordWrap);
			GUILayout.EndVertical();

			GUILayout.EndHorizontal();

			var rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

			if(Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
				action();
				Repaint();
				GUIUtility.ExitGUI();
			}

			GUILayout.Space(10);
		}
	}
}