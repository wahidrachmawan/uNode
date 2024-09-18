using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Behavior", "Awake")]
	[Description("Unity calls Awake when an enabled graph instance is being loaded.")]
    public class AwakeEvent : BaseGraphEvent {
		public override void GenerateEventCode() {
			DoGenerateCode("Awake");
		}
	}
}