using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Behavior", "Awake")]
    public class AwakeEvent : BaseGraphEvent {
		public override void GenerateEventCode() {
			DoGenerateCode("Awake");
		}
	}
}