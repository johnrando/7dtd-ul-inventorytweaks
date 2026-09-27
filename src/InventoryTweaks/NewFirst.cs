using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace InventoryTweaks
{
	/// <summary>
	/// Feature 5. A fifth button in UL's sort row, with the same click shape as the four sort
	/// buttons: left-click sorts, right-click toggles the standing behaviour, Ctrl+right-click the
	/// second one.
	///
	/// Left-click sorts the bag by <em>recency</em>: the stack that last had stock arrive from
	/// outside comes first, everything the tracker has no history for goes last, locked slots stay
	/// put. The timestamp is the one the highlight tracker already keeps per slot (it follows an item
	/// when it moves and outlives the highlight), so the sort costs no storage of its own.
	///
	/// Right-click toggles <em>new first</em>: while on, never-seen items (feature 3's highlighted
	/// stacks with no +/- marker) are kept at the front of the backpack, after every UL sort, on every
	/// real open, and the moment it is switched on. Ctrl+right-click toggles the <em>changed</em>
	/// tier: stacks the player had whose count changed (the +/- ones) form a second tier behind the
	/// new ones (switching new-first on if it was off). Each tier keeps the order of the sort it
	/// follows; when no sort ran (an open with no locked order, switching on) it is ordered by most
	/// recent change. The rest of the bag keeps its order and locked slots are never touched, the
	/// same way UL's own sorter leaves them.
	///
	/// The button is added to the window XML in memory just before XUi parses it. A Config XML
	/// patch cannot do this: patches apply in mod folder order and "InventoryTweaks" sorts before
	/// "UndeadLegacy", so UL's window would not exist yet when ours ran.
	/// </summary>
	internal static class NewFirst
	{
		internal const string ButtonId = "btnSortNewFirst";

		private const string IconSprite = "symbol_stack_exclamation";

		private const string ToolTip = "Sort by recency";

		private const string Detail = "Sort by recency\nRight-click: new items first\nCtrl+right-click: changed items next";

		/// <summary>What the XML injection did, as reported by <c>it info</c>.</summary>
		internal static string ButtonStatus = Patches.NotRunYet;

		/// <summary>Prefix on <c>XUiFromXml.loadWindows(XmlFile)</c>: adds the button after UL's Name button.</summary>
		internal static void BeforeLoadWindows(XmlFile _xmlFile)
		{
			try
			{
				XElement nameButton = _xmlFile?.XmlDoc?.Root?
					.Descendants("window")
					.Where(w => (string)w.Attribute("name") == BackpackAccess.WindowName)
					.Descendants("ulmSort")
					.FirstOrDefault(b => (string)b.Attribute("id") == "btnSortByName");
				if (nameButton == null)
				{
					ButtonStatus = "NOT ADDED - windowBackpack has no btnSortByName";
					Log.Warning(Patches.LogPrefix + "Could not find UL's sort buttons in windows.xml, so the "
						+ "new-items-first button will be missing; 'it newfirst' still works.");
					return;
				}
				if (nameButton.Parent.Elements("ulmSort").Any(b => (string)b.Attribute("id") == ButtonId))
				{
					ButtonStatus = "added - already present";
					return;
				}
				nameButton.AddAfterSelf(new XElement("ulmSort",
					new XAttribute("id", ButtonId),
					new XAttribute("sprite", IconSprite),
					new XAttribute("tooltip_key", ToolTip)));
				ButtonStatus = "added - after btnSortByName";
				Log.Out(Patches.LogPrefix + "Added the new-items-first button to windowBackpack.");
			}
			catch (Exception e)
			{
				ButtonStatus = "NOT ADDED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not add the new-items-first button: " + e.Message);
			}
		}

		/// <summary>From <see cref="SortLock.AfterInit"/>: wires the button once the window exists.</summary>
		internal static void AfterInit(XUiC_ULM_BackpackWindow _window)
		{
			XUiController button = _window.GetChildById(ButtonId);
			if (button == null)
			{
				return;
			}
			button.OnPress += OnPress;
			button.OnRightPress += OnRightPress;
			ToolTips.Register(button, ToolTip, Detail);
		}

		private static bool Usable(XUiController _sender)
		{
			if (!Settings.Enabled || _sender?.ViewComponent == null)
			{
				return false;
			}
			// UL greys the sort row out under the Shuffled Backpack debuff.
			return !(_sender.ViewComponent is XUiV_Button view) || view.Enabled;
		}

		private static void OnPress(XUiController _sender, int _mouseButton)
		{
			if (Usable(_sender))
			{
				SortByRecency(BackpackAccess.Window(_sender.xui));
			}
		}

		private static void OnRightPress(XUiController _sender, int _mouseButton)
		{
			if (!Usable(_sender))
			{
				return;
			}
			XUiC_ULM_BackpackWindow window = BackpackAccess.Window(_sender.xui);
			if (InputUtils.ControlKeyPressed)
			{
				ToggleChanged(window);
			}
			else
			{
				Toggle(window);
			}
		}

		/// <summary>
		/// Left-click: the whole bag by most recent arrival, then the tiers on top if new-first is
		/// on (within a tier that is still recency order). Same shape as a UL sort, so it scrolls
		/// to the chosen end.
		/// </summary>
		internal static void SortByRecency(XUiC_ULM_BackpackWindow _window)
		{
			if (!Settings.Enabled || _window == null || !Open(_window, out Frame frame))
			{
				return;
			}
			List<int> order = new List<int>();
			for (int i = 0; i < frame.Movable; i++)
			{
				if (!frame.FixedSlot[i] && frame.Stacks[i] != null && !frame.Stacks[i].IsEmpty())
				{
					order.Add(i);
				}
			}
			Place(frame, order.OrderByDescending(NewItemTracker.Recency).ToList());
			Counters.RecencySorts++;
			if (Settings.NewFirst)
			{
				Apply(_window, _afterSort: true);
			}
			AutoScroll(_window);
		}

		/// <summary>
		/// Right-click: flips new-first, saves, refreshes the button and applies the order if now on.
		/// Switching it off takes the changed tier with it, so the button is left in one of the three
		/// states it can draw rather than in a fourth that looks off but is not.
		/// </summary>
		internal static void Toggle(XUiC_ULM_BackpackWindow _window)
		{
			Settings.NewFirst = !Settings.NewFirst;
			Settings.NormalizeTiers();
			AfterToggle(_window);
		}

		/// <summary>
		/// Ctrl+right-click: flips the changed tier. Switching it on while new-first is off switches
		/// new-first on too, since the tier only exists behind the new items.
		/// </summary>
		internal static void ToggleChanged(XUiC_ULM_BackpackWindow _window)
		{
			Settings.ChangedFirst = !Settings.ChangedFirst;
			if (Settings.ChangedFirst)
			{
				Settings.NewFirst = true;
			}
			AfterToggle(_window);
		}

		private static void AfterToggle(XUiC_ULM_BackpackWindow _window)
		{
			Config.Save();
			ApplyIndicator(_window);
			if (Settings.NewFirst && _window != null && _window.IsOpen && Apply(_window, _afterSort: false))
			{
				AutoScroll(_window);
			}
		}

		/// <summary>
		/// Selected sprite while on. The icon is the lock's bronze for new-first alone and the
		/// trader gold once the changed tier is on as well.
		/// </summary>
		internal static void ApplyIndicator(XUiC_ULM_BackpackWindow _window)
		{
			XUiController button = _window?.GetChildById(ButtonId);
			if (button == null)
			{
				return;
			}
			bool on = Settings.Enabled && Settings.NewFirst;
			if (button.ViewComponent is XUiV_Button view)
			{
				view.Selected = on;
			}
			Color32 color = on && Settings.ChangedFirst ? SortLock.TraderIconColor : SortLock.LockedIconColor;
			SortLock.TintIcon(button, on, color);
		}

		/// <summary>Postfix on <c>ULM_StackSorter.OnBtnSort</c>: after UL sorted the backpack.</summary>
		internal static void AfterSort(ULM_StackSorter __instance)
		{
			if (__instance == null || __instance.id != "Player")
			{
				return;
			}
			Apply(BackpackAccess.Window(__instance.xui), _afterSort: true);
		}

		/// <summary>Postfix on <c>XUiC_ULM_BackpackWindow.BtnSort_OnPress</c>: the standard-controls sort.</summary>
		internal static void AfterStandardSort(XUiC_ULM_BackpackWindow __instance)
		{
			Apply(__instance, _afterSort: true);
		}

		/// <summary>
		/// A real open. If the locked sort ran, its postfix already applied the tiers in sorted
		/// order and there is nothing to do; otherwise lift only what arrived while the inventory
		/// was closed - the latest batch - by most recent change. Anything still highlighted from
		/// an earlier visit keeps its place. Same guards as the locked sort so nothing moves under
		/// the cursor.
		/// </summary>
		internal static void OnWindowOpened(XUiC_ULM_BackpackWindow _window, bool _realOpen, bool _sortedAlready)
		{
			if (!_realOpen || _sortedAlready || !Settings.Enabled || !Settings.NewFirst)
			{
				return;
			}
			XUi xui = _window.xui;
			if (xui?.dragAndDrop != null && !xui.dragAndDrop.CurrentStack.IsEmpty())
			{
				return;
			}
			if (Apply(_window, _afterSort: false, WindowLifecycle.LastCloseTime))
			{
				AutoScroll(_window);
			}
		}

		/// <summary>
		/// Moves the new stacks, then (when the changed tier is on) the changed stacks, to the front
		/// of the movable slots, everything else following in its current order. Within a tier the
		/// current order is kept after a sort; otherwise the most recently changed comes first.
		/// With <paramref name="_since"/> only stacks that changed after that time form a tier; the
		/// rest stay where they are. Locked and attribute-locked slots keep their stack. Returns
		/// whether anything moved.
		/// </summary>
		internal static bool Apply(XUiC_ULM_BackpackWindow _window, bool _afterSort, float _since = 0f)
		{
			if (!Settings.Enabled || !Settings.NewFirst || _window == null || !Open(_window, out Frame frame))
			{
				return false;
			}

			// The movable stacks by tier: 0 new, 1 changed (or rest), 2 rest.
			List<int>[] tiers = { new List<int>(), new List<int>(), new List<int>() };
			for (int i = 0; i < frame.Movable; i++)
			{
				if (frame.FixedSlot[i] || frame.Stacks[i] == null || frame.Stacks[i].IsEmpty())
				{
					continue;
				}
				bool recent = NewItemTracker.LastChange(i) > _since;
				int t = recent && NewItemTracker.IsNew(i) ? 0
					: recent && Settings.ChangedFirst && NewItemTracker.IsChanged(i) ? 1 : 2;
				tiers[t].Add(i);
			}
			if (!_afterSort)
			{
				// No sort order to follow: most recently changed first. Stable, so ties keep their place.
				for (int t = 0; t < 2; t++)
				{
					tiers[t] = tiers[t].OrderByDescending(NewItemTracker.LastChange).ToList();
				}
			}
			List<int> order = new List<int>(tiers[0].Count + tiers[1].Count + tiers[2].Count);
			order.AddRange(tiers[0]);
			order.AddRange(tiers[1]);
			order.AddRange(tiers[2]);
			if (!Place(frame, order))
			{
				return false;
			}
			Counters.NewFirstSorts++;
			return true;
		}

		/// <summary>What a rearrangement works on: the bag, the grid, and which slots may not move.</summary>
		private struct Frame
		{
			internal XUi Xui;

			internal XUiC_ULM_BackpackGrid Grid;

			internal ItemStack[] Stacks;

			/// <summary>Slots from here on have no cell and are copied through untouched.</summary>
			internal int Movable;

			internal bool[] FixedSlot;
		}

		/// <summary>
		/// Gathers the bag for a rearrangement. False when there is nothing to work on or the sort
		/// row is disabled by the shuffled-backpack debuff.
		/// </summary>
		private static bool Open(XUiC_ULM_BackpackWindow _window, out Frame _frame)
		{
			_frame = default;
			XUiC_ULM_BackpackGrid grid = BackpackAccess.Grid(_window);
			if (grid == null || grid.isShuffledBackpack)
			{
				return false;
			}
			XUiC_ItemStack[] cells = BackpackAccess.Cells(_window);
			XUi xui = _window.xui;
			ItemStack[] stacks = xui?.PlayerInventory?.GetBackpackItemStacks();
			if (cells == null || stacks == null)
			{
				return false;
			}
			int n = Math.Min(stacks.Length, cells.Length);
			bool[] fixedSlot = new bool[n];
			for (int i = 0; i < n; i++)
			{
				fixedSlot[i] = cells[i] != null && (cells[i].UserLockedSlot || cells[i].AttributeLock);
			}
			_frame = new Frame { Xui = xui, Grid = grid, Stacks = stacks, Movable = n, FixedSlot = fixedSlot };
			return true;
		}

		/// <summary>
		/// Writes the movable stacks back in <paramref name="_order"/> (indices into the frame's
		/// stacks), fixed slots keeping theirs and any movable slot left over emptied. Returns
		/// whether anything moved.
		/// </summary>
		private static bool Place(Frame _frame, List<int> _order)
		{
			ItemStack[] stacks = _frame.Stacks;
			ItemStack[] result = new ItemStack[stacks.Length];
			bool changed = false;
			int next = 0;
			for (int i = 0; i < stacks.Length; i++)
			{
				if (i >= _frame.Movable || _frame.FixedSlot[i])
				{
					result[i] = stacks[i];
					continue;
				}
				if (next < _order.Count)
				{
					result[i] = stacks[_order[next++]];
					if (!ReferenceEquals(result[i], stacks[i]))
					{
						changed = true;
					}
				}
				else
				{
					result[i] = ItemStack.Empty.Clone();
					if (stacks[i] != null && !stacks[i].IsEmpty())
					{
						changed = true;
					}
				}
			}
			if (!changed)
			{
				return false;
			}

			// The same housekeeping UL's sorter does around its own SetBackpackItemStacks.
			ItemStack assembling = _frame.Xui.AssembleItem?.CurrentItemStackController?.ItemStack;
			_frame.Xui.PlayerInventory.SetBackpackItemStacks(result);
			if (assembling != null)
			{
				_frame.Grid.AssembleLockSingleStack(assembling);
			}
			return true;
		}

		private static void AutoScroll(XUiC_ULM_BackpackWindow _window)
		{
			if (BackpackAccess.AutoScroll(_window.scrollBar))
			{
				Counters.SortsScrolled++;
			}
		}
	}
}
