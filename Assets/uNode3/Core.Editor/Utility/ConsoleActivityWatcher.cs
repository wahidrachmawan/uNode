using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
	public static class ConsoleActivityWatcher {
		[InitializeOnLoadMethod]
		static void Initialize() {
			//Delay some frame to avoid lag on editor startup.
			uNodeThreadUtility.ExecuteAfter(100, () => {
				consoleWindowType = "UnityEditor.ConsoleWindow".ToType();
				fieldActiveText = consoleWindowType.GetField("m_ActiveText", MemberData.flags);
				fieldCallStack = consoleWindowType.GetField("m_CallstackTextStart", MemberData.flags);

				EditorApplication.update += WatchConsoleActivity;
				// Debug.Log("Ready");
			});
		}

		private static Type consoleWindowType;
		private static FieldInfo fieldActiveText;
		private static FieldInfo fieldCallStack;
		private static EditorWindow consoleWindow;
		private static ConsoleWatcher consoleWatcher;

		private static float time;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset() {
			time = 0;
		}

		private static void WatchConsoleActivity() {
			// if (entryChanged == null) return;
			if (consoleWindow == null) {
				if (time < uNodeThreadUtility.time) {
					var windows = Resources.FindObjectsOfTypeAll(consoleWindowType);
					for(int i=0;i<windows.Length;i++) {
						var console = windows[i] as EditorWindow;
						var root = console.rootVisualElement;
						var watcher = root.Q<ConsoleWatcher>();
						if(watcher == null) {
							watcher = new ConsoleWatcher(console);
							root.Add(watcher);
							watcher.StretchToParentSize();
							consoleWatcher = watcher;
						}
						consoleWindow = console;
					}
					//Find console window every 2 second
					time = uNodeThreadUtility.time + 2;
				}
			}
			else if(consoleWatcher != null) {
				if(time < uNodeThreadUtility.time) {
					consoleWatcher.UpdateContainer();
					time = uNodeThreadUtility.time + 5;
				}
			}
		}

		#region Classes
		private struct ActivityData {
			public int line;
			public string path;
			public uNodeEditor.EditorScriptInfo info;

			public ActivityData(int line, string path, uNodeEditor.EditorScriptInfo info) {
				this.line = line;
				this.path = path;
				this.info = info;
			}
		}

		class ConsoleWatcher : VisualElement {
			private readonly EditorWindow consoleWindow;
			private IMGUIContainer container;
			private string lastActiveText;

			public ConsoleWatcher(EditorWindow consoleWindow) {
				this.consoleWindow = consoleWindow;
				UpdateContainer();
			}

			public void UpdateContainer() {
				if(consoleWindow != null) {
					if(consoleWindow.rootVisualElement?.parent?.childCount > 0) {
						container = consoleWindow.rootVisualElement.parent[0] as IMGUIContainer;
					}
					if(container == null) {
						this.pickingMode = PickingMode.Ignore;
					} else {
						this.pickingMode = PickingMode.Ignore;
						container.UnregisterCallback<MouseDownEvent>(ProcessEvent, TrickleDown.TrickleDown);
						container.RegisterCallback<MouseDownEvent>(ProcessEvent, TrickleDown.TrickleDown);
					}
				}
			}

			void ProcessEvent(MouseDownEvent evt) {
				if(evt.button == 0) {
					if(evt.clickCount >= 2) {
						var activeText = (string)fieldActiveText.GetValue(consoleWindow);
						lastActiveText = activeText;
						if(ConsoleActivityChanged(activeText, evt.mousePosition)) {
							evt.StopImmediatePropagation();
						}
						return;
					}
					else {
						var mPos = evt.mousePosition;
						bool flag = evt.modifiers == EventModifiers.Shift || evt.modifiers == EventModifiers.Control || evt.modifiers == EventModifiers.Alt;
						uNodeThreadUtility.ExecuteOnce(() => {
							var activeText = (string)fieldActiveText.GetValue(consoleWindow);
							//SendEventToIMGUI(mouseEvent);
							if(lastActiveText != activeText || flag) {
								lastActiveText = activeText;
								if(fieldCallStack != null) {
									fieldCallStack.SetValue(consoleWindow, 0);
								}
								ConsoleActivityChanged(activeText, mPos);
							}
						}, this);
					}
				}
			}
		}

		struct MenuData {
			public string menu;
			public GenericMenu.MenuFunction action;
		}
		#endregion

		private static bool ConsoleActivityChanged(string text, Vector2 mousePosition) {
			if(string.IsNullOrEmpty(text)) return false;
			var strs = text.Split('\n');
			List<MenuData> menus = new List<MenuData>();
			Action postMenuAction = null;

			foreach(var txt in strs) {
				var idx = txt.IndexOf(GraphException.KEY_REFERENCE);
				if(idx >= 0) {
					string str = null;
					for(int i = idx + GraphException.KEY_REFERENCE.Length; i < txt.Length; i++) {
						if(txt[i] == GraphException.KEY_REFERENCE_TAIL) {
							break;
						} else {
							str += txt[i];
						}
					}
					var ids = str.Split(GraphException.KEY_REFERENCE_SEPARATOR);
					if(ids.Length >= 2) {
						UnityEngine.Object reference = null;
						if(int.TryParse(ids[0], out var id) && int.TryParse(ids[1], out var graphID)) {
							reference = EditorUtility.InstanceIDToObject(graphID);
							if(reference == null) {
								var db = uNodeDatabase.instance?.graphDatabases;
								if(db != null) {
									foreach(var data in db) {
										if(data.fileUniqueID == graphID) {
											reference = data.asset;
											break;
										}
									}
								}
							}
						}
						else {
							var db = uNodeDatabase.instance?.graphDatabases;
							if(db != null) {
								foreach(var data in db) {
									if(data.assetGuid == ids[1]) {
										reference = data.asset;
										break;
									}
								}
							}
						}

						if(reference != null && reference is IGraph graph) {
							UGraphElement element = graph.GetGraphElement(id);
							if(element is NodeObject nodeObject) {
								menus.Add(new MenuData() {
									menu = $"Highlight Node:{nodeObject.GetTitle()} from {graph.GetFullGraphName()}",
									action = () => {
										uNodeEditor.HighlightNode(nodeObject);
										if(ids.Length > 2 && uNodeEditor.window != null) {
											var graphData = uNodeEditor.window.graphData;
											if(graphData.debugAnyScript) {
												graphData.SetAutoDebugTarget(GraphDebug.GetDebugObject(ids[2]));
											}
										}
									}
								});
							}
							else if(element != null) {
								menus.Add(new MenuData() {
									menu = $"Highlight Element: {element.name} with id: {element.id} from {(reference as IGraph).GetFullGraphName()}",
									action = () => uNodeEditor.Open(reference as IGraph, element)
								});
							}
							else {
								postMenuAction += () => {
									if(menus.Any(menu => menu.menu.Contains((reference as IGraph).GetFullGraphName())) == false) {
										menus.Add(new MenuData() {
											menu = $"Highlight Graph: {(reference as IGraph).GetFullGraphName()}",
											action = () => uNodeEditor.Open(reference as IGraph)
										});
									}
								};
							}
							continue;
						}
					}
					continue;
				}
				postMenuAction?.Invoke();
				List<ActivityData> datas = new List<ActivityData>();
				foreach(var info in uNodeEditor.SavedData.scriptInformations) {
					string path = info.path.Replace("\\", "/");
					int index = txt.IndexOf("at " + path + ":");
					if(index < 0) {
						try {
							path = path.Remove(0, System.IO.Directory.GetCurrentDirectory().Length + 1);
							index = txt.IndexOf("at " + path + ":");
						}
						catch { }
					}
					if(index >= 0) {
						datas.Add(new ActivityData(index, path, info));
					}
				}
				// datas.Sort((x, y) => CompareUtility.Compare(x.number, y.number));
				ActivityData lastData = default;
				int line = 0;
				foreach(var data in datas.OrderByDescending(x => x.line)) {
					string num = "";
					for(int i = data.line + data.path.Length + 4; i < txt.Length; i++) {
						var c = txt[i];
						if(char.IsNumber(c)) {
							num += c;
						} else {
							break;
						}
					}
					if(int.TryParse(num, out line)) {
						line--;
						lastData = data;
					}
				}
				if(lastData.info != null && uNodeEditor.CanHighlightNode(lastData.info, line)) {
					menus.Add(new MenuData() {
						menu = $"{lastData.path.Replace('/', '\\')}:{line + 1}",
						action = () => uNodeEditor.HighlightNode(lastData.info, line)
					});
					continue;
				}
			}
			if(menus.Count > 0) {
				if(menus.GroupBy(menu => menu.menu).Count() == 1) {
					menus[0].action();
				} else {
					GenericMenu menu = new GenericMenu();
					for(int i=0;i<menus.Count;i++) {
						menu.AddItem(new GUIContent(menus[i].menu), false, menus[i].action);
					}
					menu.DropDown(new Rect(mousePosition.x, mousePosition.y, 0, 0));
				}
				return true;
			}
			return false;
		}
	}
}