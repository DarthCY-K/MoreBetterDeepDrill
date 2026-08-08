using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    /// <summary>
    /// 按地图维护深层资源格索引。首次查询扫描一次全图，之后由
    /// DeepResourceGrid.SetAt 补丁对单个格子做增量更新。
    /// </summary>
    public sealed class MapResourceCache
    {
        private static readonly ConditionalWeakTable<Map, MapResourceCache> caches = new ConditionalWeakTable<Map, MapResourceCache>();

        private readonly SortedSet<int> allResourceIndices = new SortedSet<int>();
        private readonly Dictionary<ThingDef, SortedSet<int>> indicesByDef = new Dictionary<ThingDef, SortedSet<int>>();
        private readonly Dictionary<int, ThingDef> resourceDefsByIndex = new Dictionary<int, ThingDef>();
        private bool built;

        public static MapResourceCache ForMap(Map map)
        {
            return caches.GetValue(map, _ => new MapResourceCache());
        }

        public bool TryGetAny(Map map, out ThingDef def, out int count, out IntVec3 cell)
        {
            EnsureBuilt(map);
            while (allResourceIndices.Count > 0)
            {
                int index = allResourceIndices.Min;
                cell = map.cellIndices.IndexToCell(index);
                def = map.deepResourceGrid.ThingDefAt(cell);
                count = map.deepResourceGrid.CountAt(cell);
                if (def != null && count > 0)
                {
                    SynchronizeIndex(index, def, count);
                    return true;
                }

                SynchronizeIndex(index, def, count);
            }

            def = null;
            count = 0;
            cell = IntVec3.Invalid;
            return false;
        }

        public bool TryGet(Map map, ThingDef targetDef, out int count, out IntVec3 cell)
        {
            EnsureBuilt(map);
            if (targetDef == null || !indicesByDef.TryGetValue(targetDef, out var indices))
            {
                count = 0;
                cell = IntVec3.Invalid;
                return false;
            }

            while (indices.Count > 0)
            {
                int index = indices.Min;
                cell = map.cellIndices.IndexToCell(index);
                ThingDef actualDef = map.deepResourceGrid.ThingDefAt(cell);
                count = map.deepResourceGrid.CountAt(cell);
                if (actualDef == targetDef && count > 0)
                    return true;

                indices.Remove(index);
                if (indices.Count == 0 && indicesByDef.TryGetValue(targetDef, out SortedSet<int> currentIndices)
                    && ReferenceEquals(indices, currentIndices))
                {
                    indicesByDef.Remove(targetDef);
                }
                SynchronizeIndex(index, actualDef, count);
            }

            count = 0;
            cell = IntVec3.Invalid;
            return false;
        }

        /// <summary>在 SetAt 完成后，按网格中的最终值同步索引。</summary>
        public void NotifySetAt(Map map, IntVec3 cell)
        {
            if (!built)
                return;

            int index = map.cellIndices.CellToIndex(cell);
            ThingDef finalDef = map.deepResourceGrid.ThingDefAt(cell);
            int finalCount = map.deepResourceGrid.CountAt(cell);
            SynchronizeIndex(index, finalDef, finalCount);
        }

        public void Invalidate()
        {
            built = false;
            allResourceIndices.Clear();
            indicesByDef.Clear();
            resourceDefsByIndex.Clear();
        }

        private void EnsureBuilt(Map map)
        {
            if (built)
                return;

            allResourceIndices.Clear();
            indicesByDef.Clear();
            resourceDefsByIndex.Clear();
            int numCells = map.cellIndices.NumGridCells;
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = map.cellIndices.IndexToCell(i);
                ThingDef def = map.deepResourceGrid.ThingDefAt(cell);
                if (def != null && map.deepResourceGrid.CountAt(cell) > 0)
                    Add(i, def);
            }
            built = true;
        }

        private void Add(int index, ThingDef def)
        {
            if (resourceDefsByIndex.TryGetValue(index, out ThingDef existingDef) && existingDef != def)
                Remove(index, existingDef);

            allResourceIndices.Add(index);
            if (!indicesByDef.TryGetValue(def, out var indices))
            {
                indices = new SortedSet<int>();
                indicesByDef.Add(def, indices);
            }
            indices.Add(index);
            resourceDefsByIndex[index] = def;
        }

        private void Remove(int index, ThingDef def)
        {
            allResourceIndices.Remove(index);
            resourceDefsByIndex.Remove(index);
            if (!indicesByDef.TryGetValue(def, out var indices))
                return;

            indices.Remove(index);
            if (indices.Count == 0)
                indicesByDef.Remove(def);
        }

        private void SynchronizeIndex(int index, ThingDef actualDef, int actualCount)
        {
            if (resourceDefsByIndex.TryGetValue(index, out ThingDef cachedDef)
                && (actualDef != cachedDef || actualCount <= 0))
            {
                Remove(index, cachedDef);
            }

            if (actualDef != null && actualCount > 0)
                Add(index, actualDef);
            else
                allResourceIndices.Remove(index);
        }
    }
}
