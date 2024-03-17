using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

namespace CrunchyDuck.Math {
	public class BillLinkTracker : IExposable {
		public static BillLinkTracker currentlyCopied;
		/// I separate out these links
		/// 1. so they can be iterated nicely.
		/// 2. so they can have nicer display values, rather than "link 263"
		public static SortedDictionary<int, BillLinkTracker> linkIDs = new SortedDictionary<int, BillLinkTracker>();
		private static int nextLinkID = 0;
		public static int NextLinkID { 
			get {
				if (!linkIDs.ContainsKey(nextLinkID))
					return nextLinkID;
				// Find next vacant ID.
				for (int i = 0; ; i++) {
					if (!linkIDs.ContainsKey(i)) {
						nextLinkID = i;
						return nextLinkID;
					}
				}
			}
		}
		public static SortedDictionary<int, BillLinkTracker> IDs = new SortedDictionary<int, BillLinkTracker>();
		private static int nextID = 0;
		public static int NextID {
			get {
				if (!IDs.ContainsKey(nextID))
					return nextID;
				// Find a vacant ID.
				for (int i = 0; ; i++) {
					if (!IDs.ContainsKey(i)) {
						nextID = i;
						return nextID;
					}
				}
			}
		}

		public BillComponent bc;

		private int myID = -1;  // What's my ID in IDs?
		private int parentID = -1;  // What my parent's ID is in linkIDs
		public int linkID = -1;  // What my ID is in linkIDs
		public bool isMasterBC = false;
		private HashSet<int> tmp_childIDs;  // Used to resolve cross refs when loading.
		private HashSet<BillLinkTracker> children = new HashSet<BillLinkTracker>();

		public BillLinkTracker Parent {
			get {
				if (parentID == -1)
					return null;
				if (linkIDs.ContainsKey(parentID)) {
					return linkIDs[parentID];
				}
				parentID = -1;
				return null;
			}
			set {
				if (value == null)
					parentID = -1;
				else
					parentID = value.linkID;
			}
		}

		public LinkSettings linkSettings;
		//public Dictionary<string, LinkSetting> linkSettings = new Dictionary<string, LinkSetting>();

		public static void ResetStatic() {
			nextID = 0;
			nextLinkID = 0;
			linkIDs = new SortedDictionary<int, BillLinkTracker>();
			IDs = new SortedDictionary<int, BillLinkTracker>();
			currentlyCopied = null;
		}

		public BillLinkTracker(BillComponent bc) {
			this.bc = bc;
			if (Scribe.mode == LoadSaveMode.LoadingVars) {
				myID = NextID;
				IDs[myID] = this;
			}
			linkSettings = new LinkSettings(this);// GenerateLinkSettings(this);
		}

		public void ExposeData() {
			Scribe_Values.Look(ref myID, "myID", -1, true);
			if (Scribe.mode == LoadSaveMode.LoadingVars) {
				if (myID == -1)
					myID = NextID;
				IDs[myID] = this;
			}
			Scribe_Values.Look(ref linkID, "linkID", -1);
			if (linkID != -1)
				linkIDs[linkID] = this;
			Scribe_Values.Look(ref isMasterBC, "isMasterBC", false);
			Scribe_Values.Look(ref parentID, "parentID", -1);

			Scribe_Deep.Look(ref linkSettings, "settings", this);
			if (linkSettings == null)
				linkSettings = new LinkSettings(this);

			// LoadSaveMode.ResolvingCrossRefs doesn't seem to actually load anything; I need to store the relevant data on my own.
			if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.Saving) {
				tmp_childIDs = children.Select(bc => bc.myID).ToHashSet();
				Scribe_Collections.Look(ref tmp_childIDs, "children", LookMode.Value);
			}
			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs) {
				if (tmp_childIDs.Count > 0) {
					children = tmp_childIDs.Select(_i => IDs[_i]).ToHashSet();
				}
			}
		}

		public void UpdateChildren() {
			if (children == null)
				return;
			foreach (BillLinkTracker c in children) {
				c.UpdateFromParent();
			}
		}

		/// <summary>
		/// Make this child update its parents' values.
		/// </summary>
		public void UpdateToParent() {
			if (Parent == null)
				return;

			foreach (LinkSetting sett in linkSettings) {
				if (!sett.Enabled)
					continue;
				sett.UpdateToParent();
			}
			Parent.UpdateChildren();
			Parent.UpdateToParent();
		}

		public void UpdateFromParent() {
			foreach (LinkSetting sett in linkSettings) {
				if (!sett.Enabled)
					continue;
				sett.UpdateFromParent();
			}
			UpdateChildren();
		}

		/// <summary>
		/// Pastes every compatible setting, except name.
		/// </summary>
		public static void PasteAll(BillLinkTracker from, BillLinkTracker to) {

		}

		/// <summary>
		/// Break the link with a parent, from the child.
		/// </summary>
		public void BreakLink() {
			Parent.BreakLink(this);
		}

		/// <summary>
		/// Break the link with a child, from the parent.
		/// </summary>
		public void BreakLink(BillLinkTracker child) {
			RemoveChild(child);
			child.Parent = null;
		}

		public void LinkToParent(BillLinkTracker parent) {
			parent.LinkToChild(this);
		}

		public void LinkToChild(BillLinkTracker child) {
			// steal baby
			if (child.Parent != null) {
				child.Parent.BreakLink(child);
			}
			AddChild(child);
			child.UpdateFromParent();
		}

		public bool LinkWontCauseParadox(BillLinkTracker potential_parent) {
			// Check if the potential parent has any parents. If it does, are any of these parents our children?
			var par = potential_parent;
			while (par.Parent != null) {
				if (par.Parent == this)
					return false;
				par = par.Parent;
			}
			return true;
		}

		public bool AreIngredientsCompatible(BillComponent other) {
			// TODO: Special filters. I dislike vanilla's filter code immensely.
			// Can fixedIngredientFilter be null? i suspect so.
			if (bc.targetBill.recipe.fixedIngredientFilter == null || other.targetBill.recipe.fixedIngredientFilter == null) {
				Log.ErrorOnce("Hello! CrunchyDuck from Math!. Please tell me what recipe that was on so I improve my code :)", 2278);
				return false;
			}
			var t_defs = bc.targetBill.recipe.fixedIngredientFilter.allowedDefs;
			var o_defs = other.targetBill.recipe.fixedIngredientFilter.allowedDefs;

			return t_defs.SetEquals(o_defs);
		}
	
		public bool AreStockpilesCompatible(BillComponent other) {
			// This is the same code that is used to determine if you can select a stockpile, in RecipeWorkerCounter.CanPossiblyStoreInStockpile
			return bc.targetBill.recipe.products[0].thingDef == other.targetBill.recipe.products[0].thingDef;
		}

		public bool AreWorkersCompatible(BillComponent other) {
			// Similar definition to what's used in
			//  Dialog_BillConfig.GeneratePawnRestrictionOptions &
			//  BillDialogUtility.GetPawnRestrictionOptionsForBill
			var mb = bc.targetBill;
			var ob = other.targetBill;
			var mwg = mb.billStack.billGiver.GetWorkgiver();
			var owg = ob.billStack.billGiver.GetWorkgiver();

			if (mb.recipe.mechanitorOnlyRecipe != ob.recipe.mechanitorOnlyRecipe)
				return false;
			//if (mb.recipe.workSkill != ob.recipe.workSkill)
			//	return false;
			if (mwg != owg)
				return false;

			return true;
		}

		private static void MatchInputField(InputField from, InputField to) {
			to.buffer = from.buffer;
			to.CurrentValue = from.CurrentValue;
			to.lastValid = from.lastValid;
		}

		private static void MatchIngredients(BillComponent from, BillComponent to) {
			to.targetBill.ingredientFilter.allowedDefs = new HashSet<ThingDef>(from.targetBill.ingredientFilter.allowedDefs);
			if (from.linkTracker.linkSettings.ingredientsRadius.Enabled)
				to.targetBill.ingredientSearchRadius = from.targetBill.ingredientSearchRadius;
			// I believe that because they're structs, this is fine. I'm still learning to ref vs val stuff.
			to.targetBill.ingredientFilter.AllowedHitPointsPercents = from.targetBill.ingredientFilter.AllowedHitPointsPercents;
			to.targetBill.ingredientFilter.AllowedQualityLevels = from.targetBill.ingredientFilter.AllowedQualityLevels;
		}

		private static bool CanCountProducts(BillLinkTracker blt) {
			// Taken from RecipeWorkerCounter.CanCountProducts (Why does that method need bill?)
			var recipe = blt.bc.targetBill.recipe;
			return recipe.specialProducts == null && recipe.products != null && recipe.products.Count == 1;
		}

		/// <summary>
		/// Add a child, and inform that child it has been a new daddy.
		/// </summary>
		private void AddChild(BillLinkTracker child) {
			if (!isMasterBC) {
				isMasterBC = true;
				linkID = NextLinkID;
				linkIDs[linkID] = this;
			}

			child.Parent = this;
			foreach (var sett in child.linkSettings) {
				sett.Reset();
			}

			child.linkSettings.targetStockpile.state = child.linkSettings.targetStockpile.compatibleWithParent = AreStockpilesCompatible(child.bc);
			child.linkSettings.repeatMode.state = child.linkSettings.repeatMode.compatibleWithParent = CanCountProducts(child) == CanCountProducts(this);
			child.linkSettings.workers.state = child.linkSettings.workers.compatibleWithParent = AreWorkersCompatible(child.bc);
			child.linkSettings.ingredients.state = child.linkSettings.ingredients.compatibleWithParent = AreIngredientsCompatible(child.bc);
			children.Add(child);
		}

		/// <summary>
		/// Remove a child. This does not inform the child it has been abandoned.
		/// </summary>
		private void RemoveChild(BillLinkTracker child) {
			children.Remove(child);
			if (children.Count == 0) {
				isMasterBC = false;
				linkIDs.Remove(linkID);
				linkID = -1;
			}
		}


		public class LinkSetting {
			public string displayName;
			public string tooltip;
			public string tooltipIncompatible;
			public BillLinkTracker owner;
			private Action<BillLinkTracker, BillLinkTracker> update = null;

			public bool defaultState = true;
			public bool state = true;
			public bool compatibleWithParent = true;
			public bool Enabled { get { return state && compatibleWithParent; } set { state = value; } }


			public LinkSetting(BillLinkTracker owner, string display_name, Action<BillLinkTracker, BillLinkTracker> update, bool default_state = true, string tooltip = null, string tooltip_incompatible = null) {
				this.owner = owner;
				this.displayName = display_name;
				this.update = update;
				this.defaultState = this.state = default_state;
				this.tooltipIncompatible = tooltip_incompatible;
				this.tooltip = tooltip;
			}

			public void UpdateFromParent() {
				update(owner.Parent, owner);
			}

			public void UpdateToParent() {
				update(owner, owner.Parent);
			}

			public void Reset() {
				state = defaultState;
			}
		}

		public class LinkSettings : IEnumerable<LinkSetting>, IExposable {
			public BillLinkTracker lt;

			public LinkSetting name;
			public LinkSetting suspended;
			public LinkSetting targetCount;
			public LinkSetting customItemCount;
			public LinkSetting pause;
			public LinkSetting tainted;
			public LinkSetting equipped;
			public LinkSetting onlyAllowedIngredients;
			public LinkSetting countHP;
			public LinkSetting countQuality;
			public LinkSetting checkStockpile;
			public LinkSetting ingredientsRadius;
			public LinkSetting targetStockpile;
			public LinkSetting repeatMode;
			public LinkSetting workers;
			public LinkSetting ingredients;

			public LinkSettings(BillLinkTracker owner) {
				lt = owner;
				Action<BillLinkTracker, BillLinkTracker> update;

				update = (from, to) => to.bc.name = from.bc.name;
				name = new LinkSetting(owner, "CD.M.link.name".Translate(), update, false);

				update = (from, to) => to.bc.targetBill.suspended = from.bc.targetBill.suspended;
				suspended = new LinkSetting(owner, "CD.M.link.suspended".Translate(), update);

				update = (from, to) => {
					MatchInputField(from.bc.doXTimes, to.bc.doXTimes);
					MatchInputField(from.bc.doUntilX, to.bc.doUntilX);
				};
				targetCount = new LinkSetting(owner, "CD.M.link.target_count".Translate(), update);

				update = (from, to) => {
					MatchInputField(from.bc.itemsToCount, to.bc.itemsToCount);
					to.bc.customItemsToCount = from.bc.customItemsToCount;
				};
				customItemCount = new LinkSetting(owner, "CD.M.link.custom_item_count".Translate(), update);

				update = (from, to) => {
					MatchInputField(from.bc.unpause, to.bc.unpause);
					to.bc.targetBill.paused = from.bc.targetBill.paused;
					to.bc.targetBill.pauseWhenSatisfied = from.bc.targetBill.pauseWhenSatisfied;
				};
				pause = new LinkSetting(owner, "CD.M.link.pause".Translate(), update);

				update = (from, to) => to.bc.targetBill.includeTainted = from.bc.targetBill.includeTainted;
				tainted = new LinkSetting(owner, "CD.M.link.tainted".Translate(), update);

				update = (from, to) => to.bc.targetBill.includeEquipped = from.bc.targetBill.includeEquipped;
				equipped = new LinkSetting(owner, "CD.M.link.equipped".Translate(), update);

				update = (from, to) => to.bc.targetBill.limitToAllowedStuff = from.bc.targetBill.limitToAllowedStuff;
				onlyAllowedIngredients = new LinkSetting(owner, "CD.M.link.only_allowed_ingredients".Translate(), update);

				update = (from, to) => to.bc.targetBill.hpRange = from.bc.targetBill.hpRange;
				countHP = new LinkSetting(owner, "CD.M.link.count_hp".Translate(), update);

				update = (from, to) => to.bc.targetBill.qualityRange = from.bc.targetBill.qualityRange;
				countQuality = new LinkSetting(owner, "CD.M.link.count_quality".Translate(), update);

				update = (from, to) => to.bc.targetBill.includeGroup = from.bc.targetBill.includeGroup;
				checkStockpile = new LinkSetting(owner, "CD.M.link.check_stockpile".Translate(), update);

				update = (from, to) => to.bc.targetBill.ingredientSearchRadius = from.bc.targetBill.ingredientSearchRadius;
				ingredientsRadius = new LinkSetting(owner, "CD.M.link.ingredients_radius".Translate(), update);


				update = (from, to) => to.bc.targetBill.SetStoreMode(from.bc.targetBill.GetStoreMode());
				targetStockpile = new LinkSetting(owner, "CD.M.link.target_stockpile".Translate(), update, tooltip_incompatible: "CD.M.link.target_stockpile_incompatible".Translate());

				update = (from, to) => to.bc.targetBill.repeatMode = from.bc.targetBill.repeatMode;
				repeatMode = new LinkSetting(owner, "CD.M.link.repeat_mode".Translate(), update, tooltip:"CD.M.link.repeat_mode.description".Translate(), tooltip_incompatible: "CD.M.link.repeat_mode_incompatible".Translate());

				update = (from, to) => {
					// Similar code to Bill.SetPawnRestriction
					to.bc.targetBill.pawnRestriction = from.bc.targetBill.pawnRestriction;
					to.bc.targetBill.slavesOnly = from.bc.targetBill.slavesOnly;
					to.bc.targetBill.mechsOnly = from.bc.targetBill.mechsOnly;
					to.bc.targetBill.nonMechsOnly = from.bc.targetBill.nonMechsOnly;
				};
				workers = new LinkSetting(owner, "CD.M.link.workers".Translate(), update, tooltip_incompatible: "CD.M.link.workers_incompatible".Translate());

				update = (from, to) => MatchIngredients(from.bc, to.bc);
				ingredients = new LinkSetting(owner, "CD.M.link.ingredients".Translate(), update, tooltip_incompatible: "CD.M.link.ingredients_incompatible".Translate());

				ingredientsRadius.state = ingredients.Enabled;
			}

			public IEnumerator<LinkSetting> GetEnumerator() {
				return Enumerate();
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return Enumerate();
			}

			IEnumerator<LinkSetting> Enumerate() {
				yield return name;
				yield return suspended;
				yield return targetCount;
				yield return customItemCount;
				yield return pause;
				yield return tainted;
				yield return equipped;
				yield return onlyAllowedIngredients;
				yield return countHP;
				yield return countQuality;
				yield return checkStockpile;
				yield return ingredientsRadius;
				yield return targetStockpile;
				yield return repeatMode;
				yield return workers;
				yield return ingredients;
			}

			public void ExposeData() {
				int i = 0;
				foreach (LinkSetting sett in this) {
					// This method means that if the order of settings changes, loads will get weird. Don't do that.
					Scribe_Values.Look(ref sett.state, "setting_" + i);
					Scribe_Values.Look(ref sett.state, "setting_" + i + "compatible");
					i++;
				}
			}
		}
	}
}
