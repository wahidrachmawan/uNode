using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("", "Start")]
    public class StartEvent : BaseGraphEvent {
		public override void GenerateEventCode() {
			DoGenerateCode("Start");
		}
	}
}