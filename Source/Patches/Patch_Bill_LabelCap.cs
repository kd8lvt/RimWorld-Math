using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using System;

namespace CrunchyDuck.Math {
	class Patch_Bill_LabelCap {
		public static MethodInfo Target() {
			return AccessTools.PropertyGetter(typeof(Bill), "LabelCap");
		}

		public static bool Prefix(Bill __instance, ref string __result) {
			if (!(__instance is Bill_Production))
				return true;
			var bc = BillManager.instance.AddGetBillComponent((Bill_Production)__instance);
			if (bc == null) return true;
			__result = bc.name;
			return false;
		}
	}
}
