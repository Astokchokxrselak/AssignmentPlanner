import random
from pprint import pprint, pformat


LETTERS = ['a','e','i','o','u','y']
C = 3
OPERATORS = [lambda a, b: a**2 + b + C]


def next_operator(sym, total, i):
    return OPERATORS[i % len(OPERATORS)](total, LETTERS.index(sym) + 1 + i)


def get_spell_id(string):
    total = 1
    for i, c in enumerate(string):
        total = next_operator(c, total, i)
    return total


def list_letters():
    return LETTERS[:]


def concatenate_all(lis, letter):
    print(lis)
    return [L + letter for L in lis]


def get_all_spells_len(length):
    letters = list_letters()  # aeiou
    for i in range(length - 1):
        temp = letters[:]
        for letter in LETTERS:
            letters.extend([l + letter for l in temp])
    return set(letters)

max_len = 2
spells = get_all_spells_len(max_len)
ids = {}

for spell in spells:
    id = get_spell_id(spell)
    if id in ids:
        raise AssertionError("The id ", id, " for ", spell, "is already assigned to the spell", ids[id])
    ids[id] = spell

print("We have the perfect function!")


def output_ids(ids):
    print("Spells by ID")
    for id, spell in ids.items():
        print(f"{spell} ({id})")


random = random.choice(list(spells))
print("Example: ", random, " is spell", get_spell_id(random))
input()
output_ids(ids)
