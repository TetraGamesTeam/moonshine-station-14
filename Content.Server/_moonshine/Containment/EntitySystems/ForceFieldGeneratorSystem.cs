using Content.Server.Administration.Logs;
using Content.Server.DeviceLinking.Events;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._moonshine.Containment.Components;
using Content.Shared.Construction.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using System.Threading.Tasks;

namespace Content.Server._moonshine.Containment.ForceField.EntitySystems; //

public sealed class ForceFieldGeneratorSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AppearanceSystem _visualizer = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    private EntityQuery<ApcPowerReceiverComponent> _recQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForceFieldGeneratorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ForceFieldGeneratorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ForceFieldGeneratorComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<ForceFieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<ForceFieldGeneratorComponent, ReAnchorEvent>(OnReanchorEvent);
        SubscribeLocalEvent<ForceFieldGeneratorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ForceFieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<ForceFieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
        SubscribeLocalEvent<ForceFieldGeneratorComponent, DoAfterEvent>(OnDoAfter);

        _recQuery = GetEntityQuery<ApcPowerReceiverComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ForceFieldGeneratorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IsCurrentlySwitching)
            {
                comp.SwitchAccumulator += frameTime;
                if (comp.SwitchAccumulator >= comp.SwitchDelay)
                {
                    comp.SwitchAccumulator = 0f;
                    comp.IsCurrentlySwitching = false;

                }
            }

        }
    }

    #region Events

    private void OnDoAfter(EntityUid uid, ForceFieldGeneratorComponent component, DoAfterEvent args)
    {

    }

    private void OnPowerChanged(EntityUid uid, ForceFieldGeneratorComponent component, PowerChangedEvent args)
    {

    }

    private void OnSignalReceived(EntityUid uid, ForceFieldGeneratorComponent component, SignalReceivedEvent args)
    {
        if (args.Port == component.TogglePort)
            TrySwitch((uid, component));
        if (args.Port == component.OpenPort)
            TryTurnOn((uid, component));
        if (args.Port == component.ClosePort)
            TryTurnOff((uid, component));
    }

    private async void OnExamine(EntityUid uid, ForceFieldGeneratorComponent component, ExaminedEvent args)
    {
        TimeSpan ts1 = TimeSpan.FromSeconds(1);

        if (component.Enabled)
            args.PushMarkup(Loc.GetString("comp-force-on"));

        else
        {
            args.PushMarkup(Loc.GetString("comp-force-off"));
            ConnectionSetup((uid, component), "markerField");
            await Task.Delay(ts1);
            RemoveConnections((uid, component));
        }
    }

    private void OnInteract(Entity<ForceFieldGeneratorComponent> generator, ref InteractHandEvent args)
    {

    }

    private async void TryTurnOn(Entity<ForceFieldGeneratorComponent> generator)
    {
        TimeSpan ts1 = TimeSpan.FromSeconds(1.5);

        if (generator.Comp.IsCurrentlySwitching)
            return;

        generator.Comp.IsCurrentlySwitching = true;

        if (!generator.Comp.Enabled)
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-force-toggle-on"), generator);
            _audio.PlayPvs(generator.Comp.SoundWindUp, generator);
            await Task.Delay(ts1);

            if (!this.IsPowered(generator, EntityManager))
            {
                TurnOff(generator);
                return;
            }

            ConnectionSetup(generator, "forceField");
            TurnOn(generator);
            _audio.PlayPvs(generator.Comp.SoundLoop, generator);
        }
    }

    private async void TryTurnOff(Entity<ForceFieldGeneratorComponent> generator)
    {
        if (TryComp<ForceFieldGeneratorComponent>(generator.Comp.OtherGenerator, out var component) && component != null)
        {
            var otherGenerator = (generator.Comp.OtherGenerator, component);

            TimeSpan ts1 = TimeSpan.FromSeconds(1.5);

            if (generator.Comp.IsCurrentlySwitching)
                return;

            generator.Comp.IsCurrentlySwitching = true;
            if (generator.Comp.Enabled)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-force-toggle-off"), generator);
                _audio.PlayPvs(generator.Comp.SoundShutdown, generator);
                await Task.Delay(ts1);
                TurnOff(generator);

                TurnOff(otherGenerator);
            }
        }
    }

    private async void TrySwitch(Entity<ForceFieldGeneratorComponent> generator)
    {

        if (generator.Comp.IsCurrentlySwitching)
            return;

        if (!generator.Comp.Enabled)
            TryTurnOn(generator);
        else if (generator.Comp.Enabled)
            TryTurnOff(generator);
    }
    private void TurnOn(Entity<ForceFieldGeneratorComponent> generator) // включение
    {
        _popupSystem.PopupEntity(Loc.GetString("comp-force-turn-on"), generator);
        generator.Comp.Enabled = true;
        ChangeFieldVisualizer(generator);
    }

    private void TurnOff(Entity<ForceFieldGeneratorComponent> generator) // выключение
    {
        _popupSystem.PopupEntity(Loc.GetString("comp-force-turn-off"), generator);
        generator.Comp.Enabled = false;
        RemoveConnections(generator);
        ChangeFieldVisualizer(generator);
    }

    private void OnAnchorChanged(Entity<ForceFieldGeneratorComponent> generator, ref AnchorStateChangedEvent args) // если закрепление сменилось
    {
        if (!args.Anchored) // если не закреплён
            RemoveConnections(generator); // разъединить
    }

    private void OnReanchorEvent(Entity<ForceFieldGeneratorComponent> generator, ref ReAnchorEvent args) // если перезакрепили
    {
        GridCheck(generator); // хз чойта
    }

    private void OnUnanchorAttempt(EntityUid uid, ForceFieldGeneratorComponent component, UnanchorAttemptEvent args)
    {
        if (component.Enabled)
        {
            _popupSystem.PopupEntity(Loc.GetString("comp-force-anchor-warning"), args.User, args.User);
            args.Cancel();
        }
    }



    private void OnComponentRemoved(Entity<ForceFieldGeneratorComponent> generator, ref ComponentRemove args)
    {
        RemoveConnections(generator);
    }

    private void RemoveConnections(Entity<ForceFieldGeneratorComponent> generator) // удаляет поля и соединения генераторов
    {
        var (uid, component) = generator;
        foreach (var (direction, value) in component.Connections)
        {
            foreach (var field in value.Item2)
            {
                QueueDel(field);
            }
            value.Item1.Comp.Connections.Remove(direction.GetOpposite());

            if (value.Item1.Comp.Connections.Count == 0) //Change isconnected only if there's no more connections
            {
                value.Item1.Comp.IsConnected = false;
                ChangeOnLightVisualizer(value.Item1);
            }

            ChangeFieldVisualizer(value.Item1);
        }
        component.Connections.Clear();
        component.IsConnected = false;
        ChangeOnLightVisualizer(generator);
        ChangeFieldVisualizer(generator);
    }

    #endregion

    #region Connections

    public void ConnectionSetup(Entity<ForceFieldGeneratorComponent> generator, string fieldType)
    {
        var component = generator.Comp;
        var genXForm = Transform(generator);
        var connectionIsAvailable = false;

        var directions = Enum.GetValues<Direction>().Length;
        for (int i = 0; i < directions - 1; i += 2)
        {
            var dir = (Direction) i;

            if (component.Connections.ContainsKey(dir))
                continue;

            if (fieldType == "markerField" && !component.Enabled)
            {
                TryGenerateFieldConnection(dir, generator, genXForm, "markerField");
            }
            else if (fieldType == "forceField")
            {
                if (TryGenerateFieldConnection(dir, generator, genXForm, "forceField"))
                    connectionIsAvailable = true;
            }
            else
            {
                _popupSystem.PopupEntity("what", generator);
            }
        }

        if (fieldType == "forceField")
        {
            if (!connectionIsAvailable)
                _popupSystem.PopupEntity(Loc.GetString("comp-force-second-inaccessible"), generator);
        }
    }

    public bool TryGenerateFieldConnection(Direction dir, Entity<ForceFieldGeneratorComponent> generator, TransformComponent gen1XForm, string fieldType)
    {
        var component = generator.Comp;

        if (!gen1XForm.Anchored)
            return false;

        var genWorldPosRot = gen1XForm.GetWorldPositionRotation();
        var dirRad = dir.ToAngle() + genWorldPosRot.WorldRotation;

        var ray = new CollisionRay(genWorldPosRot.WorldPosition, dirRad.ToVec(), component.CollisionMask);
        var rayCastResults = _physics.IntersectRay(gen1XForm.MapID, ray, component.MaxLength, generator, false);
        var genQuery = GetEntityQuery<ForceFieldGeneratorComponent>();

        RayCastResults? closestResult = null;

        foreach (var result in rayCastResults)
        {
            if (genQuery.HasComponent(result.HitEntity))
                closestResult = result;

            break;
        }
        if (closestResult == null)
        {
            return false;
        }

        var ent = closestResult.Value.HitEntity;
        generator.Comp.OtherGenerator = ent;

        if (!TryComp<ForceFieldGeneratorComponent>(ent, out var otherFieldGeneratorComponent) ||
            otherFieldGeneratorComponent == component ||
            gen1XForm.ParentUid != Transform(ent).ParentUid)
        {
            return true;
        }

        var otherFieldGenerator = (ent, otherFieldGeneratorComponent);

        var fields = GenerateFieldConnection(generator, otherFieldGenerator, fieldType);

        component.Connections[dir] = (otherFieldGenerator, fields);
        otherFieldGeneratorComponent.Connections[dir.GetOpposite()] = (generator, fields);

        if (fieldType == "forceField")
        {
            TurnOn(otherFieldGenerator);
            ChangeFieldVisualizer(otherFieldGenerator);
            UpdateConnectionLights(otherFieldGenerator);

            if (!component.IsConnected)
            {
                component.IsConnected = true;
                ChangeOnLightVisualizer(generator);
            }

            if (!otherFieldGeneratorComponent.IsConnected)
            {
                otherFieldGeneratorComponent.IsConnected = true;
                ChangeOnLightVisualizer(otherFieldGenerator);
            }

            ChangeFieldVisualizer(generator);
            UpdateConnectionLights(generator);
        }

        return true;
    }

    private List<EntityUid> GenerateFieldConnection(Entity<ForceFieldGeneratorComponent> firstGen, Entity<ForceFieldGeneratorComponent> secondGen, string fieldType)
    {
        var fieldList = new List<EntityUid>();
        var gen1Coords = Transform(firstGen).Coordinates;
        var gen2Coords = Transform(secondGen).Coordinates;

        var delta = (gen2Coords - gen1Coords).Position;
        var dirVec = delta.Normalized();
        var stopDist = delta.Length();
        var currentOffset = dirVec;
        EntityUid newField;
        while (currentOffset.Length() < stopDist)
        {
            var currentCoords = gen1Coords.Offset(currentOffset);
            if (fieldType == "forceField")
                newField = Spawn(firstGen.Comp.CreatedField, currentCoords);
            else
                newField = Spawn(firstGen.Comp.CreatedMarkerField, currentCoords);

            var fieldXForm = Transform(newField);
            fieldXForm.AttachParent(firstGen);
            if (dirVec.GetDir() == Direction.East || dirVec.GetDir() == Direction.West)
            {
                var angle = fieldXForm.LocalPosition.ToAngle();
                var rotateBy90 = angle.Degrees + 90;
                var rotatedAngle = Angle.FromDegrees(rotateBy90);

                fieldXForm.LocalRotation = rotatedAngle;
            }

            fieldList.Add(newField);
            currentOffset += dirVec;
        }
        return fieldList;
    }

    /// <summary>
    /// Creates a light component for the spawned fields.
    /// </summary>
    public void UpdateConnectionLights(Entity<ForceFieldGeneratorComponent> generator)
    {
        if (_light.TryGetLight(generator, out var pointLightComponent))
        {
            _light.SetEnabled(generator, generator.Comp.Connections.Count > 0, pointLightComponent);
        }
    }

    /// <summary>
    /// Checks to see if this or the other gens connected to a new grid. If they did, remove connection.
    /// </summary>
    public void GridCheck(Entity<ForceFieldGeneratorComponent> generator)
    {
        var xFormQuery = GetEntityQuery<TransformComponent>();

        foreach (var (_, generators) in generator.Comp.Connections)
        {
            var gen1ParentGrid = xFormQuery.GetComponent(generator).ParentUid;
            var gent2ParentGrid = xFormQuery.GetComponent(generators.Item1).ParentUid;

            if (gen1ParentGrid != gent2ParentGrid)
                RemoveConnections(generator);
        }
    }

    #endregion

    #region VisualizerHelpers
    /// <summary>
    /// Check if a fields power falls between certain ranges to update the field gen visual for power.
    /// </summary>
    /// <param name="power"></param>
    /// <param name="generator"></param>

    /// <summary>
    /// Check if a field has any or no connections and if it's enabled to toggle the field level light
    /// </summary>
    /// <param name="generator"></param>
    private void ChangeFieldVisualizer(Entity<ForceFieldGeneratorComponent> generator)
    {
        _visualizer.SetData(generator, ContainmentFieldGeneratorVisuals.FieldLight, generator.Comp.Connections.Count switch
        {
            >1 => FieldLevelVisuals.MultipleFields,
            1 => FieldLevelVisuals.OneField,
            _ => generator.Comp.Enabled ? FieldLevelVisuals.On : FieldLevelVisuals.NoLevel
        });
    }

    private void ChangeOnLightVisualizer(Entity<ForceFieldGeneratorComponent> generator)
    {
        _visualizer.SetData(generator, ContainmentFieldGeneratorVisuals.OnLight, generator.Comp.IsConnected);
    }
    #endregion
}
