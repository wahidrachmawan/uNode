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
			foreach(VData vdata in generatorData.GetVariables()) {
				if(object.ReferenceEquals(vdata.reference, variable)) {
					return vdata.name;
				}
			}
			string name = GenerateNewName(variable.name);
			generatorData.AddVariable(new VData(name, variable.type, isInstance: false, autoCorrection: false) { 
				name = name, 
				reference = variable, 
				modifier = variable.modifier, 
				defaultValue = variable.defaultValue 
			});
			return name;
		}

		/// <summary>
		/// Get the variable name from variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static string GetVariableName(VariableData variable) {
			foreach (VData vdata in generatorData.GetVariables()) {
				if (object.ReferenceEquals(vdata.reference, variable)) {
					return vdata.name;
				}
			}
			var name = GenerateNewName(variable.name);
			generatorData.AddVariable(new VData(name, variable.type, isInstance: false, autoCorrection: false) { 
				name = name, 
				reference = variable, 
				modifier = variable.modifier,
				defaultValue = variable.value,
			});
			return name;
		}

		/// <summary>
		/// Get the variable name from ValueOutput.
		/// The variable need to be registered first.
		/// </summary>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static string GetVariableName(ValueOutput reference) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(object.ReferenceEquals(vdata.reference, reference)) {
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
			foreach(VData vdata in generatorData.GetVariables()) {
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
			if(owner != null) {
				Dictionary<string, string> map;
				if(generatorData.variableNamesMap.TryGetValue(owner, out map)) {
					string result;
					if(map.TryGetValue(name, out result)) {
						return result;
					} else {
						result = GenerateNewName(name);
						map.Add(name, result);
						return result;
					}
				} else {
					map = new Dictionary<string, string>();
					generatorData.variableNamesMap[owner] = map;
					string result = GenerateNewName(name);
					map.Add(name, result);
					return result;
				}
			}
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
		/// Get user object data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static T GetUserObject<T>(object owner) {
			if(generatorData.userObjectMap.TryGetValue(owner, out var result)) {
				return (T)result;
			}
			return default(T);
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
		/// Are the owner has user object data.
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static bool HasUserObject(object owner) {
			return generatorData.userObjectMap.ContainsKey(owner);
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