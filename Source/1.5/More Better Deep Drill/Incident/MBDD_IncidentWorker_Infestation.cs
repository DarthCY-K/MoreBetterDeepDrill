using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MoreBetterDeepDrill.Incident
{
    public class MBDD_IncidentWorker_Infestation : IncidentWorker
    {
        private static List<Thing> tmpDrills = new List<Thing>();

        private const float MinPointsFactor = 0.3f;

        private const float MaxPointsFactor = 0.6f;

        private const float MinPoints = 200f;

        private const float MaxPoints = 1000f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
                return false;

            //配置文件决定是否启用虫灾
            if (!Utils.StaticValues.ModSetting.EnableInsectoids)
                return false;

            if (Faction.OfInsects == null)
                return false;

            Map map = (Map)parms.target;
            tmpDrills.Clear();
            Utils.InfestationIncidentUtil.GetUsableDeepDrills(map, tmpDrills);
            return tmpDrills.Any<Thing>();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            tmpDrills.Clear();
            Utils.InfestationIncidentUtil.GetUsableDeepDrills(map, tmpDrills);
            Thing deepDrill;
            if (!tmpDrills.TryRandomElement(out deepDrill))
            {
                return false;
            }
            IntVec3 intVec = CellFinder.FindNoWipeSpawnLocNear(deepDrill.Position, map, ThingDefOf.TunnelHiveSpawner, Rot4.North, 2, (IntVec3 x) => x.Walkable(map) && x.GetFirstThing(map, deepDrill.def) == null && x.GetFirstThingWithComp<Comp.MBDD_CompCreatesInfestations>(map) == null && x.GetFirstThing(map, ThingDefOf.Hive) == null && x.GetFirstThing(map, ThingDefOf.TunnelHiveSpawner) == null);
            if (intVec == deepDrill.Position)
            {
                return false;
            }
            TunnelHiveSpawner tunnelHiveSpawner = (TunnelHiveSpawner)ThingMaker.MakeThing(ThingDefOf.TunnelHiveSpawner, null);
            tunnelHiveSpawner.spawnHive = false;
            tunnelHiveSpawner.insectsPoints = Mathf.Clamp(parms.points * Rand.Range(0.3f, 0.6f), 200f, 1000f);
            tunnelHiveSpawner.spawnedByInfestationThingComp = true;
            GenSpawn.Spawn(tunnelHiveSpawner, intVec, map, WipeMode.FullRefund);
            deepDrill.TryGetComp<Comp.MBDD_CompCreatesInfestations>().Notify_CreatedInfestation();
            base.SendStandardLetter(parms, new TargetInfo(tunnelHiveSpawner.Position, map, false), Array.Empty<NamedArgument>());
            return true;
        }
    }
}