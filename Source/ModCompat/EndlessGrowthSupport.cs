using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace CrunchyDuck.Math.ModCompat {
	public class EndlessGrowthSupport {

        private static Type EGCommonPatchesType { get; } = Type.GetType("SlimeSenpai.EndlessGrowth.CommonPatches, EndlessGrowth");

        private static readonly Func<int> EGGetMaxLevelForBill = AccessTools.MethodDelegate<Func<int>>(AccessTools.Method(EGCommonPatchesType, "GetMaxLevelForBill", Type.EmptyTypes));

        public static int GetMaxLevelForBill()
        {
            return EGGetMaxLevelForBill();
        }
    }
}
