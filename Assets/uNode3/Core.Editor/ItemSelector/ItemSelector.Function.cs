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
		#region Setup
		void Init() {
			Setup();
			Rect rect = new Rect(new Vector2(preferenceData.itemSelectorWidth, 0), new Vector2(preferenceData.itemSelectorWidth, preferenceData.itemSelectorHeight));
			ShowAsDropDown(rect, new Vector2(preferenceData.itemSelectorWidth, preferenceData.itemSelectorHeight));
			editorData.windowRect = rect;
			wantsMouseMove = true;
			Focus();
		}

		void Setup() {
			if(filter == null) {
				filter = new FilterAttribute() { UnityReference = false };
			}
			if(filter.OnlyGetType) {
				filter.ValidTargetType = MemberData.TargetType.Type | MemberData.TargetType.Null;
			}
			if(targetObject != null) {
				if(targetObject is Node) {
					targetObject = (targetObject as Node).nodeObject;
				}
				if(targetObject is Object) {

				}
				else if(targetObject is UGraphElement) {

				}
				else {
					throw new Exception($"targetObject must from `UnityEngine.Object` or `UGraphElement`\nType: {targetObject.GetType()}");
				}
			}

			editorData.manager = new Manager(new TreeViewState());
			editorData.manager.window = this;
			editorData.searchField = new SearchField();
			editorData.searchField.downOrUpArrowKeyPressed += editorData.manager.SetFocusAndEnsureSelectedItem;
			editorData.searchField.autoSetFocusOnFindCommand = true;
			window = this;
			uNodeThreadUtility.Queue(DoSetup);
		}

		public List<TreeViewItem> CustomTrees { get; set; }

		void DoSetup() {
			editorData.setup.Setup((progress) => {
				if(progress == 1) {
					uNodeThreadUtility.Queue(() => {
						UnityEngine.Profiling.Profiler.BeginSample("AAA");
						var categories = new List<TreeViewItem>();
						if(CustomTrees != null) {
							foreach(var tree in CustomTrees) {
								categories.Add(tree);
							}
						}
						if(displayDefaultItem) {
							var categoryTree = new SelectorCategoryTreeView("#", "", uNodeEditorUtility.GetUIDFromString("[CATEG]#"), -1);
							categories.Add(categoryTree);
							var recentTree = new SelectorCategoryTreeView("Recently", "", uNodeEditorUtility.GetUIDFromString("[CATEG]#Recently"), -1);
							recentTree.hideOnSearch = true;
							categories.Add(recentTree);
							if(displayNoneOption && filter.IsValidTarget(MemberData.TargetType.Null)) {
								categoryTree.AddChild(new SelectorMemberTreeView(MemberData.None, "None", uNodeEditorUtility.GetUIDFromString("#None")) {
									icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.NullTypeIcon)) as Texture2D
								});
							}
							if(!filter.SetMember) {
								if(!filter.IsValueTypes() && filter.IsValidTarget(MemberData.TargetType.Null) && !filter.OnlyGetType) {
									categoryTree.AddChild(new SelectorMemberTreeView(MemberData.Null, "Null", uNodeEditorUtility.GetUIDFromString("#Null")) {
										icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.NullTypeIcon)) as Texture2D
									});
								}
								//if(!filter.OnlyGetType && filter.IsValidTarget(MemberData.TargetType.Values) &&
								//	(filter.Types == null || filter.Types.Count != 1 || filter.Types[0] != typeof(Type))) {
								//	categoryTree.AddChild(new SelectorCallbackTreeView((cRect) => {
								//		var screenRect = cRect.ToScreenRect();
								//		FilterAttribute F = new FilterAttribute(filter);
								//		F.OnlyGetType = true;
								//		ItemSelector w = null;
								//		Action<MemberData> action = delegate (MemberData m) {
								//			if(w != null) {
								//				w.Close();
								//				//EditorGUIUtility.ExitGUI();
								//			}
								//			if(filter.CanManipulateArray()) {
								//				TypeSelectorWindow.ShowAsNew(Rect.zero, F, delegate (MemberData[] members) {
								//					Select(members[0]);
								//				}, m).ChangePosition(screenRect);
								//			}
								//			else {
								//				Select(m);
								//			}
								//		};
								//		w = ShowAsNew(targetObject, F, action, true).ChangePosition(cRect);
								//	}, "Values", uNodeEditorUtility.GetUIDFromString("#Values"), -1) {
								//		icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.ValueIcon)) as Texture2D
								//	});
								//}
							}
							if(displayRecentItem) {
								var listRecentItems = new List<TreeViewItem>();
								if(uNodeEditor.SavedData.recentItems != null) {
									foreach(var recent in uNodeEditor.SavedData.recentItems) {
										try {
											if(recent != null && recent.info != null) {
												if(recent.info is Type) {
													listRecentItems.Add(new TypeTreeView(recent.info as Type, recent.GetHashCode(), -1) { filter = filter });
												}
												else if(!filter.OnlyGetType && (recent.isStatic || filter.DisplayInstanceOnStatic)) {
													listRecentItems.Add(new MemberTreeView(recent.info, recent.GetHashCode(), -1));
												}
											}
										}
										catch { }
									}
								}
								while(listRecentItems.Count > 10) {
									listRecentItems.RemoveAt(listRecentItems.Count - 1);
								}
								if(listRecentItems.Count > 0) {
									foreach(var item in listRecentItems) {
										if(item is MemberTreeView) {
											var tree = item as MemberTreeView;
											if(!(tree.member is Type)) {
												tree.displayName = tree.member.DeclaringType.Name + "." + tree.displayName;
											}
										}
										recentTree.AddChild(item);
									}
									recentTree.expanded = false;
								}
							}
							if(filter.UnityReference) {
								categories.AddRange(TreeFunction.CreateRootItem(targetObject, filter));
							}
							if(targetObject is not UGraphElement && targetObject is not IGraph) {
								if(!filter.OnlyGetType) {
									categories.Add(TreeFunction.CreateTargetItem(targetObject, "Target Reference", filter));
								}
							}
							else if(filter.OnlyGetType || filter.UnityReference && (targetObject is UGraphElement || targetObject is IGraph)) {
								categories.AddRange(TreeFunction.CreateGraphItem(targetObject, filter));

								if(!filter.SetMember && filter.IsValidTarget(MemberData.TargetType.Self | MemberData.TargetType.NodePort)) {
									IGraph graph = targetObject as IGraph;
									if(graph == null && targetObject is UGraphElement graphElement) {
										graph = graphElement.graphContainer;
									}
									if(graph != null) {
										var graphType = ReflectionUtils.GetRuntimeType(graph);
										//TODO: implement auto convert for `this`
										//if(canvasData != null) {
										//	if(NodeEditorUtility.CanAutoConvertOuput(graphType, filter)) {
										//		var item = CustomItem.Create("This", () => {
										//			if(filter.IsValidType(graphType)) {
										//				Select(MemberData.This(graph));
										//			}
										//			else {

										//			}
										//		}, icon: uNodeEditorUtility.GetTypeIcon(graphType ?? typeof(TypeIcons.KeywordIcon)));
										//		categoryTree.AddChild(new SelectorCustomTreeView(item, "[THIS]".GetHashCode(), -1));
										//	}
										//}
										//else {
										//}
										if(filter.IsValidType(graphType)) {
											var item = CustomItem.Create("This", () => {
												Select(MemberData.This(graph));
											}, icon: uNodeEditorUtility.GetTypeIcon(graphType ?? typeof(TypeIcons.KeywordIcon)));
											categoryTree.AddChild(new SelectorCustomTreeView(item, "[THIS]".GetHashCode(), -1));
										}
									}
								}
							}
							categories.AddRange(TreeFunction.CreateCustomItem(customItems, expanded: m_defaultExpandedItems == null && customItemDefaultExpandState));
							if(filter.DisplayDefaultStaticType) {
								categoryTree.AddChild(new SelectorGroupedTreeView(() => {
									var result = new List<TreeViewItem>();
									result.Add(new SelectorSearchTreeView((prog) => {
										var treeResult = new List<TreeViewItem>();
										var sp = new SearchProgress();
										prog?.Invoke(sp);
										var allTypes = GetAllTypes((currProgress) => {
											prog?.Invoke(currProgress);
										}, true, true);
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
									}, "Search All Types", uNodeEditorUtility.GetUIDFromString("[SAT]"), -1));
									var nestedNS = new HashSet<string>();
									//var excludedNs = uNodePreference.GetExcludedNamespace();
									var namespaces = new List<string>(EditorReflectionUtility.GetNamespaces());
									namespaces.Sort();
									namespaces.RemoveAll(i => /*excludedNs.Contains(i) ||*/ i == null || i.Contains("."));
									foreach(var ns in namespaces) {
										result.Add(new NamespaceTreeView(ns, uNodeEditorUtility.GetUIDFromString("[N]" + ns), -1));
									}
									//var nsTypes = GetNamespaceTypes(namespaces);
									//foreach(var pair in nsTypes) {
									//	var nsTree = new SelectorCategoryTreeView(pair.Key, "", uNodeEditorUtility.GetUIDFromString("[Nested-NS]" + pair.Key), -1);
									//	foreach(var ns in nestedNS) {
									//		if(ns.StartsWith(pair.Key)) {
									//			nsTree.AddChild(new NamespaceTreeView(ns, uNodeEditorUtility.GetUIDFromString("[N]" + ns), -1));
									//		}
									//	}
									//	foreach(var type in pair.Value) {
									//		nsTree.AddChild(new TypeTreeView(type, type.GetHashCode(), -1));
									//	}
									//	//nsTree.children.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.Ordinal));
									//	nsTree.expanded = false;
									//	result.Add(nsTree);
									//}
									return result;
								}, "All Namespaces", uNodeEditorUtility.GetUIDFromString("[ALL-NS]"), -1) {
									icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.NamespaceIcon)) as Texture2D
								});
								categoryTree.AddChild(new SelectorGroupedTreeView(() => {
									return MakeFavoriteTrees(favoriteHandler, window.filter);
								}, "Favorites", uNodeEditorUtility.GetUIDFromString("[fav]"), -1) {
									icon = uNodeGUIStyle.favoriteIconOn
								});
								var namespaceTrees = new SelectorCategoryTreeView("Types", "", uNodeEditorUtility.GetUIDFromString("[NS]"), -1);
								namespaceTrees.expanded = true;

								var hideUnselectedItem = uNodePreference.preferenceData.itemSelectorShowUnselectedTypes == false && filter.OnlyGetType;

								if(displayGeneralType) {
									var categTree = new SelectorCategoryTreeView("General", "", uNodeEditorUtility.GetUIDFromString("[GENERAL]"), -1);
									var items = TreeFunction.GetGeneralTrees();
									items.ForEach(tree => {
										if(hideUnselectedItem && !M_CanSelectType(tree.type, filter))
											return;

										categTree.AddChild(tree);
									});
									if(categTree.hasChildren) {
										categTree.expanded = true;
										namespaceTrees.AddChild(categTree);
									}
								}
								if(filter.DisplayRuntimeType) {
									var runtimeItems = TreeFunction.GetRuntimeItems();
									var runtimeTypes = new Dictionary<string, List<TypeTreeView>>();
									foreach(var item in runtimeItems) {
										if(hideUnselectedItem && !M_CanSelectType(item.type, filter))
											continue;
										if(filter.DisplayGeneratedRuntimeType == false && ReflectionUtils.IsNativeType(item.type) == false)
											continue;
										if(filter.DisplayNativeRuntimeType == false && ReflectionUtils.IsNativeType(item.type))
											continue;
										var ns = item.type.Namespace;
										if(string.IsNullOrEmpty(ns) || ns == RuntimeType.RuntimeNamespace) {
											ns = "Generated Type";
										}
										List<TypeTreeView> list;
										if(!runtimeTypes.TryGetValue(ns, out list)) {
											list = new List<TypeTreeView>();
											runtimeTypes[ns] = list;
										}
										list.Add(item);
									}
									foreach(var pair in runtimeTypes) {
										var categTree = new SelectorCategoryTreeView(pair.Key, "", uNodeEditorUtility.GetUIDFromString("[RT]" + pair.Key), -1);
										var items = pair.Value;
										items.ForEach(tree => categTree.AddChild(tree));
										if(categTree.hasChildren) {
											namespaceTrees.AddChild(categTree);
										}
									}
								}

								//Get the type lists
								var typeList = editorData.setup.typeList;
								foreach(var pair in typeList) {
									var nsTree = new SelectorCategoryTreeView(pair.Key, "", uNodeEditorUtility.GetUIDFromString("[CATEG]" + pair.Key), -1);
									foreach(var type in pair.Value) {
										if(hideUnselectedItem && !M_CanSelectType(type, filter)) {
											//Skip any unselectable types
											continue;
										}
										nsTree.AddChild(new TypeTreeView(type, type.GetHashCode(), -1));
									}
									if(nsTree.hasChildren) {
										namespaceTrees.AddChild(nsTree);
									}
								}
								categories.Add(namespaceTrees);
							}
						}
						else {
							categories.AddRange(TreeFunction.CreateCustomItem(customItems, m_defaultExpandedItems == null && customItemDefaultExpandState));
						}
						categories.RemoveAll(tree => tree == null || !tree.hasChildren);
						if(displayDefaultItem) {
							categories.Insert(0, new SelectorSearchTreeView((prog) => {
								var treeResult = new List<TreeViewItem>();
								var sp = new SearchProgress();
								prog?.Invoke(sp);
								var namespaces = uNodeEditor.SavedData.favoriteNamespaces;
								var allTypes = GetNamespaceTypes(namespaces, (currProgress) => {
									prog?.Invoke(new SearchProgress() { progress = currProgress, info = "Searching on favorite namespaces" });
								}, ignoreIncludedAssemblies: true);
								sp.info = "Setup Items";
								for(int i = 0; i < allTypes.Count; i++) {
									var pair = allTypes[i];
									var nsTree = new SelectorCategoryTreeView(pair.Key, "", uNodeEditorUtility.GetUIDFromString("[FAV-NS-SEARCH]" + pair.Key), -1);
									foreach(var type in pair.Value) {
										nsTree.AddChild(new TypeTreeView(type, type.GetHashCode(), -1));
									}
									treeResult.Add(nsTree);
									sp.progress = (float)i / (float)allTypes.Count;
									prog?.Invoke(sp);
								}
								return treeResult;
							}, "Search On Favorite Namespaces", uNodeEditorUtility.GetUIDFromString("[SAT]"), -1));
						}
						if(m_defaultExpandedItems != null) {
							foreach(var str in m_defaultExpandedItems) {
								foreach(var tree in categories) {
									if(tree.displayName == str) {
										if(tree is SelectorCategoryTreeView categ) {
											categ.expanded = true;
										}
										break;
									}
								}
							}
						}
						editorData.manager.Reload(categories);
						hasSetupMember = true;
						requiredRepaint = true;
						UnityEngine.Profiling.Profiler.EndSample();
					});
				}
				else {
					requiredRepaint = true;
				}
			}, this);
		}
		#endregion

		#region Private Functions
		static bool M_CanSelectType(Type type, FilterAttribute filter) {
			return filter == null || !filter.SetMember && (
				filter.CanSelectType && filter.IsValidType(type) ||
				filter.IsValidTarget(MemberData.TargetType.Values) && filter.IsValidTypeForValueConstant(type) ||
				filter.Types?.Count == 1 && filter.Types[0] == typeof(Type) && !(type is RuntimeType)
			);
		}
		#endregion

		static List<KeyValuePair<string, List<Type>>> GetNamespaceTypes(
			IEnumerable<string> namespaces,
			Action<float> onProgress = null,
			bool includeGlobal = false, bool
			includeExcludedType = false,
			bool ignoreIncludedAssemblies = false) {


			onProgress?.Invoke(0);
			Dictionary<string, List<Type>> typeMaps = new Dictionary<string, List<Type>>();
			var typeList = new List<KeyValuePair<string, List<Type>>>();
			var preference = uNodePreference.preferenceData;
			bool showObsolete = preference.showObsoleteItem;
			var ignoredTypes = uNodePreference.GetIgnoredTypes();
			var assemblies = EditorReflectionUtility.GetAssemblies();
			var includedAssemblies = uNodePreference.GetIncludedAssemblies();
			var excludedNamespaces = uNodePreference.GetExcludedNamespace();
			var excludedTypes = uNodePreference.GetExcludedTypes();
			var globalUsingNamespaces = uNodePreference.GetGlobalUsingNamespaces();

			Func<Type, string, bool> typeFilter;
			bool isIncludedAssembly = false;

			if(ignoreIncludedAssemblies) {
				typeFilter = (type, ns) => {
					return namespaces.Contains(ns);
				};
			}
			else {
				if(preference.filterIncludedNamespaces) {
					typeFilter = (type, ns) => {
						if(!namespaces.Contains(ns)) {
							//Auto include global using namespaces
							return isIncludedAssembly && globalUsingNamespaces.Contains(ns);
						}
						return true;
					};
				}
				else {
					typeFilter = (type, ns) => {
						if(!namespaces.Contains(ns)) {
							//Exclude namespaces
							return isIncludedAssembly && !excludedNamespaces.Contains(ns);
						}
						return true;
					};
				}
			}

			for(int i = 0; i < assemblies.Length; i++) {
				var assembly = assemblies[i];
				string assemblyName = assembly.GetName().Name;
				isIncludedAssembly = includedAssemblies.Contains(assemblyName);

				foreach(var type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
					string ns = type.Namespace;
					if(string.IsNullOrEmpty(ns)) {
						ns = "global";
						if(!includeGlobal && !namespaces.Contains(ns) || !ignoreIncludedAssemblies && !isIncludedAssembly) {
							continue;
						}
					}
					else {
						if(typeFilter(type, ns) == false)
							continue;
					}

					if(type.IsNotPublic ||
						!type.IsVisible ||
						//type.IsEnum ||
						//type.IsInterface ||
						//type.IsCOMObject ||
						//type.IsGenericType ||
						type.Name.StartsWith("<", StringComparison.Ordinal) ||
						type.IsNested ||
						ignoredTypes.Contains(type) ||
						//type.IsCastableTo(typeof(Delegate)) ||
						!showObsolete && (type.IsDefinedAttribute(typeof(ObsoleteAttribute)) || type.IsDefinedAttribute(typeof(System.ComponentModel.EditorBrowsableAttribute))) ||
						!includeExcludedType && excludedTypes.Contains(type.FullName))
						continue;

					//if(excludedNS.Contains(ns)) {
					//	continue;
					//}
					List<Type> types;
					if(!typeMaps.TryGetValue(ns, out types)) {
						types = new List<Type>();
						typeMaps[ns] = types;
					}
					types.Add(type);
					//typeCount++;
				}
				onProgress?.Invoke((float)i / (float)assemblies.Length);
			}
			typeList = typeMaps.ToList();
			foreach(var list in typeList) {
				list.Value.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
			typeList.Sort((x, y) => {
				if(x.Key == "global") {
					if(y.Key == "global") {
						return 0;
					}
					return -1;
				}
				else if(y.Key == "global") {
					return 1;
				}
				//if(x.Key.StartsWith("Unity", StringComparison.Ordinal)) {
				//	if(y.Key.StartsWith("Unity", StringComparison.Ordinal)) {
				//		return string.Compare(x.Key, y.Key, StringComparison.Ordinal);
				//	}
				//	return -1;
				//} else if(y.Key.StartsWith("Unity", StringComparison.Ordinal)) {
				//	return 1;
				//}
				return string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
			});
			onProgress?.Invoke(1);
			return typeList;
		}

		static List<KeyValuePair<string, List<Type>>> GetAllTypes(Action<SearchProgress> onProgress = null, bool includeGlobal = false, bool includeExcludedType = false) {
			var progress = new SearchProgress();
			onProgress?.Invoke(progress);
			Dictionary<string, List<Type>> typeMaps = new Dictionary<string, List<Type>>();
			var typeList = new List<KeyValuePair<string, List<Type>>>();
			var showObsolete = uNodePreference.preferenceData.showObsoleteItem;
			var ignoredTypes = uNodePreference.GetIgnoredTypes();
			var excludedTypes = uNodePreference.GetExcludedTypes();
			var assemblies = EditorReflectionUtility.GetAssemblies();
			for(int i = 0; i < assemblies.Length; i++) {
				var assembly = assemblies[i];
				string assemblyName = assembly.GetName().Name;
				foreach(var type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
					string ns = type.Namespace;
					if(string.IsNullOrEmpty(ns)) {
						ns = "global";
						if(!includeGlobal) {
							continue;
						}
					}
					if(type.IsNotPublic ||
						!type.IsVisible ||
						//type.IsEnum ||
						//type.IsInterface ||
						//type.IsCOMObject ||
						//type.IsGenericType ||
						type.Name.StartsWith("<", StringComparison.Ordinal) ||
						type.IsNested ||
						ignoredTypes.Contains(type) ||
						//type.IsCastableTo(typeof(Delegate)) ||
						!showObsolete && (type.IsDefinedAttribute(typeof(ObsoleteAttribute)) || type.IsDefinedAttribute(typeof(System.ComponentModel.EditorBrowsableAttribute))) ||
						!includeExcludedType && excludedTypes.Contains(type.FullName))
						continue;

					//if(excludedNS.Contains(ns)) {
					//	continue;
					//}
					List<Type> types;
					if(!typeMaps.TryGetValue(ns, out types)) {
						types = new List<Type>();
						typeMaps[ns] = types;
					}
					types.Add(type);
					//typeCount++;
				}
				progress.progress = (float)i / (float)assemblies.Length;
				progress.info = "Get Types on:" + assemblyName;
				onProgress?.Invoke(progress);
			}
			typeList = typeMaps.ToList();
			foreach(var list in typeList) {
				list.Value.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
			typeList.Sort((x, y) => {
				if(x.Key == "global") {
					if(y.Key == "global") {
						return 0;
					}
					return -1;
				}
				else if(y.Key == "global") {
					return 1;
				}
				//if(x.Key.StartsWith("Unity", StringComparison.Ordinal)) {
				//	if(y.Key.StartsWith("Unity", StringComparison.Ordinal)) {
				//		return string.Compare(x.Key, y.Key, StringComparison.Ordinal);
				//	}
				//	return -1;
				//} else if(y.Key.StartsWith("Unity", StringComparison.Ordinal)) {
				//	return 1;
				//}
				return string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
			});
			progress.progress = 1;
			onProgress?.Invoke(progress);
			return typeList;
		}

		#region Utility 
		public class Utility {
			public static List<GUIContent> GetTooltipContents(MemberInfo member) {
				var contents = new List<GUIContent>();

				string displayName;
				if(uNodePreference.preferenceData.coloredItem) {
					displayName = NodeBrowser.GetRichMemberName(member);
				}
				else {
					displayName = NodeBrowser.GetPrettyMemberName(member);
				}
				Texture icon = uNodeEditorUtility.GetIcon(member);
				contents.Add(new GUIContent(displayName, icon));
				contents.Add(new GUIContent("Target	: " + member.MemberType));
				contents.Add(new GUIContent("Static	: " + ReflectionUtils.GetMemberIsStatic(member)));
				var mType = ReflectionUtils.GetMemberType(member);
				contents.Add(new GUIContent("Return	: " + mType.PrettyName(true), uNodeEditorUtility.GetTypeIcon(mType)));
				if(XmlDoc.hasLoadDoc) {
					if(member is ISummary summary) {
						if(!string.IsNullOrEmpty(summary.GetSummary())) {
							contents.Add(new GUIContent("<b>Documentation</b> ▼ " + summary.GetSummary().AddLineInFirst()));
						}
						if(member is RuntimeMethod) {
							var parameters = (member as RuntimeMethod).GetParameters();
							for(int x = 0; x < parameters.Length; x++) {
								var PType = parameters[x].ParameterType;
								contents.Add(new GUIContent("<b>" + parameters[x].Name + " : " + PType.PrettyName() + "</b>",
										uNodeEditorUtility.GetTypeIcon(PType)));
								if(parameters[x] is ISummary s && !string.IsNullOrEmpty(s.GetSummary())) {
									contents.Add(new GUIContent(s.GetSummary()));
								}
							}
						}
					}
					else {
						XmlElement documentation = XmlDoc.XMLFromMember(member);
						if(documentation != null) {
							contents.Add(new GUIContent("<b>Documentation ▼</b> " + documentation["summary"].InnerText.Trim().AddLineInFirst()));
						}
						switch(member.MemberType) {
							case MemberTypes.Method:
							case MemberTypes.Constructor:
								var parameters = (member as MethodBase).GetParameters();
								for(int x = 0; x < parameters.Length; x++) {
									System.Type PType = parameters[x].ParameterType;
									if(PType != null) {
										contents.Add(new GUIContent("<b>" + parameters[x].Name + " : " + PType.PrettyName() + "</b>",
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
								break;
						}
					}
				}
				return contents;
			}

			public static List<GUIContent> GetTooltipContents(TreeViewItem tree, bool onlyGetType = false) {
				List<GUIContent> contents = new List<GUIContent>();
				if(tree is TypeTreeView) {
					var item = tree as TypeTreeView;
					Texture icon;
					if(onlyGetType) {
						icon = uNodeEditorUtility.GetTypeIcon(item.type);
					}
					else {
						icon = uNodeEditorUtility.GetIcon(item.type);
					}
					contents.Add(new GUIContent(item.displayName, icon));
					contents.Add(new GUIContent("Target	: Type"));
					contents.Add(new GUIContent("Static	: True"));
					contents.Add(new GUIContent("Type	: " + item.type.PrettyName(true), uNodeEditorUtility.GetTypeIcon(item.type)));
					contents.AddRange(LoadDoc(item.type));
				}
				else if(tree is MemberTreeView) {
					var item = tree as MemberTreeView;
					contents = GetTooltipContents(item.member);
				}
				else if(tree is SelectorCustomTreeView) {
					SelectorCustomTreeView item = tree as SelectorCustomTreeView;
					if(item.item != null) {
						if(item.item.tooltip != null && !string.IsNullOrEmpty(item.item.tooltip.text)) {
							if(item.item.tooltip.text.Contains("\n")) {
								string[] str = item.item.tooltip.text.Split('\n');
								for(int i = 0; i < str.Length; i++) {
									if(i == 0) {
										contents.Add(new GUIContent(str[i], item.item.tooltip.image));
										continue;
									}
									contents.Add(new GUIContent(str[i]));
								}
							}
							else {
								contents.Add(item.item.tooltip);
							}
						}
						else if(item.item is ItemReflection ri && ri.item != null && ri.item.memberInfo != null) {
							contents = GetTooltipContents(ri.item.memberInfo);
						}
					}
					else if(item.graphItem != null) {
						Texture icon;
						switch(item.graphItem.targetType) {
							case MemberData.TargetType.Self:
								icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.KeywordIcon));
								break;
							case MemberData.TargetType.uNodeVariable:
								icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FieldIcon));
								break;
							case MemberData.TargetType.uNodeLocalVariable:
								icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.LocalVariableIcon));
								break;
							case MemberData.TargetType.uNodeProperty:
								icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.PropertyIcon));
								break;
							case MemberData.TargetType.uNodeConstructor:
							case MemberData.TargetType.uNodeFunction:
								icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.MethodIcon));
								break;
							case MemberData.TargetType.uNodeParameter:
							case MemberData.TargetType.uNodeGenericParameter:
								icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.LocalVariableIcon));
								break;
							default:
								icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.KeywordIcon));
								break;
						}
						contents.Add(new GUIContent(item.graphItem.DisplayName, icon));
						contents.Add(new GUIContent("TargetType : " + item.graphItem.targetType));
						if(item.graphItem.type != null) {
							contents.Add(new GUIContent("Type : " + item.graphItem.type.PrettyName(true), uNodeEditorUtility.GetTypeIcon(item.graphItem.type)));
						}
						if(item.graphItem.toolTip != null && !string.IsNullOrEmpty(item.graphItem.toolTip)) {
							contents.Add(new GUIContent("Documentation ▼"));
							if(item.graphItem.toolTip.Contains("\n")) {
								string[] str = item.graphItem.toolTip.Split('\n');
								for(int i = 0; i < str.Length; i++) {
									if(i == 0) {
										contents.Add(new GUIContent(str[i]));
										continue;
									}
									contents.Add(new GUIContent(str[i]));
								}
							}
							else {
								contents.Add(new GUIContent(item.graphItem.toolTip));
							}
						}
					}
				}
				return contents;
			}

			#region Doc
			private static List<GUIContent> LoadDoc(MemberInfo member) {
				var contents = new List<GUIContent>();
				if(XmlDoc.hasLoadDoc) {
					if(member != null) {
						if(member is ISummary summary) {
							if(!string.IsNullOrEmpty(summary.GetSummary())) {
								contents.Add(new GUIContent("<b>Documentation</b> ▼ " + summary.GetSummary().AddLineInFirst()));
							}
							if(member is RuntimeMethod) {
								var parameters = (member as RuntimeMethod).GetParameters();
								for(int x = 0; x < parameters.Length; x++) {
									var PType = parameters[x].ParameterType;
									contents.Add(new GUIContent("<b>" + parameters[x].Name + " : " + PType.PrettyName() + "</b>",
											uNodeEditorUtility.GetTypeIcon(PType)));
									if(parameters[x] is ISummary s && !string.IsNullOrEmpty(s.GetSummary())) {
										contents.Add(new GUIContent(s.GetSummary()));
									}
								}
							}
						}
						else {
							XmlElement documentation = XmlDoc.XMLFromMember(member);
							if(documentation != null) {
								contents.Add(new GUIContent("<b>Documentation ▼</b> " + documentation["summary"].InnerText.Trim().AddLineInFirst()));
							}
							switch(member.MemberType) {
								case MemberTypes.Method:
								case MemberTypes.Constructor:
									var parameters = (member as MethodBase).GetParameters();
									for(int x = 0; x < parameters.Length; x++) {
										System.Type PType = parameters[x].ParameterType;
										if(PType != null) {
											contents.Add(new GUIContent("<b>" + parameters[x].Name + " : " + PType.PrettyName() + "</b>",
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
									break;
							}
						}
					}
					//else if(member != null) {
					//	if(member is ISummary summary) {
					//		if(!string.IsNullOrEmpty(summary.GetSummary())) {
					//			contents.Add(new GUIContent("<b>Documentation</b> ▼ " + summary.GetSummary().AddLineInFirst()));
					//		}
					//	} else {
					//		XmlElement documentation = XmlDoc.XMLFromType(member);
					//		if(documentation != null) {
					//			contents.Add(new GUIContent("<b>Documentation</b> ▼ " + documentation["summary"].InnerText.Trim().AddLineInFirst()));
					//		}
					//	}
					//}
				}
				return contents;
			}
			#endregion
		}
		#endregion
	}
}