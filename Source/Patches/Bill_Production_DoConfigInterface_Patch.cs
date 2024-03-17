using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace CrunchyDuck.Math {
	class Bill_Production_DoConfigInterface_Patch {
        public static MethodInfo Target() {
			return AccessTools.Method(typeof(Bill_Production), "DoConfigInterface");
		}

		public static void Postfix(Bill_Production __instance, Rect baseRect, Color baseColor) {
            BillComponent bc = BillManager.instance.AddGetBillComponent(__instance);
            if (bc == null) return;
            BillLinkTracker blt = bc.linkTracker;
			BillLinkTracker curr_copied = BillLinkTracker.currentlyCopied;

            var storeModeImage = Resources.bestStockpileImage;
            var nextStoreMode = BillStoreModeDefOf.DropOnFloor;
            //tip = "IW.ClickToTakeToStockpileTip".Translate();
            var tip = "Currently taking output to stockpile. Click to drop on floor.";
            if (__instance.GetStoreMode() == BillStoreModeDefOf.DropOnFloor) {
                storeModeImage = Resources.dropOnFloorImage;
                nextStoreMode = BillStoreModeDefOf.BestStockpile;
                // TODO: Implement translations.
                //var tip = "IW.ClickToDropTip".Translate();
                tip = "Currently dropping output on floor. Click to take to stockpile.";
            }
            // Drop/take to stockpile
            int elementoffset = 4;
            elementoffset += (ModLister.modsByPackageId.ContainsKey("Xandrmoro.Rim.Defaultie") && ModLister.GetModWithIdentifier("Xandrmoro.Rim.Defaultie").Active ? 1 : 0);
            var button_rect = new Rect(baseRect.xMax - (GUIExtensions.SmallElementSize + GUIExtensions.ElementPadding) * elementoffset + 12, baseRect.y, GUIExtensions.SmallElementSize, GUIExtensions.SmallElementSize);
            if (Widgets.ButtonImage(button_rect, storeModeImage, baseColor)) {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                __instance.SetStoreMode(nextStoreMode);
            }
            TooltipHandler.TipRegion(button_rect, tip);

            // Paste bill as linked
            button_rect.width = GUIExtensions.SmallElementSize;
            button_rect.x -= button_rect.width + GUIExtensions.ElementPadding;
			if (curr_copied != null && curr_copied != blt) {
				bool butt_enabled = blt.LinkWontCauseParadox(curr_copied);
                if (Widgets.ButtonText(button_rect, "", true, true, butt_enabled ? baseColor : Color.black, active: butt_enabled)) {
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    blt.LinkToParent(curr_copied);
				}
                // Link symbol
                Rect img = button_rect.ContractedBy(2);
                //TL;DR - something changed and idk how to color things now ;-;.
                //var col = Mouse.IsOver(button_rect) ? Widgets.MouseoverOptionColor : Widgets.NormalOptionColor;
				Widgets.DrawTextureFitted(img, Resources.linkImage, 1f);

                TooltipHandler.TipRegion(button_rect, butt_enabled ? "CD.M.tooltips.make_link".Translate() : "CD.M.tooltips.link_paradox".Translate());
            }
            // Break link to parent bill
            button_rect.width = GUIExtensions.SmallElementSize + GUIExtensions.ElementPadding + GUIExtensions.SmallElementSize;
            button_rect.x -= button_rect.width + GUIExtensions.ElementPadding;
            if (blt.Parent != null) {
                if (GUIExtensions.RenderBreakLink(blt, button_rect.x, button_rect.y)) {
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    blt.BreakLink();
                }
            }
        }
	}
}
