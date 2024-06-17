using Content.Shared.Physics;
using Robust.Shared.Audio;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._moonshine.Containment.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ForceFieldGeneratorComponent : Component
{
    [DataField("soundWindUp")]
    public SoundSpecifier SoundWindUp = new SoundPathSpecifier("/Audio/_moonshine/windup.ogg");

    [DataField("soundLoop")]
    public SoundSpecifier SoundLoop = new SoundPathSpecifier("/Audio/_moonshine/loop.ogg");

    [DataField("soundShutdown")]
    public SoundSpecifier SoundShutdown = new SoundPathSpecifier("/Audio/_moonshine/shutdown.ogg");

    [DataField("switchDelay")]
    public float SwitchDelay = 1.2f;

    public bool IsCurrentlySwitching;

    public float SwitchAccumulator = 0f;

    [DataField("threshold")]
    public float Threshold = 20f;

    [DataField("maxLength")]
    public float MaxLength = 8F;

    [ViewVariables]
    public bool Enabled;
    public bool Powered;

    public EntityUid OtherGenerator;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsConnected;

    [DataField("collisionMask")]
    public int CollisionMask = (int) (CollisionGroup.MobMask | CollisionGroup.Impassable | CollisionGroup.MachineMask | CollisionGroup.Opaque);

    [ViewVariables]
    public Dictionary<Direction, (Entity<ForceFieldGeneratorComponent>, List<EntityUid>)> Connections = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("createdField", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CreatedField = "AtmosForceField";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("createdMarkerField", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CreatedMarkerField = "EffectFieldMarker";

    [DataField("openPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OpenPort = "On";

    [DataField("closePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string ClosePort = "Off";

    [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string TogglePort = "Toggle";
}

[Serializable, NetSerializable]
public enum ContainmentFieldGeneratorVisuals : byte
{
    PowerLight,
    FieldLight,
    OnLight,
}

[Serializable, NetSerializable]
public enum PowerLevelVisuals : byte
{
    NoPower,
    LowPower,
    MediumPower,
    HighPower,
}

[Serializable, NetSerializable]
public enum FieldLevelVisuals : byte
{
    NoLevel,
    On,
    OneField,
    MultipleFields,
}
