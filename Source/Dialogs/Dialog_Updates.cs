using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
using RimWorld;

namespace CrunchyDuck.Math {
	class Dialog_Updates : Window {
		private Vector2 scrollPosition = Vector2.zero;
		public override Vector2 InitialSize => new Vector2(700f, 700f);
		private List<UpdateLog> updates = new List<UpdateLog>() {
			{ new UpdateLog("1.4.3", "CD.M.updates.1.4.3".Translate()) },
			{ new UpdateLog("1.4.0", "CD.M.updates.1.4".Translate()) },
			{ new UpdateLog("1.3.0", "CD.M.updates.1.3".Translate()) }
		};
		private int i = 0;

		public Dialog_Updates() {
			forcePause = true;
			doCloseX = true;
			doCloseButton = true;
			absorbInputAroundWindow = true;
			closeOnClickedOutside = true;
		}

		public override void Close(bool doCloseSound = true) {
			base.Close(doCloseSound);
		}

		// TODO: Allow people to browse update history with > and <
		public override void DoWindowContents(Rect inRect) {
			UpdateLog u = updates[i];
			var ta = Text.Anchor;

			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.UpperCenter;
			Rect title_area = new Rect(inRect.x, inRect.y + 10, inRect.width, 34);
			Widgets.Label(title_area, "v" + u.version);

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			float scroll_area_display_height = inRect.height - CloseButSize.y - title_area.height - 10;// - 18f;
			Rect scroll_area_display = inRect.TopPartPixels(scroll_area_display_height);
			scroll_area_display.y += title_area.height + 10;
			Widgets.LabelScrollable(scroll_area_display, u.log, ref scrollPosition);
			
			Text.Anchor = ta;
		}
	}

	public struct UpdateLog {
		public string version;
		public string log;

		public UpdateLog(string version, string log) {
			this.version = version;
			this.log = log;
		}
	}
}
