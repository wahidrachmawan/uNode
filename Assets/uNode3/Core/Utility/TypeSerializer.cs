using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.UNode {
	/// <summary>
	/// An utility class to simply serialize and deserialize .NET types in a Unity context.
	/// </summary>
	public static class TypeSerializer {
		private static Dictionary<string, Type> types;
		private static object _lockObject = new object();

		public static void CleanCache() {
			lock(_lockObject) {
				types = new Dictionary<string, Type>();
			}
		}

		public static RuntimeType GetRuntimeType(string fullGraphName) {
			var graph = uNodeUtility.GetDatabase().GetGraphByUID(fullGraphName, false);
			if(graph is IReflectionType type) {
				return type.ReflectionType;
			}
			return null;
		}

		/// <summary>
		/// An extension for convert string to System.Type
		/// </summary>
		/// <param name="str"></param>
		/// <param name="throwException"></param>
		/// <returns></returns>
		public static Type ToType(this string str, bool throwException = true) {
			return Deserialize(str, throwException);
		}

		/// <summary>
		/// Function to Serialize Type to string
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string Serialize(Type type) {
			return type.FullName;
		}

		/// <summary>
		/// Function to Deserialize Type from string
		/// </summary>
		/// <param name="fullName"></param>
		/// <param name="throwException"></param>
		/// <returns></returns>
		public static Type Deserialize(string fullName, bool throwException = true) {
			if(string.IsNullOrEmpty(fullName)) {
				if(throwException)
					throw new Exception("Failed to deserialize type: " + fullName);
				else
					return null;
			}
			Type type = null;
			lock(_lockObject) {
				if(types == null) {
					types = new Dictionary<string, Type>();
				}
				if(types.TryGetValue(fullName, out type)) {
					return type;
				}
			}
			type = Type.GetType(fullName, false, false);
			if(type != null) {
				lock(_lockObject) {
					types[fullName] = type;
				}
				return type;
			}
			var runtimeAssemblies = ReflectionUtils.GetRuntimeAssembly();
			foreach(var asm in runtimeAssemblies) {
				type = asm.GetType(fullName);

				if(type != null) {
					lock(_lockObject) {
						types[fullName] = type;
					}
					return type;
				}
			}
			var loadedAssemblies = ReflectionUtils.GetAssemblies();
			foreach(var asm in loadedAssemblies) {
				type = asm.GetType(fullName);

				if(type != null) {
					lock(_lockObject) {
						types[fullName] = type;
					}
					return type;
				}
			}
			//Fix different assembly version on generic type.
			if(fullName.Contains('`')) {
				string[] data1 = fullName.Split(new char[] { '`' }, 2);
				if(data1.Length == 2) {
					int deepLevel = 0;
					int step = -1;
					int gLength = 0;
					bool skip = false;
					List<char> listChar = new List<char>();
					List<Type> gTypes = new List<Type>();
					for(int i = 0; i < data1[1].Length; i++) {
						char c = data1[1][i];
						if(skip) {
							//Continue skip until end of block
							if(c != ']') {
								continue;
							} else {
								skip = false;
							}
						}
						if(c == '[') {
							if(deepLevel == 0 && listChar.Count > 0) {
								gLength = int.Parse(string.Join("", new string[] { new string(listChar.ToArray()) }));
							}
							deepLevel++;
							if(deepLevel >= 3) {
								listChar.Add(c);
							} else {
								listChar.Clear();
							}
						} else if(c == ']') {
							if(deepLevel == 2) {
								//UnityEngine.Debug.Log(string.Join("", listChar.ToArray()));
								var cType = Deserialize(string.Join("", new string[] { new string(listChar.ToArray()) }), throwException);
								if(cType == null)
									return null;
								//UnityEngine.Debug.Log(cType);
								gTypes.Add(cType);
								//Debug.Log(string.Join("", listChar.ToArray()).Split(',')[0]);
								listChar.Clear();
							} else if(deepLevel >= 3) {
								listChar.Add(c);
							} else if(deepLevel == 1) {//An array handling
								step++;
							}
							deepLevel--;
						} else {
							if(c == ',' && deepLevel == 2) {
								skip = true;
							} else {
								listChar.Add(c);
							}
						}
					}
					if(gLength == 0) {
						if(throwException)
							throw new Exception("Failed to deserialize type: " + fullName);
						else
							return null;
					}
					var dType = Deserialize(data1[0] + "`" + gLength.ToString(), false);
					if(dType == null) {//Fallback for fail deserialization
						return type;
					}
					type = dType.MakeGenericType(gTypes.ToArray());
					while(step > 0) {
						type = type.MakeArrayType();
						step--;
					}
					lock(_lockObject) {
						types[fullName] = type;
					}
					return type;
				}
			}
			//Debug.LogWarning(fullName + " Type not found");
			if(throwException)
				throw new Exception("Failed to deserialize type: " + fullName);
			else
				return null;
		}
	}
}