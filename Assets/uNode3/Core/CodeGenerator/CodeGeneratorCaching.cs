using MaxyGames.UNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MaxyGames {
	public static partial class CG {
        /// <summary>
		/// Mark object as initialized with specific ID so it never be entered again by using HasInitialized().
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="id"></param>
		public static void SetInitialized(object owner, int id = 0) {
			HashSet<int> hash;
			if(!generatorData.initializedUserObject.TryGetValue(owner, out hash)) {
				hash = new HashSet<int>();
				generatorData.initializedUserObject[owner] = hash;
			}
			if(!hash.Contains(id)) {
				hash.Add(id);
			}
		}

		/// <summary>
		/// Are the owner with specific ID has been initialized?
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public static bool HasInitialized(object owner, int id = 0) {
			HashSet<int> hash;
			if(generatorData.initializedUserObject.TryGetValue(owner, out hash)) {
				return hash.Contains(id);
			}
			return false;
		}

        #region GetVariableName
		/// <summary>
		/// Get the variable name from variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static string GetVariableName(Variable variable) {
			foreach(VData vdata in generatorData.variables) {
				if(object.ReferenceEquals(vdata.reference, variable)) {
					if(generatorData.GetVariableNameCallack != null) {
						var str = generatorData.GetVariableNameCallack(vdata);
						if(str != null) {
							return str;
						}
					}
					return vdata.name;
				}
			}
			string name = GenerateNewName(variable.name);
			var data = generatorData.AddVariable(new VData(name, variable.type, isInstance: false) { 
				reference = variable, 
				modifier = variable.modifier, 
				defaultValue = variable.defaultValue 
			});
			if(generatorData.GetVariableNameCallack != null) {
				var rezult = generatorData.GetVariableNameCallack(data);
				if(rezult != null) {
					return rezult;
				}
			}
			return name;
		}

		/// <summary>
		/// Get the variable name from variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static string GetVariableName(VariableData variable) {
			foreach (VData vdata in generatorData.variables) {
				if (object.ReferenceEquals(vdata.reference, variable)) {
					if(generatorData.GetVariableNameCallack != null) {
						var rezult = generatorData.GetVariableNameCallack(vdata);
						if(rezult != null) {
							return rezult;
						}
					}
					return vdata.name;
				}
			}
			var name = GenerateNewName(variable.name);
			var data = generatorData.AddVariable(new VData(name, variable.type, isInstance: false) { 
				reference = variable, 
				modifier = variable.modifier,
				defaultValue = variable.value,
			});
			if(generatorData.GetVariableNameCallack != null) {
				var rezult = generatorData.GetVariableNameCallack(data);
				if(rezult != null) {
					return rezult;
				}
			}
			return name;
		}

		/// <summary>
		/// Get the variable name from variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static string GetVariableName(VData variable) {
			var rezult = generatorData.GetVariableNameCallack?.Invoke(variable);
			if(rezult != null) {
				return rezult;
			}
			return variable.name;
		}

		/// <summary>
		/// Get the variable name from ValueOutput.
		/// The variable need to be registered first using <see cref="RegisterVariable"/>.
		/// </summary>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static string GetVariableName(ValueOutput reference) {
			foreach(VData vdata in generatorData.variables) {
				if(object.ReferenceEquals(vdata.reference, reference)) {
					var rezult = generatorData.GetVariableNameCallack?.Invoke(vdata);
					if(rezult != null) {
						return rezult;
					}
					return vdata.name;
				}
			}
			throw new Exception($"The port doesn't have registered variable. Use (CG.{nameof(RegisterVariable)}) to register it.");
		}

		/// <summary>
		/// Check if the <paramref name="reference"/> has been registered variable
		/// </summary>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static bool HasRegisteredVariable(ValueOutput reference) {
			foreach(VData vdata in generatorData.variables) {
				if(object.ReferenceEquals(vdata.reference, reference)) {
					return true;
				}
			}
			return false;
		}
		#endregion

		#region GenerateVariableName
		/// <summary>
		/// Generate new unique variable name ( auto correct wrong names )
		/// </summary>
		/// <returns></returns>
		public static string GenerateNewName(string name) {
			if(string.IsNullOrEmpty(name)) {
				name = "variable";
			}
			name = uNodeUtility.AutoCorrectName(name);
			if(generatorData.VarNames.ContainsKey(name)) {
				string result;
				while(true) {
					result = name + (++generatorData.VarNames[name]).ToString();
					if(!generatorData.VarNames.ContainsKey(result)) {
						break;
					}
				}
				return result;
			} else {
				generatorData.VarNames.Add(name, 0);
				return name;
			}
		}

		/// <summary>
		/// Generate new unique variable name ( auto correct wrong names )
		/// </summary>
		/// <param name="name"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static string GenerateName(string name, object owner) {
			return GenerateName(name, owner, out _);
		}

		/// <summary>
		/// Generate new unique variable name ( auto correct wrong names )
		/// </summary>
		/// <param name="name"></param>
		/// <param name="owner"></param>
		/// <param name="isNew"></param>
		/// <returns></returns>
		public static string GenerateName(string name, object owner, out bool isNew) {
			if(owner != null) {
				Dictionary<string, string> map;
				if(generatorData.variableNamesMap.TryGetValue(owner, out map)) {
					string result;
					if(map.TryGetValue(name, out result)) {
						isNew = false;
						return result;
					}
					else {
						result = GenerateNewName(name);
						map.Add(name, result);
						isNew = true;
						return result;
					}
				}
				else {
					map = new Dictionary<string, string>();
					generatorData.variableNamesMap[owner] = map;
					string result = GenerateNewName(name);
					map.Add(name, result);
					isNew = true;
					return result;
				}
			}
			isNew = true;
			return GenerateNewName(name);
		}
		#endregion

		#region UserObject
		/// <summary>
		/// Register new user object data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static T RegisterUserObject<T>(T value, object owner) {
			generatorData.userObjectMap[owner] = value;
			return value;
		}

		/// <summary>
		/// Get user object data.
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static object GetUserObject(object owner) {
			if(generatorData.userObjectMap.TryGetValue(owner, out var result)) {
				return result;
			}
			return null;
		}

		/// <summary>
		/// Register new user object data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static T RegisterUserObject<T>(T value, object owner, string key) {
			generatorData.userObjectMap[(owner, key)] = value;
			return value;
		}

		/// <summary>
		/// Get user object data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static T GetUserObject<T>(object owner) {
			if(generatorData.userObjectMap.TryGetValue(owner, out var result)) {
				try {
					return (T)result;
				}
				catch (InvalidCastException) {
					throw new Exception($"Cannot cast {result.GetType()} to {typeof(T)}");
				}
			}
			return default(T);
		}

		/// <summary>
		/// Tries to retrieve the user object of type T associated with the specified owner.
		/// </summary>
		/// <typeparam name="T">The type of the user object to retrieve.</typeparam>
		/// <param name="owner">The owner object that may contain an associated user object.</param>
		/// <param name="value">When the method returns, contains the retrieved user object of type T if found; otherwise, the default value for
		/// T.</param>
		/// <returns>true if a user object of type T was found; otherwise, false.</returns>
		public static bool TryGetUserObject<T>(object owner, out T value) {
			if(generatorData.userObjectMap.TryGetValue(owner, out var result)) {
				try {
					value = (T)result;
					return true;
				}
				catch(InvalidCastException) {
					throw new Exception($"Cannot cast {result.GetType()} to {typeof(T)}");
				}
			}
			value = default;
			return false;
		}

		/// <summary>
		/// Get user object data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static T GetUserObject<T>(object owner, string key) {
			if(generatorData.userObjectMap.TryGetValue((owner, key), out var result)) {
				try {
					return (T)result;
				}
				catch(InvalidCastException) {
					throw new Exception($"Cannot cast {result.GetType()} to {typeof(T)}");
				}
			}
			return default(T);
		}

		/// <summary>
		/// Get user object data.
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static object GetUserObject(object owner, string key) {
			if(generatorData.userObjectMap.TryGetValue((owner, key), out var result)) {
				return result;
			}
			return null;
		}

		/// <summary>
		/// Get user object data if exist otherwise register new user object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static T GetOrRegisterUserObject<T>(T value, object owner) {
			if(generatorData.userObjectMap.ContainsKey(owner)) {
				return (T)generatorData.userObjectMap[owner];
			}
			return RegisterUserObject(value, owner);
		}

		/// <summary>
		/// Get user object data if exist otherwise register new user object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static T GetOrRegisterUserObject<T>(T value, object owner, string key) {
			if(generatorData.userObjectMap.ContainsKey((owner, key))) {
				return (T)generatorData.userObjectMap[(owner, key)];
			}
			return RegisterUserObject(value, owner);
		}

		/// <summary>
		/// Are the owner has user object data.
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static bool HasUserObject(object owner) {
			return generatorData.userObjectMap.ContainsKey(owner);
		}

		/// <summary>
		/// Are the owner has user object data.
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static bool HasUserObject(object owner, string key) {
			return generatorData.userObjectMap.ContainsKey((owner, key));
		}
		#endregion

		#region Get Functions
		public static MData GetOrRegisterFunction(string name, Type returnType, params Type[] parameterTypes) {
			var param = new System.Type[parameterTypes.Length];
			for(int i=0;i<parameterTypes.Length;i++) {
				param[i] = parameterTypes[i];
			}
			return GetOrRegisterFunction(name, returnType, param as IList<Type>);
		}

		public static MData GetOrRegisterFunction(string name, Type returnType, IList<Type> parameterTypes) {
			var mData = generatorData.GetMethodData(name, parameterTypes);
			if(mData == null) {
				mData = generatorData.AddMethod(name, returnType, parameterTypes);
			}
			return mData;
		}
		#endregion
	}
}