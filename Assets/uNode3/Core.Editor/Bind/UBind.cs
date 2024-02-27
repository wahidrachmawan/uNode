using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using System.Reflection;

namespace MaxyGames.UNode.Editors {
	public abstract class UBind {
		private string _path;
		public string path {
			get {
				if(_path == null) {
					string result = subpath;
					var current = parent;
					while(current != null) {
						result = current.subpath + "." + result;
						current = current.parent;
					}
					_path = result;
				}
				return _path;
			}
		}
		protected string subpath { get; private set; }
		public UBind parent { get; protected set; }
		public int depth {
			get {
				var num = 0;
				var obj = this.parent;
				while(obj != null) {
					obj = obj.parent;
					num++;
				}
				return num;
			}
		}
		public GUIContent label { get; protected set; }
		public virtual bool isRoot => false;
		protected virtual bool isValid => true;
		public abstract Type type { get; }

		public Type valueType {
			get {
				return rawValue?.GetType() ?? this.type;
			}
		}

		public void RegisterUndo(string name = "") {
			var unityObject = root.value as UnityEngine.Object;
			if(unityObject != null) {
				uNodeEditorUtility.RegisterUndo(unityObject);
			}
		}

		//public void Update() {
		//	foreach(var pair in childrens) {
		//		if(pair.Value.isValid)
		//			pair.Value.Update();
		//	}
		//	if(!isRoot)
		//		value = rawValue;
		//}

		//public void ApplyModifiedValues() {
		//	foreach(var pair in childrens) {
		//		if(pair.Value.isValid)
		//			pair.Value.ApplyModifiedValues();
		//	}
		//	if(!isRoot)
		//		rawValue = value;
		//}

		public UBind root {
			get {
				var obj = this;
				while(obj.parent != null)
					obj = obj.parent;
				return obj;
			}
		}

		private bool obtainedValue;


		private object lastValue;
		public object value {
			get {
				var val = rawValue;
				if(!obtainedValue) {
					lastValue = val;
					obtainedValue = true;
				} else if(!object.Equals(val, lastValue)) {
					var previousValue = lastValue;
					lastValue = val;
					OnValueChange(previousValue, val);
				}
				return val;
			}
			set {
				var previousValue = rawValue;

				lastValue = rawValue = value;

				if(!object.Equals(value, previousValue)) {
					OnValueChange(previousValue, value);
				} else {
					parent?.OnChildrenChange(this);
				}
			}
		}

		protected virtual void OnValueChange(object previousValue, object newValue) {
			if(childrens != null) {
				foreach(var pair in childrens) {
					pair.Value.OnParentValueChange(previousValue, newValue);
				}
			}
			parent?.OnChildrenChange(this);
			if(_valueChanged != null) {
				_valueChanged(previousValue);
			}
		}

		protected virtual void OnParentValueChange(object previousValue, object newValue) { }

		protected virtual void OnChildrenChange(UBind child) {
			childValueChanged?.Invoke(child);
			parent?.OnChildrenChange(child);
		}

		public void Unbind() {
			if(isRoot)
				throw new Exception("Cannot unbind root.");
			UnbindChildren();
			parent?.childrens.Remove(subpath);
		}

		public void UnbindChildren() {
			while(childrens.Count > 0) {
				childrens.First().Value.Unbind();
			}
		}

		protected abstract object rawValue { get; set; }


		private event Action<object> _valueChanged;

		public event Action<object> valueChanged {
			add {
				lastValue = rawValue;
				obtainedValue = true;
				value(this.value);
				_valueChanged += value;
			}
			remove {
				_valueChanged -= value;
			}
		}

		public event Action<UBind> childValueChanged;

		protected UBind(string subpath, UBind parent) {
			if(isRoot) {
				if(parent != null)
					throw new InvalidOperationException();
			} else {
				if(parent == null)
					throw new InvalidOperationException();
			}
			this.subpath = subpath;
			this.parent = parent;
		}

		#region Statics
		private static Dictionary<Object, UBind> objectMetadatas = new Dictionary<Object, UBind>();
		public static UBind FromObject(UnityEngine.Object obj) {
			if(obj == null)
				throw new ArgumentNullException(nameof(obj));
			if(!objectMetadatas.TryGetValue(obj, out var val)) {
				val = new UBindObject(obj);
				objectMetadatas[obj] = val;
			}
			else if(val.rawValue != null && val.rawValue as UnityEngine.Object == null) {
				//Update in case the original object is destroyed.
				val = new UBindObject(obj);
				objectMetadatas[obj] = val;
			}
			return val;
		}

		public static UBind FromGraphElement(UGraphElement graphElement) {
			var container = graphElement.graphContainer;
			if(container as Object != null) {
				var bind = FromObject(container as Object);
				return bind.GraphElement(graphElement.id);
			}
			return null;
		}

		public static UBind FromNode(UGraphElement graphElement) {
			var container = graphElement.graphContainer;
			if(container as Object != null) {
				var bind = FromObject(container as Object);
				return bind.GraphElement(graphElement.id);
			}
			return null;
		}

		public static UBind FromStatic(Type type) {
			throw null;
		}
		#endregion

		#region Indexers
		public UBind this[string name] => Member(name);

		//public UBind this[int index] => Index(index);
		#endregion

		#region Digs
		protected Dictionary<string, UBind> childrens = new Dictionary<string, UBind>();

		public UBind Member(string name) {
			if(!childrens.TryGetValue(name, out var val)) {
				var member = valueType.GetMemberCached(name);
				if(member is FieldInfo || member is PropertyInfo) {
					val = new UBindMember(member, this);
					childrens[name] = val;
				} else {
					throw new Exception("Missing member with name: " + name + " on type:" + parent.type);
				}
			}
			return val;
		}

		public UBind Index(int index) {
			if(!childrens.TryGetValue(index.ToString(), out var val)) {
				val = new UBindIndex(index, this);
				childrens[index.ToString()] = val;
			}
			return val;
		}

		public UBind GraphElement(int id) {
			if(!childrens.TryGetValue("@" + id, out var val)) {
				if(value is IGraph graph) {
					var element = graph.GetGraphElement(id);
					if(element != null) {
						val = new UBindGraphElement(element, this);
						childrens["@" + id] = val;
					}
				} else {
					throw new Exception("The value is not graph container");
				}
			}
			return val;
		}

		public UBind NodeElement(int id) {
			return GraphElement(id)?[nameof(NodeObject.node)];
		}

		public UBind Access(string path) {
			var metadata = this;
			path = path.Replace(".Array.data[", ".");
			foreach(var pathPart in path.Split('.')) {
				var str = pathPart;
				if(!string.IsNullOrEmpty(str)) {
					if(str[0] == '@') {
						metadata = metadata.GraphElement(int.Parse(str.Substring(1)));
						continue;
					} else if(str[str.Length - 1] == ']') {
						str = pathPart.RemoveLast();
					}
				}
				if(int.TryParse(str, out var index)) {
					metadata = metadata.Index(index);
				} else {
					metadata = metadata.Member(str);
				}
			}

			return metadata;
		}
		#endregion

		#region Attribute
		public abstract Attribute[] GetCustomAttributes(bool inherit = true);
		#endregion
	}

	public sealed class UBindObject : UBind {
		private Object target;

		public override bool isRoot => true;

		public override Type type => target != null ? target.GetType() : typeof(Object);

		protected override object rawValue {
			get => target;
			set {
				throw new InvalidOperationException();
			}
		}

		public UBindObject(Object target) : base("bind", null) {
			this.target = target;
		}
		public override Attribute[] GetCustomAttributes(bool inherit = true) {
			return Array.Empty<Attribute>();
		}
	}

	public sealed class UBindIndex : UBind {
		private int index;
		private Type parentType;
		private Type _type;
		public override Type type => _type;
		protected override object rawValue {
			get {
				if(parentType.IsCastableTo(typeof(IList))) {
					return (parent.value as IList)[index];
				} else {
					return (parent.value as IEnumerable).Cast<object>().ElementAt(index);
				}
			}
			set {
				if(parentType.IsCastableTo(typeof(IList))) {
					(parent.value as IList)[index] = value;
				} else {
					throw new InvalidOperationException();
				}
			}
		}

		public UBindIndex(int index, UBind parent) : base(index.ToString(), parent) {
			this.index = index;
			Initialize(true);
		}

		private void Initialize(bool throwOnFail) {
			var parentType = parent.valueType;
			this.parentType = parentType;
			_type = parentType.ElementType();
			if(type.IsCastableTo(typeof(IList))) {
				if(index < 0 || index >= ((IList)parent.value).Count) {
					if(throwOnFail) {
						throw new ArgumentOutOfRangeException("index");
					} else {
						Unbind();
						return;
					}
				}
			}
			label = new GUIContent(parent.label);
		}

		protected override void OnParentValueChange(object previousValue, object newValue) {
			Initialize(false);
		}
		public override Attribute[] GetCustomAttributes(bool inherit = true) {
			return parent.GetCustomAttributes(inherit);
		}
	}

	public sealed class UBindGraphElement : UBind {
		private UGraphElementRef elementRef;

		public UGraphElement element => elementRef.reference;

		public UBindGraphElement(UGraphElement element, UBind parent) : base("@" + element.id, parent) {
			elementRef = new UGraphElementRef(element);
			label = new GUIContent(element.name);
		}

		public override Type type => element?.GetType() ?? typeof(UGraphElement);
		protected override object rawValue {
			get {
				return element;
			}
			set {
				throw new Exception("Graph element value cannot be changed.");
			}
		}

		public override Attribute[] GetCustomAttributes(bool inherit = true) {
			return Array.Empty<Attribute>();
		}
	}

	public sealed class UBindMember : UBind {
		public enum Mode {
			Field,
			Property,
		}

		public override Type type => memberType;
		protected override object rawValue {
			get {
				switch(mode) {
					case Mode.Field:
						try {
							return field.GetValueOptimized(parent.value);
						}
						catch {
							Debug.LogError("Error from:" + field);
							throw;
						}
					case Mode.Property:
						return property.GetValueOptimized(parent.value);
					default:
						throw new InvalidOperationException();
				}
			}
			set {
				switch(mode) {
					case Mode.Field:
						field.SetValueOptimized(parent.value, value);
						break;
					case Mode.Property:
						property.SetValueOptimized(parent.value, value);
						break;
				}
			}
		}

		public string name { get; private set; }
		public MemberInfo info { get; private set; }
		public FieldInfo field { get; private set; }
		public PropertyInfo property { get; private set; }
		public Mode mode { get; private set; }
		private Type memberType;

		public UBindMember(MemberInfo member, UBind parent) : base(member.Name, parent) {
			name = member.Name;
			label = new GUIContent(UnityEditor.ObjectNames.NicifyVariableName(name));
			if(member.IsDefined(typeof(TooltipAttribute), true)) {
				label.tooltip = member.GetCustomAttribute<TooltipAttribute>().tooltip;
			}
			info = member;
			if(member is FieldInfo) {
				field = member as FieldInfo;
				mode = Mode.Field;
				memberType = field.FieldType;
			} else if(member is PropertyInfo) {
				property = member as PropertyInfo;
				mode = Mode.Property;
				memberType = property.PropertyType;
			}
		}

		public override Attribute[] GetCustomAttributes(bool inherit = true) {
			return Attribute.GetCustomAttributes(info, inherit);
		}
	}
}