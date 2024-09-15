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
#if UNITY_6000_0_OR_NEWER
					_instance = FindAnyObjectByType<RuntimeSMHelper>();
#else
					_instance = FindObjectOfType<RuntimeSMHelper>();
#endif
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