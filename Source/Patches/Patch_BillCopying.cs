using RimWorld;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;

namespace CrunchyDuck.Math {
	public class Patch_BillCopying {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill), "DoInterface");
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var codes = new List<CodeInstruction>(instructions);
			MethodInfo target_method = AccessTools.Method(typeof(Bill), "Clone");
			MethodInfo insert_method = AccessTools.Method(typeof(Patch_BillCopying), nameof(Patch_BillCopying.BillCopied));
			int insert_index = 0;

			for (var i = 0; i < codes.Count; i++) {
				CodeInstruction code = codes[i];
				if (code.Calls(target_method)) {
					insert_index = i - 1;
					codes.Insert(insert_index++, new CodeInstruction(OpCodes.Ldarg_0));
					codes.Insert(insert_index++, new CodeInstruction(OpCodes.Call, insert_method));
					break;
				}
			}
			return codes.AsEnumerable();
		}

		public static void BillCopied(Bill b) {
			if (!(b is Bill_Production bp))
				return;
			var bc = BillManager.instance.AddGetBillComponent(bp);
			if (bc == null) return;
			BillLinkTracker.currentlyCopied = bc.linkTracker;
		}
	}
}
