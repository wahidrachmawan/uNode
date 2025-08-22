#pragma warning disable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	public class uNodePreference {
		public enum DefaultAccessor {
			Public,
			Private,
		}
		public enum AutoConvertOption {
			Always,
			Manual,
		}

		public class PreferenceData {
			[Tooltip("The default access modifier for new variable")]
			public DefaultAccessor newVariableAccessor;
			[Tooltip("The default access modifier for new property")]
			public DefaultAccessor newPropertyAccessor;
			[Tooltip("The default access modifier for new function")]
			public DefaultAccessor newFunctionAccessor;

			//Node snapping
			[Tooltip("Enable the snapping feature")]
			public bool enableSnapping = true;
			[Tooltip("Enable snapping to graph")]
			public bool graphSnapping = true;
			[Tooltip("Enable snapping to port")]
			public bool nodePortSnapping = true;
			[Tooltip("The snapping range to a port")]
			public float portSnappingRange = 10;
			[Tooltip("Enable snapping to grid")]
			public bool gridSnapping = false;
			[Tooltip("Enable the space snapping feature")]
			public bool spacingSnapping = false;

			//Controls
			[Tooltip("When true, right click to move the canvas will be disabled")]
			public bool disableRightClickMove;
			[Tooltip("If true, when moving node the input value of the node will be always be carry")]
			public bool autoCarryInputValue;
			public bool carryNodes = false;
			//public bool isDim = true;

			public DisplayKind displayKind;

			public bool showGrid = true,
				inEditorDocumentation = true,
				inspectorIntegration = true,
				autoBackupOnSave = true,
				forceReloadGraph;

			[Tooltip("Auto convert port mode.")]
			public AutoConvertOption autoConvertPort = AutoConvertOption.Always;
			[Tooltip("Auto create reroute connection")]
			public bool autoCreateReroute = true;
			[Tooltip("Auto proxy connection")]
			public bool autoProxyConnection = true;
			[Tooltip("Auto add missing namespace when adding node to the graph.")]
			public bool autoAddNamespace;
			public float debugTransitionSpeed = 0.5f;


			[SerializeField]
			private int m_maxReloadMilis;
			public int maxReloadMilis {
				get {
					if(m_maxReloadMilis <= 0)
						m_maxReloadMilis = 50;
					return m_maxReloadMilis;
				}
				set {
					m_maxReloadMilis = value;
				}
			}

			//Backup
			public int maxGraphBackup = 100;

			//Browser
			public bool coloredItem = false;
			public Color itemTypeColor = new Color(0, 0.65f, 0);
			public Color itemKeywordColor = new Color(0.1f, 0.33f, 0.6f);
			public Color itemInterfaceColor = new Color(0.6f, 0.4f, 0.13f);
			public Color itemEnumColor = new Color(0.7f, 0.7f, 0.04f);

			public float itemSelectorWidth = 380;
			public float itemSelectorHeight = 600;
			public int maxRecentItemsToShow = 10;
			public int minDeepTypeSearch = 3;

			public bool itemSelectorShowUnselectedTypes;

			#region Type
			[Tooltip("Show obsolete item ( types, variables, properties, function, etc ) ")]
			public bool showObsoleteItem;
			[Tooltip("If true, will skip auto including items from included assemblies" +
				"\n\nFor novice users, set to `false` might help find a desired items" +
				"\n\nFor complex project or advanced user who doesn't want to see extra items this is recommended to be set to 'true'" +
				"\n\nNote: the included assemblies still used for some features like find a `operator` from Arithmatic node")]
			public bool ignoreIncludedAssemblies;
			[Tooltip("If true, will only included default namespaces on Global Using Namespaces" +
				"\n\nNote: if false the excluded namespaces will be used for remvoe unnecessary namespaces")]
			public bool filterIncludedNamespaces;
			public List<string> globalUsingNamespaces = new List<string>();

			public List<string> includedAssemblies;
			public List<string> nodeBrowserNamespaces;
			public List<string> excludedNamespaces;
			public List<string> excludedTypes;
			[Tooltip("If true, will auto hide c# type when there's native graph that generate the type." + 
				"\n\nIf false, will show all c# type even if there's Runtime Type that referencing the real c# type." +
				"\n\nNote: if false, you will see duplicated type the first is Runtime Type and second is real c# type it's the best to set this to true")]
			public bool autoHideNativeType = true;
			#endregion

			public bool enableErrorCheck = true;
			public string editorTheme;

			public string ilSpyPath;

			#region Generator Data
			public GeneratorData generatorData = new GeneratorData();

			[System.Serializable]
			public class GeneratorData {
				[Tooltip(@"The Generation mode for every graphs. 
-Default is using individual graph setting and force to be pure when the graph use default.
-Performance: is forcing to generate pure c# and get better performance but it may give errors when other graph is not compiled into script.
-Compatibility: is forcing to generate c# script that's compatible with all graph even when other graph is not compiled into script.")]
				public GenerationKind generationMode = GenerationKind.Default;
				[Tooltip("If true, will auto generates scripts for project graphs on build to get native c# performance.")]
				public bool autoGenerateOnBuild = true;
				[Tooltip("If true, will include full type name with namespace.\nUse this when you have error regarding to type ambiguous.")]
				public bool fullTypeName = false;
				[Tooltip("If true will include comment for each event, node, action, and validation.")]
				public bool fullComment = false;
				[Tooltip("If true, will convert line ending this should fix line ending warning.")]
				public bool convertLineEnding = true;
				[Tooltip("(Required Pro version)\nIf true, will optimize code for get / set variables and properties for Runtime Graph.")]
				public bool optimizeRuntimeCode = false;

				[Header("Compilation")]
				[Tooltip(
@"The Runtime Graph compilation method:
-Unity: The generated scripts will be saved in 'Assets/uNode.Generated' folder and will be compiled by Unity, the assembly result is natively loaded.

-Roslyn: The generated scripts will be saved in temporary folder and will be compiled by Roslyn Compiler, the assembly result is automatic loaded on Play.
Note: Auto Generate on Buld will always using Unity method.")]
				public CompilationMethod compilationMethod = CompilationMethod.Roslyn;
				[Tooltip("If true, will initialize Roslyn compiler after Unity recompilation process.")]
				public bool autoStartInitialization = true;
				[Tooltip("If true, uNode will try searching source generators in projects and include it for Roslyn\nUseful for DOTS project.")]
				public bool useSourceGenerators = true;

				[Header("Compilation ( Script Graphs )")]
				[Tooltip("(Required Pro version)\nCompile script after generating code and check for errors, and prevent save if there's any errors.")]
				public bool compileBeforeSave = false;

				[Header("Compilation ( Runtime Graphs )")]
				[Hide(nameof(compilationMethod), CompilationMethod.Unity, defaultValue = true)]
				[Tooltip("(Required Pro version)\nIf true, will compile graph in background.\nNote: The generated script will using Full Type Name for performance reason.")]
				public bool compileInBackground = true;
				[Hide(nameof(compilationMethod), CompilationMethod.Unity)]
				[Hide(nameof(compileInBackground), false)]
				[Tooltip("If true, will auto generates script on save graph.")]
				public bool autoGenerateOnSave;
				[Hide(nameof(compilationMethod), CompilationMethod.Unity)]
				[Tooltip("If true, will auto remove generated scripts after build for runtime graphs")]
				public bool autoRemoveGeneratedScriptAfterBuild;

				[Header("Others")]
				[Tooltip("Analyze the generated script with Roslyn")]
				public bool analyzeScript;
				[Hide(nameof(analyzeScript), false)]
				public bool formatScript = true;
				[Hide(nameof(analyzeScript), false)]
				[Tooltip("If enabled, static extension method calls will be rewritten using instance-style syntax where safe (e.g., StaticClass.Method(x) → x.Method()).")]
				public bool preferExtensionMethodSyntax = true;
				[Hide(nameof(analyzeScript), false)]
				public bool removeUnnecessaryCode = true;

				public bool IsAutoGenerateOnSave => compilationMethod == CompilationMethod.Roslyn && compileInBackground && autoGenerateOnSave;
			}
			#endregion

			#region Upgrades
			[SerializeField]
			private float version;
			private const float currentVersion = 3.0f;

			public PreferenceData() {
				OnDeserialized();
			}

			[System.Runtime.Serialization.OnDeserialized]
			private void OnDeserialized() {
				if(version != currentVersion) {
					version = currentVersion;
					//Stuff for upgrades

					if(debugTransitionSpeed == 0) {
						debugTransitionSpeed = 0.5f;
					}
					if(maxGraphBackup == 0) {
						maxGraphBackup = 100;
					}

					#region Initialization
					if(includedAssemblies == null) {
						includedAssemblies = new List<string>() {
							"UnityEngine",
							"UnityEngine.CoreModule",
							"Assembly-CSharp",
							"Assembly-CSharp-firstpass",
							"mscorlib",
						};
					}
					if(nodeBrowserNamespaces == null) {
						nodeBrowserNamespaces = new List<string>() {
							"System",
							"System.Collections",
							"System.Collections.Generic",
							"UnityEngine",
							"UnityEngine.AI",
							"UnityEngine.EventSystems",
							"UnityEngine.UI",
							"UnityEngine.UIElements",
						};
					}
					if(excludedNamespaces == null) {
						excludedNamespaces = new List<string>() {
							"AOT",
							"MaxyGames*",
							"JetBrains.Annotations",
							"Mono.Security.Cryptography",
							"Microsoft*",
							"System.Buffers*",
							"System.CodeDom*",
							"System.Collections.Concurrent",
							"System.Collections.ObjectModel",
							"System.Collections.Specialized",
							"System.ComponentModel*",
							"System.Configuration",
							"System.Dynamic",
							"System.Diagnostics",
							"System.Globalization",
							"System.IO*",
							"System.Configuration.Assemblies",
							"System.Deployment.Internal",
							"System.Diagnostics*",
							"System.Media",
							"System.Management*",
							"System.Linq*",
							"System.Net*",
							"System.Numerics",
							"System.Reflection*",
							"System.Resources",
							"System.Text*",
							"System.Timers",
							"System.Threading*",
							"System.Web",
							"System.Windows*",
							"System.Security*",
							"System.Runtime*",
							"UnityEngineInternal*",
							"Unity.*",
							"UnityEditor.*",
							"UnityEditorInternal*",
							"UnityEngine.Apple*",
							"UnityEngine.Android",
							"UnityEngine.Animations",
							"UnityEngine.CrashReportHandler",
							"UnityEngine.Device",
							"UnityEngine.Experimental*",
							"UnityEngine.Internal*",
							"UnityEngine.iOS*",
							"UnityEngine.SocialPlatforms*",
							"UnityEngine.VR*",
							"UnityEngine.Tizen*",
							"UnityEngine.Diagnostics",
							"UnityEditor.Experimental*",
							"UnityEngine.Assertions*",
							"UnityEngine.Rendering*",
							"UnityEngine.Jobs",
							"UnityEngine.Lumin",
							"UnityEngine.LowLevel",
							"UnityEngine.Playables",
							"UnityEngine.PlayerLoop",
							"UnityEngine.Pool",
							"UnityEngine.Profiling*",
							"UnityEngine.tvOS",
							"UnityEngine.WSA",
							"UnityEngine.Sprites",
							"UnityEngine.Scripting*",
							"UnityEngine.Serialization",
							"UnityEngine.Search",
							"UnityEngine.Networking.PlayerConnection",
							"UnityEngine.TestTools",
							"UnityEngine.Windows*",
							"UnityEngine.U2D",
						};
					}
					#endregion

				}
				if(maxRecentItemsToShow == 0) {
					maxRecentItemsToShow = 10;
				}
			}
			#endregion

			#region Hidden Preferences
			internal int m_itemSearchKind;
			internal int m_itemDeepSearchKind;
			#endregion
		}

		class NoneGraph : GraphEditor {
			private static NoneGraph _instance;
			public static NoneGraph instance {
				get {
					if(_instance == null) {
						_instance = new NoneGraph();
					}
					return _instance;
				}
			}

			public override void DrawCanvas(uNodeEditor window) {
				GUIContent info = new GUIContent("There's no graph theme.");
				Vector2 size = EditorStyles.label.CalcSize(info);
				GUI.Label(new Rect((window.position.width / 2) - (size.x / 2), (window.position.height / 2) - (size.y / 2), size.x, size.y), info);
			}

			public override void Highlight(UGraphElement element) {

			}
		}

		public static PreferenceData preferenceData {
			get {
				Initialize();
				return _preferenceData;
			}
		}

		private static PreferenceData _preferenceData;
		private static bool isInitialized = false, assemblyT, isDim, isLocked, advanced;
		private static Vector2 scrollPos;
		private static Assembly[] assemblies;

		private static int selectedMenu = 0;

		[PreferenceItem("uNode")]
		public static void PreferencesGUI() {
			Initialize();
			selectedMenu = GUILayout.Toolbar(selectedMenu, new string[] { "Graph", "Editor", "Type", "Code Generation" });
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			EditorGUI.BeginChangeCheck();
			if(selectedMenu == 0) {//Graph
				#region Theme
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(new GUIContent("Theme"));
				if(uNodeGUI.Button(string.IsNullOrEmpty(preferenceData.editorTheme) ? "Default" : preferenceData.editorTheme, EditorStyles.popup)) {
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Default"), string.IsNullOrEmpty(preferenceData.editorTheme), () => {
						preferenceData.editorTheme = "";
						Cached.editorTheme = null;
						nodeGraph.OnDisable();
						Cached.nodeGraph = null;
						SavePreference();
					});
					menu.AddSeparator("");
					var theme = uNodeEditorUtility.FindAssetsByType<EditorTheme>(new[] { "Assets", uNodeEditorUtility.GetUNodePath() });
					foreach(var t in theme) {
						if(t.ThemeName.ToLower() == "[default]")
							continue;
						menu.AddItem(new GUIContent(t.ThemeName), preferenceData.editorTheme == t.ThemeName, () => {
							preferenceData.editorTheme = t.ThemeName;
							ReloadTheme();
							SavePreference();
						});
					}
					var customGraphs = uNodeEditorUtility.FindCustomGraph();
					if(customGraphs.Count > 0) {
						menu.AddSeparator("");
						foreach(var g in customGraphs) {
							if(string.IsNullOrEmpty(g.name))
								continue;
							menu.AddItem(new GUIContent(g.name), preferenceData.editorTheme == "@" + g.name, () => {
								preferenceData.editorTheme = "@" + g.name;
								ReloadTheme();
								SavePreference();
							});
						}
					}
					menu.ShowAsContext();
				}
				EditorGUILayout.EndHorizontal();
				if(!string.IsNullOrEmpty(preferenceData.editorTheme) && editorTheme != null && AssetDatabase.Contains(editorTheme)) {
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField(new GUIContent("Theme Asset"), editorTheme, typeof(EditorTheme), false);
					EditorGUI.EndDisabledGroup();
					//editorTheme.expanded = EditorGUILayout.Foldout(editorTheme.expanded, "Theme Settings");
					//if(editorTheme.expanded) {
					//	Editor editor = CustomInspector.GetEditor(editorTheme);
					//	EditorGUI.indentLevel++;
					//	editor.OnInspectorGUI();
					//	EditorGUI.indentLevel--;
					//}
					//if(GUI.changed) {
					//	uNodeEditorUtility.MarkDirty(editorTheme);
					//}
				}
				#endregion

				preferenceData.displayKind = (DisplayKind)EditorGUILayout.EnumPopup(new GUIContent("Display Kind"), preferenceData.displayKind);
				//preferenceData.isDim = EditorGUILayout.Toggle(new GUIContent("Dim Node"), preferenceData.isDim);

				bool showGrid = EditorGUILayout.Toggle(new GUIContent("Show Grid"), preferenceData.showGrid);
				if(showGrid != preferenceData.showGrid) {
					preferenceData.showGrid = showGrid;
					uNodeEditor.RefreshEditor(true);
				}
				preferenceData.maxReloadMilis = EditorGUILayout.IntField(new GUIContent("Max Reload Milis",
@"The max progress for loading a graph in milliseconds for each frame.
-Reducing value will make loading graph slower but less lag for Unity.
-Increase value will make loading graph faster but more lag for Unity.

Recommended value is between 10-100."), preferenceData.maxReloadMilis);

				uNodeGUIUtility.ShowField(nameof(preferenceData.autoConvertPort), preferenceData);
				uNodeGUIUtility.ShowField(nameof(preferenceData.autoCreateReroute), preferenceData);
				uNodeGUIUtility.ShowField(nameof(preferenceData.autoProxyConnection), preferenceData);
				uNodeGUIUtility.ShowField(nameof(preferenceData.autoAddNamespace), preferenceData);
				preferenceData.debugTransitionSpeed = EditorGUILayout.Slider(new GUIContent("Debug Transition Speed"), preferenceData.debugTransitionSpeed, 0.5f, 4);

				EditorGUILayout.LabelField("Snapping", EditorStyles.boldLabel);
				uNodeGUIUtility.ShowField(nameof(preferenceData.enableSnapping), preferenceData);
				if(preferenceData.enableSnapping) {
					uNodeGUIUtility.ShowField(nameof(preferenceData.graphSnapping), preferenceData);
					uNodeGUIUtility.ShowField(nameof(preferenceData.nodePortSnapping), preferenceData);
					if(preferenceData.nodePortSnapping) {
						preferenceData.portSnappingRange = EditorGUILayout.Slider(new GUIContent("Port Snapping Range"), preferenceData.portSnappingRange, 10, 100);
					}
					uNodeGUIUtility.ShowField(nameof(preferenceData.gridSnapping), preferenceData);
					uNodeGUIUtility.ShowField(nameof(preferenceData.spacingSnapping), preferenceData);
				}
				EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
				if(uNodeUtility.isOSXPlatform == false) {
					uNodeGUIUtility.ShowField(nameof(preferenceData.disableRightClickMove), preferenceData);
				}
				uNodeGUIUtility.ShowField(nameof(preferenceData.autoCarryInputValue), preferenceData);
			}
			else if(selectedMenu == 1) {
				preferenceData.inspectorIntegration = EditorGUILayout.Toggle(new GUIContent("Inspector Integration", "If true, graph inspector can be displayed on Unity Inspector"), preferenceData.inspectorIntegration);
				preferenceData.autoBackupOnSave = EditorGUILayout.Toggle(new GUIContent("Create Backup On Save", "Auto create backup graph on save.\nOnly changed assets are backuped"), preferenceData.autoBackupOnSave);
				uNodeGUIUtility.ShowField(nameof(preferenceData.maxGraphBackup), preferenceData);
				// preferenceData.isLocked = EditorGUILayout.Toggle(new GUIContent("Lock Selection"), preferenceData.isLocked);
				uNodeGUIUtility.ShowField(nameof(preferenceData.newVariableAccessor), preferenceData);
				uNodeGUIUtility.ShowField(nameof(preferenceData.newPropertyAccessor), preferenceData);
				uNodeGUIUtility.ShowField(nameof(preferenceData.newFunctionAccessor), preferenceData);

				using(new EditorGUILayout.HorizontalScope()) {
					preferenceData.ilSpyPath = EditorGUILayout.TextField("ILSpy Path", preferenceData.ilSpyPath);
					if(GUILayout.Button("Browse", GUILayout.Width(100))) {
						string path = EditorUtility.OpenFilePanel("Select ILSpy executable", "", uNodeUtility.isOSXPlatform ? "" : "exe");
						if(!string.IsNullOrEmpty(path)) {
							preferenceData.ilSpyPath = path;
						}
					}
				}

				EditorGUILayout.LabelField("Node Browser", EditorStyles.boldLabel);
				uNodeGUIUtility.ShowField(nameof(preferenceData.itemSelectorWidth), preferenceData);
				uNodeGUIUtility.ShowField(nameof(preferenceData.itemSelectorHeight), preferenceData);
				uNodeGUIUtility.ShowField(nameof(preferenceData.maxRecentItemsToShow), preferenceData);
				preferenceData.coloredItem = EditorGUILayout.Toggle(new GUIContent("Colored Item"), preferenceData.coloredItem);
				if(preferenceData.coloredItem) {
					uNodeGUIUtility.ShowField(new GUIContent("Type Color"), "itemTypeColor", preferenceData);
					uNodeGUIUtility.ShowField(new GUIContent("Keyword Color"), "itemKeywordColor", preferenceData);
					uNodeGUIUtility.ShowField(new GUIContent("Interface Color"), "itemInterfaceColor", preferenceData);
					uNodeGUIUtility.ShowField(new GUIContent("Enum Color"), "itemEnumColor", preferenceData);
				}
				uNodeGUI.DrawNamespace("Browser Namespaces List", preferenceData.nodeBrowserNamespaces, null, (val) => {
					preferenceData.nodeBrowserNamespaces = val as List<string>;
					SavePreference();
				});

				uNodeGUIUtility.EditValue(
					uNodeGUIUtility.GetRect(),
					new GUIContent("Browser Excluded Types"),
					preferenceData.excludedTypes,
					typeof(List<string>),
					(obj) => {
						preferenceData.excludedTypes = obj as List<string>;
						SavePreference();
					});

				advanced = EditorGUILayout.Foldout(advanced, "Advanced");
				if(advanced) {
					EditorGUI.indentLevel++;
					//preferenceData.hideNodeIcons = EditorGUILayout.Toggle(new GUIContent("Hide Node Icons"), preferenceData.hideNodeIcons);
					//preferenceData.hideNodeComment = EditorGUILayout.Toggle(new GUIContent("Hide Node Comment"), preferenceData.hideNodeComment);
					preferenceData.enableErrorCheck = EditorGUILayout.Toggle(new GUIContent("Enable Error Check", "Set to true to enable real time in editor error check."), preferenceData.enableErrorCheck);
					preferenceData.forceReloadGraph = EditorGUILayout.Toggle(new GUIContent("Force Reload Graph", "If true, the graph will be force reloaded so the graph will have persistence data but it will slow every refresing graph since it will recreate the whole graph."), preferenceData.forceReloadGraph);
					EditorGUI.indentLevel--;
				}
			}
			else if(selectedMenu == 2) {
				EditorGUILayout.LabelField("Type Filter", EditorStyles.boldLabel);

				preferenceData.minDeepTypeSearch = EditorGUILayout.IntField(
					new GUIContent(
						"Min Word For Deep Search",
						"The minimal word for performing deep type search"),
					preferenceData.minDeepTypeSearch);

				preferenceData.itemSelectorShowUnselectedTypes = EditorGUILayout.Toggle(new GUIContent("Show Unselectable Types", "If true, unselectable types is always displayed."), preferenceData.itemSelectorShowUnselectedTypes);

				uNodeGUIUtility.ShowField(nameof(preferenceData.showObsoleteItem), preferenceData);
				uNodeGUIUtility.ShowField(nameof(preferenceData.autoHideNativeType), preferenceData);
				uNodeGUIUtility.ShowField(nameof(preferenceData.ignoreIncludedAssemblies), preferenceData);

				uNodeGUI.DrawCustomList(
					preferenceData.includedAssemblies,
					"Included Assemblies",
					(position, index, value) => {
						EditorGUI.LabelField(position, value);
					},
					(position) => {
						List<ItemSelector.CustomItem> items = new List<ItemSelector.CustomItem>();
						var ass = EditorReflectionUtility.GetAssemblies();
						foreach(var assembly in ass) {
							if(assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location))
								continue;
							string assemblyName = assembly.GetName().Name;
							if(!string.IsNullOrEmpty(assemblyName) && !preferenceData.includedAssemblies.Contains(assemblyName)) {
								items.Add(ItemSelector.CustomItem.Create(assemblyName, (obj) => {
									preferenceData.includedAssemblies.Add(assemblyName);
									SavePreference();
									//XmlDoc.ReloadDocInBackground();
								}, "Namespaces"));
							}
						}
						items.Sort((x, y) => string.CompareOrdinal(x.name, y.name));
						ItemSelector.ShowWindow(null, null, null, items).ChangePosition(position.ToScreenRect()).displayDefaultItem = false;
					},
					(index) => {
						preferenceData.includedAssemblies.RemoveAt(index);
						SavePreference();
						//XmlDoc.ReloadDocInBackground();
					}
				);

				if(preferenceData.ignoreIncludedAssemblies == false) {
					uNodeGUIUtility.ShowField(nameof(preferenceData.filterIncludedNamespaces), preferenceData);

					if(preferenceData.filterIncludedNamespaces) {
						uNodeGUI.DrawNamespace("Global Using Namespaces", preferenceData.globalUsingNamespaces, null, (val) => {
							preferenceData.globalUsingNamespaces = val as List<string>;
							SavePreference();
						});
					}
					else {
						uNodeGUIUtility.EditValue(
							uNodeGUIUtility.GetRect(),
							new GUIContent("Excluded Namespace", "The namespaces to be excluded from the included assemblies"),
							preferenceData.excludedNamespaces,
							typeof(List<string>),
							(obj) => {
								preferenceData.excludedNamespaces = obj as List<string>;
								SavePreference();
							});
					}
				}
			}
			else if(selectedMenu == 3) {
				if(uNodeUtility.IsProVersion == false) {
					EditorGUILayout.HelpBox("You are using community version, some of setting are ignored.", MessageType.Info);
				}
				uNodeGUIUtility.EditValueLayouted(
					GUIContent.none,
					preferenceData.generatorData,
					(val) => {
						preferenceData.generatorData = val;
						SavePreference();
					});
			}
			if(GUILayout.Button("Set Default", EditorStyles.miniButton)) {
				SetDefault();
			}
			if(EditorGUI.EndChangeCheck()) {
				SavePreference();
			}
			EditorGUILayout.EndScrollView();
		}

		private static void SetDefault() {
			_preferenceData = new PreferenceData();
			_preferenceData.editorTheme = "";
			ReloadTheme();
			SavePreference();
		}

		private static void Initialize() {
			if(!isInitialized) {
				isInitialized = true;
				LoadPreference();
				assemblies = EditorReflectionUtility.GetAssemblies();
				List<Assembly> ass = new List<Assembly>();
				foreach(Assembly assembly in assemblies) {
					try {
						if(string.IsNullOrEmpty(assembly.Location))
							continue;
						ass.Add(assembly);
					}
					catch { continue; }
				}
				assemblies = ass.ToArray();
			}
		}

		private static HashSet<string> _excludedNamespace;
		/// <summary>
		/// Get the excluded namespaces
		/// </summary>
		/// <returns></returns>
		public static HashSet<string> GetExcludedNamespace() {
			if(_excludedNamespace == null) {
				var namespaces = EditorReflectionUtility.GetNamespaces();
				var excluded = new HashSet<string>();
				foreach(var item in preferenceData.excludedNamespaces) {
					if(string.IsNullOrEmpty(item))
						continue;
					if(item.EndsWith("*")) {
						string name = item.RemoveLast();
						foreach(var ns in namespaces) {
							if(ns == null)
								continue;
							if(ns.StartsWith(name)) {
								excluded.Add(ns);
							}
						}
					}
					else {
						excluded.Add(item);
					}
				}
				_excludedNamespace = excluded;
			}
			return _excludedNamespace;
		}

		private static HashSet<string> _includedAssemblies;
		public static HashSet<string> GetIncludedAssemblies() {
			if(_includedAssemblies == null) {
				_includedAssemblies = new HashSet<string>(preferenceData.includedAssemblies);
			}
			return _includedAssemblies;
		}

		private static HashSet<string> _globalUsingNamespaces;
		public static HashSet<string> GetGlobalUsingNamespaces() {
			if(_globalUsingNamespaces == null) {
				_globalUsingNamespaces = new HashSet<string>(preferenceData.globalUsingNamespaces);
			}
			return _globalUsingNamespaces;
		}

		private static HashSet<string> _browserNamespaces;
		public static HashSet<string> GetBrowserNamespaceList() {
			if(_browserNamespaces == null) {
				_browserNamespaces = new HashSet<string>(preferenceData.nodeBrowserNamespaces);
			}
			return _browserNamespaces;
		}

		private static HashSet<string> _excludedTypes;
		/// <summary>
		/// Get the excluded types
		/// </summary>
		/// <returns></returns>
		public static HashSet<string> GetExcludedTypes() {
			if(_excludedTypes == null) {
				if(preferenceData.excludedTypes == null) {
					return new HashSet<string>();
				}
				_excludedTypes = new HashSet<string>();
				foreach(var item in preferenceData.excludedTypes) {
					if(string.IsNullOrEmpty(item))
						continue;
					_excludedTypes.Add(item);
				}
			}
			return _excludedTypes;
		}

		/// <summary>
		/// Get the preference data
		/// </summary>
		/// <returns></returns>
		public static PreferenceData GetPreference() {
			return preferenceData;
		}

		private static HashSet<Type> m_cachedIgnoredTypes;
		public static HashSet<Type> GetIgnoredTypes() {
			if(preferenceData.autoHideNativeType) {
				return uNodeDatabase.nativeGraphTypes;
			}
			else {
				if(m_cachedIgnoredTypes == null) {
					m_cachedIgnoredTypes = new HashSet<Type>();
				}
				return m_cachedIgnoredTypes;
			}
		}

		/// <summary>
		/// Add a new exluded type to preference and save it.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool AddExcludedType(Type type) {
			if(!preferenceData.excludedTypes.Contains(type.FullName)) {
				preferenceData.excludedTypes.Add(type.FullName);
				SavePreference();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Add a new exluded namespace to preference and save it.
		/// </summary>
		/// <param name="ns"></param>
		/// <returns></returns>
		public static bool AddExcludedNamespace(string ns) {
			if(!preferenceData.excludedNamespaces.Contains(ns)) {
				preferenceData.excludedNamespaces.Add(ns);
				SavePreference();
				return true;
			}
			return false;
		}

		#region Themes
		public static EditorTheme editorTheme {
			get {
				if(Cached.editorTheme == null) {
					if(string.IsNullOrEmpty(preferenceData.editorTheme)) {
						var theme = uNodeEditorUtility.FindAssetsByType<EditorTheme>(new[] { "Assets", uNodeEditorUtility.GetUNodePath() });
						if(theme.Any()) {
							Cached.editorTheme = theme.FirstOrDefault(t => t.ThemeName == "[default]");
							if(Cached.editorTheme == null)
								Cached.editorTheme = theme.FirstOrDefault();
						}
					}
					else {
						var theme = uNodeEditorUtility.FindAssetsByType<EditorTheme>(new[] { "Assets", uNodeEditorUtility.GetUNodePath() });
						foreach(var t in theme) {
							if(string.IsNullOrEmpty(t.ThemeName))
								continue;
							if(t.ThemeName == preferenceData.editorTheme) {
								Cached.editorTheme = t;
								break;
							}
						}
						if(Cached.editorTheme == null && theme.Any()) {
							Cached.editorTheme = theme.FirstOrDefault(t => t.ThemeName == "[default]");
							if(Cached.editorTheme == null)
								Cached.editorTheme = theme.FirstOrDefault();
						}
					}
				}
				return Cached.editorTheme;
			}
		}

		public static GraphEditor nodeGraph {
			get {
				if(Cached.nodeGraph == null) {
					if(!string.IsNullOrEmpty(preferenceData.editorTheme) && preferenceData.editorTheme.StartsWith("@")) {
						var customGraphs = uNodeEditorUtility.FindCustomGraph();
						foreach(var g in customGraphs) {
							if(string.IsNullOrEmpty(g.name))
								continue;
							if(preferenceData.editorTheme == "@" + g.name) {
								Cached.nodeGraph = ReflectionUtils.CreateInstance(g.type) as GraphEditor;
								return Cached.nodeGraph;
							}
						}
					}
					if(editorTheme != null) {
						var graphType = editorTheme.GetGraphType();
						if(graphType == null) {
							var customGraphs = uNodeEditorUtility.FindCustomGraph();
							if(customGraphs != null && customGraphs.Count > 0) {
								graphType = customGraphs[0]?.type;
							}
							if(graphType == null) {
								return NoneGraph.instance;
							}
						}
						Cached.nodeGraph = System.Activator.CreateInstance(graphType) as GraphEditor;
					}
					else {
						return NoneGraph.instance;
					}
				}
				return Cached.nodeGraph;
			}
		}

		private static class Cached {
			public static GraphEditor nodeGraph;
			public static EditorTheme editorTheme;
			public static Dictionary<Type, Color> colorMap = new Dictionary<Type, Color>();
			public static Dictionary<Type, Texture> iconMap = new Dictionary<Type, Texture>();

			public static EditorThemeTypeSettings _defaultThemeTypeSetting;
			public static EditorThemeTypeSettings defaultThemeTypeSetting {
				get {
					if(_defaultThemeTypeSetting == null) {
						_defaultThemeTypeSetting = new EditorThemeTypeSettings();
					}
					return _defaultThemeTypeSetting;
				}
			}
		}

		public static void ReloadTheme() {
			OnThemeChanged();
			Cached.editorTheme = null;
			nodeGraph.OnDisable();
			Cached.nodeGraph = null;
		}

		public static void OnThemeChanged() {
			Cached.colorMap.Clear();
			Cached.iconMap.Clear();
		}

		private static EditorThemeTypeSettings GetTypeSettings() {
			if(uNodePreference.editorTheme != null) {
				return uNodePreference.editorTheme.typeSettings;
			}
			return Cached.defaultThemeTypeSetting;
		}

		public static Color GetColorForType(Type type) {
			if(type == null)
				return GetTypeSettings().defaultTypeColor;
			if(Cached.colorMap.ContainsKey(type)) {
				return Cached.colorMap[type];
			}
			if(type.IsEnum) {
				Cached.colorMap[type] = uNodeUtility.richTextColor().enumColor;
				return uNodeUtility.richTextColor().enumColor;
			}
			else if(type.IsInterface) {
				Cached.colorMap[type] = uNodeUtility.richTextColor().interfaceColor;
				return uNodeUtility.richTextColor().enumColor;
			}
			Cached.colorMap[type] = GetTypeSettings().GetColor(type);
			return Cached.colorMap[type];
		}

		public static Texture GetIconForType(Type type) {
			if(type == null)
				return null;
			if(type.IsByRef) {
				return GetIconForType(type.GetElementType());
			}
			if(Cached.iconMap.TryGetValue(type, out var result)) {
				return result;
			}
			if(type is ICustomIcon) {
				var icon = (type as ICustomIcon).GetIcon();
				if(icon != null) {
					return icon;
				}
			}
			else if(type is IIcon) {
				var icon = (type as IIcon).GetIcon();
				if(icon != null) {
					return GetIconForType(icon);
				}
			}
			//else if(type.IsSubclassOf(typeof(Delegate))) {
			//	result = GetIconForType(typeof(Delegate));
			//	_iconsMap[type] = result;
			//	return result;
			//}
			if(type is RuntimeType) {
				if(type.IsArray || type.IsGenericType) {
					return uNodeEditorUtility.Icons.listIcon;
				}
				var rType = type as RuntimeType;
				return GetIconForType(typeof(TypeIcons.RuntimeTypeIcon));
			}
			result = GetTypeSettings().GetIcon(type) ?? GetDefaultIcon(type);
			if(result != null) {
				Cached.iconMap[type] = result;
				return result;
			}
			if(type.IsDefinedAttribute(typeof(ICustomIcon))) {
				var att = type.GetCustomAttributes(typeof(ICustomIcon), true)[0] as ICustomIcon;
				result = att.GetIcon();
			}
			else if(type == typeof(TypeIcons.FlowIcon)) {
				result = uNodeEditorUtility.Icons.flowIcon;
			}
			else if(type == typeof(TypeIcons.ValueIcon)) {
				result = uNodeEditorUtility.Icons.valueIcon;
			}
			else if(type == typeof(TypeIcons.BranchIcon)) {
				result = uNodeEditorUtility.Icons.divideIcon;
			}
			else if(type == typeof(TypeIcons.ClockIcon)) {
				result = uNodeEditorUtility.Icons.clockIcon;
			}
			else if(type == typeof(TypeIcons.RepeatIcon)) {
				result = uNodeEditorUtility.Icons.repeatIcon;
			}
			else if(type == typeof(TypeIcons.RepeatOnceIcon)) {
				result = uNodeEditorUtility.Icons.repeatOnceIcon;
			}
			else if(type == typeof(TypeIcons.SwitchIcon)) {
				result = uNodeEditorUtility.Icons.divideIcon;
			}
			else if(type == typeof(TypeIcons.MouseIcon)) {
				result = uNodeEditorUtility.Icons.mouseIcon;
			}
			else if(type == typeof(TypeIcons.EventIcon)) {
				result = uNodeEditorUtility.Icons.eventIcon;
			}
			else if(type == typeof(TypeIcons.RotationIcon) || type == typeof(Quaternion)) {
				result = uNodeEditorUtility.Icons.rotateIcon;
			}
			else if(type == typeof(Color) || type == typeof(Color32)) {
				result = uNodeEditorUtility.Icons.colorIcon;
			}
			else if(type == typeof(int)) {
				result = GetIconForType(typeof(TypeIcons.IntegerIcon));
			}
			else if(type == typeof(float)) {
				result = GetIconForType(typeof(TypeIcons.FloatIcon));
			}
			else if(type == typeof(Vector3)) {
				result = GetIconForType(typeof(TypeIcons.Vector3Icon));
			}
			else if(type == typeof(Vector2)) {
				result = GetIconForType(typeof(TypeIcons.Vector2Icon));
			}
			else if(type == typeof(Vector4)) {
				result = GetIconForType(typeof(TypeIcons.Vector4Icon));
			}
			else if(type.IsCastableTo(typeof(UnityEngine.Object))) {
				result = uNodeEditorUtility.Icons.objectIcon;
			}
			else if(type.IsCastableTo(typeof(System.Collections.IList))) {
				result = uNodeEditorUtility.Icons.listIcon;
			}
			else if(type.IsCastableTo(typeof(System.Collections.IDictionary))) {
				result = uNodeEditorUtility.Icons.bookIcon;
			}
			else if(type == typeof(void)) {
				result = GetIconForType(typeof(TypeIcons.VoidIcon));
			}
			else if(type.IsCastableTo(typeof(KeyValuePair<,>))) {
				result = uNodeEditorUtility.Icons.keyIcon;
			}
			else if(type == typeof(DateTime) || type == typeof(Time)) {
				result = uNodeEditorUtility.Icons.dateIcon;
			}
			else if(type.IsInterface) {
				result = GetIconForType(typeof(TypeIcons.InterfaceIcon));
			}
			else if(type.IsEnum) {
				result = GetIconForType(typeof(TypeIcons.EnumIcon));
			}
			else if(type == typeof(object)) {
				result = uNodeEditorUtility.Icons.valueBlueIcon;
			}
			else if(type == typeof(bool)) {
				result = uNodeEditorUtility.Icons.valueYellowRed;
			}
			else if(type == typeof(string)) {
				result = GetIconForType(typeof(TypeIcons.StringIcon));
			}
			else if(type == typeof(Type)) {
				result = uNodeEditorUtility.Icons.valueGreenIcon;
			}
			else if(type == typeof(UnityEngine.Random) || type == typeof(System.Random)) {
				result = GetIconForType(typeof(TypeIcons.RandomIcon));
			}
			// else if(type == typeof(UnityEngine.Debug)) {
			// 	result = GetIconForType(typeof(TypeIcons.BugIcon));
			// } 
			else {
				result = uNodeEditorUtility.GetIcon(type);
			}
			return Cached.iconMap[type] = result;
		}

		private static Texture GetScriptTypeIcon(string scriptName) {
			var scriptObject = (UnityEngine.Object)uNodeEditorUtility.Icons.EditorGUIUtility_GetScriptObjectFromClass.InvokeOptimized(null, new object[] { scriptName });
			if(scriptObject != null) {
				var scriptIcon = uNodeEditorUtility.Icons.EditorGUIUtility_GetIconForObject.InvokeOptimized(null, new object[] { scriptObject }) as Texture;

				if(scriptIcon != null) {
					return scriptIcon;
				}
			}
			var scriptPath = AssetDatabase.GetAssetPath(scriptObject);
			if(scriptPath != null) {
				switch(Path.GetExtension(scriptPath)) {
					case ".js":
						return EditorGUIUtility.IconContent("js Script Icon").image;
					case ".cs":
						return EditorGUIUtility.IconContent("cs Script Icon").image;
					case ".boo":
						return EditorGUIUtility.IconContent("boo Script Icon").image;
				}
			}
			return null;
		}

		private static Texture GetDefaultIcon(Type type) {
			if(type == null || type.IsGenericParameter) return null;
			if(typeof(MonoBehaviour).IsAssignableFrom(type)) {
				var icon = EditorGUIUtility.ObjectContent(null, type)?.image;
				if(icon == EditorGUIUtility.FindTexture("DefaultAsset Icon")) {
					icon = null;
				}
				if(icon != null) {
					return icon;
				}
				else {
					icon = GetScriptTypeIcon(type.Name);
					if(icon != null) {
						return icon;
					}
				}
			}
			if(typeof(UnityEngine.Object).IsAssignableFrom(type)) {
				Texture icon = EditorGUIUtility.ObjectContent(null, type)?.image;
				if(icon == EditorGUIUtility.FindTexture("DefaultAsset Icon")) {
					icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
				}
				if(icon != null) {
					return icon;
				}
			}
			return null;
		}
		#endregion

		#region Save & Load
		internal const string preferenceDirectory = "uNode3Data";

		internal static string backupPath => preferenceDirectory + Path.DirectorySeparatorChar + "Backup";

		public static void LoadPreference() {
			char separator = Path.DirectorySeparatorChar;
			string path = preferenceDirectory + separator + "PreferenceData" + ".byte";
			if(File.Exists(path)) {
				_preferenceData = SerializerUtility.Deserialize<PreferenceData>(File.ReadAllBytes(path));
			}
			if(_preferenceData == null) {
				SetDefault();
			}
		}

		public static void SavePreference() {
			if(_preferenceData == null)
				return;
			_excludedNamespace = null;
			_excludedTypes = null;
			_includedAssemblies = null;
			_browserNamespaces = null;
			_globalUsingNamespaces = null;
			OnThemeChanged();

			Directory.CreateDirectory(preferenceDirectory);
			char separator = Path.DirectorySeparatorChar;
			string path = preferenceDirectory + separator + "PreferenceData" + ".byte";
			File.WriteAllBytes(path, SerializerUtility.Serialize(_preferenceData));
		}
		#endregion
	}
}