using Content.Shared.Physics;
using Robust.Shared.Audio;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._moonshine.Misc.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SandCrateDescriptionModifierComponent : Component
{
    [DataField("desc1")]
    public string Desc1;

    [DataField("desc2")]
    public string Desc2;

    [DataField("desc3")]
    public string Desc3;

    [DataField("desc4")]
    public string Desc4;

    [DataField("desc5")]
    public string Desc5;

    [DataField("dropChance")]
    public int DropChance;

    [DataField("itemToSpawn")]
    public string ItemToSpawn;

    [ViewVariables]
    public bool ItemAlreadyDropped;
    public float ItemExaminedTimes = 0f;
}
