using System.Collections.Generic;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    public class MapResourceCache
    {
        private static readonly Dictionary<int, MapResourceCache> caches = new Dictionary<int, MapResourceCache>();

        public static MapResourceCache ForMap(Map map)
        {
            if (!caches.TryGetValue(map.uniqueID, out var cache))
            {
                cache = new MapResourceCache();
                caches[map.uniqueID] = cache;
            }
            return cache;
        }

        public static void RemoveMap(Map map)
        {
            caches.Remove(map.uniqueID);
        }

        public readonly Dictionary<ThingDef, List<IntVec3>> cellsByDef = new Dictionary<ThingDef, List<IntVec3>>();

        public IntVec3 firstResourceCell = IntVec3.Invalid;
        public ThingDef firstResourceDef;

        private bool built;

        public bool HasResources
        {
            get
            {
                if (!built)
                    return false;
                return firstResourceDef != null;
            }
        }

        public void EnsureBuilt(Map map)
        {
            if (built)
                return;
            Build(map);
        }

        private void Build(Map map)
        {
            cellsByDef.Clear();
            firstResourceCell = IntVec3.Invalid;
            firstResourceDef = null;

            int numCells = map.cellIndices.NumGridCells;
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = map.cellIndices.IndexToCell(i);
                ThingDef def = map.deepResourceGrid.ThingDefAt(cell);
                if (def == null)
                    continue;

                if (!cellsByDef.TryGetValue(def, out var list))
                {
                    list = new List<IntVec3>();
                    cellsByDef[def] = list;
                }
                list.Add(cell);

                if (firstResourceDef == null)
                {
                    firstResourceCell = cell;
                    firstResourceDef = def;
                }
            }

            built = true;
        }

        public void Invalidate()
        {
            if (!built)
                return;
            built = false;
            cellsByDef.Clear();
            firstResourceCell = IntVec3.Invalid;
            firstResourceDef = null;
        }
    }
}
