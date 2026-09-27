namespace InventoryTweaks
{
	/// <summary>
	/// Feature 6. One of UL's four sort orders can be set as the <em>trader</em> order (feature 2's
	/// gold state, Ctrl+right-click on the button). The backpack is sorted that way once each time a
	/// trader is opened, which is when a by-value order earns its keep.
	///
	/// No patch of its own: the trader window group carries vanilla's
	/// <c>open_backpack_on_open="true"</c>, so opening a trader opens <c>windowBackpack</c> too and
	/// feature 2's window-open postfix already fires. <c>XUiWindowGroup.OnOpen</c> runs the group's
	/// own controller before it opens the backpack, and <c>XUiC_TraderWindowGroup.OnOpen</c> sets
	/// <c>xui.Trader.TraderWindowGroup</c> as its first statement, so by the time the backpack
	/// opens the trader is already registered. On the way out the order is the same, so that field
	/// is back to null before the backpack closes - which is how <see cref="OnWindowClosed"/> tells
	/// the trader closing from a tab switch while it is still open.
	///
	/// The sort takes precedence over feature 2's locked order for that one open and is deliberately
	/// independent of <c>it autosort</c>: it is its own opt-in, and the trader window opening is an
	/// unambiguous event rather than something the tab-switch heuristic has to guess at.
	/// </summary>
	internal static class TraderSort
	{
		/// <summary>
		/// Whether the trader now on screen has been sorted for already. Cleared when the trader
		/// closes rather than when the backpack does - switching pages mid-trade closes the
		/// backpack, and must not re-sort the bag under the player's hands.
		/// </summary>
		private static bool sortedThisSession;

		/// <summary>
		/// A trader window is open and it is a trader rather than a vending machine. The vending
		/// machines share the window group and are told apart by the tile entity id, the same test
		/// the game itself uses to pick its "Vending"/"Trader" header.
		///
		/// <c>TraderEntity</c> is deliberately not used: it is also set during a trader quest
		/// turn-in, whose window group opens the backpack as well.
		/// </summary>
		internal static bool AtTrader(XUi _xui)
		{
			XUiM_Trader trader = _xui?.Trader;
			return trader != null && trader.TraderWindowGroup != null
				&& trader.TraderTileEntity != null && trader.TraderTileEntity.entityId != -1;
		}

		/// <summary>
		/// From <see cref="WindowLifecycle.AfterOpen"/>, before the locked sort gets its turn.
		/// Returns whether a sort ran, which stops the locked sort running as well and tells
		/// feature 5 the bag is freshly ordered.
		/// </summary>
		internal static bool OnWindowOpened(XUiC_ULM_BackpackWindow _window)
		{
			if (!Settings.Enabled || Settings.TraderSort.Length == 0 || _window == null)
			{
				return false;
			}
			XUi xui = _window.xui;
			if (!AtTrader(xui))
			{
				// Opening the bag anywhere else ends any session the close hooks missed.
				sortedThisSession = false;
				return false;
			}
			if (sortedThisSession)
			{
				Trace("skipped: this trader has been sorted for already");
				return false;
			}

			// The same guards as the locked sort, so nothing moves out from under the player.
			if (xui?.dragAndDrop != null && !xui.dragAndDrop.CurrentStack.IsEmpty())
			{
				Trace("skipped: an item is on the cursor");
				return false;
			}
			XUiC_ULM_BackpackGrid grid = BackpackAccess.Grid(_window);
			if (grid != null && grid.isShuffledBackpack)
			{
				Trace("skipped: shuffled backpack debuff");
				return false;
			}

			if (!SortLock.RunSort(_window, Settings.TraderSort))
			{
				Trace("could not sort: sorter " + (BackpackAccess.Sorter(_window) == null ? "missing" : "ok")
					+ ", button " + (_window.GetChildById(SortModes.ToButtonId(Settings.TraderSort)) == null
						? "missing" : "ok"));
				return false;
			}

			// Only once the sort actually ran: an open skipped by a guard above should be free to
			// try again on the next one.
			sortedThisSession = true;
			Counters.TraderSorts++;
			Trace("sorted by " + Settings.TraderSort);
			return true;
		}

		/// <summary>
		/// Postfix on <c>XUiC_TraderWindowGroup.OnClose()</c>: the trader is done with, so the next
		/// one sorts again. This is the exact end of a session, which is why it is worth a patch of
		/// its own rather than inferring it from the backpack closing.
		/// </summary>
		internal static void OnTraderClosed()
		{
			sortedThisSession = false;
		}

		/// <summary>
		/// From <see cref="WindowLifecycle.AfterClose"/>, as a fallback for the patch above: the
		/// trader window group nulls its field before it closes the backpack, so a null here means
		/// the trader has already gone and the next one should sort again. Non-null means the player
		/// only switched pages, which must not re-sort the bag mid-trade.
		/// </summary>
		internal static void OnWindowClosed(XUi _xui)
		{
			if (_xui?.Trader == null || _xui.Trader.TraderWindowGroup == null)
			{
				sortedThisSession = false;
			}
		}

		private static void Trace(string _what)
		{
			Log.Out(Patches.LogPrefix + "Sort at trader: " + _what + ".");
		}
	}
}
