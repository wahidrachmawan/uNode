using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[Serializable]
	public class ClassAssetModel : ClassDefinitionModel {
		public override string title => "Class Asset";

		public override Type InheritType => typeof(ScriptableObject);

		public override Type ScriptInheritType => typeof(RuntimeAsset);

		public override Type ProxyScriptType => typeof(BaseRuntimeAsset);
	}
}