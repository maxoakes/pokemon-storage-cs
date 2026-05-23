# Pokemon Storage

This was originally written in Python (https://github.com/maxoakes/pokemon-storage), but doing byte-level operations in a dynamically-typed language was a wild choice, so I migrated to C# since it is statically-typed and I know it more than Python.

This program allows the transfer of Pokemon between generations 1-4, and to a local sqlite database. 

## Limitations

* Currently only supports main series generations 1, 2, 3 and 4.
* Currently the only supported database are local `.sqlite` databases
* Supported on Linux and Windows. I don't know how to test it on Mac, but if it works for you, that is great.

## Todo

* Remove or make generic unused info when showing Pokemon from older versions
* Make theme consistant and clean it up
* Add more/swappable themes
* Refactor everything, remove unused things, combine things that are similar
* Support other database types
* Support Generation 5
* Support Generation 6
* Support Generation 7
* Support Generation 8
* Support Generation 9
