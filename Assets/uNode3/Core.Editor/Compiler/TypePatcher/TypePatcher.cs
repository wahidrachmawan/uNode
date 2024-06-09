using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MaxyGames.UNode.Editors {
	public static class TypePatcher {
		private static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		private static bool IsSameParameters(ParameterInfo[] sourceParams, ParameterInfo[] targetParams) {
			if(sourceParams.Length == targetParams.Length) {
				for(int i = 0; i < sourceParams.Length; i++) {
					if(sourceParams[i].ParameterType != targetParams[i].ParameterType) {
						return false;
					}
				}
				return true;
			}
			return false;
		}

		public static void RestorePatch() {
			foreach(var pair in patcherMap) {
				pair.Value?.Invoke();
			}
			patcherMap.Clear();
		}

		public static void Patch(Type original, Type target) {
			DoPatch(original, target);
		}

		private static System.Collections.Generic.Dictionary<Type, Action> patcherMap = new System.Collections.Generic.Dictionary<Type, Action>();

		private static void DoPatch(Type original, Type target) {
			Action restoreAction = null;
			//Swap methods
			var originalMethods = original.GetMethods(flags);
			var targetMethods = target.GetMethods(flags);
			foreach(var originalMethod in originalMethods) {
				//if(originalMethod.IsVirtual) {
				//	continue;
				//}
				var rType = originalMethod.ReturnType;
				if(rType == typeof(System.Collections.IEnumerable) || 
					rType == typeof(System.Collections.IEnumerator) ||
					rType.HasImplementInterface(typeof(System.Collections.Generic.IEnumerator<>)) || 
					rType.HasImplementInterface(typeof(System.Collections.Generic.IEnumerable<>))) {
					continue;
				}
				if(originalMethod.GetCustomAttribute(typeof(CompilerGeneratedAttribute), true) != null) {
					continue;
				}
				var targetMethod = targetMethods.FirstOrDefault(m =>
					m.Name == originalMethod.Name &&
					m.ReturnType == originalMethod.ReturnType &&
					IsSameParameters(m.GetParameters(), originalMethod.GetParameters()));
				if(targetMethod != null) {
					SwapMethod(originalMethod, targetMethod);
				}
			}
			////Swap constructors
			//var originalCtors = original.GetConstructors(flags);
			//var targetCtors = target.GetConstructors(flags);
			//foreach(var originalMethod in originalCtors) {
			//	var targetMethod = targetCtors.FirstOrDefault(m => IsSameParameters(m.GetParameters(), originalMethod.GetParameters()));
			//	if(targetMethod != null) {
			//		restoreAction += SwapMethod(originalMethod, targetMethod);
			//	}
			//}
			//Swap properties
			var originalProps = original.GetProperties(flags);
			foreach(var oProperty in originalProps) {
				var tProperty = target.GetProperty(oProperty.Name, flags);
				if(tProperty != null) {
					if(oProperty.GetGetMethod(true) != null && tProperty.GetGetMethod(true) != null) {
						if(oProperty.GetGetMethod(true).GetCustomAttribute(typeof(CompilerGeneratedAttribute), true) != null) {
							continue; //Ensure to skip auto property
						}
						SwapMethod(oProperty.GetGetMethod(true), tProperty.GetGetMethod(true));
					}
					if(oProperty.GetSetMethod(true) != null && tProperty.GetSetMethod(true) != null) {
						if(oProperty.GetSetMethod(true).GetCustomAttribute(typeof(CompilerGeneratedAttribute), true) != null) {
							continue; //Ensure to skip auto property
						}
						SwapMethod(oProperty.GetSetMethod(true), tProperty.GetSetMethod(true));
					}
				}
			}
			patcherMap[original] = restoreAction;
			//var originalFields = original.GetFields(flags);
			//var targetFields = target.GetFields(flags);
			//foreach(var tField in targetFields) {
			//	bool flag = true;
			//	foreach(var oField in originalFields) {
			//		if(tField.Name.Equals(oField)) {
			//			flag = false;
			//			break;
			//		}
			//	}
			//	if(flag) {

			//	}
			//}
		}

		public static void SwapMethod(MethodBase original, MethodBase target) {
			RuntimeHelpers.PrepareMethod(original.MethodHandle);
			IntPtr pBody = original.MethodHandle.GetFunctionPointer();

			RuntimeHelpers.PrepareMethod(target.MethodHandle);
			IntPtr pBorrowed = target.MethodHandle.GetFunctionPointer();

			unsafe {
				var ptr = (byte*)pBody.ToPointer();
				var ptr2 = (byte*)pBorrowed.ToPointer();
				DoPatch(ptr, ptr2);
				//UnityEngine.Debug.LogFormat("Patched 0x{0:X} to 0x{1:X}.", (ulong)ptr, (ulong)ptr2);
			}
		}

		private static unsafe void DoPatch(byte* source, byte* destination) {
			var ptr = source;
			var ptr2 = destination;
			var ptrDiff = ptr2 - ptr - 5;
			if(ptrDiff < (long)0xFFFFFFFF && ptrDiff > (long)-0xFFFFFFFF) {
				// 32-bit relative jump, available on both 32 and 64 bit arch.
				*ptr = 0xe9; // JMP
				*((uint*)(ptr + 1)) = (uint)ptrDiff;
			} else {
				if(Environment.Is64BitProcess) {
					// For 64bit arch and likely 64bit pointers, do:
					// PUSH bits 0 - 32 of addr
					// MOV [RSP+4] bits 32 - 64 of addr
					// RET
					var cursor = ptr;
					*(cursor++) = 0x68; // PUSH
					*((uint*)cursor) = (uint)ptr2;
					cursor += 4;
					*(cursor++) = 0xC7; // MOV [RSP+4]
					*(cursor++) = 0x44;
					*(cursor++) = 0x24;
					*(cursor++) = 0x04;
					*((uint*)cursor) = (uint)((ulong)ptr2 >> 32);
					cursor += 4;
					*(cursor++) = 0xc3; // RET
				} else {
					// For 32bit arch and 32bit pointers, do: PUSH addr, RET.
					*ptr = 0x68;
					*((uint*)(ptr + 1)) = (uint)ptr2;
					*(ptr + 5) = 0xC3;
				}
			}
		}
	}
}
