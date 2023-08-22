using System.Reflection;
using HarmonyLib;
using RimWorld;
using System;

namespace CrunchyDuck.Math
{
    public class Patch_RimFactory_RecipeWorkerCounter_CountProducts {
        public static MethodInfo Target() {
            Type type = Type.GetType("ProjectRimFactory.Common.HarmonyPatches.Patch_RecipeWorkerCounter_CountProducts, ProjectRimFactory");
            return AccessTools.Method(type, "Postfix");
        }

        public static bool Prefix(Bill_Production bill) {
            var bc = BillManager.instance.AddGetBillComponent(bill);
            if (bc == null) return true;
            // Skip this if custom item counting is in use.
            return !bc.customItemsToCount;
        }
    }
}