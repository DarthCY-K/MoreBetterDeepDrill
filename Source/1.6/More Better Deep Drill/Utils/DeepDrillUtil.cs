using RimWorld;
using UnityEngine;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    /// <summary>
    /// 深钻井工具类。提供资源查找、渲染和扫描仪检测功能。
    /// 使用全局 MapResourceCache 避免重复全图扫描。
    /// </summary>
    public static class DeepDrillUtil
    {
        /// <summary>获取地图上的下一个可开采资源类型（简化版）</summary>
        public static ThingDef GetNextResource(IntVec3 p, Map map)
        {
            GetNextResource(p, map, out var resDef, out var _, out var _);
            return resDef;
        }

        /// <summary>
        /// 获取地图上的下一个可开采资源（非定向）。
        /// 从 MapResourceCache 中取第一个资源格子。
        /// 无资源时返回基岩石头类型作为 fallback。
        /// </summary>
        public static bool GetNextResource(IntVec3 p, Map map, out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            var cache = MapResourceCache.ForMap(map);
            if (cache.TryGetAny(map, out resDef, out countPresent, out cell))
            {
                return true;
            }

            resDef = GetBaseResource(map, p);
            countPresent = int.MaxValue;
            cell = p;
            return false;
        }

        /// <summary>
        /// 获取地图上指定类型的下一个可开采资源（定向找矿）。
        /// 从 MapResourceCache 的按类型索引中查找。
        /// </summary>
        public static bool GetNextResource(IntVec3 p, Map map, out ThingDef resDef, out int countPresent, out IntVec3 cell, ThingDef targetDef)
        {
            var cache = MapResourceCache.ForMap(map);
            if (cache.TryGet(map, targetDef, out countPresent, out cell))
            {
                resDef = targetDef;
                return true;
            }

            resDef = null;
            countPresent = int.MaxValue;
            cell = p;
            return false;
        }

        /// <summary>
        /// 获取地图某个位置的基岩资源类型（石头块）。
        /// 用于资源枯竭后的 fallback 产出。
        /// </summary>
        public static ThingDef GetBaseResource(Map map, IntVec3 cell)
        {
            return DeepDrillUtility.GetBaseResource(map, cell);
        }

        /// <summary>渲染鼠标悬停格子的深钻井资源信息 (图标 + 名称 + 剩余数量)</summary>
        public static void RenderMouseAttachments(Map map)
        {
            IntVec3 c = UI.MouseCell();
            if (!c.InBounds(map))
            {
                return;
            }

            ThingDef thingDef = map.deepResourceGrid.ThingDefAt(c);
            if (thingDef != null)
            {
                int num = map.deepResourceGrid.CountAt(c);
                if (num > 0)
                {
                    Vector2 vector = c.ToVector3().MapToUIPosition();
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    float num2 = (UI.CurUICellSize() - 27f) / 2f;
                    Rect rect = new Rect(vector.x + num2, vector.y - UI.CurUICellSize() + num2, 27f, 27f);
                    Widgets.ThingIcon(rect, thingDef);
                    Widgets.Label(new Rect(rect.xMax + 4f, rect.y, 999f, 29f), "DeepResourceRemaining".Translate(NamedArgumentUtility.Named(thingDef, "RESOURCE"), num.Named("COUNT")));
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            }
        }

        /// <summary>检查地图上是否有激活的深钻井扫描仪</summary>
        public static bool AnyActiveDeepScannersOnMap(Map map)
        {
            foreach (Building item in map.listerBuildings.allBuildingsColonist)
            {
                CompDeepScanner compDeepScanner = item.TryGetComp<CompDeepScanner>();
                if (compDeepScanner != null && compDeepScanner.ShouldShowDeepResourceOverlay())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
