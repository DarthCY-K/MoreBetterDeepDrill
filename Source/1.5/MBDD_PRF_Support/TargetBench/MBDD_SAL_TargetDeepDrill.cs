using ProjectRimFactory.AutoMachineTool;
using ProjectRimFactory.SAL3;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using MoreBetterDeepDrill.Comp;

namespace MBDD_PRF_Support.TargetBench
{
    public class MBDD_SAL_TargetDeepDrill : SAL_TargetBench
    {
        private Building drilltypeBuilding;

        private MBDD_CompDeepDrill compDeepDrill;

        private readonly float[] miningyieldfactors = new float[21]
        {
        0.6f, 0.7f, 0.8f, 0.85f, 0.9f, 0.925f, 0.95f, 0.975f, 1f, 1f,
        1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f,
        1f
        };

        private const float DeepDrill_WorkAmount = 1000f;

        public MBDD_SAL_TargetDeepDrill()
        {
        }

        public MBDD_SAL_TargetDeepDrill(Building_AutoMachineTool mySAL, IntVec3 position, Map map, Rot4 rotation, Building drilltypeBuilding)
            : base(mySAL, position, map, rotation)
        {
            this.drilltypeBuilding = drilltypeBuilding;
            if (compDeepDrill == null)
            {
                compDeepDrill = drilltypeBuilding.TryGetComp<MBDD_CompDeepDrill>();
            }
        }

        public override bool Ready()
        {
            return drilltypeBuilding.TryGetComp<MBDD_CompDeepDrill>().CanDrillNow && !(drilltypeBuilding.GetComp<CompForbiddable>()?.Forbidden ?? false);
        }

        public override void Free()
        {
            generalRelease(drilltypeBuilding);
        }

        public override void Reserve()
        {
            generalReserve(drilltypeBuilding);
        }

        public override void WorkDone(out List<Thing> products)
        {
            products = new List<Thing>();

            MBDD_CompDeepDrill obj = drilltypeBuilding.TryGetComp<MBDD_CompDeepDrill>();
            if(obj != null)
            {
                float num = 1000f * Mathf.Max(mySAL.powerWorkSetting.GetSpeedFactor() * ((float)mySAL.GetSkillLevel(SkillDefOf.Mining) * 0.12f + 0.04f), 0.1f);
                float progress = num;
                float yieldPct = num * miningyieldfactors[mySAL.GetSkillLevel(SkillDefOf.Mining)] / 10000f;
                obj.DrillWorkForPRF(progress, yieldPct, Find.TickManager.TicksGame);
            }
        }

        public override bool TryStartWork(out float workAmount)
        {
            workAmount = 0f;
            if (compDeepDrill == null)
            {
                compDeepDrill = drilltypeBuilding.TryGetComp<MBDD_CompDeepDrill>();
            }

            if (compDeepDrill.CanDrillNow)
            {
                workAmount = 1000f;
                return true;
            }

            return false;
        }

        public override TargetInfo TargetInfo()
        {
            return drilltypeBuilding;
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Free();
            }

            base.ExposeData();
            Scribe_References.Look(ref drilltypeBuilding, "drilltypeBuilding");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Reserve();
            }
        }
    }
}
