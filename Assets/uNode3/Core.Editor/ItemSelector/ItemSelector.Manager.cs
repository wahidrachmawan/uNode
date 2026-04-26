using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.IMGUI.Controls;

#if UNITY_6000_2_OR_NEWER
using TView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#else
using TView = UnityEditor.IMGUI.Controls.TreeView;
using TViewItem = UnityEditor.IMGUI.Controls.TreeViewItem;
using TViewState = UnityEditor.IMGUI.Controls.TreeViewState;
#endif

namespace MaxyGames.UNode.Editors {
	public partial class ItemSelector {
		public class NestedTreeData {
			public TViewItem tree;
			public Vector2 scrollPos;
			public string searchString;
			public RelevanceData relevanceData;
			public int selectedTree;
		}

		public class RelevanceData {
			public List<TViewItem> originalSearchTrees;
		}

		public sealed class Manager : TView, IDisposable {
			public List<NestedTreeData> deepTrees = new List<NestedTreeData>();
			public bool isDeep => deepTrees.Count > 0;
			public TViewItem lastTree => deepTrees.LastOrDefault().tree;

			private Dictionary<int, bool> expandedStates = new Dictionary<int, bool>();
			private Dictionary<int, bool> nonSearchExpandeds = new Dictionary<int, bool>();
			private Dictionary<int, TreeHightlight> treeHightlights = new Dictionary<int, TreeHightlight>();

			private List<TViewItem> searchedTrees;
			private List<TViewItem> deepItems;

			public List<TViewItem> _treeViews;
			public List<TViewItem> treeViews {
				get => _treeViews;
				set {
					_treeViews = value;
					searchedTrees = null;
				}
			}

			private TooltipWindow tooltipWindow;
			private TViewItem hoveredTree;
			private TViewItem m_selectedTree;
			public TViewItem selectedTree {
				get {
					return m_selectedTree;
				}
				set {
					m_selectedTree = value;
					if(value != null) {
						try {
							FrameItem(value.id);
						}
						catch {
							//Ignore error when frame item, since it can cause error when the tree is not fully loaded yet.
						}
					}
				}
			}
			private NestedTreeData m_lastNestedData;

			public RelevanceData relevanceData;
			private string _searchString;
			public new string searchString {
				get => _searchString;
				set {
					treeHightlights.Clear();
					relevanceData = null;
					if(_searchString != value) {
						_searchString = value;
						ReloadInBackground();
					}
				}
			}

			public new bool hasSearch => !string.IsNullOrEmpty(searchString);
			public bool isReloading { get; private set; }

			public Data editorData { get; } = new Data();
			public FilterAttribute filter => editorData.filter;
			public EditorWindow window => editorData.window;

			public Manager(TViewState state) : base(state) {
				showAlternatingRowBackgrounds = true;
				showBorder = true;
				editorData.manager = this;
			}

			#region GUI
			protected override void SingleClickedItem(int id) {
				var tree = GetRows().FirstOrDefault(i => i.id == id);
				if(editorData.CanSelectTree(tree)) {
					SelectTree(tree);
				}
			}

			protected override void ContextClickedItem(int id) {
				var tree = GetRows().FirstOrDefault(i => i.id == id);
				if(editorData.CanNextTree(tree)) {
					NextTree(tree);
				}
			}

			protected override void KeyEvent() {
				OnKeyEvent();
			}

			public void OnKeyEvent() {
				Event evt = Event.current;
				if(evt.type == EventType.KeyDown) {
					TViewItem tree = selectedTree;
					if(tree != null) {
						if(evt.keyCode == KeyCode.Return) {
							if(editorData.CanSelectTree(tree)) {
								SelectTree(tree);
								evt.Use();
							}
						}
						else if(evt.keyCode == KeyCode.RightArrow) {
							if(editorData.CanNextTree(tree)) {
								NextTree(tree);
								evt.Use();
							}
							else if(CanChangeExpandTree(tree)) {
								if(!IsExpanded(tree.id)) {
									SetExpanded(tree.id, true);
									ReloadInBackground();
									evt.Use();
								}
							}
						}
						else if(evt.keyCode == KeyCode.LeftArrow) {
							if(CanChangeExpandTree(tree)) {
								if(IsExpanded(tree.id)) {
									SetExpanded(tree.id, false);
									Reload();
									evt.Use();
									return;
								}
							}
							if(tree.depth == 0 && isDeep) {
								Back();
								evt.Use();
							}
						}
						else if(evt.keyCode == KeyCode.DownArrow) {
							OffsetSelection(1, tree);
						}
						else if(evt.keyCode == KeyCode.UpArrow) {
							OffsetSelection(-1, tree);
						}
						//if(selectedTree is IRelevanceItem relevance) {
						//	Debug.Log(relevance.Score + " : " + selectedTree.GetType());
						//}
					}
					else {
						if(evt.keyCode == KeyCode.DownArrow) {
							OffsetSelection(1, 0);
						}
						else if(evt.keyCode == KeyCode.UpArrow) {
							OffsetSelection(-1, 0);
						}
					}
				}
			}

			public void OffsetSelection(int offset, int id) {
				var rows = GetRows();
				if(rows.Count != 0) {
					var tree = rows.FirstOrDefault(t => t.id == id);
					if(tree == null) {
						tree = rows.FirstOrDefault();
					}
					if(tree != null) {
						OffsetSelection(offset, tree);
					}
				}
			}

			public void OffsetSelection(int offset, TViewItem item) {
				var rows = GetRows();
				if(rows.Count != 0) {
					var tree = rows.FirstOrDefault(t => t == item);
					if(tree == null) {
						tree = rows.FirstOrDefault(t => t.id == item.id) ?? rows.FirstOrDefault();
					}
					if(tree != null) {
						int indexOfID = rows.IndexOf(tree);
						int num = Mathf.Clamp(indexOfID + offset, 0, rows.Count - 1);
						while(rows[num] == null || rows[num] is ICategoryTreeItem) {
							if(offset > 0) {
								if(num + 1 < rows.Count - 1)
									num++;
								else {
									num = indexOfID;
									break;
								}
							}
							else {
								if(num > 0)
									num--;
								else {
									num = indexOfID;
									break;
								}
							}
						}
						Event.current.Use();
						selectedTree = rows[num];
					}
				}
			}

			protected override void RowGUI(RowGUIArgs args) {
				Event evt = Event.current;
				if(args.rowRect.Contains(evt.mousePosition)) {
					if(evt.type == EventType.MouseMove) {
						m_selectedTree = args.item;
					}
					//SetFocus();
				}
				if(args.item == selectedTree) {
					bool isDark = EditorGUIUtility.isProSkin;

					Color bgColor = isDark
						? new Color(0.24f, 0.48f, 0.90f, 0.5f)  
						: new Color(0.24f, 0.49f, 0.90f, 0.5f);
					EditorGUI.DrawRect(args.rowRect, bgColor);
				}
				if(editorData.RowRepaintGUI(new ItemRowGUIArgs() { item = args.item, label = args.label, row = args.row, rowRect = args.rowRect, manager = this })) {
					//In case there's custom row gui.
					return;
				}
				if(evt.type == EventType.Repaint) {
					#region Tooltip
					if(args.rowRect.Contains(evt.mousePosition)) {
						if(hoveredTree != args.item) {
							var contents = Utility.GetTooltipContents(args.item, filter?.OnlyGetType == true);
							if(contents.Count > 0) {
								if(window.position.x + window.position.width + 300 <= Screen.currentResolution.width) {
									tooltipWindow = TooltipWindow.Show(new Vector2(window.position.x + window.position.width, window.position.y), contents);
								}
								else {
									tooltipWindow = TooltipWindow.Show(new Vector2(window.position.x - 300, window.position.y), contents);
								}
							}
							else if(tooltipWindow != null) {
								tooltipWindow.Close();
							}
							hoveredTree = args.item;
						}
					}
					#endregion
				}
				#region Draw Row
				Rect labelRect = args.rowRect;
				if(relevanceData != null) {
					labelRect.height /= 2;
				}
				var indent = GetContentIndent(args.item);
				labelRect.x += indent - 16;
				labelRect.width -= indent - 16;
				if(CanChangeExpandTree(args.item)) {
					var tree = args.item;
					Rect pos = labelRect;
					pos.width = pos.height;
					labelRect.x += pos.width;
					labelRect.width -= pos.width;
					var expand = IsExpanded(tree.id);
					var flag = GUI.Toggle(pos, expand, GUIContent.none, EditorStyles.foldout);
					if(flag != expand) {
						SetExpanded(tree.id, flag);
						if(flag) {
							ReloadInBackground();
						}
						else {
							Reload();
						}
					}
				}
				if(args.item is SelectorCategoryTreeView) {
					var tree = args.item as SelectorCategoryTreeView;
					var flag = GUI.Toggle(labelRect, tree.expanded, new GUIContent(args.label), "Button");
					if(flag != tree.expanded) {
						tree.expanded = flag;
						SetExpanded(tree.id, flag);
						if(flag && hasSearch) {
							tree.children = treeSearch.Search(tree.children, searchString, editorData.searchKind, editorData.searchFilter);
						}
						Reload();
					}
				}
				else {
					bool canSelect = editorData.CanSelectTree(args.item);
					bool canNext = editorData.CanNextTree(args.item);
					if(evt.type == EventType.Repaint) {
						if(editorData.RowRepaintGUI(new ItemRowGUIArgs() { item = args.item, label = args.label, row = args.row, rowRect = args.rowRect, manager = this })) {
							//In case there's custom row gui.
							return;
						}
						if(canSelect) {
							Rect pos = labelRect;
							pos.x += labelRect.width - 25;
							pos.width = 15;

							var selectIcon = editorData.selectIconCallback?.Invoke(args.item);
							if(selectIcon != null) {
								GUI.DrawTexture(pos, selectIcon);
							}
							else {
								uNodeGUIStyle.itemSelect.Draw(pos, GUIContent.none, false, false, false, false);
							}
						}
						if(canNext) {
							Rect pos = labelRect;
							pos.x += labelRect.width - 15;
							pos.width = 15;
							uNodeGUIStyle.itemNext.Draw(pos, GUIContent.none, false, false, false, false);
						}
						if(canSelect) {
							labelRect.width -= 25;
						}
						else if(canNext) {
							labelRect.width -= 15;
						}
						if(args.rowRect.Contains(evt.mousePosition)) {
							if(args.item is MemberTreeView) {
								if(!canSelect && canNext) {
									labelRect.width -= 10;
								}
								Rect pos = labelRect;
								pos.width = 15;
								pos.x += labelRect.width - pos.width;
								var member = (args.item as MemberTreeView).member;
								bool isFavorited = uNodeEditor.SavedData.HasFavorite(member);
								GUI.DrawTexture(pos, isFavorited ? uNodeGUIStyle.favoriteIconOn : uNodeGUIStyle.favoriteIconOff);
								labelRect.width -= pos.width;
							}
							else if(args.item is NamespaceTreeView) {
								if(!canSelect && canNext) {
									labelRect.width -= 10;
								}
								Rect pos = labelRect;
								pos.width = 15;
								pos.x += labelRect.width - pos.width;
								var ns = (args.item as NamespaceTreeView).Namespace;
								bool isFavorited = uNodeEditor.SavedData.favoriteNamespaces.Contains(ns);
								GUI.DrawTexture(pos, isFavorited ? uNodeGUIStyle.favoriteIconOn : uNodeGUIStyle.favoriteIconOff);
								labelRect.width -= pos.width;
							}
							else if(args.item is SelectorCustomTreeView) {
								var tree = args.item as SelectorCustomTreeView;
								if(tree.item is IFavoritable fav && fav.CanSetFavorite()) {
									if(!canSelect && canNext) {
										labelRect.width -= 10;
									}
									Rect pos = labelRect;
									pos.width = 15;
									pos.x += labelRect.width - pos.width;
									bool isFavorited = fav.IsFavorited();
									GUI.DrawTexture(pos, isFavorited ? uNodeGUIStyle.favoriteIconOn : uNodeGUIStyle.favoriteIconOff);
									labelRect.width -= pos.width;
								}
							}
						}
						var icon = GetIcon(args.item);
						var icon2 = GetSecondIcon(args.item);
						if(icon2 != null) {
							if(icon == null) {
								icon = icon2;
							}
							else {
								Rect pos = labelRect;
								pos.width = pos.height;
								labelRect.x += pos.width;
								labelRect.width -= pos.width;
								GUI.DrawTexture(pos, icon);
								icon = icon2;
							}
						}
						if(icon != null) {
							GUI.DrawTexture(new Rect(labelRect.x, labelRect.y, labelRect.height, labelRect.height), icon);
							labelRect.x += labelRect.height;
						}
						GUIContent label = null;
						if(args.item is IDisplayName) {
							string lbl = (args.item as IDisplayName).DisplayName;
							if(string.IsNullOrEmpty(lbl) == false) {
								label = new GUIContent(lbl);
							}
						}
						if(label == null) {
							label = new GUIContent(args.label);
						}
						if(IsStaticTree(args.item)) {
							DrawLabel(args.item, uNodeGUIStyle.itemStatic, labelRect, label);
						}
						else {
							DrawLabel(args.item, uNodeGUIStyle.itemNormal, labelRect, label);
						}
					}
					else if(evt.type == EventType.MouseDown) {
						if(evt.button == 0) {
							if(canSelect) {
								labelRect.width -= 25;
							}
							else if(canNext) {
								labelRect.width -= 15;
							}
							//Favorite
							if(args.rowRect.Contains(evt.mousePosition)) {
								if(args.item is MemberTreeView) {
									if(!canSelect && canNext) {
										labelRect.width -= 10;
									}
									Rect pos = labelRect;
									pos.width = 15;
									pos.x += labelRect.width - pos.width;
									var member = (args.item as MemberTreeView).member;
									bool isFavorited = uNodeEditor.SavedData.HasFavorite(member);
									if(pos.Contains(evt.mousePosition)) {
										if(isFavorited) {
											uNodeEditor.SavedData.RemoveFavorite(member);
										}
										else {
											uNodeEditor.SavedData.AddFavorite(member);
										}
										evt.Use();
									}
								}
								else if(args.item is NamespaceTreeView) {
									Rect pos = labelRect;
									pos.width = 15;
									pos.x += labelRect.width - pos.width;
									var ns = (args.item as NamespaceTreeView).Namespace;
									bool isFavorited = uNodeEditor.SavedData.favoriteNamespaces.Contains(ns);
									if(pos.Contains(evt.mousePosition)) {
										if(isFavorited) {
											uNodeEditor.SavedData.favoriteNamespaces.Remove(ns);
										}
										else {
											uNodeEditor.SavedData.favoriteNamespaces.Add(ns);
										}
										uNodeEditor.SaveOptions();
										evt.Use();
									}
								}
								else if(args.item is SelectorCustomTreeView) {
									var tree = args.item as SelectorCustomTreeView;
									if(tree.item is IFavoritable fav && fav.CanSetFavorite()) {
										if(!canSelect && canNext) {
											labelRect.width -= 10;
										}
										Rect pos = labelRect;
										pos.width = 15;
										pos.x += labelRect.width - pos.width;
										bool isFavorited = fav.IsFavorited();
										if(pos.Contains(evt.mousePosition)) {
											if(isFavorited) {
												fav.SetFavorite(false);
											}
											else {
												fav.SetFavorite(true);
											}
											evt.Use();
										}
									}
								}
							}
						}
					}
				}
				#endregion
				//base.RowGUI(args);
			}
			#endregion

			public void DrawLabel(TViewItem tree, GUIStyle style, Rect position, GUIContent label) {
				if(hasSearch) {
					if(!treeHightlights.TryGetValue(tree.id, out var hightlight)) {
						hightlight = new TreeHightlight();
						treeSearch.Hightlight(tree, searchString, editorData.searchKind, editorData.searchFilter, ref hightlight);
						treeHightlights[tree.id] = hightlight;
					}

					if(hightlight != null && hightlight.hightlight.Count > 0) {
						var st = new GUIStyle(style);
						st.border = new RectOffset();
						st.padding = new RectOffset();
						st.margin = new RectOffset();
						st.overflow = new RectOffset();
						st.contentOffset = new Vector2();
						if(editorData.searchKind == SearchKind.Relevant) {
							foreach(var pair in hightlight.hightlight) {
								var first = pair.Key;
								var last = pair.Value;
								if(first >= 0 && last <= label.text.Length && (last - first) > 0) {
									string str = label.text;
									string s1 = str[..first];
									string s2 = str[first..last];
									var r1 = st.CalcSize(new GUIContent(s1));
									var r2 = st.CalcSize(new GUIContent(s2));
									GUI.DrawTexture(new Rect(position.x + r1.x, position.y, r2.x, position.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.24f, 0.49f, 0.91f, 0.5f), 0, 0);
								}
							}
						}
						else {
							foreach(var pair in hightlight.hightlight) {
								var first = pair.Key;
								var last = pair.Value;
								if(first >= 0 && (first + last) <= label.text.Length) {
									string str = label.text;
									string s1 = str[..first];
									string s2 = str.Substring(first, last);
									var r1 = st.CalcSize(new GUIContent(s1));
									var r2 = st.CalcSize(new GUIContent(s2));
									GUI.DrawTexture(new Rect(position.x + r1.x, position.y, r2.x, position.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.24f, 0.49f, 0.91f, 0.5f), 0, 0);
								}
							}
						}
					}
				}
				style.Draw(position, label, false, false, false, false);

				if(relevanceData != null) {
					style = EditorStyles.miniLabel;
					position.x = 16;
					position.y += position.height;

					TViewItem originalTree = null;
					void TraverseOriginalTree(TViewItem t) {
						if(t.id == tree.id) {
							originalTree = t;
							return;
						}
						if(t.children != null) {
							foreach(var c in t.children) {
								TraverseOriginalTree(c);
								if(originalTree != null) {
									return;
								}
							}
						}
					}
					foreach(var child in relevanceData.originalSearchTrees) {
						TraverseOriginalTree(child);
						if(originalTree != null) {
							break;
						}
					}
					if(originalTree != null) {
						var list = StaticListPool<GUIContent>.Allocate();

						if(tree is MemberTreeView memberTree) {
							if(memberTree.member is Type) {
								var type = memberTree.member as Type;
								if(!string.IsNullOrEmpty(type.Namespace)) {
									list.Add(new GUIContent(type.Namespace, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.NamespaceIcon))));
								}
							}
							else {
								var type = ReflectionUtils.GetDeclaringType(memberTree.member);
								list.Add(new GUIContent(type.PrettyName(true), uNodeEditorUtility.GetTypeIcon(type)));
							}
						}

						var parent = originalTree.parent;
						while(parent != null) {
							list.Add(new GUIContent(parent.displayName, parent.icon));
							parent = parent.parent;
							if(list.Count > 10) {
								break;
							}
						}

						var next = style.CalcSize(new GUIContent(">"));
						for(int i = list.Count - 1; i >= 0; i--) {
							var content = list[i];
							var size = style.CalcSize(content);
							style.Draw(new Rect(position.x, position.y, size.x, position.height), content, false, false, false, false);
							position.x += size.x;
							if(i > 0 && i + 1 < list.Count) {
								style.Draw(new Rect(position.x, position.y, next.x, position.height), new GUIContent(">"), false, false, false, false);
								position.x += next.x;
							}
						}
						StaticListPool<GUIContent>.Free(list);
					}
					else {
						if(tree is MemberTreeView memberTree) {
							if(memberTree.member is Type) {
								var type = memberTree.member as Type;
								if(!string.IsNullOrEmpty(type.Namespace)) {
									var content = new GUIContent(type.Namespace, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.NamespaceIcon)));
									style.Draw(position, content, false, false, false, false);
								}
							}
							else {
								var type = ReflectionUtils.GetDeclaringType(memberTree.member);
								var content = new GUIContent(type.PrettyName(true), uNodeEditorUtility.GetTypeIcon(type));
								style.Draw(position, content, false, false, false, false);
							}
						}
					}
				}
			}

			#region Select & Next
			private void DoAddNestedTree(TViewItem tree) {
				deepTrees.Add(new NestedTreeData() {
					tree = tree,
					searchString = searchString,
					scrollPos = state.scrollPos,
					relevanceData = relevanceData,
					selectedTree = selectedTree.id,
				});
				m_lastNestedData = null;
			}

			public void SelectTree(TViewItem tree) {
				{
					var map = PersistenceData.Instance.itemUsedCount;
					map.TryGetValue(tree.id, out var usedCount);
					map[tree.id] = ++usedCount;

					if(string.IsNullOrWhiteSpace(searchString) == false) {
						var map2 = PersistenceData.Instance.searchedItemUsedCount;
						map2.TryGetValue((searchString, tree.id), out var searchedUsedCount);
						map2[(searchString, tree.id)] = ++searchedUsedCount;
					}
				}
				if(tree is TypeTreeView) {
					var type = (tree as TypeTreeView).type;

					//Auto resolve generic type
					var resolvedMember = ReflectionUtils.AutoResolveGenericMember(type) as Type;
					if(resolvedMember != null) {
						DoAddNestedTree(new TypeTreeView(resolvedMember, tree.id, tree.depth));
						SelectDeepTrees();
						GUIUtility.ExitGUI();
					}

					if(ResolveGenericItem(type, (mInfo) => {
						DoAddNestedTree(new TypeTreeView(mInfo as Type, tree.id, tree.depth));
						SelectDeepTrees();
						GUIUtility.ExitGUI();
					}, filter, editorData.targetObject, window)) { return; }
					DoAddNestedTree(tree);
					SelectDeepTrees();
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;

					//Auto resolve generic type
					var resolvedMember = ReflectionUtils.AutoResolveGenericMember(item.member);
					if(resolvedMember != null) {
						DoAddNestedTree(new MemberTreeView(resolvedMember, item.id, item.depth) { instance = item.instance });
						SelectDeepTrees();
						GUIUtility.ExitGUI();
					}

					if(ResolveGenericItem(item.member, (mInfo) => {
						DoAddNestedTree(new MemberTreeView(mInfo, item.id, item.depth) { instance = item.instance });
						SelectDeepTrees();
						GUIUtility.ExitGUI();
					}, filter, editorData.targetObject, window)) { return; }

					DoAddNestedTree(tree);
					SelectDeepTrees();
				}
				else if(tree is SelectorMemberTreeView) {
					var item = tree as SelectorMemberTreeView;
					editorData.Select(item.member);
				}
				else if(tree is SelectorCustomTreeView) {
					var item = tree as SelectorCustomTreeView;
					if(item.item != null) {
						if(item.item is ItemReflection ri) {
							if(ri.item != null) {
								var member = ri.item.memberInfo;

								//Auto resolve generic type
								var resolvedMember = ReflectionUtils.AutoResolveGenericMember(member);
								if(resolvedMember != null) {
									if(member is Type) {
										DoAddNestedTree(new TypeTreeView(resolvedMember as Type, tree.id, tree.depth));
									}
									else {
										DoAddNestedTree(new MemberTreeView(resolvedMember, tree.id, tree.depth) {
											instance = ri.item.instance
										});
									}
									SelectDeepTrees();
									GUIUtility.ExitGUI();
								}

								if(ResolveGenericItem(member, (mInfo) => {
									if(member is Type) {
										DoAddNestedTree(new TypeTreeView(mInfo as Type, tree.id, tree.depth));
									}
									else {
										DoAddNestedTree(new MemberTreeView(mInfo, tree.id, tree.depth) {
											instance = ri.item.instance
										});
									}
									SelectDeepTrees();
									GUIUtility.ExitGUI();
								}, filter, editorData.targetObject, window)) { return; }
								if(member is Type) {
									DoAddNestedTree(new TypeTreeView(member as Type, tree.id, tree.depth));
								}
								else {
									DoAddNestedTree(new MemberTreeView(member, tree.id, tree.depth) {
										instance = ri.item.instance
									});
								}
								SelectDeepTrees();
							}
						}
						else {
							item.item.OnSelect(editorData);
						}
					}
					else {
						throw new Exception();
					}
					window?.Close();
				}
				else if(tree is SelectorGroupedTreeView || tree is NamespaceTreeView) {
					NextTree(tree);
					return;
				}
				GUIUtility.ExitGUI();
			}

			public void NextTree(TViewItem tree) {
				if(tree is TypeTreeView) {
					if(ResolveGenericItem((tree as TypeTreeView).type, (mInfo) => {
						DoNextTree(new TypeTreeView(mInfo as Type, tree.id, tree.depth));
					}, filter, editorData.targetObject, window)) { return; }
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;
					if(ResolveGenericItem(item.member, (mInfo) => {
						DoNextTree(new MemberTreeView(mInfo, item.id, item.depth) { instance = item.instance });
					}, filter, editorData.targetObject, window)) { return; }
				}
				DoNextTree(tree);
			}

			private void DoNextTree(TViewItem tree) {
				DoAddNestedTree(tree);
				_searchString = string.Empty;
				relevanceData = null;
				GUI.FocusControl(null);
				editorData.searchField.SetFocus();
				Reload();
				OffsetSelection(0, 0);
				window?.Focus();
			}

			public void Back() {
				if(isDeep) {
					var lastData = deepTrees.Last();
					m_lastNestedData = lastData;
					deepTrees.RemoveAt(deepTrees.Count - 1);
					searchString = lastData.searchString;
					editorData.searchField.SetFocus();
					state.scrollPos = lastData.scrollPos;
					relevanceData = lastData.relevanceData;
					Reload();
				}
			}

			public void SelectDeepTrees() {
				List<TViewItem> items = new List<TViewItem>();
				foreach(var data in deepTrees) {
					var tree = data.tree;
					if(tree is MemberTreeView || tree is SelectorCustomTreeView) {
						items.Add(tree);
					}
				}
				for(int i = 0; i < items.Count; i++) {
					if(i != 0 && items[i] is TypeTreeView) {
						items.RemoveAt(i - 1);
						i--;
					}
				}
				if(items.Count > 0) {
					if(items[0] is MemberTreeView mTree && items[0] is not TypeTreeView) {
						items.Insert(0, new TypeTreeView(mTree.member.DeclaringType));
					}
					else if(items[0] is SelectorCustomTreeView customTreeView && customTreeView.item is ItemReflection itemReflection) {
						if(itemReflection.item.memberInfo != null) {
							items.Insert(0, new TypeTreeView(itemReflection.item.memberInfo.DeclaringType));
						}
					}
				}
				var lastItem = items.LastOrDefault();
				if(lastItem is TypeTreeView) {
					var type = (lastItem as TypeTreeView).type;
					if(filter.Types?.Count == 1 && filter.Types[0] == typeof(Type) && !(type is RuntimeType)) {
						MemberData val = MemberData.CreateFromType(type);
						editorData.Select(val);
						return;
					}
					else if(filter.IsValidTarget(MemberData.TargetType.Values)) {
						MemberData val = MemberData.Default(type);
						editorData.Select(val);
						return;
					}
				}
				if(lastItem is MemberTreeView) {
					var member = (lastItem as MemberTreeView).member;
					if(member != null && !(member is IRuntimeMember)) {
						uNodeEditor.SavedData.AddRecentItem(new uNodeEditor.uNodeEditorData.RecentItem() {
							info = member,
						});
					}
				}
				var members = new List<MemberData>();
				foreach(var item in items) {
					if(item is MemberTreeView) {
						var tree = item as MemberTreeView;
						var member = tree.member;
						if(member is ConstructorInfo) {
							editorData.Select(MemberData.CreateFromMember(member));
							return;
						}
						if(member is IRuntimeMember) {
							members.Add(new MemberData(member));
						}
						else {
							if(members.Count == 1 && members[0].targetType.IsTargetingUNode() == false &&
								(member is FieldInfo || member is MethodInfo || member is PropertyInfo || member is ConstructorInfo)) {
								//Make sure we use the declared type.
								members[0] = MemberData.CreateFromMember(member.DeclaringType);
							}
							members.Add(new MemberData(member) { instance = tree.instance });
						}
					}
					else if(item is SelectorMemberTreeView) {
						var tree = item as SelectorMemberTreeView;
						members.Add(tree.member);
					}
					else if(item is SelectorCustomTreeView) {
						var tree = item as SelectorCustomTreeView;
						var next = tree.item.GetMemberForNextItem(editorData);
						if(next != null) {
							members.Add(next);
						}
						else {
							return;
						}
					}
				}
				if(HasRuntimeType(members)) {
					if(members.Count == 1) {
						editorData.Select(members[0]);
						return;
					}
				}
				var itemDatas = new List<MemberData.ItemData>();
				var mData = new MemberData();
				bool flag = false;
				bool flag2 = true;
				for(int i = 0; i < members.Count; i++) {
					var member = members[i];
					if(i == 0) {
						mData.isStatic = member.isStatic;
						mData.targetType = member.targetType;
						if(!member.isStatic) {
							mData.instance = member.instance;
						}
						if(member.targetType == MemberData.TargetType.NodePort) {
							mData.instance = member;
							mData.startType = member.startType;
							break;
						}
						else if(member.targetType == MemberData.TargetType.Constructor) {
							mData = member;
							itemDatas.AddRange(member.Items);
							break;
						}
						else if(member.targetType == MemberData.TargetType.Values) {
							mData.instance = member;
							mData.startType = member.startType;
							mData.isStatic = false;
							break;
						}
						else if(member.targetType == MemberData.TargetType.uNodeType) {
							mData = member;
							continue;
						}
						else {
							mData.startType = member.startType ?? member.type;
						}
					}
					if(!flag) {
						if(!member.IsTargetingType) {
							if(flag2) {
								mData.isStatic = member.isStatic;
								flag2 = false;
							}
							mData.targetType = member.targetType;
							if(mData.instance == null && member.instance != null) {
								mData.instance = member.instance;
							}
							if(member.IsTargetingUNode && member.targetType != MemberData.TargetType.NodePort) {
								flag = true;
							}
						}
						else {
							mData.isStatic = true;
						}
					}
					if(member.isDeepTarget) {
						itemDatas.AddRange(member.Items);
						//mData.name += member.name.AddFirst(".", !string.IsNullOrEmpty(mData.name));
					}
					else {
						if(itemDatas.Count == 0 && (mData.isStatic || !mData.IsTargetingUNode)) {
							itemDatas.AddRange(member.Items);
						}
						else {
							itemDatas.Add(member.Items[member.Items.Length - 1]);
						}
					}
					mData.type = member.type;
				}
				{
					mData.Items = itemDatas.ToArray();
				}
				if(mData.targetType == MemberData.TargetType.Constructor) {
					mData.isStatic = true;
				}
				if(!mData.isStatic) {
					var firstTree = items[0];
					if(firstTree is MemberTreeView) {
						var instance = (firstTree as MemberTreeView).instance;
						if(instance == null && items[1] is MemberTreeView) {
							instance = (items[1] as MemberTreeView).instance;
						}
						if(instance != null) {
							mData.instance = instance;
						}
					}
				}
				editorData.Select(mData);
			}

			public void SelectGraphItem(GraphItem item) {
				if(item.function != null && item.function.genericParameters.Length > 0 && item.genericParameterTypes == null) {
					TypeItem[] defaultType = item.function.genericParameters.Select(p => new TypeItem(p.value,
						new FilterAttribute(this.filter) {
							Types = new List<Type>() { p.value },
							ArrayManipulator = true,
						})).ToArray();
					TypeBuilderWindow.Show(editorData.windowRect, editorData.targetObject, this.filter, delegate (MemberData[] types) {
						item.genericParameterTypes = types;
						SelectGraphItem(item);
					}, defaultType);
					GUIUtility.ExitGUI();
					return;
				}
				if(item.targetType == MemberData.TargetType.Self) {
					editorData.Select(new MemberData("this", item.type, item.targetType) { instance = item.targetObject });
					return;
				}
				var member = new MemberData();
				member.instance = item.GetInstance();
				MemberData.ItemData iData = null;
				if(item.function != null) {
					iData = new MemberData.ItemData() {
						name = item.function.name,
						reference = new FunctionRef(item.function),
					};
					GenericParameterData[] genericParamArgs = item.function.genericParameters;
					if(genericParamArgs.Length > 0) {
						TypeData[] param = new TypeData[genericParamArgs.Length];
						for(int i = 0; i < genericParamArgs.Length; i++) {
							if(item.genericParameterTypes[i].targetType == MemberData.TargetType.uNodeGenericParameter) {
								if(item.genericParameterTypes[i].genericData != null) {
									param[i] = item.genericParameterTypes[i].genericData;
								}
								else {
									param[i] = new TypeData("$" + item.genericParameterTypes[i].name);
								}
							}
							else if(item.genericParameterTypes[i].targetType == MemberData.TargetType.uNodeType) {
								var rType = item.genericParameterTypes[i].type as RuntimeType;
								param[i] = MemberDataUtility.GetTypeData(rType, null);
							}
							else {
								param[i] = MemberDataUtility.GetTypeData(item.genericParameterTypes[i].startType);
							}
						}
						iData.genericArguments = param;
					}
					var paramsInfo = item.function.parameters;
					if(paramsInfo.Count > 0) {
						if(iData == null) {
							iData = new MemberData.ItemData();
						}
						iData.parameters = MemberDataUtility.ParameterDataToTypeDatas(paramsInfo, genericParamArgs);
					}
				}
				else if(item.variable != null) {
					iData = MemberDataUtility.CreateItemData(item.variable);
				}
				else if(item.property != null) {
					iData = MemberDataUtility.CreateItemData(item.property);
				}
				else if(item.reference != null) {
					iData = new MemberData.ItemData() {
						name = item.reference.name,
						reference = item.reference,
					};
				}
				else {
					iData = new MemberData.ItemData() {
						name = item.Name,
					};
				}
				if(item.type != null) {
					member.type = item.type;
				}
				member.startType = typeof(MonoBehaviour);
				member.isStatic = false;
				member.targetType = item.targetType;
				member.Items = new[] { iData };
				editorData.Select(member);
			}
			#endregion

			#region Icon
			private Texture GetIcon(TViewItem tree) {
				if(tree is MemberTreeView) {
					return tree.icon ?? (tree as MemberTreeView).GetIcon();
				}
				else if(tree is NamespaceTreeView) {
					return uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.NamespaceIcon));
				}
				else if(tree is SelectorCustomTreeView) {
					var item = tree as SelectorCustomTreeView;
					if(item.item != null) {
						Texture icon = item.item.GetIcon();
						if(icon != null) {
							return icon;
						}
					}
				}
				return tree.icon;
			}

			private Texture GetSecondIcon(TViewItem tree) {
				if(tree is TypeTreeView) {
					return null;
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;
					return uNodeEditorUtility.GetTypeIcon(ReflectionUtils.GetMemberType(item.member));
				}
				else if(tree is SelectorCustomTreeView) {
					var item = tree as SelectorCustomTreeView;
					if(item.item != null) {
						return item.item.GetSecondaryIcon();
					}
				}
				return null;
			}
			#endregion

			#region Hiding
			public new void SetSelection(IList<int> selectedIDs) {
				if(selectedIDs.Count == 1) {
					var tree = GetRows().FirstOrDefault(x => x.id == selectedIDs[0]);
					selectedTree = tree;
					return;
				}
				else if(selectedIDs.Count == 0) {
					selectedTree = null;
					return;
				}
				throw null;
			}

			public new IList<int> GetSelection() {
				if(selectedTree == null) {
					return Array.Empty<int>();
				}
				else {
					return new[] { selectedTree.id };
				}
			}

			public new bool HasSelection() {
				return selectedTree != null;
			}

			protected override void SelectionChanged(IList<int> selectedIds) {
				base.SetSelection(Array.Empty<int>());
				SetSelection(selectedIds);
			}
			#endregion

			#region Reload
			public void Reload(List<TViewItem> trees) {
				this.treeViews = trees;
				Reload();
			}

			TreeSearchManager treeSearch = new TreeSearchManager();
			public List<SearchProgress> searchProgresses => treeSearch.progresses;

			public void ReloadInBackground() {
				var lastNestedData = m_lastNestedData;
				m_lastNestedData = null;
				relevanceData = null;

				if(hasSearch) {
					treeHightlights.Clear();
					isReloading = true;

					List<TViewItem> treeViews;
					if(isDeep) {
						treeViews = new List<TViewItem>(this.deepItems);
					}
					else {
						treeViews = new List<TViewItem>(this.treeViews);
					}
					{
						for(int i = 0; i < treeViews.Count; i++) {
							treeViews[i] = DuplicateTree(treeViews[i]);
						}
					}
					treeSearch.deepSearch = true;
					treeSearch.manager = this;
					treeSearch.SearchInBackground(treeViews, searchString, editorData.searchKind, editorData.searchFilter, (trees) => {
						var usedCount = PersistenceData.Instance.itemUsedCount;
						var usedCount2 = PersistenceData.Instance.searchedItemUsedCount;
						float GetScore(int id, IRelevanceItem relevance, int depth) {
							var score = relevance.Score;
							if(score >= BonusRelevantScore.Config.MinRelevantScore) {
								if(score >= BonusRelevantScore.Config.MinScoreForAddBonusToUsedCount) {
									if(usedCount.TryGetValue(id, out var count)) {
										score += MathF.Min(0.02f * count, BonusRelevantScore.Config.MaxGlobalUsedCountBonus);
									}
									if(usedCount2.TryGetValue((searchString, id), out count)) {
										score += MathF.Min(0.02f * count, BonusRelevantScore.Config.MaxSpecificUsedCountBonus);
									}
								}
								if(relevance is SelectorCustomTreeView or NodeTreeView) {
									//Give a little boost to custom tree since they are more likely to be what user is looking for.
									score += Mathf.Clamp01(0.8f - (0.05f * depth));
								}
								else {
									score += Mathf.Clamp01(0.5f - (0.1f * depth));
								}
							}
							return score;
						}

						if(editorData.searchKind == SearchKind.Relevant) {
							relevanceData = new RelevanceData();
							relevanceData.originalSearchTrees = trees;
							List<TViewItem> relevanceTrees = new();
							bool hasSpace = searchString.Contains(' ') || searchString.Contains('.');
							var usingNamespaces = editorData.usingNamespaces;
							float bonusScore = 0;
							void TraverseTree(TViewItem tree) {
								if(tree is TypeTreeView && editorData.searchFilter != SearchFilter.All && editorData.searchFilter != SearchFilter.Type) {
									//Skip if filter is not valid
									return;
								}
								if(tree is IRelevanceItem relevance) {
									var score = GetScore(tree.id, relevance, tree.depth);
									if(score >= BonusRelevantScore.Config.MinRelevantScore) {
										if(tree is MemberTreeView aMember) {
											if(usingNamespaces != null && usingNamespaces.Contains(ReflectionUtils.GetDeclaringType(aMember.member).Namespace) == false) {
												score += BonusRelevantScore.Config.ScoreForIrrelevantContext;
												//This to make sure bonus used count is not too much for items that are not in relevant namespaces.
												if(usedCount.TryGetValue(tree.id, out var count)) {
													score -= MathF.Min(0.01f * count, BonusRelevantScore.Config.HalfGlobalUsedCountBonus);
												}
												if(usedCount2.TryGetValue((searchString, tree.id), out count)) {
													score -= MathF.Min(0.01f * count, BonusRelevantScore.Config.HalfSpecificUsedCountBonus);
												}
											}
											if(tree is TypeTreeView && !hasSpace) {
												score += 0.2f;
												if(string.Equals(tree.displayName, searchString, StringComparison.OrdinalIgnoreCase)) {
													//In case match whole word
													score += 1f;
												}
											}
										}
										if(score > BonusRelevantScore.Config.MinScoreForAdditionalBonus) {
											//Give a bonus score to items that are already relevant to make sure they are prioritized over less relevant items.
											score += bonusScore * MathF.Min(score, 1);
										}
									}
									if(score > 0) {
										relevance.Score = score;
										relevanceTrees.Add(DuplicateTree(tree, false));
									}
								}
								if(tree.hasChildren) {
									float originalBonus = bonusScore;
									if(tree is ICategoryTreeItem category) {
										bonusScore = MathF.Max(category.GetSearchBonusScore(searchString), bonusScore);
									}
									float prevBonus = bonusScore;
									foreach(var child in tree.children) {
										TraverseTree(child);
										bonusScore = prevBonus;
									}
									bonusScore = originalBonus;
								}
							}
							foreach(var tree in trees) {
								TraverseTree(tree);
								bonusScore = 0;
							}
							string query = searchString.ToLower();
							relevanceTrees.Sort((a, b) => {

								float scoreA = (a as IRelevanceItem).Score;
								float scoreB = (b as IRelevanceItem).Score;

								return scoreB.CompareTo(scoreA);
							});
							trees = relevanceTrees;
						}

						uNodeThreadUtility.Queue(() => {
							isReloading = false;
							searchedTrees = trees;
							selectedTree = null;
							state.scrollPos = default;

							Reload();

							if(lastNestedData != null) {
								var tree = GetRows().FirstOrDefault(x => x.id == lastNestedData.selectedTree);
								if(tree != null) {
									selectedTree = tree;
								}
								state.scrollPos = lastNestedData.scrollPos;
								return;
							}

							if(editorData.searchKind == SearchKind.Relevant) {
								selectedTree = GetRows().FirstOrDefault();
								return;
							}

							float highestScore = -1;
							TViewItem highestScoreTree = null;

							foreach(var tree in GetRows()) {
								//TraverseTree(tree);
								if(tree is IRelevanceItem relevance) {
									var score = GetScore(tree.id, relevance, tree.depth);
									var parent = tree.parent;
									while(parent != null) {
										if(parent is ICategoryTreeItem categoryTree) {
											score += categoryTree.GetSearchBonusScore(searchString);
										}
										parent = parent.parent;
									}
									if(score > highestScore) {
										highestScore = score;
										highestScoreTree = tree;
									}
								}
							}
							if(highestScoreTree != null) {
								if(highestScoreTree is MemberTreeView memberTree) {
									var usingNamespaces = editorData.usingNamespaces;
									//In case the highest score doesn't contain relevance namespaces then we re-iterate to find better items
									if(usingNamespaces != null && usingNamespaces.Contains(ReflectionUtils.GetDeclaringType(memberTree.member).Namespace) == false) {
										highestScore += BonusRelevantScore.Config.ScoreForIrrelevantContext;
										foreach(var tree in GetRows()) {
											if(tree is MemberTreeView mTree) { 
												var score = GetScore(tree.id, mTree, tree.depth);
												if(usingNamespaces.Contains(ReflectionUtils.GetDeclaringType(mTree.member).Namespace) == false) {
													continue;
												}
												if(score > highestScore) {
													highestScore = score;
													highestScoreTree = tree;
												}
											}
										}
									}
								}
								selectedTree = highestScoreTree;
								//if(searchString.Length >= 3) {
								//	Debug.Log(highestScore);
								//}
							}
						});
					});
				}
				else {
					treeSearch.Terminate();
					Reload();
					isReloading = false;

					if(lastNestedData != null) {
						var tree = GetRows().FirstOrDefault(x => x.id == lastNestedData.selectedTree);
						if(tree != null) {
							selectedTree = tree;
						}
						state.scrollPos = lastNestedData.scrollPos;
					}
				}
			}

			TViewItem DuplicateTree(TViewItem tree, bool includeChildren = true, Dictionary <TViewItem, TViewItem> duplicatedTrees = null) {
				if(duplicatedTrees == null) {
					duplicatedTrees = new Dictionary<TViewItem, TViewItem>();
				}
				if(!duplicatedTrees.TryGetValue(tree, out var result)) {
					List<TViewItem> children = null;
					if(includeChildren) {
						children = tree.children;
						if((children == null || children.Count == 0) && tree is SelectorCategoryTreeView) {
							var item = tree as SelectorCategoryTreeView;
							children = item.childTrees ?? item.children;
						}
						if(children != null) {
							children = new List<TViewItem>(children);
							for(int i = 0; i < children.Count; i++) {
								children[i] = DuplicateTree(children[i], includeChildren, duplicatedTrees);
							}
						}
					}
					if(tree is SelectorCategoryTreeView) {
						var item = tree as SelectorCategoryTreeView;
						result = new SelectorCategoryTreeView(item.category, item.description, item.id) {
							depth = item.depth,
							expanded = true,
							children = children,
							parent = item.parent,
							icon = item.icon,
							hideOnSearch = item.hideOnSearch,
							bonusScore = item.bonusScore,
						};
						SetSearchExpanded(item.id, true);
					}
					else if(tree is SelectorCustomTreeView) {
						var item = tree as SelectorCustomTreeView;
						result = new SelectorCustomTreeView(item.item, item.id, item.depth) {
							children = children,
							parent = item.parent,
							icon = item.icon,
						};
					}
					else if(tree is SelectorMemberTreeView) {
						var item = tree as SelectorMemberTreeView;
						result = new SelectorMemberTreeView(item.member, item.displayName, item.id) {
							depth = item.depth,
							children = children,
							parent = item.parent,
							icon = item.icon,
						};
					}
					else if(tree is SelectorNamespaceTreeView) {
						var item = tree as SelectorNamespaceTreeView;
						result = new SelectorNamespaceTreeView(item.Namespace, item.id, item.depth) {
							children = children,
							parent = item.parent,
							icon = item.icon,
						};
					}
					else if(tree is SelectorGroupedTreeView) {
						var item = tree as SelectorGroupedTreeView;
						result = new SelectorGroupedTreeView(item.treeViews, item.displayName, item.id, item.depth) {
							children = children,
							parent = item.parent,
							icon = item.icon,
						};
					}
					else if(tree is TypeTreeView) {
						var item = tree as TypeTreeView;
						item = new TypeTreeView() {
							filter = item.filter ?? this.filter,
							children = children,
							parent = item.parent,
							type = item.type,
							member = item.member,
							id = item.id,
							depth = item.depth,
							displayName = item.displayName,
						};
						//if(item.type is RuntimeType) {
						//	item.Expand(true);
						//}
						result = item;
					}
					else if(tree is MemberTreeView) {
						var item = tree as MemberTreeView;
						result = new MemberTreeView() {
							instance = item.instance,
							nextValidation = item.nextValidation,
							selectValidation = item.selectValidation,
							children = children,
							parent = item.parent,
							member = item.member,
							id = item.id,
							depth = item.depth,
							displayName = item.displayName,
						};
					}
					else if(tree is NamespaceTreeView) {
						var item = tree as NamespaceTreeView;
						result = new NamespaceTreeView(item.Namespace, item.id, item.depth) {
							children = children,
							parent = item.parent,
							icon = item.icon,
						};
					}
					else if(tree is SelectorSearchTreeView) {
						var item = tree as SelectorSearchTreeView;
						result = new SelectorSearchTreeView(item.treeViews, item.displayName, item.id, item.depth) {
							children = children,
							parent = item.parent,
							icon = item.icon,
						};
					}
					else {
						throw new Exception("Unsupported duplicate tree: " + tree.GetType());
					}
					if(tree is IRelevanceItem relevance) {
						(result as IRelevanceItem).Score = relevance.Score;
					}
					duplicatedTrees.Add(tree, result);
				}
				return result;
			}

			#endregion

			#region Build Trees
			protected override TViewItem BuildRoot() {
				return new TViewItem { id = 0, depth = -1 };
			}

			protected override IList<TViewItem> BuildRows(TViewItem root) {
				var rows = GetRows() ?? new List<TViewItem>();
				rows.Clear();
				if(!hasSearch) {
					if(nonSearchExpandeds.Count > 0) {
						foreach(var pair in nonSearchExpandeds) {
							SetExpanded(pair.Key, pair.Value);
						}
						nonSearchExpandeds.Clear();
					}
					if(isDeep) {
						var treeView = lastTree;
						var filter = new FilterAttribute(this.filter) { Static = this.filter.Static && lastTree is TypeTreeView };
						if(treeView is MemberTreeView) {
							var item = treeView as MemberTreeView;
							var mType = ReflectionUtils.GetMemberType(item.member);
							var members = TreeFunction.CreateItemsFromType(mType, filter);
							var inheritTree = new SelectorCategoryTreeView("Inherit Members", uNodeEditorUtility.GetUIDFromString("[INHERIT]"));
							foreach(var tree in members) {
								if(CanAddTree(tree)) {
									if(tree.member?.DeclaringType != mType) {
										inheritTree.AddChild(tree);
									}
									else {
										root.AddChild(tree);
										AddTrees(tree, rows);
									}
								}
							}
							if(inheritTree.hasChildren) {
								inheritTree.expanded = true;
								root.AddChild(inheritTree);
								AddTrees(inheritTree, rows);
							}
							deepItems = new List<TViewItem>(members);
						}
						else if(treeView is NamespaceTreeView) {
							var item = treeView as NamespaceTreeView;
							var ns = item.Namespace;
							var nsList = GetNamespaceTypes(new string[] { ns }, ignoreIncludedAssemblies: true);
							var trees = new List<TViewItem>();
							{
								//var excludedNS = uNodePreference.GetExcludedNamespace();
								var namespaces = new List<string>(EditorReflectionUtility.GetNamespaces());
								namespaces.RemoveAll(n => n == null || !n.StartsWith(ns, StringComparison.Ordinal) || n.Length <= ns.Length);
								namespaces.Sort();
								trees.Add(new SelectorSearchTreeView((prog) => {
									var treeResult = new List<TViewItem>();
									var sp = new SearchProgress();
									prog?.Invoke(sp);
									var allTypes = GetNamespaceTypes(namespaces, (currProgress) => {
										prog?.Invoke(new SearchProgress() { progress = currProgress, info = "Searching type on sub namespaces: " + ns });
									}, ignoreIncludedAssemblies: true);
									sp.info = "Setup Items";
									for(int i = 0; i < allTypes.Count; i++) {
										var pair = allTypes[i];
										var nsTree = new SelectorCategoryTreeView(pair.Key, "", uNodeEditorUtility.GetUIDFromString("[CATEG-SEARCH]" + pair.Key));
										foreach(var type in pair.Value) {
											nsTree.AddChild(new TypeTreeView(type, type.GetHashCode(), -1));
										}
										treeResult.Add(nsTree);
										sp.progress = (float)i / (float)allTypes.Count;
										prog?.Invoke(sp);
									}
									return treeResult;
								}, "Search On Sub Namespace", uNodeEditorUtility.GetUIDFromString("[SAT]"), -1));
								//namespaces.RemoveAll(n => excludedNS.Contains(n));
								foreach(var n in namespaces) {
									var name = n.Remove(0, ns.Length + 1);
									trees.Add(new NamespaceTreeView(n, uNodeEditorUtility.GetUIDFromString("[N]" + n), -1) {
										displayName = name
									});
								}
							}
							foreach(var pair in nsList) {
								foreach(var type in pair.Value) {
									trees.Add(new TypeTreeView(type, type.GetHashCode(), -1));
								}
							}
							foreach(var tree in trees) {
								if(CanAddTree(tree)) {
									root.AddChild(tree);
									AddTrees(tree, rows);
								}
							}
							deepItems = new List<TViewItem>(trees);
						}
						else if(treeView is SelectorCustomTreeView) {
							var item = treeView as SelectorCustomTreeView;
							var trees = item.item.GetDeepItems(editorData);
							if(trees != null) {
								deepItems = new List<TViewItem>(trees);
								foreach(var tree in deepItems) {
									if(tree == null) continue;
									if(CanAddTree(tree)) {
										root.AddChild(tree);
										AddTrees(tree, rows);
									}
								}
							}
						}
						else if(treeView is SelectorGroupedTreeView) {
							var item = treeView as SelectorGroupedTreeView;
							var trees = item.treeViews();
							foreach(var tree in trees) {
								if(CanAddTree(tree)) {
									root.AddChild(tree);
									AddTrees(tree, rows);
								}
							}
							deepItems = new List<TViewItem>(trees);
						}
					}
					else {
						if(treeViews != null) {
							foreach(var tree in treeViews) {
								if(CanAddTree(tree)) {
									root.AddChild(tree);
									AddTrees(tree, rows);
								}
							}
						}
					}
				}
				else {
					if(searchedTrees != null) {
						var trees = searchedTrees;
						foreach(var tree in trees) {
							if(CanAddTree(tree)) {
								root.AddChild(tree);
								AddTrees(tree, rows);
							}
						}
					}
				}
				SetupDepthsFromParentsAndChildren(root);
				return rows;
			}

			private void AddTrees(TViewItem treeView, IList<TViewItem> rows) {
				if(treeView == null)
					return;
				if(!CanAddTree(treeView)) {
					return;
				}
				if(treeView is TypeTreeView) {
					var item = treeView as TypeTreeView;
					if(item.filter == null) {
						item.filter = filter;
					}
					if(!hasSearch) {
						item.Expand(IsExpanded(item.id));
					}
				}
				else if(treeView is SelectorCategoryTreeView) {
					var item = treeView as SelectorCategoryTreeView;
					if(!expandedStates.ContainsKey(item.id)) {
						SetExpanded(item.id, item.expanded);
					}
					item.expanded = IsExpanded(item.id);
					expandedStates[item.id] = item.expanded;
				}
				rows.Add(treeView);
				if(treeView.hasChildren && treeView.children != null) {
					if(CanChangeExpandTree(treeView) && !IsExpanded(treeView.id))
						return;
					foreach(var child in treeView.children) {
						AddTrees(child, rows);
					}
				}
			}

			public bool CanAddTree(TViewItem treeView) {
				if(treeView is SelectorSearchTreeView) {
					if(!hasSearch) {
						return false;
					}
				}
				else if(treeView is SelectorCategoryTreeView) {
					if(hasSearch && (treeView as SelectorCategoryTreeView).hideOnSearch) {
						return false;
					}
				}
				return true;
			}
			#endregion

			public void SetSearchExpanded(int id, bool expanded) {
				if(!nonSearchExpandeds.TryGetValue(id, out _)) {
					nonSearchExpandeds[id] = IsExpanded(id);
				}
				SetExpanded(id, expanded);
			}

			private bool CanChangeExpandTree(TViewItem item) {
				if(hasSearch) {
					return item is SelectorSearchTreeView;
				}
				else {
					return item is TypeTreeView;
				}
			}

			protected override bool CanChangeExpandedState(TViewItem item) {
				return false;
			}

			protected override bool CanMultiSelect(TViewItem item) {
				return false;
			}

			protected override float GetCustomRowHeight(int row, TViewItem item) {
				if(relevanceData != null) {
					return base.GetCustomRowHeight(row, item) * 2;
				}
				return base.GetCustomRowHeight(row, item);
			}

			#region Others
			private bool IsStaticTree(TViewItem tree) {
				if(tree is TypeTreeView) {
					return true;
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;
					return ReflectionUtils.GetMemberIsStatic(item.member);
				}
				return false;
			}

			public void Dispose() {
				tooltipWindow?.Close();
			}

			#endregion
		}
	}
}
