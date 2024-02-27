using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public class UGroupElement : UGraphElement, IGroup, IIcon {
		public Texture2D icon;

		public UGroupElement() {
			expanded = true;
		}

		public Type GetIcon() {
			if(icon == null)
				return typeof(TypeIcons.FolderIcon);
			return TypeIcons.FromTexture(icon);
		}
	}

	public abstract class UGraphElement : IEnumerable<UGraphElement>, IGraphElement, ISummary {
		#region Fields
		[SerializeField]
		private string _name;
		[SerializeField, HideInInspector]
		private int _id;
		[SerializeField]
		private string _comment;
		[SerializeField]
		private UGraphElement _parent;
		[SerializeField]
		protected List<UGraphElement> childs = new List<UGraphElement>();

		[HideInInspector]
		public bool expanded = true;

		/// <summary>
		/// True when the element is destroyed
		/// </summary>
		[field: SerializeField]
		protected bool isDestroyed { get; private set; }

		[NonSerialized]
		private bool m_isMarkedInvalid;
		/// <summary>
		/// True if the element is not destroyed or destroyed with safe mode.
		/// </summary>
		public bool IsValid => isDestroyed == false && m_isMarkedInvalid == false;
		#endregion

		#region Classes
		private class Enumerator : IEnumerator<UGraphElement> {
			private UGraphElement outer;

			private int currentIndex = -1;

			UGraphElement IEnumerator<UGraphElement>.Current => outer.GetChild(currentIndex);

			public object Current => outer.GetChild(currentIndex);

			internal Enumerator(UGraphElement outer) {
				this.outer = outer;
			}

			public bool MoveNext() {
				int childCount = outer.childCount;
				return ++currentIndex < childCount;
			}

			public void Reset() {
				currentIndex = -1;
			}

			public void Dispose() {
				Reset();
			}
		}
		#endregion

		#region Properties
		[NonSerialized]
		private Graph _graph;
		public Graph graph {
			get {
				if(object.ReferenceEquals(_graph, null)) {
					_graph = root as Graph ?? this as Graph;
				}
				return _graph;
			}
		}

		public IGraph graphContainer {
			get {
				if(!object.ReferenceEquals(graph, null)) {
					return graph.owner;
				}
				return null;
			}
		}

		public string name {
			get {
				return _name;
			}
			set {
				_name = value;
			}
		}

		/// <summary>
		/// The unique ID of graph element ( obtained from graph every change the parent, return 0 if no parent )
		/// </summary>
		public int id {
			get {
				return _id;
			}
		}

		public string comment {
			get {
				return _comment;
			}
			set {
				_comment = value;
			}
		}

		public UGraphElement parent {
			get {
				return _parent;
			}
			set {
				SetParent(value);
			}
		}

		public UGraphElement root {
			get {
				if(!object.ReferenceEquals(parent, null)) {
					var root = parent;
					while(true) {
						if(!object.ReferenceEquals(root.parent, null)) {
							root = root.parent;
						} else {
							return root;
						}
					}
				}
				return null;
			}
		}

		public bool isRoot => root == null;

		public int childCount => childs.Count;

		[NonSerialized]
		private RuntimeGraphID m_runtimeID;
		internal RuntimeGraphID runtimeID {
			get {
				if(m_runtimeID == default) {
					m_runtimeID = new RuntimeGraphID(graphContainer.GetHashCode(), id);
				}
				return m_runtimeID;
			}
			set {
				m_runtimeID = value;
			}
		}
		#endregion

		#region Utility
		public UGraphElement GetChild(int index) {
			return childs[index];
		}

		public void ForeachInChildrens(Action<UGraphElement> action, bool recursive = false) {
			if(recursive) {
				foreach(var child in childs) {
					action(child);
					child.ForeachInChildrens(action, recursive);
				}
			} else {
				childs.ForEach(action);
			}
		}

		public void ForeachInParents(Action<UGraphElement> action) {
			if(parent != null) {
				action(parent);
				parent.ForeachInParents(action);
			}
		}

		public IEnumerable<UGraphElement> GetObjectsInChildren(bool recursive = false, bool findInsideGroup = false) {
			if(recursive) {
				foreach(var child in childs) {
					yield return child;
					foreach(var cc in child.GetObjectsInChildren(recursive)) {
						yield return cc;
					}
				}
			}
			else {
				foreach(var child in childs) {
					yield return child;
					if(findInsideGroup && child is UGroupElement) {
						foreach(var cc in child.GetObjectsInChildren(recursive, findInsideGroup)) {
							yield return cc;
						}
					}
				}
			}
		}

		public IEnumerable<T> GetObjectsInChildren<T>(bool recursive = false, bool findInsideGroup = false) {
			if(recursive) {
				foreach(var child in childs) {
					if(child is T c) {
						yield return c;
					}
					foreach(var cc in child.GetObjectsInChildren<T>(recursive)) {
						yield return cc;
					}
				}
			} else {
				foreach(var child in childs) {
					if(child is T c) {
						yield return c;
					}
					if(findInsideGroup && child is UGroupElement) {
						foreach(var cc in child.GetObjectsInChildren<T>(recursive, findInsideGroup)) {
							yield return cc;
						}
					}
				}
			}
		}

		public IEnumerable<T> GetObjectsInChildren<T>(Predicate<T> predicate, bool recursive = false, bool findInsideGroup = false) {
			if(recursive) {
				foreach(var child in childs) {
					if(child is T c && predicate(c)) {
						yield return c;
					}
					foreach(var cc in child.GetObjectsInChildren<T>(predicate, recursive)) {
						yield return cc;
					}
				}
			} else {
				foreach(var child in childs) {
					if(child is T c && predicate(c)) {
						yield return c;
					}
					if(findInsideGroup && child is UGroupElement) {
						foreach(var cc in child.GetObjectsInChildren<T>(predicate, recursive, findInsideGroup)) {
							yield return cc;
						}
					}
				}
			}
		}

		public T GetObjectInChildren<T>(bool recursive = false, bool findInsideGroup = false) {
			if(recursive) {
				foreach(var child in childs) {
					if(child is T c) {
						return c;
					}
					var cc = child.GetObjectInChildren<T>(recursive);
					if(cc != null) {
						return cc;
					}
				}
			} else {
				foreach(var child in childs) {
					if(child is T c) {
						return c;
					}
					if(findInsideGroup && child is UGroupElement) {
						var cc = child.GetObjectInChildren<T>(recursive, findInsideGroup);
						if(cc != null) {
							return cc;
						}
					}
				}
			}
			return default;
		}

		public T GetObjectInChildren<T>(Predicate<T> predicate, bool recursive = false, bool findInsideGroup = false) {
			if(recursive) {
				foreach(var child in childs) {
					if(child is T c && predicate(c)) {
						return c;
					}
					var cc = child.GetObjectInChildren<T>(predicate, recursive);
					if(cc != null) {
						return cc;
					}
				}
			} else {
				foreach(var child in childs) {
					if(child is T c && predicate(c)) {
						return c;
					}
					if(findInsideGroup && child is UGroupElement) {
						var cc = child.GetObjectInChildren<T>(predicate, recursive, findInsideGroup);
						if(cc != null) {
							return cc;
						}
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get object of type T from this to parent root.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IEnumerable<T> GetObjectsInParent<T>() {
			var parent = this;
			while(parent != null) {
				if(parent is T p) {
					yield return p;
				}
				parent = parent.parent;
			}
		}

		/// <summary>
		/// Get object of type T from this to parent root.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetObjectInParent<T>() {
			var parent = this;
			while(parent != null) {
				if(parent is T p) {
					return p;
				}
				parent = parent.parent;
			}
			return default;
		}
		#endregion

		#region Functions
		public virtual bool CanChangeParent() {
			return true;
		}

		public T AddChild<T>(T child) where T : UGraphElement {
			if(!child.CanChangeParent())
				throw new Exception("Unable to change Add Child because the child is forbidden to Change it's parent");
			child.SetParent(this);
			return child;
		}

		public T InsertChild<T>(T child, int index) where T : UGraphElement {
			if(!child.CanChangeParent())
				throw new Exception("Unable to change Add Child because the child is forbidden to Change it's parent");
			if(index > parent.childs.Count) {
				throw new ArgumentOutOfRangeException(nameof(index));
			}
			child.SetParent(this);
			child.SetSiblingIndex(index);
			return child;
		}

		/// <summary>
		/// Set the parent of element
		/// </summary>
		/// <param name="parent"></param>
		public void SetParent(UGraphElement parent) {
			if(parent == null)
				throw new ArgumentNullException(nameof(parent));
			if(!CanChangeParent())
				throw new Exception("Unable to change parent because it is forbidden");
			if(isDestroyed)
				throw new Exception("The object was destroyed but you're trying to access it.");
			if(this.parent == parent) {
				//Do nothing if the parent are same for prevent additional calls
				return;
			}
			{
				//For prevent circles parenting and self parenting
				var p = parent;
				while(p != null) {
					if(p == this) {
						//Do nothing if the parent is inside of this childs.
						return;
						//throw new Exception("Unable to change parent to it's children");
					}
					p = p.parent;
				}
			}
			if(parent != null) {
				if(graphContainer != parent.graphContainer) {
					var graph = parent.graph;
					if(graph != null) {
						//Set the new ID when the graph is changed
						_id = graph.GetNewUniqueID();
						foreach(var child in GetObjectsInChildren(true, true)) {
							child._id = graph.GetNewUniqueID();
							child._graph = null;
						}
					}
				}
				if(!object.ReferenceEquals(this.parent, null)) {
					this.parent.RemoveChild(this);
				}
				parent.AddChild(this);
				_graph = null;
				parent.OnChildrenChanged();
			} else {
				if(!object.ReferenceEquals(this.parent, null)) {
					this.parent.RemoveChild(this);
				}
				//Set the id to zero if there's no parent.
				_id = 0;
				foreach(var child in GetObjectsInChildren(true, true)) {
					child._id = 0;
				}
			}
		}

		/// <summary>
		/// Destroy the element and it's childrens
		/// </summary>
		public void Destroy() {
			if(!isDestroyed) {
				OnDestroy();
				if(parent != null) {
					parent.RemoveChild(this);
				}
				isDestroyed = true;
			} else if(parent != null) {
				throw new Exception("The object was destoyed but it look like still alive.");
			}
		}

		/// <summary>
		/// Mark element to invalid.
		/// </summary>
		internal void MarkInvalid() {
			if(!m_isMarkedInvalid) {
				MarkInvalidChilds();
				m_isMarkedInvalid = true;
			}
		}

		private void MarkInvalidChilds() {
			for(int i = 0; i < childs.Count; i++) {
				childs[i].MarkInvalid();
			}
		}

		/// <summary>
		/// Callback when the object is being destroyed.
		/// </summary>
		protected virtual void OnDestroy() {
			while(childs.Count > 0) {
				childs[0].Destroy();
			}
		}

		/// <summary>
		/// Get the element slibing index
		/// </summary>
		/// <returns></returns>
		public int GetSiblingIndex() {
			if(parent != null) {
				return GetSiblingIndex(this);
			}
			return -1;
		}

		/// <summary>
		/// Set the element slibing index
		/// </summary>
		/// <param name="index"></param>
		public void SetSiblingIndex(int index) {
			if(parent != null) {
				if(index > parent.childs.Count) {
					throw new ArgumentOutOfRangeException(nameof(index));
				}
				if(isDestroyed)
					throw new Exception("The object was destroyed but you're trying to access it.");
				if(index == parent.childs.Count) {
					var slibing = parent.childs[index - 1];
					if(slibing != this) {
						PlaceInFront(slibing);
					}
				}
				else {
					var slibing = parent.childs[index];
					if(slibing != this) {
						if(slibing.GetSiblingIndex() > GetSiblingIndex()) {
							PlaceInFront(slibing);
						}
						else {
							parent.childs.Remove(this);
							parent.childs.Insert(slibing.GetSiblingIndex(), this);
							parent.OnChildrenChanged();
						}
					}
				}
			} else {
				throw new System.NullReferenceException("The parent object is null");
			}
		}

		/// <summary>
		/// Place this element in behind ( before ) the target `element`
		/// </summary>
		/// <param name="element"></param>
		public void PlaceBehind(UGraphElement element) {
			if(parent != null) {
				if(!parent.childs.Contains(element)) {
					throw new ArgumentException("The value parent must same with this parent.", nameof(element));
				}
				if(isDestroyed)
					throw new Exception("The object was destroyed but you're trying to access it.");
				var slibing = element;
				if(slibing != this) {
					parent.childs.Remove(this);
					parent.childs.Insert(slibing.GetSiblingIndex(), this);
					parent.OnChildrenChanged();
				}
			} else {
				throw new System.NullReferenceException("The parent object is null");
			}
		}

		/// <summary>
		/// Place this element in front ( after ) the target `element`
		/// </summary>
		/// <param name="element"></param>
		public void PlaceInFront(UGraphElement element) {
			if(parent != null) {
				if(!parent.childs.Contains(element)) {
					throw new ArgumentException("The value parent must same with this parent.", nameof(element));
				}
				if(isDestroyed)
					throw new Exception("The object was destroyed but you're trying to access it.");
				var slibing = element;
				if(slibing != this) {
					parent.childs.Remove(this);
					var index = slibing.GetSiblingIndex();
					if(index < parent.childCount) {
						parent.childs.Insert(index + 1, this);
					}  else {
						parent.childs.Add(this);
					}
					parent.OnChildrenChanged();
				}
			} else {
				throw new System.NullReferenceException("The parent object is null");
			}
		}

		public bool IsChildOf(UGraphElement parent) {
			if(parent != null) {
				return parent.childs.Contains(this);
			}
			return false;
		}

		private void AddChild(UGraphElement child) {
			if(!object.ReferenceEquals(child._parent, null)) {
				child._parent.RemoveChild(child);
			}
			child._parent = this;
			childs.Add(child);
			OnChildAdded(child);
		}

		private void RemoveChild(UGraphElement child) {
			child._parent = null;
			childs.Remove(child);
			OnChildRemoved(child);
			OnChildrenChanged();
		}

		protected int GetSiblingIndex(UGraphElement child) {
			for(int i = 0; i < parent.childs.Count; i++) {
				if(parent.childs[i] == child)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Callback when children is changed ( added, removed, re-ordered )
		/// </summary>
		protected virtual void OnChildrenChanged() {
			//onChildChanged?.Invoke();
			if(parent != null) {
				parent.OnChildrenChanged();
			}
		}

		protected virtual void OnChildAdded(UGraphElement element) {
			if(parent != null) {
				parent.OnChildAdded(element);
			}
		}

		protected virtual void OnChildRemoved(UGraphElement element) {
			if(parent != null) {
				parent.OnChildRemoved(element);
			}
		}

		IEnumerator<UGraphElement> IEnumerable<UGraphElement>.GetEnumerator() {
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new Enumerator(this);
		}

		public static implicit operator bool(UGraphElement exists) {
			return !CompareBaseObjects(exists, null);
		}

		public static bool operator ==(UGraphElement x, UGraphElement y) {
			return CompareBaseObjects(x, y);
		}

		public static bool operator !=(UGraphElement x, UGraphElement y) {
			return !CompareBaseObjects(x, y);
		}

		private static bool CompareBaseObjects(UGraphElement lhs, UGraphElement rhs) {
			if(object.ReferenceEquals(lhs, null) || lhs.isDestroyed) {
				return object.ReferenceEquals(rhs, null) || rhs.isDestroyed;
			}
			if(object.ReferenceEquals(rhs, null) || rhs.isDestroyed) {
				return object.ReferenceEquals(lhs, null) || lhs.isDestroyed;
			}
			return object.ReferenceEquals(lhs, rhs);
		}

		public override bool Equals(object obj) {
			return obj is UGraphElement element && element == this;
		}

		public override int GetHashCode() {
			return base.GetHashCode();
		}

		/// <summary>
		/// Initialize the element for runtime
		/// </summary>
		/// <param name="instance"></param>
		public virtual void OnRuntimeInitialize(GraphInstance instance) { }

		string ISummary.GetSummary() {
			return comment;
		}
		#endregion
	}
}