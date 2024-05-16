using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;

namespace MaxyGames.UNode {
	public static class ReflectionUtils {
		public static readonly BindingFlags publicFlags = BindingFlags.Public | BindingFlags.Instance;
		public static readonly BindingFlags publicAndNonPublicFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		public static readonly BindingFlags publicStaticFlags = BindingFlags.Public | BindingFlags.Static;
		public static readonly BindingFlags publicAndNonPublicStaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
		public static readonly BindingFlags publicAndNonPublicInstanceStaticFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

		private static Dictionary<Assembly, Type[]> assemblyTypeMap = new Dictionary<Assembly, Type[]>();
		private static Dictionary<Type, ConstructorInfo> defaultTypeConstructor = new Dictionary<Type, ConstructorInfo>();


		private static Assembly[] loadedAssemblies, assemblies;
		private static Assembly[] runtimeAssembly = new Assembly[0];
		private static HashSet<Assembly> loadedRuntimeAssemblies = new HashSet<Assembly>();

		/// <summary>
		/// Get the generated runtime assembly ( uNode compiled assembly )
		/// </summary>
		/// <returns></returns>
		public static Assembly[] GetRuntimeAssembly() {
			return runtimeAssembly;
		}

		internal static void RegisterPrivateLoadedAssembly(Assembly assembly) {
			if(assembly == null)
				return;
			loadedRuntimeAssemblies.Add(assembly);
		}

		/// <summary>
		/// Register a new Runtime Assembly
		/// </summary>
		/// <param name="assembly"></param>
		public static void RegisterRuntimeAssembly(Assembly assembly) {
			if(assembly == null)
				return;
			uNodeUtility.AddArrayAt(ref runtimeAssembly, assembly, 0);
			loadedRuntimeAssemblies.Add(assembly);
		}

		public static void CleanRuntimeAssembly() {
			runtimeAssembly = new Assembly[0];
		}

		public static IEnumerable<Assembly> GetAssemblies() {
			if(loadedAssemblies == null) {
				List<Assembly> ass = AppDomain.CurrentDomain.GetAssemblies().ToList();
				for(int i = 0; i < ass.Count; i++) {
					try {
						if(loadedRuntimeAssemblies.Contains(ass[i])) {
							ass.RemoveAt(i);
							i--;
						}
					}
					catch {
						ass.RemoveAt(i);
						i--;
					}
				}
				loadedAssemblies = ass.ToArray();
			}
			return loadedAssemblies;
		}

		public static void UpdateAssemblies() {
			loadedAssemblies = null;
			TypeSerializer.CleanCache();
		}

		/// <summary>
		/// Get the available static assembly
		/// </summary>
		/// <returns></returns>
		public static Assembly[] GetStaticAssemblies() {
			if(assemblies == null) {
				List<Assembly> ass = AppDomain.CurrentDomain.GetAssemblies().ToList();
				for(int i = 0; i < ass.Count; i++) {
					try {
						if(/*string.IsNullOrEmpty(ass[i].Location) ||*/ass[i].IsDynamic || loadedRuntimeAssemblies.Contains(ass[i])) {
							ass.RemoveAt(i);
							i--;
						}
					}
					catch {
						ass.RemoveAt(i);
						i--;
					}
				}
				assemblies = ass.ToArray();
			}
			return assemblies;
		}

		public static IEnumerable<T> GetAssemblyAttributes<T>() where T : Attribute {
			foreach(var ass in GetStaticAssemblies()) {
				var atts = GetAssemblyAttribute<T>(ass);
				if(atts != null) {
					foreach(var a in atts) {
						yield return a;
					}
				}
			}
		}

		public static IEnumerable<T> GetAssemblyAttribute<T>(Assembly assembly) where T : Attribute {
			if(assembly.IsDefined(typeof(T), true)) {
				return assembly.GetCustomAttributes<T>();
			}
			return Array.Empty<T>();
		}

		public static IEnumerable<Attribute> GetAssemblyAttributes(Type attributeType) {
			foreach(var ass in GetStaticAssemblies()) {
				var atts = GetAssemblyAttribute(attributeType, ass);
				if(atts != null) {
					foreach(var a in atts) {
						yield return a;
					}
				}
			}
		}

		public static IEnumerable<Attribute> GetAssemblyAttribute(Type attributeType, Assembly assembly) {
			if(assembly.IsDefined(attributeType, true)) {
				return assembly.GetCustomAttributes(attributeType);
			}
			return Array.Empty<Attribute>();
		}

		public static Type[] GetAssemblyTypes(Assembly assembly) {
			Type[] types;
			if(!assemblyTypeMap.TryGetValue(assembly, out types)) {
				try {
					types = assembly.GetTypes();
					assemblyTypeMap[assembly] = types;
				}
				catch {
					Debug.LogError("Error on getting type from assembly:" + assembly.FullName);
				}
			}
			//Type[] result = new Type[types.Length];
			//Array.Copy(types, result, types.Length);
			return types;
		}

		public static Type GetRuntimeType(IReflectionType obj) {
			return obj.ReflectionType;
		}

		/// <summary>
		/// Get runtime type.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static Type GetRuntimeType(object obj) {
			if(obj is IReflectionType reflection) {
				return reflection.ReflectionType;
			}
			else if(obj is IInstancedGraph instance) {
				if(instance.OriginalGraph == obj) {
					//Prevent stack overflow
					return null;
				}
				return GetRuntimeType(instance.OriginalGraph);
			}
			else if(obj is IRuntimeClass) {
				string uid = (obj as IRuntimeClass).uniqueIdentifier;
				return TypeSerializer.GetRuntimeType(uid);
			}
			else {
				if(obj is Graph graph) {
					return GetRuntimeType(graph.graphContainer);
				}
				else if(obj is Type type && type is not RuntimeType) {
					if(uNodeDatabase.nativeGraphTypes.Contains(type)) {
						var typeName = type.FullName;
						foreach(var data in uNodeDatabase.instance?.graphDatabases) {
							if(data.asset != null && data.asset.GetFullGraphName() == typeName) {
								return GetRuntimeType(data.asset);
							}
						}
					}
				}
				//throw new Exception("Unsupported to get runtime type on: " + obj + "\nValue Type: " + obj.GetType());
				return null;
			}
		}

		public static Type GetRuntimeType(object obj, bool throwOnNull) {
			if(obj is IReflectionType reflection) {
				return reflection.ReflectionType;
			}
			else if(obj is IInstancedGraph instance) {
				if(instance.OriginalGraph == obj) {
					//Prevent stack overflow
					return null;
				}
				return GetRuntimeType(instance.OriginalGraph);
			}
			else if(obj is IRuntimeClass) {
				string uid = (obj as IRuntimeClass).uniqueIdentifier;
				var type = TypeSerializer.GetRuntimeType(uid);
				if(type == null && throwOnNull) {
					throw new Exception($"No graph with name: {uid} found, maybe the graph was removed or database is outdated.");
				}
				return type;
			}
			else {
				if(obj is Graph graph) {
					return GetRuntimeType(graph.graphContainer, throwOnNull);
				}
				else if(obj is Type type && type is not RuntimeType) {
					if(uNodeDatabase.nativeGraphTypes.Contains(type)) {
						var typeName = type.FullName;
						foreach(var data in uNodeDatabase.instance?.graphDatabases) {
							if(data.asset.GetFullGraphName() == typeName) {
								return GetRuntimeType(data.asset, throwOnNull);
							}
						}
					}
				}
				if(throwOnNull)
					throw new Exception("Unsupported to get runtime type on: " + obj + "\nValue Type: " + obj.GetType());
				return null;
			}
		}

		/// <summary>
		/// Is type equal, it is same with using `==` operator but also support compare Runtime Type with CLR type.
		/// </summary>
		/// <remarks>
		/// Compare 2 type that are equal, also support comparing Runtime Type with CLR type.
		/// If one of the <paramref name="source"/> or <paramref name="destination"/> is Runtime Type that implement the <see cref="INativeType"/>
		/// and the other is CLR type, it will try convert the Runtime Type to CLR type and compare it with the other.
		/// </remarks>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <returns></returns>
		public static bool IsTypeEqual(Type source, Type destination) {
			if(source == null || destination == null)
				return source == destination;
			if(source is RuntimeType) {
				if(destination is RuntimeType) {
					return source == destination;
				}
				else {
					if(IsNativeType(source)) {
						return GetNativeType(source) == destination;
					}
				}
				return false;
			}
			else if(destination is RuntimeType) {
				if(IsNativeType(destination)) {
					return source == GetNativeType(destination);
				}
				return false;
			}
			return source == destination;
		}

		internal static bool IsCastableTo(IRuntimeClass instance, RuntimeType type, bool throwError = true) {
			var targetType = GetRuntimeType(instance);
			if(targetType == null && throwError) {
				throw new Exception($"No graph with name: {instance.uniqueIdentifier} found, maybe the graph was removed or database is outdated.");
			}
			return targetType.IsCastableTo(type);
		}

		internal static void ClearInvalidRuntimeType() {
			//TODO: clear invalid runtime type.

		}

		/// <summary>
		/// Is the 'instance' is a valid instance of type 'type'
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsValidRuntimeInstance(object instance, RuntimeType type) {
			return type.IsInstanceOfType(instance);
			// if (type is RuntimeGraphType graphType) {
			// 	if (graphType.target is IClassComponent component) {
			// 		if (instance is IRuntimeComponent runtime) {
			// 			return component.uniqueIdentifier == runtime.uniqueIdentifier;
			// 		}
			// 	} else if (graphType.target is IClassAsset asset) {
			// 		if (instance is IRuntimeAsset runtime) {
			// 			return asset.uniqueIdentifier == runtime.uniqueIdentifier;
			// 		}
			// 	}
			// } else {
			// 	return type.IsInstanceOfType(instance);
			// }
			// return false;
		}

		public static Type MakeByRefType(Type type) {
			return type.MakeByRefType();
		}

		public static Type MakeArrayType(Type type) {
			if(type is IRuntimeMember) {
				return ReflectionFaker.FakeArrayType(type);
			}
			else {
				return type.MakeArrayType();
			}
		}

		public static Type MakeGenericType(Type type, params Type[] typeArguments) {
			if(typeArguments == null) {
				throw new ArgumentNullException(nameof(typeArguments));
			}
			bool flag = type is RuntimeType;
			if(!flag) {
				for(int i=0; i< typeArguments.Length;i++) {
					if(typeArguments[i] is RuntimeType) {
						flag = true;
						break;
					}
				}
			}
			if(flag) {
				return ReflectionFaker.FakeGenericType(type, typeArguments);
			} else {
				return type.MakeGenericType(typeArguments);
			}
		}

		public static MethodInfo MakeGenericMethod(MethodInfo method, params Type[] typeArguments) {
			if(typeArguments == null) {
				throw new ArgumentNullException(nameof(typeArguments));
			}
			bool flag = false;
			if(method is not IRuntimeMember) {
				for(int i = 0; i < typeArguments.Length; i++) {
					if(typeArguments[i] is RuntimeType) {
						flag = true;
						break;
					}
				}
			}
			if(flag) {
				return ReflectionFaker.FakeGenericMethod(method, typeArguments);
			}
			else {
				return method.MakeGenericMethod(typeArguments);
			}
		}

		/// <summary>
		/// Get the actual type from instance
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static Type GetActualTypeFromInstance(object instance, bool useStartType = false) {
			if (instance == null) return null;
			if (instance is MemberData) {
				var mData = instance as MemberData;
				if (!useStartType) {
					return mData.type;
				}
				if (mData.IsTargetingGraph) {
					return GetActualTypeFromInstance(mData.startType);
				}
				if (mData.isTargeted) {
					return GetActualTypeFromInstance(mData.startTarget);
				}
			} else if (instance is IReflectionType) {
				return GetRuntimeType(instance);
			} else if (instance is RuntimeType) {
				return instance as RuntimeType;
			} else if (instance is RuntimeField) {
				return (instance as RuntimeField).owner;
			} else if (instance is RuntimeProperty) {
				return (instance as RuntimeProperty).owner;
			} else if (instance is RuntimeMethod) {
				return (instance as RuntimeMethod).owner;
			} else if(instance is ValueInput) {
				return (instance as ValueInput).type;
			}
			//else if (instance is uNodeSpawner) {
			//	return GetRuntimeType((instance as uNodeSpawner).target);
			//} else if (instance is uNodeAssetInstance) {
			//	return GetRuntimeType((instance as uNodeAssetInstance).target);
			//} else if(instance is Node) {
			//	return (instance as Node).ReturnType();
			//} else if(instance is RootObject) {
			//	return (instance as RootObject).ReturnType();
			//}
			return instance.GetType();
		}

		/// <summary>
		/// Get specific runtime type from instance of a graph / object
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static Type GetRuntimeTypeFromInstance(object instance, bool useStartType = false) {
			if (instance is MemberData) {
				var mData = instance as MemberData;
				if (!useStartType) {
					return mData.type as RuntimeType;
				}
				if (mData.IsTargetingGraph) {
					return GetRuntimeTypeFromInstance(mData.startType);
				}
				if (mData.isTargeted) {
					return GetRuntimeTypeFromInstance(mData.startTarget);
				}
			} else if (instance is GraphAsset) {
				return GetRuntimeType(instance);
			} else if (instance is RuntimeType) {
				return instance as RuntimeType;
			} else if (instance is RuntimeField) {
				return (instance as RuntimeField).owner;
			} else if (instance is RuntimeProperty) {
				return (instance as RuntimeProperty).owner;
			} else if (instance is RuntimeMethod) {
				return (instance as RuntimeMethod).owner;
			} else {
				//TODO: ValueInput
			}
			//else if (instance is uNodeSpawner) {
			//	return GetRuntimeType((instance as uNodeSpawner).target);
			//} else if (instance is uNodeAssetInstance) {
			//	return GetRuntimeType((instance as uNodeAssetInstance).target);
			//}
			return null;
		}

		/// <summary>
		/// Is the member targeting runtime type?
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static bool HasRuntimeType(MemberData member) {
			if (member == null) return false;
			if (member.targetType == MemberData.TargetType.uNodeType) {
				return true;
			}
			var instance = member.instance;
			if (instance is MemberData m) {
				return HasRuntimeType(m);
			}
			return false;
		}

		/// <summary>
		/// Is the member targeting runtime type?
		/// </summary>
		/// <param name="members"></param>
		/// <returns></returns>
		public static bool HasRuntimeType(params MemberData[] members) {
			if (members == null) return false;
			foreach (var m in members) {
				if (HasRuntimeType(m)) {
					return true;
				}
			}
			return false;
		}

#region Get Members
		/// <summary>
		/// Gets the members (properties, methods, fields, events, and so on) of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static IEnumerable<MemberInfo> GetMembers(Type type, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			foreach(var member in type.GetMember(name, flags)) {
				yield return member;
			}
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					foreach(var member in GetMembers(iface, name, flags | BindingFlags.DeclaredOnly)) {
						yield return member;
					}
				}
			}
		}

		/// <summary>
		/// Gets the members (properties, methods, fields, events, and so on) of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="validation"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static IEnumerable<MemberInfo> GetMembers(Type type, string name, Func<MemberInfo, bool> validation, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			foreach(var member in type.GetMember(name, flags)) {
				if(validation(member)) {
					yield return member;
				}
			}
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					foreach(var member in GetMembers(iface, name, validation, flags | BindingFlags.DeclaredOnly)) {
						if(validation(member)) {
							yield return member;
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets the members (properties, methods, fields, events, and so on) of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static IEnumerable<MemberInfo> GetMembers(Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			foreach(var member in type.GetMembers(flags)) {
				yield return member;
			}
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					foreach(var member in GetMembers(iface, flags | BindingFlags.DeclaredOnly)) {
						yield return member;
					}
				}
			}
		}

		/// <summary>
		/// Gets the members (properties, methods, fields, events, and so on) of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="validation"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static IEnumerable<MemberInfo> GetMembers(Type type, Func<MemberInfo, bool> validation, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			foreach(var member in type.GetMembers(flags)) {
				if(validation(member)) {
					yield return member;
				}
			}
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					foreach(var member in GetMembers(iface, validation, flags | BindingFlags.DeclaredOnly)) {
						if(validation(member)) {
							yield return member;
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets the methods of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static IEnumerable<MethodInfo> GetMethods(Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			foreach(var member in type.GetMethods(flags)) {
				yield return member;
			}
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					foreach(var member in GetMethods(iface, flags | BindingFlags.DeclaredOnly)) {
						yield return member;
					}
				}
			}
		}

		/// <summary>
		/// Gets the methods of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="validation"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static IEnumerable<MethodInfo> GetMethods(Type type, Func<MemberInfo, bool> validation, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			foreach(var member in type.GetMethods(flags)) {
				if(validation(member)) {
					yield return member;
				}
			}
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					foreach(var member in GetMethods(iface, validation, flags | BindingFlags.DeclaredOnly)) {
						if(validation(member)) {
							yield return member;
						}
					}
				}
			}
		}

		/// <summary>
		/// Get specific member info of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static MemberInfo GetMember(Type type, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			var member = type.GetMember(name, flags);
			if(member.Length > 0) {
				return member[0];
			}
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					member = iface.GetMember(name, flags);
					if(member.Length > 0) {
						return member[0];
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Gets a specific method of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static MethodInfo GetMethod(Type type, string name, params Type[] types) {
			var member = type.GetMethod(name, types);
			if(member != null)
				return member;
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					member = iface.GetMethod(name, types);
					if(member != null) {
						return member;
					}
				}
			}
			return member;
		}

		/// <summary>
		/// Gets a specific method of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static MethodInfo GetMethod(Type type, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			var member = type.GetMethod(name, flags);
			if(member != null)
				return member;
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					member = iface.GetMethod(name, flags);
					if(member != null) {
						return member;
					}
				}
			}
			return member;
		}

		/// <summary>
		/// Gets a specific method of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static MethodInfo GetMethod(Type type, string name, Type[] types, BindingFlags flags, Binder binder = null, ParameterModifier[] modifiers = null) {
			var member = type.GetMethod(name, flags, binder, types, modifiers);
			if(member != null)
				return member;
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					member = iface.GetMethod(name, flags, binder, types, modifiers);
					if(member != null) {
						return member;
					}
				}
			}
			return member;
		}

		/// <summary>
		/// Gets the properties of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static IEnumerable<PropertyInfo> GetProperties(Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			foreach(var member in type.GetProperties(flags)) {
				yield return member;
			}
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					foreach(var member in GetProperties(iface, flags | BindingFlags.DeclaredOnly)) {
						yield return member;
					}
				}
			}
		}

		/// <summary>
		/// Gets a specific property of the current Type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static PropertyInfo GetProperty(Type type, string name, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) {
			var member = type.GetProperty(name, flags);
			if(member != null)
				return member;
			if(type.IsInterface) {
				foreach(var iface in type.GetInterfaces()) {
					member = iface.GetProperty(name, flags);
					if(member != null) {
						return member;
					}
				}
			}
			return member;
		}
#endregion

		public static FieldInfo FindFieldInfo(Type parentType, string path) {
			FieldInfo fInfo = null;
			string[] strArray = path.Split('.');
			for (int i = 0; i < strArray.Length; i++) {
				fInfo = parentType.GetField(strArray[i]);
				parentType = fInfo.FieldType;
			}
			return fInfo;
		}

		public static object GetFieldValue(object start, string path) {
			if (start == null)
				return null;
			object parent = start;
			Type t = parent.GetType();
			FieldInfo fInfo = null;
			string[] strArray = path.Split('.');
			for (int i = 0; i < strArray.Length; i++) {
				fInfo = t.GetField(strArray[i]);
				if (fInfo == null)
					throw new NullReferenceException("could not find field in path : " + path + "\nType:" + t.FullName);
				t = fInfo.FieldType;
				if (parent != null) {
					parent = fInfo.GetValueOptimized(parent);
				} else {
					return null;
				}
			}
			return parent;
		}

		public static object GetFieldValue(object start, string path, BindingFlags flags) {
			if (start == null)
				return null;
			object parent = start;
			Type t = parent.GetType();
			FieldInfo fInfo = null;
			string[] strArray = path.Split('.');
			for (int i = 0; i < strArray.Length; i++) {
				fInfo = t.GetField(strArray[i], flags);
				if (fInfo == null)
					throw new NullReferenceException("could not find field in path : " + path + "\nType:" + t.FullName);
				t = fInfo.FieldType;
				if (parent != null) {
					parent = fInfo.GetValueOptimized(parent);
				} else {
					return null;
				}
			}
			return parent;
		}

		public static object GetPropertyValue(object start, string path, BindingFlags flags) {
			if (start == null)
				return null;
			object parent = start;
			Type t = parent.GetType();
			PropertyInfo info = null;
			string[] strArray = path.Split('.');
			for (int i = 0; i < strArray.Length; i++) {
				info = t.GetProperty(strArray[i], flags);
				if (info == null)
					throw new NullReferenceException("could not find property in path : " + path + "\nType:" + t.FullName);
				t = info.PropertyType;
				if (parent != null) {
					parent = info.GetValueOptimized(parent);
				} else {
					return null;
				}
			}
			return parent;
		}

		public static void SetPropertyValue(object start, string name, object value, BindingFlags flags) {
			if (start == null)
				return;
			object parent = start;
			Type t = parent.GetType();
			var info = t.GetProperty(name, flags);
			if (info == null)
				throw new NullReferenceException("could not find property in path : " + name + "\nType:" + t.FullName);
			info.SetValueOptimized(parent, value);
		}

		/// <summary>
		/// Convert Delegate type to another
		/// </summary>
		/// <param name="src"></param>
		/// <param name="targetType"></param>
		/// <param name="doTypeCheck"></param>
		/// <returns></returns>
		public static Delegate ConvertDelegate(Delegate src, Type targetType, bool doTypeCheck = true) {
			//Is it null or of the same type as the target?
			if (src == null || src.GetType() == targetType)
				return src;
			//Is it multiple cast?
			return src.GetInvocationList().Count() == 1
				? Delegate.CreateDelegate(targetType, src.Target, src.Method, doTypeCheck)
				: src.GetInvocationList().Aggregate<Delegate, Delegate>
					(null, (current, d) => Delegate.Combine(current, ConvertDelegate(d, targetType, doTypeCheck)));
		}

		/// <summary>
		/// Is the type is Native type ( from c# or type that implement <see cref="INativeMember"/> )
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsNativeType(Type type) {
			if(type is IRuntimeMember) {
				if(type is INativeMember) {
					return true;
				}
				else if(type is IFakeType) {
					if(type is GenericFakeType GFT) {
						return GFT.IsOnlyNativeTypes();
					}
					else if(type.IsArray) {
						return IsNativeType(type.GetElementType());
					}
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Is the member is Native member ( from c# or type that implement <see cref="INativeMember"/> )
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static bool IsNativeMember(MemberInfo member) {
			if(member is IRuntimeMember) {
				if(member is IFakeMember) {
					if(member is MethodBase method) {
						var args = method.GetGenericArguments();
						for(int i = 0; i < args.Length; i++) {
							if(IsNativeType(args[i]) == false)
								return false;
						}
						var parameters = method.GetParameters();
						for(int i = 0; i < parameters.Length; i++) {
							if(IsNativeType(parameters[i].ParameterType) == false)
								return false;
						}
					}
				}
				if(member is INativeMember) {
					return true;
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Get the actual native c# type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns>Return the actual c# type</returns>
		public static Type GetNativeType(Type type) {
			if(type is IRuntimeMember) {
				if(type is INativeMember native) {
					return native.GetNativeMember() as Type;
				}
				else if(type is IFakeType fakeType) {
					return fakeType.GetNativeType();
				}
				else if(type is IRuntimeType runtimeType) {
					return runtimeType.GetNativeType();
				}
				return null;
			}
			return type;
		}

		public static Type GetNativeType(Type type, bool throwOnError) {
			if(type is IRuntimeMember) {
				if(type is INativeMember native) {
					var result = native.GetNativeMember() as Type;
					if(result != null) {
						return result;
					}
					if(throwOnError)
						throw new Exception("Couldn't resolve type: " + type + "\nMaybe it still not compiled, renamed or wrong names.");
					else
						return null;
				}
				else if(type is IFakeType fakeType) {
					var result = fakeType.GetNativeType() as Type;
					if(result != null) {
						return result;
					}
				}
				else if(type is IRuntimeType runtimeType) {
					var result = runtimeType.GetNativeType();
					if(result != null) {
						return result;
					}
				}
				if(throwOnError)
					throw new Exception("Couldn't get native type for: " + type);
				else
					return null;
			}
			return type;
		}

		/// <summary>
		/// Is the target null or default.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsNullOrDefault(object target, Type type = null) {
			if (target != null) {
				if (target is UnityEngine.Object) {
					return false;
				}
				if (type == null) {
					type = target.GetType();
				}
				var CInfo = GetDefaultConstructor(type);
				if(CInfo != null) {
					object obj = CreateInstance(type);
					return obj.Equals(target);
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Are the type can be create a new instance.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool CanCreateInstance(Type type) {
			if (type == typeof(string) || type.IsPrimitive || type.IsValueType && type != typeof(void))
				return true;
			if (type.IsInterface || type.IsAbstract || type.ContainsGenericParameters)
				return false;
			if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
				return false;
			}
			if (type.IsClass) {
				ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
				if (ctor == null) {
					return type.IsArray;
				}
			}
			return true;
		}

		public static ConstructorInfo GetDefaultConstructor(Type type) {
			ConstructorInfo result = type.GetConstructor(publicAndNonPublicFlags, null, Type.EmptyTypes, null);
			//try {
			//	result = type.GetConstructor(publicAndNonPublicFlags, null, Type.EmptyTypes, null);
			//}
			//catch(AmbiguousMatchException) {
			//	result = type.GetConstructor(publicAndNonPublicFlags, null, Type.EmptyTypes, null);
			//}
			if(result != null) {
				return result;
			}
			else {
				//var ctors = type.GetConstructors(ReflectionUtils.publicAndNonPublicStaticFlags);
				//foreach(var ctor in ctors) {
				//	if(ctor.GetParameters().Length == 0) {
				//		return ctor;
				//	}
				//}
				if(type.IsValueType) {
					if(!defaultTypeConstructor.TryGetValue(type, out result)) {
						result = new RuntimeDefaultConstructor(type);
						defaultTypeConstructor[type] = result;
					}
					return result;
				}
				return null;
			}
		}

		/// <summary>
		/// Create an instance of type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static object CreateInstance(Type type, params object[] args) {
			if(type == null) return null;
			if (type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object))) {
				return null;
			}
			if (type == typeof(string) || type == typeof(object)) {
				return "";
			} else if (type == typeof(bool)) {
				return default(bool);
			} else if (type == typeof(float)) {
				return default(float);
			} else if (type == typeof(int)) {
				return default(int);
			}
			if (type.IsArray) {
				Array array = Array.CreateInstance(type.GetElementType(), args.Length);
				if (args.Length > 0) {
					for (int i = 0; i < args.Length; i++) {
						array.SetValue(args[i], i);
					}
				}
				return array;
			}
			if (type.IsAbstract) {
				return null;
			}
			if (type.IsClass) {
				ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
				if (ctor == null) {
					if (args.Length > 0) {
						return System.Activator.CreateInstance(type, args);
					}
					return null;
				}
			}
			if(type is RuntimeType) {
				if(type is FakeType) {
					return CreateInstance((type as FakeType).target, args);
				}
				else if(type is INativeMember nativeMember) {
					type = nativeMember.GetNativeMember() as Type;
					return CreateInstance(type);
				}
				return null;
			}
			return Activator.CreateInstance(type, args);
		}

		private static Dictionary<Type, FieldInfo[]> _fieldMap;
		/// <summary>
		/// Get instance field public and non public
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static FieldInfo[] GetFieldsCached(Type type) {
			if(_fieldMap == null)
				_fieldMap = new Dictionary<Type, FieldInfo[]>();
			if(!_fieldMap.TryGetValue(type, out var members)) {
				members = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
				_fieldMap[type] = members;
			}
			return members;
		}

		private static Dictionary<Type, MemberInfo[]> _instanceMemberMap;
		public static MemberInfo[] GetMembersCached(Type type) {
			if(_instanceMemberMap == null)
				_instanceMemberMap = new Dictionary<Type, MemberInfo[]>();
			if(!_instanceMemberMap.TryGetValue(type, out var members)) {
				members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
				_instanceMemberMap[type] = members;
			}
			return members;
		}

		private static Dictionary<Type, MemberInfo[]> _staticMemberMap;
		public static MemberInfo[] GetStaticMembersCached(Type type) {
			if(_staticMemberMap == null)
				_staticMemberMap = new Dictionary<Type, MemberInfo[]>();
			if(!_staticMemberMap.TryGetValue(type, out var members)) {
				members = type.GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
				_staticMemberMap[type] = members;
			}
			return members;
		}

		private static Dictionary<Type, MemberInfo[]> _fieldAndPropertiesMap;
		/// <summary>
		/// Get instance field and property: public and non public
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static MemberInfo[] GetFieldAndPropertiesCached(Type type) {
			if(_fieldAndPropertiesMap == null)
				_fieldAndPropertiesMap = new Dictionary<Type, MemberInfo[]>();
			if(!_fieldAndPropertiesMap.TryGetValue(type, out var members)) {
				members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(m => m.MemberType.HasFlags(MemberTypes.Field | MemberTypes.Property)).ToArray();
				_fieldAndPropertiesMap[type] = members;
			}
			return members;
		}

		public static FieldInfo[] GetFieldsFromType(Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) {
			return type.GetFields(flags);
		}

		public static FieldInfo[] GetFields(object obj, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) {
			return obj.GetType().GetFields(flags);
		}

		public static PropertyInfo[] GetProperties(object obj, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance) {
			return obj.GetType().GetProperties(flags);
		}

		public static bool IsPublicMember(MemberInfo member) {
			switch(member.MemberType) {
				case MemberTypes.Constructor:
					var ctor = member as ConstructorInfo;
					return ctor.IsPublic;
				case MemberTypes.Event:
					var evt = member as EventInfo;
					return evt.AddMethod != null ? evt.AddMethod.IsPublic : evt.RemoveMethod.IsPublic;
				case MemberTypes.Field:
					var field = member as FieldInfo;
					return field.IsPublic;
				case MemberTypes.Property:
					var prop = member as PropertyInfo;
					return prop.GetMethod != null && prop.GetMethod.IsPublic || prop.SetMethod != null && prop.SetMethod.IsPublic;
				case MemberTypes.Method:
					var method = member as MethodInfo;
					return method.IsPublic;
				case MemberTypes.TypeInfo:
					var type = member as Type;
					return type.IsPublic;
			}
			return false;
		}

		public static bool IsProtectedMember(MemberInfo member) {
			switch(member.MemberType) {
				case MemberTypes.Constructor:
					var ctor = member as ConstructorInfo;
					return ctor.IsFamily;
				case MemberTypes.Event:
					var evt = member as EventInfo;
					return evt.AddMethod != null ? evt.AddMethod.IsFamily : evt.RemoveMethod.IsFamily;
				case MemberTypes.Field:
					var field = member as FieldInfo;
					return field.IsFamily;
				case MemberTypes.Property:
					var prop = member as PropertyInfo;
					return prop.GetMethod != null && prop.GetMethod.IsFamily || prop.SetMethod != null && prop.SetMethod.IsFamily;
				case MemberTypes.Method:
					var method = member as MethodInfo;
					return method.IsFamily /*&& !method.Attributes.HasFlags(MethodAttributes.VtableLayoutMask)*/;
				case MemberTypes.TypeInfo:
					var type = member as Type;
					return type.IsNestedFamily;
			}
			return false;
		}

		public static bool CanSetMember(MemberInfo member) {
			if(!CanSetMemberValue(member)) {
				return false;
			}
			Type memberType = GetMemberType(member);
			if(memberType != typeof(string) &&
				memberType != typeof(int) &&
				memberType != typeof(float) &&
				memberType != typeof(Vector2) &&
				memberType != typeof(Vector3) &&
				memberType != typeof(Color) &&
				(memberType != typeof(bool)) &&
				memberType != typeof(Quaternion) &&
				memberType != typeof(Rect) &&
				!memberType.IsSubclassOf(typeof(UnityEngine.Object))) {
				return memberType.IsEnum;
			}
			return true;
		}

		public static bool CanGetMember(MemberInfo member, FilterAttribute filter = null) {
			if (!CanGetMemberValue(member)) {
				return false;
			}
			if (member.MemberType == MemberTypes.NestedType)
				return true;
			Type memberType = GetMemberType(member);
			if (memberType != typeof(string) && 
				memberType != typeof(int) && 
				memberType != typeof(float) && 
				memberType != typeof(Vector2) && 
				memberType != typeof(Vector3) && 
				memberType != typeof(Color) && 
				memberType != typeof(bool) && 
				memberType != typeof(Quaternion) && 
				memberType != typeof(Rect) && 
				memberType != typeof(void)) {
				return memberType.IsVisible || 
					filter != null && filter.NonPublic && IsProtectedMember(member) ||
					memberType.IsSubclassOf(typeof(UnityEngine.Object));
			}
			return true;
		}

		public static bool CanGetMemberValue(MemberInfo member) {
			MemberTypes memberType = member.MemberType;
			if (memberType == MemberTypes.Property) {
				return (member as PropertyInfo).CanRead;
			}
			return true;
		}

		public static bool CanSetMemberValue(MemberInfo member) {
			MemberTypes memberType = member.MemberType;
			if (memberType == MemberTypes.Field) {
				return !(member as FieldInfo).IsInitOnly;
			} else if (memberType == MemberTypes.Property) {
				var prop = member as PropertyInfo;
				if(prop.CanWrite)
					return true;
				if(prop.PropertyType.IsByRef) {
					return Attribute.IsDefined(prop, typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute)) == false;
				}
				return false;
			} else if (memberType == MemberTypes.Event) {
				return true;
			}
			return false;
		}

		public static bool IsValidMember(FieldInfo member, BindingFlags flags, Type source = null) {
			if(member == null) return false;
			if(member.IsPublic && flags.HasFlags(BindingFlags.Public) == false)
				return false;
			if(member.IsPrivate && flags.HasFlags(BindingFlags.NonPublic) == false)
				return false;
			if(member.IsStatic && flags.HasFlags(BindingFlags.Static) == false)
				return false;
			if(member.IsFamily && flags.HasFlags(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic) == false)
				return false;
			if(source != null && flags.HasFlags(BindingFlags.DeclaredOnly) && member.DeclaringType != source)
				return false;
			return true;
		}

		public static bool IsValidMember(PropertyInfo member, BindingFlags flags, Type source = null) {
			if(member == null) return false;
			if(member.GetMethod != null) {
				if(member.SetMethod != null) {
					if(IsValidMember(member.GetMethod, flags, source) == false && IsValidMember(member.SetMethod, flags, source) == false)
						return false;
				}
				else {
					if(IsValidMember(member.GetMethod, flags, source) == false)
						return false;
				}
			}
			else if(member.SetMethod != null) {
				if(IsValidMember(member.SetMethod, flags, source) == false)
					return false;
			}
			else {
				return false;
			}
			return true;
		}

		public static bool IsValidMember(MethodInfo member, BindingFlags flags, Type source = null) {
			if(member == null) return false;
			if(member.IsPublic && flags.HasFlags(BindingFlags.Public) == false)
				return false;
			if(member.IsPrivate && flags.HasFlags(BindingFlags.NonPublic) == false)
				return false;
			if(member.IsStatic && flags.HasFlags(BindingFlags.Static) == false)
				return false;
			if(member.IsFamily && flags.HasFlags(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic) == false)
				return false;
			if(source != null && flags.HasFlags(BindingFlags.DeclaredOnly) && member.DeclaringType != source)
				return false;
			return true;
		}

		public static bool IsValidMember(ConstructorInfo member, BindingFlags flags, Type source = null) {
			if(member == null) return false;
			if(member.IsPublic && flags.HasFlags(BindingFlags.Public) == false)
				return false;
			if(member.IsPrivate && flags.HasFlags(BindingFlags.NonPublic) == false)
				return false;
			if(member.IsStatic && flags.HasFlags(BindingFlags.Static) == false)
				return false;
			if(member.IsFamily && flags.HasFlags(BindingFlags.FlattenHierarchy | BindingFlags.NonPublic) == false)
				return false;
			if(source != null && flags.HasFlags(BindingFlags.DeclaredOnly) && member.DeclaringType != source)
				return false;
			return true;
		}

		public static bool IsValidMember(Type member, BindingFlags flags, Type source = null) {
			if(member == null) return false;
			if(member.IsPublic && flags.HasFlags(BindingFlags.Public) == false)
				return false;
			if(member.IsNotPublic && flags.HasFlags(BindingFlags.NonPublic) == false)
				return false;
			if(source != null && flags.HasFlags(BindingFlags.DeclaredOnly) && member.DeclaringType != source)
				return false;
			return true;
		}

		public static bool IsValidMember(MemberInfo member, BindingFlags flags, Type source = null) {
			if(member == null) return false;
			switch(member.MemberType) {
				case MemberTypes.Constructor:
					return IsValidMember(member as ConstructorInfo, flags, source);
				case MemberTypes.Field:
					return IsValidMember(member as FieldInfo, flags, source);
				case MemberTypes.Method:
					return IsValidMember(member as MethodInfo, flags, source);
				case MemberTypes.NestedType:
				case MemberTypes.TypeInfo:
					return IsValidMember(member as Type, flags, source);
				case MemberTypes.Property:
					return IsValidMember(member as PropertyInfo, flags, source);
			}
			return true;
		}

		#region Get Candidates
		internal static List<FieldInfo> GetFieldCandidates(IList<FieldInfo> members, BindingFlags flags, Type source = null) {
			var result = new List<FieldInfo>(members.Count);
			foreach(var m in members) {
				if(IsValidMember(m, flags, source)) {
					result.Add(m);
				}
			}
			return result;
		}

		internal static List<ConstructorInfo> GetConstructorCandidates(IList<ConstructorInfo> members, BindingFlags flags, Type source = null) {
			var result = new List<ConstructorInfo>(members.Count);
			foreach(var m in members) {
				if(IsValidMember(m, flags, source)) {
					result.Add(m);
				}
			}
			return result;
		}

		internal static List<PropertyInfo> GetPropertyCandidates(IList<PropertyInfo> members, BindingFlags flags, Type source = null) {
			var result = new List<PropertyInfo>(members.Count);
			foreach(var m in members) {
				if(IsValidMember(m, flags, source)) {
					result.Add(m);
				}
			}
			return result;
		}

		internal static List<MethodInfo> GetMethodCandidates(IList<MethodInfo> members, BindingFlags flags, Type source = null) {
			var result = new List<MethodInfo>(members.Count);
			foreach(var m in members) {
				if(IsValidMember(m, flags, source)) {
					result.Add(m);
				}
			}
			return result;
		}

		internal static List<T> GetMemberCandidates<T>(IList<T> members, BindingFlags flags, Type source = null) where T : MemberInfo {
			var result = new List<T>(members.Count);
			foreach(var m in members) {
				if(IsValidMember(m, flags, source)) {
					result.Add(m);
				}
			}
			return result;
		}
		#endregion

		public static bool IsValidParameters(MethodBase method) {
			var parameters = method.GetParameters();
			return IsValidParameters(parameters);
		}

		public static bool IsValidParameters(ParameterInfo[] parameters) {
			for(int i = 0; i < parameters.Length; i++) {
				if(parameters[i].ParameterType.IsPointer) {
					//Invalid when there's a pointer type, since uNode doesn't support it.
					return false;
				}
			}
			return true;
		}

		public static bool IsValidConstructor(ConstructorInfo ctor, int maxCtorParam = 0, int minCtorParam = 0) {
			if (ctor != null) {
				if(minCtorParam != 0 || maxCtorParam < 20) {
					ParameterInfo[] Pinfo = ctor.GetParameters();
					if(Pinfo.Length > maxCtorParam || Pinfo.Length < minCtorParam) {
						return false;
					}
					if(!IsValidParameters(Pinfo)) {
						return false;
					}
				}
				if (!ctor.IsGenericMethod && !ctor.ContainsGenericParameters) {
					return true;
				}
			}
			return false;
		}

		public static bool IsValidMethod(MethodInfo method, int maxMethodParam = 0, int minMethodParam = 0, FilterAttribute filter = null) {
			if (method != null) {
				if (filter.SetMember && method.ReturnType == typeof(void)) {
					return false;
				}
				bool isValid = true;
				if(minMethodParam != 0 || maxMethodParam < 20) {
					ParameterInfo[] Pinfo = method.GetParameters();
					if(Pinfo.Length > maxMethodParam || Pinfo.Length < minMethodParam) {
						isValid = false;
					}
				}
				if (isValid && (filter != null && filter.DisplayGenericType || !method.IsGenericMethod && !method.ContainsGenericParameters)) {
					return true;
				}
			}
			return false;
		}

		public static bool IsValidMethodParameter(MethodInfo method, Type parameterType) {
			if(method != null) {
				var parameters = method.GetParameters();
				if(parameters.Length == 1 && parameters[0].ParameterType == parameterType) {
					return true;
				}
			}
			return false;
		}

		public static bool IsValidMethodParameter(MethodInfo method, params Type[] parameterTypes) {
			if(method != null) {
				var parameters = method.GetParameters();
				if(parameters.Length == parameterTypes.Length) {
					for(int i=0;i<parameters.Length;i++) {
						if(parameters[i].ParameterType != parameterTypes[i]) {
							return false;
						}
					}
					return true;
				}
			}
			return false;
		}

		public static List<MethodInfo> GetValidMethodInfo(Type type, string method,
			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static,
			int maxMethodParam = 0, int minMethodParam = 0) {
			List<MethodInfo> validMethods = new List<MethodInfo>();
			List<MethodInfo> mMethods = type.GetMethods(bindingFlags).ToList();

			for (int b = 0; b < mMethods.Count; ++b) {
				MethodInfo mi = mMethods[b];

				string name = mi.Name;
				if (name != method)
					continue;
				if (IsValidMethod(mi, maxMethodParam, minMethodParam)) {
					validMethods.Add(mi);
				}
			}
			return validMethods;
		}

		private static Dictionary<MethodBase, bool> refOrOutMap = null;
		/// <summary>
		/// Are the method has ref/out parameter.
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static bool HasRefOrOutParameter(MethodBase method) {
			if(method == null)
				return false;
			if (refOrOutMap == null)
				refOrOutMap = new Dictionary<MethodBase, bool>();
			bool val;
			if (refOrOutMap.TryGetValue(method, out val)) {
				return val;
			} else {
				ParameterInfo[] paramsInfo = method.GetParameters();
				for (int p = 0; p < paramsInfo.Length; p++) {
					if (paramsInfo[p].IsOut || paramsInfo[p].ParameterType.IsByRef) {
						val = true;
						break;
					}
				}
				refOrOutMap[method] = val;
			}
			return val;
		}

		public static MemberInfo[] GetMemberInfo(Type type, MemberData memberData, BindingFlags bindingAttr, bool throwOnFail = true) {
			if (object.ReferenceEquals(type, null)) {
				if (!throwOnFail || !Application.isPlaying)
					return null;
				throw new System.Exception("type can't null");
			}
			var path = memberData.Items;
			if(path.Length == 0)
				return null;
			MemberInfo[] infoArray = new MemberInfo[path.Length - 1];
			for (int i = 0; i < path.Length; i++) {
				if (i == 0)
					continue;
				string mName = path[i].GetActualName();
				//Event
				EventInfo eventInfo = type.GetEvent(mName, bindingAttr);
				if (eventInfo != null) {
					infoArray[i - 1] = eventInfo;
					if (i + 1 == path.Length)
						break;
					type = eventInfo.EventHandlerType;
					continue;
				}
				//Field
				FieldInfo field = type.GetField(mName, bindingAttr);
				if (field != null) {
					infoArray[i - 1] = field;
					if (i + 1 == path.Length)
						break;
					type = field.FieldType;
					continue;
				}
				//Property
				PropertyInfo property;
				try {
					property = GetProperty(type, mName, bindingAttr);
				} catch (AmbiguousMatchException) {
					property = GetProperty(type, mName, bindingAttr | BindingFlags.DeclaredOnly);
				}
				if (property != null) {
					infoArray[i - 1] = property;
					if (i + 1 == path.Length)
						break;
					type = property.PropertyType;
					continue;
				}
				//Method
				Type[] paramsType = Type.EmptyTypes;
				Type[] genericType = Type.EmptyTypes;
				{
					if(throwOnFail) {
						if(memberData.ParameterTypes != null)
							paramsType = memberData.ParameterTypes[i] ?? paramsType;
						if(memberData.GenericTypes != null)
							genericType = memberData.GenericTypes[i] ?? genericType;
					} else {
						if(MemberData.Utilities.SafeGetParameterTypes(memberData) != null)
							paramsType = MemberData.Utilities.SafeGetParameterTypes(memberData)[i] ?? paramsType;
						if(MemberData.Utilities.SafeGetGenericTypes(memberData) != null)
							genericType = MemberData.Utilities.SafeGetGenericTypes(memberData)[i] ?? genericType;
					}
					// try {
					// 	// MemberDataUtility.DeserializeMemberItem(reflection.Items[i], reflection.targetReference, out genericType, out paramsType);
					// } catch {
					// 	if (throwOnFail) {
					// 		throw;
					// 	}
					// 	return null;
					// }
				}
				MethodInfo method = null;
				if (genericType.Length > 0) {
					bool flag = false;
					bool flag2 = false;
					var methods = GetMethods(type, bindingAttr);
					MethodInfo backupMethod = null;
					foreach(var m in methods) { 
						method = m;
						if (!method.Name.Equals(mName) || !method.IsGenericMethodDefinition) {
							continue;
						}
						if (method.GetGenericArguments().Length == genericType.Length && method.GetParameters().Length == paramsType.Length) {
							for (int y = 0; y < genericType.Length;y++) {
								if(genericType[y] == null) {
									if (throwOnFail) {
										throw new Exception("Type not found: " + MemberDataUtility.GetGenericName(memberData.Items[i].genericArguments[y]));
									}
									return null;
								}
							}
							if (uNodeUtility.isPlaying) {
								method = ReflectionUtils.MakeGenericMethod(method, genericType);
							} else {
								try {
									method = ReflectionUtils.MakeGenericMethod(method, genericType);
								} catch (Exception ex) {
									if (throwOnFail) {
										Debug.LogException(ex);
									}
									return null;
								}
							}
							backupMethod = method;//for alternatife when method not found.
							try {
								ParameterInfo[] parameters = method.GetParameters();
								bool flag3 = false;
								for(int z = 0; z < parameters.Length; z++) {
									if(parameters[z].ParameterType != paramsType[z]) {
										flag3 = true;
										break;
									}
								}
								if(flag3)
									continue;
							} catch {
								if(throwOnFail) {
									throw;
								}
								return null;
							}
							break;
						}
					}
					if (backupMethod != null) {
						infoArray[i - 1] = backupMethod;
						if (HasRefOrOutParameter(backupMethod)) {
							memberData.HasRefOrOut = true;
						}
						if (i + 1 == path.Length) {
							flag2 = true;
							break;
						}
						type = backupMethod.ReturnType;
						flag = true;
					}
					if (flag2)
						break;
					if (flag)
						continue;
				}
				try {
					method = GetMethod(type, mName, paramsType, bindingAttr);
				} catch (AmbiguousMatchException) {
					var methods = type.GetMethods();
					for(int x=0;x<methods.Length;x++) {
						var m = methods[x];
						if(m.Name == mName) {
							var param = m.GetParameters();
							if(paramsType.Length == param.Length) {
								bool mflag = true;
								for(int y = 0; y < param.Length; y++) {
									if(param[y].ParameterType != paramsType[y]) {
										mflag = false;
										break;
									}
								}
								if(mflag) {
									method = m;
									break;
								}
							}
						}
					}
				}
				if (method != null) {
					if (method.IsGenericMethodDefinition && genericType.Length > 0) {
						if (uNodeUtility.isPlaying) {
							method = ReflectionUtils.MakeGenericMethod(method, genericType);
						} else {
							try {
								method = ReflectionUtils.MakeGenericMethod(method, genericType);
							} catch { continue; }
						}
					}
					infoArray[i - 1] = method;
					if (HasRefOrOutParameter(method)) {
						memberData.HasRefOrOut = true;
					}
					if (i + 1 == path.Length)
						break;
					type = method.ReturnType;
					continue;
				}
				if(memberData.targetType == MemberData.TargetType.Constructor) {
					//Constructor
					var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, paramsType, null);
					if(ctor == null && paramsType.Length == 0) {
						ctor = GetDefaultConstructor(type);
						if(ctor == null) {
							return null;
						}
					}
					if(ctor == null) {
						if(throwOnFail) {
							throw new System.Exception("Member not found at path:" + string.Join('.', path.Select(p => p.GetActualName())) +
								", maybe you have wrong type, member name changed or wrong target.\ntype:" +
								type.PrettyName(true));
						}
						else {
							return null;
						}
					}
					infoArray[i - 1] = ctor;
					if(memberData != null && HasRefOrOutParameter(ctor)) {
						memberData.HasRefOrOut = true;
					}
					if(i + 1 == path.Length)
						break;
					type = GetMemberType(ctor.DeclaringType);
				} else {
					MemberInfo[] member = type.GetMember(mName, bindingAttr);
					if(member != null && member.Length > 0) {
						infoArray[i - 1] = member[0];
						if(i + 1 == path.Length)
							break;
						type = GetMemberType(member[0]);
					} else {
						if(throwOnFail) {
							throw new System.Exception("Member not found at path:" + string.Join('.', path.Select(p => p.GetActualName())) + 
								", maybe you have wrong type, member name changed or wrong target.\ntype:" +
								type.PrettyName(true));
						} else {
							return null;
						}
					}
				}
			}
			return infoArray;
		}

		public static object GetMemberTargetRef(MemberInfo[] memberInfo, object target) {
			if (object.ReferenceEquals(target, null)) {
				return null;
			}
			if (object.ReferenceEquals(memberInfo, null)) {
				throw new System.Exception("memberInfo can't null");
			}
			object lastObject = target;
			for (int i = 0; i < memberInfo.Length; i++) {
				if (i + 1 == memberInfo.Length)
					break;
				MemberInfo member = memberInfo[i];
				if(object.ReferenceEquals(lastObject, null) && member.MemberType != MemberTypes.NestedType) {
					throw new NullReferenceException("Null value obtained when accessing memebers: " + string.Join(".", memberInfo.Take(i + 1).Select(m => m.Name)));
				}
				switch (member.MemberType) {
					case MemberTypes.Field:
						FieldInfo field = member as FieldInfo;
						lastObject = field.GetValueOptimized(lastObject);
						break;
					case MemberTypes.Property:
						PropertyInfo property = member as PropertyInfo;
						lastObject = property.GetValueOptimized(lastObject);
						break;
					case MemberTypes.Method:
						MethodInfo method = member as MethodInfo;
						lastObject = method.InvokeOptimized(lastObject, null);
						break;
					case MemberTypes.NestedType:
						lastObject = null;
						break;
					case MemberTypes.Constructor:
						lastObject = (member as ConstructorInfo).Invoke(lastObject, null);
						break;
					case MemberTypes.Event:
						lastObject = (member as EventInfo).EventHandlerType.GetMethod("Invoke").Invoke(lastObject, null);
						break;
					default:
						throw new Exception();
				}
				if (memberInfo[i].MemberType != MemberTypes.NestedType && object.ReferenceEquals(lastObject, null)) {
					return null;
				}
			}
			return lastObject;
		}

		public static object GetMemberTargetRef(MemberInfo[] memberInfo, object target, out object parent, object[] invokeParams) {
			if (object.ReferenceEquals(target, null)) {
				throw new ArgumentNullException(nameof(target), "The target instance is null");
			}
			if (object.ReferenceEquals(memberInfo, null)) {
				throw new ArgumentNullException(nameof(memberInfo));
			}
			parent = target;
			object lastObject = target;
			int lastInvokeNum = 0;
			for (int i = 0; i < memberInfo.Length; i++) {
				if (i + 1 == memberInfo.Length)
					break;
				MemberInfo member = memberInfo[i];
				switch (member.MemberType) {
					case MemberTypes.Field:
						FieldInfo field = member as FieldInfo;
						parent = lastObject;
						lastObject = field.GetValueOptimized(lastObject);
						break;
					case MemberTypes.Property:
						PropertyInfo property = member as PropertyInfo;
						parent = lastObject;
						lastObject = property.GetValueOptimized(lastObject);
						break;
					case MemberTypes.Method:
						MethodInfo method = member as MethodInfo;
						parent = lastObject;
						int paramsLength = method.GetParameters().Length;
						if (invokeParams != null && paramsLength == invokeParams.Length) {
							lastObject = method.InvokeOptimized(lastObject, invokeParams);
						} else if (invokeParams != null && paramsLength > 0 && lastInvokeNum + paramsLength <= invokeParams.Length) {
							object[] obj = new object[paramsLength];
							for (int x = lastInvokeNum; x < paramsLength; x++) {
								obj[x - lastInvokeNum] = invokeParams[x];
							}
							lastObject = method.InvokeOptimized(lastObject, obj);
							if (HasRefOrOutParameter(method)) {
								for (int x = lastInvokeNum; x < paramsLength; x++) {
									invokeParams[x] = obj[x - lastInvokeNum];
								}
							}
							lastInvokeNum += paramsLength;
						} else {
							lastObject = method.InvokeOptimized(lastObject, null);
						}
						break;
					case MemberTypes.NestedType:
						lastObject = null;
						break;
					case MemberTypes.Constructor:
						ConstructorInfo ctor = member as ConstructorInfo;
						paramsLength = ctor.GetParameters().Length;
						if (invokeParams != null && paramsLength == invokeParams.Length) {
							lastObject = ctor.Invoke(lastObject, invokeParams);
						} else if (invokeParams != null && paramsLength > 0 && lastInvokeNum + paramsLength <= invokeParams.Length) {
							object[] obj = new object[paramsLength];
							for (int x = lastInvokeNum; x < paramsLength; x++) {
								obj[x - lastInvokeNum] = invokeParams[x];
							}
							lastObject = ctor.Invoke(lastObject, obj);
							if (HasRefOrOutParameter(ctor)) {
								for (int x = lastInvokeNum; x < paramsLength; x++) {
									invokeParams[x] = obj[x - lastInvokeNum];
								}
							}
							lastInvokeNum += paramsLength;
						} else {
							lastObject = ctor.Invoke(lastObject, null);
						}
						break;
					case MemberTypes.Event:
						MethodInfo minfo = (member as EventInfo).EventHandlerType.GetMethod("Invoke");
						parent = lastObject;
						paramsLength = minfo.GetParameters().Length;
						if (invokeParams != null && paramsLength == invokeParams.Length) {
							lastObject = minfo.Invoke(lastObject, invokeParams);
						} else if (invokeParams != null && paramsLength > 0 && lastInvokeNum + paramsLength <= invokeParams.Length) {
							object[] obj = new object[paramsLength];
							for (int x = lastInvokeNum; x < paramsLength; x++) {
								obj[x - lastInvokeNum] = invokeParams[x];
							}
							lastObject = minfo.Invoke(lastObject, obj);
							if (HasRefOrOutParameter(minfo)) {
								for (int x = lastInvokeNum; x < paramsLength; x++) {
									invokeParams[x] = obj[x - lastInvokeNum];
								}
							}
							lastInvokeNum += paramsLength;
						} else {
							lastObject = minfo.Invoke(lastObject, null);
						}
						break;
					default:
						throw new Exception(member.MemberType.ToString());
				}
				if (memberInfo[i].MemberType != MemberTypes.NestedType && object.ReferenceEquals(lastObject, null)) {
					return null;
				}
			}
			return lastObject;
		}

		public static bool GetMemberIsStatic(MemberInfo member) {
			switch (member.MemberType) {
				case MemberTypes.Field:
					return (member as FieldInfo).IsStatic;
				case MemberTypes.Property:
					var prop = member as PropertyInfo;
					if(prop.GetMethod != null) {
						return prop.GetMethod.IsStatic;
					} else {
						return prop.SetMethod != null && prop.SetMethod.IsStatic;
					}
				case MemberTypes.Method:
					return (member as MethodInfo).IsStatic;
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					return true;
				case MemberTypes.Event:
					return (member as EventInfo).GetAddMethod().IsStatic;
				case MemberTypes.Constructor:
					return true;
					//return (member as ConstructorInfo).IsStatic;
			}
			return false;
		}

		public static Type GetMemberType(MemberInfo member) {
			if(member == null)
				throw new ArgumentNullException(nameof(member));
			switch (member.MemberType) {
				case MemberTypes.Field:
					return (member as FieldInfo).FieldType;
				case MemberTypes.Property:
					return (member as PropertyInfo).PropertyType;
				case MemberTypes.Method:
					return (member as MethodInfo).ReturnType;
				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					return member as Type;
				case MemberTypes.Event:
					Type delegateType = (member as EventInfo).EventHandlerType;
					return delegateType;
				//MethodInfo invoke = delegateType.GetMethod("Invoke");
				//return invoke.ReturnType;
				case MemberTypes.Constructor:
					return (member as ConstructorInfo).DeclaringType;
			}
			throw new ArgumentException("MemberInfo must be of type FieldInfo, PropertyInfo, NestedType or Event " + " - " + member.MemberType, "member");
		}

		public static void SetBoxedMemberValue(object parent, MemberInfo targetInfo, object target, MemberInfo propertyInfo, object value) {
			SetMemberValue(propertyInfo, target, value);
			SetMemberValue(targetInfo, parent, target);
		}

		public static void SetMemberValue(MemberInfo member, object target, object value) {
			if (!CanSetMemberValue(member))
				return;
			MemberTypes memberType = member.MemberType;
			if (memberType != MemberTypes.Field) {
				if (memberType != MemberTypes.Property) {
					throw new ArgumentException("MemberInfo must be type FieldInfo or PropertyInfo", "member");
				}
			} else {
				(member as FieldInfo).SetValueOptimized(target, value);
				return;
			}
			(member as PropertyInfo).SetValueOptimized(target, value);
		}

		public static MethodInfo[] GetOverloadingMethod(MethodInfo method) {
			MethodInfo[] memberInfos = null;
			if (method != null) {
				if (method.ReflectedType != null) {
					memberInfos = method.ReflectedType.GetMethods();
				} else if (method.DeclaringType != null) {
					memberInfos = method.DeclaringType.GetMethods();
				}
				if (memberInfos != null) {
					memberInfos = memberInfos.Where(m =>
						m.Name.Equals(method.Name)).ToArray();
				}
			}
			return memberInfos;
		}

		public static T GetAttributeFrom<T>(object from, bool inherit = false) where T : Attribute {
			return from.GetType().GetCustomAttribute(typeof(T), inherit) as T;
		}

		public static T GetAttribute<T>(params object[] attributes) where T : Attribute {
			if(attributes == null)
				return null;
			foreach(var v in attributes) {
				if(v is T) {
					return v as T;
				}
			}
			return null;
		}

		public static T GetAttribute<T>(params Attribute[] attributes) where T : Attribute {
			if(attributes == null)
				return null;
			foreach(var v in attributes) {
				if(v is T) {
					return v as T;
				}
			}
			return null;
		}

		public static MemberInfo AutoResolveGenericMember(MemberInfo member) {
			if(member is IRuntimeMember) {
				return null;
			}
			if(member is Type type) {
				if(type.IsGenericTypeDefinition) {
					Type[] genericType = type.GetGenericArguments();
					for(int i = 0; i < genericType.Length; i++) {
						genericType[i] = AutoResolveGenericMember(genericType[i]) as Type;
						if(genericType[i] == null) {
							return null;
						}
					}
					return ReflectionUtils.MakeGenericType(type, genericType);
				}
				else {
					return GetDefaultConstraint(type);
				}
			}
			else if(member is MethodInfo method) {
				if(method.IsGenericMethodDefinition) {
					Type[] genericType = method.GetGenericArguments();
					for(int i = 0; i < genericType.Length; i++) {
						genericType[i] = AutoResolveGenericMember(genericType[i]) as Type;
						if(genericType[i] == null) {
							return null;
						}
					}
					return ReflectionUtils.MakeGenericMethod(method, genericType);
				}
			}
			return null;
		}

		public static Type GetDefaultConstraint(Type type) {
			if(type.IsGenericParameter) {
				var constraints = type.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
				if((constraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0) {//class constraint

					var pType = type.GetGenericParameterConstraints();
					if(pType.Length == 0) {
						return typeof(object);
					}
					else if(pType.Length == 1 && !pType[0].IsInterface) {
						return pType[0];
					}

					return null;
				}
				else if((constraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) {//struct constraint
					return null;
				}
				else if((constraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0) {//new constraint
					var pType = type.GetGenericParameterConstraints();
					if(pType.Length == 0) {
						return typeof(object);
					}
					return null;
				}
				else {
					//no constraint
					var pType = type.GetGenericParameterConstraints();
					if(pType.Length == 0) {
						return typeof(object);
					}
					else if(pType.Length == 1 && pType[0] != typeof(ValueType)) {
						return pType[0];
					}
					return null;
				}
			}
			return null;
		}

		public static Type GetDefaultConstraint(Type type, Type preferedType) {
			if(preferedType == null) return GetDefaultConstraint(type);
			if(type.IsGenericParameter) {
				var constraints = type.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
				if((constraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0) {//class constraint
					if(preferedType.IsClass) {
						var pType = type.GetGenericParameterConstraints();
						if(pType.Length == 0) {
							return preferedType;
						}
						else if(pType.Length == 1 && preferedType.IsCastableTo(pType[0])) {
							return preferedType;
						}
					}

					return null;
				}
				else if((constraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) {//struct constraint
					if(preferedType.IsValueType) {
						var pType = type.GetGenericParameterConstraints();
						if(pType.Length == 0) {
							return preferedType;
						}
						else if(pType.Length == 1 && preferedType.IsCastableTo(pType[0])) {
							return preferedType;
						}
						else if(pType.Length == 2 && preferedType.IsCastableTo(pType[1])) {
							return preferedType;
						}
					}
					return null;
				}
				else if((constraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0) {//new constraint
					var pType = type.GetGenericParameterConstraints();
					if(pType.Length == 0) {
						return preferedType;
					}
					else if(pType.Length == 1 && preferedType.IsCastableTo(pType[0])) {
						return preferedType;
					}
					return null;
				}
				else {
					//no constraint
					var pType = type.GetGenericParameterConstraints();
					if(pType.Length == 0) {
						return preferedType;
					}
					else if(pType.Length == 1 && pType[0] != typeof(ValueType) && preferedType.IsCastableTo(pType[0])) {
						return preferedType;
					}
					else if(pType.Length == 2 && pType[0] == typeof(ValueType) && preferedType.IsCastableTo(pType[1])) {
						return preferedType;
					}
					return null;
				}
			}
			return null;
		}

		public static Type ReplaceUnconstructedType(Type type, Func<Type, Type> replacement) {
			if(type.IsGenericParameter) {
				return replacement(type);
			}
			else if(type.ContainsGenericParameters) {
				if(type.IsGenericType) {
					var types = type.GetGenericArguments();
					for(int i = 0; i < types.Length; i++) {
						types[i] = ReplaceUnconstructedType(types[i], replacement);
					}
					if(type.IsGenericTypeDefinition == false) {
						return ReflectionUtils.MakeGenericType(type.GetGenericTypeDefinition(), types);
					}
					return ReflectionUtils.MakeGenericType(type, types);
				}
				else if(type.IsArray) {
					if(type.GetArrayRank() > 1) {
						throw new NotImplementedException("Multidimensional array currently is not supported.");
					}
					return ReflectionUtils.MakeArrayType(replacement(type.GetElementType()));
				}
				else if(type.IsByRef) {
					return ReplaceUnconstructedType(type.ElementType(), replacement).MakeByRefType();
				}
				else {
					throw new NotImplementedException();
				}
			}
			return type;
		}

		public static MethodInfo GetUserDefinedOperator(Type leftType, Type rightType, string name) {
			Type[] types = new Type[2] { leftType, rightType };
			Type nonNullableType = leftType.GetNonNullableType();
			Type nonNullableType2 = rightType.GetNonNullableType();
			BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			MethodInfo methodInfo = nonNullableType.GetMethodValidated(name, bindingAttr, null, types, null);
			if(methodInfo == null && !AreEquivalent(leftType, rightType)) {
				methodInfo = nonNullableType2.GetMethodValidated(name, bindingAttr, null, types, null);
			}
			return methodInfo;
		}

		private static MethodInfo GetMethodValidated(this Type type, string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) {
			MethodInfo method = type.GetMethod(name, bindingAttr, binder, types, modifiers);
			if(!method.MatchesArgumentTypes(types)) {
				return null;
			}
			return method;
		}

		public static bool MatchesArgumentTypes(this MethodInfo mi, Type[] argTypes) {
			if(mi == null || argTypes == null) {
				return false;
			}
			ParameterInfo[] parameters = mi.GetParameters();
			if(parameters.Length != argTypes.Length) {
				return false;
			}
			for(int i = 0; i < parameters.Length; i++) {
				if(!AreReferenceAssignable(parameters[i].ParameterType, argTypes[i])) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Value passing for passing value to function, etc.
		/// Note: if the value is value type it will be duplicated.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static object ValuePassing(object value) {
			if(value == null)
				return null;
			if(!_valuePassing.TryGetValue(value.GetType(), out var func)) {
				var type = value.GetType();
				if(type.IsValueType) {
					//For value type, the value will be duplicated
					System.Linq.Expressions.ParameterExpression paramA = System.Linq.Expressions.Expression.Parameter(typeof(object), "a");
					func = System.Linq.Expressions.Expression.Lambda<System.Func<object, object>>(
							System.Linq.Expressions.Expression.Convert(
								System.Linq.Expressions.Expression.Convert(paramA, type),
								typeof(object)), paramA).Compile();
				}
				else {
					func = (val) => val;
				}
				_valuePassing[type] = func;
			}
			return func(value);
		}

		static Dictionary<Type, Func<object, object>> _valuePassing = new Dictionary<Type, Func<object, object>>();

		internal static bool AreReferenceAssignable(Type dest, Type src) {
			if(AreEquivalent(dest, src)) {
				return true;
			}
			if(!dest.IsValueType && !src.IsValueType && dest.IsAssignableFrom(src)) {
				return true;
			}
			return false;
		}

		internal static bool AreEquivalent(Type t1, Type t2) {
			if(!(t1 == t2)) {
				return t1.IsEquivalentTo(t2);
			}
			return true;
		}

		internal static Type GetNonNullableType(this Type type) {
			if(IsNullableType(type)) {
				return type.GetGenericArguments()[0];
			}
			return type;
		}

		internal static bool IsNullableType(Type type) {
			if(type.IsGenericType) {
				return type.GetGenericTypeDefinition() == typeof(Nullable<>);
			}
			return false;
		}
	}
}