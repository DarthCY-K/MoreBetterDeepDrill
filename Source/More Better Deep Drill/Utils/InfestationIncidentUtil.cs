using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    public static class InfestationIncidentUtil
    {
        public static void GetUsableDeepDrills(Map map, List<Thing> outDrills)
        {
            outDrills.Clear();

            List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Faction != null && list[i].Faction == Faction.OfPlayer)
                {
                    bool hasComp = list[i].HasComp<Comp.MBDD_CompCreatesInfestations>();
                    if (hasComp)
                        outDrills.Add(list[i]);
                }
            }
        }
    }
}