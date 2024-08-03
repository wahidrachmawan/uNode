using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MaxyGames.UNode {
	public static class ReflectionFaker {
		static Dictionary<int, FakeType> genericFakeTypes = new Dictionary<int, FakeType>();

		public static FakeType FakeGenericType(Type type, Type[] typeArguments) {
			if(typeArguments == null) {
				throw new ArgumentNullException(nameof(typeArguments));
			}
			if(type.IsConstructedGenericType) {
				//throw new ArgumentException("Invalid type: a type must non-constructed type", nameof(type));
				type = type.GetGenericTypeDefinition();
			}
			int hash = 23;
			hash = hash * 31 + type.GetHashCode();
			for(int i = 0; i < typeArguments.Length; i++) {
				if(typeArguments[i] == null) {
					throw new ArgumentNullException(nameof(typeArguments), "Null type argument at index:" + i);
				}
				hash = hash * 31 + typeArguments[i].GetHashCode();
			}
			if(!genericFakeTypes.TryGetValue(hash, out var result)) {
				result = new GenericFakeType(type, typeArguments);
				genericFakeTypes[hash] = result;
			}
			return result;
		}

		internal static FakeNativeMethod FakeGenericMethod(MethodInfo method, Type[] typeArguments) {
			if(method is not IRuntimeMember) {
				int hash = typeArguments.Length;
				for(int i = 0; i < typeArguments.Length; i++) {
					if(typeArguments[i] == null) {
						throw new ArgumentNullException(nameof(typeArguments), "Null type argument at index:" + i);
					}
					hash ^= typeArguments[i].GetHashCode();
				}
				if(!fakeNativeMethods.TryGetValue(method, out var map)) {
					map = new Dictionary<int, FakeNativeMethod>();
					fakeNativeMethods[method] = map;
				}
				if(!map.TryGetValue(hash, out var result)) {
					result = new FakeNativeMethod(method, typeArguments);
					map[hash] = result;
					return result;
				}
				return result;
			}
			return null;
		}

		static Dictionary<MethodInfo, Dictionary<int, FakeNativeMethod>> fakeNativeMethods = new Dictionary<MethodInfo, Dictionary<int, FakeNativeMethod>>();
		static Dictionary<Type, Type> arrayFakeTypes = new Dictionary<Type, Type>();

		public static Type FakeArrayType(Type type) {
			if(!arrayFakeTypes.TryGetValue(type, out var result)) {
				result = new ArrayFakeType(type);
				arrayFakeTypes[type] = result;
			}
			return result;
		}

		public static FakeType FakeActionDelegate(Type[] typeArguments) {
			return FakeGenericType(("System.Action`" + typeArguments.Length).ToType(), typeArguments);
		}

		public static FakeType FakeFuncDelegate(Type returnType, Type[] typeArguments) {
			if(typeArguments.Length == 0) {
				return FakeGenericType(typeof(Func<>), new[] { returnType });
			}
			var types = new Type[typeArguments.Length + 1];
			for(int i=0;i<typeArguments.Length;i++) {
				types[i] = typeArguments[i];
			}
			types[types.Length - 1] = returnType;
			return FakeGenericType(("System.Func`" + typeArguments.Length + 1).ToType(), types);
		}

		public static FakeType FakeFuncDelegate(Type[] typeArguments) {
			if(typeArguments.Length == 1) {
				return FakeGenericType(typeof(Func<>), typeArguments);
			}
			return FakeGenericType(("System.Func`" + typeArguments.Length).ToType(), typeArguments);
		}

	}
}