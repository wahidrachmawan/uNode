using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MaxyGames.UNode.Editors {
    public class ErrorCheckWindow : EditorWindow {
		public static ErrorCheckWindow window;
		[SerializeField]
		public static bool onlySelectedGraph = true;
		private ListView listView;
		private int _localUpdateID = -1;

		class ErrorData {
			public UGraphElementRef element;
			public uNodeUtility.ErrorMessage message;
		}

		static int _updateID;
		static List<ErrorData> errorMessages = new List<ErrorData>();

		[MenuItem("Tools/uNode/Error Check")]
		public static void ShowWindow() {
			window = (ErrorCheckWindow)GetWindow(typeof(ErrorCheckWindow));
			window.titleContent = new GUIContent("Error Check");
			window.minSize = new Vector2(250, 250);
			window.Show();
		}

		public static void UpdateErrorMessages() {
			_updateID++;
			errorMessages.Clear();
			if(window == null)
				return;
			if(onlySelectedGraph) {
				var selectedGraph = uNodeEditor.window?.graphData.graph;
				if(selectedGraph != null && GraphUtility.ErrorChecker.defaultAnalizer.graphErrors.TryGetValue(selectedGraph, out var map)) {
					foreach(var pair in map) {
						var data = pair.Value;
						if(data.HasError(InfoType.Error)) {
							var reference = new UGraphElementRef(pair.Key);
							foreach(var error in data.GetErrors(InfoType.Error)) { 
								errorMessages.Add(new ErrorData() {
									element = reference,
									message = error,
								});
							}
						}
					}
				}
			} else {
				foreach(var (key, map) in GraphUtility.ErrorChecker.defaultAnalizer.graphErrors) {
					foreach(var pair in map) {
						var data = pair.Value;
						if(data.HasError(InfoType.Error)) {
							var reference = new UGraphElementRef(pair.Key);
							foreach(var error in data.GetErrors(InfoType.Error)) {
								errorMessages.Add(new ErrorData() {
									element = reference,
									message = error,
								});
							}
						}
					}
				}
			}
		}

		private void OnEnable() {
			window = this;
			UpdateErrorMessages();
			listView = new ListView(errorMessages,
				makeItem: () => {
					var ve = new VisualElement() {
						name = "list-element",
					};
					ve.style.paddingLeft = 5;
					ve.Add(new Label() {
						name = "element-label",
					});
					ve.Add(new Label() {
						name = "element-sub-label",
					});
					return ve;
				},
				bindItem: (ve, index) => {
					var label = ve[0] as Label;
					var sublabel = ve[1] as Label;
					label.text = errorMessages[index].message.message.Split('\n').FirstOrDefault();
					sublabel.text = GetElementPath(errorMessages[index].element.reference);
				});
			listView.fixedItemHeight = 32;
			listView.selectionType = SelectionType.Single;
#if UNITY_2022_3_OR_NEWER
			listView.selectionChanged += SelectionChanged;
			listView.itemsChosen += SelectionChanged;
#else
			listView.onSelectionChange += SelectionChanged;
			listView.onItemsChosen += SelectionChanged;
#endif
			listView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
			listView.horizontalScrollingEnabled = true;
			listView.style.flexGrow = 1;
			var toolbar = new Toolbar();
			toolbar.Add(new ToolbarButton() {
				text = "Refresh",
				clickable = new Clickable(() => {
					UpdateErrorMessages();
				}),
			});
			toolbar.Add(new ToolbarSpacer() { flex = true });
			var onlySelectedGraphBtn = new ToolbarToggle() {
				name = "only-selected-graph",
				text = "Only Selected Graph",
			};
			onlySelectedGraphBtn.SetValueWithoutNotify(onlySelectedGraph);
			onlySelectedGraphBtn.RegisterValueChangedCallback(evt => {
				onlySelectedGraph = evt.newValue;
				UpdateErrorMessages();
			});
			toolbar.Add(onlySelectedGraphBtn);
			rootVisualElement.Add(toolbar);
			rootVisualElement.Add(listView);
		}

		private static string GetElementPath(UGraphElement element, bool richText = false) {
			string path = null;
			if(element != null) {
				var current = element;
				while(current != null) {
					string str;
					if(richText && current is IRichName richName) {
						str = richName.GetRichName();
					} else if(current is IPrettyName prettyName) {
						str = prettyName.GetPrettyName();
					} else {
						str = current.name;
					}
					if(!string.IsNullOrEmpty(str)) {
						path = str + path.AddFirst(" > ");
					}
					current = current.parent;
				}
			}
			return path ?? string.Empty;
		}

		internal static List<(string, Texture)> GetElementPathWithIcon(UGraphElement element, bool richText = false, bool fullPath = false) {
			var result = new List<(string, Texture)>();
			if(element != null) {
				var current = element;
				while(current != null) {
					if(!fullPath && current.parent == null) break;
					string str;
					if(richText && current is IRichName richName) {
						str = richName.GetRichName();
					}
					else if(current is IPrettyName prettyName) {
						str = prettyName.GetPrettyName();
					}
					else {
						str = current.name;
					}
					if(!string.IsNullOrEmpty(str)) {
						result.Insert(0, (str, uNodeEditorUtility.GetTypeIcon(current)));
					}
					current = current.parent;
				}
			}
			return result;
		}

		private static void SelectionChanged(IEnumerable<object> obj) {
			var selectedObj = obj.FirstOrDefault();
			if(selectedObj is ErrorData data && data != null) {
				if(data.element.reference is NodeObject nodeObject && nodeObject != null) {
					uNodeEditor.HighlightNode(nodeObject);
				}
				else if(data.element.reference != null) {
					uNodeEditor.Open(data.element.reference.graphContainer);
					uNodeEditor.window.ChangeEditorSelection(data.element.reference);
				}
			}
		}

		private void Update() {
			if(_localUpdateID != _updateID) {
				_localUpdateID = _updateID;
				listView.RefreshItems();
			}
		}
	}
}