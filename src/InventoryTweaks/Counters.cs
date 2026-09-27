namespace InventoryTweaks
{
	/// <summary>
	/// Live counters behind <c>it info</c>: the startup log proves the patches were installed,
	/// these prove they are being reached. No locking: all writes happen on the main thread.
	/// </summary>
	internal static class Counters
	{
		/// <summary>Sorts that moved the list to the chosen end (any window).</summary>
		internal static int SortsScrolled;

		/// <summary>Real opens of the backpack that moved the list to the chosen end.</summary>
		internal static int OpenScrolls;

		/// <summary>Left-clicks on the scrollbar's end buttons that moved the list.</summary>
		internal static int ButtonScrolls;

		/// <summary>Sorts run automatically because a sort order is locked.</summary>
		internal static int AutoSorts;

		/// <summary>Sorts run because a trader window opened.</summary>
		internal static int TraderSorts;

		/// <summary>Times new items were moved to the front of the backpack.</summary>
		internal static int NewFirstSorts;

		/// <summary>Left-clicks on the ! button: the backpack sorted by recency.</summary>
		internal static int RecencySorts;

		internal static int KeyCloses;

		/// <summary>Backpack slots flagged as newly arrived.</summary>
		internal static int ItemsFlagged;

		/// <summary>Flagged slots cleared because they were on screen long enough.</summary>
		internal static int ItemsSeen;

		/// <summary>Bag change events the tracker examined.</summary>
		internal static int BagChanges;

		internal static void Reset()
		{
			SortsScrolled = 0;
			OpenScrolls = 0;
			ButtonScrolls = 0;
			AutoSorts = 0;
			TraderSorts = 0;
			NewFirstSorts = 0;
			RecencySorts = 0;
			KeyCloses = 0;
			ItemsFlagged = 0;
			ItemsSeen = 0;
			BagChanges = 0;
		}
	}
}
