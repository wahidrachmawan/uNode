using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	[Serializable]
	public class GeneratedScriptData {
		public string fileName;
		public TypeData selfTypeData = new TypeData();
		public List<TypeData> typeDatas = new List<TypeData>();

		public string typeName => selfTypeData?.typeName;


		public bool debug = false;
		public bool debugValueNode = true;

		/// <summary>
		/// If true, the graph will be compiled to C# to run using native c# performance on build or in editor using ( Generate C# Scripts ) menu.
		/// </summary>
		public bool compileToScript = true;

		[Serializable]
		public class ObjectData {
			public string name;
			public UnityEngine.Object value;
		}
		public List<ObjectData> unityObjects = new List<ObjectData>();

		[Serializable]
		public class TypeData {
			public string typeName;
			public UnityEngine.Object reference;
		}
	}
}