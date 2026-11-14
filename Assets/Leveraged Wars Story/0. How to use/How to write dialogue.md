# How to write dialogue

A dialogue note is one scene: a mission briefing, a chat with a member of the court in the tent, or a cutscene during a battle. The player sees it Fire Emblem style: a text box at the bottom, the speaker's name, and up to two portraits.

Start from the **Dialogue Scene** template, or make sure your note begins with this property:

```
---
dialogue: true
---
```

## A complete example

```
---
dialogue: true
---
%% Draft 2. Jagen should sound more worried here. %%

# [[Jagen.png|Jagen]]
My prince, I spotted several armed thugs coming towards us.

We must take up arms and prepare for battle.

# [[Dark Prince.png|Prince Julian]]
Sound the horn. It is time that I earn my spurs.

# Jagen
<wpm=120>
*As you command.*
```

This plays four text boxes: two for Jagen, one for Julian, one more for Jagen, typed out slowly and in italics.

## Speakers

- A heading with one `#` starts a new speaker: everything under it, until the next `#` heading, is what they say.
- **Give a speaker a portrait** by making the heading a link to their image: `# [[Jagen.png|Jagen]]`. The part after `|` is the name shown in the game. Type `[[` in Obsidian and start typing the image's name to pick it from a list.
- `# Jagen` on its own, after Jagen has had a portrait, brings him back with the **same** portrait.
- **A new expression:** link a different image with the same name, e.g. `# [[Jagen angry.png|Jagen]]`. From then on Jagen uses the new portrait.
- **Narration:** a speaker who never has a portrait (for example `# Narrator`) speaks with no portrait. Their name still shows above the text.

## Text boxes

- **Each paragraph is one text box.** Leave an empty line between paragraphs to make the next box.
- A single line break (Enter once) stays a line break inside the same box.
- Keep a box to two or three short sentences so it fits on screen.
- `**bold**`, `*italic*` and `~~strikethrough~~` show in the game as they do in Obsidian.
- Links in the middle of a line are fine (`[[Amsteradan]]`): only their text appears in the game.

## Who stands where

Two portraits fit on screen, one on the left and one on the right. The first speaker with a portrait goes on the left, the second on the right. When a third arrives, they replace whoever spoke longest ago. The one talking is shown brightly; the other is dimmed.

To choose a side yourself, add `{left}` or `{right}` at the end of the heading: `# [[Jagen.png|Jagen]] {right}`.

## Typing speed

Text types out letter by letter. To change the speed of the **next** text box only, put a line like `<wpm=90>` above it: the number is words per minute. Slow is around 80, normal is around 250, very fast is 400 or more.

## Notes to yourself

- Anything **above the first `#` heading** is never shown: use it for scene notes.
- Anything between `%%` marks is never shown, wherever it is: `%% check this with the lore %%`. (`<!-- like this -->` works too.)

## When something looks wrong

Click the scene's game file in Unity (same name as the note) to see every text box as the player will. Yellow warnings explain problems, for example:

- *portrait not found*: the image is not in the vault, or its name is misspelt;
- *the portrait link is out of date*: the image was moved, and the link points at the old place. Re-link it by name;
- *No text boxes found*: the note has no `#` speaker heading, or nothing under it.
