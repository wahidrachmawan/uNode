using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode {
	/// <summary>
	/// This class used to save data to be used for constucting a new object
	/// </summary>
	[System.Serializable]
	public sealed class ValueData : ISerializationCallbackReceiver, IGetValue {
		[System.NonSerialized]
		private BaseValueData value;

		public BaseValueData Value {
			get {
				return value;
			}
			set {
				this.value = value;
				OnBeforeSerialize();
			}
		}

		[SerializeField]
		private MemberData serializedType = new MemberData();
		[SerializeField, HideInInspector]
		private string Type;

		public MemberData typeData {
			get {
				if(!string.IsNullOrEmpty(Type)) {
					serializedType = MemberData.CreateFromType(Type);
					Type = string.Empty;
				}
				return serializedType;
			}
			set {
				serializedType = value;
				Type = string.Empty;
			}
		}

		/// <summary>
		/// The type of this variable
		/// </summary>
		public System.Type type {
			get {
				return typeData.startType;
			}
			set {
				typeData = MemberData.CreateFromType(value);
			}
		}

		public object Get() {
			if(value == null)
				return null;
			return value.Get();
		}

		public object GetNew() {
			OnAfterDeserialize();
			return Get();
		}

		[SerializeField]
		private byte[] _odinSerializedData;
		[SerializeField]
		private List<Object> listReference;

		public void OnBeforeSerialize() {
//#if UNITY_EDITOR
//			Event e = Event.current;
//			if(e != null && e.type != EventType.Used &&
//				(e.type == EventType.Repaint ||
//				e.type == EventType.MouseDrag ||
//				e.type == EventType.Layout ||
//				e.type == EventType.ScrollWheel)) {
//				return;
//			}
//#endif
			_odinSerializedData = SerializerUtility.Serialize(value, out listReference);
		}

		public void OnAfterDeserialize() {
			value = SerializerUtility.DeserializeWeak(_odinSerializedData, listReference) as BaseValueData;
		}

		public override string ToString() {
			if(value == null) {
				return "null";
			}
			return value.ToString();
		}

		public static ValueData CreateFromConstructor(System.Reflection.ConstructorInfo ctor) {
			var data = new ValueData();
			if(ctor != null) {
				data.type = ReflectionUtils.GetMemberType(ctor);
				data.Value = new ConstructorValueData(ctor);
			}
			return data;
		}

		public static ValueData CreateFromValue(object value) {
			var data = new ValueData();
			if(value != null) {
				data.type = value.GetType();
				data.Value = new ObjectValueData() { value = value };
			}
			return data;
		}

		public static ValueData CreateFromValue(ConstructorValueData value) {
			var data = new ValueData();
			if(value != null) {
				data.type = value.type;
				data.Value = value;
			}
			return data;
		}

		public static ValueData CreateFromValue(ObjectValueData value) {
			var data = new ValueData();
			if(value != null) {
				data.type = value.type;
				data.Value = value;
			}
			return data;
		}
	}

	/// <summary>
	/// Base class for all value data.
	/// </summary>
	[System.Serializable]
	public abstract class BaseValueData : IGetValue {
		public abstract System.Type type { get; }
		public abstract object Get();
	}

	[System.Serializable]
	public class VariableData : IValue, IAttributeSystem {
		public string name;
		public SerializedValue serializedValue;

		public List<AttributeData> attributes = new List<AttributeData>();

		[System.NonSerialized]
		public FieldModifier modifier;

		public bool resetOnEnter = true;

		public System.Type type {
			get {
				if(serializedValue != null) {
					return serializedValue.type;
				}
				return typeof(object);
			}
			set {
				if(serializedValue != null) {
					serializedValue.type = value;
				} else {
					serializedValue = new SerializedValue(null, value);
				}
			}
		}

		public object value {
			get => Get();
			set => Set(value);
		}

		public object Get() {
			return serializedValue.value;
		}

		public void Set(object value) {
			serializedValue = new SerializedValue(value);
		}

		List<AttributeData> IAttributeSystem.Attributes {
			get => attributes;
		}

		#region Constructors
		public VariableData() { }

		public VariableData(string name, System.Type type, object value = null) {
			this.name = name;
			this.type = type;
			this.value = value;
		}
		#endregion
	}

	public abstract class BaseValueData<T> : BaseValueData {
		public T value;

		public override System.Type type {
			get {
				if(value != null) {
					return value.GetType();
				}
				return typeof(T);
			}
		}

		public override object Get() {
			if(value is BaseValueData) {
				return (value as BaseValueData).Get();
			}
			return value;
		}
		
		public BaseValueData(T value) {
			this.value = value;
		}
	}

	/// <summary>
	/// A class that hold any instance object.
	/// </summary>
	[System.Serializable]
	public class ObjectValueData : BaseValueData {
		public object value;

		public override System.Type type {
			get {
				if(value != null) {
					return value.GetType();
				}
				return null;
			}
		}

		public override object Get() {
			if(value is BaseValueData) {
				return (value as BaseValueData).Get();
			}
			return value;
		}

		public ObjectValueData() { }

		public ObjectValueData(object value) {
			this.value = value;
		}
	}

	[System.Serializable]
	public class FieldValueData : BaseValueData<MemberData> {
		public string name;

		public FieldValueData() : base(MemberData.None) {

		}
		
		public FieldValueData(MemberData value) : base(value) {
		}
	}

	/// <summary>
	/// A class that hold constructor data.
	/// </summary>
	[System.Serializable]
	public class ConstructorValueData : BaseValueData {
		public ParameterValueData[] parameters = new ParameterValueData[0];
		public ParameterValueData[] initializer = new ParameterValueData[0];
		public SerializedType typeData = new SerializedType(typeof(object));

		public override System.Type type {
			get { 
				return typeData.type;
			}
		}

		public override object Get() {
			object obj = System.Activator.CreateInstance(type, parameters?.Select(i => i.Get()).ToArray());
			ApplyInitializer(ref obj);
			return obj;
		}

		public ConstructorValueData() { }

		public ConstructorValueData(System.Reflection.ConstructorInfo ctor) {
			typeData = new SerializedType(ctor.DeclaringType);
			System.Reflection.ParameterInfo[] param = ctor.GetParameters();
			parameters = new ParameterValueData[param.Length];
			for(int i = 0; i < param.Length; i++) {
				parameters[i] = new ParameterValueData(param[i].Name, param[i].ParameterType);
			}
		}

		public ConstructorValueData(System.Reflection.ConstructorInfo ctor, params ParameterValueData[] parameters) {
			typeData = new SerializedType(ctor.DeclaringType);
			this.parameters = parameters;
		}

		public ConstructorValueData(System.Type type) {
			typeData = new SerializedType(type);
		}

		public ConstructorValueData(object value) {
			if(value == null)
				throw new System.ArgumentNullException(nameof(value));
			var type = value.GetType();
			var ctors = type.GetConstructors();
			if(ctors.Length > 0) {
				var ctor = ctors[0];
				for(int i=0;i<ctors.Length;i++) {
					if(ctors[i].GetParameters().Length == 0) {
						ctor = ctors[i];
						break;
					}
				}
				System.Reflection.ParameterInfo[] param = ctor.GetParameters();
				parameters = new ParameterValueData[param.Length];
				for(int i = 0; i < param.Length; i++) {
					parameters[i] = new ParameterValueData(param[i].Name, param[i].ParameterType);
				}
				var initializers = new List<ParameterValueData>();
				var members = type.GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				for(int i=0;i<members.Length;i++) {
					var member = members[i];
					if(member is System.Reflection.FieldInfo field) {
						var val = field.GetValue(value);
						initializers.Add(new ParameterValueData(field.Name, field.FieldType, val));
					} else if(member is System.Reflection.PropertyInfo property) {
						if(property.CanRead && property.CanWrite) {
							var val = property.GetValue(value);
							initializers.Add(new ParameterValueData(property.Name, property.PropertyType, val));
						}
					}
				}
			}
			typeData = new SerializedType(value.GetType());
		}

		public void ApplyInitializer(ref object obj) {
			if(obj == null)
				return;
			System.Type t = obj.GetType();
			if(t != null && t == type && initializer.Length > 0) {
				if(obj is System.Collections.IList) {
					if(t.IsArray) {
						System.Array array = obj as System.Array;
						foreach(var param in initializer) {
							if(param == null)
								continue;
							uNodeUtility.AddArray(ref array, param.Get());
						}
					} else {
						System.Collections.IList list = obj as System.Collections.IList;
						foreach(var param in initializer) {
							if(param == null)
								continue;
							list.Add(param.Get());
						}
					}
				} else {
					foreach(var param in initializer) {
						if(param == null)
							continue;
						var members = t.GetMember(param.name);
						if(members.Length == 0)
							continue;
						foreach(var member in members) {
							if(member is System.Reflection.FieldInfo) {
								var field = member as System.Reflection.FieldInfo;
								field.SetValueOptimized(obj, param.Get());
								break;
							} else if(member is System.Reflection.PropertyInfo) {
								var prop = member as System.Reflection.PropertyInfo;
								prop.SetValueOptimized(obj, param.Get());
								break;
							}
						}
					}
				}
			}
		}

		public override string ToString() {
			if(type != null) {
				string pInfo = null;
				if(parameters != null && parameters.Length > 0) {
					for(int i = 0; i < parameters.Length; i++) {
						if(i != 0) {
							pInfo += ", ";
						}
						pInfo += parameters[i] != null ? parameters[i].ToString() : "null";
					}
				}
				return "new " + type + "(" + pInfo + ")";
			}
			return "null";
		}
	}

	/// <summary>
	/// A class that hold parameter data.
	/// </summary>
	[System.Serializable]
	public class ParameterValueData : BaseValueData {
		public string name;
		[SerializeField]
		private SerializedValue serializedValue = new SerializedValue();

		public ParameterValueData() {

		}

		public ParameterValueData(string name, System.Type type) {
			this.name = name;
			serializedValue = SerializedValue.CreateFromType(type);
		}

		public ParameterValueData(string name, System.Type type, object value) {
			this.name = name;
			serializedValue = new SerializedValue(value, type);
		}

		public override System.Type type {
			get {
				if(serializedValue != null) {
					return serializedValue.type;
				}
				return typeof(object);
			}
		}

		public object value {
			get => Get();
			set => Set(value);
		}

		public void Set(object value) {
			if(serializedValue == null) {
				serializedValue = new SerializedValue(value);
			}
			else {
				serializedValue.value = value;
			}
		}

		public override object Get() {
			return serializedValue.value;
		}

		public override string ToString() {
			if(type != null) {
				return type.PrettyName() + " " + name;
			}
			return base.ToString();
		}
	}
}