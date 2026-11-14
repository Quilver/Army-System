# Using notes in Unity

For whoever connects the story to the missions. Writers can skip this.

## Where notes are used

Each mission has a **Prep** sheet in Unity, at `Assets/Missions/Mxx - <name>/PreBattle/Mxx Prep`. Select it to see these fields in the Inspector:

| Field | Takes | What the player sees |
| --- | --- | --- |
| **Briefing Flow** | a dialogue note's game file | Plays when the tent opens before the battle, and is shown as text on the war map. |
| **Loans** | loan notes' game files, one per row | The offers pinned up in War Debts, in this order. |
| **Court** (3 members) | a dialogue note's game file per member | Plays when the player talks to them: 0 the Tactician, 1 the Captain, 2 the Dispatch-bearer. Empty: a placeholder line. |

Battle cutscenes: in the mission's battle scene, select the **CutScene** object and set its **Dialogue** field.

To fill a field, drag the note's **game file** (the file with the note's name and a cube icon, next to the note in the Project window) into it. Dragging the note itself does not work.

## Where the game files come from

Unity makes the game file for a note the moment it sees the note: **dialogue: true** makes a *Dialogue Script*, a **Gold** property makes a *Loan Offer*. They are rebuilt every time the note is saved. Never edit a game file: edit the note.

Useful menu items (top menu **Leveraged Wars > Story**):

- **Rebuild All Story Assets**: makes any missing game files and rebuilds all of them. Use it if something looks out of date.
- **Open Story Vault in Obsidian**.

For a markdown file outside the vault, right click it and choose **Create > Cutscenes > Dialogue Script from Markdown** or **Create > Preparations > Loan Offer from Markdown**.

## Moving things

Move and rename notes, images and folders in Unity's Project window. The game file moves with its note, and every mission keeps using it. If a note is moved in Obsidian instead, Unity finds it again by its name and moves the game file next to it; if it was also renamed, drag the note into the game file's **Markdown File** field. Images moved or renamed in Obsidian lose their connection to characters' profiles and have to be set again.

## For programmers

Format and code: `DialogueMarkdown` and `DialogueScript` (dialogue), `LoanMarkdown` and `LoanOffer` (loans), `StoryAssets` (building and keeping them in step with the notes).
