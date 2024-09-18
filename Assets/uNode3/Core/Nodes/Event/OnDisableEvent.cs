using System;
using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.UNode.Nodes {
    [EventMenu("Behavior", "On Disable")]
    [StateEvent]
    [Description("This function is called when the behaviour becomes disabled.")]
    public class OnDisableEvent : BaseComponentEvent {
        public override void GenerateEventCode() {
            DoGenerateCode(UEventID.OnDisable);
        }
    }
}