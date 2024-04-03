using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace MaxyGames.UNode.Editors {
	#region Classes
	/// <summary>
	/// A dragable manipulator that uses <see cref="DragAndDrop"/> to handle dragging.
	/// </summary>
	public class DraggableElementManipulator : MouseManipulator {
		public const string s_DragDataType = "DraggableElement";

		public string dragTitle;
		public Func<Dictionary<string, object>> getGenericData;
		public Func<UnityEngine.Object[]> getObjectReferences;
		public Action onStartDragging;
		public Action onStopDragging;

		enum DragState {
			AtRest,
			Ready,
			Dragging
		}

		private DragState m_DragState = DragState.AtRest;
		private IEventHandler m_DraggedElement;

		public DraggableElementManipulator() {
			activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
		}

		void OnMouseDownEvent(MouseDownEvent e) {
			if(e.button == 0) {
				m_DragState = DragState.Ready;
				m_DraggedElement = e.currentTarget;
			}
		}

		void OnMouseMoveEvent(MouseMoveEvent e) {
			if(m_DragState == DragState.Ready && m_DraggedElement == e.currentTarget) {
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.SetGenericData(s_DragDataType, this);
				var datas = getGenericData?.Invoke();
				if(datas != null) {
					foreach(var (type, data) in datas) {
						DragAndDrop.SetGenericData(type, data);
					}
				}
				var references = getObjectReferences?.Invoke();
				if(references != null) {
					DragAndDrop.objectReferences = references;
				}
				DragAndDrop.StartDrag(dragTitle ?? "Drag uNode");
				m_DragState = DragState.Dragging;
				onStartDragging?.Invoke();
				target.ReleaseMouse();
			}
		}

		void OnMouseUpEvent(MouseUpEvent e) {
			if(m_DragState == DragState.Ready && e.button == 0) {
				m_DraggedElement = null;
				m_DragState = DragState.AtRest;
				onStopDragging?.Invoke();
				target.ReleaseMouse();
			}
		}

		protected override void RegisterCallbacksOnTarget() {
			target.RegisterCallback<MouseDownEvent>(OnMouseDownEvent, TrickleDown.TrickleDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
			target.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<MouseDownEvent>(OnMouseDownEvent, TrickleDown.TrickleDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
		}
	}

	/// <summary>
	/// Generic dropable element manipulator that receive drag and drop events
	/// </summary>
	public class DropableElementManipulator : Manipulator {
		public Action<DragPerformEvent> onDragPerform;
		public Action<DragUpdatedEvent> onDragUpdate;
		public Action<DragEnterEvent> onDragEnter;
		public Action<DragLeaveEvent> onDragLeave;
		public Action<DragExitedEvent> onDragExited;

		private void OnDragPerformEvent(DragPerformEvent evt) {
			if(onDragPerform != null) {
				onDragPerform(evt);
				evt.StopImmediatePropagation();
			}
		}

		private void OnDragUpdatedEvent(DragUpdatedEvent evt) {
			if(onDragUpdate != null) {
				onDragUpdate(evt);
				evt.StopImmediatePropagation();
			}
		}

		private void OnDragEnterEvent(DragEnterEvent evt) {
			if(onDragEnter != null) {
				onDragEnter(evt);
				evt.StopImmediatePropagation();
			}
		}

		private void OnDragLeaveEvent(DragLeaveEvent evt) {
			if(onDragLeave != null) {
				onDragLeave(evt);
				evt.StopImmediatePropagation();
			}
		}

		private void OnDragExitedEvent(DragExitedEvent evt) {
			if(onDragExited != null) {
				onDragExited(evt);
				evt.StopImmediatePropagation();
			}
		}

		protected override void RegisterCallbacksOnTarget() {
			target.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
			target.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
			target.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
			target.RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
			target.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
		}

		protected override void UnregisterCallbacksFromTarget() {
			target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
			target.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
			target.UnregisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
			target.UnregisterCallback<DragEnterEvent>(OnDragEnterEvent);
			target.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
		}
	}

	/// <summary>
	/// A clickable element that can have icon, label, click event, and context menu.
	/// </summary>
	public class ClickableElement : VisualElement {
		public Clickable clickable { get; set; }

		public Action<EventBase> onClick;
		public Label label;
		public Image icon;
		private Image breadcrumb;
		public DropdownMenu menu;

		public ClickableElement() {
			Init("");
		}

		public ClickableElement(Action onClick) {
			this.onClick = _ => onClick?.Invoke();
			Init("");
		}

		public ClickableElement(Action<EventBase> onClick) {
			this.onClick = onClick;
			Init("");
		}

		public ClickableElement(string text) {
			Init(text);
		}

		public ClickableElement(string text, Action onClick) {
			this.onClick = _ => onClick?.Invoke();
			Init(text);
		}

		public ClickableElement(string text, Action<EventBase> onClick) {
			this.onClick = onClick;
			Init(text);
		}

		protected void Init(string text) {
			clickable = new Clickable((evt) => {
				if(onClick != null) {
					onClick(evt);
				}
				this.ShowMenu(menu);
			});
			this.AddManipulator(clickable);
			style.flexDirection = FlexDirection.Row;

			label = new Label(text);
			label.pickingMode = PickingMode.Ignore;
			Add(label);
		}

		public void ShowIcon(Texture icon) {
			if(this.icon == null) {
				this.icon = new Image() {
					name = "icon"
				};
				this.icon.pickingMode = PickingMode.Ignore;
				Insert(0, this.icon);
			}
			if(icon != null)
				this.icon.image = icon;
		}

		public void EnableBreadcrumb(bool enable) {
			if(breadcrumb == null) {
				breadcrumb = new Image() {
					name = "breadcrumb"
				};
				Add(breadcrumb);
			}
			if(enable) {
				breadcrumb.ShowElement();
			} else {
				breadcrumb.HideElement();
			}
		}
	}
	#endregion

	public class UIElementGraph : NodeGraph {
		public UGraphView graphView;
		[NonSerialized]
		public VisualElement graphContentView, graphRootView;
		[NonSerialized]
		public VisualElement toolbarView;
		[NonSerialized]
		public VisualElement rootContainer;
		[NonSerialized]
		public VisualElement rootContent;
		[NonSerialized]
		public VisualElement graphPanelView;
		[NonSerialized]
		public VisualElement inspectorPanel;
		[NonSerialized]
		public VisualElement tabbarContainer;
		[NonSerialized]
		public Toolbar statusContainerView;

		private GraphPanel graphPanel;

		private ToolbarMenu zoomStatus;
		private ToolbarButton errorStatus;
		private Label errorLabel;
		private Image errorImage;
		private ToolbarButton debugButton;
		private ToolbarButton saveButton;
		private ToolbarButton frameButton;

		public static bool richText {
			get {
				var theme = UIElementUtility.Theme;
				return theme != null ? theme.textSettings.useRichText : true;
			}
		}

		private bool hasInitialize;

		public override void OnEnable() {
			InitToolbar();
			InitializeRootView();
			InitializeGraph();
		}

		private string GetDebugName() {
			string names = "None";

			if(!GraphDebug.useDebug) {
				names = "Disable";
			}
			else if(debugTarget != null) {
				if(debugTarget is UnityEngine.Object) {
					if(debugTarget == graphData.graph && graphData.graph is IInstancedGraph instanced) {
						if(instanced.OriginalGraph == graphData.graph) {
							names = "self";
						}
						else {
							names = "@" + graphData.graph.GetGraphName();
						}
					}
					else if(graphData.debugAnyScript) {
						names = "Auto: " + uNodeUtility.GetObjectName(debugTarget);
					}
					else {
						names = uNodeUtility.GetObjectName(debugTarget);
					}
				}
				else if(debugTarget is Type) {
					if(graphData.debugAnyScript) {
						names = "Auto: Static";
					}
					else {
						names = "Static: " + (debugTarget as Type).PrettyName();
					}
				}
				else if(graphData.debugAnyScript) {
					names = "Auto";
				}
				else {
					names = uNodeUtility.GetObjectName(debugTarget);
				}
			}
			else if(graphData.debugAnyScript) {
				if(graphData.graph is IInstancedGraph instanced) {
					if(instanced.OriginalGraph == graphData.graph) {
						names = "self";
					}
					else {
						names = "@" + graphData.graph.GetGraphName();
					}
					//if(runtime.runtimeBehaviour == null && Application.isPlaying) {
					//	debugObject = runtime;
					//}
				}
				else {
					names = "Auto";
				}
			}
			//if(graphData.graph is not IInstancedGraph) {
			//	names = "@" + graphData.graph.GetGraphName();
			//}
			return "Debug: " + names;
		}

		private void InitToolbar() {
			if(toolbarView != null && toolbarView.parent != null) {
				return;
			}
			var toolbar = new Toolbar() {
				name = "toolbar",
			};
			toolbar.AddStyleSheet("uNodeStyles/NativeGraphStyle");
			toolbarView = toolbar;
			window.rootVisualElement.Add(toolbar);
			var leftVisibilityBtn = new ToolbarButton() {
				text = uNodeEditor.SavedData.leftVisibility ? "<<" : ">>",
				tooltip = "Show or hide sidebar ( left panel )",
			};
			leftVisibilityBtn.clickable = new Clickable(() => {
				uNodeEditor.SavedData.leftVisibility = !uNodeEditor.SavedData.leftVisibility;
				leftVisibilityBtn.text = uNodeEditor.SavedData.leftVisibility ? "<<" : ">>";
			});
			toolbar.Add(leftVisibilityBtn);

			#region Debug
			debugButton = new ToolbarButton() {
				text = GetDebugName(),
				clickable = new Clickable(evt => {
					if(evt is MouseUpEvent || evt is PointerUpEvent) {
						GenericMenu menu = new GenericMenu();
						menu.AddDisabledItem(new GUIContent("Script Debugger"), false);
						menu.AddSeparator("");
						if(graphData.graph is IScriptGraphType scriptGraph) {
							var data = scriptGraph.ScriptTypeData?.scriptGraph?.ScriptData;
							if(data != null) {
								menu.AddItem(new GUIContent("Debug Mode " + (data.debug ? " (Enabled) " : " (Disabled) ")), data.debug, delegate () {
									data.debug = !data.debug;
								});
								if(data.debug) {
									menu.AddItem(new GUIContent("Debug Value" + (data.debugValueNode ? " (Enabled) " : " (Disabled) ")), data.debugValueNode, delegate () {
										data.debugValueNode = !data.debugValueNode;
									});
								}
							}
						} else if(graphData.graph is IGraphWithScriptData graph) {
							menu.AddItem(new GUIContent("Debug Mode " + (graph.ScriptData.debug ? " (Enabled) " : " (Disabled) ")), graph.ScriptData.debug, delegate () {
								graph.ScriptData.debug = !graph.ScriptData.debug;
							});
							if(graph.ScriptData.debug) {
								menu.AddItem(new GUIContent("Debug Value" + (graph.ScriptData.debugValueNode ? " (Enabled) " : " (Disabled) ")), graph.ScriptData.debugValueNode, delegate () {
									graph.ScriptData.debugValueNode = !graph.ScriptData.debugValueNode;
								});
							}
						}
						menu.AddSeparator("");
						menu.AddDisabledItem(new GUIContent("Debug References"), false);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("None"), false, delegate () {
							debugTarget = null;
							graphData.debugSelf = false;
							graphData.debugAnyScript = false;
							GraphDebug.useDebug = true;
						});
						menu.AddItem(new GUIContent("Disable"), GraphDebug.useDebug == false, delegate () {
							GraphDebug.useDebug = !GraphDebug.useDebug;
						});
						menu.AddItem(new GUIContent("Auto"), graphData.debugAnyScript, delegate () {
							debugTarget = null;
							graphData.debugAnyScript = true;
							GraphDebug.useDebug = true;
						});
						//if(graphData.graph is IInstancedGraph instanced && instanced.OriginalGraph == graphData.graph) {
						//	menu.AddItem(new GUIContent("Self"), false, delegate () {
						//		debugTarget = null;
						//		GraphDebug.useDebug = true;
						//		if(graphData.graph is IInstancedGraph instancedGraph && instancedGraph.OriginalGraph != null) {
						//			uNodeEditor.Open(instancedGraph.OriginalGraph);
						//		}
						//	});
						//}
						if(graphData.graph != null) {
							if(Application.isPlaying) {
								HashSet<object> instances = new HashSet<object>(32);
								Action staticAction = null;
								void FindInstance(System.Runtime.CompilerServices.ConditionalWeakTable<object, GraphDebug.DebugData> debugMap) {
									foreach(var pair in debugMap) {
										if(instances.Count > 250)
											break;
										var debugObject = pair.Key;
										if(debugObject != null && debugObject != graphData.graph) {
											if(debugObject is Type) {
												//Static Debugging
												var type = debugObject as Type;
												if(type.FullName == graphData.graph.GetFullGraphName()) {
													staticAction += () => {
														menu.AddItem(new GUIContent("Static: " + type.PrettyName()), debugTarget == debugObject, delegate (object reference) {
															KeyValuePair<object, GraphDebug.DebugData> objPair = (KeyValuePair<object, GraphDebug.DebugData>)reference;
															debugTarget = objPair.Key;
															GraphDebug.useDebug = true;
														}, pair);
													};
												}
												continue;
											}
											if(debugObject is UnityEngine.Object && (debugObject as UnityEngine.Object) == null)
												continue;
											if(debugObject is IInstancedGraph) {
												if(uNodeUtility.IsCastableGraph((debugObject as IInstancedGraph).OriginalGraph, graphData.graph) == false)
													continue;
											}
											else if(debugObject.GetType().FullName != graphData.graph.GetFullGraphName()) {
												continue;
											}
											string niceName = uNodeUtility.GetObjectName(pair.Key);
											if(instances.Add(pair.Key)) {
												menu.AddItem(new GUIContent("Instances/" + instances.Count + "-" + niceName), debugTarget == debugObject, delegate (object reference) {
													KeyValuePair<object, GraphDebug.DebugData> objPair = (KeyValuePair<object, GraphDebug.DebugData>)reference;
													UnityEngine.Object o = objPair.Key as UnityEngine.Object;
													if(o != null) {
														EditorGUIUtility.PingObject(o);
													}
													debugTarget = objPair.Key;
													GraphDebug.useDebug = true;
												}, pair);
											}
										}
									}
								}
								if(GraphDebug.debugData.ContainsKey(graphData.graph.GetGraphID())) {
									FindInstance(GraphDebug.debugData[graphData.graph.GetGraphID()]);
								}

								if(graphData.graph is IReflectionType) {
									var db = uNodeDatabase.instance;
									foreach(var (id, map) in GraphDebug.debugData) {
										var other = db.graphDatabases.FirstOrDefault(data => data.fileUniqueID == id)?.asset;
										if(other != null) {
											IGraph inherited = other;
											while(true) {
												inherited = RuntimeGraphUtility.GetInheritedGraph(inherited);
												if(inherited == null)
													break;
												if(inherited == graphData.graph) {
													FindInstance(map);
													break;
												}
											}
										}
									}
								}
								if(staticAction != null) {
									staticAction();
								}
							}

							var objs = GameObject.FindObjectsOfType<MonoBehaviour>();
							int counts = 0;
							foreach(var obj in objs) {
								if(counts > 250)
									break;
								if(obj is IInstancedGraph instancedGraph) {
									if(uNodeUtility.IsCastableGraph(instancedGraph.OriginalGraph, graphData.graph)) {
										menu.AddItem(new GUIContent("Scene/" + counts + "-" + obj.gameObject.name), debugTarget == obj as object, () => {
											if(obj != null) {
												EditorGUIUtility.PingObject(obj);
											}
											debugTarget = obj;
											GraphDebug.useDebug = true;
											//uNodeEditor.Open(instancedGraph.OriginalGraph);
										});
										counts++;
									}
								}
							}
						}
						menu.ShowAsContext();
					}
				})
			};
			toolbar.Add(debugButton);
			#endregion

			frameButton = new ToolbarButton(() => {
				FrameGraph();
			}) {
				text = "Frame Graph",
				tooltip = "Frame the graph\nHotkey: F",
			};
			toolbar.Add(frameButton);

			saveButton = new ToolbarButton(() => {
				GraphUtility.SaveAllGraph();
			}) {
				text = "Save",
				tooltip = "Write all unsaved graph assets to disk.",
			};
			toolbar.Add(saveButton);

			toolbar.Add(new ToolbarSpacer() { flex = true });

			var previewBtn = new ToolbarButton() {
				text = "Preview",
				tooltip = "Preview C# Script\nHotkey: F9",
				clickable = new Clickable(() => {
					window?.PreviewSource();
				})
			};
			toolbar.Add(previewBtn);

			var compileBtn = new ToolbarButton() {
				text = "Compile",
				tooltip = "Generate C# Script\nHotkey: F10 ( compile current graph )",
				clickable = new Clickable(Compile)
			};
			toolbar.Add(compileBtn);

			var selectBtn = new ToolbarButton(() => {
				if(tabData.owner != null) {
					EditorGUIUtility.PingObject(tabData.owner);
					//Selection.instanceIDs = new int[] { editorData.owner.GetInstanceID() };
				}
			}) {
				text = "Select",
				tooltip = "Select graph asset",
			};
			toolbar.Add(selectBtn);

			var refreshBtn = new ToolbarButton(() => {
				window?.Refresh(true);
			}) {
				text = "Refresh",
				tooltip = "Refresh the graph.\nHotkey: F5",
			};
			toolbar.Add(refreshBtn);

			var inspectorBtn = new ToolbarToggle() {
				text = "Inspector",
				tooltip = "View to edit selected node, variable, function, etc",
			};
			inspectorBtn.SetValueWithoutNotify(uNodeEditor.SavedData.rightVisibility);
			inspectorBtn.RegisterValueChangedCallback(evt => {
				uNodeEditor.SavedData.rightVisibility = evt.newValue;
			});
			toolbar.Add(inspectorBtn);

			var moreToolBtn = new ToolbarButton() {
				text = "☰",
				clickable = new Clickable(evt => {
					if(evt is MouseUpEvent || evt is PointerUpEvent) {
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Preference Editor"), false, () => {
							ActionWindow.ShowWindow(() => {
								uNodePreference.PreferencesGUI();
							});
						});
						menu.AddItem(new GUIContent("Node Display/Default"), uNodePreference.preferenceData.displayKind == DisplayKind.Default, () => {
							uNodePreference.preferenceData.displayKind = DisplayKind.Default;
							uNodePreference.SavePreference();
							uNodeGUIUtility.GUIChangedMajor(null);
						});
						menu.AddItem(new GUIContent("Node Display/Partial"), uNodePreference.preferenceData.displayKind == DisplayKind.Partial, () => {
							uNodePreference.preferenceData.displayKind = DisplayKind.Partial;
							uNodePreference.SavePreference();
							uNodeGUIUtility.GUIChangedMajor(null);
						});
						menu.AddItem(new GUIContent("Node Display/Full"), uNodePreference.preferenceData.displayKind == DisplayKind.Full, () => {
							uNodePreference.preferenceData.displayKind = DisplayKind.Full;
							uNodePreference.SavePreference();
							uNodeGUIUtility.GUIChangedMajor(null);
						});
						menu.AddSeparator("");
						//menu.AddItem(new GUIContent("Code Generator Options"), false, () => {
						//	ActionWindow.ShowWindow(() => {
						//		ShowGenerateCSharpGUI();
						//	});
						//});
						//menu.AddItem(new GUIContent("Graph Explorer"), false, () => {
						//	ExplorerWindow.ShowWindow();
						//});
						menu.AddItem(new GUIContent("Global Search"), false, () => {
							uNodeEditorUtility.ShowGlobalSearch();
						});
						menu.AddItem(new GUIContent("Graph Hierarchy"), false, () => {
							uNodeEditorUtility.ShowGraphHierarchy();
						});
						menu.AddItem(new GUIContent("Node Browser"), false, () => {
							uNodeEditorUtility.ShowNodeBrowser();
						});
						//menu.AddSeparator("");
						//menu.AddItem(new GUIContent("Import"), false, () => {
						//	ActionWindow.ShowWindow(() => {
						//		ShowImportGUI();
						//	});
						//});
						//menu.AddItem(new GUIContent("Export"), false, () => {
						//	ActionWindow.ShowWindow(() => {
						//		ShowExportGUI();
						//	});
						//});
						menu.AddSeparator("");
						//menu.AddItem(new GUIContent("Fix missing members"), false, () => {
						//	RefactorWindow.Refactor(editorData.graph);
						//});
						menu.AddItem(new GUIContent("Refresh All Graphs"), false, () => {
							uNodeEditor.ClearCache();
						});
						menu.AddItem(new GUIContent("Check All Graph Errors"), false, () => {
							GraphUtility.ErrorChecker.CheckGraphErrors();
						});
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Show All Breakpoints"), false, () => {
							GraphUtility.ShowNodeUsages(node => {
								return GraphDebug.Breakpoint.HasBreakpoint(node);
							});
						});
						menu.AddItem(new GUIContent("Remove All Breakpoints"), false, () => {
							GraphDebug.Breakpoint.ClearBreakpoints();
						});
						menu.ShowAsContext();
					}
				}),
			};
			toolbar.Add(moreToolBtn);
			UIElementUtility.ForceDarkToolbarStyleSheet(toolbar);
		}

		public override void OnDisable() {
			graphPanel?.RemoveFromHierarchy();
			graphPanel = null;
			toolbarView?.RemoveFromHierarchy();
			toolbarView = null;
			tabbarContainer?.RemoveFromHierarchy();
			tabbarContainer = null;
			graphView?.RemoveFromHierarchy();
			graphView = null;
			mainGUIContainer?.RemoveFromHierarchy();
			mainGUIContainer = null;
			statusContainerView?.RemoveFromHierarchy();
			statusContainerView = null;
			graphContentView?.RemoveFromHierarchy();
			graphContentView = null;
			rootContainer?.RemoveFromHierarchy();
			rootContainer = null;
			OnNoTarget();
		}

		public override void OnNoTarget() {
			if(graphContentView != null) {
				graphContentView.RemoveFromHierarchy();
				graphContentView = null;
			}
			statusContainerView?.RemoveFromHierarchy();
		}

		bool _isTabbarMarkedReload;
		public void MarkReloadTabbar() {
			if(!_isTabbarMarkedReload) {
				_isTabbarMarkedReload = true;
				uNodeThreadUtility.Queue(() => {
					_isTabbarMarkedReload = false;
					ReloadTabbar();
				});
			}
		}

		private void InitTabbar(VisualElement container) {
			#region Main/Selection Tab
			if(window.mainTab != null && window.mainTab == window.selectedTab && !window.mainTab.selectedGraphData.isValidGraph) {
				window.mainTab.owner = null;
				window.mainTab.graph = null;
				window.mainTab.selectedGraphData = new GraphEditorData();
			}
			var tabMainElement = new ClickableElement("\"Main\"") {
				name = "tab-element",
				onClick = (_) => {
					window.ChangeEditorTarget(null);
				},
			};
			tabMainElement.AddManipulator(new ContextualMenuManipulator((evt) => {
				evt.menu.AppendAction("Close All But This", (act) => {
					window.tabDatas.Clear();
					window.ChangeEditorTarget(null);
					ReloadTabbar();
				}, DropdownMenuAction.AlwaysEnabled);
				evt.menu.AppendSeparator("");
				evt.menu.AppendAction("Find Object", (act) => {
					EditorGUIUtility.PingObject(window.mainTab.owner);
					ReloadTabbar();
				}, DropdownMenuAction.AlwaysEnabled);
				evt.menu.AppendAction("Select Object", (act) => {
					EditorGUIUtility.PingObject(window.mainTab.owner);
					Selection.instanceIDs = new int[] { window.mainTab.owner.GetInstanceID() };
					ReloadTabbar();
				}, DropdownMenuAction.AlwaysEnabled);
			}));
			if(window.selectedTab == window.mainTab) {
				tabMainElement.AddToClassList("tab-selected");
			}
			container.Add(tabMainElement);
			#endregion

			for(int i = 0; i < window.tabDatas.Count; i++) {
				var tabData = window.tabDatas[i];
				try {
					if(tabData == null) {
						window.tabDatas.RemoveAt(i);
						i--;
						continue;
					}
					else if(tabData.owner == null) {
						if(!object.ReferenceEquals(tabData.owner, null) &&
							EditorUtility.InstanceIDToObject(tabData.owner.GetInstanceID()) is UnityEngine.Object unityObject &&
							unityObject is IGraph graph) {

							var newTabData = new uNodeEditor.TabData(unityObject);

							var graphData = new GraphEditorData(unityObject);
							newTabData.graphDatas.Add(graphData);
							newTabData.selectedGraphData = graphData;
							if(tabData.selectedGraphData?.currentCanvas is UGraphElement oldCanvas && graph.GraphData.GetElementByID(oldCanvas.id) is UGraphElement newCanvas) {
								graphData.currentCanvas = newCanvas;
								graphData.position = tabData.selectedGraphData.position;
							}

							if(window.selectedTab == tabData) {
								tabData = newTabData;
								window.tabDatas[i] = tabData;
								window.Refresh(true);
							}
							else {
								tabData = newTabData;
								window.tabDatas[i] = tabData;
							}
						}
						else {
							window.tabDatas.RemoveAt(i);
							i--;
							continue;
						}
					}
					else {
						tabData.RemoveIncorrectDatas();
					}
				}
				catch(Exception ex) {
					Debug.LogException(ex);
					//window.tabDatas.RemoveAt(i);
					//i--;
					continue;
				}
				var tabElement = new ClickableElement(tabData.displayName) {
					name = "tab-element",
					onClick = (_) => {
						window.ChangeEditorTarget(tabData);
					},
				};
				tabElement.RemoveManipulator(tabElement.clickable);
				tabElement.ShowIcon(uNodeEditorUtility.GetTypeIcon(tabData.owner));

				tabElement.AddManipulator(new DraggableElementManipulator() {
					dragTitle = "Drag Tab: " + tabData.displayName,
					getGenericData = () => {
						return new Dictionary<string, object>() {
							{ TabDragKEY, tabData }
						};
					},
					getObjectReferences = () => {
						return new[] { tabData.owner };
					}
				});
				tabElement.AddManipulator(tabElement.clickable);
				tabElement.AddManipulator(new DropableElementManipulator() {
					onDragUpdate = (evt) => {
						if(evt.currentTarget == tabElement) {
							var tab = DragAndDrop.GetGenericData(TabDragKEY) as uNodeEditor.TabData;
							if(tab != null) {
								DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
							}
						}
					},
					onDragLeave = (evt) => {
						var tab = DragAndDrop.GetGenericData(TabDragKEY) as uNodeEditor.TabData;
						if(tab != null) {
							DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
						}
					},
					onDragPerform = (evt) => {
						var tab = DragAndDrop.GetGenericData(TabDragKEY) as uNodeEditor.TabData;
						if(tab != null) {
							var tabIndex = window.tabDatas.IndexOf(tab);
							var currentIndex = window.tabDatas.IndexOf(tabData);
							var selectedTab = window.selectedTab;
							window.tabDatas[tabIndex] = tabData;
							window.tabDatas[currentIndex] = tab;
							window.selectedTab = selectedTab;
							ReloadTabbar();
						}
					},
				});
				tabElement.AddManipulator(new ContextualMenuManipulator((evt) => {
					evt.menu.AppendAction("Close", (act) => {
						var oldData = window.selectedTab;
						window.tabDatas.Remove(tabData);
						window.SaveEditorData();
						window.ChangeEditorTarget(oldData);
						ReloadTabbar();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Close All", (act) => {
						window.tabDatas.Clear();
						window.SaveEditorData();
						window.ChangeEditorTarget(null);
						ReloadTabbar();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Close Others", (act) => {
						var current = window.selectedTab;
						window.tabDatas.Clear();
						window.tabDatas.Add(current);
						window.SaveEditorData();
						window.ChangeEditorTarget(current);
						ReloadTabbar();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Find Object", (act) => {
						EditorGUIUtility.PingObject(tabData.owner);
						ReloadTabbar();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Select Object", (act) => {
						EditorGUIUtility.PingObject(tabData.owner);
						Selection.instanceIDs = new int[] { tabData.owner.GetInstanceID() };
						ReloadTabbar();
					}, DropdownMenuAction.AlwaysEnabled);
				}));
				if(window.selectedTab == tabData) {
					tabElement.AddToClassList("tab-selected");
				}
				container.Add(tabElement);
			}

			#region Plus Tab
			{
				var plusElement = new ClickableElement("+") {
					name = "tab-element",
				};
				{
					plusElement.menu = new DropdownMenu();
					plusElement.menu.AppendAction("Open...", (act) => {
						//window.OpenNewGraphTab();
						ReloadTabbar();
					});

					#region Recent Files
					List<UnityEngine.Object> lastOpenedObjects = uNodeEditor.FindLastOpenedGraphs();
					for(int i = 0; i < lastOpenedObjects.Count; i++) {
						var obj = lastOpenedObjects[i];
						if(obj is IGraph) {
							var root = obj as IGraph;
							plusElement.menu.AppendAction("Open Recent/" + root.GetGraphName(), (act) => {
								uNodeEditor.Open(root);
							});
						}
					}
					if(lastOpenedObjects.Count > 0) {
						plusElement.menu.AppendSeparator("Open Recent/");
						plusElement.menu.AppendAction("Open Recent/Clear Recent", (act) => {
							uNodeEditor.ClearLastOpenedGraphs();
						});
					}
					#endregion

					plusElement.menu.AppendSeparator("");
					plusElement.menu.AppendAction("Create New Graph...", (act) => {
						GraphCreatorWindow.ShowWindow();
					});
				}
				container.Add(plusElement);
			}
			#endregion
		}

		private void ReloadTabbar() {
			if(tabbarContainer != null) {
				tabbarContainer.RemoveFromHierarchy();
			}
			tabbarContainer = new VisualElement() {
				name = "tabbar-container"
			};
			tabbarContainer.AddStyleSheet("uNodeStyles/Tabbar");
			tabbarContainer.AddStyleSheet(UIElementUtility.Theme.tabbarStyle);

			#region Tabbar
			{
				var tabbar = new VisualElement() {
					name = "tabbar"
				};
				tabbarContainer.Add(tabbar);

				InitTabbar(tabbar);
			}
#endregion

			#region Pathbar
			var pathbar = new VisualElement() {
				name = "pathbar"
			};
			if(graphData.graph != null) {
				var graph = new ClickableElement(graphData.graph.GetGraphName()) {
					name = "path-element"
				};
				graph.AddToClassList("path-graph");
				if(tabData.owner is IScriptGraph) {
					var scriptGraph = tabData.owner as IScriptGraph;
					graph.menu = new DropdownMenu();
					
					var graphs = scriptGraph.TypeList.references.Where(item => item is IGraph).Select(item => item as IGraph).ToArray();
					if(graphs == null)
						return;
					for(int i = 0; i < graphs.Length; i++) {
						var g = graphs[i];
						if(g == null)
							continue;
						graph.menu.AppendAction(g.GetGraphName(), (act) => {
							if(g == graphData.graph) {
								//var data = tabData.graphDatas.FirstOrDefault(item => item.graph == g);
								//if(data != null) {
								//	tabData.selectedGraphData = data;
								//}
								window.ChangeEditorSelection(null);
							}
							else {
								uNodeEditor.Open(g);
							}
						}, (act) => {
							if(g == graphData.graph) {
								return DropdownMenuAction.Status.Checked;
							}
							return DropdownMenuAction.Status.Normal;
						});
					}
				}
				Type graphIcon = typeof(TypeIcons.GraphIcon);
				if(graphData.graph is IClassGraph) {
					IClassGraph classSystem = graphData.graph as IClassGraph;
					graphIcon = classSystem.InheritType == typeof(ValueType) ? typeof(TypeIcons.StructureIcon) : typeof(TypeIcons.ClassIcon);
				}
				graph.ShowIcon(uNodeEditorUtility.GetTypeIcon(graphIcon));
				graph.EnableBreadcrumb(true);
				pathbar.Add(graph);
				var root = window.selectedTab.selectedGraphData.selectedRoot;
				var function = new ClickableElement(root is NodeContainerWithEntry ? root.name : $"[{graphData.mainGraphTitle}]") {
					name = "path-element"
				};
				function.AddToClassList("path-function");
				{
					function.menu = new DropdownMenu();
					if(graphData.isSupportMainGraph) {
						function.menu.AppendAction($"[{graphData.mainGraphTitle}]", (act) => {
							if(graphData.selectedRoot != null || graphData.selectedGroup != null) {
								graphData.ClearSelection();
								graphData.currentCanvas = null;
								Refresh();
								UpdatePosition();
							}
							window.ChangeEditorSelection(null);
						}, (act) => {
							if(graphData.selectedRoot == null) {
								return DropdownMenuAction.Status.Checked;
							}
							return DropdownMenuAction.Status.Normal;
						});
					}

					List<NodeContainer> roots = new List<NodeContainer>();
					roots.AddRange(graphData.graph.GetFunctions());
					roots.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));
					for(int i = 0; i < roots.Count; i++) {
						var r = roots[i];
						if(r == null)
							continue;
						function.menu.AppendAction(r.name, (act) => {
							if(graphData.currentCanvas == r) {
								window.ChangeEditorSelection(r);
							} else {
								graphData.currentCanvas = r;
								SelectionChanged();
								Refresh();
								UpdatePosition();
							}
						}, (act) => {
							if(r == graphData.selectedRoot) {
								return DropdownMenuAction.Status.Checked;
							}
							return DropdownMenuAction.Status.Normal;
						});
					}
				}
				function.ShowIcon(uNodeEditorUtility.GetTypeIcon(root == null ? typeof(TypeIcons.StateIcon) : typeof(TypeIcons.MethodIcon)));
				pathbar.Add(function);
				if(graphData.graph != null && graphData.selectedGroup != null) {
					function.EnableBreadcrumb(true);
					List<NodeObject> GN = new List<NodeObject>();
					UGraphElement parent = graphData.selectedGroup;
					while(parent != null) {
						if(parent is NodeObject parentNode) {
							if(parentNode.node is ISuperNode) {
								GN.Add(parentNode);
							}
						} else if(parent is NodeContainer) {
							break;
						}
						parent = parent.parent;
					}
					for(int i = GN.Count - 1; i >= 0; i--) {
						var nestedGraph = GN[i];
						var element = new ClickableElement(nestedGraph.GetTitle()) {
							name = "path-element",
							onClick = (_) => {
								window.ChangeEditorSelection(nestedGraph, false);
								if(graphData.selectedGroup != nestedGraph) {
									graphData.currentCanvas = nestedGraph;
									Refresh();
									UpdatePosition();
								}
							}
						};
						element.AddToClassList("path-nested");
						element.ShowIcon(uNodeEditorUtility.GetTypeIcon(nestedGraph.GetNodeIcon()));
						pathbar.Add(element);
						if(i != 0) {
							element.EnableBreadcrumb(true);
						}
					}
				}
			} else {
				var graph = new ClickableElement("[NO GRAPH]") {
					name = "path-element"
				};
				pathbar.Add(graph);
			}
			tabbarContainer.Add(pathbar);
			#endregion

			rootContent.Add(tabbarContainer);
			tabbarContainer.SendToBack();
		}

		public override void FrameGraph() {
			if(graphView != null) {
				graphView.FrameAll();
			}
		}

		public override void HandleShortcut(GraphShortcutType type) {
			if(graphView.HandleShortcut(type)) {

			}
			else if(graphPanel != null && graphPanel.HandleShortcut(type)) {

			}
			else {
				base.HandleShortcut(type);
			}
		}

		public override void GUIChanged(object obj, UIChangeType changeType) {
			if(obj is Node) {
				obj = (obj as Node).nodeObject;
			}
			switch(changeType) {
				case UIChangeType.Small:
					if(obj is NodeObject) {
						var nodeObject = obj as NodeObject;
						if(graphView.cachedNodeMap.TryGetValue(nodeObject, out var view)) {
							if(graphView.nodeViews.Contains(view)) {
								if(view.autoReload) {
									//Reload the node if auto reload is true
									graphView.MarkRepaint(view);
								} else {
									view.UpdateUI();
									view.MarkDirtyRepaint();
								}
							} else {
								//This will ensure the node will create up to date views when the GUI changed.
								graphView.nodeViews.Remove(view);
								graphView.cachedNodeMap.Remove(nodeObject);
							}
						}
					}
					MarkReloadTabbar();
					break;
				case UIChangeType.Average:
					if(obj is NodeObject) {
						var nodeObject = obj as NodeObject;
						if(graphView.cachedNodeMap.TryGetValue(nodeObject, out var view)) {
							if(graphView.nodeViews.Contains(view)) {
								//Ensure to reload node view when the GUI changed
								graphView.MarkRepaint(view);
							} else {
								//This will ensure the node will create up to date views when the GUI changed.
								graphView.nodeViews.Remove(view);
								graphView.cachedNodeMap.Remove(nodeObject);
							}
						}
						MarkReloadTabbar();
					}
					else if(obj is Variable || obj is Property || obj is Function || obj is Constructor) {
						ReloadView(true);
					}
					else {
						MarkReloadTabbar();
					}
					break;
				case UIChangeType.Important:
					ReloadView(true);
					break;
			}
		}

		public override void ReloadView(bool fullReload) {
			if(graphContentView == null || graphView == null) {
				OnEnable();
			}
			MarkReloadTabbar();
			graphPanel?.MarkRepaint();
			graphView.MarkRepaint(fullReload);
		}

		public override void OnSearchChanged(string search) {
			base.OnSearchChanged(search);
			if(graphView != null) {
				graphView.OnSearchChanged(searchQuery);
			}
		}

		public override void OnSearchNext() {
			base.OnSearchNext();
			if(graphView != null) {
				graphView.OnSearchNext();
			}
		}

		public override void OnSearchPrev() {
			base.OnSearchPrev();
			if(graphView != null) {
				graphView.OnSearchPrev();
			}
		}

		public bool isGraphLoaded {
			get { return graphView != null && graphView.graph != null; }
		}

		void InitializeRootView() {
			if(graphContentView != null) {
				graphContentView.RemoveFromHierarchy();
			}
			if(rootContainer == null) {
				rootContainer = new VisualElement() {
					name = "root-container",
				};
				rootContainer.AddStyleSheet("uNodeStyles/NativeGraphStyle");
				rootContainer.AddStyleSheet("uNodeStyles/NativeControlStyle");
				rootContainer.AddStyleSheet(UIElementUtility.Theme.graphStyle);
				UIElementUtility.ForceDarkStyleSheet(rootContainer);

				graphPanelView = new VisualElement() {
					name = "graph-panel",
				};
				graphPanelView.Add(graphPanel = new GraphPanel(this));
				rootContainer.Add(graphPanelView);

				inspectorPanel = new VisualElement() {
					name = "inspector-panel",
				};
				inspectorPanel.Add(new IMGUIContainer(OnInspectorGUI));

				rootContent = new VisualElement() {
					name = "root-content",
				};
				rootContainer.Add(rootContent);
			}
			if(rootContainer.parent != window.rootVisualElement) {
				rootContainer.RemoveFromHierarchy();
				window.rootVisualElement.Add(rootContainer);
			}
			graphContentView = new VisualElement() {
				name = "graph-content"
			};
			graphRootView = new VisualElement() {
				name = "graphRootView"
			};
			graphContentView.Add(graphRootView);
			rootContent.Add(graphContentView);

			InitStatusBar();
		}

		private Vector2 m_scrollInspectorPos;
		private void OnInspectorGUI() {
			m_scrollInspectorPos = EditorGUILayout.BeginScrollView(m_scrollInspectorPos);
			if(graphData != null) {
				CustomInspector.ShowInspector(graphData);
			}
			EditorGUILayout.EndScrollView();
		}

		private int lastErrorCount;
		public override void OnErrorUpdated() {
			var errorCount = window != null ? window.ErrorsCount() : 0;
			if(lastErrorCount != errorCount) {
				errorLabel.text = errorCount + " errors";
				lastErrorCount = errorCount;
				if(errorCount > 0) {
					if(errorImage?.parent == null) {
						errorStatus?.Insert(0, errorImage);
					}
				} else {
					errorImage?.RemoveFromHierarchy();
				}
			}
		}

		private void InitStatusBar() {
			if(statusContainerView == null) {
				statusContainerView = new Toolbar() {
					name = "status-bar",
				};
				errorStatus = new ToolbarButton() {
					name = "status-error",
					//text = "0 errors",
					clickable = new Clickable(() => {
						ErrorCheckWindow.ShowWindow();
					})
				};
				errorStatus.style.flexDirection = FlexDirection.Row;
				errorLabel = new Label("0 errors");
				errorImage = new Image();
				//errorImage.AddToClassList(HelpBox.iconUssClassName);
				errorImage.AddToClassList(HelpBox.iconErrorUssClassName);
				//errorStatus.Add(errorImage);
				errorStatus.Add(errorLabel);
				statusContainerView.Add(errorStatus);

				#region Snap
				var snapStatus = new ToolbarButton() {
					name = "status-snap",
				};
				{
					snapStatus.AddToClassList(ToolbarMenu.ussClassName);
					var m_TextElement = new TextElement();
					m_TextElement.AddToClassList(ToolbarMenu.textUssClassName);
					m_TextElement.pickingMode = PickingMode.Ignore;
					m_TextElement.text = "Snap";
					snapStatus.Add(m_TextElement);
					var m_ArrowElement = new VisualElement();
					m_ArrowElement.AddToClassList(ToolbarMenu.arrowUssClassName);
					m_ArrowElement.pickingMode = PickingMode.Ignore;
					snapStatus.Add(m_ArrowElement);
				}
				void updateSnapStatus() {
					snapStatus.EnableInClassList("toggle-on", uNodePreference.preferenceData.enableSnapping &&
						(uNodePreference.preferenceData.graphSnapping ||
						uNodePreference.preferenceData.gridSnapping ||
						uNodePreference.preferenceData.spacingSnapping ||
						uNodePreference.preferenceData.nodePortSnapping));
				}
				updateSnapStatus();
				snapStatus.clickable = new Clickable(evt => {
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Enable Snapping"), uNodePreference.preferenceData.enableSnapping, () => {
						uNodePreference.preferenceData.enableSnapping = !uNodePreference.preferenceData.enableSnapping;
						uNodePreference.SavePreference();
						updateSnapStatus();
					});
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Graph Snapping"), uNodePreference.preferenceData.graphSnapping, () => {
						uNodePreference.preferenceData.graphSnapping = !uNodePreference.preferenceData.graphSnapping;
						uNodePreference.SavePreference();
						updateSnapStatus();
					});
					menu.AddItem(new GUIContent("Node Port Snapping"), uNodePreference.preferenceData.nodePortSnapping, () => {
						uNodePreference.preferenceData.nodePortSnapping = !uNodePreference.preferenceData.nodePortSnapping;
						uNodePreference.SavePreference();
						updateSnapStatus();
					});
					menu.AddItem(new GUIContent("Grid Snapping"), uNodePreference.preferenceData.gridSnapping, () => {
						uNodePreference.preferenceData.gridSnapping = !uNodePreference.preferenceData.gridSnapping;
						uNodePreference.SavePreference();
						updateSnapStatus();
					});
					menu.AddItem(new GUIContent("Spacing Snapping"), uNodePreference.preferenceData.spacingSnapping, () => {
						uNodePreference.preferenceData.spacingSnapping = !uNodePreference.preferenceData.spacingSnapping;
						uNodePreference.SavePreference();
						updateSnapStatus();
					});
					menu.ShowAsContext();
				});
				statusContainerView.Add(snapStatus);
				#endregion

				#region Carry
				var carryStatus = new ToolbarToggle() {
					name = "status-carry",
					text = "Carry",
				};
				carryStatus.SetValueWithoutNotify(uNodePreference.preferenceData.carryNodes);
				carryStatus.RegisterValueChangedCallback(evt => {
					uNodePreference.preferenceData.carryNodes = evt.newValue;
					uNodePreference.SavePreference();
				});
				statusContainerView.Add(carryStatus);

				statusContainerView.Add(new ToolbarSpacer() {
					flex = true,
				});
				#endregion

				#region Zoom
				zoomStatus = new ToolbarMenu() {
					name = "status-zoom",
					text = "Zoom : 1.00",
				};
				zoomStatus.variant = ToolbarMenu.Variant.Popup;
				zoomStatus.menu.AppendAction("0.3x", act => {
					graphView.SetZoomScale(0.3f);
				});
				zoomStatus.menu.AppendAction("0.5x", act => {
					graphView.SetZoomScale(0.5f);
				});
				zoomStatus.menu.AppendAction("0.7x", act => {
					graphView.SetZoomScale(0.7f);
				});
				zoomStatus.menu.AppendAction("1x", act => {
					graphView.SetZoomScale(1);
				});
				zoomStatus.menu.AppendAction("1.5x", act => {
					graphView.SetZoomScale(1.5f);
				});
				zoomStatus.menu.AppendAction("2x", act => {
					graphView.SetZoomScale(2f);
				});
				statusContainerView.Add(zoomStatus);
				#endregion

				#region Search
				var searchStatus = new ToolbarPopupSearchField() {
					name = "status-search",
				};
				searchStatus.RegisterValueChangedCallback(evt => {
					OnSearchChanged(evt.newValue);
				});
				statusContainerView.Add(searchStatus);

				var prevStatus = new ToolbarButton() {
					name = "status-snap",
					text = "Prev",
				};
				prevStatus.clicked += () => {
					OnSearchPrev();
				};
				statusContainerView.Add(prevStatus);

				var nextStatus = new ToolbarButton() {
					name = "status-snap",
					text = "Next",
				};
				nextStatus.clicked += () => {
					OnSearchNext();
				};
				statusContainerView.Add(nextStatus);

				prevStatus.SetEnabled(!string.IsNullOrEmpty(searchStatus.value.Trim()));
				nextStatus.SetEnabled(!string.IsNullOrEmpty(searchStatus.value.Trim()));
				searchStatus.RegisterValueChangedCallback(evt => {
					prevStatus.SetEnabled(!string.IsNullOrEmpty(evt.newValue.Trim()));
					nextStatus.SetEnabled(!string.IsNullOrEmpty(evt.newValue.Trim()));
				});
				#endregion

				UIElementUtility.ForceDarkToolbarStyleSheet(statusContainerView);
			}
		}

		public void InitializeGraph() {
			if(graphView != null && !graphRootView.Contains(graphView)) {
				graphView.RemoveFromHierarchy();
			}
			if(graphView == null)
				graphView = new UGraphView();
			if(!graphRootView.Contains(graphView))
				graphRootView.Add(graphView);
			graphView.Initialize(this);
			graphView.StretchToParentSize();
			ReloadTabbar();
		}

		private IMGUIContainer mainGUIContainer;
		private uNodeEditor.GraphExplorerTree explorerTree;
		private UnityEditor.IMGUI.Controls.SearchField explorerSearch;

		public override void DrawMainTab(uNodeEditor window) {
			base.DrawMainTab(window);
			if(mainGUIContainer == null) {
				if(explorerSearch == null) {
					explorerSearch = new UnityEditor.IMGUI.Controls.SearchField();
				}
				if(explorerTree == null) {
					explorerTree = new uNodeEditor.GraphExplorerTree();
				}
				mainGUIContainer = new IMGUIContainer(() => {
					var areaRect = new Rect(0, 0, mainGUIContainer.layout.width, mainGUIContainer.layout.height);
					var explorerRect = areaRect;
					if(explorerRect.width > 400) {
						var rect = new Rect(400, 0, explorerRect.width - 400, explorerRect.height);
						explorerRect.width = 400;
						if(rect.width > 300) {
							explorerRect.width += rect.width - 300;
							rect.x += rect.width - 300;
							rect.width = 300;
						}
						GUILayout.BeginArea(rect);
						if(GUILayout.Button(new GUIContent("New Graph"))) {
							GraphCreatorWindow.ShowWindow();
							Event.current.Use();
						}
						if(GUILayout.Button(new GUIContent("Open Graph"))) {
							//OpenNewGraphTab();
						}
						if(Selection.activeGameObject == null) {
							EditorGUILayout.HelpBox("Double Click a uNode Graph Assets to edit the graph or click 'New Graph' to create a new graph.\n" +
								"Or select a GameObject in hierarchy to create a new Scene Graph", MessageType.Info);
						}
						else {
							if(uNodeEditorUtility.IsPrefab(Selection.activeGameObject)) {
								var comp = Selection.activeGameObject.GetComponent<IInstancedGraph>();
								//if(comp is uNodeRuntime) {
								//	EditorGUILayout.HelpBox(string.Format("To edit graph or create a new uNode graph with \'{0}\', please open the prefab", Selection.activeGameObject.name), MessageType.Info);
								//}
								if(comp != null && comp.OriginalGraph != null) {
									if(GUILayout.Button(new GUIContent("Edit Graph"))) {
										uNodeEditor.Open(comp.OriginalGraph);
										Event.current.Use();
										return;
									}
									EditorGUILayout.HelpBox(string.Format("To edit \'{0}\' graph, please edit graph in new Tab", Selection.activeGameObject.name), MessageType.Info);
								}
							}
							else {
								if(GUILayout.Button(new GUIContent("Create from Selection"))) {
									var graph = Selection.activeGameObject.AddComponent<GraphComponent>();
									uNodeEditor.Open(graph);
									Event.current.Use();
									return;
								}
								EditorGUILayout.HelpBox(string.Format("To begin a new uNode graph with \'{0}\', create a uNode component", Selection.activeGameObject.name), MessageType.Info);
							}
						}
						GUILayout.EndArea();
					}
					var search = explorerSearch.OnGUI(new Rect(0, 0, explorerRect.width, 16), explorerTree.searchString);
					if(search != explorerTree.searchString) {
						explorerTree.searchString = search;
						explorerTree.Reload();
					}
					explorerRect.height -= 16;
					explorerRect.y += 16;
					explorerTree.OnGUI(explorerRect);
				});
				mainGUIContainer.style.flexGrow = 1;
			}

			if(mainGUIContainer.parent == null) {
				rootContent.Add(mainGUIContainer);
				mainGUIContainer.BringToFront();
			}
			if(graphPanelView?.parent != null) {
				graphPanelView.RemoveFromHierarchy();
			}
		}

		public override void DrawCanvas(uNodeEditor window) {
			base.DrawCanvas(window);
			if(graphContentView == null) {
				OnEnable();
			}
			if(!hasInitialize) {
				ReloadView();
				hasInitialize = true;
			}
			if(statusContainerView.parent == null) {
				rootContent.Add(statusContainerView);
				statusContainerView.BringToFront();
			}
			if(mainGUIContainer?.parent != null) {
				mainGUIContainer.RemoveFromHierarchy();
			}

			if(Event.current.type == EventType.Repaint && uNodeThreadUtility.frame % 2 == 0) {
				zoomScale = graphView.scale;
				zoomStatus.text = "Zoom : " + zoomScale.ToString("F2");
				if(graphPanelView != null) {
					if(uNodeEditor.SavedData.leftVisibility) {
						if(graphPanelView.parent == null) {
							rootContainer.Insert(0, graphPanelView);
						}
						graphPanelView.style.width = uNodeEditor.SavedData.leftPanelWidth;
					} else {
						if(graphPanelView.parent != null) {
							graphPanelView.RemoveFromHierarchy();
						}
					}
				}
				if(inspectorPanel != null) {
					if(uNodeEditor.SavedData.rightVisibility) {
						if(inspectorPanel.parent != graphContentView) {
							inspectorPanel.RemoveFromHierarchy();
							graphContentView.Add(inspectorPanel);
						}
						inspectorPanel.style.width = uNodeEditor.SavedData.rightPanelWidth;
					}
					else {
						if(inspectorPanel.parent != null) {
							inspectorPanel.RemoveFromHierarchy();
						}
					}
				}
				if(EditorUtility.IsPersistent(graphData.owner) == false) {
					if(saveButton.visible)
						saveButton.visible = false;
				} else {
					if(saveButton.visible == false)
						saveButton.visible = true;
				}
				if(graphData.isGraphOpen) {
					if(frameButton.resolvedStyle.display == DisplayStyle.None)
						frameButton.SetDisplay(true);
				}
				else {
					if(frameButton.resolvedStyle.display == DisplayStyle.Flex)
						frameButton.SetDisplay(false);
				}
			}

			if(graphView.layout.Contains(topMousePos)) {
				graphView.IMGUIEvent(Event.current);
			}
			_debugData = GetDebugData();
			if(Event.current.type == EventType.Repaint && uNodeThreadUtility.frame % 2 == 0) {
				debugButton.text = GetDebugName();
			}
		}

		private GraphDebug.DebugData _debugData;

		public GraphDebug.DebugData GetDebugInfo() {
			if(_debugData == null) {
				return GetDebugData();
			}
			return _debugData;
		}

		public override void MoveCanvas(Vector2 position) {
			base.MoveCanvas(position);
			if(graphView != null) {
				graphView.UpdatePosition(position, graphView.scale);
			}
			// ReloadView();
		}

		public override void Highlight(UGraphElement element) {
			if(element is NodeObject) {
				graphView.HighlightNodes(new NodeObject[] { element as NodeObject });
			}
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class NodeCustomEditor : Attribute {
		public Type nodeType;

		public NodeCustomEditor(Type nodeType) {
			this.nodeType = nodeType;
		}
	}
}