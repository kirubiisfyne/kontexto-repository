# The Rookie

### Narrative
Maya/Kiko tries to enter The Paragon Publication office. The glossy double doors feature a "NO ID, NO ENTRY" sign. A strict Security Officer NPC blocks the path, stating the area is restricted to editorial staff. Maya/Kiko proudly presents a crumpled yellow intermediate pad paper with their name and "Encoder" written in shaky handwriting. The guard dismisses it as not signed and tells Maya/Kiko to get a “real one”.

### Cutscene
Camera Pan of the Paragon Publication Office Exterior and Interior.

**Maya/Kiko**
> "Okay. First day as a probationary member. Just walk in. Act like you belong. Don't trip."

```
[Narrative Box / UI Text]:
“Objective: Enter the Publication Office.”
```

---

## Interaction 1: The Security Guard (Office-Admin NPC)
*A strict-looking security guard cuts the player’s path just as Maya/Kiko is about to enter.*

**Security Guard**
> "Hold up, Kid. Where’s your application form? This area is restricted to editorial staff only."

**Maya/Kiko**
> "Oh! I have it! Enzo signed me up!"

### Cutscene
* [Maya/Kiko holding the application and handing it out to the guard]
* [Hand to Guard]

**Security Guard**
> “Your form is not signed. The Paragon has rules. If I let you in now, the admin will have my badge. Go get it signed by the Editor-in-Chief.”

**Maya/Kiko**
> "Great… I haven't even stepped inside, and I'm already failing. Okay... let’s look for Enzo..."

---

## Game Level 1

### Subtask 1: Find Enzo
*Maya/Kiko must look for Enzo. There, they interact with the Enzo NPC who manages the club.*

`[Maya/Kiko finds Enzo in the library]`

#### Inactive - First Interaction
**Maya/Kiko**
> “Uhh… Enzo, can you sign my application form for me, please?”

**Enzo**
> “Yeah, just leave it there on the table.”

**Maya/Kiko**
> “But uh… I kinda need it right now. The security guard won’t let me in the office.”

**Enzo**
> “But Ugh… I have so many urgent things to do too!! … Help me with some print-outs first. Mrs. Santos is making me do so much for the first day.”

#### Subtask 2: The Printer
*Maya/Kiko must go to the printer in the library, print Enzo's papers, and deliver them to Enzo.*

#### Active - Task Not Completed Yet
**Enzo**
> “Have you done it yet? I need to get these files to Mrs. Santos ASAP..”

#### Completed Task
**Enzo**
> “Mhmm… page one to page fifty seven! A4, All complete with zero missing parts and jams. Perfect! All done!!! Thank you so so much! You have no idea how much this saved me. Now… let me see that form…”

---

### Subtask 3: Return to the Office
*Maya/Kiko must then return to the building where the Paragon Office is located and present the signed form again.*

#### Dialogue Variations Based on State

* **Before Signing (Active Hint):**
    * **Security Guard:** "Just.. go get the form signed kid. it’s as easy as that”
* **Attempting Entry Without Signed Form (Active):**
    * **Security Guard:** "Sorry bud, no form, no entry..”
* **Presenting Signed Form (Completed):**
    * **Security Guard:** "Let me see that.. Alright, rookie. You look like you belong now. Go on in.”

`[Maya/Kiko walks up to the Publication Office and sees Mrs. Santos]`

---

## Interaction 2: Mrs. Santos
*Inside the cold, quiet office smelling of old paper, Maya/Kiko meets Mrs. Santos, the adviser. Mrs. Santos sits at a desk with perfectly stacked papers and a **RED PEN**. She states that the guard doing his job proves they have standards. Mrs. Santos slides a messy, handwritten sticky note across the desk and demands a staff profile for the digital database.*

#### Inactive - First Interaction
**Mrs. Santos**
> “Hi, Rookie. You’re late.”

**Maya/Kiko** *(Internal Monologue)*
> "Oh no. She noticed. Do I tell her about the guard? Or do I just apologize?"

**Maya/Kiko**
> "The guard wouldn't let me in, Ma'am."

**Mrs. Santos**
> "The guard? Good. That means he's doing his job. We have standards here at The Paragon. If you can't present a proper ID, you can't be trusted with a headline."
>
> "Since you're the new encoder, let's see if you can follow simple instructions. I need your staff profile for the digital database."

**Maya/Kiko**
> "Just... type my name and bio?"

**Mrs. Santos**
> "Not just 'type,' Rookie. Format. I want your Name in a professional standard. I want your Bio in Times New Roman. And for goodness sake, use proper sizing."

#### Active - Task Incomplete
**Mrs. Santos**
> "That's your station. Don't make me use the red pen on your first attempt. Go."

---

## Main Task: Formatting Skill
> ### 📘 Gameplay Tutorial: Font Face & Size
> Welcome to your first lesson in document formatting.
>
> In professional and academic writing, the visual presentation of your text is just as important as the content itself.
> * **Font Face** refers to the text's design. In academic writing, use traditional Serif fonts (e.g., Times New Roman) for printed essays, or clean Sans-serif fonts (e.g., Arial) for digital viewing. Always avoid informal novelty fonts.
> * **Font Size** establishes a clear visual hierarchy using points (pt). Standardize your main body text at 11pt or 12pt, use 14pt to 16pt for subheadings, and reserve 18pt or higher strictly for main titles.

### Objective
Create an Official Staff Profile.
* **The "Messy" Initial State:** Starts with `"Malaya/Francisco magbanua"` in tiny Comic Sans.
* **Player Goal:** Change the Font Face to a professional standard and increase the Font Size to make it readable.
* **The "Red Pen" Feedback (Failure Condition):** If the player tries to hit "PRINT" without following the rules, the Red Pen System triggers: *“Incorrect, Try Again.”* * **Resolution:** Once printed correctly, a clean, professionally presented profile slides out of the machine.

---

## Level Clear

```
[System Notification]:
LEVEL COMPLETE 
Skill Unlocked: Basic Formatting (Font Face & Size) 
```

#### Complete Task Dialogue
**Mrs. Santos**
> ”Hmmmm… I gotta say, not bad for a Rookie. Well done. I’m sure there will be more challenges for you to face.. This is just the beginning.”
