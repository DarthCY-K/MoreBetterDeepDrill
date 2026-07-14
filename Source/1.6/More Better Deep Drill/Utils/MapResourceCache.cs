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
        private bool built;

        public static MapResourceCache ForMap(Map map)
        {
            return caches.GetValue(map, _ => new MapResourceCache());
        }

        public bool TryGetAny(Map map, out ThingDef def, out int count, out IntVec3 cell)
        {
            EnsureBuilt(map);
            if (allResourceIndices.Count == 0)
            {
                def = null;
                count = 0;
                cell = IntVec3.Invalid;
                return false;
            }

            cell = map.cellIndices.IndexToCell(allResourceIndices.Min);
            def = map.deepResourceGrid.ThingDefAt(cell);
            count = map.deepResourceGrid.CountAt(cell);
            return true;
        }

        public bool TryGet(Map map, ThingDef targetDef, out int count, out IntVec3 cell)
        {
            EnsureBuilt(map);
            if (targetDef == null || !indicesByDef.TryGetValue(targetDef, out var indices) || indices.Count == 0)
            {
                count = 0;
                cell = IntVec3.Invalid;
                return false;
            }

            cell = map.cellIndices.IndexToCell(indices.Min);
            count = map.deepResourceGrid.CountAt(cell);
            return true;
        }

        /// <summary>在 SetAt 修改数组前，用旧值和即将写入的新值更新索引。</summary>
        public void NotifySetAt(Map map, IntVec3 cell, ThingDef newDef, int newCount)
        {
            if (!built)
                return;

            ThingDef oldDef = map.deepResourceGrid.ThingDefAt(cell);
            int oldCount = map.deepResourceGrid.CountAt(cell);
            int index = map.cellIndices.CellToIndex(cell);

            if (oldDef != null && oldCount > 0)
                Remove(index, oldDef);
            if (newDef != null && newCount > 0)
                Add(index, newDef);
        }

        public void Invalidate()
        {
            built = false;
            allResourceIndices.Clear();
            indicesByDef.Clear();
        }

        private void EnsureBuilt(Map map)
        {
            if (built)
                return;

            allResourceIndices.Clear();
            indicesByDef.Clear();
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
            allResourceIndices.Add(index);
            if (!indicesByDef.TryGetValue(def, out var indices))
            {
                indices = new SortedSet<int>();
                indicesByDef.Add(def, indices);
            }
            indices.Add(index);
        }

        private void Remove(int index, ThingDef def)
        {
            allResourceIndices.Remove(index);
            if (!indicesByDef.TryGetValue(def, out var indices))
                return;

            indices.Remove(index);
            if (indices.Count == 0)
                indicesByDef.Remove(def);
        }
    }
}
