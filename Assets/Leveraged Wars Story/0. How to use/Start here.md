# Start here

This vault is where the story of *Leveraged Wars* is written. It lives inside the game's project folder, so the game reads these notes directly: you write a scene here, save it, and the game plays it. No copying and pasting into Unity.

Read this note first, then [[How to write dialogue]] and [[How to write loans]]. Whoever connects your notes to missions in Unity should read [[Using notes in Unity]].

## What is in the vault

| Folder | What goes in it |
| --- | --- |
| **0. How to use** | These guides, and the **Templates** for new notes. |
| **1. Characters** | Character portraits (the images shown next to dialogue), and character notes if you want them. |
| **2. Missions** | One folder per mission (M1, M2, ...): its briefing, court conversations, battle cutscenes, and any notes about it. |
| **3. Loans** | One note per loan offer the player can take before a battle. |

You can add any other notes you like (lore, outlines, character sheets, ideas). The game ignores every note that is not marked as dialogue or a loan.

## Which notes the game uses

A note's **properties** (the box at the top of a note in Obsidian, written between two `---` lines) tell the game what it is:

- **dialogue: true** → the note is a **dialogue scene**: a briefing, a conversation in the tent, or a cutscene in battle. See [[How to write dialogue]].
- **Gold** (with Cost, Time left and Prestige Cost) → the note is a **loan offer**. See [[How to write loans]].
- Anything else → just a note for you. The game ignores it.

The quickest way to start a new one is a template: in Obsidian, open the command palette (Ctrl+P), choose **Templates: Insert template**, and pick **Dialogue Scene** or **Loan**. Notes inside the Templates folder are never used by the game.

## What happens when you save

As soon as Unity notices a new dialogue or loan note, it makes a game file for it with the same name, next to the note. You will not see that file in Obsidian, and you never need to edit it: every time you save the note, Unity rebuilds it from your text. Anywhere the game already uses the scene or loan picks up your changes straight away.

## Three rules that keep everything connected

1. **Move and rename files in Unity, not in Obsidian.** Unity keeps a hidden partner file (`.meta`) next to every note and image, and that is how the game remembers which note goes with which mission. Obsidian does not know about these partner files, so when it moves or renames something the game loses track of it. Writing and editing notes in Obsidian is always fine; reorganising should be done in Unity's Project window. (If a dialogue or loan note does get moved in Obsidian, Unity finds it again by its name, as long as the name did not change. Images and renamed notes have to be reconnected by hand.)
2. **Keep images inside the vault**, and link them by name, like `[[Jagen.png|Jagen]]`. A link by name keeps working wherever the image is moved, both in Obsidian and in the game.
3. **Give every image a different file name.** If two portraits are both called `Jagen.png`, nobody can tell which one you mean.

## Checking your work

Click a note's game file in Unity (the file with the note's name and the cube icon) to see what the game will show: every text box with its portraits, or the loan card. Anything Unity could not understand, such as a portrait it cannot find, is listed there as a yellow warning, and also appears in Unity's Console when you save.
