using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CrunchyDuck.Math {
	// TODO: Handle copy/pasting.
	class BillManager : GameComponent {
		public static BillManager instance;  // singleton my beloved

		public Dictionary<int, BillComponent> billTable = new Dictionary<int, BillComponent>();
		public const int updateRegularity = 2500;  // 1 in game hour.
		public static Dictionary<string, ThingDef> searchabeThings = new Dictionary<string, ThingDef>();

		public BillManager(Game game) {
			instance = this;
			// Reset static variables.
			BillLinkTracker.ResetStatic();

		}

		// Create it if it doesn't exist and return it.
		public BillComponent AddGetBillComponent(Bill_Production bill) {
			int load_id = GetBillID(bill);
			if (load_id >= 0)
            {
				BillComponent bill_comp;
				if (billTable.ContainsKey(load_id)) {
					bill_comp = billTable[load_id];
				}
				else
				{
					Math.ExtLog("Found bill with Load ID " + load_id + " (" + bill.Label + ") without a BillComponent. Creating a new one...");
					bill_comp = new BillComponent(bill);
					billTable[load_id] = bill_comp;
				}
				return bill_comp;
            }
			return null;
		}

		public override void GameComponentTick() {
			base.GameComponentTick();
			if (Current.Game.tickManager.TicksGame % updateRegularity == 0) {
				MathTick();
			}
		}

		/// <summary>
		/// Updates bills, mostly.
		/// </summary>
		public void MathTick()
        {
            Math.ClearCacheMaps();

            // Update linked bills.
            foreach (BillLinkTracker blt in BillLinkTracker.linkIDs.Values) {
				blt.UpdateChildren();
			}

			// We create a copy as billTable can be modified during iteration by having null bills removed.
			// It would be more elegant to throw and catch an error to make this object handle the billTable,
			// But I use Math.DoMath in a lot of places outside of here that I don't want erroring.
			Dictionary<int, BillComponent> bt_copy = billTable.ToDictionary(entry => entry.Key, entry => entry.Value);
			foreach (BillComponent bc in bt_copy.Values) {
				UpdateBill(bc);
				// TODO: This is a rough fix for doXTimes being messy with Math. Find a more reliable way to update the doXTimes buffer.
				bc.doXTimes.SetAll(UnityEngine.Mathf.CeilToInt(bc.doXTimes.CurrentValue));
			}
		}

		public static void UpdateBill(BillComponent bc) {
			Math.DoMath(bc.doUntilX.lastValid, bc.doUntilX);
			//Math.DoMath(item.repeat_count_last_valid, ref item.targetBill.repeatCount, item);
			Math.DoMath(bc.unpause.lastValid, bc.unpause);
			Math.DoMath(bc.itemsToCount.lastValid, bc.itemsToCount);
		}

		public void RemoveBillComponent(BillComponent bc) {
			var i = billTable.FirstIndexOf(kvp => kvp.Value == bc);
			billTable.Remove(i);
		}

		public static int GetBillID(Bill_Production bill_production) {
			return (int)AccessTools.Field(typeof(Bill), "loadID").GetValue(bill_production);
		}
	}
}
