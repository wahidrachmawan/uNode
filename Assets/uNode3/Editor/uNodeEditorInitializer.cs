using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.Callbacks;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using MaxyGames.OdinSerializer.Editor;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Provides function to initialize useful function.
	/// </summary>
	[InitializeOnLoad]
	public class uNodeEditorInitializer {
		static Texture uNodeIcon;
		static List<int> markedObjects = new List<int>();
		static HashSet<string> assetGUIDs = new HashSet<string>();
		static Dictionary<string, Object> markedAssets = new Dictionary<string, UnityEngine.Object>();

		static uNodeEditorInitializer() {
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyItem;
			EditorApplication.projectWindowItemInstanceOnGUI += ProjectItem;
			SceneView.duringSceneGui += OnSceneGUI;
			EditorApplication.update += Update;
			Undo.undoRedoPerformed += UndoRedoPerformed;
			// Setup();
			uNodeUtility.Init();

			uNodeGUIUtility.onGUIChanged += GUIChanged;

			#region Bind Init
			EditorApplication.playModeStateChanged += OnPlayModeChanged;

			var typePatcher = "MaxyGames.UNode.Editors.TypePatcher".ToType(false);
			if(typePatcher != null) {
				var method = typePatcher.GetMethod("Patch", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				EditorBinding.patchType = (System.Action<System.Type, System.Type>)System.Delegate.CreateDelegate(typeof(System.Action<System.Type, System.Type>), method);
			}
			if(EditorApplication.isPlayingOrWillChangePlaymode) {
				uNodeUtility.isPlaying = true;
#if UNODE_COMPILE_ON_PLAY && UNODE_PRO
				uNodeThreadUtility.Queue(() => {
					GenerationUtility.CompileProjectGraphsAnonymous();
				});
#endif
			}
			else {
				//Set is playing to false
				uNodeUtility.isPlaying = false;
			}
			#endregion

			#region Compilers
			RoslynUtility.Data.GetAssemblies = EditorReflectionUtility.GetAssemblies;
			RoslynUtility.Data.useSourceGenerators = () => uNodePreference.preferenceData.generatorData.useSourceGenerators;
			RoslynUtility.Data.compilationMethod = () => uNodePreference.preferenceData.generatorData.compilationMethod;
			RoslynUtility.Data.tempAssemblyPath = GenerationUtility.tempAssemblyPath;
			if(uNodePreference.preferenceData.generatorData.autoStartInitialization) {
				//Wait for 100 frame before start the initialization
				//Cause we don't want to impact a performance after recompilation or entering playmode
				uNodeThreadUtility.ExecuteAfter(100, () => {
					EditorProgressBar.ShowProgressBar("Initializing Compiler", 1);
					//Create a new thread to init the Roslyn, since we don't want to freeze the Unity even for a few seconds.
					var thread = new System.Threading.Thread(() => {
						try {
							//Start compiling in background.
							RoslynUtility.CompileScript(new[] { "struct Dummy { public int x; public int y; }" });
							uNodeThreadUtility.Queue(() => {
								//Clear the progress bar
								EditorProgressBar.ClearProgressBar();
							});
						}
						catch(System.Threading.ThreadAbortException) { }
						catch(Exception ex) {
							Debug.Log(ex.Message + "\n" + ex.GetType());
						}
					});
					thread.IsBackground = true;
					thread.Priority = System.Threading.ThreadPriority.Lowest;
					//Start the thread
					thread.Start();

				});
			}
			#endregion

			#region uNodeUtils Init
			uNodeUtility.isInEditor = true;
			//if(uNodeUtility.guiChanged == null) {
			//	uNodeUtility.guiChanged += uNodeEditor.GUIChanged;
			//}
			if(uNodeUtility.richTextColor == null) {
				uNodeUtility.richTextColor = () => {
					if(uNodePreference.editorTheme != null) {
						return uNodePreference.editorTheme.textSettings;
					}
					else {
						return new EditorTextSetting();
					}
				};
			}
			if(uNodeUtility.getColorForType == null) {
				uNodeUtility.getColorForType = (t) => {
					return uNodePreference.GetColorForType(t);
				};
			}
			if(uNodeUtility.getObjectID == null) {
				Dictionary<Object, int> keys = new Dictionary<Object, int>();
				uNodeUtility.getObjectID = delegate (UnityEngine.Object obj) {
					if(obj == null)
						return 0;
					if(uNodeThreadUtility.IsInMainThread == false) {
						//In case this is not main thread.
						if(keys.TryGetValue(obj, out var rezult)) {
							return rezult;
						}
						return obj.GetHashCode();
					}
					if(!EditorUtility.IsPersistent(obj)) {
						return obj.GetHashCode();
					}
					if(uNodeEditorUtility.IsPrefabInstance(obj)) {
						var o = PrefabUtility.GetCorrespondingObjectFromSource(obj);
						if(o == null)
							return obj.GetHashCode();
						return uNodeUtility.GetObjectID(o);
					}
					if(keys.TryGetValue(obj, out var result) == false) {
						if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long localID)) {
							var id = uNodeUtility.GetHashCode(uNodeUtility.GetHashCode(guid), localID);
							result = (int)id;
						}
						else {
							result = obj.GetHashCode();
						}
						keys[obj] = result;
					}
					return result;
				};
			}
			#endregion

			var path = uNodeEditorUtility.GetUNodePath() + "/Pro.Editor";
			if(Directory.Exists(path)) {
#if !UNODE_PRO
				uNodeThreadUtility.ExecuteAfter(60, () => {
					uNodeEditorUtility.AddDefineSymbols(new string[] { "UNODE_PRO" });
				});
#endif
			}
			else {
#if UNODE_PRO
				uNodeThreadUtility.ExecuteAfter(60, () => {
					uNodeEditorUtility.RemoveDefineSymbols(new string[] { "UNODE_PRO" });
				});
#endif
			}
		}

		private static void GUIChanged(object obj, UIChangeType changeType) {
			Graph graphData = null;
			if(obj is IGraph) {
				graphData = (obj as IGraph).GraphData;
			}
			else if(obj is Node) {
				graphData = (obj as Node).nodeObject.graph;
			}
			else if(obj is UGraphElement) {
				graphData = (obj as UGraphElement).graph;
			}
			if(graphData != null) {
				graphData.version++;
				RuntimeGraphUtility.GraphRunner.UpdateAllRunners();
			}
		}

		private static void UndoRedoPerformed() {
			uNodeUtility.undoRedoPerformed = true;
			uNodeThreadUtility.ExecuteAfter(1, () => {
				uNodeUtility.undoRedoPerformed = false;
			});

			uNodeEditor.window?.UndoRedoPerformed();
			if(Selection.activeObject is CustomInspector customInspector) {
				customInspector.unserializedEditorData = null;
			}
			//Update all graph for live edit
			RuntimeGraphUtility.GraphRunner.UpdateAllRunners();
		}

		private static void ResetGraphAssets() {
			var assets = uNodeEditorUtility.FindAssetsByType(typeof(ScriptableObject));
			foreach(var asset in assets) {
				if(asset is IRefreshable refreshable) {
					refreshable.Refresh();
				}
			}

		}

		[InitializeOnEnterPlayMode]
		private static void OnEnterPlayMode() {
			//Set is playing to true
			uNodeUtility.isPlaying = true;
			uNodeThreadUtility.Update();
		}

		private static void OnPlayModeChanged(PlayModeStateChange state) {
			switch(state) {
				case PlayModeStateChange.ExitingEditMode: {
					//Make sure we save all temporary graph on exit play mode or edit mode.
					GraphUtility.SaveAllGraph();
					//GenerationUtility.SaveData();
					//if(uNodeEditor.window != null) {
					//	uNodeEditor.window.SaveEditorData();
					//}
					ResetGraphAssets();

					////Update the queued until all is completed.
					//int count = 0;
					//while(uNodeThreadUtility.IsNeedUpdate()) {
					//	count++;
					//	if(count > 1000) {
					//		//Force skip the update.
					//		break;
					//	}
					//	uNodeThreadUtility.Update();
					//}
					uNodeThreadUtility.ClearTask();
					break;
				}
				case PlayModeStateChange.EnteredEditMode: {
					//If user is saving graph in play mode
					if(EditorPrefs.GetBool("unode_graph_saved_in_playmode", true)) {
						//Ensure the saved graph in play mode keep the changes.
						//GraphUtility.DestroyTempGraph();
						EditorPrefs.SetBool("unode_graph_saved_in_playmode", false);
					}
					//Set is playing to false
					uNodeUtility.isPlaying = false;
					//Clear the invalid Runtime Type
					ReflectionUtils.ClearInvalidRuntimeType();

					uNodeThreadUtility.ExecuteAfter(5, () => {
						uNodeEditor.ClearCache();
					});
					//EditorBinding.restorePatch?.Invoke();
					break;
				}
				case PlayModeStateChange.ExitingPlayMode: {
					//Update the assembly
					ReflectionUtils.UpdateAssemblies();
					//Clean compiled runtime assembly so the runtime type is cannot be loaded again
					ReflectionUtils.CleanRuntimeAssembly();

					////Update the queued until all is completed.
					//int count = 0;
					//while(uNodeThreadUtility.IsNeedUpdate()) {
					//	count++;
					//	if(count > 1000) {
					//		//Force skip the update.
					//		break;
					//	}
					//	uNodeThreadUtility.Update();
					//}
					uNodeThreadUtility.ClearTask();

					//Clear all debug data.
					GraphDebug.debugData.Clear();
					break;
				}
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		static void RuntimeSetup() {
			GraphDebug.debugData.Clear();
			//If play mode options is enable and domain reload is disable
			if(EditorSettings.enterPlayModeOptionsEnabled && EditorSettings.enterPlayModeOptions.HasFlags(EnterPlayModeOptions.DisableDomainReload)) {
				//then enable is playing and clean graph cache
				uNodeUtility.isPlaying = true;
				uNodeEditor.ClearCache();
				//Clean Type Cache so it get fresh types.
				TypeSerializer.CleanCache();
				//Clean compiled runtime assembly so the runtime type is cannot be loaded again.
				ReflectionUtils.CleanRuntimeAssembly();
#if UNODE_COMPILE_ON_PLAY && UNODE_PRO
				if(uNodePreference.preferenceData.generatorData.compilationMethod == CompilationMethod.Unity) {
					//Do compile graphs project in temporary folder and load it when using auto compile on play
					GenerationUtility.CompileProjectGraphsAnonymous();
				}
#endif
			}
			//Load a Runtime Graph Assembly.
			if(uNodePreference.preferenceData.generatorData.compilationMethod == CompilationMethod.Roslyn) {
				GenerationUtility.LoadRuntimeAssembly();
#if UNODE_COMPILE_ON_PLAY && UNODE_PRO
				Debug.LogWarning("Warning: you're using Compile On Play & Roslyn compilation, this is not supported.\nThe auto compile on play will be ignored.");
#endif
			}
		}

		[InitializeOnLoadMethod]
		static void Setup() {
			uNodeIcon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.UNodeIcon));
			UpdateMarkedObject();

			#region Debug Init
			{
				GraphDebug.Breakpoint.onBreakpointChanged -= SaveDebugData;
				GraphDebug.Breakpoint.onBreakpointChanged += SaveDebugData;
			}
			if(GraphDebug.Breakpoint.getBreakpoints == null) {
				GraphDebug.Breakpoint.getBreakpoints = () => {
					return nodeDebugData;
				};
			}
			#endregion

			#region Completions
			if(CompletionEvaluator.completionToNode == null) {
				CompletionEvaluator.completionToNode = (CompletionInfo completion, GraphEditorData editorData, Vector2 graphPosition) => {
					NodeObject result = null;
					if(completion.isKeyword) {
						switch(completion.keywordKind) {
							case KeywordKind.As:
								NodeEditorUtility.AddNewNode<Nodes.NodeConvert>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Break:
								NodeEditorUtility.AddNewNode<Nodes.NodeBreak>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Continue:
								NodeEditorUtility.AddNewNode<Nodes.NodeContinue>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Default:
								NodeEditorUtility.AddNewNode<Nodes.DefaultNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.type = member.startType;
											}
										}
										result = node;
									});
								break;
							case KeywordKind.For:
								NodeEditorUtility.AddNewNode<Nodes.ForNumberLoop>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Foreach:
								NodeEditorUtility.AddNewNode<Nodes.ForeachLoop>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.If:
								NodeEditorUtility.AddNewNode<Nodes.NodeIf>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.condition.AssignToDefault(member);
											}
										}
										result = node;
									});
								break;
							case KeywordKind.Is:
								NodeEditorUtility.AddNewNode<Nodes.ISNode>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Lock:
								NodeEditorUtility.AddNewNode<Nodes.NodeLock>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target.AssignToDefault(member);
											}
										}
										result = node;
									});
								break;
							case KeywordKind.Return:
								NodeEditorUtility.AddNewNode<Nodes.NodeReturn>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Switch:
								NodeEditorUtility.AddNewNode<Nodes.NodeSwitch>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target.AssignToDefault(member);
											}
										}
										result = node;
									});
								break;
							case KeywordKind.Throw:
								NodeEditorUtility.AddNewNode<Nodes.NodeThrow>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Try:
								NodeEditorUtility.AddNewNode<Nodes.NodeTry>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.Try.ConnectTo(member.startItem.GetReferenceValue() as UPort);
											}
										}
										result = node;
									});
								break;
							case KeywordKind.Using:
								NodeEditorUtility.AddNewNode<Nodes.NodeUsing>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.While:
								NodeEditorUtility.AddNewNode<Nodes.WhileLoop>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.condition.AssignToDefault(member);
											}
										}
										result = node;
									});
								break;
						}
					}
					else if(completion.isSymbol) {
						switch(completion.name) {
							case "+":
							case "-":
							case "*":
							case "/":
							case "%":
								NodeEditorUtility.AddNewNode<Nodes.MultiArithmeticNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.inputs[0].port.AssignToDefault(member);
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.inputs[1].port.AssignToDefault(member);
											}
										}
										if(completion.name == "+") {
											node.operatorKind = ArithmeticType.Add;
										}
										else if(completion.name == "-") {
											node.operatorKind = ArithmeticType.Subtract;
										}
										else if(completion.name == "*") {
											node.operatorKind = ArithmeticType.Multiply;
										}
										else if(completion.name == "/") {
											node.operatorKind = ArithmeticType.Divide;
										}
										else if(completion.name == "%") {
											node.operatorKind = ArithmeticType.Modulo;
										}
										result = node;
									});
								break;
							case "==":
							case "!=":
							case ">":
							case ">=":
							case "<":
							case "<=":
								NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.inputA.AssignToDefault(member);
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.inputB.AssignToDefault(member);
											}
										}
										if(completion.name == "==") {
											node.operatorKind = ComparisonType.Equal;
										}
										else if(completion.name == "!=") {
											node.operatorKind = ComparisonType.NotEqual;
										}
										else if(completion.name == ">") {
											node.operatorKind = ComparisonType.GreaterThan;
										}
										else if(completion.name == ">=") {
											node.operatorKind = ComparisonType.GreaterThanOrEqual;
										}
										else if(completion.name == "<") {
											node.operatorKind = ComparisonType.LessThan;
										}
										else if(completion.name == "<=") {
											node.operatorKind = ComparisonType.LessThanOrEqual;
										}
										result = node;
									});
								break;
							case "++":
							case "--":
								NodeEditorUtility.AddNewNode<Nodes.IncrementDecrementNode>(editorData,
									graphPosition,
									(node) => {
										node.isDecrement = completion.name == "--";
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target.AssignToDefault(member);
											}
											node.isPrefix = false;
										}
										else if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target.AssignToDefault(member);
											}
											node.isPrefix = true;
										}
										result = node;
									});
								break;
							case "=":
							case "+=":
							case "-=":
							case "/=":
							case "*=":
							case "%=":
								NodeEditorUtility.AddNewNode<Nodes.NodeSetValue>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target.AssignToDefault(member);
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.value.AssignToDefault(member);
											}
										}
										if(completion.name == "=") {
											node.setType = SetType.Change;
										}
										else if(completion.name == "+=") {
											node.setType = SetType.Add;
										}
										else if(completion.name == "-=") {
											node.setType = SetType.Subtract;
										}
										else if(completion.name == "/=") {
											node.setType = SetType.Divide;
										}
										else if(completion.name == "*=") {
											node.setType = SetType.Multiply;
										}
										else if(completion.name == "%=") {
											node.setType = SetType.Modulo;
										}
										result = node;
									});
								break;
							case "||":
								NodeEditorUtility.AddNewNode<Nodes.MultiORNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.inputs[0].port.AssignToDefault(member);
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.inputs[1].port.AssignToDefault(member);
											}
										}
										result = node;
									});
								break;
							case "&&":
								NodeEditorUtility.AddNewNode<Nodes.MultiANDNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.inputs[0].port.AssignToDefault(member);
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.inputs[1].port.AssignToDefault(member);
											}
										}
										result = node;
									});
								break;
							default:
								throw new System.Exception("Unsupported symbol:" + completion.name);
						}
					}
					return result;
				};
			}
			#endregion

			EditorReflectionUtility.GetNamespaces();
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
			EditorApplication.quitting += OnUnityQuitting;

			uNodeThreadUtility.ExecuteAfter(5, () => {
				XmlDoc.LoadDocInBackground();
			});

			DragAndDrop.AddDropHandler(HierarchyDropHandler);
			DragAndDrop.AddDropHandler(InspectorWindowDrag);

			Update();
		}

		private static DragAndDropVisualMode InspectorWindowDrag(Object[] targets, bool perform) {
			void DoDropComponentType(Type type, UnityEngine.Object target) {
				if(target is GameObject) {
					var gameObject = target as GameObject;
					var comp = Undo.AddComponent(gameObject, type);
				}
				else if(target is Component) {
					var component = target as Component;
					var comp = Undo.AddComponent(component.gameObject, type);
					var components = component.GetComponents<Component>();
					var index = Array.IndexOf(components, component);
					var count = components.Length - index - 1;
					for(int i = 0; i < count; i++) {
						UnityEditorInternal.ComponentUtility.MoveComponentUp(comp);
					}
				}
			}
			void DoDropClassDefinition(ClassDefinition asset, UnityEngine.Object target) {
				if(target is GameObject) {
					var gameObject = target as GameObject;
					var comp = Undo.AddComponent<ClassComponent>(gameObject);
					comp.target = asset;
				}
				else if(target is Component) {
					var component = target as Component;
					var comp = Undo.AddComponent<ClassComponent>(component.gameObject);
					comp.target = asset;
					var components = component.GetComponents<Component>();
					var index = Array.IndexOf(components, component);
					var count = components.Length - index - 1;
					for(int i = 0; i < count; i++) {
						UnityEditorInternal.ComponentUtility.MoveComponentUp(comp);
					}
				}
			}
			void DoDropSingleton(GraphSingleton asset, UnityEngine.Object target) {
				if(target is GameObject) {
					var gameObject = target as GameObject;
					var comp = Undo.AddComponent<SingletonInitializer>(gameObject);
					comp.target = asset;
				}
				else if(target is Component) {
					var component = target as Component;
					var comp = Undo.AddComponent<SingletonInitializer>(component.gameObject);
					comp.target = asset;
					var components = component.GetComponents<Component>();
					var index = Array.IndexOf(components, component);
					var count = components.Length - index - 1;
					for(int i = 0; i < count; i++) {
						UnityEditorInternal.ComponentUtility.MoveComponentUp(comp);
					}
				}
			}

			DragAndDropVisualMode HandleDrag(UnityEngine.Object obj) {
				if(obj is ClassDefinition classDefinition) {
					if(classDefinition.InheritType.IsCastableTo(typeof(MonoBehaviour))) {
						if(perform) {
							DoDropClassDefinition(classDefinition, targets.FirstOrDefault());
						}
						else {

						}
						return DragAndDropVisualMode.Copy;
					}
					return DragAndDropVisualMode.Rejected;
				}
				else if(obj is IScriptGraph scriptGraph) {
					foreach(var type in scriptGraph.TypeList) {
						if(type is GraphAsset graph && graph.GetGraphName() == obj.name) {
							var nativeType = graph.GetFullGraphName().ToType(false);
							if(nativeType != null && nativeType.IsCastableTo(typeof(MonoBehaviour))) {
								if(perform) {
									DoDropComponentType(nativeType, targets.FirstOrDefault());
								}
								else {

								}
								return DragAndDropVisualMode.Copy;
							}
						}
					}
				}
				else if(obj is IScriptGraphType scriptGraphType) {
					if(scriptGraphType is GraphAsset graph && graph.GetGraphName() == obj.name) {
						var nativeType = graph.GetFullGraphName().ToType(false);
						if(nativeType != null && nativeType.IsCastableTo(typeof(MonoBehaviour))) {
							if(perform) {
								DoDropComponentType(nativeType, targets.FirstOrDefault());
							}
							else {

							}
							return DragAndDropVisualMode.Copy;
						}
					}
				}
				else if(obj is GraphSingleton) {
					if(perform) {
						DoDropSingleton(obj as GraphSingleton, targets.FirstOrDefault());
					}
					return DragAndDropVisualMode.Copy;
				}
				return DragAndDropVisualMode.None;
			}

			if(targets.Length == 1 && (targets[0] is GameObject || targets[0] is Component)) {
				var references = DragAndDrop.objectReferences;
				if(references?.Length == 1) {
					return HandleDrag(references[0]);
				}
			}
			return DragAndDropVisualMode.None;
		}

		private static DragAndDropVisualMode HierarchyDropHandler(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform) {
			void DoDrop(string name, Func<GameObject, Component> addComponent) {
				var dropTarget = EditorUtility.InstanceIDToObject(dropTargetInstanceID) as GameObject;
				switch(dropMode) {
					case HierarchyDropFlags.DropBetween: {
						if(dropTarget != null) {
							var index = dropTarget.transform.GetSiblingIndex();
							var gameObject = new GameObject(name);
							Undo.RegisterCreatedObjectUndo(gameObject, "Create graph object");
							var comp = addComponent(gameObject);
							gameObject.transform.SetParent(dropTarget.transform.parent);
							gameObject.transform.SetSiblingIndex(index + 1);
						}
						else {
							goto default;
						}
						break;
					}
					case HierarchyDropFlags.DropAfterParent | HierarchyDropFlags.DropBetween | HierarchyDropFlags.DropAbove: {
						if(dropTarget != null) {
							var gameObject = new GameObject(name);
							Undo.RegisterCreatedObjectUndo(gameObject, "Create graph object");
							var comp = addComponent(gameObject);
							gameObject.transform.SetParent(dropTarget.transform.parent);
							gameObject.transform.SetAsFirstSibling();
						}
						else {
							goto default;
						}
						break;
					}
					default: {
						if(dropTarget != null) {
							var gameObject = dropTarget;
							addComponent(gameObject);
						}
						else {
							var gameObject = new GameObject(name);
							Undo.RegisterCreatedObjectUndo(gameObject, "Create graph object");
							addComponent(gameObject);
						}
						break;
					}
				}
			}

			DragAndDropVisualMode HandleDrag(UnityEngine.Object obj) {
				if(obj is ClassDefinition classDefinition) {
					if(classDefinition.InheritType.IsCastableTo(typeof(MonoBehaviour))) {
						if(perform) {
							DoDrop(classDefinition.GraphName, (go) => {
								var result = Undo.AddComponent<ClassComponent>(go);
								result.target = classDefinition;
								return result;
							});
						}
						else {
							//Debug.Log(dropMode);
						}
						return DragAndDropVisualMode.Copy;
					}
					return DragAndDropVisualMode.Rejected;
				}
				else if(obj is IScriptGraph scriptGraph) {
					foreach(var type in scriptGraph.TypeList) {
						if(type is GraphAsset graph && graph.GetGraphName() == obj.name) {
							var nativeType = graph.GetFullGraphName().ToType(false);
							if(nativeType != null && nativeType.IsCastableTo(typeof(MonoBehaviour))) {
								if(perform) {
									DoDrop(nativeType.Name, (go) => {
										var result = Undo.AddComponent(go, nativeType);
										return result;
									});
								}
								else {

								}
								return DragAndDropVisualMode.Copy;
							}
						}
					}
				}
				else if(obj is IScriptGraphType scriptGraphType) {
					if(scriptGraphType is GraphAsset graph && graph.GetGraphName() == obj.name) {
						var nativeType = graph.GetFullGraphName().ToType(false);
						if(nativeType != null && nativeType.IsCastableTo(typeof(MonoBehaviour))) {
							if(perform) {
								DoDrop(nativeType.Name, (go) => {
									var result = Undo.AddComponent(go, nativeType);
									return result;
								});
							}
							else {

							}
							return DragAndDropVisualMode.Copy;
						}
					}
				}
				else if(obj is GraphSingleton) {
					if(perform) {
						DoDrop((obj as GraphSingleton).GraphName, (go) => {
							var result = Undo.AddComponent<SingletonInitializer>(go);
							result.target = obj as GraphSingleton;
							return result;
						});
					}
					return DragAndDropVisualMode.Copy;
				}
				return DragAndDropVisualMode.None;
			}

			var references = DragAndDrop.objectReferences;
			if(references?.Length == 1) {
				return HandleDrag(references[0]);
			}
			return DragAndDropVisualMode.None;
		}

		private static void OnBeforeAssemblyReload() {
			if(Selection.activeObject is GraphAsset) {
				Selection.activeObject = null;
			}
			GenerationUtility.SaveData();
		}

		//[InitializeOnLoad]
		//class DomainReloadCallback : ScriptableObject {
		//	static DomainReloadCallback() {
		//		uNodeThreadUtility.Queue(() => {
		//			s_Instance = CreateInstance<DomainReloadCallback>();
		//		});
		//	}

		//	private static DomainReloadCallback s_Instance;
		//	private void OnDisable() {
		//		OnBeforeAssemblyReload();
		//	}
		//}

		private static void OnUnityQuitting() {
			GenerationUtility.SaveData();
		}

		static int refreshTime;

		static void Update() {
			#region Startup
			if(WelcomeWindow.IsShowOnStartup && EditorApplication.timeSinceStartup < 30) {
				WelcomeWindow.ShowWindow();
			}
			#endregion

			uNodeUtility.preferredDisplay = uNodePreference.preferenceData.displayKind;
			GraphDebug.transitionSpeed = uNodePreference.preferenceData.debugTransitionSpeed;

			if(System.DateTime.Now.Second > refreshTime || refreshTime > 60 && refreshTime - 60 >= System.DateTime.Now.Second) {
				UpdateMarkedObject();
				refreshTime = System.DateTime.Now.Second + 4;
			}
			uNodeUtility.isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
			uNodeThreadUtility.Update();

			if(!EditorApplication.isPaused) {
				GraphDebug.debugLinesTimer = Mathf.Repeat(GraphDebug.debugLinesTimer += 1.5f * uNodeThreadUtility.deltaTime, 1f);
			}

		}

		#region Project & Hierarchy
		static void UpdateMarkedObject() {
			if(uNodeIcon != null) {
				markedAssets.Clear();
				foreach(var guid in assetGUIDs) {
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
					if(asset is GameObject) {
						var go = asset as GameObject;
						var comp = go.GetComponent<IGraph>();
						if(comp != null) {
							markedAssets.Add(guid, comp as UnityEngine.Object);
						}
					}
					else if(asset is ICustomIcon) {
						markedAssets.Add(guid, asset);
					}
				}
				var scene = EditorSceneManager.GetActiveScene();
				if(scene != null) {
					var objects = scene.GetRootGameObjects();
					foreach(var obj in objects) {
						FindObjectToMark(obj.transform);
					}
				}
			}
		}

		static void FindObjectToMark(Transform transform) {
			if(transform.GetComponent<IRuntimeClass>() != null) {
				markedObjects.Add(transform.gameObject.GetInstanceID());
			}
			foreach(Transform t in transform) {
				FindObjectToMark(t);
			}
		}

		static GraphAsset draggedUNODE;
		static void HierarchyItem(int instanceID, Rect selectionRect) {
			//Show uNode Icon
			if(uNodeIcon != null) {
				Rect r = new Rect(selectionRect);
				r.x += r.width - 4;
				//r.x -= 5;
				r.width = 18;

				if(markedObjects.Contains(instanceID)) {
					GUI.Label(r, uNodeIcon);
				}
			}
			//HandleDragAndDropEvents();
			//if(Event.current.type == EventType.DragUpdated) {
			//	if(DragAndDrop.objectReferences?.Length == 1) {
			//		var obj = DragAndDrop.objectReferences[0];
			//		if(obj is GraphAsset graphAsset) {
			//			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			//			Event.current.Use();
			//		}
			//	}
			//}
			//Drag & Drop
			//if(Event.current.type == EventType.DragPerform) {
			//	if(DragAndDrop.objectReferences?.Length == 1) {
			//		var obj = DragAndDrop.objectReferences[0];
			//		if(obj is GraphAsset graphAsset) {
			//			//var comp = (obj as GameObject).GetComponent<uNodeRoot>();
			//			//if(comp is uNodeRuntime) {
			//			//	//if(EditorUtility.DisplayDialog("", "Do you want to Instantiate the Prefab or Spawn the graph?", "Prefab", "Graph")) {
			//			//	//	comp = null;
			//			//	//	PrefabUtility.InstantiatePrefab(comp);
			//			//	//	Event.current.Use();
			//			//	//}
			//			//	return;
			//			//}
			//			//if(comp != null && (comp is IClassComponent || comp is IGraphWithUnityEvent)) 
			//			{
			//				draggedUNODE = graphAsset;
			//				DragAndDrop.AcceptDrag();
			//				Event.current.Use();
			//				EditorApplication.delayCall += () => {
			//					if(draggedUNODE != null && draggedUNODE is ClassDefinition co && co.InheritType.IsCastableTo(typeof(MonoBehaviour))) {
			//						var gameObject = new GameObject(draggedUNODE.GetGraphName());
			//						var spawner = gameObject.AddComponent<ClassComponent>();
			//						spawner.target = co;
			//						Selection.objects = new Object[] { gameObject };
			//						draggedUNODE = null;
			//					}
			//				};
			//			}
			//		}
			//	}
			//}

			//if(draggedUNODE != null && draggedUNODE is ClassDefinition co && co.InheritType.IsCastableTo(typeof(MonoBehaviour))) {
			//	if(selectionRect.Contains(Event.current.mousePosition)) {
			//		var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			//		if(gameObject != null) {
			//			var spawner = gameObject.AddComponent<ClassComponent>();
			//			spawner.target = co;
			//			Selection.objects = new Object[] { gameObject };
			//			draggedUNODE = null;
			//		}
			//	}
			//}
		}

		private static void HandleDragAndDropEvents() {
			if(Event.current.type == EventType.DragUpdated) {
				if(DragAndDrop.objectReferences?.Length == 1) {
					var obj = DragAndDrop.objectReferences[0];
					if(obj is GraphAsset && !(obj is IIndependentGraph)) {
						Event.current.type = EventType.MouseDrag;
						DragAndDrop.PrepareStartDrag();
						DragAndDrop.objectReferences = new Object[0];
						DragAndDrop.StartDrag("Drag uNode");
						Event.current.Use();
					}
				}
			}
		}

		private static void OnSceneGUI(SceneView obj) {
			HandleDragAndDropEvents();
		}

		private static void ProjectItem(int instanceID, Rect rect) {
			HandleDragAndDropEvents();
			if(uNodeIcon == null)
				return;
			var obj = EditorUtility.InstanceIDToObject(instanceID);
			if(obj is not ICustomIcon && obj is not IScriptGraph && obj is not IIcon) {
				obj = null;
			}
			if(obj != null) {
				var isSmall = IsIconSmall(ref rect);
				if(obj is IIcon iicon) {
					if(iicon.GetIcon() != null) {
						DrawCustomIcon(rect, uNodeEditorUtility.GetTypeIcon(iicon.GetIcon()), isSmall);
					}
					else {
						DrawCustomIcon(rect, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.RuntimeTypeIcon)), isSmall);
					}
				}
				else if(obj is ICustomIcon customIcon) {
					if(customIcon.GetIcon() != null) {
						DrawCustomIcon(rect, customIcon.GetIcon(), isSmall);
					}
					else {
						DrawCustomIcon(rect, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.RuntimeTypeIcon)), isSmall);
					}
				}
				else if(obj is IClassGraph classGraph) {
					if(classGraph.InheritType == typeof(ValueType)) {
						DrawCustomIcon(rect, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.StructureIcon)), isSmall);
					}
					else {
						DrawCustomIcon(rect, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.ClassIcon)), isSmall);
					}
				}
				else if(obj is IScriptGraph) {
					Texture icon = null;
					if(icon == null) {
						icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.ClassIcon));
					}
					DrawCustomIcon(rect, icon, isSmall);
				}
				else {
					DrawCustomIcon(rect, uNodeIcon, isSmall);
				}
			}
		}

		private static void DrawCustomIcon(Rect rect, Texture texture, bool isSmall) {
			const float LARGE_ICON_SIZE = 128f;
			if(rect.width > LARGE_ICON_SIZE) {
				// center the icon if it is zoomed
				var offset = (rect.width - LARGE_ICON_SIZE) / 2f;
				rect = new Rect(rect.x + offset, rect.y + offset, LARGE_ICON_SIZE, LARGE_ICON_SIZE);
			}
			else {
				if(isSmall && !IsTreeView(rect))
					rect = new Rect(rect.x + 3, rect.y, rect.width, rect.height);
			}
			GUI.DrawTexture(rect, texture);
		}
		private static bool IsTreeView(Rect rect) {
			return (rect.x - 16) % 14 == 0;
		}

		private static bool IsIconSmall(ref Rect rect) {
			var isSmall = rect.width > rect.height;

			if(isSmall)
				rect.width = rect.height;
			else
				rect.height = rect.width;

			return isSmall;
		}
		#endregion

		private static Dictionary<int, HashSet<int>> _nodeDebugData;
		private static Dictionary<int, HashSet<int>> nodeDebugData {
			get {
				if(_nodeDebugData == null) {
					_nodeDebugData = uNodeEditorUtility.LoadEditorData<Dictionary<int, HashSet<int>>>("BreakpointsMap");
					if(_nodeDebugData == null) {
						_nodeDebugData = new Dictionary<int, HashSet<int>>();
					}
				}
				return _nodeDebugData;
			}
		}

		private static void SaveDebugData() {
			uNodeEditorUtility.SaveEditorData(_nodeDebugData, "BreakpointsMap");
		}

		#region AOT Scans
		public static bool AOTScan(out List<Type> serializedTypes) {
			return AOTScan(out serializedTypes, true, true, true, true, null);
		}

		public static bool AOTScan(out List<Type> serializedTypes, bool scanBuildScenes = true, bool scanAllAssetBundles = true, bool scanPreloadedAssets = true, bool scanResources = true, List<string> resourcesToScan = null) {
			using(AOTSupportScanner aOTSupportScanner = new AOTSupportScanner()) {
				aOTSupportScanner.BeginScan();
				if(scanBuildScenes && !aOTSupportScanner.ScanBuildScenes(includeSceneDependencies: true, showProgressBar: true)) {
					Debug.Log("Project scan canceled while scanning scenes and their dependencies.");
					serializedTypes = null;
					return false;
				}
				if(scanResources && !aOTSupportScanner.ScanAllResources(includeResourceDependencies: true, showProgressBar: true, resourcesToScan)) {
					Debug.Log("Project scan canceled while scanning resources and their dependencies.");
					serializedTypes = null;
					return false;
				}
				if(scanAllAssetBundles && !aOTSupportScanner.ScanAllAssetBundles(showProgressBar: true)) {
					Debug.Log("Project scan canceled while scanning asset bundles and their dependencies.");
					serializedTypes = null;
					return false;
				}
				if(scanPreloadedAssets && !aOTSupportScanner.ScanPreloadedAssets(showProgressBar: true)) {
					Debug.Log("Project scan canceled while scanning preloaded assets and their dependencies.");
					serializedTypes = null;
					return false;
				}
				aOTSupportScanner.GetType().GetField("allowRegisteringScannedTypes", MemberData.flags).SetValueOptimized(aOTSupportScanner, true);
				var graphTypes = ScanAOTOnGraphs();
				OnPreprocessBuild();
				var types = aOTSupportScanner.EndScan();
				foreach(var type in types) {
					graphTypes.Add(type);
				}
				foreach(var type in graphTypes.ToArray()) {
					if(type.IsCastableTo(typeof(IList))) {
						var elementType = type.ElementType();
						if(!elementType.IsInterface && !elementType.IsAbstract) {
							graphTypes.Add(elementType);
						}
					}
				}
				serializedTypes = graphTypes.ToList();
				for(int i = 0; i < serializedTypes.Count; i++) {
					if(EditorReflectionUtility.IsInEditorAssembly(serializedTypes[i])) {
						serializedTypes.RemoveAt(i);
						i--;
					}
				}
			}
			return true;
		}

		private static void AnalizeMemberData(MemberData member, HashSet<Type> types) {
			try {
				if(member.isTargeted) {
					object mVal;
					if(member.targetType.IsTargetingValue()) {
						mVal = member.Get(null);
					}
					else {
						mVal = member.instance;
					}
					if(mVal != null && !(mVal is Object)) {
						if(mVal is MemberData mData) {
							AnalizeMemberData(mData, types);
						}
						else if(!types.Contains(mVal.GetType())) {
							types.Add(mVal.GetType());
							SerializerUtility.Serialize(mVal);
						}
					}
				}
			}
			catch { }
		}

		private static HashSet<Type> ScanAOTOnGraphs() {
			HashSet<Type> serializedTypes = new HashSet<Type>();
			List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

			objects.AddRange(GraphUtility.FindAllGraphAssets());
			Action<object> analyzer = (param) => {
				EditorReflectionUtility.AnalizeSerializedObject(param, (fieldObj) => {
					if(fieldObj is ISerializationCallbackReceiver serialization) {
						serialization.OnBeforeSerialize();
					}
					return false;
				});
			};
			foreach(var obj in objects) {
				if(obj is GameObject) {
					var scripts = (obj as GameObject).GetComponentsInChildren<MonoBehaviour>(true);
					foreach(var script in scripts) {
						if(script is ISerializationCallbackReceiver serialization) {
							serialization.OnBeforeSerialize();
						}
						//if(script is IVariableSystem VS && VS.Variables != null) {
						//	foreach(var var in VS.Variables) {
						//		var.Serialize();
						//	}
						//}
						//if(script is ILocalVariableSystem IVS && IVS.LocalVariables != null) {
						//	foreach(var var in IVS.LocalVariables) {
						//		var.Serialize();
						//	}
						//}
						analyzer(script);
					}
				}
				else if(obj is ISerializationCallbackReceiver) {
					(obj as ISerializationCallbackReceiver).OnBeforeSerialize();
				}
				else {
					analyzer(obj);
				}
			}
			SerializerUtility.Serialize(new MemberData());
			return serializedTypes;
		}
		#endregion

		#region Build Processor
		//private static bool isEditorOpen;
		private static bool hasRunPreBuild;

		public static void OnPreprocessBuild() {
			if(hasRunPreBuild)
				return;
#if UNODE_TRIM_ON_BUILD
			uNodeUtility.trimmedObjects = new HashSet<Object>();
#endif
			hasRunPreBuild = true;
			GraphUtility.UpdateDatabase();
#if UNODE_TRIM_ON_BUILD && UNODE_TRIM_AGGRESSIVE && UNODE_PRO
			{
				var db = uNodeUtility.GetDatabase();
				db.nativeGraphDatabases.Clear();
				for(int i = 0; i < db.graphDatabases.Count; i++) {
					var tmp = db.graphDatabases[i];
					if(tmp.asset is IScriptGraphType || tmp.asset is IMacroGraph) {
						db.graphDatabases.RemoveAt(i);
						i--;
						continue;
					}
				}
			}
#endif
			try {
				GraphUtility.CreateBackup(GraphUtility.FindAllGraphAssets().Select(item => AssetDatabase.GetAssetPath(item)), "GraphBeforeBuild");
			}
			catch(Exception ex) {
				Debug.LogException(ex);
			}
			GraphUtility.SaveAllGraph();
			if(uNodePreference.preferenceData.generatorData.autoGenerateOnBuild) {
				GenerationUtility.CompileProjectGraphs();
				while(uNodeThreadUtility.IsNeedUpdate()) {
					uNodeThreadUtility.Update();
				}
				//if(uNodeEditor.window != null) {
				//	uNodeEditor.window.Close();
				//	isEditorOpen = true;
				//}
			}
		}

		public static void OnPostprocessBuild() {
			//if(isEditorOpen) {
			//	uNodeThreadUtility.ExecuteAfter(5, () => {
			//		uNodeEditor.ShowWindow();
			//	});
			//	isEditorOpen = false;
			//}
			hasRunPreBuild = false;
#if UNODE_TRIM_ON_BUILD && UNODE_PRO
			uNodeThreadUtility.Queue(() => {
				//Debug.Log(uNodeUtility.trimmedObjects.Count);
				foreach(var asset in uNodeUtility.trimmedObjects) {
					//Debug.Log("Presist object:" + UnityEditor.AssetDatabase.GetAssetPath(asset));
					if(asset is GraphAsset) {
						var graph = asset as GraphAsset;
						graph.serializedGraph.SerializeGraph();
					}
					EditorUtility.SetDirty(asset);
				}
				GraphUtility.SaveAllGraph();
#if UNODE_TRIM_AGGRESSIVE
				GraphUtility.UpdateDatabase();
#endif
			});
#endif
		}
		#endregion
	}

	static class uNodeAssetHandler {
		[OnOpenAsset(int.MinValue)]
		public static bool OpenEditor(int instanceID, int line) {
			Object obj = EditorUtility.InstanceIDToObject(instanceID);
			//if(obj is GameObject) {
			//	GameObject go = obj as GameObject;
			//	var graph = go.GetComponent<IGraph>();
			//	if(graph != null) {
			//		uNodeEditor.Open(graph);
			//		return true; //comment this to allow editing prefab.
			//	}
			//} else 
			if(obj is GraphAsset) {
				uNodeEditor.Open(obj as IGraph);
				return true;
			}
			else if(obj is IScriptGraph) {
				uNodeEditor.Open(obj as IScriptGraph);
				return true;
			}
			return false;
		}
	}

	class uNodeAssetModificationProcessor : AssetModificationProcessor {
		private const string k_FileExtension = ".asset";

		public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options) {
			if(Path.GetExtension(path) == k_FileExtension) {
				var type = AssetDatabase.GetMainAssetTypeAtPath(path);
				if(type != null && (type.IsCastableTo(typeof(IGraph)) || type.IsCastableTo(typeof(IScriptGraph)))) {
					uNodeDatabase.ClearCache();
				}
			}

			return default;
		}
	}
}