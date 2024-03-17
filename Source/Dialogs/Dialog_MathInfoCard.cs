using CrunchyDuck.Math.ModCompat;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

namespace CrunchyDuck.Math {
	// TODO: Possibly add a button to copy a variable directly from infocard.


	// oh my god the vanilla game dialog_infocard is programmed so poorly
	// the interface is so simple, yet for some reason instead of abstracting it down,
	// they have a shit load of different modes for different types of defs
	// it is some of the most poorly thought out code i've seen in a while

	// Hi this is Kd - I was thinking of removing this ^ big block of comments, but then I read them and almost died laughing, so they stay. Side-note: I couldn't agree more, Duck. It's real bad.
	class Dialog_MathInfoCard : Window {
		public List<StatDrawEntry> statEntries;
		public BillComponent attachedBill;
		public StatCategoryDef catIntroduction = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDIntroduction");
		public StatCategoryDef catPawnGroups = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDPawnGroups");
		public StatCategoryDef catModifiers = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDModifiers");
		public StatCategoryDef catTutorials = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDTutorials");
		public StatCategoryDef catExamples = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDExamples");
		public StatCategoryDef catFunctions = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDFunctions");
		public StatCategoryDef catBasics = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDBasics");
		public StatCategoryDef catTraits = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDTraits");
        public StatCategoryDef catWorkTags = DefDatabase<StatCategoryDef>.AllDefs.First(scd => scd.defName == "CDWorkTags");

        public static MethodInfo StatsWorker = AccessTools.Method(typeof(StatsReportUtility), "DrawStatsWorker");
		public static MethodInfo StatsFinalize = AccessTools.Method(typeof(StatsReportUtility), "FinalizeCachedDrawEntries");
		public static FieldInfo statsCache = AccessTools.Field(typeof(StatsReportUtility), "cachedDrawEntries");
#if v1_4
		public static FieldInfo statsCacheValues = AccessTools.Field(typeof(StatsReportUtility), "cachedEntryValues");
#endif
		public override Vector2 InitialSize => new Vector2(950f, 760f);
		public override float Margin => 0.0f;
		private InfoCardTab tab;

		// TODO BUG: Using search bar disables scroll.
		public override QuickSearchWidget CommonSearchWidget => StatsReportUtility.QuickSearchWidget;
		public override void Notify_CommonSearchChanged() => StatsReportUtility.Notify_QuickSearchChanged();

		private Dictionary<string, float> valueCache = new Dictionary<string, float>();

		public Dialog_MathInfoCard(BillComponent bc) {
			attachedBill = bc;
			// Get a cache of current values. I'll clean this up some other time.
			valueCache["pawns"] = bc.Cache.humanPawns.Count();
			object r;

			new MathFilters.PawnFilter(bc).Parse("colonists", out r);
			valueCache["colonists"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("mechanitors", out r);
			valueCache["mechanitors"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("slaves", out r);
			valueCache["slaves"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("prisoners", out r);
			valueCache["prisoners"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("guests", out r);
			valueCache["guests"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("animals", out r);
			valueCache["animals"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("adults", out r);
			valueCache["adults"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("kids", out r);
			valueCache["kids"] = ((MathFilters.MathFilter)r).Count();

			new MathFilters.PawnFilter(bc).Parse("babies", out r);
			valueCache["babies"] = ((MathFilters.MathFilter)r).Count();

			statEntries = GetBasicEntries();
			// If these values aren't reset you get some corruption nonsense because the system is jank.
#if v1_4
			statsCacheValues.SetValue(null, new List<string>());
#endif
			tab = InfoCardTab.Basic;

			forcePause = true;
			doCloseX = true;
			doCloseButton = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;
		}

		public override void Close(bool doCloseSound = true) {
			base.Close(doCloseSound);
		}

		// TODO: It would be nice if you could collapse categorydefs by clicking on them. Maybe add this later.
		public override void DoWindowContents(Rect inRect) {
			if (Math.IsNewImportantVersion()) {
				Find.WindowStack.Add(new Dialog_Updates());
			}
			if (MathSettings.settings.lastVersionInfocardChecked != Math.version) {
				MathSettings.settings.lastVersionInfocardChecked = Math.version;
				MathSettings.settings.Write();
			}
			List<TabRecord> tabs = new List<TabRecord>();

			tabs.Add(new TabRecord("Basic", () => tab = InfoCardTab.Basic, tab == InfoCardTab.Basic));
			tabs.Add(new TabRecord("CD.M.infocard.pawns".Translate(), () => tab = InfoCardTab.Pawns, tab == InfoCardTab.Pawns));
			tabs.Add(new TabRecord("CD.M.infocard.categories".Translate(), () => tab = InfoCardTab.Categories, tab == InfoCardTab.Categories));
			tabs.Add(new TabRecord("StatDefs", () => tab = InfoCardTab.StatDefs, tab == InfoCardTab.StatDefs));

			if (Math.compositableLoadoutsSupportEnabled)
				tabs.Add(new TabRecord("CD.M.infocard.compositable_loadouts".Translate(), () => tab = InfoCardTab.CompositableLoadouts, tab == InfoCardTab.CompositableLoadouts));

			Rect label_area = new Rect(inRect);
			label_area = label_area.ContractedBy(18f);
			label_area.height = 34f;
			label_area.xMax -= 34f;

			label_area.x += 34f;
			Text.Font = GameFont.Medium;
			Widgets.Label(label_area, "Math");

			Rect stats_area = new Rect(inRect);
			stats_area.yMin = label_area.yMax + 40f;
			stats_area.yMax += -38f;

			TabDrawer.DrawTabs(stats_area, tabs);

			// By default you need to pass in Defs to get Window to show entries. This gets around that.
			if (tab == InfoCardTab.Basic)
				statEntries = GetBasicEntries();
			else if (tab == InfoCardTab.Categories)
				statEntries = GetCategoriesEntries();
			else if (tab == InfoCardTab.Pawns)
				statEntries = GetPawnsEntries();
			else if (tab == InfoCardTab.StatDefs)
				statEntries = GetStatDefsEntries();
			else if (tab == InfoCardTab.CompositableLoadouts)
				statEntries = GetCompositableLoadoutsEntries();

            statsCache.SetValue(null, new List<StatDrawEntry>());
			statsCache.SetValue(null, statEntries);
			StatsFinalize.Invoke(null, new object[] { statsCache.GetValue(null) });

			// Draw image in top left.
			Rect card_image = new Rect(inRect.x + 9f, label_area.y, 34f, 34f);
			Widgets.ButtonImage(card_image, Resources.infoButtonImage, GUI.color);

			// Draw version number
			Rect version_butt = label_area.RightPartPixels(70);
			version_butt.xMax -= 15;
			if (Widgets.ButtonText(version_butt, "v" + Math.version)) {
				Find.WindowStack.Add(new Dialog_Updates());
			}

			StatsWorker.Invoke(null, new object[] { stats_area.ContractedBy(18f), null, null });
		}

		private List<StatDrawEntry> GetBasicEntries() {
			var stats = new List<StatDrawEntry>();
			StatDrawEntry stat;
			int display_priority = 10000;

			var cat = catIntroduction;
			stat = new StatDrawEntry(cat, "Description".Translate(), "", "CD.M.infocard.introduction.description".Translate(),
				display_priority--);
			stats.Add(stat);

			stats.Add(new StatDrawEntry(cat, "CD.M.infocard.basic.tutorials.stacks".Translate(), "", "CD.M.infocard.basic.tutorials.stacks.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "CD.M.infocard.basic.tutorials.variable_names".Translate(), "", "CD.M.infocard.basic.tutorials.variable_names.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "CD.M.infocard.basic.tutorials.itemcount".Translate(), "", "CD.M.infocard.basic.tutorials.itemcount.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "CD.M.infocard.basic.tutorials.doXTimes".Translate(), "", "CD.M.infocard.basic.tutorials.doXTimes.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "CD.M.infocard.basic.tutorials.user_variables".Translate(), "", "CD.M.infocard.basic.tutorials.user_variables.description".Translate(), display_priority--));

			// !!! BIG WARNING !!!
			// the label/second variable in these all have a zero-width space placed at the start.
			// This is done to force the variables to be lowercase in menus.
			// nice thinking oken
			// BEWARE THE HIDDEN HORRORS.
			cat = catExamples;
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.basic.examples.stacks".Translate(), "", "CD.M.infocard.basic.examples.stacks.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.basic.examples.pawns".Translate(), "", "CD.M.infocard.basic.examples.pawns.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.basic.examples.intake".Translate(), "", "CD.M.infocard.basic.examples.intake.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.basic.examples.meals".Translate(), "", "CD.M.infocard.basic.examples.meals.description".Translate(), display_priority--));

			//stats.Add(new StatDrawEntry(cat, "​" + "CD.M.tutorial.example.intake".Translate(), "", "CD.M.tutorial.example.intake.description".Translate(), display_priority--));
			//stats.Add(new StatDrawEntry(cat, "​" + "CD.M.tutorial.example.colonists".Translate(), "", "CD.M.tutorial.example.colonists.description".Translate(), display_priority--));
			//stats.Add(new StatDrawEntry(cat, "​" + "CD.M.tutorial.example.clothing.production".Translate(), "", "CD.M.tutorial.example.clothing.production.description".Translate(), display_priority--));
			//stats.Add(new StatDrawEntry(cat, "​" + "CD.M.tutorial.example.if".Translate(), "", "CD.M.tutorial.example.if.description".Translate(), 2950));

			return stats;
		}

		private List<StatDrawEntry> GetStatDefsEntries() {
			var stats = new List<StatDrawEntry>();
			int display_priority = 10000;

			var cat = catIntroduction;
			stats.Add(new StatDrawEntry(cat, "Description".Translate(), "", "CD.M.infocard.statdefs.description".Translate(), display_priority--));

			cat = catExamples;
			stats.Add(new StatDrawEntry(cat, "CD.M.infocard.statdefs.examples.pawn".Translate(), "", "CD.M.infocard.statdefs.examples.pawn.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "CD.M.infocard.statdefs.examples.nutrition".Translate(), "", "CD.M.infocard.statdefs.examples.nutrition.description".Translate(), display_priority--));

			cat = catModifiers;
			stats.Add(new StatDrawEntry(cat, "​" + "male", "", "CD.M.counters.male.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "female", "", "CD.M.counters.female.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "intake", "", "CD.M.counters.intake.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "bandwidth", "", "CD.M.counters.bandwidth.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "stack limit", "", "CD.M.counters.stack.limit.description".Translate(), display_priority--));

			cat = catBasics;
			var stats_sorted = Math.searchableStats.Values.OrderBy(t => t.label);
			foreach (StatDef statdef in stats_sorted) {
				stats.Add(new StatDrawEntry(cat, "​" + statdef.label.ToParameter(), "", statdef.description ?? "", display_priority--));
			}

			return stats;
		}

		private List<StatDrawEntry> GetCategoriesEntries() {
			var stats = new List<StatDrawEntry>();
			int display_priority = 10000;

			var cat = catIntroduction;
			stats.Add(new StatDrawEntry(cat, "Description".Translate(), "", "CD.M.infocard.categories.description".Translate(), display_priority--));

			cat = catBasics;
			var cats_sorted = MathFilters.CategoryFilter.searchableCategories.Values.OrderBy(t => t.label);
			foreach (ThingCategoryDef catdef in cats_sorted) {
				stats.Add(new StatDrawEntry(cat, "​" + catdef.label.ToParameter(), "", catdef.description ?? "", display_priority--));
			}

			return stats;
		}

		private List<StatDrawEntry> GetPawnsEntries() {
			var stats = new List<StatDrawEntry>();
			int display_priority = 10000;

			var cat = catIntroduction;
			stats.Add(new StatDrawEntry(cat, "Description".Translate(), "", "CD.M.infocard.pawns.description".Translate(), display_priority--));

			cat = catTutorials;
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.tutorials.searching_pawns".Translate(), "", "CD.M.infocard.pawns.tutorials.searching_pawns.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.tutorials.searching_pawn_stats".Translate(), "", "CD.M.infocard.pawns.tutorials.searching_pawn_stats.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.tutorials.searching_pawn_traits".Translate(), "", "CD.M.infocard.pawns.tutorials.searching_pawn_traits.description".Translate(), display_priority--));
            stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.tutorials.searching_pawn_worktags".Translate(), "", "CD.M.infocard.pawns.tutorials.searching_pawn_worktags.description".Translate(), display_priority--));
            stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.tutorials.filtering".Translate(), "", "CD.M.infocard.pawns.tutorials.filtering.description".Translate(), display_priority--));

			cat = catExamples;
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.examples.pawngroup".Translate(), "", "CD.M.infocard.pawns.examples.pawngroup.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.examples.individual_pawn".Translate(), "", "CD.M.infocard.pawns.examples.individual_pawn.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.examples.trait_searching".Translate(), "", "CD.M.infocard.pawns.examples.trait_searching.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "" + "CD.M.infocard.pawns.examples.skills".Translate(), "", "CD.M.infocard.pawns.examples.skills.description".Translate(), display_priority--));
            stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.examples.worktag_searching".Translate(), "", "CD.M.infocard.pawns.examples.worktag_searching.description".Translate(), display_priority--));
            stats.Add(new StatDrawEntry(cat, "​" + "CD.M.infocard.pawns.examples.pawngroup_filtering".Translate(), "", "CD.M.infocard.pawns.examples.pawngroup_filtering.description".Translate(), display_priority--));

			cat = catPawnGroups;
			stats.Add(new StatDrawEntry(cat, "​" + "animals", "", "CD.M.infocard.pawns.animals.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "pawns", "", "CD.M.infocard.pawns.pawns.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "mechanoids", "", "CD.M.infocard.pawns.mechanoids.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "colonists", "", "CD.M.infocard.pawns.colonists.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "guests", "", "CD.M.infocard.pawns.guests.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "prisoners", "", "CD.M.infocard.pawns.prisoners.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "slaves", "", "CD.M.infocard.pawns.slaves.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "adults", "", "CD.M.infocard.pawns.adults.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "kids", "", "CD.M.infocard.pawns.kids.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "babies", "", "CD.M.infocard.pawns.babies.description".Translate(), display_priority--));
			stats.Add(new StatDrawEntry(cat, "​" + "mechanitors", "", "CD.M.infocard.pawns.mechanitors.description".Translate(), display_priority--));

			// Order traits alphabetically
			cat = catTraits;
			var traits_sorted = Math.searchableTraits.Values.OrderByDescending(t => t.traitDef.degreeDatas[t.index].label);
			int i = 0;
			// Display traits
			foreach (var (traitDef, index) in traits_sorted) {
				var trait_dat = traitDef.degreeDatas[index];
				stats.Add(new StatDrawEntry(cat, "​" + trait_dat.label.ToParameter(), "", trait_dat.description, i++));
			}

			// Work Tags
			cat = catWorkTags;
            foreach (string tag in Enum.GetNames(typeof(WorkTags)).OrderByDescending(w => w))
            {
                stats.Add(new StatDrawEntry(cat, tag, "", CharacterCardUtility.GetWorkTypesDisabledByWorkTag((WorkTags)Enum.Parse(typeof(WorkTags),tag)), i++));
            }

            return stats;
		}

		private List<StatDrawEntry> GetCompositableLoadoutsEntries() {
			var stats = new List<StatDrawEntry>();
			int display_priority = 10000;
			
			var cat = catIntroduction;
			stats.Add(new StatDrawEntry(cat, "CD.M.infocard.compositable_loadouts".Translate(), "", "CD.M.infocard.compositable_loadouts.description".Translate(), display_priority--));
			
			cat = catExamples;
			IReadOnlyList<object> tags = CompositableLoadoutsSupport.GetTags(); 
			if (tags.Count == 0) {
				stats.Add(new StatDrawEntry(cat, "CD.M.infocard.compositable_loadouts.no_tags".Translate(), "", "CD.M.infocard.compositable_loadouts.no_tags.description".Translate(), display_priority--));
			}
			else {
				var tagNames = new SortedSet<string>();
				foreach (object tag in tags) {
					tagNames.Add(CompositableLoadoutsSupport.GetTagName(tag));
				}
				foreach (string tag in tagNames) {
					stats.Add(new StatDrawEntry(cat, "CD.M.infocard.compositable_loadouts.tag".Translate(tag.ToParameter()), "", "CD.M.infocard.compositable_loadouts.tag.description".Translate(tag), display_priority--));
				}
			}
			return stats;
		}
	}

	public enum InfoCardTab {
		Basic,
		Pawns,
		Categories,
		StatDefs,
		CompositableLoadouts
	}
}
