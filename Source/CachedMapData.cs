using System;
using CrunchyDuck.Math.MathFilters;
using CrunchyDuck.Math.ModCompat;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Verse;
using ThingFilter = CrunchyDuck.Math.MathFilters.ThingFilter;

namespace CrunchyDuck.Math {
	// TODO: Add in checking against pawn skills, like "get all pawns with shooting > 3"
	public class CachedMapData 
	{
		private readonly Map _map;
		//Unused
		//private static Regex v13_getIntake = new Regex(@"Final value: (\d+(?:.\d+)?)", RegexOptions.Compiled);

		public Dictionary<string, Pawn> pawns_dict = new Dictionary<string, Pawn>();
		public readonly List<Thing> humanPawns = new List<Thing>();
		public readonly List<Thing> ownedAnimals = new List<Thing>();
		public Dictionary<string, List<Thing>> resources = new Dictionary<string, List<Thing>>();

        public Map GetMap => _map;
        public bool UpdateRequested { get; private set; } = false;

        public CachedMapData(Map map, CancellationToken ct) 
        {
			this._map = map;

			foreach (var p in map.mapPawns.AllPawns.Where(pawn => pawn.Faction == Faction.OfPlayer)) 
			{
				if (ct.IsCancellationRequested) return; // This cache is no longer valid. We can drop the caching.
				
				var guest = p.IsQuestLodger() || p.guest?.HostFaction == Faction.OfPlayer;
				var prisoner = p.IsPrisonerOfColony;
				var slave = p.IsSlaveOfColony;

				if (p.AnimalOrWildMan()) 
				{
					ownedAnimals.Add(p);
					pawns_dict[p.LabelShort.ToParameter()] = p;
					continue;
				}
				
				 //TODO: optimize here.
				if (guest || prisoner || slave) 
				{
						humanPawns.Add(p);
						pawns_dict[p.LabelShort.ToParameter()] = p;
				}
				
			}
		}

		// Ugly async call from sync method. TODO: add Cancelation Token at least.
        public void RequestUpdate(Action<CachedMapData> onUpdate, CancellationToken ct)
        {
			if(UpdateRequested) return;
            UpdateRequested = true;
#pragma warning disable 4014
            PerformUpdate(onUpdate, ct);
#pragma warning restore 4014
        }

        private async Task PerformUpdate(Action<CachedMapData> onUpdate, CancellationToken ct)
        {
            CachedMapData newMapData = default;

			try
            {
                newMapData = new CachedMapData(this.GetMap, ct);
                if (ct.IsCancellationRequested) return;
            }
            catch (NullReferenceException)
            {
	            if (ct.IsCancellationRequested) return;
				// Map was destroyed.
				onUpdate?.Invoke(default);
				return;
			}
            if (ct.IsCancellationRequested) return;
            onUpdate?.Invoke(newMapData);
		}


        /// <summary>
        /// Searches for a variable in an input string based on the given bill component.
        /// </summary>
        /// <param name="input">The input string to search in.</param>
        /// <param name="bc">The bill component to use for searching.</param>
        /// <param name="count">The variable count found in the input string.</param>
        /// <returns>Returns true if the variable is found in the input string and the count is set; otherwise, false.</returns>
        public bool SearchVariable(string input, BillComponent bc, out float count) 
        {
			count = 0;
			var commands = input.Split('.');
			MathFilter filter = null;
			for (var i = 0; i < commands.Length; i++) 
			{
				var command = commands[i];
				// Initialize a filter.
				if (filter == null) 
				{
					// thing
					if (Math.searchableThings.ContainsKey(command)) 
					{
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
				var type = filter.Parse(command, out object result);
				switch (type) 
				{
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

			if (filter == null) return false;
			
			if (filter.CanCount) 
			{
				count = filter.Count();
				return true;
			}

			return false;
		}

		public List<Thing> GetThings(string thingName, BillComponent bc) 
		{
			var foundThings = new List<Thing>();
			
			// Fill the list of *all* of this thing first
			if (!resources.ContainsKey(thingName)) 
			{
				var td = Math.searchableThings[thingName];
				
				// Patch to fix a missing key bug report:
				// https://steamcommunity.com/workshop/filedetails/discussion/2876902608/3487500856972015279/?ctp=3#c3495383439605482600
				var l = _map.listerThings.ThingsOfDef(td).ListFullCopy();
				resources[thingName] = l ?? new List<Thing>();
				// Count equipped/inventory/hands.
				foreach (var thing in _map.mapPawns.FreeColonistsAndPrisonersSpawned.Select(pawn => GetThingInPawn(pawn, td)).SelectMany(things => things))
				{
					resources[thingName].Add(thing);
				}

				if (Math.rimfactorySupportEnabled)
					resources[thingName].AddRange(RimFactorySupport.GetThingsFromPRF(_map, td));
			}

			// TODO: Index things that are on corpses. 
			// Filter this thing based on parameters.
			var producedThing = bc.targetBill.recipe.ProducedThingDef;
			if (producedThing == null)
			{
				//TODO:
				return new List<Thing>();
			}

			foreach (var thing in from t in resources[thingName] 
			         select t.GetInnerIfMinified() 
			         into thing let zone = bc.targetBill.includeGroup
			         where zone == null || zone.CellsList.Contains(thing.InteractionCell) 
			         where !thing.IsForbidden(Faction.OfPlayer) where !thing.def.useHitPoints || bc.targetBill.hpRange.Includes((float) thing.HitPoints / (float) thing.MaxHitPoints) 
			         select thing)
			{
				// Quality
				if (thing.TryGetQuality(out var q) && !bc.targetBill.qualityRange.Includes(q))
					continue;

				var canChooseTainted = producedThing.IsApparel && producedThing.apparel.careIfWornByCorpse;

 			    // Tainted
				var a = thing.GetType() == typeof(Apparel) ? (Apparel) thing : null;
				if (canChooseTainted && !bc.targetBill.includeTainted && a?.WornByCorpse == true)
				{
					continue;
				}

				// Equipped.
					//bool can_choose_equipped = producted_thing.IsWeapon || producted_thing.IsApparel;
				if (/* can_choose_equipped && */ !bc.targetBill.includeEquipped && thing.IsHeldByPawn()) 
				{
					continue;
				}
				

				foundThings.Add(thing);
			}

			return foundThings;
		}

		// Code taken from RecipeWorkerCounter.CountProducts
		/// <summary>
		/// Retrieves a list of things of a specific ThingDef that are currently in the possession of a Pawn.
		/// </summary>
		/// <param name="pawn">The Pawn whose possessions will be searched.</param>
		/// <param name="def">The ThingDef of the desired things.</param>
		/// <returns>A list of things that match the specified ThingDef and are currently in the possession of the Pawn.</returns>
		private static List<Thing> GetThingInPawn(Pawn pawn, ThingDef def) {
			var things = new List<Thing>();
			foreach (var equipment in pawn.equipment.AllEquipmentListForReading.Where(equipment => equipment.def.Equals(def)))
			{
				things.Add((Thing) equipment);
			}

			foreach (var apparel in pawn.apparel.WornApparel.Where(apparel => apparel.def.Equals(def)))
			{
				things.Add((Thing) apparel);
			}

			foreach (var heldThing in pawn.inventory.innerContainer.InnerListForReading.Where(heldThing => heldThing.def.Equals(def)))
			{
				things.Add(heldThing);
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
