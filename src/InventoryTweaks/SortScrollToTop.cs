namespace InventoryTweaks
{
	/// <summary>
	/// Feature 1. UL's sort leaves the list on whatever page it was, so the sorted result is out
	/// of sight. After any sort, jump to the end <c>it scroll</c> asks for: the top (page 0) as
	/// standard, or the bottom. The same <c>ULM_StackSorter</c> serves the backpack ("Player"),
	/// loot containers ("LootWindow") and vehicle storage ("Vehicle"); it carries a reference to
	/// the latter two windows, and the backpack window is looked up by name.
	/// </summary>
	internal static class SortScrollToTop
	{
		/// <summary>Postfix on <c>ULM_StackSorter.OnBtnSort(XUiController, int)</c>.</summary>
		internal static void AfterSort(ULM_StackSorter __instance)
		{
			if (!Settings.Enabled || !Settings.ScrollOn || __instance == null)
			{
				return;
			}

			ULM_StackScrollBar scrollBar = null;
			switch (__instance.id)
			{
			case "Player":
				scrollBar = BackpackAccess.Window(__instance.xui)?.scrollBar;
				break;
			case "LootWindow":
				if (Settings.ScrollContainers)
				{
					scrollBar = __instance.lootWindow?.scrollBar;
				}
				break;
			case "Vehicle":
				if (Settings.ScrollContainers)
				{
					scrollBar = __instance.vehicleWindow?.scrollBar;
				}
				break;
			}

			if (BackpackAccess.AutoScroll(scrollBar))
			{
				Counters.SortsScrolled++;
			}
		}

		/// <summary>Postfix on <c>XUiC_ULM_BackpackWindow.BtnSort_OnPress(PackedBoolArray)</c>.</summary>
		internal static void AfterStandardSort(XUiC_ULM_BackpackWindow __instance)
		{
			if (!Settings.Enabled || !Settings.ScrollOn)
			{
				return;
			}
			if (BackpackAccess.AutoScroll(__instance?.scrollBar))
			{
				Counters.SortsScrolled++;
			}
		}
	}
}
