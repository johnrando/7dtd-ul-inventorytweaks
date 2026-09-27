using UnityEngine;

namespace InventoryTweaks
{
	/// <summary>
	/// Runtime knobs, all switchable from the <c>it</c> console command. The values here are the
	/// built-in defaults; <see cref="Config"/> reads the player's file over them at startup.
	/// </summary>
	internal static class Settings
	{
		/// <summary>Master switch. When off every hook returns immediately.</summary>
		internal static bool Enabled = true;

		/// <summary>
		/// Where the backpack list jumps on every real open: "top", "bottom" or "off". Off also
		/// stops the jump to the top after a sort, which is always the top since that is where a
		/// sort puts its result. Set by <c>it scroll</c> or by right-clicking one of the
		/// scrollbar's end buttons (the active one draws bronze). Persisted.
		/// </summary>
		internal static string ScrollTo = "top";

		internal static bool ScrollOn => ScrollTo != "off";

		internal static bool ScrollBottom => ScrollTo == "bottom";

		/// <summary>...and the same after a sort in loot-container and vehicle-storage windows.</summary>
		internal static bool ScrollContainers = true;

		/// <summary>
		/// The locked sort order, one of <see cref="SortModes"/>' names, or empty for none. Set by
		/// right-clicking a sort button (on or off) or by <c>it lock</c>. Persisted.
		/// </summary>
		internal static string LockedSort = "";

		/// <summary>While a sort is locked, re-sort every time the inventory is opened.</summary>
		internal static bool AutoSort = true;

		/// <summary>
		/// The order the backpack is sorted into when a trader window opens, one of
		/// <see cref="SortModes"/>' names, or empty for none. Set by Ctrl+right-clicking a sort
		/// button (on or off) or by <c>it tradersort</c>. Independent of <see cref="AutoSort"/>
		/// and of <see cref="LockedSort"/>, which it overrides for that one open. Persisted.
		/// Never the same order as <see cref="LockedSort"/> - see <see cref="NormalizeSorts"/>.
		/// </summary>
		internal static string TraderSort = "";

		/// <summary>
		/// Keep never-seen items at the front of the backpack: after every sort and on every real
		/// open. Toggled by right-clicking the fifth button in UL's sort row (whose left-click sorts
		/// the bag by recency) or by <c>it newfirst</c>. Persisted.
		/// </summary>
		internal static bool NewFirst = false;

		/// <summary>
		/// With <see cref="NewFirst"/>: changed stacks (the +/- ones) form a second tier behind the
		/// new ones. Toggled by Ctrl+right-clicking the same button or <c>it changedfirst</c>. Persisted.
		/// Never on while <see cref="NewFirst"/> is off - see <see cref="NormalizeTiers"/>.
		/// </summary>
		internal static bool ChangedFirst = false;

		/// <summary>
		/// Holds the two tier switches to the three states their button can draw: off, new first,
		/// new then changed. The changed tier only exists behind the new one, so it is never on by
		/// itself. Left on its own it would draw as off while still counting, and the next click
		/// would land two states along instead of one - the button then needs clicking twice to
		/// settle where one click should have put it.
		/// </summary>
		internal static void NormalizeTiers()
		{
			if (!NewFirst)
			{
				ChangedFirst = false;
			}
		}

		/// <summary>
		/// Locks <paramref name="_mode"/>, dropping it as the trader order if it was one: a button
		/// draws either bronze or gold, never both.
		/// </summary>
		internal static void SetLocked(string _mode)
		{
			LockedSort = _mode;
			if (_mode.Length > 0 && TraderSort == _mode)
			{
				TraderSort = "";
			}
		}

		/// <summary>Makes <paramref name="_mode"/> the trader order, unlocking it if it was locked.</summary>
		internal static void SetTrader(string _mode)
		{
			TraderSort = _mode;
			if (_mode.Length > 0 && LockedSort == _mode)
			{
				LockedSort = "";
			}
		}

		/// <summary>
		/// Holds each sort button to the three states it can draw: off, locked, trader. One button
		/// cannot be both, so the same order is never in both settings at once. The right-click
		/// cycle and the two setters above keep this by construction; this is for a hand-edited
		/// settings file, where there is no "which one was just set" and the lock wins.
		/// </summary>
		internal static void NormalizeSorts()
		{
			if (TraderSort.Length > 0 && TraderSort == LockedSort)
			{
				TraderSort = "";
			}
		}

		/// <summary>Pressing the key of the page already showing (B for character, and so on) closes the inventory.</summary>
		internal static bool ToggleClose = true;

		/// <summary>Frame items that arrived or changed count since the player last looked at them.</summary>
		internal static bool Highlight = true;

		/// <summary>
		/// Frame colour for highlighted cells: a muted gold at about 80% opacity. The frame is UL's
		/// own hover sprite, whose border width is baked into the art, so tint and opacity are the
		/// only ways to make it quieter.
		/// </summary>
		internal static Color32 HighlightColor = new Color32(215, 170, 70, 205);

		/// <summary>Prefix the stack count of a highlighted stackable with +/- markers.</summary>
		internal static bool Markers = true;
	}
}
