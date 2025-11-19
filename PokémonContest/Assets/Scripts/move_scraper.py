import os, re

raw_moves = '''Absorb 			  	Quite an appealing move.
Acid 			  	Badly startles Pokémon that the audience has high expectations of.
Ally Switch 			  	Scrambles the order in which Pokémon will move on the next turn.
Aromatherapy 			  	Prevents the user from being startled until the turn ends.
Assurance 			  	Works better the later it is used in a turn.
Attack Order 			  	An appealing move that can be used repeatedly without boring the audience.
Beat Up 			  	Works well if the user is pumped up.
Calm Mind 			  	Gets the Pokémon pumped up. Helps prevent nervousness, too.
Camouflage 			  	Shows off the Pokémonâ€™s appeal about as well as all the moves before it this turn.
Charge 			  	Gets the Pokémon pumped up. Helps prevent nervousness, too.
Confuse Ray 			  	Badly startles Pokémon that the audience has high expectations of.
Confusion 			  	Quite an appealing move.
Crafty Shield 			  	Excites the audience a lot if used first.
Dark Void 			  	Makes the remaining Pokémon nervous.
Defend Order 			  	Prevents the user from being startled one time this turn.
Destiny Bond 			  	A move of huge appeal, but using it prevents the user from taking further contest moves.
Disable 			  	Makes the remaining Pokémon nervous.
Dream Eater 			  	Works well if it is the same type as the move used by the last Pokémon.
Eerie Impulse 				Badly startles the last Pokémon to act before the user.
Electric Terrain 			  	Excites the audience a lot if used first.
Electrify 				Badly startles Pokémon that used a move of the same type.
Embargo 			  	Brings down the energy of any Pokémon that have already used a move this turn.
Fairy Lock 			  	Temporarily stops the crowd from growing excited.
Feint 			  	Causes the user to move earlier on the next turn.
Feint Attack 			  	Works great if the user goes first this turn.
Flatter 			  	Makes the remaining Pokémon nervous.
Fly 			  	Prevents the user from being startled one time this turn.
Foresight 				Badly startles Pokémon that used a move of the same type.
Forest's Curse 				Works better the more the crowd is excited.
Foul Play 			  	Shows off the Pokémonâ€™s appeal about as well as the move used just before it.
Future Sight 			  	Works well if it is the same type as the move used by the last Pokémon.
Gear Grind 			  	Quite an appealing move.
Giga Drain 				Badly startles the last Pokémon to act before the user.
Grass Whistle 			  	Prevents the user from being startled one time this turn.
Gravity 			  	Makes the remaining Pokémon nervous.
Guard Split 			  	Shows off the Pokémonâ€™s appeal about as well as all the moves before it this turn.
Guard Swap 			  	Shows off the Pokémonâ€™s appeal about as well as all the moves before it this turn.
Gust 				Badly startles the last Pokémon to act before the user.
Heal Block 			  	Temporarily stops the crowd from growing excited.
Heal Order 				Badly startles Pokémon that used a move of the same type.
Heart Swap 			  	Shows off the Pokémonâ€™s appeal about as well as all the moves before it this turn.
Helping Hand 			  	Quite an appealing move.
Hex 				Badly startles Pokémon that used a move of the same type.
Hidden Power 			  	An appealing move that can be used repeatedly without boring the audience.
Hyperspace Hole 			  	Excites the audience a lot if used first.
Hypnosis 				Startles all of the Pokémon to act before the user.
Imprison 			  	Temporarily stops the crowd from growing excited.
Ingrain 			  	Gets the Pokémon pumped up. Helps prevent nervousness, too.
Kinesis 			  	An appealing move that can be used repeatedly without boring the audience.
Knock Off 				Badly startles all of the Pokémon to act before the user.
Leech Life 			  	Shows off the Pokémonâ€™s appeal about as well as the move used just before it.
Leech Seed 			  	Gets the Pokémon pumped up. Helps prevent nervousness, too.
Lock-On 			  	Causes the user to move earlier on the next turn.
Low Sweep 				Prevents the user from being startled until the turn ends.
Luster Purge 				Badly startles Pokémon that used a move of the same type.
Magic Room 			  	Temporarily stops the crowd from growing excited.
Magnet Rise 			  	Makes the remaining Pokémon nervous.
Magnetic Flux 			  	Gets the Pokémon pumped up. Helps prevent nervousness, too.
Me First 			  	Causes the user to move earlier on the next turn.
Mega Drain 				Startles the last Pokémon to act before the user.
Metal Sound 				Startles all of the Pokémon to act before the user.
Mind Reader 			  	Causes the user to move earlier on the next turn.
Miracle Eye 			  	Works great if the user goes first this turn.
Mirror Move 			  	Shows off the Pokémonâ€™s appeal about as well as the move used just before it.
Mist Ball 				Badly startles all Pokémon that successfully showed their appeal.
Nasty Plot 			  	Gets the Pokémon pumped up. Helps prevent nervousness, too.
Natural Gift 			  	Works better the more the crowd is excited.
Needle Arm 			  	Quite an appealing move.
Night Shade 			  	An appealing move that can be used repeatedly without boring the audience.
Nightmare 				Startles all of the Pokémon to act before the user.
Odor Sleuth 			  	Prevents the user from being startled one time this turn.
Pain Split 			  	Shows off the Pokémonâ€™s appeal about as well as all the moves before it this turn.
Parabolic Charge 			  	Shows off the Pokémonâ€™s appeal about as well as all the moves before it this turn.
Pay Day 			  	Excites the audience in any kind of contest.
Poison Fang 			  	Quite an appealing move.
Poison Gas 			  	Makes audience expect little of other contestants.
Poison Powder 			  	Brings down the energy of any Pokémon that have already used a move this turn.
Poison Sting 				Startles the last Pokémon to act before the user.
Poison Tail 			  	Brings down the energy of any Pokémon that have already used a move this turn.
Powder 			  	Makes the audience quickly grow bored when an appeal move has little effect.
Power Split 			  	Shows off the Pokémonâ€™s appeal about as well as all the moves before it this turn.
Power Swap 			  	Shows off the Pokémonâ€™s appeal about as well as all the moves before it this turn.
Power Trick 			  	Works well if it is the same type as the move used by the last Pokémon.
Psych Up 			  	Works well if it is the same type as the move used by the last Pokémon.
Psychic 			  	Quite an appealing move.
Psycho Boost 			  	A very appealing move, but after using this move, the user is more easily startled.
Psycho Shift 			  	Works great if the user goes last this turn.
Psywave 			  	Effectiveness varies depending on when it is used.
Pursuit 				Badly startles Pokémon that used a move of the same type.
Quash 			  	Causes the user to move earlier on the next turn.
Rage Powder 			  	Temporarily stops the crowd from growing excited.
Recover 			  	Works well if it is the same type as the move used by the last Pokémon.
Recycle 			  	Shows off the Pokémonâ€™s appeal about as well as the move used just before it.
Reflect 			  	Prevents the user from being startled one time this turn.
Reflect Type 			  	Works well if it is the same type as the move used by the last Pokémon.
Rock Tomb 			  	Makes audience expect little of other contestants.
Roost 			  	Makes the audience quickly grow bored when an appeal move has little effect.
Sand Tomb 			  	Temporarily stops the crowd from growing excited.
Screech 			  	Makes audience expect little of other contestants.
Secret Power 			  	Works well if the user is pumped up.
Shadow Ball 			  	Quite an appealing move.
Shadow Punch 			  	Works great if the user goes first this turn.
Shadow Sneak 			  	Causes the user to move earlier on the next turn.
Shift Gear 			  	Gets the Pokémon pumped up. Helps prevent nervousness, too.
Sketch 			  	Shows off the Pokémonâ€™s appeal about as well as the move used just before it.
Skill Swap 			  	Shows off the Pokémonâ€™s appeal about as well as the move used just before it.
Sleep Powder 				Startles all of the Pokémon to act before the user.
Smokescreen 				Prevents the user from being startled one time this turn.
Snatch 			  	Shows off the Pokémonâ€™s appeal about as well as the move used just before it.
Spider Web 			  	Makes the remaining Pokémon nervous.
Spikes 			  	Makes the remaining Pokémon nervous.
Stored Power 			  	Works well if the user is pumped up.
String Shot 				Startles the last Pokémon to act before the user.
Stun Spore 				Badly startles all Pokémon that successfully showed their appeal.
Sucker Punch 			  	Excites the audience a lot if used first.
Supersonic 			  	Makes audience expect little of other contestants.
Switcheroo 				Badly startles Pokémon that used a move of the same type.
Synchronoise 			  	Works well if it is the same type as the move used by the last Pokémon.
Synthesis 			  	Effectiveness varies depending on when it is used.
Taunt 				Badly startles Pokémon that the audience has high expectations of.
Telekinesis 			  	Makes the remaining Pokémon nervous.
Topsy-Turvy 			  	Scrambles the order in which Pokémon will move on the next turn.
Toxic 			  	Brings down the energy of any Pokémon that have already used a move this turn.
Toxic Spikes 			  	Makes the remaining Pokémon nervous.
Transform 			  	An appealing move that can be used repeatedly without boring the audience.
Trick 				Badly startles Pokémon that used a move of the same type.
Trick Room 			  	Scrambles the order in which Pokémon will move on the next turn.
Venom Drench 			  	Brings down the energy of any Pokémon that have already used a move this turn.
Whirlwind 			  	Causes the user to move later on the next turn.
Wonder Room 			  	Scrambles the order in which Pokémon will move on the next turn.
Worry Seed 			  	Makes the remaining Pokémon nervous.
Zen Headbutt 			  	Quite an appealing move.'''

moves_dictionary = {}
total_moves = raw_moves.split("\n")

for move in total_moves:
    if not move.strip():
        continue

    # Split on FIRST occurrence of 2+ spaces/tabs
    parts = re.split(r'\s{2,}', move.strip(), maxsplit=1)

    if len(parts) == 2:
        name, desc = parts
        moves_dictionary[name] = desc
    else:
        print("Skipping (couldn't parse):", move)

# Write them to a text file
with open('/home/xommon/Documents/GitHub/PokemonContest/PokémonContest/Assets/Scripts/moves.txt', 'w') as file:
    for move, desc in moves_dictionary.items():
        if desc == "Quite an appealing move.":
            file.write(f'{move}|4|0|0|{desc}')
        elif desc == "Badly startles Pokémon that the audience has high expectations of.":
            file.write(f'{move}|2|1|0|Badly startles the Pokémon that performed first.')

