using CrunchyDuck.Math.MathFilters;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using Verse;

namespace CrunchyDuck.Math.ModCompat {
	public class CompositableLoadoutsSupport {
		private static Type LoadoutManagerType = Type.GetType("Inventory.LoadoutManager, Inventory");
		private static Type LoadoutTagType = Type.GetType("Inventory.Tag, Inventory");
		private static Type LoadoutUtilityType = Type.GetType("Inventory.Utility, Inventory");
		private static AccessTools.FieldRef<GameComponent, IDictionary> LoadoutManagerPawnTagsField = AccessTools.FieldRefAccess<IDictionary>(LoadoutManagerType, "pawnTags");
		private static AccessTools.FieldRef<object, string> TagNameField = AccessTools.FieldRefAccess<string>(LoadoutTagType, "name");
		private static AccessTools.FieldRef<GameComponent, IReadOnlyList<object>> LoadoutManagerTagsField = AccessTools.FieldRefAccess<IReadOnlyList<object>>(LoadoutManagerType, "tags");

		public static Func<Pawn, bool> IsValidLoadoutHolder = AccessTools.MethodDelegate<Func<Pawn, bool>>(
			AccessTools.Method(LoadoutUtilityType, "IsValidLoadoutHolder"));
		public static IReadOnlyList<Pawn> GetPawnsWithTag(object tag) {
			return ((IReadOnlyList<Pawn>) LoadoutManagerPawnTagsField(GetLoadoutManager())[tag]);
		}

		public static IReadOnlyList<object> GetTags() {
			return LoadoutManagerTagsField(GetLoadoutManager());
		}

		public static string GetTagName(object tag) {
			return TagNameField(tag);
		}

		private static GameComponent GetLoadoutManager() =>
			Current.Game.GetComponent(LoadoutManagerType);

		public static bool GetCompositableLoadoutFilter(string command, BillComponent bc, ref MathFilter filter) {
			if (!Math.compositableLoadoutsSupportEnabled || !TryFindTagByName(command, out object tag))
				return false;
			filter = new CompositableLoadoutTagsFilter(bc, tag);
			return true;
		}
		
		public static bool TryFindTagByName(string name, out object tagResult) {
			foreach (object tag in GetTags()) {
				if (!TagMatchesParameterName(tag, name))
					continue;
				tagResult = tag;
				return true;
			}
			tagResult = null;
			return false;
		}

		private static bool TagMatchesParameterName(object tag, string parameterName) =>
			TagNameField(tag).ToParameter() == parameterName;
	}
}
