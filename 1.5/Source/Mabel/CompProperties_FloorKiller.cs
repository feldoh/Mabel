using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Mabel;

public class CompProperties_FloorKiller : CompProperties
{
    public IntRange ticksBetweenFloorDestruction = new(30000, 300000);

    public CompProperties_FloorKiller() => compClass = typeof(CompFloorKiller);
}

public class CompFloorKiller : ThingComp
{
    private static int friendshipDate = 1507;
    private int _nextDestruction;

    private CompProperties_FloorKiller Props => (CompProperties_FloorKiller) props;

    private bool CanDestroy => parent is Pawn pawn && pawn.Awake() &&
                               !pawn.health.Downed &&
                               (pawn.mindState.exitMapAfterTick <= 0 ||
                                GenTicks.TicksGame < pawn.mindState.exitMapAfterTick);

    public override void Initialize(CompProperties initProps)
    {
        base.Initialize(initProps);
        _nextDestruction = GenTicks.TicksGame + Props.ticksBetweenFloorDestruction.RandomInRange;
    }

    public override void CompTick()
    {
        base.CompTick();
        if (GenTicks.TicksGame < _nextDestruction) return;
        _nextDestruction += TryDestroyFloor() ? Props.ticksBetweenFloorDestruction.RandomInRange : friendshipDate;
    }

    public bool TryDestroyFloor()
    {
        if (!CanDestroy) return false;
        IntVec3 cell = parent.Position;
        Map parentMap = parent.Map;
        if (parentMap == null || !parentMap.terrainGrid.CanRemoveTopLayerAt(cell)) return false;
        parentMap.terrainGrid.RemoveTopLayer(cell);
        FilthMaker.RemoveAllFilth(cell, parentMap);
        FilthMaker.TryMakeFilth(cell, parentMap, ThingDefOf.Filth_Dirt, 2, FilthSourceFlags.Terrain);
        FilthMaker.TryMakeFilth(cell, parentMap, ThingDefOf.Filth_Vomit, 1, FilthSourceFlags.Pawn);
        Messages.Message("Mabel_FloorDestructionMessage".Translate(parent.LabelShort), parent, MessageTypeDefOf.NegativeEvent, historical: false);
        return true;
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref _nextDestruction, "nextDestruction", 60000);
        base.PostExposeData();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (!DebugSettings.ShowDevGizmos) yield break;

        yield return new Command_Action
        {
            defaultLabel = "DEV: Destroy Floor Now",
            defaultDesc = "DEV: Force the next floor destruction tick to be now",
            action = () => _nextDestruction = 0
        };
    }
}
