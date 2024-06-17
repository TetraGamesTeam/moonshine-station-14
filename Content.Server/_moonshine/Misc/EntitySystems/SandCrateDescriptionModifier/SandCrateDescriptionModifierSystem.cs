using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Singularity.Events;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Tag;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared._moonshine.Misc.Components;

namespace Content.Server._moonshine.Misc.SandCrateDescriptionModifier.EntitySystems;

public sealed class SandCrateDescriptionModifierSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AppearanceSystem _visualizer = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandCrateDescriptionModifierComponent, InteractHandEvent>(OnInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }

    private void OnExamine(EntityUid uid, SandCrateDescriptionModifierComponent component, ExaminedEvent args)
    {
        component.ItemExaminedTimes += 1f;

        switch (component.ItemExaminedTimes)
        {
            case 1:
                args.PushMarkup(component.Desc1);
                break;
            case 2:
                args.PushMarkup(component.Desc2);
                break;
        }
    }

    private void OnInteract(Entity<SandCrateDescriptionModifierComponent> generator, ref InteractHandEvent args)
    {

    }
}
