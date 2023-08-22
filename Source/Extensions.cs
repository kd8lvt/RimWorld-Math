using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;

namespace CrunchyDuck.Math {
	public static class Extensions {
		public static string ToParameter(this string str) {
			str = str.Replace("\"", "_");
			str = str.Replace(".", "_");
			str = str.ToLower();
			return str;
		}

		public static bool IsParameter(this string str) {
			foreach(char c in str) {
				if (c == '"' || c == '.' || char.IsUpper(c))
					return false;
			}
			return true;
		}

		public static bool HasMethod(this object objectToCheck, string methodName) {
			var type = objectToCheck.GetType();
			return type.GetMethod(methodName) != null;
		}

		public static bool IsHeldByPawn(this Thing thing) {
			var owner = thing.holdingOwner.Owner;
			if (owner is Pawn_InventoryTracker) {
				return true;
			}
			if (owner is Pawn_ApparelTracker) {
				return true;
			}
			return false;
		}

		public static void Move<T>(this IList<T> list, int from, int to) {
			T item = list[from];
			list.RemoveAt(from);
			list.Insert(to, item);
		}
	}

	public static class RectExtensions {
		/// <summary>
		/// Remove a chunk from the rect and return it.
		/// </summary>
		public static Rect ChopRectLeft(ref this Rect rect, float percent) {
			Rect chunk = rect.LeftPart(percent);
			rect.xMin += chunk.width;
			return chunk;
		}

		/// <summary>
		/// Remove a chunk from the rect and return it.
		/// </summary>
		public static Rect ChopRectLeft(ref this Rect rect, int pixels) {
			Rect chunk = rect.LeftPartPixels(pixels);
			rect.xMin += chunk.width;
			return chunk;
		}

		/// <summary>
		/// Remove a chunk from the rect and return it.
		/// </summary>
		public static Rect ChopRectRight(ref this Rect rect, float percent) {
			Rect chunk = rect.RightPart(percent);
			rect.xMax -= chunk.width;
			return chunk;
		}

		/// <summary>
		/// Remove a chunk from the rect and return it.
		/// </summary>
		public static Rect ChopRectRight(ref this Rect rect, int pixels, int right_margin = GUIExtensions.ElementPadding) {
			Rect chunk = rect.RightPartPixels(pixels + right_margin);
			rect.xMax -= chunk.width;
			chunk.xMax -= right_margin;
			return chunk;
		}
	}
}
