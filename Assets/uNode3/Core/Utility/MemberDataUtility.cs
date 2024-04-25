using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace MaxyGames.UNode {
	public static class MemberDataUtility {
		public const string RUNTIME_ID = "[runtime]";

		public static MemberData.ItemData CreateItemData(Variable variable) {
			if(variable == null)
				throw new ArgumentNullException(nameof(variable));
			if(variable.graphContainer == null)
				throw new NullReferenceException("The graph container is null");
			return new MemberData.ItemData() {
				name = variable.name,
				reference = new VariableRef(variable, variable.graphContainer),
			};
		}

		public static MemberData.ItemData CreateItemData(Property property) {
			if(property == null)
				throw new ArgumentNullException(nameof(property));
			if(property.graphContainer == null)
				throw new NullReferenceException("The graph container is null");
			return new MemberData.ItemData() {
				name = property.name,
				reference = new PropertyRef(property, property.graphContainer),
			};
		}

		public static MemberData.ItemData CreateItemData(Function function) {
			if(function == null)
				throw new ArgumentNullException(nameof(function));
			if(function.graphContainer == null)
				throw new NullReferenceException("The graph container is null");
			var data = new MemberData.ItemData() {
				name = function.name,
				reference = new FunctionRef(function, function.graphContainer),
			};
			if(function.genericParameters.Length > 0) {
				throw new System.Exception("Can't Add Function with generic parameter, try using Add Value Node or manually select function");
			}
			var paramsInfo = function.parameters;
			if(paramsInfo.Count > 0) {
				data.parameters = ParameterDataToTypeDatas(paramsInfo, null);
			}
			return data;
		}

		public static MemberData.ItemData CreateItemData(ParameterRef parameter) {
			if(parameter == null)
				throw new ArgumentNullException(nameof(parameter));
			if(parameter.reference == null || parameter.reference.graphContainer == null)
				throw new NullReferenceException("The graph container is null");
			return new MemberData.ItemData() {
				name = parameter.name,
				reference = parameter,
			};
		}

		public static MemberData.ItemData CreateItemData(Function function, Type[] genericTypes) {
			if(function == null)
				throw new ArgumentNullException(nameof(function));
			if(function.graphContainer == null)
				throw new NullReferenceException("The graph container is null");
			var data = new MemberData.ItemData() {
				name = function.name,
				reference = new FunctionRef(function, function.graphContainer),
			};
			GenericParameterData[] genericParamArgs = function.genericParameters;
			if(genericParamArgs.Length > 0) {
				if(genericTypes != null && genericTypes.Length == genericParamArgs.Length) {
					TypeData[] param = new TypeData[genericTypes.Length];
					for(int i = 0; i < genericTypes.Length; i++) {
						param[i] = MemberDataUtility.GetTypeData(genericTypes[i]);
					}
					data.genericArguments = param;
				}
				else {
					throw new System.Exception("Can't Add Function because incorrect given generic types.");
				}
			}
			var paramsInfo = function.parameters;
			if(paramsInfo.Count > 0) {
				data.parameters = ParameterDataToTypeDatas(paramsInfo, genericParamArgs);
			}
			return data;
		}

		public static MemberData.ItemData CreateItemData(MemberInfo member) {
			MemberData.ItemData iData = new MemberData.ItemData() {
				name = member.Name,
			};
			if(member is IRuntimeMemberWithRef) {
				var reference = (member as IRuntimeMemberWithRef).GetReference();
				iData.reference = reference;
				iData.name = RUNTIME_ID;
			}
			MethodInfo methodInfo = member as MethodInfo;
			if(member is EventInfo) {
				methodInfo = ((EventInfo)(member)).EventHandlerType.GetMethod("Invoke");
			}
			ConstructorInfo ctor = member as ConstructorInfo;
			if(methodInfo != null || ctor != null) {
				Type[] genericMethodArgs = methodInfo != null ? methodInfo.GetGenericArguments() : null;
				if(genericMethodArgs != null && genericMethodArgs.Length > 0) {
					TypeData[] param = new TypeData[genericMethodArgs.Length];
					for(int i = 0; i < genericMethodArgs.Length; i++) {
						param[i] = GetTypeData(genericMethodArgs[i], null);
					}
					iData.genericArguments = param;
				}
				ParameterInfo[] paramsInfo = methodInfo != null ? methodInfo.GetParameters() : ctor.GetParameters();
				if(paramsInfo.Length > 0) {
					TypeData[] paramData = new TypeData[paramsInfo.Length];
					for(int x = 0; x < paramsInfo.Length; x++) {
						TypeData gData = GetTypeData(paramsInfo[x], genericMethodArgs != null ? genericMethodArgs.Select(it => it.Name).ToArray() : null);
						paramData[x] = gData;
					}
					iData.parameters = paramData;
				}
			}
			return iData;
		}

		public static bool HasGenericArguments(Type type) {
			return type != null && (type.GetGenericArguments().Length > 0 ||
				type.HasElementType && HasGenericArguments(type.GetElementType()));
		}

		public static TypeData GetTypeData(ParameterInfo parameter, params string[] genericName) {
			string name = null;
			if(parameter.ParameterType.IsGenericParameter) {
				name = "#" + GetGenericIndex(parameter.ParameterType.Name, genericName);
			}
			else {
				return GetTypeData(parameter.ParameterType, genericName);
			}
			return new TypeData(name);
		}

		public static TypeData GetTypeData(Type type, params string[] genericName) {
			if(type == null)
				throw new ArgumentNullException(nameof(type));
			TypeData data = new TypeData();
			int array = 0;
			while(type.IsArray) {
				type = type.GetElementType();
				array++;
			}
			string name;
			if(type.IsGenericParameter) {
				if(genericName != null && genericName.Length > 0) {
					name = "#" + GetGenericIndex(type.Name, genericName);
				}
				else {
					name = "$" + type.Name;
				}
			}
			else if(type is RuntimeType) {
				if(type.IsByRef) {
					data.name = "&";
					var elementType = type.GetElementType();
					data.parameters = new TypeData[] { GetTypeData(elementType, genericName) };
					return data;
				}
				if(type is IRuntimeMemberWithRef memberWithRef) {
					name = RUNTIME_ID;
					data.reference = memberWithRef.GetReference();
				}
				else if(type is IFakeType) {
					if(type.IsArray) {
						data.name = "?";
						var elementType = type.GetElementType();
						data.parameters = new TypeData[] { GetTypeData(elementType, genericName) };
						return data;
					}
					else if(type.IsGenericType) {
						Type[] genericArgs = type.GetGenericArguments();
						if(genericArgs.Length > 0) {
							data.parameters = new TypeData[genericArgs.Length];
							for(int i = 0; i < genericArgs.Length; i++) {
								data.parameters[i] = GetTypeData(genericArgs[i], genericName);
							}
						}
						else if(data.parameters.Length == 0) {
							data.parameters = null;
						}
						if(!type.IsGenericTypeDefinition) {
							type = type.GetGenericTypeDefinition();
						}
						name = "!" + type.FullName;
					}
					else {
						throw new Exception("Unsupported RuntimeType: " + type);
					}
				}
				else if(type is MissingType) {
					if(type is MissingGraphType) {
						name = RUNTIME_ID;
						data.reference = NativeTypeRef.FromMissingType(type as MissingGraphType);
					}
					else {
						name = type.Name;
					}
				}
				else {
					throw new Exception("Unsupported RuntimeType: " + type);
				}
			}
			else if(type.IsGenericType) {
				Type[] genericArgs = type.GetGenericArguments();
				if(genericArgs.Length > 0) {
					data.parameters = new TypeData[genericArgs.Length];
					for(int i = 0; i < genericArgs.Length; i++) {
						data.parameters[i] = GetTypeData(genericArgs[i], genericName);
					}
				}
				else if(data.parameters.Length == 0) {
					data.parameters = null;
				}
				if(!type.IsGenericTypeDefinition) {
					type = type.GetGenericTypeDefinition();
				}
				name = type.FullName;
			}
			else {
				name = type.FullName;
			}
			while(array > 0) {
				name += "[]";
				array--;
			}
			data.name = name;
			return data;
		}

		public static TypeData GetTypeData(MemberData member) {
			if(member.targetType == MemberData.TargetType.uNodeGenericParameter) {
				return new TypeData("$" + member.name);
			}
			Type type = member.Get<Type>(null);
			TypeData data = new TypeData();
			int array = 0;
			while(type.IsArray) {
				type = type.GetElementType();
				array++;
			}
			string name;
			if(type.IsGenericParameter) {
				name = "$" + type.Name;
			}
			else if(type is RuntimeType) {
				if(type.IsByRef) {
					data.name = "&";
					var elementType = type.GetElementType();
					data.parameters = new TypeData[] { GetTypeData(elementType, null) };
					return data;
				}
				if(type is IRuntimeMemberWithRef memberWithRef) {
					name = RUNTIME_ID;
					data.reference = memberWithRef.GetReference();
				}
				else if(type is IFakeType) {
					if(type.IsArray) {
						data.name = "?";
						var elementType = type.GetElementType();
						data.parameters = new TypeData[] { GetTypeData(elementType, null) };
						return data;
					}
					else if(type.IsGenericType) {
						Type[] genericArgs = type.GetGenericArguments();
						if(genericArgs.Length > 0) {
							data.parameters = new TypeData[genericArgs.Length];
							for(int i = 0; i < genericArgs.Length; i++) {
								data.parameters[i] = GetTypeData(genericArgs[i], null);
							}
						}
						else if(data.parameters.Length == 0) {
							data.parameters = null;
						}
						if(!type.IsGenericTypeDefinition) {
							type = type.GetGenericTypeDefinition();
						}
						name = "!" + type.FullName;
					}
					else {
						throw new Exception("Unsupported RuntimeType: " + type);
					}
				}
				else if(type is MissingType) {
					if(type is MissingGraphType) {
						name = RUNTIME_ID;
						data.reference = NativeTypeRef.FromMissingType(type as MissingGraphType);
					}
					else {
						name = type.Name;
					}
				}
				else {
					throw new Exception("Unsupported RuntimeType: " + type);
				}
			}
			else if(type.IsGenericType) {
				Type[] genericArgs = type.GetGenericArguments();
				if(genericArgs.Length > 0) {
					data.parameters = new TypeData[genericArgs.Length];
					for(int i = 0; i < genericArgs.Length; i++) {
						data.parameters[i] = GetTypeData(genericArgs[i], null);
					}
				}
				else if(data.parameters.Length == 0) {
					data.parameters = null;
				}
				if(!type.IsGenericTypeDefinition) {
					type = type.GetGenericTypeDefinition();
				}
				name = type.FullName;
			}
			else {
				name = type.FullName;
			}
			while(array > 0) {
				name += "[]";
				array--;
			}
			data.name = name;
			return data;
		}

		public static int GetGenericIndex(string name, params string[] genericName) {
			if(genericName != null) {
				for(int i = 0; i < genericName.Length; i++) {
					if(genericName[i] == name) {
						return i;
					}
				}
			}
			return 0;
		}

		public static void ReflectGenericData(TypeData data, Action<TypeData> action) {
			action(data);
			if(data.parameters != null) {
				foreach(TypeData d in data.parameters) {
					ReflectGenericData(d, action);
				}
			}
		}

		public static TypeData[] ParameterDataToTypeDatas(IList<ParameterData> parameters, GenericParameterData[] genericParameters = null) {
			if(genericParameters == null) {
				genericParameters = new GenericParameterData[0];
			}
			TypeData[] paramData = new TypeData[parameters.Count];
			for(int x = 0; x < parameters.Count; x++) {
				if(parameters[x].type.isNative) {
					paramData[x] = new TypeData() { name = parameters[x].type.type.FullName };
				}
				else {
					var type = parameters[x].type.type;
					if(type is IRuntimeMemberWithRef runtimeRefType) {
						paramData[x] = new TypeData() {
							name = RUNTIME_ID,
							reference = runtimeRefType.GetReference()
						};
					}
					else if(type is IFakeType) {
						paramData[x] = GetTypeData(type, null);
					}
					else {
						//TODO: Generic Type
						//paramData[x] = new TypeData(
						//	"#" + GetGenericIndex(parameters[x].type.name,
						//	genericParameters.Select(it => it.name).ToArray())
						//);
						throw new Exception("Unsupported RuntimeType: " + type);
					}
				}
			}
			return paramData;
		}

		/// <summary>
		/// The all items name from ItemData.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="targets"></param>
		/// <param name="GenericType"></param>
		/// <param name="ParameterType"></param>
		public static void GetItemName(MemberData.ItemData item, out string[] GenericType, out string[] ParameterType) {
			string[] GType = Array.Empty<string>();
			string[] PType = Array.Empty<string>();
			if(item != null) {
				if(item.reference != null && (item.genericArguments == null || item.genericArguments.Length == 0)) {
					if(item.GetReferenceValue() is IParameterSystem parameterSystem) {
						PType = new string[parameterSystem.Parameters.Count];
						for(int i = 0; i < parameterSystem.Parameters.Count; i++) {
							PType[i] = parameterSystem.Parameters[i].type.typeName;
						}
					}
				}
				else {
					if(item.genericArguments != null) {
						GType = new string[item.genericArguments.Length];
						for(int i = 0; i < GType.Length; i++) {
							GType[i] = GetGenericName(item.genericArguments[i]);
						}
					}
					if(item.parameters != null) {
						PType = new string[item.parameters.Length];
						for(int i = 0; i < PType.Length; i++) {
							PType[i] = GetParameterName(item.parameters[i], GType);
						}
					}
				}
			}
			GenericType = GType;
			ParameterType = PType;
		}

		/// <summary>
		/// The all items name from ItemData.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="targets"></param>
		/// <param name="GenericType"></param>
		/// <param name="ParameterType"></param>
		public static void GetRichItemName(MemberData.ItemData item, out string[] GenericType, out string[] ParameterType) {
			string[] GType = Array.Empty<string>();
			string[] PType = Array.Empty<string>();
			if(item != null) {
				if(item.reference != null && (item.genericArguments == null || item.genericArguments.Length == 0)) {
					if(item.GetReferenceValue() is IParameterSystem parameterSystem) {
						PType = new string[parameterSystem.Parameters.Count];
						for(int i = 0; i < parameterSystem.Parameters.Count; i++) {
							PType[i] = parameterSystem.Parameters[i].type.GetRichName();
						}
					}
				}
				else {
					if(item.genericArguments != null) {
						GType = new string[item.genericArguments.Length];
						for(int i = 0; i < GType.Length; i++) {
							GType[i] = GetRichGenericName(item.genericArguments[i]);
						}
					}
					if(item.parameters != null) {
						PType = new string[item.parameters.Length];
						for(int i = 0; i < PType.Length; i++) {
							PType[i] = GetRichParameterName(item.parameters[i], GType);
						}
					}
				}
			}
			GenericType = GType;
			ParameterType = PType;
		}

		/// <summary>
		/// Get generic name from TypeData.
		/// </summary>
		/// <param name="genericData"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static string GetGenericName(TypeData genericData) {
			int array = 0;
			string n = genericData.name;
			while(n.EndsWith("[]")) {
				n = n.Remove(n.Length - 2);
				array++;
			}
			System.Type t = null;
			if(n[0] == '$') {
				n = n.Remove(0, 1);
			}
			else if(n[0] == '&') {
				return GetGenericName(genericData.parameters[0]) + '&';
			}
			else if(n == RUNTIME_ID || genericData.reference != null) {
				var reference = genericData.reference;
				if(reference != null) {
					return reference.name;
				}
			}
			else {
				t = TypeSerializer.Deserialize(n, false);
				if(t == null)
					return n;
			}
			if(t != null && t.IsGenericTypeDefinition && genericData.parameters != null && genericData.parameters.Length > 0) {
				string[] T = new string[genericData.parameters.Length];
				for(int i = 0; i < genericData.parameters.Length; i++) {
					T[i] = GetGenericName(genericData.parameters[i]);
				}
				n = String.Format("{0}<{1}>", t.Name.Split('`')[0], String.Join(", ", T));
			}
			while(array > 0) {
				n += "[]";
				array--;
			}
			return n;
		}

		/// <summary>
		/// Get parameter name from TypeData.
		/// </summary>
		/// <param name="genericData"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		public static string GetParameterName(TypeData genericData, IList<string> types = null) {
			int array = 0;
			string n = genericData.name;
			while(n.EndsWith("[]")) {
				n = n.Remove(n.Length - 2);
				array++;
			}
			Type t = null;
			switch(n[0]) {
				case '$': {//For generic param
					n = n.Remove(0, 1);
					GenericParameterData data = FindGenericData(n, genericData.reference);
					if(data != null) {
						t = data.value;
					}
					else {
						t = typeof(object);
					}
					break;
				}
				case '#': {//for parameter references by index
					n = n.Remove(0, 1);
					if(types != null) {
						n = types[int.Parse(n)];
						t = TypeSerializer.Deserialize(n, false);
					}
					break;
				}
				case '[': {//for default runtime type references
					if(n == RUNTIME_ID) {
						var reference = genericData.reference;
						if(reference != null) {
							if(array > 0) {
								string str = null;
								while(array > 0) {
									array--;
									str += "[]";
								}
								return reference.name + str;
							}
							return reference.name;
						}
					}
					else {
						goto default;
					}
					break;
				}
				case '!': {//Generic runtime type
					t = TypeSerializer.Deserialize(n.Remove(0, 1), false);
					break;
				}
				case '?': {//Array runtime type
					string str = null;
					while(array > 0) {
						array--;
						str += "[]";
					}
					return GetParameterName(genericData.parameters[0], types) + str;
				}
				case '&': {//By Ref Type
					return GetParameterName(genericData.parameters[0], types) + '&';
				}
				default: {
					t = TypeSerializer.Deserialize(n, false);
					break;
				}
			}
			if(t != null && t.IsGenericTypeDefinition && genericData.parameters != null && genericData.parameters.Length > 0) {
				string[] T = new string[genericData.parameters.Length];
				for(int i = 0; i < genericData.parameters.Length; i++) {
					T[i] = GetParameterName(genericData.parameters[i], types);
				}
				n = String.Format("{0}<{1}>", t.Name.Split('`')[0], String.Join(", ", T));
			}
			while(array > 0) {
				n += "[]";
				array--;
			}
			return n;
		}

		/// <summary>
		/// Get generic name from TypeData.
		/// </summary>
		/// <param name="genericData"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static string GetRichGenericName(TypeData genericData) {
			int array = 0;
			string n = genericData.name;
			while(n.EndsWith("[]")) {
				n = n.Remove(n.Length - 2);
				array++;
			}
			System.Type t = null;
			if(n[0] == '$') {
				n = n.Remove(0, 1);
			}
			else if(n[0] == '&') {
				return GetRichGenericName(genericData.parameters[0]) + '&';
			}
			else if(n == RUNTIME_ID || genericData.reference != null) {
				var reference = genericData.reference;
				if(reference != null) {
					return reference.name;
				}
			}
			else {
				t = TypeSerializer.Deserialize(n, false);
				if(t == null)
					return n;
			}
			if(t != null) {
				if(t.IsGenericTypeDefinition && genericData.parameters != null && genericData.parameters.Length > 0) {
					string[] T = new string[genericData.parameters.Length];
					for(int i = 0; i < genericData.parameters.Length; i++) {
						T[i] = GetRichGenericName(genericData.parameters[i]);
					}
					n = String.Format("{0}<{1}>", uNodeUtility.WrapTextWithTypeColor(t.Name.Split('`')[0], t), String.Join(", ", T));
				}
				else {
					n = uNodeUtility.WrapTextWithTypeColor(t.PrettyName(), t);
				}
			}
			while(array > 0) {
				n += "[]";
				array--;
			}
			return n;
		}

		/// <summary>
		/// Get parameter name from TypeData.
		/// </summary>
		/// <param name="genericData"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		public static string GetRichParameterName(TypeData genericData, IList<string> types = null) {
			int array = 0;
			string n = genericData.name;
			while(n.EndsWith("[]")) {
				n = n.Remove(n.Length - 2);
				array++;
			}
			Type t = null;
			switch(n[0]) {
				case '$': {//For generic param
					n = n.Remove(0, 1);
					GenericParameterData data = FindGenericData(n, genericData.reference);
					if(data != null) {
						t = data.value;
					}
					else {
						t = typeof(object);
					}
					break;
				}
				case '#': {//for parameter references by index
					n = n.Remove(0, 1);
					if(types != null) {
						n = types[int.Parse(n)];
						t = TypeSerializer.Deserialize(n, false);
					}
					break;
				}
				case '[': {//for default runtime type references
					if(n == RUNTIME_ID) {
						var reference = genericData.reference;
						if(reference != null) {
							return reference.name;
						}
					}
					else {
						goto default;
					}
					break;
				}
				case '!': {//Generic runtime type
					t = TypeSerializer.Deserialize(n.Remove(0, 1), false);
					break;
				}
				case '?': {//Array runtime type
					return GetRichParameterName(genericData.parameters[0], types) + "[]";
				}
				case '&': {//By Ref Type
					return GetRichParameterName(genericData.parameters[0], types) + '&';
				}
				default: {
					t = TypeSerializer.Deserialize(n, false);
					break;
				}
			}
			if(t != null) {
				if(t.IsGenericTypeDefinition && genericData.parameters != null && genericData.parameters.Length > 0) {
					string[] T = new string[genericData.parameters.Length];
					for(int i = 0; i < genericData.parameters.Length; i++) {
						T[i] = GetRichParameterName(genericData.parameters[i], types);
					}
					n = String.Format("{0}<{1}>", uNodeUtility.WrapTextWithTypeColor(t.PrettyName().Split('`')[0], t), String.Join(", ", T));
				}
				else {
					n = uNodeUtility.WrapTextWithTypeColor(t.PrettyName(), t);
				}
			}
			while(array > 0) {
				n += "[]";
				array--;
			}
			return n;
		}

		/// <summary>
		/// Get the generic types from ItemData.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static Type[] GetGenericTypes(MemberData.ItemData item) {
			Type[] GType = Type.EmptyTypes;
			if(item.genericArguments != null) {
				GType = new Type[item.genericArguments.Length];
				for(int i = 0; i < GType.Length; i++) {
					GType[i] = GetGenericType(item.genericArguments[i]);
				}
			}
			return GType;
		}

		/// <summary>
		/// Get the parameter types from ItemData.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="targets"></param>
		/// <param name="genericType"></param>
		/// <returns></returns>
		public static Type[] GetParameterTypes(MemberData.ItemData item, params Type[] genericType) {
			Type[] PType = Type.EmptyTypes;
			if(item.parameters != null) {
				PType = new Type[item.parameters.Length];
				for(int i = 0; i < PType.Length; i++) {
					PType[i] = GetParameterType(item.parameters[i], genericType);
				}
			}
			return PType;
		}

		/// <summary>
		/// Deserialize the MemberData Item only for Generic Parameter
		/// </summary>
		/// <param name="item"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static Type[] DeserializeItemGeneric(MemberData.ItemData item, params object[] targets) {
			return DeserializeItemGeneric(item, targets as IList<object>);
		}

		/// <summary>
		/// Deserialize the MemberData Item only for Generic Parameter
		/// </summary>
		/// <param name="item"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static Type[] DeserializeItemGeneric(MemberData.ItemData item) {
			Type[] GType = Type.EmptyTypes;
			if(item != null) {
				if(item.genericArguments != null && item.genericArguments.Length > 0) {
					GType = new Type[item.genericArguments.Length];
					for(int i = 0; i < GType.Length; i++) {
						GType[i] = GetGenericType(item.genericArguments[i]);
					}
				}
			}
			return GType;
		}

		/// <summary>
		/// Deserialize the MemberData Item
		/// </summary>
		/// <param name="item"></param>
		/// <param name="targets"></param>
		/// <param name="ParameterType"></param>
		public static void DeserializeMemberItem(MemberData.ItemData item, out Type[] ParameterType) {
			Type[] GType = Type.EmptyTypes;
			Type[] PType = Type.EmptyTypes;
			if(item != null) {
				if(item.reference != null && (item.genericArguments == null || item.genericArguments.Length == 0)) {
					if(item.GetReferenceValue() is IParameterSystem parameterSystem) {
						PType = new Type[parameterSystem.Parameters.Count];
						for(int i = 0; i < parameterSystem.Parameters.Count; i++) {
							PType[i] = parameterSystem.Parameters[i].Type;
						}
					}
				}
				else {
					if(item.genericArguments != null && item.genericArguments.Length > 0) {
						GType = new Type[item.genericArguments.Length];
						for(int i = 0; i < GType.Length; i++) {
							GType[i] = GetGenericType(item.genericArguments[i]);
						}
					}
					if(item.parameters != null && item.parameters.Length > 0) {
						PType = new Type[item.parameters.Length];
						for(int i = 0; i < PType.Length; i++) {
							PType[i] = GetParameterType(item.parameters[i], GType);
						}
					}
				}
			}
			ParameterType = PType;
		}

		/// <summary>
		/// Deserialize the MemberData Item
		/// </summary>
		/// <param name="item"></param>
		/// <param name="targets"></param>
		/// <param name="GenericType"></param>
		/// <param name="ParameterType"></param>
		public static void DeserializeMemberItem(MemberData.ItemData item, out Type[] GenericType, out Type[] ParameterType, bool throwError = true) {
			Type[] GType = Type.EmptyTypes;
			Type[] PType = Type.EmptyTypes;
			if(item != null) {
				if(item.reference != null && (item.genericArguments == null || item.genericArguments.Length == 0)) {
					if(item.GetReferenceValue() is IParameterSystem parameterSystem) {
						PType = new Type[parameterSystem.Parameters.Count];
						for(int i = 0; i < parameterSystem.Parameters.Count; i++) {
							PType[i] = parameterSystem.Parameters[i].Type;
						}
					}
				}
				else {
					if(item.genericArguments != null) {
						GType = new Type[item.genericArguments.Length];
						for(int i = 0; i < GType.Length; i++) {
							GType[i] = GetGenericType(item.genericArguments[i], throwError);
						}
					}
					if(item.parameters != null) {
						PType = new Type[item.parameters.Length];
						for(int i = 0; i < PType.Length; i++) {
							PType[i] = GetParameterType(item.parameters[i], GType, throwError);
						}
					}
				}
			}
			GenericType = GType;
			ParameterType = PType;
		}

		/// <summary>
		/// Get the Type of Parameter.
		/// </summary>
		/// <param name="typeData"></param>
		/// <param name="types"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static Type GetParameterType(TypeData typeData, IList<Type> types = null, bool throwError = true) {
			int array = 0;
			string n = typeData.name;
			while(n.EndsWith("[]")) {
				n = n.Remove(n.Length - 2);
				array++;
			}
			Type t = null;
			switch(n[0]) {
				case '$': {//For generic param
					n = n.Remove(0, 1);
					GenericParameterData data = FindGenericData(n, typeData.reference);
					if(data != null) {
						t = data.value;
					}
					else {
						t = typeof(object);
					}
					break;
				}
				case '#': {//for parameter references by index
					n = n.Remove(0, 1);
					if(types != null) {
						t = types[int.Parse(n)];
					}
					else {
						t = typeof(object);
					}
					break;
				}
				case '[': {//for default runtime type references
					if(n == RUNTIME_ID) {
						var reference = typeData.reference?.ReferenceValue;
						if(reference != null) {
							if(reference is Type type) {
								if(array > 0) {
									return ReflectionUtils.MakeArrayType(type);
								}
								return type;
							}
							if(array > 0) {
								var result = ReflectionUtils.GetRuntimeType(reference);
								return ReflectionUtils.MakeArrayType(result);
							}
							return ReflectionUtils.GetRuntimeType(reference);
						}
					}
					else {
						goto default;
					}
					break;
				}
				case '!': {//Generic runtime type
					t = TypeSerializer.Deserialize(n.Remove(0, 1), throwError);
					if(t == null) {
						return RuntimeType.FromMissingType(n.Remove(0, 1));
					}
					if(t.IsGenericTypeDefinition && typeData.parameters != null && typeData.parameters.Length > 0) {
						Type[] T = new Type[typeData.parameters.Length];
						for(int i = 0; i < typeData.parameters.Length; i++) {
							T[i] = GetParameterType(typeData.parameters[i], types, throwError);
						}
						t = ReflectionFaker.FakeGenericType(t, T);
						while(array > 0) {
							t = ReflectionFaker.FakeArrayType(t);
							array--;
						}
						return t;
					}
					break;
				}
				case '?': {//Array runtime type
					return ReflectionUtils.MakeArrayType(GetParameterType(typeData.parameters[0], types, throwError));
				}
				case '&': {//By Ref Type
					return GetParameterType(typeData.parameters[0], types, throwError)?.MakeByRefType();
				}
				default: {
					t = TypeSerializer.Deserialize(n, throwError);
					if(t == null) {
						return RuntimeType.FromMissingType(n);
					}
					break;
				}
			}
			if(t == null)
				return null;
			if(t.IsGenericTypeDefinition && typeData.parameters != null && typeData.parameters.Length > 0) {
				Type[] T = new Type[typeData.parameters.Length];
				for(int i = 0; i < typeData.parameters.Length; i++) {
					T[i] = GetParameterType(typeData.parameters[i], types, throwError);
				}
				t = t.MakeGenericType(T);
			}
			while(array > 0) {
				t = t.MakeArrayType();
				array--;
			}
			return t;
		}

		/// <summary>
		/// Get the Type of Parameter.
		/// </summary>
		/// <param name="typeData"></param>
		/// <param name="types"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static System.Type GetParameterType(TypeData typeData, IList<GenericParameterData> types) {
			int array = 0;
			string n = typeData.name;
			while(n.EndsWith("[]")) {
				n = n.Remove(n.Length - 2);
				array++;
			}
			System.Type t = null;
			switch(n[0]) {
				case '$': {//For generic param
					n = n.Remove(0, 1);
					GenericParameterData data = FindGenericData(n, typeData.reference);
					if(data != null) {
						t = data.value;
					}
					else {
						t = typeof(object);
					}
					break;
				}
				case '#': {//for parameter references by index
					n = n.Remove(0, 1);
					if(types != null) {
						t = types[int.Parse(n)].value;
					}
					else {
						t = typeof(object);
					}
					break;
				}
				case '[': {//for default runtime type references
					if(n == RUNTIME_ID) {
						var reference = typeData.reference?.ReferenceValue;
						if(reference != null) {
							if(reference is Type type) {
								if(array > 0) {
									return ReflectionUtils.MakeArrayType(type);
								}
								return type;
							}
							if(array > 0) {
								var result = ReflectionUtils.GetRuntimeType(reference);
								return ReflectionUtils.MakeArrayType(result);
							}
							return ReflectionUtils.GetRuntimeType(reference);
						}
					}
					else {
						goto default;
					}
					break;
				}
				case '!': {//Generic runtime type
					t = TypeSerializer.Deserialize(n.Remove(0, 1));
					if(t.IsGenericTypeDefinition && typeData.parameters != null && typeData.parameters.Length > 0) {
						Type[] T = new Type[typeData.parameters.Length];
						for(int i = 0; i < typeData.parameters.Length; i++) {
							T[i] = GetParameterType(typeData.parameters[i], types);
						}
						t = ReflectionFaker.FakeGenericType(t, T);
						while(array > 0) {
							t = ReflectionFaker.FakeArrayType(t);
							array--;
						}
						return t;
					}
					break;
				}
				case '?': {//Array runtime type
					return ReflectionFaker.FakeArrayType(GetParameterType(typeData.parameters[0], types));
				}
				case '&': {//By Ref Type
					return GetParameterType(typeData.parameters[0], types)?.MakeByRefType();
				}
				default: {
					t = TypeSerializer.Deserialize(n);
					break;
				}
			}
			if(t.IsGenericTypeDefinition && typeData.parameters != null && typeData.parameters.Length > 0) {
				Type[] T = new Type[typeData.parameters.Length];
				for(int i = 0; i < typeData.parameters.Length; i++) {
					T[i] = GetParameterType(typeData.parameters[i], types);
				}
				t = t.MakeGenericType(T);
			}
			while(array > 0) {
				t = t.MakeArrayType();
				array--;
			}
			return t;
		}

		/// <summary>
		/// Find a GenericParameterData by name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="targets"></param>
		/// <returns></returns>
		public static GenericParameterData FindGenericData(string name, BaseReference reference) {
			if(reference == null)
				return null;
			if(reference is FunctionRef) {
				var function = (reference as FunctionRef).reference;
				return function.GetGenericParameter(name);
			}
			else if(reference is IGenericParameterSystem) {
				var system = reference as IGenericParameterSystem;
				return system.GetGenericParameter(name);
			}
			return null;
		}

		/// <summary>
		/// Get the Type of Generic Parameter.
		/// </summary>
		/// <param name="genericData"></param>
		/// <returns></returns>
		public static System.Type GetGenericType(TypeData genericData, bool throwError = true) {
			int array = 0;
			string n = genericData.name;
			while(n.EndsWith("[]")) {
				n = n.Remove(n.Length - 2);
				array++;
			}
			System.Type t = null;
			switch(genericData.name[0]) {
				case '$': {//For generic param
					GenericParameterData data = FindGenericData(n.Remove(0, 1), genericData.reference);
					if(data != null) {
						t = data.value;
					}
					else {
						t = typeof(object);
					}
					break;
				}
				case '[': {//for default runtime type references
					if(n == RUNTIME_ID) {
						var reference = genericData.reference.ReferenceValue;
						if(reference != null) {
							if(reference is Type type) {
								if(array > 0) {
									return ReflectionUtils.MakeArrayType(type);
								}
								return type;
							}
							if(array > 0) {
								var result = ReflectionUtils.GetRuntimeType(reference);
								return ReflectionUtils.MakeArrayType(result);
							}
							return ReflectionUtils.GetRuntimeType(reference);
						}
					}
					else {
						goto default;
					}
					break;
				}
				case '!': {//Generic runtime type
					t = TypeSerializer.Deserialize(n.Remove(0, 1), throwError);
					if(t == null) {
						return RuntimeType.FromMissingType(n.Remove(0, 1));
					}
					if(t.IsGenericTypeDefinition && genericData.parameters != null && genericData.parameters.Length > 0) {
						Type[] T = new Type[genericData.parameters.Length];
						for(int i = 0; i < genericData.parameters.Length; i++) {
							var GP = GetGenericType(genericData.parameters[i], throwError);
							if(GP is MissingType) {
								return GP;
							}
							T[i] = GP;
						}
						t = ReflectionFaker.FakeGenericType(t, T);
					}
					break;
				}
				case '?': {//Array runtime type
					t = GetGenericType(genericData.parameters[0], throwError);
					if(t is MissingType) {
						return t;
					}
					t = ReflectionFaker.FakeArrayType(t);
					break;
				}
				case '&': {//By Ref Type
					t = GetGenericType(genericData.parameters[0], throwError);
					if(t is MissingType) {
						return t;
					}
					t = t.MakeByRefType();
					break;
				}
				default: {
					t = TypeSerializer.Deserialize(n, throwError);
					if(t == null) {
						return RuntimeType.FromMissingType(n);
					}
					break;
				}
			}
			if(t != null && t.IsGenericTypeDefinition && genericData.parameters != null && genericData.parameters.Length > 0) {
				Type[] T = new Type[genericData.parameters.Length];
				for(int i = 0; i < genericData.parameters.Length; i++) {
					var GP = GetGenericType(genericData.parameters[i], throwError);
					if(GP is MissingType) {
						return GP;
					}
					T[i] = GP;
				}
				t = t.MakeGenericType(T);
			}
			while(array > 0) {
				t = t.MakeArrayType();
				array--;
			}
			return t;
		}

		/// <summary>
		/// Create list of type data from Members.
		/// </summary>
		/// <param name="members"></param>
		/// <returns></returns>
		public static List<TypeData> MakeTypeDatas(IList<MemberData> members) {
			List<TypeData> typeDatas = new List<TypeData>();
			foreach(MemberData member in members) {
				if(member.genericData != null) {
					typeDatas.Add(member.genericData);
				}
				else {
					typeDatas.Add(GetTypeData(member));
				}
			}
			return typeDatas;
		}

		// private static List<MemberData.ItemData> BuildItemFromMemberInfos(IEnumerable<MemberInfo> members) {
		// 	return null;
		// }


		private static MemberData.ItemData BuildItemFromMemberInfo(MemberInfo member) {
			MemberData.ItemData iData = null;
			MethodBase methodInfo = member as MethodBase;
			if(methodInfo != null) {
				Type[] genericMethodArgs = Type.EmptyTypes;
				if(methodInfo.MemberType != MemberTypes.Constructor) {
					if(genericMethodArgs != null && genericMethodArgs.Length > 0) {
						TypeData[] param = new TypeData[genericMethodArgs.Length];
						for(int i = 0; i < genericMethodArgs.Length; i++) {
							param[i] = MemberDataUtility.GetTypeData(genericMethodArgs[i], null);
						}
						iData = new MemberData.ItemData() { genericArguments = param };
					}
				}
				ParameterInfo[] paramsInfo = methodInfo.GetParameters();
				if(paramsInfo.Length > 0) {
					TypeData[] paramData = new TypeData[paramsInfo.Length];
					for(int x = 0; x < paramsInfo.Length; x++) {
						TypeData gData = MemberDataUtility.GetTypeData(paramsInfo[x],
							genericMethodArgs != null ? genericMethodArgs.Select(it => it.Name).ToArray() : null);
						paramData[x] = gData;
					}
					if(iData == null) {
						iData = new MemberData.ItemData();
					}
					iData.parameters = paramData;
				}
			}
			return iData;
		}

		public static object GetActualInstance(MemberData member) {
			switch(member.targetType) {
				case MemberData.TargetType.uNodeVariable:
				case MemberData.TargetType.uNodeLocalVariable:
					return MemberData.CreateFromValue(member.startItem.GetReferenceValue() as Variable);
				case MemberData.TargetType.uNodeProperty:
					return MemberData.CreateFromValue(member.startItem.GetReferenceValue() as Property);
				case MemberData.TargetType.uNodeParameter:
					var parameterRef = member.startItem.GetReference<ParameterRef>();
					return MemberData.CreateFromValue(parameterRef);
			}
			return member.instance;
		}
	}
}