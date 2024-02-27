using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

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
				var field = instance.GetType().GetFieldCached(name);
				if(field == null) {
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

			public static void SetVariable(object instance, string name, object value, char @operator) {
				var field = instance.GetType().GetFieldCached(name);
				if(field == null) {
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

			public static object GetVariable(object instance, string name) {
				var field = instance.GetType().GetFieldCached(name);
				if(field == null) {
					throw new Exception($"Variable with name:{name} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				return field.GetValueOptimized(instance);
			}

			public static object GetProperty(object instance, string name) {
				var property = instance.GetType().GetPropertyCached(name);
				if(property == null) {
					throw new Exception($"Property with name:{name} not found from type {instance.GetType().FullName}." +
						"\nIt may because of outdated generated script, try to generate the script again.");
				}
				return property.GetValueOptimized(instance);
			}


			public static void SetProperty(object instance, string name, object value) {
				var property = instance.GetType().GetPropertyCached(name);
				if(property == null) {
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

			public static void SetProperty(object instance, string name, object value, char @operator) {
				var property = instance.GetType().GetPropertyCached(name);
				if(property == null) {
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

			public static object InvokeFunction(object instance, string name, object[] values) {
				Type[] types = new Type[values != null ? values.Length : 0];
				if(values != null) {
					for(int i = 0; i < types.Length; i++) {
						types[i] = values[i] != null ? values[i].GetType() : typeof(object);
					}
					for(int i = 0; i < values.Length; i++) {
						values[i] = uNodeHelper.GetActualRuntimeValue(values[i]);
					}
				}
				var func = instance.GetType().GetMethod(name, types);
				if(func == null) {
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

			public static object InvokeFunction(object instance, string name, Type[] parameters, object[] values) {
				//TODO: cache getmethod for performance
				var func = instance.GetType().GetMethod(name, parameters);
				if(func == null) {
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

		public static GraphAsset GetGraphByID(string uid) {
			return uNodeDatabase.instance.GetGraphByUID(uid);
		}

		/// <summary>
		/// Get the actual runtime object, if the target is uNodeSpawner then get the RuntimeBehaviour
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
			var uniqueIdentifier = typeof(T).Name;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponent(gameObject, uniqueIdentifier);
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
		/// Get Generated Class Component
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="component"></param>
		/// <returns></returns>
		public static T GetGeneratedComponent<T>(this Component component) {
			var uniqueIdentifier = typeof(T).Name;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponent(component, uniqueIdentifier);
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
			var uniqueIdentifier = typeof(T).Name;
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
			var uniqueIdentifier = typeof(T).Name;
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
			var uniqueIdentifier = typeof(T).Name;
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
			var uniqueIdentifier = typeof(T).Name;
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
	}
}