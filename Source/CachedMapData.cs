using CrunchyDuck.Math.MathFilters;
using CrunchyDuck.Math.ModCompat;
using LazyCache;
using RimWorld;
using System.Collections.Generic;
using Verse;
using ThingFilter = CrunchyDuck.Math.MathFilters.ThingFilter;

namespace CrunchyDuck.Math {
	public class CachedMapData {
		private Map map;

		public Dictionary<string, Pawn> pawns_dict = new Dictionary<string, Pawn>();
		public List<Thing> humanPawns = new List<Thing>();
		public List<Thing> ownedAnimals = new List<Thing>();
		public Dictionary<string, List<Thing>> resources = new Dictionary<string, List<Thing>>();
		private IAppCache appCache = new CachingService();

		public CachedMapData(Map map) {
			this.map = map;
			foreach (Pawn p in map.mapPawns.AllPawns) {
				bool in_faction = p.Faction == Faction.OfPlayer;
				bool animal = p.AnimalOrWildMan();
				bool guest = p.IsQuestLodger() || p.guest?.HostFaction == Faction.OfPlayer;
				bool prisoner = p.IsPrisonerOfColony;
				bool slave = p.IsSlaveOfColony;

				if (animal && in_faction) {
					ownedAnimals.Add(p);
					pawns_dict[p.LabelShort.ToParameter()] = p;
				}
				else {
					if (in_faction || guest || prisoner || slave) {
						humanPawns.Add(p);
						pawns_dict[p.LabelShort.ToParameter()] = p;
					}
				}
			}
		}

		public bool SearchVariable(string input,BillComponent bc,out float count)
        {
			bool ret = false;
			count = appCache.GetOrAdd(bc.targetBill.GetUniqueLoadID(), () =>
			{
				float res = 0f;
				ret = SyncSearchVariable(input, bc, out res);
				return res;
			},System.DateTimeOffset.Now.AddSeconds(5)); //Recalc this every ~5 seconds
			return ret;
        }

		public bool SyncSearchVariable(string input, BillComponent bc, out float count) {
			count = 0;
			string[] commands = input.Split('.');
			MathFilter filter = null;
			for (int i = 0; i < commands.Length; i++) {
				string command = commands[i];
				// Initialize a filter.
				if (filter == null) {
					// thing
					if (Math.searchableThings.ContainsKey(command)) {
						filter = new ThingFilter(bc, command);
						continue;
					}
					// category
					else if (CategoryFilter.names.Contains(command)) {
						if (i + 1 < commands.Length &&
						    CategoryFilter.searchableCategories.TryGetValue(commands[++i], out ThingCategoryDef value)) {
							filter = new ThingFilter(bc, value);
							continue;
						}
						else
							return false;
					}
					// thingdef
					else if (ThingDefFilter.names.Contains(command)) {
						if (i + 1 < commands.Length &&
						    Math.searchableThings.TryGetValue(commands[++i], out ThingDef value)) {
							filter = new ThingDefFilter(value);
							continue;
						}
						else
							return false;
					}
					// pawn
					else if (PawnFilter.filterMethods.ContainsKey(command) || pawns_dict.ContainsKey(command)) {
						filter = new PawnFilter(bc);
					}
					// compositable loadouts; Has to be extracted to separate methods because otherwise it throws exceptions when run without the mod.
					else if (Math.compositableLoadoutsSupportEnabled && CompositableLoadoutTagsFilter.names.Contains(command)) {
						if (i + 1 < commands.Length && CompositableLoadoutsSupport.GetCompositableLoadoutFilter(commands[++i], bc, ref filter))
							continue;
						return false;
					}
					// Can't find filter.
					else {
						return false;
					}

				}

				// Parse input
				ReturnType type = filter.Parse(command, out object result);
				switch (type) {
					case ReturnType.Count:
						count = (float) result;
						return true;
					case ReturnType.Null:
						return false;
					default:
						filter = (MathFilter) result;
						break;
				}
			}

			if (filter.CanCount) {
				count = filter.Count();
				return true;
			}

			return false;
		}

		public List<Thing> GetThings(string thing_name, BillComponent bc) {
			List<Thing> found_things = new List<Thing>();
			// Fill the list of *all* of this thing first
			if (!resources.ContainsKey(thing_name)) {
				ThingDef td = Math.searchableThings[thing_name];
				// Patch to fix a missing key bug report:
				// https://steamcommunity.com/workshop/filedetails/discussion/2876902608/3487500856972015279/?ctp=3#c3495383439605482600
				var l = map.listerThings.ThingsOfDef(td).ListFullCopy();
				resources[thing_name] = l ?? new List<Thing>();
				// Count equipped/inventory/hands.
				foreach (Pawn pawn in map.mapPawns.FreeColonistsAndPrisonersSpawned) {
					List<Thing> things = GetThingInPawn(pawn, td);
					foreach (var thing in things) {
						resources[thing_name].Add(thing);
					}
				}

				if (Math.rimfactorySupportEnabled)
					resources[thing_name].AddRange(RimFactorySupport.GetThingsFromPRF(map, td));
			}

			// TODO: Index things that are on corpses. 
			// Filter this thing based on parameters.
			foreach (Thing _thing in resources[thing_name]) {
				Thing thing = _thing.GetInnerIfMinified();
				// Check if in stockpile.
				// TODO: Make default only check stockpiles, with an option to make it check everywhere.
				var zone = bc.targetBill.includeGroup;
				if (zone != null && !zone.CellsList.Contains(thing.InteractionCell)) {
					continue;
				}

				// Forbidden
				if (thing.IsForbidden(Faction.OfPlayer))
					continue;

				// Hitpoints
				if (thing.def.useHitPoints &&
				    !bc.targetBill.hpRange.Includes((float) thing.HitPoints / (float) thing.MaxHitPoints)) {
					continue;
				}

				// Quality
				if (thing.TryGetQuality(out QualityCategory q) && !bc.targetBill.qualityRange.Includes(q))
					continue;

				var producted_thing = bc.targetBill.recipe.ProducedThingDef;
				if (producted_thing != null) {
					// Tainted
					bool can_choose_tainted = producted_thing.IsApparel && producted_thing.apparel.careIfWornByCorpse;
					Apparel a = thing.GetType() == typeof(Apparel) ? (Apparel) thing : null;
					if (can_choose_tainted && !bc.targetBill.includeTainted && a?.WornByCorpse == true)
						continue;

					// Equipped.
					//bool can_choose_equipped = producted_thing.IsWeapon || producted_thing.IsApparel;
					if (/**can_choose_equipped &&**/ !bc.targetBill.includeEquipped && thing.IsHeldByPawn()) {
						continue;
					}
				}

				found_things.Add(thing);
			}

			return found_things;
		}

		// Code taken from RecipeWorkerCounter.CountProducts
		public static List<Thing> GetThingInPawn(Pawn pawn, ThingDef def) {
			List<Thing> things = new List<Thing>();
			foreach (ThingWithComps equipment in pawn.equipment.AllEquipmentListForReading) {
				if (equipment.def.Equals(def)) {
					things.Add((Thing) equipment);
				}
			}

			foreach (var apparel in pawn.apparel.WornApparel) {
				if (apparel.def.Equals(def)) {
					things.Add((Thing) apparel);
				}
			}

			foreach (Thing heldThing in pawn.inventory.innerContainer.InnerListForReading) {
				if (heldThing.def.Equals(def)) {
					things.Add(heldThing);
				}
			}

			return things;
		}
	}
	//public struct SearchVariableReturn {
	//	public bool success;
	//	public float count;
	//	public ReturnType type;

	//	public SearchVariableReturn(ReturnType type, bool success) {
	//		this.type = type;
	//		this.success = success;
	//	}
	//}
}
