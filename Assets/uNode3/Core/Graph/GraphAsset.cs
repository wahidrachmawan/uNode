using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace MaxyGames.UNode {
	public abstract class GraphAsset : ScriptableObject, IGraph, IIcon {
		public Texture2D icon;

		[SerializeField]
		protected SerializedGraph m_serializedGraph = new SerializedGraph();
		
		public Graph GraphData => m_serializedGraph.GetGraph(this);

		public SerializedGraph serializedGraph => m_serializedGraph;

		public virtual Type GetIcon() {
			return TypeIcons.FromTexture(icon);
		}
	}
}