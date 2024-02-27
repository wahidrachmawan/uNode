using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[Serializable]
	public class ClassComponentModel : ClassDefinitionModel {
		public override string title => "Class Component";

		public override Type InheritType => typeof(MonoBehaviour);

		public override Type ScriptInheritType => typeof(RuntimeBehaviour);

		public override Type ProxyScriptType => typeof(BaseRuntimeBehaviour);
	}
}