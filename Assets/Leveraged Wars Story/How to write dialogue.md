# How to write dialogue

Notes with the property **dialogue: true** become playable dialogue in Unity: a *Dialogue Script* asset appears next to the note the moment Unity sees it, and is rebuilt every time you save the note. Use the template (**Templates > Dialogue Scene**) to start one. Notes without the property are left alone, so lore, outlines and character sheets can live here too.

## The format

- `# Name` starts a speaker. Link a portrait to give them one: `# [[Jagen.png|Jagen]]`, `# ![[Jagen.png]]`, or a link made with *Copy Obsidian URL*, e.g. `# [Jagen](obsidian://open?vault=...)`.
- Every paragraph under the heading is one text box. A single line break stays a line break inside the box.
- `# Jagen` again (no link) brings the same character back with the same portrait. A new link with the same name swaps their portrait (a new expression).
- A name that never had a portrait speaks as narration, with no portrait.
- Two portraits fit on stage. A new speaker takes the empty side, otherwise replaces whoever spoke longest ago. Add `{left}` or `{right}` to the heading to choose.
- `<wpm=90>` on its own line sets the typing speed of the next text box.
- `**bold**`, `*italic*` and `~~strike~~` show in game. Links in a line keep only their text.
- Anything before the first `#` heading, and anything between `%%` marks (or `<!-- -->`), is a note and never shows.
- You can still use `## Text` subheadings for each box if you prefer; in a speaker's section that uses them, plain lines before the first one are notes.

## In Unity

- Select the note's Dialogue Script to preview every box with its portraits and any warnings (a portrait not found, a missing name).
- Drop the Dialogue Script into any dialogue slot: a mission's briefing flow, a court member, or a CutScene.
- **Leveraged Wars > Dialogue > Rebuild All Dialogue Scripts** makes any missing scripts and rebuilds the rest.
- **Leveraged Wars > Dialogue > Open Story Vault in Obsidian** opens this vault.
- Portraits must be inside this vault, with their Texture Type set to *Sprite (2D and UI)*.
