using System;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace MaxyGames.UNode.Editors {
	#region TreeViews
	public class TypeTreeView : MemberTreeView {
		public Type type;
		public FilterAttribute filter;

		private List<MemberTreeView> members;

		internal void Search(Func<MemberInfo, bool> validation) {
			children = new List<TreeViewItem>(ItemSelector.TreeFunction.CreateItemsFromType(type, filter, true, validation));
		}

		public void Expand(bool enable) {
			if(enable) {
				if(members == null) {
					members = ItemSelector.TreeFunction.CreateItemsFromType(type, filter, true);
				}
				if(members != null) {
					children = new List<TreeViewItem>(members);
				}
			} else {
				children = null;
			}
		}

		public TypeTreeView() {

		}

		public TypeTreeView(Type type) : base(type, type.GetHashCode(), -1) {
			this.type = type;
			member = type;
		}

		public TypeTreeView(Type type, int id, int depth) : base(type, id, depth) {
			this.type = type;
			member = type;
		}

		public static TypeTreeView Create(Type type) {
			return new TypeTreeView(type, uNodeEditorUtility.GetUIDFromString(type.FullName), -1);
		}
	}

	internal class NamespaceTreeView : TreeViewItem {
		public string Namespace;

		public NamespaceTreeView() {

		}

		public NamespaceTreeView(string Namespace, int id, int depth) : base(id, depth, Namespace) {
			this.Namespace = Namespace;
		}
	}

	public class MemberTreeView : TreeViewItem {
		public MemberInfo member;
		public object instance;

		public Func<bool> selectValidation;
		public Func<bool> nextValidation;

		public MemberTreeView() {

		}

		public MemberTreeView(MemberInfo member) : base(member.GetHashCode(), -1, NodeBrowser.GetMemberName(member)) {
			this.member = member;
		}

		public MemberTreeView(MemberInfo member, int id, int depth) : base(id, depth, NodeBrowser.GetMemberName(member)) {
			this.member = member;
		}

		public Texture GetIcon() {
			if(member is Type) {
				return uNodeEditorUtility.GetTypeIcon(member as Type);
			}
			return uNodeEditorUtility.GetIcon(member);
		}

		public bool CanSelect() {
			return selectValidation == null || selectValidation();
		}

		public bool HasDeepMember() {
			if(nextValidation != null) {
				return nextValidation();
			}
			return EditorReflectionUtility.ValidateNextMember(member, FilterAttribute.Default);
		}
	}

	internal class NodeTreeView : TreeViewItem {
		public NodeTreeData data;

		public NodeTreeView() {

		}

		public NodeTreeView(NodeTreeData data, int id, int depth) : base(id, depth, data.name) {
			this.data = data;
		}
	}

	internal class NodeTreeData {
		public string name;
		public NodeMenu menu;
		public INodeItemCommand command;
		public string category;
	}
	#endregion

	[System.Serializable]
	public class BrowserState : TreeViewState {
		public enum TypeKind {
			All,
			Function,
			Variable,
			Property,
			Type,
		}

		public SearchKind searchKind;
		public TypeKind typeKind;
		public string searchText;
	}

	public class NodeBrowser : TreeView {
		public EditorWindow window;

		private string _searchString;
		public new string searchString {
			get {
				return _searchString;
			}
			set {
				if(_searchString != value) {
					_searchString = value;
					SearchChanged();
				}
			}
		}

		private BrowserState browserState;
		private TooltipWindow tooltipWindow;
		private List<KeyValuePair<string, List<Type>>> typeList = new List<KeyValuePair<string, List<Type>>>();
		//private int typeCount;
		private static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		private Dictionary<string, NamespaceTreeView> namespaceTrees = new Dictionary<string, NamespaceTreeView>();
		private Dictionary<Type, TypeTreeView> typeTrees = new Dictionary<Type, TypeTreeView>();
		private Dictionary<MemberInfo, MemberTreeView> memberTrees = new Dictionary<MemberInfo, MemberTreeView>();

		#region Init
		public NodeBrowser(BrowserState state) : base(state) {
			browserState = state;
#if UNITY_2019_3_OR_NEWER
			showAlternatingRowBackgrounds = true;
#endif
			Init();
			Reload();
		}

		public void Init() {
			//int typeCount = 0;
			Dictionary<string, List<Type>> typeMaps = new Dictionary<string, List<Type>>();
			var namespaces = uNodePreference.GetBrowserNamespaceList();
			var excludedNS = uNodePreference.GetExcludedNamespace();
			var excludedTypes = uNodePreference.GetExcludedTypes();
			foreach(var assembly in EditorReflectionUtility.GetAssemblies()) {
				string assemblyName = assembly.GetName().Name;
				foreach(var type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
					string ns = type.Namespace;
					//if(ns.StartsWith("Unity.") || ns.Contains("Experimental") || ns.Contains("Internal")) {
					//	continue;
					//}
					if(string.IsNullOrEmpty(ns)) {
						ns = "global";
					} else if(!namespaces.Contains(ns)) {
						continue;
					}
					if(type.IsNotPublic ||
						!type.IsVisible ||
						type.IsEnum ||
						type.IsInterface ||
						//type.IsCOMObject ||
						type.IsGenericType ||
						type.Name.StartsWith("<", StringComparison.Ordinal) ||
						type.IsCastableTo(typeof(Delegate)) ||
						type.IsDefinedAttribute(typeof(ObsoleteAttribute)) ||
						type.IsDefinedAttribute(typeof(System.ComponentModel.EditorBrowsableAttribute)) ||
						excludedTypes.Contains(type.FullName))
						continue;
					if(excludedNS.Contains(ns)) {
						continue;
					}
					List<Type> types;
					if(!typeMaps.TryGetValue(ns, out types)) {
						types = new List<Type>();
						typeMaps[ns] = types;
					}
					types.Add(type);
					//typeCount++;
				}
			}
			//this.typeCount = typeCount;
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
				} else if(y.Key == "global") {
					return 1;
				}
				//if(x.Key.StartsWith("Unity")) {
				//	if(y.Key.StartsWith("Unity")) {
				//		return string.Compare(x.Key, y.Key, StringComparison.Ordinal);
				//	}
				//	return -1;
				//} else if(y.Key.StartsWith("Unity")) {
				//	return 1;
				//}
				return string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
			});
		}
		#endregion

		public void SearchChanged() {
			Reload();
		}

		public override void OnGUI(Rect rect) {
			base.OnGUI(rect);
			if(Event.current.type == EventType.MouseEnterWindow || Event.current.type == EventType.MouseLeaveWindow) {
				if(tooltipWindow != null) {
					tooltipWindow.Close();
				}
			}
			if(Event.current.type == EventType.Repaint) {
				if(hoverItem != null && lastHoverItem != hoverItem) {
					List<GUIContent> contents = null;
					if(hoverItem is MemberTreeView) {
						var item = hoverItem as MemberTreeView;
						contents = ItemSelector.Utility.GetTooltipContents(item.member);
					}
					if(contents != null && contents.Count > 0) {
						var position = window.position;
						if(position.x + position.width + 300 <= Screen.currentResolution.width) {
							tooltipWindow = TooltipWindow.Show(new Vector2(position.x + position.width, position.y), contents, 300, 600);
						} else {
							tooltipWindow = TooltipWindow.Show(new Vector2(position.x - 300, position.y), contents, 300, 600);
						}
					} else if(tooltipWindow != null) {
						tooltipWindow.Close();
					}
					lastHoverItem = hoverItem;
				}
			}
		}

		#region Utility
		public static string GetMemberName(MemberInfo member) {
			switch(member.MemberType) {
				case MemberTypes.Constructor:
					return member.DeclaringType.PrettyName();
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					return (member as Type).PrettyName();
			}
			return member.Name;
		}

		public static string GetPrettyMemberName(MemberInfo member) {
			switch(member.MemberType) {
				case MemberTypes.Method:
					return EditorReflectionUtility.GetPrettyMethodName(member as MethodInfo, false);
				case MemberTypes.Constructor:
					return EditorReflectionUtility.GetPrettyConstructorName(member as ConstructorInfo);
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					return (member as Type).PrettyName();
			}
			return member.Name;
		}

		public static string GetRichMemberName(MemberInfo member) {
			Type type = member as Type;
			if(type != null) {
				if(type.IsEnum) {
					return uNodeUtility.WrapTextWithColor(type.PrettyName(), uNodePreference.GetPreference().itemEnumColor, false);
				}
				else if(type.IsInterface) {
					return uNodeUtility.WrapTextWithColor(type.PrettyName(), uNodePreference.GetPreference().itemInterfaceColor, false);
				}
				return uNodeUtility.WrapTextWithColor(type.PrettyName(), uNodePreference.GetPreference().itemTypeColor, false);
			}
			switch(member.MemberType) {
				case MemberTypes.Method:
					return GetRichMethodName(member as MethodInfo, false);
				case MemberTypes.Constructor:
					return GetRichConstructorNames(member as ConstructorInfo);
			}
			return member.Name;
		}

		public static string GetRichConstructorNames(ConstructorInfo ctor) {
			ParameterInfo[] info = ctor.GetParameters();
			string mConstructur = "(";
			for(int i = 0; i < info.Length; i++) {
				var type = info[i].ParameterType;
				if(type.IsEnum){
					mConstructur += uNodeUtility.WrapTextWithColor(type.PrettyName(false, info[i]).Split('.').Last(), uNodePreference.GetPreference().itemEnumColor, false);
				} else if(type.IsInterface) {
					mConstructur += uNodeUtility.WrapTextWithColor(type.PrettyName(false, info[i]).Split('.').Last(), uNodePreference.GetPreference().itemInterfaceColor, false);
				} else {
					mConstructur += uNodeUtility.WrapTextWithColor(type.PrettyName(false, info[i]).Split('.').Last(), uNodePreference.GetPreference().itemTypeColor, false);
				}
				mConstructur += " " + info[i].Name;
				if(i + 1 < info.Length) {
					mConstructur += ", ";
				}
			}
			mConstructur += ")";
			return uNodeUtility.WrapTextWithColor("new ", uNodePreference.GetPreference().itemKeywordColor, false) + GetRichMemberName(ctor.DeclaringType) + mConstructur;
		}

		public static string GetRichMethodName(MethodInfo method, bool includeReturnType = true) {
			ParameterInfo[] info = method.GetParameters();
			string mConstructur = null;
			if(method.IsGenericMethod) {
				foreach(Type arg in method.GetGenericArguments()) {
					if(string.IsNullOrEmpty(mConstructur)) {
						mConstructur += "<" + arg.ToString();
						continue;
					}
					mConstructur += "," + arg.ToString();
				}
				mConstructur += ">";
			}
			mConstructur += "(";
			for(int i = 0; i < info.Length; i++) {
				var type = info[i].ParameterType;
				if(type.IsEnum){
					mConstructur += uNodeUtility.WrapTextWithColor(type.PrettyName(false, info[i]).Split('.').Last(), uNodePreference.GetPreference().itemEnumColor, false);
				} else if(type.IsInterface) {
					mConstructur += uNodeUtility.WrapTextWithColor(type.PrettyName(false, info[i]).Split('.').Last(), uNodePreference.GetPreference().itemInterfaceColor, false);
				} else {
					mConstructur += uNodeUtility.WrapTextWithColor(type.PrettyName(false, info[i]).Split('.').Last(), uNodePreference.GetPreference().itemTypeColor, false);
				}
				mConstructur += " " + info[i].Name;
				if(i + 1 < info.Length) {
					mConstructur += ", ";
				}
			}
			mConstructur += ")";
			//string name = method.Name;
			//switch(name) {
			//	case "op_Addition":
			//		name = "Add";
			//		break;
			//	case "op_Subtraction":
			//		name = "Subtract";
			//		break;
			//	case "op_Multiply":
			//		name = "Multiply";
			//		break;
			//	case "op_Division":
			//		name = "Divide";
			//		break;
			//}
			if(includeReturnType) {
				return GetRichMemberName(method.ReturnType) + " " + method.Name + mConstructur;
			} else {
				return method.Name + mConstructur;
			}
		}
		#endregion

		protected override TreeViewItem BuildRoot() {
			return new TreeViewItem { id = 0, depth = -1 };
		}

		protected override IList<TreeViewItem> BuildRows(TreeViewItem root) {
			var rows = GetRows() ?? new List<TreeViewItem>();
			rows.Clear();
			bool isSearching = !string.IsNullOrEmpty(searchString);
			{//Node
				var item = new TreeViewItem(uNodeEditorUtility.GetUIDFromString("[NODES]"), -1, "Nodes");
				if(isSearching || IsExpanded(item.id)) {
					//Init menu
					var menus = NodeEditorUtility.FindNodeMenu();
					var createNodeMenus = NodeEditorUtility.FindCreateNodeCommands();
					//Init data
					List<NodeTreeData> datas = new List<NodeTreeData>();
					foreach(var m in menus) {
						datas.Add(new NodeTreeData() {
							name = m.name,
							menu = m,
							category = m.category,
						});
					}
					foreach(var m in createNodeMenus) {
						datas.Add(new NodeTreeData() {
							name = m.name,
							command = m,
							category = m.category,
						});
					}
					//Sorting
					datas.Sort((x, y) => {
						int index = string.Compare(x.category, y.category, StringComparison.OrdinalIgnoreCase);
						if(index == 0) {
							return string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase);
						}
						return index;
					});
					//Finalize
					AddNodes(datas, item, rows);
				} else {
					item.children = CreateChildListForCollapsedParent();
				}
				root.AddChild(item);
				rows.Insert(0, item);
			}
			foreach(var pair in typeList) {
				int prevCount = rows.Count;
				var item = new NamespaceTreeView(pair.Key, uNodeEditorUtility.GetUIDFromString("[NS]" + pair.Key), -1);
				namespaceTrees[pair.Key] = item;
				if(pair.Value.Count > 0) {
					if(isSearching || IsExpanded(item.id)) {
						AddChildrenType(pair.Value, item, rows);
					} else {
						item.children = CreateChildListForCollapsedParent();
					}
				}
				if(!isSearching || item.children != null && item.children.Count > 0 || IsMatchSearch(item, searchString)) {
					root.AddChild(item);
					rows.Insert(rows.Count - (rows.Count - prevCount), item);
				}
			}
			SetupDepthsFromParentsAndChildren(root);
			return rows;
		}

		protected override bool CanChangeExpandedState(TreeViewItem item) {
			if(!string.IsNullOrEmpty(searchString)) {
				return false;
			}
			return item.hasChildren;
		}

		Dictionary<Type, List<MemberInfo>> memberMaps = new Dictionary<Type, List<MemberInfo>>();
		List<MemberInfo> GetSortedMember(Type type) {
			List<MemberInfo> members;
			if(!memberMaps.TryGetValue(type, out members)) {
				members = type.GetMembers(flags).ToList();
				members.Sort((x, y) => {
					if(x is ConstructorInfo && y is ConstructorInfo) {
						return string.Compare((x as ConstructorInfo).GetParameters().Length.ToString(), (y as ConstructorInfo).GetParameters().Length.ToString(), StringComparison.OrdinalIgnoreCase);
					} else if(x is ConstructorInfo) {
						return -1;
					} else if(y is ConstructorInfo) {
						return 1;
					} else if(x is MethodInfo && y is MethodInfo) {
						int i = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
						if(i == 0) {
							return string.Compare((x as MethodInfo).GetParameters().Length.ToString(), (y as MethodInfo).GetParameters().Length.ToString(), StringComparison.OrdinalIgnoreCase);
						}
						return i;
					}
					return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
				});
				memberMaps[type] = members;
			}
			return members;
		}

		void AddNodes(List<NodeTreeData> nodes, TreeViewItem item, IList<TreeViewItem> rows) {
			bool isSearching = !string.IsNullOrEmpty(searchString);
			if(item.children == null)
				item.children = new List<TreeViewItem>();
			TreeViewItem lastCategoryTree = null;
			int prevCount = rows.Count;
			for(int i = 0; i < nodes.Count; ++i) {
				var node = nodes[i];
				if(lastCategoryTree == null || lastCategoryTree.displayName != node.category) {//Add by category
					if(lastCategoryTree != null) {
						if(!isSearching || lastCategoryTree.children != null && lastCategoryTree.children.Count > 0 || IsMatchSearch(lastCategoryTree, searchString)) {
							item.AddChild(lastCategoryTree);
							rows.Insert(rows.Count - (rows.Count - prevCount), lastCategoryTree);
							prevCount = rows.Count;
						}
					}
					var childItem = new TreeViewItem(uNodeEditorUtility.GetUIDFromString("[CATEG]" + node.category), -1, node.category);
					lastCategoryTree = childItem;
					if(!isSearching && !IsExpanded(childItem.id)) {
						childItem.children = CreateChildListForCollapsedParent();
					}
				}
				if(lastCategoryTree != null) {
					if(IsExpanded(lastCategoryTree.id) && !isSearching || isSearching && IsMatchSearch(node.name, searchString)) {
						var childItem = new NodeTreeView(node, uNodeEditorUtility.GetUIDFromString("[NODE]" + node.name), -1);
						if(node.menu != null) {
							childItem.icon = uNodeEditorUtility.GetTypeIcon(node.menu.hasFlowInput ? typeof(TypeIcons.FlowIcon) : typeof(TypeIcons.ExtensionIcon)) as Texture2D;
						} else if(node.command != null) {
							childItem.icon = uNodeEditorUtility.GetTypeIcon(node.command.icon ?? typeof(TypeIcons.ExtensionIcon)) as Texture2D;
						}
						lastCategoryTree.AddChild(childItem);
						rows.Add(childItem);
					}
				}
			}
			if(!isSearching || lastCategoryTree.children != null && lastCategoryTree.children.Count > 0 || IsMatchSearch(lastCategoryTree, searchString)) {
				item.AddChild(lastCategoryTree);
				rows.Insert(rows.Count - (rows.Count - prevCount), lastCategoryTree);
			}
		}

		void AddChildrenType(List<Type> types, TreeViewItem item, IList<TreeViewItem> rows) {
			bool isSearching = !string.IsNullOrEmpty(searchString);
			if(item.children == null)
				item.children = new List<TreeViewItem>();
			for(int i = 0; i < types.Count; ++i) {
				int prevCount = rows.Count;
				var type = types[i];
				var childItem = new TypeTreeView(type, uNodeEditorUtility.GetUIDFromString(type.FullName), -1);
				childItem.icon = childItem.GetIcon() as Texture2D;
				typeTrees[type] = childItem;
				var members = GetSortedMember(type);
				if(members.Count > 0) {
					if(isSearching) {
						if(searchString.Length > 2)
							AddChildrenMember(members, childItem, rows);
					} else if(IsExpanded(childItem.id)) {
						AddChildrenMember(members, childItem, rows);
					} else {
						childItem.children = CreateChildListForCollapsedParent();
					}
				}
				if(!isSearching || childItem.children != null && childItem.children.Count > 0 || IsMatchSearch(childItem, searchString)) {
					item.AddChild(childItem);
					rows.Insert(rows.Count - (rows.Count - prevCount), childItem);
				}
			}
		}

		void AddChildrenMember(List<MemberInfo> members, TreeViewItem item, IList<TreeViewItem> rows) {
			bool isSearching = !string.IsNullOrEmpty(searchString);
			if(item.children == null)
				item.children = new List<TreeViewItem>();
			for(int i = 0; i < members.Count; ++i) {
				var member = members[i];
				if(member.MemberType == MemberTypes.Method &&
					(member.Name.StartsWith("get_", StringComparison.Ordinal) ||
					member.Name.StartsWith("set_", StringComparison.Ordinal) ||
					member.Name.StartsWith("op_", StringComparison.Ordinal) ||
					member.Name.StartsWith("add_", StringComparison.Ordinal) ||
					member.Name.StartsWith("remove_", StringComparison.Ordinal))) {
					continue;
				}
				if(member.MemberType == MemberTypes.Constructor && member.DeclaringType.IsCastableTo(typeof(Component))) {
					continue;
				}
				//string name = member.DeclaringType.FullName + "+" + member.Name;
				//switch(member.MemberType) {
				//	case MemberTypes.Constructor:
				//	case MemberTypes.Method:
				//		name += "=" + (member as MethodBase).GetParameters().Length;
				//		break;
				//}
				//var hashed = MD5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(name));
				if(!isSearching || IsMatchSearch(member, searchString)) {
					var childItem = new MemberTreeView(member, /*BitConverter.ToInt32(hashed, 0)*/ member.GetHashCode(), -1);
					childItem.icon = childItem.GetIcon() as Texture2D;
					memberTrees[member] = childItem;
					item.AddChild(childItem);
					rows.Add(childItem);
				}
			}
		}


		private TreeViewItem hoverItem, lastHoverItem;
		protected override void RowGUI(RowGUIArgs args) {
			Event evt = Event.current;
			if(evt.type == EventType.Repaint) {
#if !UNITY_2019_3_OR_NEWER
				if(!args.selected) {
					GUIStyle style = (args.row % 2 != 0) ? uNodeGUIStyle.itemBackground2 : uNodeGUIStyle.itemBackground;
					style.Draw(args.rowRect, GUIContent.none, false, false, false, false);
				}
#endif
				if(args.item is MemberTreeView && !(args.item is TypeTreeView)) {
					MemberTreeView member = args.item as MemberTreeView;
					Rect labelRect = args.rowRect;
					labelRect.x += GetContentIndent(args.item) + 16;
					Texture icon = uNodeEditorUtility.GetTypeIcon(ReflectionUtils.GetMemberType(member.member));
					if(icon != null) {
						GUI.DrawTexture(new Rect(labelRect.x, labelRect.y, 16, 16), icon);
						labelRect.x += 16;
						labelRect.width -= 16;
					}
					GUIContent label = null;
					{
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
					if(label == null) {
						label = new GUIContent(args.label);
					}
					if(ReflectionUtils.GetMemberIsStatic(member.member)) {
						uNodeGUIStyle.itemStatic.Draw(labelRect, new GUIContent(label), false, false, false, false);
					} else {
						uNodeGUIStyle.itemNormal.Draw(labelRect, new GUIContent(label), false, false, false, false);
					}
					args.label = "";
				}
			}
			//if(args.rowRect.Contains(evt.mousePosition)) {
			//	hoverItem = args.item;
			//	if(hoverItem != lastHoverItem) {
			//		window.Repaint();
			//	}
			//}
			//extraSpaceBeforeIconAndLabel = 18f;

			//toggleRect.width = 16f;

			//// Ensure row is selected before using the toggle (usability)
			//if(evt.type == EventType.MouseDown && toggleRect.Contains(evt.mousePosition))
			//	SelectionClick(args.item, false);
			var labelStyle = ((GUIStyle)"TV Line");
			labelStyle.richText = true;
			base.RowGUI(args);
			labelStyle.richText = false;
		}

		protected bool IsMatchSearch(object obj, string search) {
			string name = obj.ToString();
			if(obj is TreeViewItem) {
				name = (obj as TreeViewItem).displayName;
			} else if(obj is MemberInfo) {
				MemberInfo member = obj as MemberInfo;
				switch(browserState.typeKind) {
					case BrowserState.TypeKind.Function:
						if(member.MemberType != MemberTypes.Method) {
							return false;
						}
						break;
					case BrowserState.TypeKind.Property:
						if(member.MemberType != MemberTypes.Property) {
							return false;
						}
						break;
					case BrowserState.TypeKind.Variable:
						if(member.MemberType != MemberTypes.Field) {
							return false;
						}
						break;
					case BrowserState.TypeKind.Type:
						return false;
				}
				name = member.Name;
			}
			switch(browserState.searchKind) {
				case SearchKind.Contains:
					return name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
				case SearchKind.Equals:
					return name.ToLower().Equals(search.ToLower(), StringComparison.OrdinalIgnoreCase);
				case SearchKind.Startwith:
					return name.ToLower().StartsWith(search.ToLower(), StringComparison.OrdinalIgnoreCase);
				case SearchKind.Endswith:
					return name.ToLower().EndsWith(search.ToLower(), StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		protected override bool CanMultiSelect(TreeViewItem item) {
			return false;
		}

		//protected override void SingleClickedItem(int id) {
		//	base.SingleClickedItem(id);
		//	var item = FindItem(id, rootItem);
		//	if(item != null) {
		//		hoverItem = item;
		//		if(hoverItem != lastHoverItem) {
		//			window.Repaint();
		//		}
		//	}
		//}

		protected override void SelectionChanged(IList<int> selectedIds) {
			base.SelectionChanged(selectedIds);
			if(selectedIds != null && selectedIds.Count > 0) {
				var item = FindItem(selectedIds[0], rootItem);
				if(item != null) {
					hoverItem = item;
					if(hoverItem != lastHoverItem) {
						window.Repaint();
					}
				}
			}
		}

		protected override void ContextClickedItem(int id) {
			base.ContextClickedItem(id);
			var item = FindItem(id, rootItem);
			if(item != null) {
				if(item is TypeTreeView) {
					window.Repaint();
					var tree = item as TypeTreeView;
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Add to Excluded Types"), false, () => {
						uNodePreference.AddExcludedType(tree.type);
						Init();
						Reload();
					});
					menu.ShowAsContext();
				} else if(item is NamespaceTreeView) {
					window.Repaint();
					var tree = item as NamespaceTreeView;
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Add to Excluded Namespace"), false, () => {
						uNodePreference.AddExcludedNamespace(tree.Namespace);
						Init();
						Reload();
					});
					menu.ShowAsContext();
				}
			}
		}

		protected override bool CanStartDrag(CanStartDragArgs args) {
			return args.draggedItemIDs.Count == 1 && (args.draggedItem is TypeTreeView || args.draggedItem is MemberTreeView || args.draggedItem is NodeTreeView);
		}

		protected override void SetupDragAndDrop(SetupDragAndDropArgs args) {
			var item = FindItem(args.draggedItemIDs[0], rootItem);
			if(item != null) {
				DragAndDrop.PrepareStartDrag();
				if(item is TypeTreeView) {
					DragAndDrop.SetGenericData("uNode", (item as TypeTreeView).type);
				} else if(item is MemberTreeView) {
					DragAndDrop.SetGenericData("uNode", (item as MemberTreeView).member);
				} else if(item is NodeTreeView) {
					DragAndDrop.SetGenericData("uNode", (item as NodeTreeView).data.menu as object ?? (item as NodeTreeView).data.command as object);
				} else {
					throw null;
				}
				DragAndDrop.objectReferences = new UnityObject[0];
				DragAndDrop.StartDrag("Drag Node");
			}
		}

		public void Frame(int id, bool frame, bool ping, bool animated) {
			//Frame
			var field = typeof(TreeView).GetField("m_TreeView", MemberData.flags);
			if(field != null) {
				var tv = field.GetValueOptimized(this);
				var frameM = field.FieldType.GetMethod(
					"Frame",
					new Type[] {
						typeof(int),
						typeof(bool),
						typeof(bool),
						typeof(bool),
					});
				if(frameM != null) {
					frameM.Invoke(tv, new object[] { id, frame, ping, animated });
				}
			}
		}

		public void RevealItem(MemberInfo member) {
			searchString = "";
			if(member is Type) {
				Type type = member as Type;
				bool flag = false;
				string ns = "";
				foreach(var tl in typeList) {
					foreach(var t in tl.Value) {
						if(t == type) {
							flag = true;
							ns = tl.Key;
							break;
						}
					}
				}
				if(flag) {
					NamespaceTreeView nsTree;
					if(namespaceTrees.TryGetValue(ns, out nsTree)) {
						SetExpanded(nsTree.id, true);
						Reload();
						TypeTreeView typeTree;
						if(typeTrees.TryGetValue(type, out typeTree)) {
							SetSelection(new int[] { typeTree.id });
							SetFocus();
							Frame(typeTree.id, true, false, true);
						}
					}
				}
			} else {
				Type type = member.DeclaringType;
				bool flag = false;
				string ns = "";
				foreach(var tl in typeList) {
					foreach(var t in tl.Value) {
						if(t == type) {
							flag = true;
							ns = tl.Key;
							break;
						}
					}
				}
				if(flag) {
					if(member.ReflectedType != type) {
						var members = type.GetMember(member.Name, member.MemberType, MemberData.flags);
						if(members.Length > 0) {
							if(members.Length == 1) {
								member = members[0];
							} else {
								bool flag2 = false;
								if(member is MethodBase) {
									MethodBase method = member as MethodBase;
									foreach(var m in members) {
										MethodBase mBase = m as MethodBase;
										if(mBase != null && mBase.GetParameters().Length == method.GetParameters().Length) {
											var p1 = mBase.GetParameters();
											var p2 = method.GetParameters();
											bool flag3 = true;
											for(int i = 0; i < p1.Length; i++) {
												if(p1[i].ParameterType != p2[i].ParameterType) {
													flag3 = false;
													break;
												}
											}
											if(flag3) {
												flag2 = true;
												member = m;
												break;
											}
										}
									}
								}
								if(!flag2) {
									member = members.FirstOrDefault();
								}
							}
						}
					}
					NamespaceTreeView nsTree;
					if(namespaceTrees.TryGetValue(ns, out nsTree)) {
						SetExpanded(nsTree.id, true);
						Reload();
						TypeTreeView typeTree;
						if(typeTrees.TryGetValue(type, out typeTree)) {
							SetExpanded(typeTree.id, true);
							Reload();
							MemberTreeView memberTree;
							if(memberTrees.TryGetValue(member, out memberTree)) {
								SetSelection(new int[] { memberTree.id });
								SetFocus();
								Frame(memberTree.id, true, false, true);
							} else {
								SetSelection(new int[] { typeTree.id });
								SetFocus();
								Frame(typeTree.id, true, false, true);
							}
						}
					}
				}
			}
		}
	}
}
