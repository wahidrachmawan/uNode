using System.Collections.Generic;
using UnityEngine;
using MaxyGames.OdinSerializer;
using System.Reflection;
using System;
using Object = UnityEngine.Object;
using MaxyGames.OdinSerializer.Utilities;
using System.Linq;

//[assembly: RegisterFormatterLocator(typeof(MaxyGames.UNode.SerializerUtility.UNodeFormatterLocator))]

namespace MaxyGames.UNode {
	//[Serializable]
	//public struct SerializationData {
	//	[SerializeField]
	//	public DataFormat SerializedFormat;
	//	[SerializeField]
	//	public byte[] SerializedBytes;
	//	[SerializeField]
	//	public List<SerializationNode> SerializationNodes;
	//	[SerializeField]
	//	public List<Object> References;

	//	public bool isFilled {
	//		get {
	//			switch(SerializedFormat) {
	//				case DataFormat.Binary:
	//					return SerializedBytes != null;
	//				case DataFormat.Nodes:
	//					return SerializationNodes != null;
	//				default:
	//					return false;
	//			}
	//		}
	//	}

	//	public void Reset() {
	//		SerializedFormat = DataFormat.Binary;
	//		SerializedBytes = new byte[0];
	//		SerializationNodes?.Clear();
	//		References?.Clear();
	//	}
	//}

	[System.Serializable]
	public class OdinSerializedData {
		public DataFormat format = DataFormat.Binary;

		public List<Object> references;

		public byte[] data;
		public List<SerializationNode> serializationNodes;
		public string serializedType;

		private System.Type _type;
		public System.Type type {
			get {
				if(_type == null) {
					if(string.IsNullOrEmpty(serializedType)) {
						return typeof(object);
					}
					_type = TypeSerializer.Deserialize(serializedType, false);
				}
				return _type;
			}
			set {
				_type = type;
				serializedType = type.FullName;
			}
		}

		public bool isFilled {
			get {
				switch(format) {
					case DataFormat.Nodes:
						return serializationNodes != null && serializationNodes.Count > 0;
					default:
						return data != null && data.Length > 0;
				}
			}
		}

		public object ToValue() {
			return SerializerUtility.Deserialize(this);
		}

		public void FromValue<T>(T value) {
			var serialized = CreateFrom(value);
			data = serialized.data;
			serializationNodes = serialized.serializationNodes;
			format = serialized.format;
			references = serialized.references;
			type = serialized.type;
		}

		public void CopyFrom(OdinSerializedData serializedData) {
			format = serializedData.format;
			if(serializedData.data != null) {
				data = new byte[serializedData.data.Length];
				Array.Copy(serializedData.data, data, data.Length);
			}
			else {
				data = Array.Empty<byte>();
			}
			if(serializedData.serializationNodes != null) {
				serializationNodes = new List<SerializationNode>(serializedData.serializationNodes);
			}
			references = new List<Object>(serializedData.references);
			serializedType = serializedData.serializedType;
		}

		public static OdinSerializedData CreateFrom<T>(T value) {
			return SerializerUtility.SerializeValue(value);
		}
	}

	public static class SerializerUtility {
		public static ISerializationPolicy serializationPolicy;

		#region Properties
		public static Cache<SerializationContext> UnitySerializationContext {
			get {
				var context = Cache<SerializationContext>.Claim();
				context.Value.Config.SerializationPolicy = SerializationPolicies.Unity;
				return context;
			}
		}

		public static Cache<DeserializationContext> UnityDeserializationContext {
			get {
				var context = Cache<DeserializationContext>.Claim();
				context.Value.Config.SerializationPolicy = SerializationPolicies.Unity;
				return context;
			}
		}

		public static Cache<SerializationContext> UNodeSerializationContext {
			get {
				var context = Cache<SerializationContext>.Claim();
				context.Value.Config.SerializationPolicy = serializationPolicy;
				return context;
			}
		}

		public static Cache<DeserializationContext> UNodeDeserializationContext {
			get {
				var context = Cache<DeserializationContext>.Claim();
				context.Value.Config.SerializationPolicy = serializationPolicy;
				return context;
			}
		}
		#endregion

		static SerializerUtility() {
			serializationPolicy = new CustomSerializationPolicy("UNodeSerializationPolicy", allowNonSerializableTypes: true, member => {
				if(member is FieldInfo field) {
					if(field.FieldType.IsDefined<GraphElementAttribute>(true)) {
						return false;
					}
				} 
				//else if(member is PropertyInfo property && property.IsDefined<SerializeField>(false)) {
				//	if(property.PropertyType.IsDefined<GraphElementAttribute>(true)) {
				//		Debug.Log(property);
				//		return false;
				//	}
				//}
				return SerializationPolicies.Unity.ShouldSerializeMember(member);
			});
		}

		#region Classes
		//class IgnoreFormatter<T> : IFormatter<T> {
		//	public Type SerializedType => typeof(T);

		//	public object Deserialize(IDataReader reader) {
		//		return null;
		//	}

		//	public void Serialize(object value, IDataWriter writer) {
		//		writer.WriteNull(null);
		//	}

		//	public void Serialize(T value, IDataWriter writer) {
		//		writer.WriteNull(null);
		//	}

		//	T IFormatter<T>.Deserialize(IDataReader reader) {
		//		return default(T);
		//	}
		//}

		//internal class UNodeFormatterLocator : IFormatterLocator {
		//	public bool TryGetFormatter(Type type, FormatterLocationStep step, ISerializationPolicy policy, out IFormatter formatter) {
		//		if(policy == serializationPolicy && step == FormatterLocationStep.AfterRegisteredFormatters) {
		//			if(type.IsSubclassOf(typeof(UGraphElement)) || type.IsSubclassOf(typeof(UPort))) {
		//				formatter = (IFormatter)Activator.CreateInstance(typeof(IgnoreFormatter<>).MakeGenericType(type));
		//				return true;
		//			}
		//		}
		//		formatter = null;
		//		return false;
		//	}
		//}

		public sealed class GraphReferenceResolver : IExternalIndexReferenceResolver, ICacheNotificationReceiver {
			private Dictionary<object, int> referenceIndexMapping = new Dictionary<object, int>(32, ReferenceEqualityComparer<object>.Default);
			private List<object> referencedObjects;
			private Graph graph;

			public GraphReferenceResolver() {
				this.referencedObjects = new List<object>();
			}

			public List<object> GetReferencedObjects() {
				return this.referencedObjects;
			}

			public void PrepareForSerialization(Graph graph) {
				this.graph = graph;
			}

			public void SetReferencedObjects(List<object> referencedUnityObjects) {
				if(referencedUnityObjects == null) {
					referencedUnityObjects = new List<object>();
				}

				this.referencedObjects = referencedUnityObjects;
				this.referenceIndexMapping.Clear();

				for(int i = 0; i < this.referencedObjects.Count; i++) {
					if(object.ReferenceEquals(this.referencedObjects[i], null) == false) {
						if(!this.referenceIndexMapping.ContainsKey(this.referencedObjects[i])) {
							this.referenceIndexMapping.Add(this.referencedObjects[i], i);
						}
					}
				}
			}

			/// <summary>
			/// Determines whether the specified value can be referenced externally via this resolver.
			/// </summary>
			/// <param name="value">The value to reference.</param>
			/// <param name="index">The index of the resolved value, if it can be referenced.</param>
			/// <returns>
			///   <c>true</c> if the reference can be resolved, otherwise <c>false</c>.
			/// </returns>
			public bool CanReference(object value, out int index) {
				if(this.referencedObjects == null) {
					this.referencedObjects = new List<object>(32);
				}

				var obj = value as UnityEngine.Object;

				if(object.ReferenceEquals(null, obj) == false || value is IValueReference) {
					//Serializing Unity Object or value references
					if(this.referenceIndexMapping.TryGetValue(value, out index) == false) {
						index = this.referencedObjects.Count;
						this.referenceIndexMapping.Add(value, index);
						this.referencedObjects.Add(value);
					}

					return true;
				} else if(value is UGraphElement) {
					UGraphElement graphElement = value as UGraphElement;
					if(!object.ReferenceEquals(graph, null) && (graphElement == null || graphElement.graph != graph)) {
						graphElement = null;
					}
					//Serializing other reference values
					if(this.referenceIndexMapping.TryGetValue(value, out index) == false) {
						index = this.referencedObjects.Count;
						this.referenceIndexMapping.Add(value, index);
						this.referencedObjects.Add(graphElement);
					}

					return true;
				} else if(value is UPort) {
					//Skip if value is port as serializing port might lead to unexpected behavior.
					if(this.referenceIndexMapping.TryGetValue(value, out index) == false) {
						index = this.referencedObjects.Count;
						this.referenceIndexMapping.Add(value, index);
						this.referencedObjects.Add(null);
					}

					return true;
				}

				index = -1;
				return false;
			}

			/// <summary>
			/// Tries to resolve the given reference index to a reference value.
			/// </summary>
			/// <param name="index">The index to resolve.</param>
			/// <param name="value">The resolved value.</param>
			/// <returns>
			///   <c>true</c> if the index could be resolved to a value, otherwise <c>false</c>.
			/// </returns>
			public bool TryResolveReference(int index, out object value) {
				if(this.referencedObjects == null || index < 0 || index >= this.referencedObjects.Count) {
					// Sometimes something has destroyed the list of references in between serialization and deserialization
					// (Unity prefab instances are especially bad at preserving such data), and in these cases we still don't
					// want the system to fall back to a formatter, so we give out a null value.
					value = null;
					return true;
				}

				value = this.referencedObjects[index];
				return true;
			}

			/// <summary>
			/// Resets this instance.
			/// </summary>
			public void Reset() {
				this.graph = null;
				this.referencedObjects = null;
				this.referenceIndexMapping.Clear();
			}

			void ICacheNotificationReceiver.OnFreed() {
				this.Reset();
			}

			void ICacheNotificationReceiver.OnClaimed() {
			}
		}
		#endregion

		#region Private
		private static SerializationContext GetValidSerializationContext(SerializationContext context = null) {
			if(context != null) {
				return context;
			}
			else {
				using(var cache = UNodeSerializationContext) {
					return cache;
				}
			}
		}

		private static DeserializationContext GetValidDeserializationContext(DeserializationContext context = null) {
			if(context != null) {
				return context;
			}
			else {
				using(var cache = UNodeDeserializationContext) {
					return cache;
				}
			}
		}

		private static T M_DeserializeValue<T>(byte[] bytes, DataFormat format, List<UnityEngine.Object> referencedUnityObjects, DeserializationContext context = null) {
			if(context != null) {
				return SerializationUtility.DeserializeValue<T>(bytes, format, referencedUnityObjects, context);
			} else {
				using(var cache = Cache<DeserializationContext>.Claim()) {
					cache.Value.Config.SerializationPolicy = serializationPolicy;
					return SerializationUtility.DeserializeValue<T>(bytes, format, referencedUnityObjects, cache.Value);
				}
			}
		}

		private static T M_DeserializeValue<T>(List<SerializationNode> serializationNodes, List<UnityEngine.Object> referencedUnityObjects, DeserializationContext context = null) {
			if(context != null) {
				return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(serializationNodes, referencedUnityObjects, typeof(T), context).ConvertTo<T>();
			}
			else {
				using(var cache = Cache<DeserializationContext>.Claim()) {
					cache.Value.Config.SerializationPolicy = serializationPolicy;
					return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(serializationNodes, referencedUnityObjects, typeof(T), cache.Value).ConvertTo<T>();
				}
			}
		}
		#endregion


		private class SerializedInstanceFieldData {
			public string name;
			public byte[] data;
			public SerializedType type;
		}

		private static bool IsSupportedForRuntimeSerialization(Type type) {
			if(type is IRuntimeMember)
				return false;
			if(type.IsArray) {
				return IsSupportedForRuntimeSerialization(type.GetElementType());
			}
			else if(type.IsGenericType) {
				var genericParameters = type.GetGenericArguments();
				foreach(var t in genericParameters) {
					if(IsSupportedForRuntimeSerialization(t) == false)
						return false;
				}
				return true;
			}
			return type.IsCastableTo(typeof(UnityEngine.Object)) == false;
		}

		public static void DeserializeInstanceGraphVariables(byte[] data, object obj) {
			var serializedDatas = SerializationUtility.DeserializeValue<List<SerializedInstanceFieldData>>(data, DataFormat.Binary, UnityDeserializationContext);
			if(serializedDatas != null) {
				if(obj is IInstancedGraph instancedGraph && instancedGraph.Instance != null) {
					var instance = instancedGraph.Instance;
					var graph = instancedGraph.OriginalGraph;
					var variables = graph.GetAllVariables().ToArray();
					foreach(var field in serializedDatas) {
						var variable = variables.FirstOrDefault(v => v.name == field.name);
						if(variable != null && field.type != null && field.type.type != null) {
							var value = DeserializeWeak(field.data, field.type, UnityDeserializationContext);
							variable.Set(instance, value);
						}
					}
				}
				else if(obj is IRuntimeClass runtimeClass) {
					foreach(var field in serializedDatas) {
						if(field.type != null && field.type.type != null) {
							var value = DeserializeWeak(field.data, field.type, UnityDeserializationContext);
							runtimeClass.SetVariable(field.name, value);
						}
					}
				}
				else if(obj is IRuntimeClassContainer) {
					DeserializeInstanceGraphVariables(data, (obj as IRuntimeClassContainer).RuntimeClass);
				}
			}
		}

		public static byte[] SerializeInstanceGraphVariables(object obj) {
			if(obj is IInstancedGraph instancedGraph && instancedGraph.Instance != null) {
				var graph = instancedGraph.OriginalGraph;
				var variables = graph.GetAllVariables();
				List<SerializedInstanceFieldData> serializedDatas = new List<SerializedInstanceFieldData>();
				foreach(var var in variables) {
					if(var.modifier.isPublic && IsSupportedForRuntimeSerialization(var.type)) {
						var data = SerializationUtility.SerializeValueWeak(instancedGraph.Instance.GetElementData(var), DataFormat.Binary, UnitySerializationContext);
						if(data != null) {
							serializedDatas.Add(new SerializedInstanceFieldData() {
								name = var.name,
								data = data,
								type = var.type,
							});
						}
					}
				}
				return SerializationUtility.SerializeValue(serializedDatas, DataFormat.Binary, UnitySerializationContext);
			}
			else if(obj is IRuntimeClass runtimeClass) {
				var db = uNodeDatabase.instance;
				if(db == null)
					throw new Exception("Database was not found.");
				var graph = db.GetGraphByUID(runtimeClass.uniqueIdentifier, false);
				if(graph != null) {
					var variables = graph.GetAllVariables();
					List<SerializedInstanceFieldData> serializedDatas = new List<SerializedInstanceFieldData>();
					foreach(var var in variables) {
						if(var.modifier.isPublic && IsSupportedForRuntimeSerialization(var.type)) {
							var data = SerializationUtility.SerializeValueWeak(runtimeClass.GetVariable(var.name), DataFormat.Binary, UnitySerializationContext);
							if(data != null) {
								serializedDatas.Add(new SerializedInstanceFieldData() {
									name = var.name,
									data = data,
									type = var.type,
								});
							}
						}
					}
					return SerializationUtility.SerializeValue(serializedDatas, DataFormat.Binary, UnitySerializationContext);
				}
				else {
					throw new Exception("Graph with id: " + runtimeClass.uniqueIdentifier + " was not found in database.");
				}
			}
			else if(obj is IRuntimeClassContainer) {
				return SerializeInstanceGraphVariables((obj as IRuntimeClassContainer).RuntimeClass);
			}
			return null;
		}

		public static T Duplicate<T>(T value) {
			if(value == null) {
				return default;
			} else if(value is Object) {
				return value;
			}
			else if(value.GetType().IsValueType) {
				return (T)ReflectionUtils.ValuePassing(value);
			}
			return Deserialize<T>(Serialize(value, out var references), references);
		}

		public static OdinSerializedData SerializeValue<T>(T value, SerializationContext context = null) {
			OdinSerializedData data = new OdinSerializedData();
			data.data = Serialize(value, out data.references, context);
			//Since odin might fail on deserializing primitive type, we need save the original type so when deserializing value odin should not fail 
			if(typeof(T) == typeof(object) && value != null) {
				//Ensure we get the correct value type when the value is object
				var type = value.GetType();
				if(type.IsPrimitive) {
					data.type = type;
				}
			} else if(typeof(T).IsPrimitive) {
				data.type = typeof(T);
			}
			return data;
		}

		public static OdinSerializedData SerializeValue<T>(T value, DataFormat format, SerializationContext context = null) {
			context = GetValidSerializationContext(context);

			OdinSerializedData data = new OdinSerializedData();
			data.format = format;
			if(format == DataFormat.Nodes) {
				OdinSerializer.Unity_Integration.uNodeSerializationUtility.SerializeToNodes(value, ref data.serializationNodes, ref data.references, context);
				if(data.data != null)
					data.data = Array.Empty<byte>();
			}
			else {
				data.data = Serialize(value, out data.references, context);
				data.serializationNodes?.Clear();
			}
			//Since odin might fail on deserializing primitive type, we need save the original type so when deserializing value odin should not fail 
			if(typeof(T) == typeof(object) && value != null) {
				//Ensure we get the correct value type when the value is object
				var type = value.GetType();
				if(type.IsPrimitive) {
					data.type = type;
				}
			}
			else if(typeof(T).IsPrimitive) {
				data.type = typeof(T);
			}
			return data;
		}

		public static object Deserialize(OdinSerializedData serializedData) {
			if(serializedData == null)
				return null;
			if(serializedData.format == DataFormat.Nodes) {
				return DeserializeWeak(serializedData.serializationNodes, serializedData.references, serializedData.type);
			}
			else {
				return DeserializeWeak(serializedData.data, serializedData.references, serializedData.type);
			}
		}

		public static byte[] Serialize<T>(T value, SerializationContext context = null) {
			return SerializationUtility.SerializeValue(value, DataFormat.Binary, GetValidSerializationContext(context));
		}

		public static byte[] Serialize<T>(T value, out List<Object> unityReferences, SerializationContext context = null) {
			if(value == null) {
				unityReferences = new List<Object>();
				return new byte[0];
			}
			//else if(typeof(T) == typeof(object)) {
			//	return SerializationUtility.SerializeValueWeak(value, DataFormat.Binary, out unityReferences);
			//}
			return SerializationUtility.SerializeValue(value, DataFormat.Binary, out unityReferences, GetValidSerializationContext(context));
		}

		public static T Deserialize<T>(byte[] data, DeserializationContext context = null) {
			return SerializationUtility.DeserializeValue<T>(data, DataFormat.Binary, GetValidDeserializationContext(context));
		}

		public static T Deserialize<T>(byte[] data, List<Object> unityReferences, DeserializationContext context = null) {
			return M_DeserializeValue<T>(data, DataFormat.Binary, unityReferences, context);
		}

		public static T Deserialize<T>(OdinSerializedData serializedData, DeserializationContext context = null) {
			if(serializedData == null || !serializedData.isFilled)
				return default;
			if(serializedData.format == DataFormat.Nodes) {
				return M_DeserializeValue<T>(serializedData.serializationNodes, serializedData.references, context);
			}
			else {
				return M_DeserializeValue<T>(serializedData.data, DataFormat.Binary, serializedData.references, context);
			}
		}

		public static object DeserializeWeak(byte[] data, List<Object> unityReferences, System.Type type = null) {
			if(data == null) {
				if(type != null && type.IsValueType) {
					//Ensure we create new value if the type is value type
					//return ReflectionUtils.CreateInstance(type);
				}
				return null;
			}
			if(type != null) {
				//This will fix some incorrect type result for primitive type
				switch(type.FullName) {
					case "System.Char":
						return SerializationUtility.DeserializeValue<char>(data, DataFormat.Binary);
					case "System.Single":
						return SerializationUtility.DeserializeValue<float>(data, DataFormat.Binary);
					case "System.Int32":
						return SerializationUtility.DeserializeValue<int>(data, DataFormat.Binary);
					case "System.Int64":
						return SerializationUtility.DeserializeValue<long>(data, DataFormat.Binary);
					case "System.Byte":
						return SerializationUtility.DeserializeValue<byte>(data, DataFormat.Binary);
					case "System.Boolean":
						return SerializationUtility.DeserializeValue<bool>(data, DataFormat.Binary);
					case "System.String":
						return SerializationUtility.DeserializeValue<string>(data, DataFormat.Binary);
				}
				return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, DataFormat.Binary, unityReferences, type);
			}
			return SerializationUtility.DeserializeValueWeak(data, DataFormat.Binary, unityReferences);
		}

		public static object DeserializeWeak(List<SerializationNode> data, List<Object> unityReferences, System.Type type = null) {
			if(data == null) {
				if(type != null && type.IsValueType) {
					//Ensure we create new value if the type is value type
					//return ReflectionUtils.CreateInstance(type);
				}
				return null;
			}
			if(type != null) {
				//This will fix some incorrect type result for primitive type
				switch(type.FullName) {
					case "System.Char":
						return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, unityReferences, typeof(char));
					case "System.Single":
						return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, unityReferences, typeof(float));
					case "System.Int32":
						return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, unityReferences, typeof(int));
					case "System.Int64":
						return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, unityReferences, typeof(long));
					case "System.Byte":
						return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, unityReferences, typeof(byte));
					case "System.Boolean":
						return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, unityReferences, typeof(bool));
					case "System.String":
						return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, unityReferences, typeof(string));
				}
				return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, unityReferences, type);
			}
			return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, unityReferences, typeof(object));
		}

		public static object DeserializeWeak(byte[] data, System.Type type = null, DeserializationContext context = null) {
			context = GetValidDeserializationContext(context);
			if(data == null) {
				if(type != null && type.IsValueType) {
					//Ensure we create new value if the type is value type
					//return ReflectionUtils.CreateInstance(type);
				}
				return null;
			}
			if(data.Length == 0) {
				return null;
			} else if(type != null) {
				//This will fix some incorrect type result for primitive type
				switch(type.FullName) {
					case "System.Char":
						return SerializationUtility.DeserializeValue<char>(data, DataFormat.Binary, context);
					case "System.Single":
						return SerializationUtility.DeserializeValue<float>(data, DataFormat.Binary, context);
					case "System.Int32":
						return SerializationUtility.DeserializeValue<int>(data, DataFormat.Binary, context);
					case "System.Int64":
						return SerializationUtility.DeserializeValue<long>(data, DataFormat.Binary, context);
					case "System.Byte":
						return SerializationUtility.DeserializeValue<byte>(data, DataFormat.Binary, context);
					case "System.Boolean":
						return SerializationUtility.DeserializeValue<bool>(data, DataFormat.Binary, context);
					case "System.String":
						return SerializationUtility.DeserializeValue<string>(data, DataFormat.Binary, context);
				}
				if(context != null) {
					return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, DataFormat.Binary, type, context);
				} else {
					using(var cache = UNodeDeserializationContext) {
						return OdinSerializer.Unity_Integration.uNodeSerializationUtility.DeserializeValueWeak(data, DataFormat.Binary, type, cache);
					}
				}
			}
			return SerializationUtility.DeserializeValueWeak(data, DataFormat.Binary, context);
		}

		//public static string SerializeToJson<T>(T value) {
		//	if(value == null) {
		//		return string.Empty;
		//	}
		//	if(value is Object) {
		//		return string.Empty;
		//	}
		//	return System.Text.Encoding.UTF8.GetString(M_SerializeValue(value, DataFormat.JSON));
		//}

		//public static string SerializeToJson<T>(T value, out List<Object> unityReferences) {
		//	if(value == null) {
		//		unityReferences = new List<Object>();
		//		return string.Empty;
		//	}
		//	if(value is Object) {
		//		unityReferences = new List<Object>() { value as Object };
		//		return string.Empty;
		//	}
		//	UnitySerializationUtility.DeserializeUnityObject
		//	return System.Text.Encoding.UTF8.GetString(M_SerializeValue(value, DataFormat.JSON, out unityReferences));
		//}

		//public static T DeserializeFromJson<T>(string json) {
		//	if(string.IsNullOrEmpty(json)) {
		//		return default(T);
		//	}
		//	return SerializationUtility.DeserializeValue<T>(System.Text.Encoding.UTF8.GetBytes(json), DataFormat.JSON);
		//}

		//public static T DeserializeFromJson<T>(string json, List<Object> unityReferences) {
		//	if(string.IsNullOrEmpty(json)) {
		//		return default(T);
		//	}
		//	return SerializationUtility.DeserializeValue<T>(System.Text.Encoding.UTF8.GetBytes(json), DataFormat.JSON, unityReferences);
		//}

		//public static object DeserializeFromJson(string json, List<Object> unityReferences, System.Type type = null) {
		//	if(string.IsNullOrEmpty(json)) {
		//		if(type != null && type.IsValueType) {
		//			//Ensure we create new value if the type is value type
		//			return ReflectionUtils.CreateInstance(type);
		//		}
		//		return null;
		//	}
		//	var data = System.Text.Encoding.UTF8.GetBytes(json);
		//	if(type.IsCastableTo(typeof(Object)) || data.Length == 0) {
		//		if(unityReferences != null && unityReferences.Count > 0) {
		//			return unityReferences[0];
		//		}
		//		return null;
		//	} else if(type != null) {
		//		//This will fix some incorrect type result for primitive type
		//		switch(type.FullName) {
		//			case "System.Char":
		//				return SerializationUtility.DeserializeValue<char>(data, DataFormat.JSON, unityReferences);
		//			case "System.Single":
		//				return SerializationUtility.DeserializeValue<float>(data, DataFormat.JSON, unityReferences);
		//			case "System.Int32":
		//				return SerializationUtility.DeserializeValue<int>(data, DataFormat.JSON, unityReferences);
		//			case "System.Int64":
		//				return SerializationUtility.DeserializeValue<long>(data, DataFormat.JSON, unityReferences);
		//			case "System.Byte":
		//				return SerializationUtility.DeserializeValue<byte>(data, DataFormat.JSON, unityReferences);
		//			case "System.Boolean":
		//				return SerializationUtility.DeserializeValue<bool>(data, DataFormat.JSON, unityReferences);
		//			case "System.String":
		//				return SerializationUtility.DeserializeValue<string>(data, DataFormat.JSON, unityReferences);
		//		}
		//	}
		//	return SerializationUtility.DeserializeValueWeak(data, DataFormat.JSON, unityReferences);
		//}
	}
}