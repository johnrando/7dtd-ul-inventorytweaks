using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace InventoryTweaks
{
	/// <summary>
	/// Feature 7. A button at either end of the backpack's scrollbar: left-click jumps the list to
	/// that end, right-click makes that end the one <c>it scroll</c> jumps to on a real open (the
	/// active button draws bronze), and right-clicking the active button turns that off - which
	/// also stops the jump to the top after a sort. Same click shape as the sort row.
	///
	/// There is no room beside UL's scrollbar - the carry-weight box sits under it and the sort row
	/// close above - so the track is shortened to fit the buttons inside the grid's height. UL
	/// hardcodes the track at 722px in <c>ULM_StackScrollBar.OnOpen</c>, deriving the tab height
	/// and the pixels per page from it, so after that runs the postfix here rescales both to the
	/// shorter track. The tab is positioned relative to the scrollbar rect's origin, so the track
	/// stays at the origin and the rect itself is moved down to leave room for the top button.
	///
	/// Like feature 5's button, the two are added to the window XML in memory just before XUi
	/// parses it; a Config XML patch would run before UL's, by folder order.
	/// </summary>
	internal static class ScrollButtons
	{
		internal const string UpId = "btnScrollTop";

		internal const string DownId = "btnScrollBottom";

		/// <summary>UL's track length, the value its scrollbar hardcodes.</summary>
		private const int UlTrack = 722;

		private const int ButtonSize = 20;

		private const int Gap = 2;

		/// <summary>Room taken at each end of the track: a button and a gap.</summary>
		private const int Inset = ButtonSize + Gap;

		internal const int Track = UlTrack - 2 * Inset;

		private const string ButtonSprite = "ui_btn_page_up";

		private const string ButtonHoverSprite = "ui_btn_page_up_h";

		private const string UpToolTip = "Scroll to top";

		private const string DownToolTip = "Scroll to bottom";

		private const string DetailSuffix = "\nRight-click: Auto-scroll on open";

		/// <summary>Each button's default and hover colours before we tinted them.</summary>
		private static readonly Dictionary<XUiV_Button, Color[]> originalColors = new Dictionary<XUiV_Button, Color[]>();

		/// <summary>What the XML injection did, as reported by <c>it info</c>.</summary>
		internal static string ButtonStatus = Patches.NotRunYet;

		/// <summary>Prefix on <c>XUiFromXml.loadWindows(XmlFile)</c>: adds a button to each end of the scrollbar rect.</summary>
		internal static void BeforeLoadWindows(XmlFile _xmlFile)
		{
			try
			{
				XElement rect = _xmlFile?.XmlDoc?.Root?
					.Descendants("window")
					.Where(w => (string)w.Attribute("name") == BackpackAccess.WindowName)
					.Descendants("rect")
					.FirstOrDefault(r => (string)r.Attribute("name") == "scrollBar");
				if (rect == null)
				{
					ButtonStatus = "NOT ADDED - windowBackpack has no scrollBar rect";
					Log.Warning(Patches.LogPrefix + "Could not find UL's backpack scrollbar in windows.xml, so "
						+ "the scroll-to-end buttons will be missing; 'it scroll' still works.");
					return;
				}
				if (rect.Elements("button").Any(b => (string)b.Attribute("name") == UpId))
				{
					ButtonStatus = "added - already present";
					return;
				}

				// The page sprite points right; NGUI's rotation is counter-clockwise. The buttons
				// are centred so the rotation does not move them.
				int half = ButtonSize / 2;
				rect.Add(Button(UpId, half, Inset - half, 90, UpToolTip));
				rect.Add(Button(DownId, half, -(Track + Gap + half), -90, DownToolTip));

				// The track sprite takes its 722 from a style; the attribute here is the same
				// request the postfix makes again once UL has laid the bar out.
				XElement track = rect.Elements("sprite")
					.FirstOrDefault(s => (string)s.Attribute("name") != "scrollTab");
				track?.SetAttributeValue("size", ButtonSize + "," + Track);

				ButtonStatus = "added - both ends of the scrollbar";
				Log.Out(Patches.LogPrefix + "Added the scroll-to-end buttons to windowBackpack.");
			}
			catch (Exception e)
			{
				ButtonStatus = "NOT ADDED - " + e.Message;
				Log.Warning(Patches.LogPrefix + "Could not add the scroll-to-end buttons: " + e.Message);
			}
		}

		private static XElement Button(string _name, int _x, int _y, int _rotation, string _toolTip)
		{
			return new XElement("button",
				new XAttribute("name", _name),
				new XAttribute("depth", "6"),
				new XAttribute("size", ButtonSize + "," + ButtonSize),
				new XAttribute("pivot", "center"),
				new XAttribute("pos", _x + "," + _y),
				new XAttribute("rotation", _rotation.ToString()),
				new XAttribute("type", "sliced"),
				new XAttribute("sprite", ButtonSprite),
				new XAttribute("hoversprite", ButtonHoverSprite),
				new XAttribute("hoverscale", "1.1"),
				new XAttribute("foregroundlayer", "true"),
				new XAttribute("tooltip_key", _toolTip));
		}

		/// <summary>From <see cref="SortLock.AfterInit"/>: wires the buttons once the window exists.</summary>
		internal static void AfterInit(XUiC_ULM_BackpackWindow _window)
		{
			Wire(_window, UpId, UpToolTip);
			Wire(_window, DownId, DownToolTip);
		}

		private static void Wire(XUiC_ULM_BackpackWindow _window, string _id, string _toolTip)
		{
			XUiController button = _window.GetChildById(_id);
			if (button == null)
			{
				return;
			}
			button.OnPress += OnPress;
			button.OnRightPress += OnRightPress;
			ToolTips.Register(button, _toolTip, _toolTip + DetailSuffix);
		}

		/// <summary>"top" or "bottom" for one of our buttons, else null.</summary>
		private static string Direction(XUiController _sender)
		{
			if (!Settings.Enabled || _sender?.ViewComponent == null)
			{
				return null;
			}
			switch (_sender.ViewComponent.ID)
			{
			case UpId:
				return "top";
			case DownId:
				return "bottom";
			default:
				return null;
			}
		}

		private static void OnPress(XUiController _sender, int _mouseButton)
		{
			string direction = Direction(_sender);
			if (direction == null)
			{
				return;
			}
			XUiC_ULM_BackpackWindow window = BackpackAccess.Window(_sender.xui);
			if (BackpackAccess.ScrollTo(window?.scrollBar, direction == "bottom"))
			{
				Counters.ButtonScrolls++;
			}
		}

		/// <summary>Right-click: this end becomes the auto-scroll end, or off if it already was.</summary>
		private static void OnRightPress(XUiController _sender, int _mouseButton)
		{
			string direction = Direction(_sender);
			if (direction == null)
			{
				return;
			}
			Settings.ScrollTo = Settings.ScrollTo == direction ? "off" : direction;
			Config.Save();
			ApplyIndicator(BackpackAccess.Window(_sender.xui));
		}

		/// <summary>
		/// Bronze on the button whose end is the auto-scroll end, default on the other. Through the
		/// default and hover colours rather than <c>Selected</c>: a selected button never shows its
		/// hover sprite, so the tint would swallow the hover highlight.
		/// </summary>
		internal static void ApplyIndicator(XUiC_ULM_BackpackWindow _window)
		{
			if (_window == null)
			{
				return;
			}
			Tint(_window, UpId, Settings.Enabled && Settings.ScrollTo == "top");
			Tint(_window, DownId, Settings.Enabled && Settings.ScrollTo == "bottom");
		}

		private static void Tint(XUiC_ULM_BackpackWindow _window, string _id, bool _on)
		{
			if (!(_window.GetChildById(_id)?.ViewComponent is XUiV_Button view))
			{
				return;
			}
			if (!originalColors.TryGetValue(view, out Color[] original))
			{
				original = new[] { view.DefaultSpriteColor, view.HoverSpriteColor };
				originalColors[view] = original;
			}
			view.DefaultSpriteColor = _on ? (Color)SortLock.LockedIconColor : original[0];
			view.HoverSpriteColor = _on ? (Color)SortLock.LockedIconColor : original[1];
		}

		/// <summary>
		/// Postfix on <c>ULM_StackScrollBar.OnOpen()</c>. UL has just laid the bar out for 722px;
		/// for the backpack's bar, and only while it is showing, shorten the track and rescale the
		/// tab and the page step to match, then drop the rect to make room for the top button. UL
		/// calls <c>UpdateScrollBarPosition</c> straight after, which places the tab from the new
		/// step. Loot and vehicle bars share this class and are left alone.
		/// </summary>
		internal static void AfterScrollBarOpen(ULM_StackScrollBar __instance)
		{
			if (__instance == null || __instance.maxPages <= 0 || __instance.scrollBar == null)
			{
				return;
			}
			XUiC_ULM_BackpackWindow window = BackpackAccess.Window(__instance.inventory?.xui);
			if (window == null || !ReferenceEquals(window.scrollBar, __instance))
			{
				return;
			}
			if (window.GetChildById(UpId) == null)
			{
				// The XML injection did not happen, so nothing needs the room.
				return;
			}

			XUiView rect = __instance.scrollBar.ViewComponent;
			Vector3 position = rect.UiTransform.localPosition;
			rect.UiTransform.localPosition = new Vector3(position.x, -Inset, position.z);

			List<XUiController> children = __instance.scrollBar.Children;
			for (int i = 0; i < children.Count; i++)
			{
				if (children[i] != __instance.scrollTab && children[i].ViewComponent is XUiV_Sprite track)
				{
					track.Size = new Vector2i(ButtonSize, Track);
					break;
				}
			}

			int tab = 0;
			if (__instance.scrollTab?.ViewComponent is XUiV_Sprite tabSprite)
			{
				tab = Math.Max(ButtonSize, Mathf.RoundToInt(tabSprite.Size.y * (float)Track / UlTrack));
				tabSprite.Size = new Vector2i(tabSprite.Size.x, tab);
			}
			__instance.scrollBarPixelsPerPage = (float)(Track - tab) / __instance.maxPages;
		}

		/// <summary>
		/// A real open: jump to the chosen end. Runs last, so it has the final say over a sort that
		/// ran on this open and scrolled to the top.
		/// </summary>
		internal static void OnWindowOpened(XUiC_ULM_BackpackWindow _window, bool _realOpen)
		{
			if (!_realOpen || !Settings.Enabled || !Settings.ScrollOn || _window == null)
			{
				return;
			}
			if (BackpackAccess.AutoScrollOnOpen(_window.scrollBar))
			{
				Counters.OpenScrolls++;
			}
		}
	}
}
