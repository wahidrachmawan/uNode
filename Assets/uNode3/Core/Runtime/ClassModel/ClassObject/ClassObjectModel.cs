using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[Serializable]
	public class ClassObjectModel : ClassDefinitionModel {
		public override string title => "Class Object";

		public override Type InheritType => typeof(object);

		public override Type ScriptInheritType => typeof(RuntimeObject);

		public override Type ProxyScriptType => typeof(BaseRuntimeObject);

		public override object CreateInstance(string graphUID) {
			return Create(graphUID);
		}

		public override object CreateWrapperInstance(string graphUID) {
			return CreateWrapper(graphUID, false);
		}

		//For code generation to create correct instance.
		public static BaseRuntimeObject Create(string graphUID) {
			//Get the native c# type
			var type = graphUID.ToType(false);
			if(type != null) {
				//Get the actual graph by id
				var target = uNodeUtility.GetDatabase().GetGraphByUID(graphUID) as ClassDefinition;
				//Instance native c# graph
				var instance = Activator.CreateInstance(type) as RuntimeObject;
				//Initialize the references
				var references = target.scriptData.unityObjects;
				for(int i = 0; i < references.Count; i++) {
					instance.SetVariable(references[i].name, references[i].value);
				}
				return instance;
			}
			else {
				return CreateWrapper(graphUID);
			}
		}

		public static ClassObject CreateWrapper(string graphUID, bool initialize = true) {
			//Get the actual graph by id
			var target = uNodeUtility.GetDatabase().GetGraphByUID(graphUID) as ClassDefinition;
			//Create the instance
			var result = new ClassObject();
			result.target = target;
			if(initialize) {
				result.EnsureInitialized();
			}
			return result;
		}
	}
}