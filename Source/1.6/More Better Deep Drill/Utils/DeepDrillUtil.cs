using RimWorld;
using System.Linq;
using UnityEngine;
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

        /// <summary>
        /// 默认找矿方法
        /// </summary>
        /// <param name="p">位置</param>
        /// <param name="map">当前地图</param>
        /// <param name="resDef">返回的资源类型</param>
        /// <param name="countPresent">数量？</param>
        /// <param name="cell">单元块</param>
        /// <returns>是否找到资源</returns>
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

        /// <summary>
        /// 定向找矿方法
        /// </summary>
        /// <param name="p">位置</param>
        /// <param name="map">当前地图</param>
        /// <param name="resDef">返回的资源类型</param>
        /// <param name="countPresent">数量？</param>
        /// <param name="cell">单元块</param>
        /// <param name="targetOre">定向目标矿物</param>
        /// <returns>是否找到资源</returns>
        public static bool GetNextResource(IntVec3 p, Map map, out ThingDef resDef, out int countPresent, out IntVec3 cell, ThingDef targetDef)
        {
            foreach (var intVec in map.AllCells)
            {
                ThingDef thingDef = map.deepResourceGrid.ThingDefAt(intVec);
                if (thingDef != null && thingDef == targetDef)
                {
                    resDef = thingDef;
                    countPresent = map.deepResourceGrid.CountAt(intVec);
                    cell = intVec;
                    return true;
                }
            }

            resDef = null;
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