using System.Linq;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    public static class DeepDrillUtil
    {
        public static ThingDef GetNextResource(IntVec3 p, Map map)
        {
            GetNextResource(p, map, out var resDef, out var _, out var _);
            return resDef;
        }

        public static bool GetNextResource(IntVec3 p, Map map, out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            foreach (var intVec in map.AllCells)
            {
                ThingDef thingDef = map.deepResourceGrid.ThingDefAt(intVec);
                if (thingDef != null)
                {
                    resDef = thingDef;
                    countPresent = map.deepResourceGrid.CountAt(intVec);
                    cell = intVec;
                    return true;
                }
            }

            resDef = GetBaseResource(map, p);
            countPresent = int.MaxValue;
            cell = p;
            return false;
        }

        public static ThingDef GetBaseResource(Map map, IntVec3 cell)
        {
            if (!map.Biome.hasBedrock)
            {
                return null;
            }

            Rand.PushState();
            Rand.Seed = cell.GetHashCode();
            ThingDef result = (from rock in Find.World.NaturalRockTypesIn(map.Tile)
                               select rock.building.mineableThing).RandomElement();
            Rand.PopState();
            return result;
        }
    }
}