﻿using FFXIVClientStructs.FFXIV.Component.GUI;

namespace ECommons.UIHelpers.AddonMasterImplementations;
public partial class AddonMaster
{
    public unsafe class AirShipExploration : AddonMasterBase<AtkUnitBase>
    {
        public AirShipExploration(nint addon) : base(addon) { }
        public AirShipExploration(void* addon) : base(addon) { }

        public AtkComponentButton* DeployButton => Addon->GetButtonNodeById(85);

        public void Deploy() => ClickButtonIfEnabled(DeployButton);
    }
}