using System.Collections.Generic;

namespace InventoryTweaks
{
	/// <summary>
	/// Short tooltips with detail on demand. Every button this mod adds or extends (the scroll-to-end
	/// buttons, the ! button and UL's four sort buttons) keeps a one-line tooltip ending in
	/// <see cref="Tag"/>; while Alt is held the full description of its right-click behaviour shows
	/// instead. The game's tooltip setter pushes a new text into the tooltip already on screen, so
	/// the swap happens under the cursor without moving it. Alt is not bound to anything in UL's
	/// inventory; the GUI actions that might have served are controller-only or taken by search.
	/// </summary>
	internal static class ToolTips
	{
		/// <summary>The tag on every short text. Square brackets are NGUI markup, so change this if the label eats it.</summary>
		internal const string Tag = " [alt+]";

		private struct Pair
		{
			internal string Short;

			internal string Detail;
		}

		private static readonly Dictionary<XUiV_Button, Pair> registered = new Dictionary<XUiV_Button, Pair>();

		private static bool altWasDown;

		private static bool enabledWas = true;

		/// <summary>Stores the pair for <paramref name="_button"/> and shows the short text now.</summary>
		internal static void Register(XUiController _button, string _short, string _detail)
		{
			if (!(_button?.ViewComponent is XUiV_Button view))
			{
				return;
			}
			registered[view] = new Pair { Short = _short, Detail = _detail };
			Apply(view, registered[view], altWasDown);
		}

		/// <summary>From the backpack window's per-frame update: only touches the texts when Alt or the master switch changes.</summary>
		internal static void Tick()
		{
			bool alt = Settings.Enabled && InputUtils.AltKeyPressed;
			if (alt == altWasDown && Settings.Enabled == enabledWas)
			{
				return;
			}
			altWasDown = alt;
			enabledWas = Settings.Enabled;
			foreach (KeyValuePair<XUiV_Button, Pair> entry in registered)
			{
				Apply(entry.Key, entry.Value, alt);
			}
		}

		/// <summary>Master switch off: UL's own text, no tag, no detail.</summary>
		private static void Apply(XUiV_Button _view, Pair _pair, bool _alt)
		{
			if (!Settings.Enabled)
			{
				_view.ToolTip = _pair.Short;
			}
			else
			{
				_view.ToolTip = _alt ? _pair.Detail : _pair.Short + Tag;
			}
		}
	}
}
