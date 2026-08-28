using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// The kind of node browser item.
	/// </summary>
	public enum NodeBrowserEntryKind {
		Node = 0,
		Type = 1,
		Member = 2,
		Folder = 3,
		Namespace = 4,
	}

	/// <summary>
	/// How a type item's generated member list behaves.
	/// The stored name list flips meaning with the mode:
	/// IncludeAll  names are hidden members; ExcludeAll  names are visible members.
	/// </summary>
	public enum TypeMemberMode {
		/// <summary>All generated members are shown unless excluded by name.</summary>
		IncludeAll = 0,
		/// <summary>No generated members are shown unless included by name.</summary>
		ExcludeAll = 1,
	}

	/// <summary>
	/// ScriptableSingleton container for all node browser data.
	/// Persisted automatically by Unity inside Library/ScriptableSingletons
	/// (outside the Assets folder) using Unity's native serializer so that
	/// SerializedType references round-trip correctly. Members are not
	/// persisted  they are generated from their type items via reflection.
	/// The hierarchy is stored nested: each category holds its root entries,
	/// and folders/namespaces embed their children.
	/// </summary>
	[FilePath(uNodePreference.preferenceDirectory + "/NodeBrowser.asset", FilePathAttribute.Location.ProjectFolder)]
	public class NodeBrowserDataAsset : ScriptableSingleton<NodeBrowserDataAsset> {
		[Serializable]
		public class BrowserCategory {
			public string id;
			public string name;
			[SerializeReference]
			public List<BrowserEntry> roots = new List<BrowserEntry>();
		}

		[Serializable]
		public class BrowserEntry {
			public string id;
			public NodeBrowserEntryKind kind;
			public string displayName;

			/// <summary>The targeted type. Used for Type kind.</summary>
			public SerializedType targetType;

			/// <summary>
			/// Member/type names list. Meaning flips with memberMode:
			/// hidden members/types in IncludeAll, visible ones in ExcludeAll.
			/// </summary>
			public List<string> excludedMembers = new List<string>();

			/// <summary>How the generated list behaves (see TypeMemberMode).</summary>
			public TypeMemberMode memberMode = TypeMemberMode.IncludeAll;

			/// <summary>
			/// True for rows generated at runtime (namespace types / type members /
			/// deep search results). Never serialized.
			/// </summary>
			[NonSerialized]
			public bool isVirtual;

			/// <summary>Embedded children. Only meaningful for Folder/Namespace entries.</summary>
			[SerializeReference]
			public List<BrowserEntry> children = new List<BrowserEntry>();

			[NonSerialized]
			private (Type, NodeMenu) m_nodeMenu;
			public NodeMenu nodeMenu {
				get {
					if(kind == NodeBrowserEntryKind.Node) {
						Type type = targetType;
						if(m_nodeMenu.Item1 == type) {
							return m_nodeMenu.Item2;
						}
						if(type != null && type.IsDefined(typeof(NodeMenu), true)) {
							m_nodeMenu = (type, type.GetCustomAttribute<NodeMenu>(true));
							return m_nodeMenu.Item2;
						}
					}
					return null;
				}
			}

			/// <summary>
			/// Runtime-only reflected member for virtual Member entries.
			/// Never serialized  open generics stay intact.
			/// </summary>
			[System.NonSerialized]
			public MemberInfo rawMember;

			/// <summary>Runtime back-reference to the containing entry/category root.</summary>
			[System.NonSerialized]
			public BrowserEntry parentEntry;

			/// <summary>Runtime back-reference for virtual rows: the favorited owner.</summary>
			[System.NonSerialized]
			public BrowserEntry ownerEntry;

			[NonSerialized]
			private List<BrowserEntry> m_cachedChilds;

			public List<BrowserEntry> GetConstructedChildrens() {
				if(kind == NodeBrowserEntryKind.Namespace) {
					if(m_cachedChilds == null) {
						return m_cachedChilds = NodeBrowserManager.GetVirtualNamespaceChildren(this);
					}
					return m_cachedChilds;
				}
				else if(kind == NodeBrowserEntryKind.Folder) {
					return children;
				}
				else {
					if(m_cachedChilds == null) {
						return m_cachedChilds = NodeBrowserManager.GenerateMembersForTypeEntry(this);
					}
					return m_cachedChilds;
				}
			}

			/// <summary>
			/// The resolved System.Type of this entry (declaring type for members).
			/// Returns null for Folder/Namespace and virtual entries.
			/// </summary>
			public Type resolvedType {
				get {
					if(isVirtual) return null;
					if(targetType != null && targetType.isAssigned)
						return targetType.type;
					if(rawMember != null)
						return rawMember is Type rt ? rt : rawMember.DeclaringType;
					return null;
				}
			}

			/// <summary>Full name of the group type this entry belongs to.</summary>
			public string typeName {
				get {
					var t = resolvedType;
					return t != null ? t.FullName : string.Empty;
				}
			}

			/// <summary>The reflected member name for Member kind entries.</summary>
			public string memberName {
				get {
					if(kind != NodeBrowserEntryKind.Member)
						return null;
					return rawMember?.Name;
				}
			}

			/// <summary>True when this entry can contain serialized child entries.</summary>
			public bool CanHaveChilds =>
				kind == NodeBrowserEntryKind.Folder || kind == NodeBrowserEntryKind.Namespace;

			/// <summary>True when this entry may receive drops from other items.</summary>
			public bool CanBeDropTarget => kind == NodeBrowserEntryKind.Folder;
		}

		public List<BrowserCategory> categories = new List<BrowserCategory>();

		/// <summary>Entry ids whose tree row is currently expanded (persisted).</summary>
		public List<string> expandedEntries = new List<string>();

		public void Save() {
			EditorUtility.SetDirty(this);
			base.Save(true);
		}
	}

	/// <summary>
	/// Static facade over the FavoritesDataAsset singleton providing tree CRUD,
	/// persistence and change notifications.
	/// </summary>
	public static class NodeBrowserManager {
		/// <summary>Raised whenever the favorites data changed.</summary>
		public static event Action onChanged;

		public static NodeBrowserDataAsset asset => NodeBrowserDataAsset.instance;

		static bool s_Initialized;

		static void EnsureInitialized() {
			if(s_Initialized)
				return;
			s_Initialized = true;
			foreach(var cat in asset.categories)
				RefreshParents(cat);
		}

		static void RaiseChanged() {
			onChanged?.Invoke();
		}

		/// <summary>Raise the onChanged event (for external mutations).</summary>
		public static void NotifyChanged() {
			RaiseChanged();
		}

		public static void Save() {
			EnsureInitialized();
			asset.Save();
		}

		#region Categories
		/// <summary>Id of the built-in, never-saved Browser category.</summary>
		public const string BrowserCategoryID = "[browser]";

		static NodeBrowserDataAsset.BrowserCategory s_BrowserCategory;

		/// <summary>True when the category is the built-in (non-saved) browser.</summary>
		public static bool IsBrowserCategory(NodeBrowserDataAsset.BrowserCategory category) {
			return category != null && category.id == BrowserCategoryID;
		}

		/// <summary>
		/// Built-in read-only category whose roots are the namespaces from the
		/// preference browser list. Never persisted; rebuilt per session and
		/// whenever the reflection cache is cleared.
		/// </summary>
		public static NodeBrowserDataAsset.BrowserCategory GetBrowserCategory() {
			if(s_BrowserCategory != null)
				return s_BrowserCategory;
			var cat = new NodeBrowserDataAsset.BrowserCategory {
				id = BrowserCategoryID,
				name = "Browser",
			};
			foreach(var ns in uNodePreference.GetBrowserNamespaceList()) {
				cat.roots.Add(new NodeBrowserDataAsset.BrowserEntry {
					id = "[bns]:" + ns,
					kind = NodeBrowserEntryKind.Namespace,
					isVirtual = true,
					displayName = ns,
				});
			}
			s_BrowserCategory = cat;
			return cat;
		}

		public static List<NodeBrowserDataAsset.BrowserCategory> GetCategories() {
			EnsureInitialized();
			var list = new List<NodeBrowserDataAsset.BrowserCategory>(asset.categories);
			list.Add(GetBrowserCategory()); // built-in, never persisted
			return list;
		}

		public static NodeBrowserDataAsset.BrowserCategory GetDefaultCategory() {
			EnsureInitialized();
			return GetBrowserCategory();
		}

		public static NodeBrowserDataAsset.BrowserCategory GetOrCreateCategory(string name) {
			EnsureInitialized();
			var cat = asset.categories.FirstOrDefault(c => c.name == name);
			if(cat == null) {
				cat = new NodeBrowserDataAsset.BrowserCategory {
					id = Guid.NewGuid().ToString(),
					name = name,
				};
				asset.categories.Add(cat);
				Save();
				RaiseChanged();
			}
			return cat;
		}

		public static void RemoveCategory(NodeBrowserDataAsset.BrowserCategory category) {
			if(category == null) return;
			foreach(var id in Flatten(category).Select(e => e.id))
				SetExpandedInternal(id, false);
			asset.categories.Remove(category);
			Save();
			RaiseChanged();
		}
		#endregion

		#region Tree Access
		/// <summary>Depth-first iteration over every persisted entry of a category.</summary>
		public static IEnumerable<NodeBrowserDataAsset.BrowserEntry> Flatten(NodeBrowserDataAsset.BrowserCategory category) {
			if(category == null)
				yield break;
			foreach(var root in category.roots) {
				foreach(var e in Flatten(root))
					yield return e;
			}
		}

		static IEnumerable<NodeBrowserDataAsset.BrowserEntry> Flatten(NodeBrowserDataAsset.BrowserEntry entry) {
			if(entry == null)
				yield break;
			yield return entry;
			if(entry.CanHaveChilds) {
				foreach(var child in entry.children) {
					foreach(var e in Flatten(child))
						yield return e;
				}
			}
		}

		/// <summary>(Re)assigns runtime parent references for a whole category.</summary>
		public static void RefreshParents(NodeBrowserDataAsset.BrowserCategory category) {
			if(category == null) return;
			foreach(var root in category.roots)
				RefreshParents(root, null);
		}

		static void RefreshParents(NodeBrowserDataAsset.BrowserEntry entry, NodeBrowserDataAsset.BrowserEntry parent) {
			if(entry == null) return;
			entry.parentEntry = parent;
			if(entry.CanHaveChilds) {
				foreach(var c in entry.children)
					RefreshParents(c, entry);
			}
		}

		/// <summary>True when ancestor is entry itself or any of its parents.</summary>
		public static bool IsDescendantOf(NodeBrowserDataAsset.BrowserEntry entry, NodeBrowserDataAsset.BrowserEntry ancestor) {
			var cur = entry;
			while(cur != null) {
				if(ReferenceEquals(cur, ancestor))
					return true;
				cur = cur.parentEntry;
			}
			return false;
		}
		#endregion

		#region CRUD
		/// <summary>
		/// Insert an entry under parent (null = category root). Assigns an id,
		/// sets the runtime parent link, persists, and notifies.
		/// </summary>
		public static NodeBrowserDataAsset.BrowserEntry AddEntry(
			NodeBrowserDataAsset.BrowserCategory category,
			NodeBrowserDataAsset.BrowserEntry parent,
			NodeBrowserDataAsset.BrowserEntry entry) {
			EnsureInitialized();
			if(category == null || entry == null)
				return entry;
			if(string.IsNullOrEmpty(entry.id))
				entry.id = Guid.NewGuid().ToString();
			var container = parent != null ? parent.children : category.roots;
			container.Add(entry);
			entry.parentEntry = parent;
			Save();
			RaiseChanged();
			return entry;
		}

		/// <summary>
		/// Remove an entry from its container. Children are removed with it
		/// automatically since they live inside the entry.
		/// </summary>
		public static void Remove(NodeBrowserDataAsset.BrowserEntry entry) {
			EnsureInitialized();
			if(entry == null) return;
			if(!Detach(entry)) return;
			SetExpandedInternal(entry.id, false);
			Save();
			RaiseChanged();
		}

		/// <summary>Detaches an entry from its container; returns false when not found.</summary>
		static bool Detach(NodeBrowserDataAsset.BrowserEntry entry) {
			if(entry.parentEntry != null)
				return entry.parentEntry.children.Remove(entry);
			foreach(var cat in asset.categories) {
				if(cat.roots.Remove(entry)) {
					RefreshParents(cat);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Move an entry under newParent (null = category root) at the given
		/// sibling index (-1 = append). Validates: folder-only parent, same
		/// category implied by object graph, no self/descendant moves.
		/// </summary>
		public static bool Move(NodeBrowserDataAsset.BrowserEntry entry,
			NodeBrowserDataAsset.BrowserEntry newParent, int index, NodeBrowserDataAsset.BrowserCategory category) {
			EnsureInitialized();
			if(entry == null || category == null)
				return false;
			if(newParent != null && !newParent.CanBeDropTarget)
				return false;
			if(IsDescendantOf(newParent, entry)) // would create a cycle
				return false;
			if(ReferenceEquals(entry.parentEntry, newParent) &&
				(entry.parentEntry != null || category.roots.Contains(entry))) {
				// Same container  pure reorder below handles it; still validate bounds.
			}
			if(index < -1)
				index = -1;
			var sourceList = entry.parentEntry != null ? entry.parentEntry.children : category.roots;
			if(!sourceList.Contains(entry)) {
				// Stale runtime links  refresh and retry once.
				RefreshParents(category);
				sourceList = entry.parentEntry != null ? entry.parentEntry.children : category.roots;
				if(!sourceList.Contains(entry))
					return false;
			}
			var targetList = newParent != null ? newParent.children : category.roots;
			int insertAt = index < 0 || index >= targetList.Count ? targetList.Count : index;
			if(ReferenceEquals(sourceList, targetList)) {
				int oldIndex = sourceList.IndexOf(entry);
				if(oldIndex == insertAt || oldIndex == insertAt - 1)
					return true; // already in place
			}
			DetachSilent(entry);
			insertAt = Mathf.Clamp(insertAt, 0, targetList.Count);
			targetList.Insert(insertAt, entry);
			entry.parentEntry = newParent;
			Save();
			RaiseChanged();
			return true;
		}

		static void DetachSilent(NodeBrowserDataAsset.BrowserEntry entry) {
			if(entry.parentEntry != null)
				entry.parentEntry.children.Remove(entry);
			else {
				foreach(var cat in asset.categories)
					cat.roots.Remove(entry);
			}
		}

		/// <summary>Sort a container's direct children with the given comparison.</summary>
		public static void SortChildren(NodeBrowserDataAsset.BrowserEntry parent,
			NodeBrowserDataAsset.BrowserCategory category, Comparison<NodeBrowserDataAsset.BrowserEntry> comparison) {
			EnsureInitialized();
			var list = parent != null ? parent.children : category.roots;
			if(list == null || list.Count < 2)
				return;
			list.Sort(comparison);
			Save();
			RaiseChanged();
		}

		public static void Rename(NodeBrowserDataAsset.BrowserEntry entry, string newName) {
			if(entry == null || string.IsNullOrWhiteSpace(newName)) return;
			entry.displayName = newName.Trim();
			Save();
			RaiseChanged();
		}
		#endregion

		#region Expanded State
		/// <summary>True when the given entry's tree row is persisted as expanded.</summary>
		public static bool IsEntryExpanded(string entryID) {
			return !string.IsNullOrEmpty(entryID) && asset.expandedEntries.Contains(entryID);
		}

		public static void SetEntryExpanded(string entryID, bool expanded) {
			if(string.IsNullOrEmpty(entryID))
				return;
			bool changed;
			if(expanded) {
				changed = !asset.expandedEntries.Contains(entryID);
				if(changed)
					asset.expandedEntries.Add(entryID);
			}
			else {
				changed = asset.expandedEntries.Remove(entryID);
			}
			if(changed)
				Save();
		}

		static void SetExpandedInternal(string entryID, bool expanded) {
			if(string.IsNullOrEmpty(entryID))
				return;
			if(expanded)
				asset.expandedEntries.Add(entryID);
			else
				asset.expandedEntries.Remove(entryID);
		}
		#endregion

		#region Visibility Rules
		/// <summary>
		/// Mode-aware visibility of a reflected member under a type favorite.
		/// IncludeAll  visible unless listed; ExcludeAll  visible only when
		/// listed. A null owner is always visible.
		/// </summary>
		public static bool IsMemberVisibleIn(NodeBrowserDataAsset.BrowserEntry typeEntry, MemberInfo member) {
			if(typeEntry == null || member == null)
				return true;
			bool inList = typeEntry.excludedMembers != null && typeEntry.excludedMembers.Contains(member.Name);
			return typeEntry.memberMode == TypeMemberMode.ExcludeAll ? inList : !inList;
		}

		/// <summary>
		/// Mode-aware visibility of a type name under a namespace favorite.
		/// Mirrors IsMemberVisibleIn. A null owner is always visible.
		/// </summary>
		public static bool IsTypeNameVisibleIn(NodeBrowserDataAsset.BrowserEntry nsEntry, string typeName) {
			if(nsEntry == null || string.IsNullOrEmpty(typeName))
				return true;
			bool inList = nsEntry.excludedMembers != null && nsEntry.excludedMembers.Contains(typeName);
			return nsEntry.memberMode == TypeMemberMode.ExcludeAll ? inList : !inList;
		}

		/// <summary>True for compiler-generated property accessors (get_/set_).</summary>
		public static bool IsAccessorMethod(MemberInfo member) {
			return member is MethodInfo method &&
				(method.Name.StartsWith("get_", StringComparison.Ordinal) ||
				 method.Name.StartsWith("set_", StringComparison.Ordinal));
		}
		#endregion

		#region Reflection Cache
		static readonly object s_CacheLock = new object();
		static readonly Dictionary<string, Type[]> s_NsTypesCache = new Dictionary<string, Type[]>();
		static readonly Dictionary<Type, MemberInfo[]> s_MembersCache = new Dictionary<Type, MemberInfo[]>();

		/// <summary>
		/// Reflected types of a namespace. 
		/// </summary>
		public static Type[] GetNamespaceTypesRaw(string @namespace) {
			if(string.IsNullOrEmpty(@namespace))
				return Type.EmptyTypes;
			lock(s_CacheLock) {
				if(s_NsTypesCache.TryGetValue(@namespace, out var cached))
					return cached;
			}
			var list = new List<Type>();
			foreach(var asm in EditorReflectionUtility.GetAssemblies()) {
				Type[] types;
				try { types = asm.GetTypes(); }
				catch { continue; }
				foreach(var t in types) {
					if(t == null || t.IsNested || t.Namespace != @namespace) continue;
					if(t.IsSpecialName || t.Name.Contains('<') || t.Name.StartsWith("__")) continue;
					list.Add(t);
				}
			}
			var arr = list.ToArray();
			lock(s_CacheLock) {
				s_NsTypesCache[@namespace] = arr;
			}
			return arr;
		}

		/// <summary>Reflected members of a type (unfiltered, cached).</summary>
		public static MemberInfo[] GetMembersRaw(Type type) {
			if(type == null)
				return Array.Empty<MemberInfo>();
			lock(s_CacheLock) {
				if(s_MembersCache.TryGetValue(type, out var cached))
					return cached;
			}
			MemberInfo[] members;
			try {
				members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
			}
			catch {
				members = Array.Empty<MemberInfo>();
			}
			lock(s_CacheLock) {
				s_MembersCache[type] = members;
			}
			return members;
		}

		/// <summary>
		/// True when raw reflection results for this owner are already cached
		/// (its generated children can be built instantly).
		/// </summary>
		public static bool HasRawCache(NodeBrowserDataAsset.BrowserEntry owner) {
			if(owner == null) return false;
			lock(s_CacheLock) {
				if(owner.kind == NodeBrowserEntryKind.Namespace)
					return s_NsTypesCache.ContainsKey(owner.displayName);
				if(owner.kind == NodeBrowserEntryKind.Type) {
					Type t = null;
					try { t = owner.resolvedType; } catch { }
					return t != null && s_MembersCache.ContainsKey(t);
				}
			}
			return false;
		}

		/// <summary>
		/// Populates the raw reflection caches for this owner
		/// (namespace types or type members). Thread-safe.
		/// </summary>
		public static void WarmReflectionCache(NodeBrowserDataAsset.BrowserEntry owner) {
			if(owner == null) return;
			switch(owner.kind) {
				case NodeBrowserEntryKind.Namespace:
					GetNamespaceTypesRaw(owner.displayName);
					break;
				case NodeBrowserEntryKind.Type: {
					Type t = null;
					try { t = owner.resolvedType; } catch { }
					if(t != null)
						GetMembersRaw(t);
					break;
				}
			}
		}

		/// <summary>Drops all cached reflection results and the browser category
		/// (e.g. on domain reload) so both rebuild fresh.</summary>
		public static void ClearReflectionCache() {
			lock(s_CacheLock) {
				s_NsTypesCache.Clear();
				s_MembersCache.Clear();
			}
			s_BrowserCategory = null;
		}
		#endregion

		#region Virtual Generation
		/// <summary>
		/// Resolve the reflected MemberInfo of a member entry. Virtual entries
		/// carry the raw MemberInfo directly (open generics stay intact).
		/// </summary>
		public static MemberInfo GetEntryMember(NodeBrowserDataAsset.BrowserEntry entry) {
			if(entry == null || entry.kind != NodeBrowserEntryKind.Member)
				return null;
			return entry.rawMember;
		}

		/// <summary>Generates visible member entries for any type entry
		/// Returns an empty list on failure.</summary>
		public static List<NodeBrowserDataAsset.BrowserEntry> GenerateMembersForTypeEntry(
			NodeBrowserDataAsset.BrowserEntry entry) {
			var result = new List<NodeBrowserDataAsset.BrowserEntry>();
			Type t = null;
			try { t = entry.targetType?.type; } catch { }
			if(t == null || t.IsEnum)
				return result;
			string declName = t.FullName ?? t.Name;
			foreach(var m in NodeBrowserManager.GetMembersRaw(t)) {
				if(m is EventInfo) continue;
				if(m is ConstructorInfo ctor && ctor.GetParameters().Length > 6) continue;
				if(NodeBrowserManager.IsAccessorMethod(m)) continue;
				result.Add(new NodeBrowserDataAsset.BrowserEntry {
					id = "[member]:" + declName + "::" + m.Name + "::" + m.MetadataToken,
					kind = NodeBrowserEntryKind.Member,
					rawMember = m,
					isVirtual = true,
					displayName = m.Name,
					ownerEntry = entry,
					parentEntry = entry,
				});
			}
			result.Sort((a, b) => string.Compare(a.displayName, b.displayName, StringComparison.OrdinalIgnoreCase));
			return result;
		}

		/// <summary>
		/// Read-only: generate virtual type entries for the given namespace favorite.
		/// When ignoreVisibility is false the namespace's memberMode + excludedMembers
		/// list filter which types are returned. These are never persisted.
		/// </summary>
		public static List<NodeBrowserDataAsset.BrowserEntry> GetVirtualNamespaceChildren(
			NodeBrowserDataAsset.BrowserEntry nsEntry, bool ignoreVisibility = false) {
			var result = new List<NodeBrowserDataAsset.BrowserEntry>();
			if(nsEntry == null || string.IsNullOrEmpty(nsEntry.displayName))
				return result;
			foreach(var t in GetNamespaceTypesRaw(nsEntry.displayName)) {
				if(!ignoreVisibility && !IsTypeNameVisibleIn(nsEntry, t.Name))
					continue;
				result.Add(new NodeBrowserDataAsset.BrowserEntry {
					id = "[ns]:" + t.AssemblyQualifiedName,
					kind = NodeBrowserEntryKind.Type,
					targetType = new SerializedType(t),
					isVirtual = true,
					displayName = t.Name,
					ownerEntry = nsEntry,
					parentEntry = nsEntry,
				});
			}
			result.Sort((a, b) => string.Compare(a.targetType.type.FullName, b.targetType.type.FullName, StringComparison.OrdinalIgnoreCase));
			return result;
		}
		#endregion
	}
}

