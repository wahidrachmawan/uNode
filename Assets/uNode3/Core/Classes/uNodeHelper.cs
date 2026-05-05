using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MaxyGames.UNode {
	/// <summary>
	/// Provides useful function.
	/// </summary>
	public static class uNodeHelper {
		public static class RuntimeUtility {
			public static void InitializeVariables(IRuntimeClass target, IGraph graph, List<VariableData> variables) {
				//Initialize the variable
				foreach(var v in graph.GetAllVariables()) {
					var var = v;
					for(int x = 0; x < variables.Count; x++) {
						if(var.name.Equals(variables[x].name)) {
							target.SetVariable(var.name, variables[x].Get());
							goto skip;
						}
					}
					target.SetVariable(var.name, SerializerUtility.Duplicate(var.defaultValue));
				skip:
					continue;
				}
			}

			public static void SetVariable(object instance, string name, object value) {
				var accessor = ObjectAccessor.GetInstance(instance.GetType());
				accessor.SetVariable(instance, name, value);
			}

			public static void SetVariable(object instance, string name, object value, char @operator) {
				var accessor = ObjectAccessor.GetInstance(instance.GetType());
				accessor.SetVariable(instance, name, value, @operator);
			}

			public static object GetVariable(object instance, string name) {
				var accessor = ObjectAccessor.GetInstance(instance.GetType());
				return accessor.GetVariable(instance, name);
			}

			public static object GetProperty(object instance, string name) {
				var accessor = ObjectAccessor.GetInstance(instance.GetType());
				return accessor.GetProperty(instance, name);
			}

			public static void SetProperty(object instance, string name, object value) {
				var accessor = ObjectAccessor.GetInstance(instance.GetType());
				accessor.SetProperty(instance, name, value);
			}

			public static void SetProperty(object instance, string name, object value, char @operator) {
				var accessor = ObjectAccessor.GetInstance(instance.GetType());
				accessor.SetProperty(instance, name, value, @operator);
			}

			public static object InvokeFunction(object instance, string name, object[] values) {
				var accessor = ObjectAccessor.GetInstance(instance.GetType());
				return accessor.InvokeFunction(instance, name, values);
			}

			public static object InvokeFunction(object instance, string name, Type[] parameters, object[] values) {
				var accessor = ObjectAccessor.GetInstance(instance.GetType());
				return accessor.InvokeFunction(instance, name, parameters, values);
			}

			public static object InvokeFunctionByID(object obj, string graphID, int functionID, object[] values) {
				if(values != null) {
					for(int i = 0; i < values.Length; i++) {
						values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
					}
				}
				var graph = GetGraphByID(graphID);
				var func = graph.GetGraphElement(functionID) as Function;
				if(func == null) {
					throw new Exception($"Function with id:{functionID} not found from graph {graph}." +
						"\nIt may because it was removed or wrong given ID.");
				}
				if(obj is IInstancedGraph instanced && instanced.Instance != null) {
					return func.Invoke(instanced.Instance, values);
				} else if(obj is IRuntimeClassContainer container) {
					return InvokeFunction(container.RuntimeClass, func.name, func.ParameterTypes, values);
				}
				return InvokeFunction(obj, func.name, func.ParameterTypes, values);
			}
		}

		internal sealed class ObjectAccessor {
			readonly Dictionary<string, FieldInfo> fields = new();
			readonly Dictionary<int, FieldInfo> fieldsID = new();
			readonly Dictionary<string, PropertyInfo> properties = new();
			readonly Dictionary<int, PropertyInfo> propertiesID = new();
			readonly Dictionary<string, EventInfo> events = new();
			readonly Dictionary<int, EventInfo> eventsID = new();
			readonly Dictionary<MethodSignature, MethodInfo> methods = new();
			readonly Dictionary<int, MethodInfo> methodsID = new();

			static Dictionary<Type, ObjectAccessor> instances = new();

			public static ObjectAccessor GetInstance(Type type) {
				if(!instances.TryGetValue(type, out var result)) {
					result = new ObjectAccessor(type);
					instances[type] = result;
				}
				return result;
			}

			struct MethodSignature : IEquatable<MethodSignature> {
				#region Fields
				private string methodName;
				private Type[] parameterTypes;
				private int cachedHash;
				#endregion

				#region Constructors
				public MethodSignature(string name, params Type[] parameters) {
					methodName = name;
					parameterTypes = parameters ?? new Type[0];
					cachedHash = 0;
					cachedHash = ComputeHash();
				}

				public MethodSignature(MethodInfo method) {
					if(method == null) {
						methodName = "";
						parameterTypes = new Type[0];
						cachedHash = 0;
						cachedHash = ComputeHash();
						return;
					}

					methodName = method.Name;

					ParameterInfo[] parameters = method.GetParameters();
					parameterTypes = new Type[parameters.Length];
					for(int i = 0; i < parameters.Length; i++) {
						parameterTypes[i] = parameters[i].ParameterType;
					}

					cachedHash = 0;
					cachedHash = ComputeHash();
				}
				#endregion

				private int ComputeHash() {
					unchecked {
						// Use GetHashCode for maximum speed
						int hash = methodName?.GetHashCode() ?? 0;
						hash = hash * 31 + (parameterTypes?.Length ?? 0).GetHashCode();

						for(int i = 0; i < (parameterTypes?.Length ?? 0); i++) {
							if(parameterTypes[i] != null) {
								// Combine type name and namespace for uniqueness
								hash = hash * 31 + (parameterTypes[i].Name?.GetHashCode() ?? 0);
								hash = hash * 31 + (parameterTypes[i].Namespace?.GetHashCode() ?? 0);
							}
						}

						return hash;
					}
				}

				public bool Equals(MethodSignature other) {
					// Quick check using hash first
					if(cachedHash != other.cachedHash)
						return false;

					// Full equality check
					if(methodName != other.methodName)
						return false;

					int thisParamCount = parameterTypes?.Length ?? 0;
					int otherParamCount = other.parameterTypes?.Length ?? 0;

					if(thisParamCount != otherParamCount)
						return false;

					for(int i = 0; i < thisParamCount; i++) {
						if(parameterTypes[i] != other.parameterTypes[i])
							return false;
					}

					return true;
				}

				public override bool Equals(object obj) {
					return obj is MethodSignature other && Equals(other);
				}

				public override int GetHashCode() {
					return cachedHash;
				}

				public static bool operator ==(MethodSignature left, MethodSignature right) {
					return left.Equals(right);
				}

				public static bool operator !=(MethodSignature left, MethodSignature right) {
					return !left.Equals(right);
				}

				public override string ToString() {
					string paramString = parameterTypes != null
						? string.Join(", ", Array.ConvertAll(parameterTypes, t => t?.Name ?? "null"))
						: "";
					return $"{methodName}({paramString}) [Hash: {cachedHash}]";
				}
			}

			public ObjectAccessor(Type type) {
				const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

				var members = type.GetMembers(flags);

				foreach(var member in members) {
					if(member is FieldInfo field) {
						fields[member.Name] = field;
						fieldsID[Animator.StringToHash(member.Name)] = field;
					}
					else if(member is PropertyInfo property) {
						properties[member.Name] = property;
						propertiesID[Animator.StringToHash(member.Name)] = property;
					}
					else if(member is MethodInfo method) {
						var signature = $"{method.Name}{string.Join(',', method.GetParameters().Select(p => p.ParameterType.FullName))}";
						methods[new MethodSignature(method)] = method;
						methodsID[Animator.StringToHash(signature)] = method;
					}
					else if(member is EventInfo eventInfo) {
						events[member.Name] = eventInfo;
						eventsID[Animator.StringToHash(member.Name)] = eventInfo;
					}
				}
			}

			#region ByName
			public void SetVariable(object instance, string name, object value) {
				if(fields.TryGetValue(name, out var field) == false) {
					throw new Exception($"Variable with name:{name} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				try {
					field.SetValueOptimized(instance, value);
				}
				catch(Exception ex) {
					throw new Exception($"Error on performing set variable: '{name}'\nName:{name}\nType:{field.FieldType.FullName}\nValue:{value?.GetType().FullName}\nErrors:{ex.ToString()}", ex);
				}
			}

			public void SetVariable(object instance, string name, object value, char @operator) {
				if(fields.TryGetValue(name, out var field) == false) {
					if(@operator == '+' || @operator == '-') {
						if(events.TryGetValue(name, out var eventInfo)) {
							switch(@operator) {
								case '+':
									eventInfo.AddEventHandler(instance, value as Delegate);
									break;
								case '-':
									eventInfo.RemoveEventHandler(instance, value as Delegate);
									break;
							}
							return;
						}
					}
					throw new Exception($"Variable with name:{name} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				switch(@operator) {
					case '+':
					case '-':
					case '/':
					case '*':
					case '%':
						var val = field.GetValueOptimized(instance);
						value = uNodeHelper.ArithmeticOperator(val, value, @operator, field.FieldType, value?.GetType());
						break;
				}
				try {
					field.SetValueOptimized(instance, value);
				}
				catch(Exception ex) {
					throw new Exception($"Error on performing set variable: '{name}'\nName:{name}\nType:{field.FieldType.FullName}\nValue:{value?.GetType().FullName}\nErrors:{ex.ToString()}", ex);
				}
			}

			public object GetVariable(object instance, string name) {
				if(fields.TryGetValue(name, out var field) == false) {
					throw new Exception($"Variable with name:{name} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				return field.GetValueOptimized(instance);
			}

			public object GetProperty(object instance, string name) {
				if(properties.TryGetValue(name, out var property) == false) {
					throw new Exception($"Property with name:{name} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				return property.GetValueOptimized(instance);
			}

			public void SetProperty(object instance, string name, object value) {
				if(properties.TryGetValue(name, out var property) == false) {
					throw new Exception($"Property with name:{name} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				try {
					property.SetValueOptimized(instance, value);
				}
				catch(Exception ex) {
					throw new Exception($"Error on performing set property: '{name}'\nName:{name}\nType:{property.PropertyType.FullName}\nValue:{value?.GetType().FullName}\nErrors:{ex.ToString()}", ex);
				}
			}

			public void SetProperty(object instance, string name, object value, char @operator) {
				if(properties.TryGetValue(name, out var property) == false) {
					throw new Exception($"Property with name:{name} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				switch(@operator) {
					case '+':
					case '-':
					case '/':
					case '*':
					case '%':
						var val = property.GetValueOptimized(instance);
						value = uNodeHelper.ArithmeticOperator(val, value, @operator, property.PropertyType, value?.GetType());
						break;
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				try {
					property.SetValueOptimized(instance, value);
				}
				catch(Exception ex) {
					throw new Exception($"Error on performing set property: '{name}'\nName:{name}\nType:{property.PropertyType.FullName}\nValue:{value?.GetType().FullName}\nErrors:{ex.ToString()}", ex);
				}
			}

			public object InvokeFunction(object instance, string name, object[] values) {
				Type[] types = new Type[values != null ? values.Length : 0];
				if(values != null) {
					for(int i = 0; i < types.Length; i++) {
						types[i] = values[i] != null ? values[i].GetType() : typeof(object);
					}
					for(int i = 0; i < values.Length; i++) {
						values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
					}
				}
				if(methods.TryGetValue(new MethodSignature(name, types), out var func) == false) {
					throw new Exception($"Function with name:{name} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				try {
					return func.InvokeOptimized(instance, values);
				}
				catch(Exception ex) {
					throw new Exception($"Error on invoking function: '{name}'\nErrors:{ex.ToString()}", ex);
				}
			}

			public object InvokeFunction(object instance, string name, Type[] parameters, object[] values) {
				if(methods.TryGetValue(new MethodSignature(name, parameters), out var func) == false) {
					if(parameters == null) {
						parameters = Type.EmptyTypes;
					}
					var methods = instance.GetType().GetMethods();
					for(int i = 0; i < methods.Length; i++) {
						if(methods[i].Name == name) {
							var param = methods[i].GetParameters();
							if(param.Length == parameters.Length) {
								for(int y = 0; i < param.Length; y++) {
									if(param[y].ParameterType != parameters[y]) {
										if(parameters[y] is RuntimeType runtimeType && runtimeType.FullName == param[y].ParameterType.FullName) {
											continue;
										}
										goto CONTINUE;
									}
								}
								func = methods[i];
								goto SKIP;
							}
						}
					CONTINUE:
						continue;
					}
				SKIP:
					if(func == null)
						throw new Exception($"Function with name:{name} not found from type {instance.GetType().FullName}." +
							"\nIt may because of outdated generated script, try to generate the script again.");
				}
				if(values != null) {
					for(int i = 0; i < values.Length; i++) {
						values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
					}
				}
				try {
					return func.InvokeOptimized(instance, values);
				}
				catch(Exception ex) {
					throw new Exception($"Error on invoking function: '{name}'\nErrors:{ex.ToString()}", ex);
				}
			}
			#endregion
			#region ByID
			public void SetVariable(object instance, int id, object value) {
				if(fieldsID.TryGetValue(id, out var field) == false) {
					throw new Exception($"Variable with id:{id} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				try {
					field.SetValueOptimized(instance, value);
				}
				catch(Exception ex) {
					throw new Exception($"Error on performing set variable: '{id}'\nName:{id}\nType:{field.FieldType.FullName}\nValue:{value?.GetType().FullName}\nErrors:{ex.ToString()}", ex);
				}
			}

			public void SetVariable(object instance, int id, object value, char @operator) {
				if(fieldsID.TryGetValue(id, out var field) == false) {
					if(@operator == '+' || @operator == '-') {
						if(eventsID.TryGetValue(id, out var eventInfo)) {
							switch(@operator) {
								case '+':
									eventInfo.AddEventHandler(instance, value as Delegate);
									break;
								case '-':
									eventInfo.RemoveEventHandler(instance, value as Delegate);
									break;
							}
							return;
						}
					}
					throw new Exception($"Variable with id:{id} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				switch(@operator) {
					case '+':
					case '-':
					case '/':
					case '*':
					case '%':
						var val = field.GetValueOptimized(instance);
						value = uNodeHelper.ArithmeticOperator(val, value, @operator, field.FieldType, value?.GetType());
						break;
				}
				try {
					field.SetValueOptimized(instance, value);
				}
				catch(Exception ex) {
					throw new Exception($"Error on performing set variable: '{id}'\nName:{id}\nType:{field.FieldType.FullName}\nValue:{value?.GetType().FullName}\nErrors:{ex.ToString()}", ex);
				}
			}

			public object GetVariable(object instance, int id) {
				if(fieldsID.TryGetValue(id, out var field) == false) {
					throw new Exception($"Variable with id:{id} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				return field.GetValueOptimized(instance);
			}

			public object GetProperty(object instance, int id) {
				if(propertiesID.TryGetValue(id, out var property) == false) {
					throw new Exception($"Property with id:{id} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				return property.GetValueOptimized(instance);
			}

			public void SetProperty(object instance, int id, object value) {
				if(propertiesID.TryGetValue(id, out var property) == false) {
					throw new Exception($"Property with id:{id} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				try {
					property.SetValueOptimized(instance, value);
				}
				catch(Exception ex) {
					throw new Exception($"Error on performing set property: '{id}'\nName:{id}\nType:{property.PropertyType.FullName}\nValue:{value?.GetType().FullName}\nErrors:{ex.ToString()}", ex);
				}
			}

			public void SetProperty(object instance, int id, object value, char @operator) {
				if(propertiesID.TryGetValue(id, out var property) == false) {
					throw new Exception($"Property with name:{id} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				switch(@operator) {
					case '+':
					case '-':
					case '/':
					case '*':
					case '%':
						var val = property.GetValueOptimized(instance);
						value = uNodeHelper.ArithmeticOperator(val, value, @operator, property.PropertyType, value?.GetType());
						break;
				}
				value = uNodeHelper.GetActualRuntimeValue(value);
				try {
					property.SetValueOptimized(instance, value);
				}
				catch(Exception ex) {
					throw new Exception($"Error on performing set property: '{id}'\nName:{id}\nType:{property.PropertyType.FullName}\nValue:{value?.GetType().FullName}\nErrors:{ex.ToString()}", ex);
				}
			}

			public object InvokeFunction(object instance, int id, object[] values) {
				Type[] types = new Type[values != null ? values.Length : 0];
				if(values != null) {
					for(int i = 0; i < types.Length; i++) {
						types[i] = values[i] != null ? values[i].GetType() : typeof(object);
					}
					for(int i = 0; i < values.Length; i++) {
						values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
					}
				}
				if(methodsID.TryGetValue(id, out var func) == false) {
					throw new Exception($"Function with id:{id} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				try {
					return func.InvokeOptimized(instance, values);
				}
				catch(Exception ex) {
					throw new Exception($"Error on invoking function: '{id}'\nErrors:{ex.ToString()}", ex);
				}
			}
			#endregion
		}

		internal static class ObjectAccessor<T> {
			static ObjectAccessor m_instance;

			static ObjectAccessor() {
				m_instance = ObjectAccessor.GetInstance(typeof(T));
			}

			#region ByName
			public static void SetVariable(object instance, string name, object value) {
				m_instance.SetVariable(instance, name, value);
			}

			public static void SetVariable(object instance, string name, object value, char @operator) {
				m_instance.SetVariable(instance, name, value, @operator);
			}

			public static object GetVariable(object instance, string name) {
				return m_instance.GetVariable(instance, name);
			}

			public static object GetProperty(object instance, string name) {
				return m_instance.GetProperty(instance, name);
			}

			public static void SetProperty(object instance, string name, object value) {
				m_instance.SetProperty(instance, name, value);
			}

			public static void SetProperty(object instance, string name, object value, char @operator) {
				m_instance.SetProperty(instance, name, value, @operator);
			}

			public static object InvokeFunction(object instance, string name, object[] values) {
				return m_instance.InvokeFunction(instance, name, values);
			}

			public static object InvokeFunction(object instance, string name, Type[] parameters, object[] values) {
				return m_instance.InvokeFunction(instance, name, parameters, values);
			}
			#endregion
			#region ByID
			public static void SetVariable(object instance, int id, object value) {
				m_instance.SetVariable(instance, id, value);
			}

			public static void SetVariable(object instance, int id, object value, char @operator) {
				m_instance.SetVariable(instance, id, value);
			}

			public static object GetVariable(object instance, int id) {
				return m_instance.GetVariable(instance, id);
			}

			public static object GetProperty(object instance, int id) {
				return m_instance.GetProperty(instance, id);
			}

			public static void SetProperty(object instance, int id, object value) {
				m_instance.SetProperty(instance, id, value);
			}

			public static void SetProperty(object instance, int id, object value, char @operator) {
				m_instance.SetProperty(instance, id, value, @operator);
			}

			public static object InvokeFunction(object instance, int id, object[] values) {
				return m_instance.InvokeFunction(instance, id, values);
			}
			#endregion
		}

		/// <summary>
		/// Get graph asset from database by unique ID
		/// </summary>
		/// <param name="uid"></param>
		/// <returns></returns>
		public static GraphAsset GetGraphByID(string uid) {
			return uNodeDatabase.instance.GetGraphByUID(uid);
		}

		/// <summary>
		/// Get the actual runtime object
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static object GetActualRuntimeValue(object value) {
			if(value is IRuntimeClassContainer) {
				return (value as IRuntimeClassContainer).RuntimeClass;
			}
			return value;
		}

		/// <summary>
		/// Get UNode Graph Component
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="uniqueIdentifier"></param>
		/// <returns></returns>
		public static IRuntimeClass GetGraphComponent(GameObject gameObject, string uniqueIdentifier) {
			var graphs = gameObject.GetComponents<IRuntimeClass>();
			foreach(var graph in graphs) {
				if(graph.IsTypeOf(uniqueIdentifier)) {
					return graph;
				}
			}
			return null;
		}

		public static GraphInstance GetGraphInstance(object obj) {
			if(obj is IInstancedGraph instanced) {
				return instanced.Instance;
			}
			return obj as GraphInstance;
		}

		public static bool CompareRuntimeObject(IRuntimeClass x, IRuntimeClass y) {
			if(x == null && y == null)
				return true;
			if(uNodeUtility.isPlaying) {
				if(x is IRuntimeClassContainer) {
					if(y is IRuntimeClassContainer) {
						return x == y;
					}
					return object.ReferenceEquals((x as IRuntimeClassContainer).RuntimeClass, y);
				}
				else if(y is IRuntimeClassContainer) {
					return object.ReferenceEquals((y as IRuntimeClassContainer).RuntimeClass, x);
				}
			}
			if(x is UnityEngine.Object || y is UnityEngine.Object) {
				return (x as UnityEngine.Object) == (y as UnityEngine.Object);
			}
			return x == y;
		}

		#region Runtime Utils
		public static object GetVariable(IInstancedGraph obj, string name) {
			FieldInfo field;
			if(obj.Instance != null) {
				field = obj.GraphType.GetFieldCached(name);
			}
			else {
				field = obj.GetType().GetFieldCached(name);
			}
			if(field == null) {
				throw new Exception($"Variable with name:{name} not found from graph {obj.OriginalGraph}.");
			}
			return field.GetValueOptimized(obj);
		}

		public static void SetVariable(IInstancedGraph obj, string name, object value) {
			value = uNodeHelper.GetActualRuntimeValue(value);
			FieldInfo field;
			if(obj.Instance != null) {
				field = obj.GraphType.GetFieldCached(name);
			}
			else {
				field = obj.GetType().GetFieldCached(name);
			}
			if(field == null) {
				throw new Exception($"Variable with name:{name} not found from graph {obj.OriginalGraph}.");
			}
			field.SetValueOptimized(obj, value);
		}

		public static void SetVariable(IInstancedGraph obj, string name, object value, char @operator) {
			value = uNodeHelper.GetActualRuntimeValue(value);
			FieldInfo field;
			if(obj.Instance != null) {
				field = obj.GraphType.GetFieldCached(name);
			}
			else {
				field = obj.GetType().GetFieldCached(name);
			}
			if(field == null) {
				throw new Exception($"Variable with name:{name} not found from graph {obj.OriginalGraph}.");
			}
			switch(@operator) {
				case '+':
				case '-':
				case '/':
				case '*':
				case '%':
					var val = field.GetValueOptimized(obj);
					value = uNodeHelper.ArithmeticOperator(val, value, @operator, field.FieldType, value?.GetType());
					break;
			}
			field.SetValueOptimized(obj, value);
		}

		public static object GetProperty(IInstancedGraph obj, string name) {
			PropertyInfo property;
			if(obj.Instance != null) {
				property = obj.GraphType.GetPropertyCached(name);
			}
			else {
				property = obj.GetType().GetPropertyCached(name);
			}
			if(property == null) {
				throw new Exception($"Property with name:{name} not found from graph {obj.OriginalGraph}.");
			}
			return property.GetValueOptimized(obj);
		}

		public static void SetProperty(IInstancedGraph obj, string name, object value) {
			value = uNodeHelper.GetActualRuntimeValue(value);
			PropertyInfo property;
			if(obj.Instance != null) {
				property = obj.GraphType.GetPropertyCached(name);
			}
			else {
				property = obj.GetType().GetPropertyCached(name);
			}
			if(property == null) {
				throw new Exception($"Property with name:{name} not found from graph {obj.OriginalGraph}.");
			}
			property.SetValueOptimized(obj, value);
		}

		public static void SetProperty(IInstancedGraph obj, string name, object value, char @operator) {
			value = uNodeHelper.GetActualRuntimeValue(value);
			PropertyInfo property;
			if(obj.Instance != null) {
				property = obj.GraphType.GetPropertyCached(name);
			}
			else {
				property = obj.GetType().GetPropertyCached(name);
			}
			if(property == null) {
				throw new Exception($"Property with name:{name} not found from graph {obj.OriginalGraph}.");
			}
			switch(@operator) {
				case '+':
				case '-':
				case '/':
				case '*':
				case '%':
					var val = property.GetValue(obj);
					value = uNodeHelper.ArithmeticOperator(val, value, @operator, property.PropertyType, value?.GetType());
					break;
			}
			property.SetValueOptimized(obj, value);
		}

		public static object InvokeFunction(IInstancedGraph obj, string name, object[] values) {
			Type[] types = new Type[values != null ? values.Length : 0];
			if(values != null) {
				for(int i = 0; i < types.Length; i++) {
					types[i] = values[i] != null ? values[i].GetType() : typeof(object);
				}
				for(int i = 0; i < values.Length; i++) {
					values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
				}
			}
			return InvokeFunction(obj, name, types, values);
		}

		public static object InvokeFunction(IInstancedGraph obj, string name, Type[] parameters, object[] values) {
			if(values != null) {
				for(int i = 0; i < values.Length; i++) {
					values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
				}
			}
			if(obj.Instance != null) {
				var data = obj.OriginalGraph.GetFunction(name, parameters);
				if(data != null) {
					return data.Invoke(obj.Instance, values);
				}
			}
			MethodInfo func;
			if(obj.Instance != null) {
				func = obj.GraphType.GetMethod(name, parameters);
			}
			else {
				func = obj.GetType().GetMethod(name, parameters);
			}
			if(func == null) {
				throw new Exception($"Function with name:{name} not found from graph {obj.OriginalGraph}.");
			}
			return func.InvokeOptimized(obj, values);
		}
		#endregion

		#region GetGeneratedComponent
		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gameObject"></param>
		/// <returns></returns>
		public static T GetGeneratedComponent<T>(this GameObject gameObject) {
			var comps = gameObject.GetComponents(typeof(IRuntimeComponent));
			foreach(var c in comps) {
				if(c is T rezult) {
					return rezult;
				}
				else if(c is IRuntimeClassContainer) {
					var result = (c as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}


			//var uniqueIdentifier = typeof(T).FullName;
			//if(typeof(T).IsInterface) {
			//	uniqueIdentifier = "i:" + uniqueIdentifier;
			//}
			//object comp = GetGeneratedComponent(gameObject, uniqueIdentifier);
			//if(comp != null) {
			//	if(comp is T) {
			//		return (T)comp;
			//	}
			//	else if(comp is IRuntimeClassContainer) {
			//		var result = (comp as IRuntimeClassContainer).RuntimeClass;
			//		if(result is T) {
			//			return (T)result;
			//		}
			//	}
			//}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="component"></param>
		/// <returns></returns>
		public static T GetGeneratedComponent<T>(this Component component) {
			var comps = component.GetComponents(typeof(IRuntimeComponent));
			foreach(var c in comps) {
				if(c is T rezult) {
					return rezult;
				}
				else if(c is IRuntimeClassContainer) {
					var result = (c as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}

			//var uniqueIdentifier = typeof(T).FullName;
			//if(typeof(T).IsInterface) {
			//	uniqueIdentifier = "i:" + uniqueIdentifier;
			//}
			//object comp = GetGeneratedComponent(component, uniqueIdentifier);
			//if(comp != null) {
			//	if(comp is T) {
			//		return (T)comp;
			//	}
			//	else if(comp is IRuntimeClassContainer) {
			//		var result = (comp as IRuntimeClassContainer).RuntimeClass;
			//		if(result is T) {
			//			return (T)result;
			//		}
			//	}
			//}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponent(this GameObject gameObject, Type type) {
			var comps = gameObject.GetComponents<IRuntimeComponent>();
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <param name="component"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponent(this Component component, Type type) {
			var comps = component.GetComponents<IRuntimeComponent>();
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}

		public static bool TryGetGeneratedComponent(this Component component, Type type, out BaseRuntimeBehaviour comp) {
			comp = component.GetGeneratedComponent(type);
			return comp != null;
		}

		public static bool TryGetGeneratedComponent(this GameObject gameObject, Type type, out BaseRuntimeBehaviour comp) {
			comp = gameObject.GetGeneratedComponent(type);
			return comp != null;
		}

		public static bool TryGetGeneratedComponent(this Component component, string uniqueID, out BaseRuntimeBehaviour comp) {
			comp = component.GetGeneratedComponent(uniqueID);
			return comp != null;
		}

		public static bool TryGetGeneratedComponent(this GameObject gameObject, string uniqueID, out BaseRuntimeBehaviour comp) {
			comp = gameObject.GetGeneratedComponent(uniqueID);
			return comp != null;
		}

		public static bool TryGetGeneratedComponent<T>(this Component component, out T comp) {
			comp = component.GetGeneratedComponent<T>();
			return comp as UnityEngine.Object;
		}

		public static bool TryGetGeneratedComponent<T>(this GameObject gameObject, out T comp) {
			comp = gameObject.GetGeneratedComponent<T>();
			return comp as UnityEngine.Object;
		}

		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="uniqueID"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponent(this GameObject gameObject, string uniqueID) {
			var comps = gameObject.GetComponents<IRuntimeComponent>();
			if(uniqueID.StartsWith("i:", StringComparison.Ordinal)) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.IsTypeOf(uniqueID)) {
						return c as BaseRuntimeBehaviour;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as BaseRuntimeBehaviour;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.IsTypeOf(uniqueID)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <param name="component"></param>
		/// <param name="uniqueID"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponent(this Component component, string uniqueID) {
			var comps = component.GetComponents<IRuntimeComponent>();
			if(uniqueID.StartsWith("i:", StringComparison.Ordinal)) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.IsTypeOf(uniqueID)) {
						return c as BaseRuntimeBehaviour;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as BaseRuntimeBehaviour;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.IsTypeOf(uniqueID)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}
		#endregion

		#region GetGeneratedComponentInChildren
		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gameObject"></param>
		/// <returns></returns>
		public static T GetGeneratedComponentInChildren<T>(this GameObject gameObject, bool includeInactive = false) {
			var uniqueIdentifier = typeof(T).FullName;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponentInChildren(gameObject, uniqueIdentifier, includeInactive);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				}
				else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gameObject"></param>
		/// <returns></returns>
		public static T GetGeneratedComponentInChildren<T>(this Component component, bool includeInactive = false) {
			var uniqueIdentifier = typeof(T).FullName;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponentInChildren(component, uniqueIdentifier, includeInactive);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				}
				else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="type"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponentInChildren(this GameObject gameObject, Type type, bool includeInactive = false) {
			var comps = gameObject.GetComponentsInChildren<IRuntimeComponent>(includeInactive);
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}


		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <param name="component"></param>
		/// <param name="type"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponentInChildren(this Component component, Type type, bool includeInactive = false) {
			var comps = component.GetComponentsInChildren<IRuntimeComponent>(includeInactive);
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="uniqueID"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponentInChildren(this GameObject gameObject, string uniqueID, bool includeInactive = false) {
			var comps = gameObject.GetComponentsInChildren<IRuntimeComponent>(includeInactive);
			if(uniqueID.StartsWith("i:", StringComparison.Ordinal)) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.IsTypeOf(uniqueID)) {
						return c as BaseRuntimeBehaviour;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as BaseRuntimeBehaviour;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.IsTypeOf(uniqueID)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <param name="component"></param>
		/// <param name="uniqueID"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponentInChildren(this Component component, string uniqueID, bool includeInactive = false) {
			var comps = component.GetComponentsInChildren<IRuntimeComponent>(includeInactive);
			if(uniqueID.StartsWith("i:", StringComparison.Ordinal)) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.IsTypeOf(uniqueID)) {
						return c as BaseRuntimeBehaviour;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as BaseRuntimeBehaviour;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.IsTypeOf(uniqueID)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}
		#endregion

		#region GetGeneratedComponentInParent
		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gameObject"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static T GetGeneratedComponentInParent<T>(this GameObject gameObject, bool includeInactive = false) {
			var uniqueIdentifier = typeof(T).FullName;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponentInParent(gameObject, uniqueIdentifier, includeInactive);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				}
				else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="component"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static T GetGeneratedComponentInParent<T>(this Component component, bool includeInactive = false) {
			var uniqueIdentifier = typeof(T).FullName;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponentInParent(component, uniqueIdentifier, includeInactive);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				}
				else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="type"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponentInParent(this GameObject gameObject, Type type, bool includeInactive = false) {
			var comps = gameObject.GetComponentsInParent<IRuntimeComponent>(includeInactive);
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <param name="component"></param>
		/// <param name="type"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponentInParent(this Component component, Type type, bool includeInactive = false) {
			var comps = component.GetComponentsInParent<IRuntimeComponent>(includeInactive);
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="uniqueID"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponentInParent(this GameObject gameObject, string uniqueID, bool includeInactive = false) {
			var comps = gameObject.GetComponentsInParent<IRuntimeComponent>(includeInactive);
			if(uniqueID.StartsWith("i:", StringComparison.Ordinal)) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.IsTypeOf(uniqueID)) {
						return c as BaseRuntimeBehaviour;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as BaseRuntimeBehaviour;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.IsTypeOf(uniqueID)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <param name="component"></param>
		/// <param name="uniqueID"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static BaseRuntimeBehaviour GetGeneratedComponentInParent(this Component component, string uniqueID, bool includeInactive = false) {
			var comps = component.GetComponentsInParent<IRuntimeComponent>(includeInactive);
			if(uniqueID.StartsWith("i:", StringComparison.Ordinal)) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.IsTypeOf(uniqueID)) {
						return c as BaseRuntimeBehaviour;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as BaseRuntimeBehaviour;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.IsTypeOf(uniqueID)) {
					return c as BaseRuntimeBehaviour;
				}
			}
			return null;
		}
		#endregion

		/// <summary>
		/// GetComponentInParent including inactive object
		/// </summary>
		/// <param name="gameObject"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetComponentInParent<T>(GameObject gameObject) {
			if(gameObject == null) return default;
			return GetComponentInParent<T>(gameObject.transform);
		}

		/// <summary>
		/// GetComponentInParent including inactive object
		/// </summary>
		/// <param name="transform"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetComponentInParent<T>(Component component) {
			if(component == null) return default;
			Transform parent = component.transform;
			while(parent != null) {
				var comp = parent.GetComponent<T>();
				if(comp != null) {
					return comp;
				}
				parent = parent.parent;
			}
			return default;
		}

		/// <summary>
		/// Set value for the object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="value"></param>
		/// <param name="operator"></param>
		/// <returns></returns>
		public static T SetObject<T>(T obj, object value, char @operator) {
			switch(@operator) {
				case '+':
					if(value == null) {
						throw new ArgumentNullException(nameof(value));
					}
					value = Operator.Add(obj, value, typeof(T), value.GetType());
					break;
				case '-':
					if(value == null) {
						throw new ArgumentNullException(nameof(value));
					}
					value = Operator.Subtract(obj, value, typeof(T), value.GetType());
					break;
				case '/':
					if(value == null) {
						throw new ArgumentNullException(nameof(value));
					}
					value = Operator.Divide(obj, value, typeof(T), value.GetType());
					break;
				case '*':
					if(value == null) {
						throw new ArgumentNullException(nameof(value));
					}
					value = Operator.Multiply(obj, value, typeof(T), value.GetType());
					break;
				case '%':
					if(value == null) {
						throw new ArgumentNullException(nameof(value));
					}
					value = Operator.Modulo(obj, value, typeof(T), value.GetType());
					break;
			}
			if(value != null) {
				return (T)value;
			}
			return default;
		}

		/// <summary>
		/// Set value for the object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="reference"></param>
		/// <param name="value"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static T SetObject<T>(T reference, object value, SetType setType) {
			switch(setType) {
				case SetType.Change:
					return value != null ? (T)value : default;
				case SetType.Add:
					return (T)ArithmeticOperator(reference, value, ArithmeticType.Add);
				case SetType.Subtract:
					return (T)ArithmeticOperator(reference, value, ArithmeticType.Subtract);
				case SetType.Divide:
					return (T)ArithmeticOperator(reference, value, ArithmeticType.Divide);
				case SetType.Multiply:
					return (T)ArithmeticOperator(reference, value, ArithmeticType.Multiply);
			}
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Set value for the object.
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="value"></param>
		/// <param name="setType"></param>
		public static void SetObject(ref object reference, object value, SetType setType) {
			switch(setType) {
				case SetType.Change:
					reference = value;
					break;
				case SetType.Add:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Add);
					break;
				case SetType.Subtract:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Subtract);
					break;
				case SetType.Divide:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Divide);
					break;
				case SetType.Multiply:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Multiply);
					break;
			}
		}

		/// <summary>
		/// Set value for the object.
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="value"></param>
		/// <param name="setType"></param>
		/// <returns></returns>
		public static object SetObject(object reference, object value, SetType setType) {
			switch(setType) {
				case SetType.Change:
					reference = value;
					break;
				case SetType.Add:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Add);
					break;
				case SetType.Subtract:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Subtract);
					break;
				case SetType.Divide:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Divide);
					break;
				case SetType.Multiply:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Multiply);
					break;
			}
			return reference;
		}

		public static bool OperatorComparison(object a, object b, ComparisonType operatorType) {
			if(a != null && b != null) {
				if(a is Enum && b is Enum) {
					a = Operator.Convert(a, Enum.GetUnderlyingType(a.GetType()));
					b = Operator.Convert(b, Enum.GetUnderlyingType(b.GetType()));
				}
				switch(operatorType) {
					case ComparisonType.Equal:
						return Operator.Equal(a, b, a.GetType(), b.GetType());
					case ComparisonType.NotEqual:
						return Operator.NotEqual(a, b, a.GetType(), b.GetType());
					case ComparisonType.GreaterThan:
						return Operator.GreaterThan(a, b, a.GetType(), b.GetType());
					case ComparisonType.LessThan:
						return Operator.LessThan(a, b, a.GetType(), b.GetType());
					case ComparisonType.GreaterThanOrEqual:
						return Operator.GreaterThanOrEqual(a, b, a.GetType(), b.GetType());
					case ComparisonType.LessThanOrEqual:
						return Operator.LessThanOrEqual(a, b, a.GetType(), b.GetType());
					default:
						throw new System.InvalidCastException();
				}
			}
			else {
				switch(operatorType) {
					case ComparisonType.Equal:
						return Operator.Equal(a, b);
					case ComparisonType.NotEqual:
						return Operator.NotEqual(a, b);
					case ComparisonType.GreaterThan:
						return Operator.GreaterThan(a, b);
					case ComparisonType.LessThan:
						return Operator.LessThan(a, b);
					case ComparisonType.GreaterThanOrEqual:
						return Operator.GreaterThanOrEqual(a, b);
					case ComparisonType.LessThanOrEqual:
						return Operator.LessThanOrEqual(a, b);
					default:
						throw new System.InvalidCastException();
				}
			}
		}

		public static bool OperatorComparison(object a, object b, ComparisonType operatorType, Type aType, Type bType) {
			if(a is Enum && b is Enum) {
				a = Operator.Convert(a, Enum.GetUnderlyingType(a.GetType()));
				b = Operator.Convert(b, Enum.GetUnderlyingType(b.GetType()));
			}
			switch(operatorType) {
				case ComparisonType.Equal:
					return Operator.Equal(a, b, aType, bType);
				case ComparisonType.NotEqual:
					return Operator.NotEqual(a, b, aType, bType);
				case ComparisonType.GreaterThan:
					return Operator.GreaterThan(a, b, aType, bType);
				case ComparisonType.LessThan:
					return Operator.LessThan(a, b, aType, bType);
				case ComparisonType.GreaterThanOrEqual:
					return Operator.GreaterThanOrEqual(a, b, aType, bType);
				case ComparisonType.LessThanOrEqual:
					return Operator.LessThanOrEqual(a, b, aType, bType);
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object ShiftOperator(object a, int b, ShiftType operatorType) {
			switch(operatorType) {
				case ShiftType.LeftShift:
					return Operators.LeftShift(a, b, a.GetType());
				case ShiftType.RightShift:
					return Operators.RightShift(a, b, a.GetType());
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object BitwiseOperator(object a, object b, BitwiseType operatorType) {
			switch(operatorType) {
				case BitwiseType.And:
					return Operators.And(a, b);
				case BitwiseType.Or:
					return Operators.Or(a, b);
				case BitwiseType.ExclusiveOr:
					return Operators.ExclusiveOr(a, b);
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object ArithmeticOperator(object a, object b, ArithmeticType operatorType) {
			switch(operatorType) {
				case ArithmeticType.Add:
					return Operator.Add(a, b);
				case ArithmeticType.Subtract:
					return Operator.Subtract(a, b);
				case ArithmeticType.Divide:
					return Operator.Divide(a, b);
				case ArithmeticType.Multiply:
					return Operator.Multiply(a, b);
				case ArithmeticType.Modulo:
					return Operator.Modulo(a, b);
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object ArithmeticOperator(object a, object b, ArithmeticType operatorType, Type aType, Type bType) {
			if(aType == null) {
				aType = typeof(object);
			}
			if(bType == null) {
				bType = aType;
			}
			switch(operatorType) {
				case ArithmeticType.Add:
					return Operator.Add(a, b, aType, bType);
				case ArithmeticType.Subtract:
					return Operator.Subtract(a, b, aType, bType);
				case ArithmeticType.Divide:
					return Operator.Divide(a, b, aType, bType);
				case ArithmeticType.Multiply:
					return Operator.Multiply(a, b, aType, bType);
				case ArithmeticType.Modulo:
					return Operator.Modulo(a, b, aType, bType);
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object ArithmeticOperator(object a, object b, char operatorCode, Type aType, Type bType) {
			if(aType == null) {
				aType = a?.GetType() ?? bType ?? b?.GetType();
			}
			if(bType == null) {
				bType = aType;
			}
			switch(operatorCode) {
				case '+':
					return Operator.Add(a, b, aType, bType);
				case '-':
					return Operator.Subtract(a, b, aType, bType);
				case '/':
					return Operator.Divide(a, b, aType, bType);
				case '*':
					return Operator.Multiply(a, b, aType, bType);
				case '%':
					return Operator.Modulo(a, b, aType, bType);
				default:
					throw new System.InvalidCastException();
			}
		}

		class NestedRunners : IEnumerator {
			public List<IEnumerator> enumerators = new List<IEnumerator>();

			public NestedRunners() { }

			public NestedRunners(IEnumerator enumerator) {
				enumerators.Add(enumerator);
			}

			public object Current { get; private set; }

			public bool MoveNext() {
				var target = enumerators[enumerators.Count - 1];
				bool flag = target.MoveNext();
				Current = target.Current;
				if(flag) {
					if(Current is IEnumerator && !(Current is CustomYieldInstruction)) {
						enumerators.Add(Current as IEnumerator);
						return MoveNext();
					}
				}
				else {
					enumerators.RemoveAt(enumerators.Count - 1);
					if(enumerators.Count > 0) {
						return MoveNext();
					}
				}
				return enumerators.Count > 0;
			}

			public void Reset() {

			}
		}

		public static IEnumerator GetIteratorTargets(IEnumerable iterator) {
			var runner = new NestedRunners(iterator.GetEnumerator());
			while(runner.MoveNext()) {
				yield return runner.Current;
			}
			yield break;
		}

		public static IEnumerator GetIteratorTargets(IEnumerator iterator) {
			var runner = new NestedRunners(iterator);
			while(runner.MoveNext()) {
				yield return runner.Current;
			}
			yield break;
		}
	}
}