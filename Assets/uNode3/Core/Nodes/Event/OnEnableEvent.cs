using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Behavior", "On Enable")]
    [StateEvent]
    public class OnEnableEvent : BaseComponentEvent {
        public override void GenerateEventCode() {
            DoGenerateCode(UEventID.OnEnable);
        }
    }
}