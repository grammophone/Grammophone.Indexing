# Grammophone.Indexing
This .NET library offers:
* Generalized edit distance functionality, for any type of object in place of characters, using arbitrary distance metrics.
* Various forms of a generalized radix tree. The trees are generalized in the sense that they use any object type as a character, and they can contain more than one items along with optional user-defined extra data per item stored. Items will be mentioned as 'words' hereon.
* Combination of the above to offer approximate search.
* Implementation of the "all-substrings" kernel, see below.

Edit distance computations between sequences of generic elements of type `C` are offered via the `DynamicMatrix` class, especially through its static methods. These can be used directly, but they are also used by the tree implementations to provide approximate search.

The concrete implementations of the trees spawn from a common abstract ancestor, the `RadixTree`. The following UML diagram summarizes the class hierarchy of the trees.

![Trees class diagram](http://s11.postimg.org/xgfhyajab/Indexing_Class_Diagram.png)

The generic argument `C` is the type of the character, `D` is the type of the user data stored per indexed word, and `N` is the type of data stored per node of the tree. These types can be full-blown objects on the heap or just value types, like primitives such as char.

The `WordTree` subclass indexes whole words as they are added to it. In contrast, the `SuffixTree` contains all the suffixes of the words added to it. The suffix tree is built in O(n) time where n is the sum of the lengths of all words added to it using [Ukkonen's method (1995)](http://www.cs.helsinki.fi/u/ukkonen/SuffixT1.pdf). Any type `C` offering `HashCode` and `Equals` methods of O(1) complexity preserves the tree's O(n) build time.

A subclass of the `SuffixTree`, the `KernelSuffixTree` can compute the sum of the weighted kernels of a given word against all words indexed in the tree in O(k) time, where k is just the size of the given word, irrelevantly from the number and size of words stored in the tree. It uses the method described by [Vishwanathan and Smola (2004)](http://www.stat.purdue.edu/~vishy/papers/VisSmo04.pdf). This weighted sum of kernels has the form specified by the representer theorem, thus it is applicable to kernel methods directly:

![Representer theorem](http://s14.postimg.org/z3v4bhvip/representer.png)

The trees inherit from `RadixTree` various methods for exact and approximate search of their items or their prefixes. Thus, for `WordTree`, this translates to whole words and prefixes, and for `SuffixTree`, it translates to suffixes and substrings. Approximate searches are accomplished via generalized edit distance, using a user-supplied character replacement cost function.

A simple example project is also [available](https://github.com/grammophone/Grammophone.Indexing.Test) to demonstrate the usage of the various tree types using the `char` as character type `C`.

This library has no dependencies.
