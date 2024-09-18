using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("", "Start")]
	[Description("Start is called on the frame when a graph instance is enabled just before any of the Update methods are called the first time.")]
    public class StartEvent : BaseGraphEvent {
		public override void GenerateEventCode() {
			DoGenerateCode("Start");
		}
	}
}