using System.Collections.Generic;
using UnityEngine;

namespace InventoryTweaks
{
	/// <summary>
	/// <c>it</c> (or <c>inventorytweaks</c>). The bare command prints the settings block and
	/// changes nothing; every line names the command that changes it, so it doubles as the menu.
	/// <c>it info</c> adds the diagnostics and counters that answer "is this thing working".
	/// </summary>
	public class ConsoleCmdInventoryTweaks : ConsoleCmdAbstract
	{
		public override bool IsExecuteOnClient => false;

		public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
		{
			string command = _params.Count > 0 ? _params[0].ToLower() : string.Empty;

			switch (command)
			{
			case "":
				OutputMenu("InventoryTweaks is " + OnOff(Settings.Enabled));
				return;

			case "on":
			case "off":
				SetEnabled(command == "on");
				return;

			case "scroll":
				SetScroll(_params);
				return;

			case "containers":
				Settings.ScrollContainers = !Settings.ScrollContainers;
				Config.Save();
				Output("Scroll after a sort in loot and vehicle windows " + OnOff(Settings.ScrollContainers) + ".");
				return;

			case "lock":
				SetLock(_params);
				return;

			case "autosort":
				Settings.AutoSort = !Settings.AutoSort;
				Config.Save();
				Output("Sort on open while locked " + OnOff(Settings.AutoSort) + ".");
				return;

			case "tradersort":
				SetTraderSort(_params);
				return;

			case "newfirst":
			{
				bool hadChanged = Settings.ChangedFirst;
				ToggleNewFirst(_changed: false);
				Output("New items first " + OnOff(Settings.NewFirst)
					+ (hadChanged && !Settings.ChangedFirst ? " (the changed tier went with it)." : "."));
				return;
			}

			case "changedfirst":
			{
				bool hadNew = Settings.NewFirst;
				ToggleNewFirst(_changed: true);
				Output("Changed items behind new ones " + OnOff(Settings.ChangedFirst)
					+ (Settings.NewFirst && !hadNew ? " (new items first is on as well now)." : "."));
				return;
			}

			case "toggle":
				Settings.ToggleClose = !Settings.ToggleClose;
				Config.Save();
				Output("Page key closes the inventory " + OnOff(Settings.ToggleClose) + ".");
				return;

			case "highlight":
				Settings.Highlight = !Settings.Highlight;
				Config.Save();
				if (!Settings.Highlight)
				{
					NewItemTracker.Clear();
				}
				Output("New-item highlight " + OnOff(Settings.Highlight) + ".");
				return;

			case "color":
			case "colour":
				SetColor(_params);
				return;

			case "markers":
				Settings.Markers = !Settings.Markers;
				Config.Save();
				RefreshWindow();
				Output("Stack-count markers " + OnOff(Settings.Markers) + ".");
				return;

			case "clear":
				NewItemTracker.Clear();
				Output("Highlights cleared.");
				return;

			case "info":
				OutputInfo();
				return;

			case "reset":
				Counters.Reset();
				Output("Counters reset.");
				return;

			default:
				Output("Unknown option '" + _params[0]
					+ "'. Try: it [on|off|scroll {off|top|bottom}|containers|lock|autosort|tradersort|newfirst|changedfirst|toggle|highlight|color|markers|clear|info|reset]");
				return;
			}
		}

		private static void OutputMenu(string _header)
		{
			Output(_header);
			Switch("it on|off", EnabledChoices(), "master switch");
			Switch("it scroll {off|top|bottom}", ScrollChoices(), "opening scrolls the backpack there, sorting to the top - or right-click a scrollbar button");
			Switch("it containers", OnOffChoices(Settings.ScrollContainers), "...loot / vehicle windows too, after a sort");
			Switch("it lock {mode}", ModeChoices(Settings.LockedSort), "locked sort - or right-click a sort button");
			Switch("it autosort", OnOffChoices(Settings.AutoSort), "re-sort on open while locked");
			Switch("it tradersort {mode}", ModeChoices(Settings.TraderSort), "sort when a trader opens - or ctrl+right-click a sort button");
			Switch("it newfirst", OnOffChoices(Settings.NewFirst), "new items first - or right-click the ! button");
			Switch("it changedfirst", OnOffChoices(Settings.ChangedFirst), "...then changed items - or ctrl+right-click it");
			Switch("it toggle", OnOffChoices(Settings.ToggleClose), "a page's key closes the inventory again");
			Switch("it highlight", OnOffChoices(Settings.Highlight), "frame new or changed items until hovered");
			Line("it color {r,g,b,a}", Config.Color(Settings.HighlightColor));
			Switch("it markers", OnOffChoices(Settings.Markers), "+/- before a changed stack count");
		}

		private static void SetEnabled(bool _on)
		{
			bool changed = Settings.Enabled != _on;
			Settings.Enabled = _on;
			if (changed)
			{
				Config.Save();
				RefreshWindow();
			}
			OutputMenu("InventoryTweaks is " + (changed ? "now " : "already ") + OnOff(_on));
		}

		/// <summary><c>it scroll {off|top|bottom}</c>. The old on/off still parses, on meaning top.</summary>
		private static void SetScroll(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: it scroll {off|top|bottom} - currently: " + Settings.ScrollTo);
				return;
			}
			if (!Config.TryScrollTo(_params[1]))
			{
				Output("'" + _params[1] + "' is not a scroll end - off, top or bottom.");
				return;
			}
			Config.Save();
			RefreshWindow();
			Output("Scroll on open: " + Settings.ScrollTo
				+ (Settings.ScrollOn ? " - the backpack will jump to the " + Settings.ScrollTo
					+ " on every real open, and to the top after every sort." : " - sorts no longer scroll either."));
		}

		private static void SetLock(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: it lock {none|weight|price|group|name} - currently: " + ModeLine(Settings.LockedSort));
				return;
			}
			string was = Settings.TraderSort;
			if (!Config.TryLock(_params[1]))
			{
				Output("'" + _params[1] + "' is not a sort order - none, weight, price, group or name.");
				return;
			}
			Config.Save();
			RefreshWindow();
			Output("Locked sort: " + ModeLine(Settings.LockedSort)
				+ (Settings.LockedSort.Length > 0 ? " - the backpack will sort on every open." : ".")
				+ (was.Length > 0 && Settings.TraderSort.Length == 0
					? " The trader sort was the same order, so it is now off." : ""));
		}

		/// <summary>
		/// <c>it tradersort {mode}</c>. The same shape as the lock, and the same exclusivity: a
		/// button is either the locked order or the trader one.
		/// </summary>
		private static void SetTraderSort(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: it tradersort {none|weight|price|group|name} - currently: "
					+ ModeLine(Settings.TraderSort));
				return;
			}
			string was = Settings.LockedSort;
			if (!Config.TryTraderSort(_params[1]))
			{
				Output("'" + _params[1] + "' is not a sort order - none, weight, price, group or name.");
				return;
			}
			Config.Save();
			RefreshWindow();
			Output("Sort at a trader: " + ModeLine(Settings.TraderSort)
				+ (Settings.TraderSort.Length > 0
					? " - the backpack will sort that way when a trader window opens." : ".")
				+ (was.Length > 0 && Settings.LockedSort.Length == 0
					? " That order was locked, so the lock is now off." : ""));
		}

		private static void SetColor(List<string> _params)
		{
			if (_params.Count != 2)
			{
				Output("Usage: it color {r,g,b[,a]} - currently: " + Config.Color(Settings.HighlightColor));
				return;
			}
			if (!Config.TryColor(_params[1], out Color32 color))
			{
				Output("Colours are r,g,b or r,g,b,a with each channel 0-255, like 215,170,70,205.");
				return;
			}
			Settings.HighlightColor = color;
			Config.Save();
			RefreshWindow();
			Output("Highlight colour: " + Config.Color(Settings.HighlightColor));
		}

		/// <summary>
		/// Flips new-items-first. With UL present this goes through the button's own toggle so an
		/// open window is refreshed and re-ordered at once; without it only the setting changes.
		/// </summary>
		private static void ToggleNewFirst(bool _changed)
		{
			if (UndeadLegacyInfo.Present)
			{
				try
				{
					ToggleNewFirstUl(_changed);
					return;
				}
				catch (System.Exception e)
				{
					Log.Warning(Patches.LogPrefix + "Could not refresh the inventory window: " + e.Message);
				}
			}
			if (_changed)
			{
				Settings.ChangedFirst = !Settings.ChangedFirst;
				if (Settings.ChangedFirst)
				{
					Settings.NewFirst = true;
				}
			}
			else
			{
				Settings.NewFirst = !Settings.NewFirst;
			}
			Settings.NormalizeTiers();
			Config.Save();
		}

		/// <summary>Kept separate so this method is only JIT-compiled with UL present.</summary>
		private static void ToggleNewFirstUl(bool _changed)
		{
			XUiC_ULM_BackpackWindow window = BackpackAccess.Window(LocalPlayerUI.primaryUI?.xui);
			if (_changed)
			{
				NewFirst.ToggleChanged(window);
			}
			else
			{
				NewFirst.Toggle(window);
			}
		}

		/// <summary>Re-applies the lock indicator on an open window after a console change.</summary>
		private static void RefreshWindow()
		{
			if (!UndeadLegacyInfo.Present)
			{
				return;
			}
			try
			{
				RefreshWindowUl();
			}
			catch (System.Exception e)
			{
				Log.Warning(Patches.LogPrefix + "Could not refresh the inventory window: " + e.Message);
			}
		}

		/// <summary>Kept separate so this method is only JIT-compiled with UL present.</summary>
		private static void RefreshWindowUl()
		{
			XUiC_ULM_BackpackWindow window = BackpackAccess.Window(LocalPlayerUI.primaryUI?.xui);
			if (window == null)
			{
				return;
			}
			SortLock.ApplyIndicator(window);
			BackpackAccess.DirtyCells(window);
		}

		private static string OnOff(bool _on)
		{
			return _on ? "ON" : "OFF";
		}

		private static void OutputInfo()
		{
			OutputMenu("InventoryTweaks is " + OnOff(Settings.Enabled));
			Line("settings file", Config.Status);
			Line("Undead Legacy", UndeadLegacyInfo.Status);
			Line("sorter field", BackpackAccess.SorterStatus);
			Line("sort scroll", UlPatches.SortScrollStatus);
			Line("std sort scroll", UlPatches.StandardSortScrollStatus);
			Line("sort lock", UlPatches.SortLockStatus);
			Line("window open", UlPatches.WindowOpenStatus);
			Line("window close", UlPatches.WindowCloseStatus);
			Line("window update", UlPatches.WindowUpdateStatus);
			Line("bag changes", UlPatches.BagChangeStatus);
			Line("highlight draw", UlPatches.HighlightStatus);
			Line("count markers", UlPatches.MarkerStatus);
			Line("key toggle", UlPatches.ToggleCloseStatus);
			Line("new-first button", UlPatches.NewFirstButtonStatus + "; " + NewFirst.ButtonStatus);
			Line("new-first sort", UlPatches.NewFirstSortStatus);
			Line("new-first std sort", UlPatches.NewFirstStandardSortStatus);
			Line("trader close", UlPatches.TraderCloseStatus);
			Line("scroll buttons", UlPatches.ScrollButtonsStatus + "; " + ScrollButtons.ButtonStatus);
			Line("scroll track", UlPatches.ScrollTrackStatus);
			Line("sorts scrolled", Counters.SortsScrolled + " (" + Counters.AutoSorts + " automatic)");
			Line("scrolled on open", Counters.OpenScrolls.ToString());
			Line("button scrolls", Counters.ButtonScrolls.ToString());
			Line("trader sorts", Counters.TraderSorts.ToString());
			Line("new-first sorts", Counters.NewFirstSorts.ToString());
			Line("recency sorts", Counters.RecencySorts.ToString());
			Line("closed by page key", Counters.KeyCloses.ToString());
			Line("bag changes seen", Counters.BagChanges.ToString());
			Line("items flagged", Counters.ItemsFlagged + " flagged, " + Counters.ItemsSeen + " cleared by hover");
			Line("highlighted now", NewItemTracker.HighlightedCount().ToString());

			if (Counters.BagChanges == 0)
			{
				Output("Note: no backpack change has reached the tracker yet. Picking anything up should");
				Output("move 'bag changes seen' - if it stays at zero, the hook is not live.");
			}
		}

		private static void Line(string _label, string _value)
		{
			Output("  " + _label.PadRight(26) + ": " + _value);
		}

		private static void Switch(string _label, string _choices, string _note)
		{
			Line(_label, _choices.PadRight(44) + " - " + _note);
		}

		private static string Choices(params string[] _options)
		{
			return "[ " + string.Join(" | ", _options) + " ]";
		}

		private static string Mark(string _option, bool _live)
		{
			return _live ? ">" + _option + "<" : _option;
		}

		private static string EnabledChoices()
		{
			return Choices(Mark("on", Settings.Enabled), Mark("off", !Settings.Enabled));
		}

		private static string OnOffChoices(bool _on)
		{
			return Choices(Mark("on", _on), Mark("off", !_on));
		}

		private static string ScrollChoices()
		{
			return Choices(Mark("off", !Settings.ScrollOn), Mark("top", Settings.ScrollTo == "top"),
				Mark("bottom", Settings.ScrollBottom));
		}

		/// <summary>The none/weight/price/group/name menu, marking whichever <paramref name="_mode"/> is.</summary>
		private static string ModeChoices(string _mode)
		{
			string[] options = new string[SortModes.Names.Length + 1];
			options[0] = Mark("none", _mode.Length == 0);
			for (int i = 0; i < SortModes.Names.Length; i++)
			{
				options[i + 1] = Mark(SortModes.Names[i], _mode == SortModes.Names[i]);
			}
			return Choices(options);
		}

		private static string ModeLine(string _mode)
		{
			return _mode.Length == 0 ? "none" : _mode;
		}

		private static void Output(string _line)
		{
			SdtdConsole.Instance.Output(_line);
		}

		public override string[] getCommands()
		{
			return new string[2] { "it", "inventorytweaks" };
		}

		public override string getDescription()
		{
			return "Reports InventoryTweaks' settings; 'it on' and 'it off' switch it.";
		}

		public override string getHelp()
		{
			return "Usage: it [on|off|scroll {off|top|bottom}|containers|lock {mode}|autosort|tradersort {mode}|newfirst|changedfirst|toggle|highlight|color {r,g,b,a}"
				+ "|markers|clear|info|reset]"
				+ "\r\n\r\nInventory quality-of-life for Undead Legacy's backpack. 'it' on its own prints "
				+ "the settings and changes nothing. Each line names the command that changes it, so "
				+ "the settings block is also the menu."
				+ "\r\n\r\n'it on' and 'it off' are the master switch; off, every hook returns immediately."
				+ "\r\n\r\n'it scroll {off|top|bottom}' picks the end the backpack jumps to on every real "
				+ "open (not on a tab switch). While it is on, any sort also jumps to the top, where the "
				+ "sorted result is; off leaves the list where it is in both cases. In game, the backpack "
				+ "scrollbar has a button at each end: left-click jumps there, right-click makes it the end "
				+ "opening jumps to (the button lights up bronze), and right-clicking the bronze one turns "
				+ "that off. Every sort and scroll "
				+ "button's tooltip ends in [alt+]: hold Alt while hovering for what its right-clicks do. 'it containers' "
				+ "extends the after-sort jump to the loot container and vehicle storage windows, which "
				+ "share UL's sorter."
				+ "\r\n\r\n'it lock {mode}' locks a sort order: none, weight, price, group or name. In game, "
				+ "right-click a sort button to lock it (the button lights up bronze), right-click it "
				+ "again to unlock. While locked, 'it autosort' (on by default) re-sorts the backpack every "
				+ "time the inventory is opened - never while it is open, never while you are holding "
				+ "an item, and not on a plain tab switch between crafting, character and the like."
				+ "\r\n\r\n'it tradersort {mode}' picks the order the backpack is sorted into when a trader "
				+ "window opens - by price, to put what is worth selling at the front. In game, "
				+ "ctrl+right-click a sort button: it lights up gold, and ctrl+right-click again turns it "
				+ "off. One button can be bronze and one gold, never the same one, so making the locked "
				+ "order the trader order drops the lock and the other way round; setting one on another "
				+ "button leaves the other alone. It sorts once per trader, whatever 'it autosort' says and whatever is locked - a "
				+ "locked order still runs everywhere else, it just does not run at the trader. Vending "
				+ "machines are left alone, as is a tab switch while you are already trading."
				+ "\r\n\r\n'it newfirst' keeps items you have never had before (the highlighted ones with no "
				+ "+/- marker) at the front of the backpack: after every sort, on every real open, and "
				+ "the moment it is switched on. In game, right-click the ! button next to the sort buttons "
				+ "(a left-click sorts the whole bag by recency: the stack that last had something arrive "
				+ "comes first). "
				+ "'it changedfirst' (ctrl+right-click the same button) adds a second tier behind them for "
				+ "stacks you had whose count changed; switching new-first off drops that tier with it, "
				+ "so the button is only ever off, new first, or new then changed. "
				+ "Each tier keeps the order of the sort it follows; "
				+ "when no sort ran (an open with no locked order, or switching on) it is by most recent "
				+ "change. The rest of the bag keeps its order and locked slots are left alone."
				+ "\r\n\r\n'it toggle' makes the key that opened a page close the inventory when pressed "
				+ "again (B for the character page, N for skills, and so on), as the unmodded game does. "
				+ "Undead Legacy drops that, leaving only Tab and Escape."
				+ "\r\n\r\n'it highlight' toggles the new-item frame. An item that arrives in the backpack, "
				+ "or a stack whose count changes, keeps a coloured frame until you hover its cell or "
				+ "close the inventory (a tab switch does not count). 'it markers' toggles the +/- "
				+ "before a changed stack count: one for a change of a single item, three once the "
				+ "stack has reached three times what you last saw (or a third of it), two in between. "
				+ "'it color {r,g,b,a}' sets the frame colour. 'it clear' drops every highlight. "
				+ "Highlights live in memory only and reset when the game restarts."
				+ "\r\n\r\nEvery setting is written straight to a settings file in the game's user data "
				+ "folder next to Saves, so it survives a restart and a mod update. 'it info' prints "
				+ "its path, the state of each patch and the counters; 'it reset' zeroes the counters. "
				+ "'inventorytweaks' is an alias for 'it'.";
		}
	}
}
