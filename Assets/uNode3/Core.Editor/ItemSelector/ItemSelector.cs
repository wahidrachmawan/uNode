using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	public partial class ItemSelector {
		private Data editorData = new Data();

		private static readonly string[] defaultUsingNamespace = new[] { "UnityEngine" };

		public const string CategoryInherited = "Inherit Members";

		public Object targetUnityObject {
			get {
				if(targetObject is UGraphElement)
					return (targetObject as UGraphElement).graphContainer as Object;
				return targetObject as Object;
			}
		}

		public object targetObject;
		public List<CustomItem> customItems = new List<CustomItem>();
		public bool customItemDefaultExpandState = true;
		
		private IEnumerable<string> m_defaultExpandedItems;
		public IEnumerable<string> defaultExpandedItems {
			set {
				m_defaultExpandedItems = value;
			}
			private get => m_defaultExpandedItems;
		}

		public bool canSearch = true,
			displayNoneOption = true,
			displayCustomVariable = true,
			displayDefaultItem = true,
			displayGeneralType = true,
			displayRecentItem = true;
		public Func<List<CustomItem>> favoriteHandler;

		public static int MinWordForDeepTypeSearch {
			get {
				return preferenceData.minDeepTypeSearch;
			}
		}
		public Action<MemberData> selectCallback {
			get => editorData.selectCallback;
			set => editorData.selectCallback = value;
		}
		/// <summary>
		/// The Item Selector using namespaces.
		/// Note: for change the using namespaces, call immediately after showing window and do not delay even one frame otherwise it will not work.
		/// </summary>
		public HashSet<string> usingNamespaces {
			get => editorData.usingNamespaces;
			set => editorData.usingNamespaces = value;
		}

		#region PrivateFields
		private FilterAttribute filter {
			get => editorData.filter;
			set => editorData.filter = value;
		}
		private bool _hasFocus, requiredRepaint;
		#endregion

		#region ShowItems
		static bool IsCorrectItem(ParameterData item, FilterAttribute filter) {
			if (item != null) {
				if (filter != null) {
					return !filter.OnlyGetType && filter.UnityReference;
				}
				return true;
			}
			return false;
		}

		static bool IsCorrectItem(GenericParameterData item, FilterAttribute filter) {
			if (item != null) {
				if (filter != null) {
					if (!(filter.CanSelectType && filter.UnityReference)) {
						return false;
					}
					return filter.IsValidType(typeof(System.Type));
				}
				return true;
			}
			return false;
		}

		static bool IsCorrectItem(Function item, FilterAttribute filter) {
			if (item != null) {
				if (filter != null) {
					if (!filter.IsValidType(item.ReturnType()))
						return false;
					if (item.parameters != null) {
						if (filter.MaxMethodParam < item.parameters.Count) {
							return false;
						}
						if (filter.MinMethodParam > item.parameters.Count) {
							return false;
						}
					}
					if (item.genericParameters != null && !filter.DisplayGenericType && item.genericParameters.Length > 0) {
						return false;
					}
					return filter.IsValidTarget(MemberTypes.Method);
				}
				return true;
			}
			return false;
		}
		#endregion

		#region Select
		public void Select(MemberData member) {
			if(selectCallback != null) {
				selectCallback(member);
			}
			Close();
		}

		static bool HasRuntimeType(IList<MemberData> members) {
			for (int i = 0; i < members.Count; i++) {
				var m = members[i];
				if (m.targetType == MemberData.TargetType.uNodeType) {
					return true;
				}
			}
			return false;
		}

		static bool IsGenericParameter(Type type) {
			return type.IsGenericParameter ||
				type.HasElementType && IsGenericParameter(type.GetElementType()) ||
				type.GetGenericArguments().Any(x => IsGenericParameter(x));
		}

		static bool IsGenericTypeDefinition(Type type) {
			return type.IsGenericTypeDefinition ||
				type.HasElementType && IsGenericTypeDefinition(type.GetElementType()) ||
				type.GetGenericArguments().Any(x => IsGenericTypeDefinition(x));
		}
		#endregion

		#region Others
		public static void SortCustomItems(List<CustomItem> customItems) {
			customItems.Sort((x, y) => {
				int index = string.Compare(x.category, y.category, StringComparison.OrdinalIgnoreCase);
				if (index == 0) {
					return string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase);
				}
				return index;
			});
		}

		public static List<GraphItem> GetGraphItems(object target, FilterAttribute filter = null) {
			if(target is Node) {
				target = (target as Node).nodeObject;
			}
			if(target is UGraphElement) {
				return new List<GraphItem>();
			} else {
				List<GraphItem> ESItems = new List<GraphItem>();
				var VS = target as IGraphWithVariables;
				var PS = target as IGraphWithProperties;
				if(VS != null)
					ESItems.AddRange((VS as IGraph).GetVariables().Select(item => new GraphItem(item, target)));
				if(PS != null)
					ESItems.AddRange((PS as IGraph).GetProperties().Select(item => new GraphItem(item, target)));
				if(target is IGraph) {
					if(filter == null || !filter.SetMember && filter.ValidMemberType.HasFlags(MemberTypes.Method) && filter.IsValidTarget(MemberTypes.Method)) {
						ESItems.AddRange((target as IGraph).GetFunctions().Where(item => IsCorrectItem(item, filter)).Select(item => new GraphItem(item, target)));
					}
				}
				ESItems.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
				RemoveIncorrectGraphItem(ESItems, filter);
				return ESItems;
			}
		}

		public static List<CustomItem> MakeExtensionItems(Type type, ICollection<string> ns, FilterAttribute filter, string category = "Data") {
			List<CustomItem> customItems = new List<CustomItem>();
			if(type.IsByRefLike) {
				//Skip if type is by ref structure and avoid Unity crash
				return customItems;
			}
			var assemblies = EditorReflectionUtility.GetAssemblies();
			foreach (var assembly in assemblies) {
				var extensions = EditorReflectionUtility.GetExtensionMethods(assembly, type, (mi) => {
					var nsName = mi.DeclaringType.Namespace;
					return string.IsNullOrEmpty(nsName) || ns.Contains(nsName);
				});
				if (extensions.Count > 0) {
					customItems.AddRange(MakeCustomItems(extensions.Select(item => item as MemberInfo), filter, category));
				}
			}
			customItems.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));
			return customItems;
		}

		public static List<CustomItem> MakeCustomTypeItems(ICollection<Type> types, string category = "Data") {
			List<EditorReflectionUtility.ReflectionItem> items = new List<EditorReflectionUtility.ReflectionItem>();
			foreach (Type type in types) {
				items.Add(GetItemFromType(type, null));
			}
			return items.Select(i => CustomItem.Create(i.displayName, i, category: category)).ToList();
		}

		public static List<CustomItem> MakeCustomItemsForMacros(UGraphElement canvas, Vector2 position, NodeFilter nodeFilter, Type type, Action<Node> onAddNode = null, string category = "Macros") {
			var customItems = new List<CustomItem>();
			var macros = GraphUtility.FindGraphs<MacroGraph>();
			foreach(var macro in macros) {
				var m = macro;
				try {
					switch(nodeFilter) {
						case NodeFilter.FlowInput:
							if(m.outputFlows.Any() == false) {
								continue;
							}
							break;
						case NodeFilter.FlowOutput:
							if(m.inputFlows.Any() == false) {
								continue;
							}
							break;
						case NodeFilter.ValueInput:
							if(m.outputValues.Any(p => p.type == typeof(object) || p.type.type.IsCastableTo(type)) == false) {
								continue;
							}
							break;
						case NodeFilter.ValueOutput:
							if(m.inputValues.Any(p => p.type == typeof(object) || p.type.type.IsCastableTo(type)) == false) {
								continue;
							}
							break;
					}
				}
				catch {
					continue;
				}
				customItems.Add(ItemSelector.CustomItem.Create(m.GetGraphName(), () => {
					NodeEditorUtility.AddNewNode<Nodes.LinkedMacroNode>(canvas, null, null, position, (node) => {
						node.macroAsset = macro;
						node.Refresh();
						node.Register();
						NodeEditorUtility.AutoAssignNodePorts(node);
						onAddNode?.Invoke(node);
					});
				}, 
				category.Add(".") + m.category,
				icon: uNodeEditorUtility.GetTypeIcon(m.GetIcon()),
				tooltip: new GUIContent(m.GraphData.comment)));
			}
			return customItems;
		}

		public static List<CustomItem> MakeCustomItems(object target, FilterAttribute filter = null, string category = "Data") {
			if (target == null)
				return null;
			var items = GetGraphItems(target, filter);
			return items.Select(i => CustomItem.Create(i, category: category)).ToList();
		}

		public static List<CustomItem> MakeCustomItems(Type type, FilterAttribute filter = null, string category = "Data", string inheritCategory = "") {
			if(type == null)
				return null;
			if(filter == null)
				filter = FilterAttribute.Default;
			if(string.IsNullOrEmpty(inheritCategory)) {
				var items = EditorReflectionUtility.AddGeneralReflectionItems(type, filter);
				items.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.OrdinalIgnoreCase));
				return items.Select(i => CustomItem.Create(i.displayName, i, category: category)).ToList();
			} else {
				var items = EditorReflectionUtility.AddGeneralReflectionItems(type, filter);
				items.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.OrdinalIgnoreCase));
				var result = items.Select(i => CustomItem.Create(i.displayName, i, category: category)).ToList();
				var inheritItems = new List<CustomItem>();
				for(int i = 0; i < result.Count; i++) {
					var item = result[i] as ItemReflection;
					if(item.item.memberInfo?.DeclaringType != type) {
						inheritItems.Add(result[i]);
						result.RemoveAt(i);
						i--;
					}
				}
				if(result.Count == 0) {
					result = inheritItems;
				} else {
					for(int i = 0; i < inheritItems.Count; i++) {
						inheritItems[i].category = inheritCategory;
						result.Add(inheritItems[i]);
					}
				}
				return result;
			}
		}

		public static List<CustomItem> MakeCustomItems(IEnumerable<MemberInfo> members, FilterAttribute filter, string category = "Data") {
			var items = EditorReflectionUtility.AddGeneralReflectionItems(null, members, filter);
			items.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.OrdinalIgnoreCase));
			return items.Select(i => CustomItem.Create(i.displayName, i, category: category)).ToList();
		}

		public static List<CustomItem> MakeCustomItems(Type type, object instance, FilterAttribute filter, string category = "Data") {
			if (type == null)
				return null;
			List<EditorReflectionUtility.ReflectionItem> items = null;
			if (instance is IGraph) {
				if(instance is IClassGraph) {
					items = EditorReflectionUtility.AddGeneralReflectionItems((instance as IClassGraph).InheritType, new FilterAttribute(filter) { Static = false });
				} else {
					throw new NotImplementedException();
				}
			} else {
				items = EditorReflectionUtility.AddGeneralReflectionItems(type, new FilterAttribute(filter) { Static = false });
			}
			if (items != null) {
				bool flag = false;
				//TODO: Fixme
				//if (instance is IGraph) {
				//	var root = instance as IGraph;
				//	var data = root.GetComponent<uNodeData>();
				//	flag = data == null ? type.Name == root.Name : type.Name == root.Name && data.generatorSettings.Namespace == type.Namespace;
				//}
				if (filter != null && !filter.SetMember && (filter.IsValidType(type) || flag) && filter.IsValidTarget(MemberData.TargetType.Self)) {
					var item = new EditorReflectionUtility.ReflectionItem() {
						canSelectItems = true,
						hasNextItems = false,
						isStatic = false,
						memberInfo = null,
						memberType = type,
						instance = new MemberData("this", type, MemberData.TargetType.Self) { instance = instance },
					};
					items.Insert(0, item);
				}
				//RemoveIncorrectGeneralItem(TargetType);
				items.RemoveAll(i => i.memberInfo != null && i.memberInfo.MemberType == MemberTypes.Constructor);
				items.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.OrdinalIgnoreCase));
			}
			items.ForEach(item => { if (item.instance == null) item.instance = instance; });
			return items.Select(i => CustomItem.Create(i.displayName, i, category: category)).ToList();
		}

		public static List<CustomItem> MakeCustomItems(Type type, FilterAttribute filter,
			Func<EditorReflectionUtility.ReflectionItem, bool> validation, string category = "Data") {
			if (type == null)
				return null;
			var items = EditorReflectionUtility.AddGeneralReflectionItems(type, filter);
			items.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.OrdinalIgnoreCase));
			return items.Where(i => validation(i)).Select(i => CustomItem.Create(i.displayName, i, category: category)).ToList();
		}

		public static List<CustomItem> MakeCustomItemsForInstancedType(Type[] types, Action<object> onClick, bool allowSceneObject, Func<Object, bool> validation = null) {
			var items = new List<CustomItem>();
			foreach (var type in types) {
				IEnumerable<Object> objects;
				if (type.IsCastableTo(typeof(IGraph))) {
					objects = GraphUtility.FindGraphs(type);
				}
				else if(type.IsCastableTo(typeof(Component))) {
					objects = uNodeEditorUtility.FindComponentInPrefabs(type);
				} else {
					objects = uNodeEditorUtility.FindAssetsByType(type);
				}
				foreach (var c in objects) {
					if (c.GetType().IsCastableTo(type) && (validation == null || validation(c))) {
						items.Add(ItemSelector.CustomItem.Create($"{uNodeUtility.GetObjectName(c)} ({c.GetType().PrettyName()})", onClick, c, "Project", icon: uNodeEditorUtility.GetTypeIcon(c)));
					}
				}
				if (allowSceneObject) {
					var objs = GameObject.FindObjectsOfType<MonoBehaviour>();
					foreach (var c in objs) {
						if (c.GetType().IsCastableTo(type) && (validation == null || validation(c))) {
							items.Add(ItemSelector.CustomItem.Create($"{c.gameObject.name} ({c.GetType().PrettyName()})", onClick, c, "Scene", icon: uNodeEditorUtility.GetTypeIcon(c)));
						}
					}
				}
			}
			return items;
		}

		public static List<CustomItem> MakeCustomItemsForInstancedType<T>(Action<object> onClick, bool allowSceneObject) where T : UnityEngine.Object {
			var items = new List<CustomItem>();
			//TODO: fixme
			//if (typeof(T).IsCastableTo(typeof(Component))) {
			//	List<T> components;
			//	if(typeof(T).IsCastableTo(typeof(uNodeComponentSystem))) {
			//		components = GraphUtility.FindGraphComponents<T>();
			//	} else {
			//		components = uNodeEditorUtility.FindComponentInPrefabs<T>();
			//	}
			//	foreach (var c in components) {
			//		if (c is Component comp) {
			//			items.Add(ItemSelector.CustomItem.Create($"{comp.gameObject.name} ({c.GetType().PrettyName()})", onClick, c, "Project", icon: uNodeEditorUtility.GetTypeIcon(c)));
			//		}
			//	}
			//	if (allowSceneObject) {
			//		var objs = GameObject.FindObjectsOfType<T>();
			//		foreach (var c in objs) {
			//			items.Add(ItemSelector.CustomItem.Create($"{(c as Component).gameObject.name} ({c.GetType().PrettyName()})", onClick, c, "Scene", icon: uNodeEditorUtility.GetTypeIcon(c)));
			//		}
			//	}
			//}
			return items;
		}

		public static List<CustomItem> MakeCustomItemsForInstancedType(Type type, Action<object> onClick, bool allowSceneObject) {
			var items = new List<ItemSelector.CustomItem>();
			var icon = uNodeEditorUtility.GetTypeIcon(type);
			if (type.IsCastableTo(typeof(Component))) {
				var components = uNodeEditorUtility.FindComponentInPrefabs<IRuntimeComponent>();
				foreach (var c in components) {
					if (c is IGraph) continue; //Ensure to continue if it's a graph
					if(c.IsTypeOf(type) == false) continue;
					items.Add(ItemSelector.CustomItem.Create($"{(c as Component).gameObject.name} ({c.GetType().PrettyName()})", onClick, c, "Project", icon: icon));
				}
				if (allowSceneObject) {
					var objs = GameObject.FindObjectsOfType<MonoBehaviour>();
					foreach (var c in objs) {
						if(c.IsTypeOf(type) == false) continue;
						items.Add(ItemSelector.CustomItem.Create($"{c.gameObject.name} ({c.GetType().PrettyName()})", onClick, c, "Scene", icon: icon));
					}
				}
			}
			else if (type.IsCastableTo(typeof(ScriptableObject))) {
				var assets = uNodeEditorUtility.FindAssetsByType<ScriptableObject>();
				foreach (var c in assets) {
					if(c is IGraph) continue; //Ensure to continue if it's a graph
					if(c.IsTypeOf(type) == false) continue; 
					items.Add(ItemSelector.CustomItem.Create($"{c.name} ({type.Name})", onClick, c, "Project", icon: icon));
				}
			}
			else if (type.IsInterface) {
				var components = uNodeEditorUtility.FindComponentInPrefabs<IInstancedGraph>();
				foreach (var c in components) {
					if(c.IsTypeOf(type) == false) continue;
					//if (c.OriginalGraph as Object) continue; //Ensure to continue if it's a graph
					//if (!ReflectionUtils.GetRuntimeType(c.OriginalGraph).HasImplementInterface(type)) continue;
					items.Add(ItemSelector.CustomItem.Create($"{(c as Component).gameObject.name} ({c.GetType().PrettyName()})", onClick, c, "Project", icon: icon));
				}
				var assets = uNodeEditorUtility.FindAssetsByType<ScriptableObject>();
				foreach (var c in assets) {
					if(c.IsTypeOf(type) == false) continue;
					items.Add(ItemSelector.CustomItem.Create($"{c.name} ({type.FullName})", onClick, c, "Project", icon: icon));
					//if(c is IInstancedGraph instancedGraph && instancedGraph.OriginalGraph as Object) {
					//	if(!ReflectionUtils.GetRuntimeType(instancedGraph.OriginalGraph).HasImplementInterface(type)) continue;
					//}
				}
				if (allowSceneObject) {
					var objs = GameObject.FindObjectsOfType<MonoBehaviour>();
					foreach (var c in objs) {
						if(c.IsTypeOf(type) == false) continue;
						items.Add(ItemSelector.CustomItem.Create($"{c.gameObject.name} ({c.GetType().PrettyName()})", onClick, c, "Scene", icon: icon));
						//if (c is IInstancedGraph instancedGraph && instancedGraph.OriginalGraph as Object) {
						//	if (!ReflectionUtils.GetRuntimeType(instancedGraph.OriginalGraph).HasImplementInterface(type)) continue;
						//	items.Add(ItemSelector.CustomItem.Create($"{c.gameObject.name} ({c.GetType().PrettyName()})", onClick, c, "Scene", icon: icon));
						//}
					}
				}
			}
			else {
				throw new InvalidOperationException();
			}
			return items;
		}

		public static List<TreeViewItem> MakeFavoriteTrees(Func<List<CustomItem>> favoriteHandler, FilterAttribute filter) {
			var result = new List<TreeViewItem>();
			if(favoriteHandler != null) {
				var customItems = favoriteHandler();
				if(customItems != null) {
					List<SelectorCategoryTreeView> categTrees = new List<SelectorCategoryTreeView>();
					foreach(var item in customItems) {
						var categ = categTrees.FirstOrDefault(t => t.category == item.category);
						if(categ == null) {
							categ = new SelectorCategoryTreeView(item.category, "", uNodeEditorUtility.GetUIDFromString("[CATEG]" + item.category), -1);
							categ.expanded = true;
							categTrees.Add(categ);
						}
						categ.AddChild(new SelectorCustomTreeView(item, item.GetHashCode(), -1));
					}
					categTrees.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.OrdinalIgnoreCase));
					foreach(var categ in categTrees) {
						result.Add(categ);
					}
				}
			}
			var favorites = uNodeEditor.SavedData.favoriteItems;
			if(favorites != null) {//Favorite Type and Members
				var typeTrees = new List<TypeTreeView>();
				var memberTrees = new List<MemberTreeView>();
				foreach(var fav in favorites) {
					if(fav.info != null && (filter == null || filter.IsValidMember(fav.info))) {
						if(fav.info is Type type) {
							typeTrees.Add(new TypeTreeView(type));
						} else {
							var tree = new MemberTreeView(fav.info);
							tree.displayName = tree.member.DeclaringType.Name + "." + tree.displayName;
							memberTrees.Add(tree);
						}
					}
				}
				typeTrees.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.OrdinalIgnoreCase));
				memberTrees.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.OrdinalIgnoreCase));
				var typeCategory = new SelectorCategoryTreeView("Types", "", uNodeEditorUtility.GetUIDFromString("[TYPES]"), -1);
				typeCategory.expanded = true;
				foreach(var tree in typeTrees) {
					typeCategory.AddChild(tree);
				}
				var memberCategory = new SelectorCategoryTreeView("Members", "", uNodeEditorUtility.GetUIDFromString("[MEMBERS]"), -1);
				memberCategory.expanded = true;
				foreach(var tree in memberTrees) {
					memberCategory.AddChild(tree);
				}
				if(typeCategory.hasChildren)
					result.Add(typeCategory);
				if(memberCategory.hasChildren)
					result.Add(memberCategory);
			}
			{//Favorite Namespaces
				var nsTrees = new List<NamespaceTreeView>();
				foreach(var fav in uNodeEditor.SavedData.favoriteNamespaces) {
					nsTrees.Add(new NamespaceTreeView(fav, uNodeEditorUtility.GetUIDFromString("[NS-FAV]" + fav), -1));
				}
				nsTrees.Sort((x, y) => string.Compare(x.displayName, y.displayName, StringComparison.OrdinalIgnoreCase));
				var nsCategory = new SelectorCategoryTreeView("Namespaces", "", uNodeEditorUtility.GetUIDFromString("[NS]"), -1);
				nsCategory.expanded = true;
				foreach(var tree in nsTrees) {
					nsCategory.AddChild(tree);
				}
				if(nsCategory.hasChildren)
					result.Add(nsCategory);
			}
			return result;
		}

		public static void RemoveIncorrectGraphItem(List<GraphItem> RESData, FilterAttribute filter) {
			if (filter == null)
				return;
			for (int i = 0; i < RESData.Count; i++) {
				var var = RESData[i];
				bool canShow = true;
				if (canShow && filter.Types.Count > 0) {
					bool hasType = false;
					if (var.type == null || var.targetType == MemberData.TargetType.Self)
						continue;
					hasType = filter.IsValidType(var.type);
					if (!hasType) {
						canShow = false;
					}
					if (canShow && var.type != null) {
						if (filter.OnlyArrayType || filter.OnlyGenericType) {
							if (filter.OnlyGenericType && filter.OnlyArrayType) {
								canShow = var.type.IsArray || var.type.IsGenericType;
							} else if (filter.OnlyArrayType) {
								canShow = var.type.IsArray;
							} else if (filter.OnlyGenericType) {
								canShow = var.type.IsGenericType;
							}
						}
					}
					if (!canShow && !var.haveNextItem) {
						RESData.RemoveAt(i);
						i--;
					}
				}
			}
		}

		static EditorReflectionUtility.ReflectionItem GetItemFromType(Type type, FilterAttribute filter) {
			return new EditorReflectionUtility.ReflectionItem() {
				isStatic = true,
				memberInfo = type,
				canSelectItems = filter == null || 
					filter.CanSelectType && filter.IsValidType(type) || 
					filter.IsValidTarget(MemberData.TargetType.Values) && filter.IsValidTypeForValueConstant(type) ||
					filter.Types?.Count == 1 && filter.Types[0] == typeof(Type) && !(type is RuntimeType),
				hasNextItems = true,
				memberType = type,
			};
		}

		static List<Type> GetGeneralTypes() {
			List<Type> type = new List<Type>();
			type.Add(typeof(string));
			type.Add(typeof(float));
			type.Add(typeof(bool));
			type.Add(typeof(int));
			//type.Add(typeof(Enum));
			type.Add(typeof(Color));
			type.Add(typeof(Vector2));
			type.Add(typeof(Vector3));
			type.Add(typeof(Transform));
			type.Add(typeof(GameObject));
			//type.Add(typeof(uNodeRuntime));
			return type;
		}
		#endregion
	}
}