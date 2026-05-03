# Pokemon Storage

This was originally written in Python (https://github.com/maxoakes/pokemon-storage), but doing byte-level operations in a dynamically-typed language was a wild choice, so I am migrating to C# since I know it about as much as I do Python.

This program allows the transfer of Pokemon between generations 1-4, and to a local sqlite database. 

## Limitations

* Currently only supports main series generations 1, 2, 3 and 4.
* Currently the only supported database are local `.sqlite` databases
* Supported on Linux and Windows. I don't know how to test it on Mac, but if it works for you, that is great.

## Todo

* [BUG] Some sprites do not show up
* [BUG] Ribbons are not loading from games
* Remove unused info when showing Pokemon from older versions
* [BUG] Pokemon origin game version not loading correctly on some occasions, need to find source
* Make theme consistant and clean it up
* Add more/swappable themes
* Refactor everything, remove unused things, combine things that are similar
* Support other database types
* Support Generation 5
* Support Generation 6
* Support Generation 7
* Support Generation 8
* Support Generation 9

## AI Usage Disclosure

AI was used in the following capacities during development:
* Github Co-pilot autocomplete of repetitive lines and declarations
* ChatGPT used to convert the properties of the `PartyPokemon` class into HTML version of the right-side About panel, then the convertion of that HTML to `.axaml`. This has since been extensively reviewed and revised. 