using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MaxyGames.UNode.Editors {
	[Serializable]
	public abstract class UTreeElement<TElement> : ISerializationCallbackReceiver where TElement : UTreeElement<TElement> {
		#region Fields
		protected TElement _parent;
		[SerializeReference]
		protected List<TElement> childs = new List<TElement>();
		#endregion

		#region Properties
		public TElement parent {
			get {
				return _parent;
			}
			set {
				SetParent(value);
			}
		}

		public TElement root {
			get {
				if(!object.ReferenceEquals(parent, null)) {
					var root = parent;
					while(true) {
						if(!object.ReferenceEquals(root.parent, null)) {
							root = root.parent;
						}
						else {
							return root;
						}
					}
				}
				return null;
			}
		}

		public bool isRoot => root == null;

		public int childCount => childs.Count;

		public IEnumerable<TElement> children => GetObjectsInChildren(false, false);
		#endregion

		#region Utility
		public TElement GetChild(int index) {
			return childs[index];
		}

		public void ForeachInChildrens(Action<TElement> action, bool recursive = false) {
			if(recursive) {
				foreach(var child in childs) {
					action(child);
					child.ForeachInChildrens(action, recursive);
				}
			}
			else {
				childs.ForEach(action);
			}
		}

		public void ForeachInParents(Action<TElement> action) {
			if(parent != null) {
				action(parent);
				parent.ForeachInParents(action);
			}
		}

		public IEnumerable<TElement> GetObjectsInChildren(bool recursive = false, bool findInsideGroup = false) {
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
					if(findInsideGroup && child is IGroup) {
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
			}
			else {
				foreach(var child in childs) {
					if(child is T c) {
						yield return c;
					}
					if(findInsideGroup && child is IGroup) {
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
			}
			else {
				foreach(var child in childs) {
					if(child is T c && predicate(c)) {
						yield return c;
					}
					if(findInsideGroup && child is IGroup) {
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
			}
			else {
				foreach(var child in childs) {
					if(child is T c) {
						return c;
					}
					if(findInsideGroup && child is IGroup) {
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
			}
			else {
				foreach(var child in childs) {
					if(child is T c && predicate(c)) {
						return c;
					}
					if(findInsideGroup && child is IGroup) {
						var cc = child.GetObjectInChildren<T>(predicate, recursive, findInsideGroup);
						if(cc != null) {
							return cc;
						}
					}
				}
			}
			return default;
		}
		#endregion

		#region Functions
		public virtual bool CanChangeParent() {
			return true;
		}

		public T AddChild<T>(T child) where T : TElement {
			if(!child.CanChangeParent())
				throw new Exception("Unable to change Add Child because the child is forbidden to Change it's parent");
			child.SetParent(this as TElement);
			return child;
		}

		public T InsertChild<T>(T child, int index) where T : TElement {
			if(!child.CanChangeParent())
				throw new Exception("Unable to change Add Child because the child is forbidden to Change it's parent");
			if(index > parent.childs.Count) {
				throw new ArgumentOutOfRangeException(nameof(index));
			}
			child.SetParent(this as TElement);
			child.SetSiblingIndex(index);
			return child;
		}

		/// <summary>
		/// Set the parent of element
		/// </summary>
		/// <param name="parent"></param>
		public virtual void SetParent(TElement parent) {
			if(parent == null)
				throw new ArgumentNullException(nameof(parent));
			if(!CanChangeParent())
				throw new Exception("Unable to change parent because it is forbidden");
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
				if(!object.ReferenceEquals(this.parent, null)) {
					this.parent.RemoveChild(this as TElement);
				}
				parent.AddChild(this as TElement);
				parent.OnChildrenChanged();
			}
			else {
				if(!object.ReferenceEquals(this.parent, null)) {
					this.parent.RemoveChild(this as TElement);
				}
			}
		}

		/// <summary>
		/// Get the element slibing index
		/// </summary>
		/// <returns></returns>
		public int GetSiblingIndex() {
			if(parent != null) {
				return GetSiblingIndex(this as TElement);
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
							parent.childs.Remove(this as TElement);
							parent.childs.Insert(slibing.GetSiblingIndex(), this as TElement);
							parent.OnChildrenChanged();
						}
					}
				}
			}
			else {
				throw new System.NullReferenceException("The parent object is null");
			}
		}

		/// <summary>
		/// Place this element in behind ( before ) the target `element`
		/// </summary>
		/// <param name="element"></param>
		public void PlaceBehind(TElement element) {
			if(parent != null) {
				if(!parent.childs.Contains(element)) {
					throw new ArgumentException("The value parent must same with this parent.", nameof(element));
				}
				var slibing = element;
				if(slibing != this) {
					parent.childs.Remove(this as TElement);
					parent.childs.Insert(slibing.GetSiblingIndex(), this as TElement);
					parent.OnChildrenChanged();
				}
			}
			else {
				throw new System.NullReferenceException("The parent object is null");
			}
		}

		/// <summary>
		/// Place this element in front ( after ) the target `element`
		/// </summary>
		/// <param name="element"></param>
		public void PlaceInFront(TElement element) {
			if(parent != null) {
				if(!parent.childs.Contains(element)) {
					throw new ArgumentException("The value parent must same with this parent.", nameof(element));
				}
				var slibing = element;
				if(slibing != this) {
					parent.childs.Remove(this as TElement);
					var index = slibing.GetSiblingIndex();
					if(index < parent.childCount) {
						parent.childs.Insert(index + 1, this as TElement);
					}
					else {
						parent.childs.Add(this as TElement);
					}
					parent.OnChildrenChanged();
				}
			}
			else {
				throw new System.NullReferenceException("The parent object is null");
			}
		}

		public bool IsChildOf(TElement parent) {
			if(parent != null) {
				return parent.childs.Contains(this);
			}
			return false;
		}

		private void AddChild(TElement child) {
			if(!object.ReferenceEquals(child._parent, null)) {
				child._parent.RemoveChild(child);
			}
			child._parent = this as TElement;
			childs.Add(child);
			OnChildAdded(child);
		}

		private void RemoveChild(TElement child) {
			child._parent = null;
			childs.Remove(child);
			OnChildRemoved(child);
			OnChildrenChanged();
		}

		public virtual void RemoveFromHierarchy() {
			if(!object.ReferenceEquals(this.parent, null)) {
				this.parent.RemoveChild(this as TElement);
			}
		}

		protected int GetSiblingIndex(TElement child) {
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
			if(parent != null) {
				parent.OnChildrenChanged();
			}
		}

		protected virtual void OnChildAdded(TElement element) {
			if(parent != null) {
				parent.OnChildAdded(element);
			}
		}

		protected virtual void OnChildRemoved(TElement element) {
			if(parent != null) {
				parent.OnChildRemoved(element);
			}
		}
		#endregion

		void ISerializationCallbackReceiver.OnBeforeSerialize() {

		}

		void ISerializationCallbackReceiver.OnAfterDeserialize() {
			if(childs != null) {
				for(int i = 0; i < childs.Count; i++) {
					if(childs[i] == null) {
						childs.RemoveAt(i);
						i--;
						continue;
					}
					childs[i]._parent = this as TElement;
				}
			}
		}
	}
}
