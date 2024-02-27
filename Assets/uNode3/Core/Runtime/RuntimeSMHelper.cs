using UnityEngine;
using System.Collections;
using MaxyGames.UNode;

namespace MaxyGames.Runtime {
	[AddComponentMenu("")]
	public class RuntimeSMHelper : MonoBehaviour {
		private static RuntimeSMHelper _instance;
		public static RuntimeSMHelper Instance {
			get {
				if(_instance == null) {
					_instance = FindObjectOfType<RuntimeSMHelper>();
					if(_instance == null) {
						GameObject go = new GameObject("Helper");
						_instance = go.AddComponent<RuntimeSMHelper>();
					}
				}
				return _instance;
			}
		}
	}
}