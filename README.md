# Tickle

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) 
[![Unity](https://img.shields.io/badge/Unity-%23000000.svg?logo=unity&logoColor=white)](#) 
[![C#](https://custom-icon-badges.demolab.com/badge/C%23-%23239120.svg?logo=cshrp&logoColor=white)](#)
[![GitHub](https://img.shields.io/badge/GitHub-%23121011.svg?logo=github&logoColor=white)](#)


A performant, yet lightweight and intuitive tween library for Unity. Optionally uses Unity's Job System to unlock even more performance!

[Official Site](kstan.gitlab.io/tickle) | [Read Blog](kstan.gitlab.io/tickle-intro)

**Note: Tickle is not yet production-ready. We are looking for [contributors](##Support-and-Contribute)!**

## Why "Tickle"

The name "Tickle" reflects our focus on improving developer experience by building something that is **"small, simple, fast"**.

There are already many tween libraries out there, but we want to create a library that

- Has no bloat: offers only a light set of API for the most intuitive and common features found in most tween libraries - no obscure features that you will never use
- Does not contain compulsory external dependencies
- Is beautiful to read and easy to reason, even if at the low cost of very little GC allocation, and yet...
- ...could still be trusted for relatively high performance, which could optionally be made even better with Unity's Job System!

## Getting Started

### Installation
TODO: Instructions will be added to support the following installation methods:
- Package Manager
- OpenUPM
- AssetPackage

After installation, please make sure that you go to **"Project Settings" > "Player"**, and then enable **"Allow unsafe code"**.

### "Tickle" - Creating a simple lerp

A "Tickle" is a simple description of the linear interpolation (i.e. a "Lerp") of a single property.

```c#
Transform player;

// Creating a "Tickle"
ITickle tickle = player.LerpScale(start: 1, end: 2, duration: 3);
tickle.OnComplete(() => Debug.Log("Finished with scaling"));
tickle.Start();

// You can also declare these operations in a single line, like this:
tickle = player.LerpPosition(startPos, endPos, duration)
    .OnComplete(() => Debug.Log("DONE"))
    .Start();
```

### "TickleSet" - Creating a set of lerps

A "TickleSet" is a set of Tickles that run parallel to each other. To make it easy to reason, you may want to know that a TickleSet is simply an array of Tickles behind the scenes.

```c#
// Creating a TickleSet
var tickleSet = new TickleSet()
    .Join(player.LerpPosition(startPos, endPos, duration))
    .Join(player.LerpScale(startSize, endSize, duration));
tickleSet.OnComplete(() => Debug.Log("Finished with set"));
tickleSet.Start();

// You may also write it this way
tickleSet = player.LerpPosition(startPos, endPos, duration)
    .Join(player.LerpScale(startSize, endSize, duration))
    .OnComplete(() => Debug.Log("Finished with set"))
    .Start();

// Or this way, simply as an array of ITickles
ITickle[] tickleSet = new ITickle[] {
    player.LerpPosition(startPos, endPos, duration),
    player.LerpScale(startSize, endSize, duration)
};
tickleSet.OnComplete(() => Debug.Log("Finished with set"))
tickleSet.Start();
```
Just write it the way you think is clearest and prettiest to you!

### "TickleChain" - Creating a sequence of lerps

A "TickleChain" is a sequence of Tickles that run one after another. Behind the scene, it is just an array of TickleSets, or a 2D array of Tickles!

```c#
// Creating a TickleChain
var tickleChain = new TickleChain()
    .Chain(tickleSet)
    .Chain(player.LerpScale(startSize, endSize, duration))
    .OnComplete(() => Debug.Log("Finished with sequence"))
    .Start();

// You may also write it as a 2D array of ITickles
ITickle[][] tickleChain = new ITickle[][] {
    new ITickle[] { // TickleSet 1
        player.LerpPosition(startPos, endPos, duration),
        player.LerpScale(startSize, endSize, duration)
    },
    new ITickle[] { // TickleSet 2
        player.LerpPosition(endPos, startPos, duration),
        player.LerpScale(endSize, startSize, duration)
    }
}
.OnComplete(() => Debug.Log("Finished with sequence"))
.Start();
```
Again, choose the way to write it such that the sequence of events is the clearest to you.

### Enabling Unity's Burst + Job System
```
```

## To-dos

- Unit testing and QA
- Add tweening for more common properties
- Add more easing functions
- Implement the Sparse Set data structure
- Object pooling for near zero-allocation
- Benchmark performance against other tween libraries
- Create demo scenes

## Support and Contribute

- Bug reports: [Issues](https://github.com/ks-tan/Tickle/issues)
- Questions and feature requests: [Discussions](https://github.com/ks-tan/Tickle/discussions)
- To become a contributor, feel free to contact me at tankangsoon@gmail.com
