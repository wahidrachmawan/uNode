using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[AddComponentMenu("uNode/Singleton Initializer")]
    public class SingletonInitializer : MonoBehaviour {
        public GraphSingleton target;

		/// <summary>
		/// The list of variable.
		/// </summary>
		[HideInInspector]
		public List<VariableData> variables = new List<VariableData>();

		void Awake() {
			if(target == null) {
				return;
			}
			target.EnsureInitialized();
			//Initialize the variable
			for(int x = 0; x < variables.Count; x++) {
				target.SetVariable(variables[x].name, variables[x].Get());
			}
		}
	}
}