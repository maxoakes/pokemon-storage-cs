# Pokemon Storage

This program allows the extraction and insertion of Pokemon to and from Pokemon main series generations 1 though 4.

It uses a sqlite backend to store Pokemon. I/O is done by keeping the Pokemon in memory using a standard format. When writing back to save files, a byte array is created using version-appropriate standards. Pokedex entries are also modified as appropriate.

![Sample](https://raw.githubusercontent.com/maxoakes/pokemon-storage-cs/refs/heads/main/preview.png)

This was originally written in Python (https://github.com/maxoakes/pokemon-storage), but doing byte-level operations in a dynamically-typed language was a wild choice, so I migrated to C# since it is statically-typed and I know it more than Python.

## Limitations

* Currently only supports main series generations 1, 2, 3 and 4.
* Currently the only supported database are local `.sqlite` databases
* Supported on Linux and Windows. Developed and tested only on Linux. I don't know how to test it on Mac, but if it works for you, that is great.

## Todo

* [BUG] Gen 4 Johto Pokemon report they were met at 'Faraway Place'
* Add feature to delete and/or replace Pokemon from database (and save files?)
* Remove unused info when showing Pokemon from older versions
* Make theme consistant and clean it up
* Add more/swappable themes
* Refactor everything, remove unused things, combine things that are similar
* Support Generation 5
* Support Generation 6
* Support Generation 7
* Support Generation 8
* Support Generation 9
