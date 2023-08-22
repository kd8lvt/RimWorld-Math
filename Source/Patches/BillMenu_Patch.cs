using RimWorld;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using Verse.Sound;

// This file handles making the bill menu work nicer.
namespace CrunchyDuck.Math {
	static class BillMenuData {
		public static BillComponent bc = null;
		public static Dialog_MathBillConfig billDialog = null;
		public static Bill_Production billProduction = null;
		public static bool didTargetCountThisFrame = false;
		public static Rect rect;
	
		public static bool RenderingTarget { get { return bc.isDoUntilX && !didTargetCountThisFrame; } }
		public static bool RenderingUnpause { get { return bc.isDoUntilX && didTargetCountThisFrame; } }
		public static bool RenderingRepeat { get { return bc.isDoXTimes; } }

		public static void AssignTo(BillComponent bc) {
			didTargetCountThisFrame = false;
			BillMenuData.bc = bc;
		}

		public static void AssignTo(Dialog_MathBillConfig bill_dialogue, Rect rect) {
			didTargetCountThisFrame = false;
			billDialog = bill_dialogue;
			BillMenuData.rect = rect;
			billProduction = bill_dialogue.bill;
			bc = BillManager.instance.AddGetBillComponent(billProduction);
		}

		public static void Unassign() {
			billDialog = null;
			billProduction = null;
			bc = null;
		}

		public static InputField GetCurrentlyRenderingField() {
			if (RenderingRepeat) {
				return bc.doXTimes;
			}
			else if (RenderingTarget) {
				return bc.doUntilX;
			}
			else {
				return bc.unpause;
			}
		}
	}

	// This gets the outer + and - buttons to work.
	class DoConfigInterface_Patch {
		public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill_Production), "DoConfigInterface");
		}

#if v1_4
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var codes = new List<CodeInstruction>(instructions);
			MethodInfo button_icon_method = AccessTools.Method(typeof(WidgetRow), "ButtonIcon");
			int call_count = 0;

			for (var i = 0; i < codes.Count; i++) {
				CodeInstruction code = codes[i];
				if (code.Calls(button_icon_method)) {
					MethodInfo m;
					Label? branch_target = null;
					switch (call_count) {
						case 0:
							codes[i + 1].Branches(out branch_target);
							m = AccessTools.Method(typeof(DoConfigInterface_Patch), "Increment");
							codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, m));
							codes.Insert(i + 3, new CodeInstruction(OpCodes.Br, branch_target));
							break;
						case 1:
							codes[i + 1].Branches(out branch_target);
							m = AccessTools.Method(typeof(DoConfigInterface_Patch), "Decrement");
							codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, m));
							codes.Insert(i + 3, new CodeInstruction(OpCodes.Br, branch_target));
							break;
					}
					call_count++;
					if (call_count > 1)
						break;
				}
			}
			return codes.AsEnumerable();
		}
#endif

		public static void Prefix(Bill_Production __instance, Rect baseRect, Color baseColor) {
			var bc = BillManager.instance.AddGetBillComponent(__instance);
			if (bc == null) return;
			BillMenuData.AssignTo(bc);
		}

		public static void Postfix() {
			BillMenuData.Unassign();
		}

		public static void Decrement() {
			DoEq(false);
		}

		public static void Increment() {
			DoEq(true);
		}

		public static void DoEq(bool increment) {
			var bc = BillMenuData.bc;
			var i = bc.targetBill.recipe.targetCountAdjustment* GenUI.CurrentAdjustmentMultiplier();
			i *= increment ? 1 : -1;

			InputField f = BillMenuData.GetCurrentlyRenderingField();
			f.SetAll(UnityEngine.Mathf.CeilToInt(f.CurrentValue) + i);
			SoundDefOf.DragSlider.PlayOneShotOnCamera();
		}
	}
}