MVP
Brittany
Elfi
Must Haves:
Battle background sprite (forest area)
HUD:
TurnOrder UI (showcasing next ~10 turns at side of screen, symbol for each character)
Should show on the left.
https://discord.com/channels/@me/1414726935430303897/1447307508346781782
Character UI (left lower corner of screen): Character Name or icon to indicate who’s the player, Health Bar, MP Bar, space for status effects on the right side of MP in the turn order UI
Skills UI: Skills selection UI, display of skills
Action to button inspiration here: https://discord.com/channels/@me/1414726935430303897/1447309566001020948
The actual UI will potentially be a simple table, but @elfikhndorf@yahoo.de will play around.
Damage number display (close to effected character)
Number display can be just white with black outline, displayed near the character
If its easy we can add in font size for the amount of damage has been done (e.g. large damage = large font, small damage = smaller font)
https://discord.com/channels/@me/1414726935430303897/1447310867715850251
https://discord.com/channels/@me/1414726935430303897/1447311078039093350
Victory and Defeat screen
Overlay of Victory: congrats you won. Super simple text. Black transparent layer.

Indicators for Status effects (symbol):
Poison (purple)
Sleep (zzz), Sneeze
Accuracy down (crosshare symbol)
Flower Trap (flower symbol)
Shield of Leaves (shield or leaf symbol)
Taunt (middle finger or megaphone)
Heartwood Pulse (thorn absorbing half of incoming dmg/healing, could be magnet symbol)
Defense up (shield with arrow upwards or plus symbol)

Code functionality
Battle State Tracker
Turn Order Calculation
Status Effects Tracker
Player Turn: check if alive, check status effect consequences and turn change, enable choosing skill and choosing target
Enemy Turn: check if alive, check status effect consequences and turn change, choose random skill of boss
Skill Execution: play sound effect/animations (e.g. attack, getting hit), display damage number, change HP and MP UI values, calculate State changes
Character Skills functionality - Brittany Gibbons to create high level architecture in order to get the foundation of this work, then collaborate to execute the remainder of the work.
Calculation of damage based on multiplier, action taken, and when it needs to be executed. Area of attack.
Alder Finch: Fire Basic Attack, Bramble Bind, Echo Verse, Hunters Mark, Hail of Arrows
Miri Quickroot: Wind Basic Attack, Hayfever Haze, Herbal Remedy, Bloom Burst
Thorn of Liria: Verdant Aegis, Runebark Resonance, Heartwood Pulse

Three playable character sprite sheets
Animations:
Attack
Getting Damage
Cast Spell
Death
Getting healed

Boss character sprite sheet and skills
Animations: Attack, Receiving damage, Area Attack (Spore Burst), Death, Getting healed, Thorn armour animation
Different colored states (indicate elemental weakness)
Small light minions:
attack, damage, death, healing animation

Should Haves:
Character Animations:
Status animations: Poison, Sleep, Sneeze, Accuracy down, Flower Trap, Shield of Leaves, Taunt, Heartwood Pulse (thorn absorbing half of incoming dmg/healing), Defense up
Area attack indicator (could be red circle)

Boss Animations:
Entry Animation, root attack animation (Snare zones, character in it slowed down)

Boss Battle Music
Sound effects:
Damage (boss, character)
Healing
Attack
Magical Attack (could be different ones for each attack)
Bramble Bind (Alder)
Echo Verse (Alder)
Hunters Mark (Alder)
Hail of Arrows (Alder)
Hayfever Haze (Miri)
Herbal Remedy (Miri)
Bloom Burst (Miri)
Verdant Aegis (Thorn)
Runebark Resonance (Thorn)
Heartwood Pulse (Thorn)
Root Swipe (Boss)
Spore Burst (Boss)
Ancient Reflex (Boss)

Nice to Haves:

Narrative Entry for boss fight (scene with dialogue as introduction to battle
Narrative Exit for Battle → giving choice for mercy
Battle Phase 2 (can shape work points for it once we get there)
Victory screen: Very optional summary: Number of hits, how much health did characters, number of turns, did you defeat the boss?
Give a rating later on.
