using Verse;
using System.Collections.Generic;

namespace CrunchyDuck.Math {
	public class Settings : ModSettings {
        public float textInputAreaBonus = 200f;
		public bool extlogging = false;

		public string lastVersionInfocardChecked = "";
		public Dictionary<string, UserVariable> userVariablesDict = new Dictionary<string, UserVariable>();
		public List<UserVariable> userVariables = new List<UserVariable>();

        public override void ExposeData() {
            base.ExposeData();
            Scribe_Values.Look(ref textInputAreaBonus, "CDtextInputAreaBonus", 200f);
            Scribe_Values.Look(ref extlogging, "CDMathExtLogging", false);

			Scribe_Values.Look(ref lastVersionInfocardChecked, "CDlastVersionInfocardChecked", "");
			Scribe_Collections.Look(ref userVariables, "userVariables", LookMode.Deep);
			// No idea why Collections doesn't seem to have an option for a default, or at least return an empty list.
			if (userVariables == null)
				userVariables = new List<UserVariable>();
			UpdateUserVariables();
			//userVariables = new List<UserVariable>();
			//for (int i = 0; i < 1; i++) {
			//	userVariables.Add(new UserVariable());
			//}
		}

		public void UpdateUserVariables() {
			userVariablesDict = new Dictionary<string, UserVariable>();
			foreach (UserVariable uv in userVariables) {
				if (!userVariablesDict.ContainsKey(uv.name))
					userVariablesDict[uv.name] = uv;
			}
		}

        // Pete's slider code.
        // https://github.com/PeteTimesSix/ResearchReinvented/blob/main/ResearchReinvented/Source/Utilities/ListingExtensions.cs
        //public static void SliderLabeled(this Listing_Standard instance, string label, ref float value, float min, float max, float roundTo = -1, float displayMult = 1, int decimalPlaces = 0, string valueSuffix = "", string tooltip = null, Action onChange = null) {
        //    if (!string.IsNullOrEmpty(label))
        //        instance.Label($"{label}: {(value * displayMult).ToString($"F{decimalPlaces}")}{valueSuffix}", tooltip: tooltip);
        //    var valueBefore = value;
        //    value = instance.FullSlider(value, min, max, roundTo: roundTo);
        //    if (value != valueBefore) {
        //        onChange?.Invoke();
        //    }
        //}
        //public static float FullSlider(this Listing_Standard instance, float val, float min, float max, float roundTo = -1f, bool middleAlignment = false, string label = null, string leftAlignedLabel = null, string rightAlignedLabel = null) {
        //    float newVal = Widgets.HorizontalSlider(instance.GetRect(22f), val, min, max, middleAlignment, label, leftAlignedLabel, rightAlignedLabel, roundTo);
        //    if (newVal != val) {
        //        SoundDefOf.DragSlider.PlayOneShotOnCamera(null);
        //    }
        //    instance.Gap(instance.verticalSpacing);
        //    return newVal;
        //}
    }

	// I couldn't find a page on how Mod works, so I'm definitely using it poorly here. Suspect I could replace Math with this.
	public class MathSettings : Mod {
		public static Settings settings;

		public MathSettings(ModContentPack content) : base(content) {
			settings = GetSettings<Settings>();
		}

		/// <summary>
		/// The (optional) GUI part to set your settings.
		/// </summary>
		/// <param name="inRect">A Unity Rect with the size of the settings window.</param>
		public override void DoSettingsWindowContents(UnityEngine.Rect inRect) {
			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(inRect);
			try
			{
				listingStandard.Label("Bill input area expansion: " + settings.textInputAreaBonus.ToString(), tooltip: "How much the text field for bill input is expanded. A larger field makes it easier to have larger equations.");
				settings.textInputAreaBonus = listingStandard.Slider(settings.textInputAreaBonus, 0f, 600f);
				listingStandard.CheckboxLabeled("Extended Logging: ", ref settings.extlogging, tooltip: "Unless you were asked to enable this, you probably don't want it.");
			}
			finally
			{
				listingStandard.End();
			}
			base.DoSettingsWindowContents(inRect);
		}

		/// <summary>
		/// Override SettingsCategory to show up in the list of settings.
		/// Using .Translate() is optional, but does allow for localisation.
		/// </summary>
		/// <returns>The (translated) mod name.</returns>
		public override string SettingsCategory() {
			return "Math!";
		}
	}
}
