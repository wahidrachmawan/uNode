using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Linq;
using System.Xml;
using System.Reflection;

namespace MaxyGames.UNode.Editors {
	public class AutoCompleteWindow : EditorWindow {
		#region Styles
		private static class Styles {
			public static readonly GUIStyle textAreaStyle;

			// Default background Color(0.76f, 0.76f, 0.76f)
			private static readonly Color bgColorLightSkin = new Color(0.87f, 0.87f, 0.87f);
			// Default background Color(0.22f, 0.22f, 0.22f)
			private static readonly Color bgColorDarkSkin = new Color(0.2f, 0.2f, 0.2f);
			// Default text Color(0.0f, 0.0f, 0.0f)
			private static readonly Color textColorLightSkin = new Color(0.0f, 0.0f, 0.0f);
			// Default text Color(0.706f, 0.706f, 0.706f)
			private static readonly Color textColorDarkSkin = new Color(0.706f, 0.706f, 0.706f);

			private static Texture2D _backgroundTexture;
			public static Texture2D backgroundTexture {
				get {
					if(_backgroundTexture == null) {
						_backgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
						_backgroundTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? bgColorDarkSkin : bgColorLightSkin);
						_backgroundTexture.Apply();
					}
					return _backgroundTexture;
				}
			}

			static Styles() {
				textAreaStyle = new GUIStyle(EditorStyles.textArea);
				textAreaStyle.padding = new RectOffset();

				var style = textAreaStyle.focused;
				style.background = backgroundTexture;
				style.textColor = EditorGUIUtility.isProSkin ? textColorDarkSkin : textColorLightSkin;

				textAreaStyle.focused = style;
				textAreaStyle.active = style;
				textAreaStyle.onActive = style;
				textAreaStyle.hover = style;
				textAreaStyle.normal = style;
				textAreaStyle.onNormal = style;
			}
		}
		#endregion

		#region Create Window
		public static AutoCompleteWindow CreateWindow(Rect position, Func<CompletionInfo[], bool> onConfirm, CompletionEvaluator.CompletionSetting setting = null) {
			var window = CreateInstance(typeof(AutoCompleteWindow)) as AutoCompleteWindow;
			window.onConfirm = onConfirm;
			if(setting != null) {
				window.completionEvaluator = new CompletionEvaluator(setting);
			}
			else {
				window.completionEvaluator = new CompletionEvaluator();
			}
			window.ShowAsDropDown(position, new Vector2(200, 100));
			return window;
		}

		public static AutoCompleteWindow CreateWindow(Vector2 position, Func<CompletionInfo[], bool> onConfirm, CompletionEvaluator.CompletionSetting setting = null) {
			var window = CreateInstance(typeof(AutoCompleteWindow)) as AutoCompleteWindow;
			window.onConfirm = onConfirm;
			if(setting != null) {
				window.completionEvaluator = new CompletionEvaluator(setting);
			}
			else {
				window.completionEvaluator = new CompletionEvaluator();
			}
			Vector2 windowSize = new Vector2(300, 20);
			window.ShowAsDropDown(WindowUtility.MousePosToRect(position, Vector2.zero), windowSize);
			return window;
		}
		#endregion

		private TooltipWindow tooltipWindow;
		private Func<CompletionInfo[], bool> onConfirm;

		private TextEditor textEditor;

		private CompletionEvaluator completionEvaluator;
		private CompletionInfo[] completions;
		private CompletionInfo selectedCompletion;

		private TextField textField;
		private TreeView treeView;

		private float currentHeight;
		private float height = 200;
		private Vector2 startPosition;
		private int overrideIndex;
		private List<CompletionInfo> memberPaths;
		private int newOverrideIndex;
		private bool needUpdateTooltip;

		private void OnEnable() {
		}
		private void OnDisable() {
			if(tooltipWindow != null) {
				tooltipWindow.Close();
			}
		}

		private void OnGUI() {
			if(startPosition == Vector2.zero) {
				startPosition = position.position;
			}
			if(Event.current.type == EventType.Repaint) {
				height = Mathf.Max(textField.layout.height, 20);
				try {
					if(completions?.Length > 0 && treeView != null && treeView.itemsSource != null) {
						height += treeView.fixedItemHeight * treeView.itemsSource.Count + 20;
					}
				}
				catch { }
				height = Mathf.Min(height, 400);
				if(currentHeight != height) {
					currentHeight = height;
					ShowAsDropDown(new Rect(startPosition.x, startPosition.y, 0, 0), new Vector2(position.width, currentHeight));
				}
			}
			if(string.IsNullOrEmpty(textField.text) == false && textField.focusController.focusedElement != textField) {
				textField.Focus();
				textField.cursorIndex = textField.text.Length;
			}
			if(needUpdateTooltip) {
				DoUpdateTooltip();
			}
		}

		private void OnAutocompleteConfirm() {
			var confirmedInput = selectedCompletion;
			if(confirmedInput == null && completions?.Length > 0) {
				confirmedInput = completions[0];
			}
			var text = textField.text;
			string oldText = text;
			string str = confirmedInput.name.ToLower();
			while(str.Length > 0) {
				if(text.EndsWith(".")) {
					text += confirmedInput.name;
				}
				else if(text.ToLower().EndsWith(str)) {
					int index = text.ToLower().LastIndexOf(str);
					text = text.Remove(index);
					text += confirmedInput.name;
					break;
				}
				else {
					str = str.RemoveLast();
				}
			}
			//lastWord = text;
			//textEditor.MoveTextEnd();
			if(oldText.Equals(text)) {
				if(onConfirm != null) {
					completionEvaluator.Evaluate(memberPaths, confirmedInput, (infos) => {
						if(onConfirm(infos)) {
							Close();
						}
					});
				}
			}
			else {
				textField.value = text;
			}
		}


		private void CreateGUI() {
			var root = rootVisualElement;
			root.RegisterCallback<KeyDownEvent>(evt => {
				if(selectedCompletion != null && completions != null) {
					var index = Array.IndexOf(completions, selectedCompletion);
					if(index >= 0) {
						switch(evt.keyCode) {
							case KeyCode.UpArrow:
								if(index > 0) {
									selectedCompletion = completions[--index];
									treeView.RefreshItems();
									treeView.ScrollToItem(index);
								}
								UpdateTooltip();
								break;
							case KeyCode.DownArrow:
								if(index + 1 < completions.Length) {
									selectedCompletion = completions[++index];
									treeView.RefreshItems();
									treeView.ScrollToItem(index);
								}
								break;
						}
					}
				}
				else {
					switch(evt.keyCode) {
						case KeyCode.UpArrow:
							newOverrideIndex--;
							if(newOverrideIndex < 0) {
								newOverrideIndex = 0;
							}
							UpdateTooltip();
							break;
						case KeyCode.DownArrow:
							newOverrideIndex++;
							UpdateTooltip();
							break;
					}
				}
				if(evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Tab) {
					if(completions?.Length > 0) {
						OnAutocompleteConfirm();
					}
				}
				if(evt.keyCode == KeyCode.UpArrow || evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.Return) {
					evt.StopImmediatePropagation();
					evt.PreventDefault();
				}
			}, TrickleDown.TrickleDown);
			textField = new TextField();
			root.Add(textField);
			textField.RegisterValueChangedCallback(TextChanged);
			textField.Focus();
			treeView = new TreeView(() => {
				var element = new VisualElement();
				element.style.flexDirection = FlexDirection.Row;
				return element;
			}, (element, index) => {
				var completion = completions[index];

				//Cleanup
				foreach(var child in element.Children().ToArray()) {
					child.RemoveFromHierarchy();
				}
				var root = new VisualElement();
				root.style.flexDirection = FlexDirection.Row;
				root.StretchToParentWidth();
				root.RegisterCallback<MouseDownEvent>(evt => {
					selectedCompletion = completion;
					treeView.RefreshItems();
					if(evt.clickCount == 2) {
						OnAutocompleteConfirm();
					}
				});
				element.Add(root);

				if(completion == selectedCompletion) {
					root.style.backgroundColor = textField.textSelection.selectionColor;
				}
				Texture icon = null;
				var label = new Label();
				label.text = completions[index].name;
				if(completion.kind == CompletionKind.Type) {
					var type = completion.member as Type;
					if(type.IsClass) {
						icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.ClassIcon));
					}
					else if(type.IsInterface) {
						icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.InterfaceIcon));
					}
					else if(type.IsEnum) {
						icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.EnumIcon));
					}
					else {
						icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.StructureIcon));
					}
				}
				else {
					switch(completion.kind) {
						case CompletionKind.Namespace:
							icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.NamespaceIcon));
							break;
						case CompletionKind.Keyword:
							icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.KeywordIcon));
							break;
						case CompletionKind.Field:
						case CompletionKind.uNodeVariable:
							icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FieldIcon));
							break;
						case CompletionKind.Method:
						case CompletionKind.uNodeFunction:
							icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.MethodIcon));
							break;
						case CompletionKind.Property:
						case CompletionKind.uNodeProperty:
							icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.PropertyIcon));
							break;
						case CompletionKind.uNodeLocalVariable:
							icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.LocalVariableIcon));
							break;
						case CompletionKind.uNodeParameter:
							icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.LocalVariableIcon));
							break;
						default:
							icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.ExtensionIcon));
							break;
					}
				}
				var image = new Image();
				image.SetSize(new Vector2(20, 20));
				image.image = icon;
				root.Add(image);
				root.Add(label);
			});
			//To make sure we are handling selection manually
			treeView.selectionType = SelectionType.None;
			//Setup the item height
			treeView.fixedItemHeight = 20;
			treeView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
			treeView.showBorder = true;
			treeView.showAlternatingRowBackgrounds = AlternatingRowBackground.All;

			treeView.HideElement();
			treeView.SetEnabled(false);

			root.Add(treeView);
		}

		private void TextChanged(ChangeEvent<string> evt) {
			selectedCompletion = null;

			var text = evt.newValue;
			completionEvaluator.SetInput(text, completions => {
				this.completions = completionEvaluator.completions;
				this.memberPaths = completionEvaluator.memberPaths;

				treeView.HideElement();
				treeView.SetEnabled(false);
				if(completions != null && completions.Length > 0) {
					bool flag = true;
					if(completions.Length == 1) {
						if(/*completions[0].name.Trim() == text.Trim() ||*/
							completions[0].isSymbol ||
							completions[0].isDot ||
							completions[0].kind == CompletionKind.Literal) {
							flag = false;
						}
					}
					if(flag) {
						selectedCompletion = completions[0];
						//height += height * completions.Length + 3;
						//if(height > 300) {
						//	height = 300;
						//}
						var items = new List<TreeViewItemData<CompletionInfo>>(completions.Length);
						foreach(var completion in completions) {
							items.Add(new TreeViewItemData<CompletionInfo>(completion.GetHashCode(), completion, null));
						}
						if(items.Count > 0) {
							treeView.SetEnabled(true);
							treeView.ShowElement();
							treeView.SetRootItems(items);
							treeView.Rebuild();
							//SetExpands(container, treeView);
						}
					}
				}
				UpdateTooltip();
			});
		}

		private void UpdateTooltip() {
			needUpdateTooltip = true;
		}

		public void DoUpdateTooltip() {
			overrideIndex = newOverrideIndex;
			bool closeTooltip = true;
			MethodBase methodBase = null;
			int numOfOverload = 0;
			if(memberPaths?.Count > 0) {
				bool flag = false;
				for(int i = memberPaths.Count - 1; i > 0; i--) {
					var mPath = memberPaths[i];
					if(mPath.isSymbol) {
						switch(mPath.name) {
							case "(":
							case "<": {//For constructor, function and genric.
								var member = memberPaths[i - 1];
								if(member.member is MethodInfo) {
									MethodInfo[] memberInfos = null;
									if(member.member.ReflectedType != null) {
										memberInfos = member.member.ReflectedType.GetMethods();
									}
									else if(member.member.DeclaringType != null) {
										memberInfos = member.member.DeclaringType.GetMethods();
									}
									if(memberInfos != null) {
										memberInfos = memberInfos.Where(m =>
											m.Name.Equals(member.name)).ToArray();
										if(memberInfos != null && memberInfos.Length > 0) {
											if(overrideIndex + 1 > memberInfos.Length) {
												overrideIndex = memberInfos.Length - 1;
												newOverrideIndex = overrideIndex;
											}
											methodBase = memberInfos[overrideIndex];
											numOfOverload = memberInfos.Length;
											//Update the member to the current overloaded method
											mPath.member = methodBase;
										}
									}
								}
								else if(member.member is ConstructorInfo) {
									ConstructorInfo[] memberInfos = null;
									if(member.member.ReflectedType != null) {
										memberInfos = member.member.ReflectedType.GetConstructors(
											BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
									}
									else if(member.member.DeclaringType != null) {
										memberInfos = member.member.DeclaringType.GetConstructors(
											BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
									}
									if(memberInfos != null) {
										memberInfos = memberInfos.Where(m =>
											m.Name.Equals(member.name)).ToArray();
										if(memberInfos != null && memberInfos.Length > 0) {
											if(overrideIndex + 1 > memberInfos.Length) {
												overrideIndex = memberInfos.Length - 1;
												newOverrideIndex = overrideIndex;
											}
											methodBase = memberInfos[overrideIndex];
											numOfOverload = memberInfos.Length;
											//Update the member to the current overloaded method
											mPath.member = methodBase;
										}
									}
								}
								break;
							}
							case "["://For indexer

								break;
							case ")":
							case ">":
							case "]":
								flag = true;
								break;
						}
					}
					if(methodBase != null || flag)
						break;
				}
			}
			List<GUIContent> contents = new List<GUIContent>();
			if(selectedCompletion != null) {
				switch(selectedCompletion.kind) {
					case CompletionKind.Type: {
						Type type = selectedCompletion.member as Type;
						if(type != null) {
							contents.Add(new GUIContent(type.PrettyName(true), uNodeEditorUtility.GetIcon(type)));
						}
						break;
					}
					case CompletionKind.Field: {
						FieldInfo field = selectedCompletion.member as FieldInfo;
						if(field != null) {

						}
						break;
					}
					case CompletionKind.Property: {
						PropertyInfo property = selectedCompletion.member as PropertyInfo;
						if(property != null) {

						}
						break;
					}
					case CompletionKind.Method: {
						MethodInfo method = selectedCompletion.member as MethodInfo;
						if(method != null && method != methodBase) {
							ResolveMethodTooltip(method, numOfOverload, contents);
						}
						break;
					}
				}
				if(contents.Count > 0) {
					contents.Add(new GUIContent(""));
				}
			}
			if(methodBase != null) {
				ResolveMethodTooltip(methodBase, numOfOverload, contents);
			}

			if(contents.Count > 0) {
				GUIContent c = null;
				for(int i = 0; i < contents.Count; i++) {
					if(c == null ||
						uNodeEditorUtility.RemoveHTMLTag(c.text).Length <
						uNodeEditorUtility.RemoveHTMLTag(contents[i].text).Length) {
						c = contents[i];
					}
				}
				float width = uNodeGUIStyle.RichLabel.CalcSize(c).x + 20;
				if(position.x + position.width + width <= Screen.currentResolution.width) {
					tooltipWindow = TooltipWindow.Show(new Vector2(position.x + position.width, position.y), contents, width);
				}
				else {
					tooltipWindow = TooltipWindow.Show(new Vector2(position.x - width, position.y), contents, width);
				}
				closeTooltip = false;
			}
			if(closeTooltip && tooltipWindow != null) {
				tooltipWindow.Close();
			}
		}

		private void ResolveMethodTooltip(MethodBase methodBase, int numOfOverload, List<GUIContent> contents) {
			if(methodBase is MethodInfo) {
				contents.Add(new GUIContent(
					EditorReflectionUtility.GetPrettyMethodName(methodBase as MethodInfo),
					uNodeEditorUtility.GetIcon(methodBase)));
			}
			else if(methodBase is ConstructorInfo) {
				contents.Add(new GUIContent(
					EditorReflectionUtility.GetPrettyConstructorName(methodBase as ConstructorInfo),
					uNodeEditorUtility.GetIcon(methodBase)));
			}
			var mType = ReflectionUtils.GetMemberType(methodBase);
			#region Docs
			if(XmlDoc.hasLoadDoc) {
				XmlElement documentation = XmlDoc.XMLFromMember(methodBase);
				if(documentation != null) {
					contents.Add(new GUIContent("Documentation ▼ " + documentation["summary"].InnerText.Trim().AddLineInFirst()));
				}
				var parameters = methodBase.GetParameters();
				if(parameters.Length > 0) {
					for(int x = 0; x < parameters.Length; x++) {
						Type PType = parameters[x].ParameterType;
						if(PType != null) {
							contents.Add(new GUIContent(parameters[x].Name + " : " +
								uNodeUtility.GetDisplayName(PType),
								uNodeEditorUtility.GetTypeIcon(PType)));
							if(documentation != null && documentation["param"] != null) {
								XmlNode paramDoc = null;
								XmlNode doc = documentation["param"];
								while(doc.NextSibling != null) {
									if(doc.Attributes["name"] != null && doc.Attributes["name"].Value.Equals(parameters[x].Name)) {
										paramDoc = doc;
										break;
									}
									doc = doc.NextSibling;
								}
								if(paramDoc != null && !string.IsNullOrEmpty(paramDoc.InnerText)) {
									contents.Add(new GUIContent(paramDoc.InnerText.Trim()));
								}
							}
						}
					}
				}
			}
			#endregion
			//contents.Add(new GUIContent("Return	: " + mType.PrettyName(true), uNodeEditorUtility.GetTypeIcon(mType)));
			if(numOfOverload > 0)
				contents.Add(new GUIContent("▲ " + (overrideIndex + 1).ToString() + " of " + numOfOverload + " ▼"));
		}
	}
}