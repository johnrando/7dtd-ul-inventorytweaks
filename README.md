# InventoryTweaks

A 7 Days To Die mod for **Undead Legacy**. Eight small quality-of-life fixes for UL's backpack:

- **Sorting scrolls to the top, opening to the top or bottom.** Clicking a sort button re-sorts
  and jumps the list to the first page, so the sorted result is actually in view. Opening the
  inventory jumps to the first page too, or to the last one if you prefer, or neither. Also after a
  sort in loot containers and vehicle storage.
- **Scroll-to-end buttons.** A button at each end of the backpack scrollbar jumps to the top or
  the bottom. Right-click one and that end becomes where opening jumps to (it lights up bronze);
  right-click it again to turn that off.
- **Short tooltips, detail on Alt.** Every sort and scroll button's tooltip is one line ending in
  `[alt+]`; hold Alt while hovering and it swaps to what the button's right-clicks do.
- **Right-click a sort button to lock it.** The button lights up bronze, the backpack sorts, and
  from then on it re-sorts every time you open the inventory. Right-click again to unlock. The
  lock is remembered across restarts.
- **Ctrl+right-click a sort button to sort that way at traders.** The button turns gold and the
  backpack is sorted that way once each time you open a trader — by price, to put what is worth
  selling at the front. Ctrl+right-click again turns it off. It runs whatever else is locked, and
  a locked order still runs everywhere else.
- **New and changed items stay highlighted until you look at them.** Anything that lands in
  your backpack, or any stack whose count changes, gets a coloured frame that goes away when you
  hover the cell or close the inventory. Stackables also show how much the count moved: `+ 47`,
  `++ 47`, `+++ 47` (or `-`, `--`, `---`) in front of the number.
- **Sort by recency, and new items first.** A fifth button (`!`) beside UL's sort buttons.
  Left-click sorts the bag by when each stack last had something arrive, newest first. Right-click
  it and anything you have never had before is kept at the front of the bag: after every sort, on
  every real open, and the moment you switch it on. Ctrl+right-click it and stacks you already
  had whose count changed follow as a second tier. The rest keeps its sorted order; locked slots
  stay put.
- **The key that opened a page closes it.** Press B for the character page and B again to close
  the inventory, the way the unmodded game does. UL leaves only Tab and Escape to close.

## Installing

Download the zip from [Releases](https://github.com/johnrando/7dtd-ul-inventorytweaks/releases) and
extract it into the game's `Mods/`. The mod folder is the root of the archive, so it lands as:

```
Mods/InventoryTweaks/
├── ModInfo.xml
└── InventoryTweaks.dll
```

**Undead Legacy is required.** The mod patches UL's own inventory window; without UL it logs a
warning at startup and does nothing. Load order does not matter, and nothing needs building.

## Console commands

`it` prints the menu and changes nothing — `inventorytweaks` is an alias. Every line names the
command that changes it and says what it is for, so the menu is also the reference:

```
InventoryTweaks is ON
  it on|off                 : [ >on< | off ]                    - master switch
  it scroll {off|top|bottom}: [ off | >top< | bottom ]           - opening scrolls the backpack there, sorting to the top - or right-click a scrollbar button
  it containers             : [ >on< | off ]                    - ...loot / vehicle windows too, after a sort
  it lock {mode}            : [ >none< | weight | price | group | name ] - locked sort - or right-click a sort button
  it autosort               : [ >on< | off ]                    - re-sort on open while locked
  it tradersort {mode}      : [ >none< | weight | price | group | name ] - sort when a trader opens - or ctrl+right-click a sort button
  it newfirst               : [ on | >off< ]                    - new items first - or right-click the ! button
  it changedfirst           : [ on | >off< ]                    - ...then changed items - or ctrl+right-click it
  it toggle                 : [ >on< | off ]                    - a page's key closes the inventory again
  it highlight              : [ >on< | off ]                    - frame new or changed items until hovered
  it color {r,g,b,a}        : 215,170,70,205
  it markers                : [ >on< | off ]                    - +/- before a changed stack count
```

Three more: `it clear` drops every highlight, `it info` prints the same block with the patch
state and counters added, and `it reset` zeroes those counters. **Changes are saved** — see
[Settings file](#settings-file).

## What it does, precisely

**Sort to top, open to top or bottom.** UL's backpack is a 300-cell grid shown eight rows at a
time; its scrollbar is really a page number. After any of UL's sorts, the page is set to 0, which
is where the sorted result is. On a real open of the inventory - not on UL's close-and-reopen as
you switch tabs - the page is set to 0 or, with `it scroll bottom`, to the last page; that runs
after any sort-on-open, so it has the final say. Nothing about the sort itself changes.

**Scroll-to-end buttons.** There is no free space beside UL's scrollbar - the carry-weight box
sits under it and the sort row just above - so the track is shortened from 722 to 678 pixels and a
20-pixel button (UL's own page-turn sprite, rotated) sits at each end, inside the grid's height.
UL hardcodes the track length when it lays the bar out on open, deriving the tab height and the
pixels per page from it, so a postfix on that method rescales both for the shorter track. The tab
is positioned from the bar's origin, so the track stays put and the bar itself is moved down to
make room for the top button. Loot and vehicle windows, which share the class, are left alone.
Left-click jumps to that end; right-click sets `it scroll` to it (bronze), or to off if it was
already that end. The tint goes through the button's default and hover colours rather than its
selected state, which would hide the hover highlight.

**Tooltips.** UL's four sort buttons keep their own text with ` [alt+]` appended; the `!` and
scroll buttons get a short line of their own. The backpack window's per-frame update checks Alt
and, only when it changes, rewrites each button's tooltip; the game pushes a new text into the
tooltip already on screen, so the swap happens under the cursor. Alt is not bound to anything in
UL's inventory - the GUI actions that might have served are controller-only or taken by search.

**Locked sort.** Right-click reaches a different event from the one UL's sorter listens on, so UL's
own left-click behaviour is untouched. The sort on open is UL's own sort, run through UL's own
sorter, so slots you have locked with the slot-lock key stay put and the search filter is honoured
exactly as a left-click would. It runs only when it cannot get in your way:

- on a real open, not when UL closes and reopens the window as you switch between the crafting,
  character and other tabs;
- when nothing is on your cursor;
- when the sort buttons are not greyed out by UL's *Shuffled Backpack* debuff.

**Sort at traders.** Ctrl+right-click puts a sort button in a gold state meaning *this is the
order to use at a trader*; Ctrl+right-click again turns it off. It is a separate click from the
lock, so setting one never disturbs the other on a different button. One button can be bronze and
one gold, never the same one, so making the locked order the trader order drops the lock and the
other way round.

UL's trader window opens the backpack alongside it, which is the same window-open the locked sort
already hooks, so the sort itself needs no new hook — the mod just asks whether a trader is on
screen. It sorts **once per trader**: switching to another page and back while you are still
trading leaves your bag alone. The end of that visit is the one thing that does get a hook, on the
trader window's own close, so that walking away and coming back sorts again.
It ignores `it autosort` and overrides the locked order for that one open,
which is the point — your normal order still runs everywhere else. Vending machines are left
alone. The same cursor and *Shuffled Backpack* guards as the locked sort apply.

Sorting by price puts the valuable stacks first and anything a trader will not buy last; it is UL's
own price sort, so it reads the item's economic value and is the same order you would get by
clicking the button yourself.

**Sort by recency.** The `!` button is added to UL's sort row in memory, just before the game
parses the window XML — a plain XML patch could not do it, because mod patches apply in folder
order and `InventoryTweaks` sorts before `UndeadLegacy`. Its left-click is a sort like the other
four: every stack is ordered by the moment stock last arrived in it from outside the bag, newest
first, and anything that was already in the bag when you spawned goes last. The timestamp is the
one the highlight already keeps for each slot — it follows an item when it moves and stays after
the highlight is cleared — so it is one number per slot, never grows, and starts over each game.
Locked slots stay put, and with *new first* on the tiers are applied on top.

**New items first.** Right-click toggles *new first*; the button lights up in the same bronze as
a locked sort. Ctrl+right-click toggles the *changed* tier on top (switching new-first on if it
was off); the icon then takes the trader gold. Both are saved like the lock.

While on, after any UL sort of the backpack — a sort button, the locked sort on open, or the
standard-controls sort — the stacks the new-item highlight knows you never had before (framed,
no `+`/`-` marker) are pulled to the front of the movable slots, then, with the changed tier on,
the stacks you already owned whose count changed (the `+`/`-` ones), then everything else. Each
tier keeps the order UL just sorted it into. On a real open with no locked sort, and at the
moment you switch the button on, there is no sort order to follow, so within each tier the most
recently changed stack comes first and the rest of the bag stays as it was. On open only the
latest batch is lifted - stacks that arrived while the inventory was closed; anything still
highlighted from an earlier visit keeps its place.

Slots locked with the slot-lock key are never moved, exactly as UL's own sorter leaves them; the
item open in the modify window stays selected. Hovering a highlighted item acknowledges it, so
it drops back into normal order on the next sort. The same guards as the locked sort apply on
open: not on a tab switch, never while an item is on the cursor, never under the *Shuffled
Backpack* debuff.

**Page key closes.** Vanilla's window selector closes everything when the page a key asks for is
the one already showing. UL replaces that method with a Harmony prefix of its own and drops the
toggle. Harmony runs every prefix even after one skips the original, so the mod cannot simply
close from a prefix - UL's would reopen the page straight after. Instead a prefix notes whether
the page was already showing, UL's prefix runs as usual (opening a page that is open does
nothing), and a postfix then closes the inventory; every other press is left untouched. It
applies to every page key (B, N, O, M and so on), not Tab, which UL handles separately. Only a
key press counts: an item's *Recipes* action, perk links and the like also ask the selector for
a page that may already be showing, and those must stay no-ops, so the toggle fires only on the
frame a page-key action was dispatched.

**New-item highlight.** The frame is UL's own hover frame, tinted; its width is part of the
sprite, so the colour setting (with its alpha) is what controls how loud it is.

The bag only reports "something changed", so the mod keeps a fingerprint of every slot (item,
quality, seed) plus the count you last acknowledged, and diffs the bag on each change. Items that
merely moved — by a sort, a drag, a shift-click — carry their state with them; merging two stacks
adds their acknowledged counts together and splitting one divides it, so only stock that came
from (or went) outside the backpack registers as a change. A stack nobody had before is
highlighted with no marker.

A highlight clears when you hover the cell, or when you close the inventory — a real close, not
UL's close-and-reopen as you switch tabs. Anything that arrives while the inventory is closed is
still highlighted when you next open it. Being on screen does nothing by itself; a cell you never
hover stays highlighted until you close the window.

**Count markers.** For a stackable whose count differs from what you last acknowledged, the
number is prefixed: one `+` or `-` for a change of a single item, three once the stack has reached
three times the acknowledged count (or fallen to a third of it), two for anything in between.
Hovering resets the acknowledged count to the current one.

Hover, selection, drag and UL's search overlay all draw over the frame unchanged.

## Defaults

| Setting | Default |
|---|---|
| scroll on open (sorts go to the top) | top |
| ...in loot / vehicle windows (after a sort) | on |
| locked sort | none |
| re-sort on open while locked | on |
| sort when a trader opens | none |
| new items first | off |
| ...then changed items | off |
| page key closes the inventory | on |
| new-item highlight | on |
| highlight colour | 215,170,70,205 |
| count markers | on |

## Settings file

Every setting survives a restart. A change made with `it`, by right-clicking a sort button, the
`!` button or a scrollbar button, is
written straight out to:

```
%APPDATA%/7DaysToDie/InventoryTweaks/settings.txt
```

— the game's own user data folder, next to `Saves`, rather than `Mods/InventoryTweaks/`, so
updating the mod does not take your settings with it. `it info` prints the full path and whether
the last read or write worked.

It is plain `key = value` text, one line per setting, each naming the command that sets it:

```
enabled       = on               # it on|off
scroll        = top              # it scroll {off|top|bottom} - or right-click a scrollbar button
containers    = on               # it containers
lock          = none             # it lock {none|weight|price|group|name} - or right-click a sort button
autosort      = on               # it autosort
tradersort    = none             # it tradersort {none|weight|price|group|name} - or ctrl+right-click a sort button
newfirst      = off              # it newfirst - or right-click the ! button by the sort buttons
changedfirst  = off              # it changedfirst - or ctrl+right-click the ! button
toggle        = on               # it toggle
highlight     = on               # it highlight
color         = 215,170,70,205   # it color {r,g,b,a}
markers       = on               # it markers
```

Edit it by hand with the game closed — it is rewritten whenever a setting changes. A line that
will not parse is logged and ignored rather than fatal, and deleting the file brings back the
defaults above (which live in `Settings.cs`).

## Undead Legacy

**Required.** Tested against **UL 2.7.36**. Every patch targets a UL class by name and reports
whether it applied in the startup log and in `it info`; a UL update that renames one of them
degrades to a log line, not a crash. The mod warns, but still installs, when it sees a UL build
outside the range it was tested on.

## Limitations

- **Highlights are not saved.** They live in memory and reset when the game is restarted. The
  sort lock and the trader sort are saved.
- **Backpack only.** Items that land straight in the toolbelt are not highlighted.
- Highlights are per client. On a dedicated server the mod runs on each player's own machine and
  only sees that player's backpack.

## Building

Requires the .NET SDK; there are no NuGet dependencies. The mod builds in place inside the game
install, against the game's own assemblies and the installed `Mods/UndeadLegacy/UndeadLegacy.dll`.

```
dotnet build src/InventoryTweaks/InventoryTweaks.csproj -c Release
```

That restages `dist/InventoryTweaks/`, ready to copy into `Mods/`. To also build the release archive:

```
dotnet build src/InventoryTweaks/InventoryTweaks.csproj -c Release -t:Package
```

That writes `release/InventoryTweaks-v<version>-<date>.zip`, taking the version from `ModInfo.xml`.
Neither `dist/` nor `release/` is tracked — the zip is published as a GitHub Release instead.

## License

MIT — see `LICENSE`.
