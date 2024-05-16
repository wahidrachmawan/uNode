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

namespace MaxyGames.UNode.Editors {
	public partial class ItemSelector {
		public sealed class Manager : TreeView, IDisposable {
			public ItemSelector window;

			public List<TreeViewItem> deepTrees = new List<TreeViewItem>();
			public bool isDeep => deepTrees.Count > 0;
			public TreeViewItem lastTree => deepTrees.LastOrDefault();

			private Dictionary<int, bool> expandedStates = new Dictionary<int, bool>();
			private Dictionary<int, bool> nonSearchExpandeds = new Dictionary<int, bool>();
			private Dictionary<int, TreeHightlight> treeHightlights = new Dictionary<int, TreeHightlight>();

			private List<TreeViewItem> searchedTrees;
			private List<TreeViewItem> deepItems;

			public List<TreeViewItem> _treeViews;
			public List<TreeViewItem> treeViews {
				get => _treeViews;
				set {
					_treeViews = value;
					searchedTrees = null;
				}
			}

			private TooltipWindow tooltipWindow;
			private TreeViewItem hoveredTree;

			private string _searchString;
			private string _searchString2;
			public new string searchString {
				get => isDeep ? _searchString2 : _searchString;
				set {
					treeHightlights.Clear();
					if(isDeep) {
						if(_searchString2 != value) {
							_searchString2 = value;
							ReloadInBackground();
						}
					}
					else {
						if(_searchString != value) {
							_searchString = value;
							ReloadInBackground();
						}
					}
				}
			}

			public new bool hasSearch => !string.IsNullOrEmpty(searchString);
			public bool isReloading { get; private set; }

			private Data editorData => window?.editorData;
			public FilterAttribute filter => editorData?.filter;

			public Manager(TreeViewState state) : base(state) {
				showAlternatingRowBackgrounds = true;
				showBorder = true;
			}

			#region GUI
			protected override void SingleClickedItem(int id) {
				var tree = GetRows().FirstOrDefault(i => i.id == id);
				if(CanSelectTree(tree)) {
					SelectTree(tree);
				}
			}

			protected override void ContextClickedItem(int id) {
				var tree = GetRows().FirstOrDefault(i => i.id == id);
				if(CanNextTree(tree)) {
					NextTree(tree);
				}
			}

			protected override void KeyEvent() {
				Event evt = Event.current;
				if(evt.type == EventType.KeyDown) {
					var selections = GetSelection();
					if(selections.Count > 0) {
						var tree = GetRows().FirstOrDefault(i => i.id == selections[0]);
						if(tree != null) {
							if(evt.keyCode == KeyCode.Return) {
								if(CanSelectTree(tree)) {
									SelectTree(tree);
									evt.Use();
								}
							}
							else if(evt.keyCode == KeyCode.RightArrow) {
								if(CanNextTree(tree)) {
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
								if(HasFocus() && tree.depth == 0 && isDeep) {
									Back();
									evt.Use();
								}
							}
							else if(evt.keyCode == KeyCode.DownArrow) {
								OffsetSelection(1, tree.id);
								SetFocusAndEnsureSelectedItem();
							}
							else if(evt.keyCode == KeyCode.UpArrow) {
								OffsetSelection(-1, tree.id);
								SetFocusAndEnsureSelectedItem();
							}
						}
					}
				}
			}

			public void OffsetSelection(int offset, int id) {
				var rows = GetRows();
				if(rows.Count != 0) {
					var tree = rows.FirstOrDefault(t => t.id == id);
					if(tree != null) {
						int indexOfID = rows.IndexOf(tree);
						int num = Mathf.Clamp(indexOfID + offset, 0, rows.Count - 1);
						while(rows[num] == null || rows[num] is SelectorCategoryTreeView) {
							if(offset > 0) {
								if(num + 1 < rows.Count - 1)
									num++;
								else
									return;
							}
							else {
								if(num - 1 > 0)
									num--;
								else
									return;
							}
						}
						Event.current.Use();
						SetSelection(new int[] { rows[num].id });
					}
				}
			}

			protected override void RowGUI(RowGUIArgs args) {
				Event evt = Event.current;
				if(args.rowRect.Contains(evt.mousePosition)) {
					if(evt.type == EventType.MouseMove) {
						SetSelection(new int[] { args.item.id });
					}
					//SetFocus();
				}
				if(evt.type == EventType.Repaint) {
					#region Tooltip
					if(args.rowRect.Contains(evt.mousePosition)) {
						if(hoveredTree != args.item) {
							var contents = Utility.GetTooltipContents(args.item, window.filter.OnlyGetType);
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
					bool canSelect = CanSelectTree(args.item);
					bool canNext = CanNextTree(args.item);
					if(evt.type == EventType.Repaint) {
						if(canSelect) {
							Rect pos = labelRect;
							pos.x += labelRect.width - 25;
							pos.width = 15;
							uNodeGUIStyle.itemSelect.Draw(pos, GUIContent.none, false, false, false, false);
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
						if(args.item is MemberTreeView) {
							var tree = args.item as MemberTreeView;
							if(tree.member is MethodInfo method) {
								if(method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)) {
									label = new GUIContent(EditorReflectionUtility.GetPrettyExtensionMethodName(method));
								}
							}
							if(label == null) {
								if(uNodePreference.preferenceData.coloredItem) {
									label = new GUIContent(NodeBrowser.GetRichMemberName(tree.member));
								}
								else {
									label = new GUIContent(NodeBrowser.GetPrettyMemberName(tree.member));
								}
							}
						}
						else if(args.item is SelectorCustomTreeView) {
							var tree = args.item as SelectorCustomTreeView;
							if(tree.item is ItemReflection ri) {
								if(ri.item != null) {
									var member = ri.item.memberInfo;
									if(member is MethodInfo method) {
										if(method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)) {
											label = new GUIContent(EditorReflectionUtility.GetPrettyExtensionMethodName(method));
										}
									}
									if(label == null && member != null) {
										if(uNodePreference.preferenceData.coloredItem) {
											label = new GUIContent(NodeBrowser.GetRichMemberName(member));
										}
										else {
											label = new GUIContent(NodeBrowser.GetPrettyMemberName(member));
										}
									}
								}
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

			private void DrawLabel(TreeViewItem tree, GUIStyle style, Rect position, GUIContent label) {
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
						foreach(var pair in hightlight.hightlight) {
							var first = pair.Key;
							var last = pair.Value;
							if(first >= 0 && (first + last) < label.text.Length) {
								string str = label.text;
								string s1 = str.Substring(0, first);
								string s2 = str.Substring(first, last);
								var r1 = st.CalcSize(new GUIContent(s1));
								var r2 = st.CalcSize(new GUIContent(s2));
								GUI.DrawTexture(new Rect(position.x + r1.x, position.y, r2.x, position.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, new Color(0.24f, 0.49f, 0.91f, 0.5f), 0, 0);
							}
						}
					}
				}
				style.Draw(position, label, false, false, false, false);
			}

			#region Select & Next
			public void SelectTree(TreeViewItem tree) {
				if(tree is TypeTreeView) {
					var type = (tree as TypeTreeView).type;

					//Auto resolve generic type
					var resolvedMember = ReflectionUtils.AutoResolveGenericMember(type) as Type;
					if(resolvedMember != null) {
						deepTrees.Add(new TypeTreeView(resolvedMember, tree.id, tree.depth));
						SelectDeepTrees();
						GUIUtility.ExitGUI();
					}

					if(ResolveGenericItem(type, (mInfo) => {
						deepTrees.Add(new TypeTreeView(mInfo as Type, tree.id, tree.depth));
						SelectDeepTrees();
						GUIUtility.ExitGUI();
					})) { return; }
					deepTrees.Add(tree);
					SelectDeepTrees();
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;

					//Auto resolve generic type
					var resolvedMember = ReflectionUtils.AutoResolveGenericMember(item.member);
					if(resolvedMember != null) {
						deepTrees.Add(new MemberTreeView(resolvedMember, item.id, item.depth) { instance = item.instance });
						SelectDeepTrees();
						GUIUtility.ExitGUI();
					}

					if(ResolveGenericItem(item.member, (mInfo) => {
						deepTrees.Add(new MemberTreeView(mInfo, item.id, item.depth) { instance = item.instance });
						SelectDeepTrees();
						GUIUtility.ExitGUI();
					})) { return; }

					deepTrees.Add(tree);
					SelectDeepTrees();
				}
				else if(tree is SelectorCallbackTreeView) {
					var item = tree as SelectorCallbackTreeView;
					item.onSelect(window.editorData.windowRect);
					window.Close();
				}
				else if(tree is SelectorMemberTreeView) {
					var item = tree as SelectorMemberTreeView;
					window.Select(item.member);
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
										deepTrees.Add(new TypeTreeView(resolvedMember as Type, tree.id, tree.depth));
									}
									else {
										deepTrees.Add(new MemberTreeView(resolvedMember, tree.id, tree.depth) {
											instance = ri.item.instance
										});
									}
									SelectDeepTrees();
									GUIUtility.ExitGUI();
								}

								if(ResolveGenericItem(member, (mInfo) => {
									if(member is Type) {
										deepTrees.Add(new TypeTreeView(mInfo as Type, tree.id, tree.depth));
									}
									else {
										deepTrees.Add(new MemberTreeView(mInfo, tree.id, tree.depth) {
											instance = ri.item.instance
										});
									}
									SelectDeepTrees();
									GUIUtility.ExitGUI();
								})) { return; }
								if(member is Type) {
									deepTrees.Add(new TypeTreeView(member as Type, tree.id, tree.depth));
								}
								else {
									deepTrees.Add(new MemberTreeView(member, tree.id, tree.depth) {
										instance = ri.item.instance
									});
								}
								SelectDeepTrees();
							}
						}
						else {
							item.item.OnSelect(window);
						}
					}
					else if(item.graphItem != null) {
						SelectGraphItem(item.graphItem);
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

			public void NextTree(TreeViewItem tree) {
				if(tree is TypeTreeView) {
					if(ResolveGenericItem((tree as TypeTreeView).type, (mInfo) => {
						DoNextTree(new TypeTreeView(mInfo as Type, tree.id, tree.depth));
					})) { return; }
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;
					if(ResolveGenericItem(item.member, (mInfo) => {
						DoNextTree(new MemberTreeView(mInfo, item.id, item.depth) { instance = item.instance });
					})) { return; }
				}
				DoNextTree(tree);
			}

			private void DoNextTree(TreeViewItem tree) {
				_searchString2 = string.Empty;
				deepTrees.Add(tree);
				editorData.searchField.SetFocus();
				Reload();
			}

			private void Back() {
				if(isDeep) {
					_searchString2 = string.Empty;
					deepTrees.RemoveAt(deepTrees.Count - 1);
					editorData.searchField.SetFocus();
					Reload();
				}
			}

			public void SelectDeepTrees() {
				List<TreeViewItem> items = new List<TreeViewItem>();
				foreach(var tree in deepTrees) {
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
						window.Select(val);
						return;
					}
					else if(filter.IsValidTarget(MemberData.TargetType.Values)) {
						MemberData val = MemberData.Default(type);
						window.Select(val);
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
							window.Select(MemberData.CreateFromMember(member));
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
						if(tree.graphItem != null) {
							members.Add(tree.graphItem.GetData());
						}
						else if(tree.item != null && tree.item is ItemReflection) {
							var cItem = (tree.item as ItemReflection).item;
							if(cItem.memberInfo is ConstructorInfo) {
								window.Select(MemberData.CreateFromMember(cItem.memberInfo));
								return;
							}
							var member = MemberData.CreateFromMember(cItem.memberInfo);
							if(!cItem.isStatic) {
								member.instance = cItem.instance;
							}
							members.Add(member);
						}
						else {
							throw null;
						}
					}
				}
				if(HasRuntimeType(members)) {
					if(members.Count == 1) {
						window.Select(members[0]);
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
				window.Select(mData);
			}

			public bool CanSelectTree(TreeViewItem tree) {
				if(tree is TypeTreeView) {
					var item = tree as TypeTreeView;
					return M_CanSelectType(item.type, filter);
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;
					return item.CanSelect();
				}
				else if(tree is SelectorCallbackTreeView) {
					return true;
				}
				else if(tree is SelectorMemberTreeView) {
					var item = tree as SelectorMemberTreeView;
					var type = item.member.type;
					if(type != null && !item.member.targetType.HasFlags(MemberData.TargetType.Self | MemberData.TargetType.Null)) {
						return IsValidTypeToSelect(type);
					}
					return true;
				}
				else if(tree is SelectorCustomTreeView) {
					var item = tree as SelectorCustomTreeView;
					if(item.item != null) {
						if(item.item.CanSelect(window))
							return true;
					}
					if(item.graphItem != null) {
						if(item.graphItem.type != null) {
							if(IsValidTypeToSelect(item.graphItem.type)) {
								return true;
							}
							else if(item.graphItem.targetType == MemberData.TargetType.Self) {
								if(item.graphItem.targetObject is IClassGraph cls) {
									return IsValidTypeToSelect(cls.InheritType);
								}
							}
							return false;
						}
						else if(item.graphItem.genericParameter != null) {
							return filter == null || filter.CanSelectType;
						}
					}
				}
				else if(tree is SelectorGroupedTreeView) {
					return true;
				}
				else if(tree is NamespaceTreeView) {
					return true;
				}
				return false;
			}

			private bool IsValidTypeToSelect(Type type) {
				return filter == null || filter.IsValidType(type);
			}

			public bool CanNextTree(TreeViewItem tree) {
				if(tree is TypeTreeView) {
					var item = tree as TypeTreeView;
					var type = item.type;
					return !type.IsEnum;
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;
					return item.HasDeepMember();
				}
				else if(tree is SelectorCustomTreeView) {
					var item = tree as SelectorCustomTreeView;
					if(item.item is ItemReflection ri) {
						return ri.item != null && ri.item.hasNextItems;
					}
					return item.graphItem != null && item.graphItem.haveNextItem;
				}
				//else if(tree is SelectorGroupedTreeView) {
				//	return true;
				//} else if(tree is NamespaceTreeView) {
				//	return true;
				//}
				return false;
			}

			void SelectGraphItem(GraphItem item) {
				if(item.function != null && item.function.genericParameters.Length > 0 && item.genericParameterTypes == null) {
					TypeItem[] defaultType = item.function.genericParameters.Select(p => new TypeItem(p.value,
						new FilterAttribute(window.filter) {
							Types = new List<Type>() { p.value },
							ArrayManipulator = true,
						})).ToArray();
					TypeBuilderWindow.Show(window.editorData.windowRect, window.targetObject, window.filter, delegate (MemberData[] types) {
						item.genericParameterTypes = types;
						SelectGraphItem(item);
					}, defaultType);
					GUIUtility.ExitGUI();
					return;
				}
				if(item.targetType == MemberData.TargetType.Self) {
					window.Select(new MemberData("this", item.type, item.targetType) { instance = item.targetObject });
					return;
				}
				var member = new MemberData();
				member.instance = item.GetInstance();
				MemberData.ItemData iData = null;
				if(item.function != null) {
					iData = new MemberData.ItemData() {
						name = item.function.name,
						reference = new FunctionRef(item.function, item.function.graphContainer),
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
				window.Select(member);
			}


			bool ResolveGenericItem(MemberInfo member, Action<MemberInfo> onResolved) {
				if(member == null)
					return false;
				MethodInfo method = member as MethodInfo;
				Type mType = member as Type;

				void DoResolve(MemberData[] types) {
					bool hasGenericParameterTarget = types.Any(i => i.targetType == MemberData.TargetType.uNodeGenericParameter);
					if(!hasGenericParameterTarget) {
						if(method != null && method.IsGenericMethodDefinition) {
							method = ReflectionUtils.MakeGenericMethod(method, types.Select(i => i.startType).ToArray());
							member = method;
						}
						else if(mType != null && mType.IsGenericTypeDefinition) {
							mType = ReflectionUtils.MakeGenericType(mType, types.Select(i => i.startType).ToArray());
							member = mType;
						}
						else {
							//item.genericArguments = MemberDataUtility.MakeTypeDatas(types, item.genericObjects);
							//item.genericObjects = new List<Object>();
							//item.genericObjects.AddRange(types.Where(i => i != null && i.instance != null).Select(i => (i.GetInstance() as UnityEngine.Object)).ToList());
							throw new InvalidOperationException();
						}
					}
					else {
						throw new InvalidOperationException();
						//item.genericArguments = MemberDataUtility.MakeTypeDatas(types, item.genericObjects);
						//item.genericObjects = new List<Object>();
						//item.genericObjects.AddRange(types.Where(i => i != null && i.instance != null).Select(i => (i.GetInstance() as UnityEngine.Object)).ToList());
						//if(method != null && method.IsGenericMethodDefinition) {
						//	item.genericParameterTypes = types;
						//} else if(item.instance is MemberData mData) {
						//	mData.name = types[0].name;
						//	mData.instance = types[0].instance;
						//	mData.targetType = MemberData.TargetType.uNodeGenericParameter;
						//}
					}
					onResolved(member);
				}

				if(mType != null && mType.IsGenericTypeDefinition || method != null && method.IsGenericMethodDefinition) {
					Type[] genericType = method != null ? method.GetGenericArguments() : mType.GetGenericArguments();
					FilterAttribute F = new FilterAttribute(filter);
					F.OnlyGetType = true;
					//F.DisplayRuntimeType = false;
					F.Types = new List<Type>();
					TypeItem[] typeItems = new TypeItem[genericType.Length];
					for(int i = 0; i < genericType.Length; i++) {
						FilterAttribute fil = new FilterAttribute(genericType[i].BaseType) {
							OnlyGetType = true,
							//DisplayRuntimeType = false,
						};
						fil.ToFilterGenericConstraints(genericType[i]);
						typeItems[i] = new TypeItem(genericType[i].BaseType, fil);
					}
					if(genericType.Length == 1) {
						ItemSelector w = null;
						Action<MemberData> action = delegate (MemberData m) {
							if(w != null) {
								w.Close();
								//EditorGUIUtility.ExitGUI();
							}
							if(typeItems[0].filter.Types.Count > 0) {
								DoResolve(new[] { m });
							}
							else {
								TypeBuilderWindow.Show(Rect.zero, window.targetObject, typeItems[0].filter, delegate (MemberData[] types) {
									DoResolve(types);
								}, new TypeItem(m, typeItems[0].filter)).ChangePosition(window.position);
							}
						};
						Rect newPos = window.position;
						if(newPos.x + (newPos.width * 2) >= Screen.currentResolution.width) {
							newPos.x -= newPos.width - 20;
						}
						else {
							newPos.x += newPos.width - 20;
						}
						w = ShowAsNew(window.targetObject, typeItems[0].filter, action).ChangePosition(newPos);
						return true;
					}
					TypeBuilderWindow.Show(Rect.zero, window.targetObject, F, delegate (MemberData[] types) {
						DoResolve(types);
					}, typeItems).ChangePosition(window.position);
					return true;
				}
				return false;
			}
			#endregion

			#region Icon
			private Texture GetIcon(TreeViewItem tree) {
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

			private Texture GetSecondIcon(TreeViewItem tree) {
				if(tree is TypeTreeView) {
					return null;
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;
					return uNodeEditorUtility.GetTypeIcon(ReflectionUtils.GetMemberType(item.member));
				}
				else if(tree is SelectorCustomTreeView) {
					var item = tree as SelectorCustomTreeView;
					if(item.graphItem != null) {
						return item.graphItem.GetIcon();
					}
					else if(item.item != null) {
						return item.item.GetSecondaryIcon();
					}
				}
				return null;
			}
			#endregion

			#region Reload
			public void Reload(List<TreeViewItem> trees) {
				this.treeViews = trees;
				Reload();
			}

			TreeSearchManager treeSearch = new TreeSearchManager();
			public List<SearchProgress> searchProgresses => treeSearch.progresses;

			public void ReloadInBackground() {
				if(hasSearch) {
					isReloading = true;
					List<TreeViewItem> treeViews;
					if(isDeep) {
						treeViews = new List<TreeViewItem>(this.deepItems);
					}
					else {
						treeViews = new List<TreeViewItem>(this.treeViews);
					}
					{
						var duplicates = new Dictionary<TreeViewItem, TreeViewItem>();
						for(int i = 0; i < treeViews.Count; i++) {
							treeViews[i] = DuplicateTree(treeViews[i], duplicates);
						}
					}
					treeSearch.deepSearch = true;
					treeSearch.manager = this;
					treeSearch.SearchInBackground(treeViews, searchString, editorData.searchKind, editorData.searchFilter, (trees) => {
						uNodeThreadUtility.Queue(() => {
							isReloading = false;
							searchedTrees = trees;
							Reload();
						});
					});
				}
				else {
					treeSearch.Terminate();
					Reload();
					isReloading = false;
				}
			}

			TreeViewItem DuplicateTree(TreeViewItem tree, Dictionary<TreeViewItem, TreeViewItem> duplicatedTrees = null) {
				if(duplicatedTrees == null) {
					duplicatedTrees = new Dictionary<TreeViewItem, TreeViewItem>();
				}
				if(!duplicatedTrees.TryGetValue(tree, out var result)) {
					List<TreeViewItem> children = tree.children;
					if((children == null || children.Count == 0) && tree is SelectorCategoryTreeView) {
						var item = tree as SelectorCategoryTreeView;
						children = item.childTrees ?? item.children;
					}
					if(children != null) {
						children = new List<TreeViewItem>(children);
						for(int i = 0; i < children.Count; i++) {
							children[i] = DuplicateTree(children[i], duplicatedTrees);
						}
					}
					if(tree is SelectorCategoryTreeView) {
						var item = tree as SelectorCategoryTreeView;
						result = new SelectorCategoryTreeView(item.category, item.description, item.id, item.depth) {
							expanded = true,
							children = children,
							parent = item.parent,
							icon = item.icon,
							hideOnSearch = item.hideOnSearch,
						};
						SetSearchExpanded(item.id, true);
					}
					else if(tree is SelectorCallbackTreeView) {
						var item = tree as SelectorCallbackTreeView;
						result = new SelectorCallbackTreeView(item.onSelect, item.displayName, item.id, item.depth) {
							children = children,
							parent = item.parent,
							icon = item.icon,
						};
					}
					else if(tree is SelectorCustomTreeView) {
						var item = tree as SelectorCustomTreeView;
						if(item.item != null) {
							result = new SelectorCustomTreeView(item.item, item.id, item.depth) {
								children = children,
								parent = item.parent,
								icon = item.icon,
							};
						}
						else {
							result = new SelectorCustomTreeView(item.graphItem, item.id, item.depth) {
								children = children,
								parent = item.parent,
								icon = item.icon,
							};
						}
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
							filter = item.filter ?? window.filter,
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
					duplicatedTrees.Add(tree, result);
				}
				return result;
			}

			#endregion

			#region Build Trees
			protected override TreeViewItem BuildRoot() {
				return new TreeViewItem { id = 0, depth = -1 };
			}

			protected override IList<TreeViewItem> BuildRows(TreeViewItem root) {
				var rows = GetRows() ?? new List<TreeViewItem>();
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
						var filter = new FilterAttribute(window.filter) { Static = window.filter.Static && lastTree is TypeTreeView };
						if(treeView is MemberTreeView) {
							var item = treeView as MemberTreeView;
							var members = TreeFunction.CreateItemsFromType(ReflectionUtils.GetMemberType(item.member), filter);
							foreach(var tree in members) {
								if(CanAddTree(tree)) {
									root.AddChild(tree);
									AddTrees(tree, rows);
								}
							}
							deepItems = new List<TreeViewItem>(members);
						}
						else if(treeView is NamespaceTreeView) {
							var item = treeView as NamespaceTreeView;
							var ns = item.Namespace;
							var nsList = GetNamespaceTypes(new string[] { ns }, ignoreIncludedAssemblies: true);
							var trees = new List<TreeViewItem>();
							{
								//var excludedNS = uNodePreference.GetExcludedNamespace();
								var namespaces = new List<string>(EditorReflectionUtility.GetNamespaces());
								namespaces.RemoveAll(n => n == null || !n.StartsWith(ns, StringComparison.Ordinal) || n.Length <= ns.Length);
								namespaces.Sort();
								trees.Add(new SelectorSearchTreeView((prog) => {
									var treeResult = new List<TreeViewItem>();
									var sp = new SearchProgress();
									prog?.Invoke(sp);
									var allTypes = GetNamespaceTypes(namespaces, (currProgress) => {
										prog?.Invoke(new SearchProgress() { progress = currProgress, info = "Searching type on sub namespaces: " + ns });
									}, ignoreIncludedAssemblies: true);
									sp.info = "Setup Items";
									for(int i = 0; i < allTypes.Count; i++) {
										var pair = allTypes[i];
										var nsTree = new SelectorCategoryTreeView(pair.Key, "", uNodeEditorUtility.GetUIDFromString("[CATEG-SEARCH]" + pair.Key), -1);
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
							deepItems = new List<TreeViewItem>(trees);
						}
						else if(treeView is SelectorCustomTreeView) {
							var item = treeView as SelectorCustomTreeView;
							if(item.graphItem != null) {
								var members = TreeFunction.CreateItemsFromType(item.graphItem.type, filter);
								foreach(var tree in members) {
									if(CanAddTree(tree)) {
										root.AddChild(tree);
										AddTrees(tree, rows);
									}
								}
								deepItems = new List<TreeViewItem>(members);
							}
							else if(item.item is ItemReflection ri && ri.item != null) {
								var members = TreeFunction.CreateItemsFromType(ri.item.memberType, filter);
								foreach(var tree in members) {
									if(CanAddTree(tree)) {
										root.AddChild(tree);
										AddTrees(tree, rows);
									}
								}
								deepItems = new List<TreeViewItem>(members);
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
							deepItems = new List<TreeViewItem>(trees);
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

			private void AddTrees(TreeViewItem treeView, IList<TreeViewItem> rows) {
				if(treeView == null)
					return;
				if(!CanAddTree(treeView)) {
					return;
				}
				if(treeView is TypeTreeView) {
					var item = treeView as TypeTreeView;
					if(item.filter == null) {
						item.filter = window.filter;
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

			public bool CanAddTree(TreeViewItem treeView) {
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

			private bool CanChangeExpandTree(TreeViewItem item) {
				if(hasSearch) {
					return item is SelectorSearchTreeView;
				}
				else {
					return item is TypeTreeView;
				}
			}

			protected override bool CanChangeExpandedState(TreeViewItem item) {
				return false;
			}

			protected override bool CanMultiSelect(TreeViewItem item) {
				return false;
			}

			#region Others
			private bool IsStaticTree(TreeViewItem tree) {
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
