# How to write loans

Before each battle the player can borrow money in the tent's **War Debts** book. Each loan offer is pinned up as a card. Every card is one note in **3. Loans**.

Start from the **Loan** template. A loan note looks like this:

```
---
Gold: 500
Cost: 2000
Time left: 10
Prestige Cost: 12
---
His majesty of Moricia has great hopes in you.
*Easy money always comes with a hook*
```

## The properties

| Property | Meaning |
| --- | --- |
| **Gold** | Gold the player receives when they take the loan. |
| **Cost** | Gold the player must pay back. The card works out the interest from Gold and Cost (500 borrowed, 2000 to repay = 300% interest). |
| **Time left** | Months the player has to repay it. |
| **Prestige Cost** | Prestige the player needs to take the loan; it is spent when they do. |
| **Title** *(optional)* | The name on the card. Without it, the card uses the note's name, so `Loan, easy.md` is called "Loan, easy". |

All values are whole numbers. Upper or lower case and spaces in the names do not matter (`Time Left` and `time left` both work).

## The card text

Everything below the properties is the text on the card: who is offering the money and what the catch is. Keep it to two or three short lines.

- Line breaks are kept.
- `**bold**`, `*italic*` and `~~strikethrough~~` show in the game.
- `%% notes %%` are never shown.

## Using a loan in a mission

A loan only appears in the game once it is added to a mission's list of loans in Unity (see [[Using notes in Unity]]). The same loan can be offered in several missions. Changing the note changes the loan everywhere it is offered.

If a number is missing or is not a number, the loan's game file in Unity shows a yellow warning saying which one.
