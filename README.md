# Tickle

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/ks-tan/Tickle/blob/main/LICENSE)
[![Unity](https://img.shields.io/badge/Unity-%23000000.svg?logo=unity&logoColor=white)](#) 
[![C#](https://custom-icon-badges.demolab.com/badge/C%23-%23239120.svg?logo=cshrp&logoColor=white)](#)
[![GitHub](https://img.shields.io/badge/GitHub-%23121011.svg?logo=github&logoColor=white)](#)


A performant, lightweight and intuitive tween library for Unity. Optionally uses Unity's Job System to unlock even more performance!

[Documentation](kstan.gitlab.io/tickle) | [Read Blog](kstan.gitlab.io/tickle-intro)

**Note: Tickle is not yet production-ready. We are looking for [contributors](##Support-and-Contribute)!**

## ✔️ Why "Tickle"

The name "Tickle" reflects our focus on improving developer experience by building something that is **"small, simple, and fast"**.

```C++
transform.LerpPosition(start, end, duration).Start();
```

There are already many tween libraries out there, but we want to create a library that

- Offers only a light set of API for the most intuitive and common features, such that it can be easily extended/customized to your own needs
- Does not contain compulsory external dependencies
- Is beautiful to read and easy to reason, even if at the low cost of very little GC allocation, and yet...
- ...could still be trusted for relatively high performance, which could optionally be made even better with Unity's Job System!

You may read more about the API design decisions and technical implementation details in the [blog](kstan.gitlab.io/tickle-intro).


## 📖 Getting Started

You may read the complete documentation **[here](kstan.gitlab.io/tickle)**.

### 💻 Installation

Simply download the latest released Unity Asset Package [here](https://github.com/ks-tan/Tickle/releases/).

After installation, please make sure that you go to **"Project Settings" > "Player" > "Other Settings**, and then enable **"Allow unsafe code"**.

> We will support installation via [OpenUPM](https://openupm.com/) and perhaps other methods once we have arrived at a stable first version (i.e., v1.0.0).

### 🕺 "Tickle" - Creating a simple lerp

A "Tickle" is a simple description of the linear interpolation (i.e. a "Lerp") of a single property.

```c++
ITickle tickle = transform.LerpScale(start: 1, end: 2, duration: 3);
tickle.OnComplete(() => Debug.Log("Finished with scaling"));
tickle.Start();
// Also supports tickle.Stop()/Pause()/Resume()
```

You may also chain together your method calls like this:

```C++
ITickle tickle = transform
    .LerpScale(start, end, duration)
    .OnComplete(() => Debug.Log("Finished with scaling"))
    .Start();
```

### 🕺💃 "TickleSet" - Creating a set of lerps

A "TickleSet" is a set of Tickles that run parallel to each other. Intuitively, a TickleSet is simply an array of Tickles.

```c#
// An array of Tickles simply makes a "TickleSet"
ITickle[] tickleSet = new ITickle[] {
    transform.LerpPosition(startPos, endPos, duration),
    transform.LerpScale(startSize, endSize, duration)
};
tickleSet.OnComplete(() => Debug.Log("Finished with set"))
tickleSet.Start();
```

Or if it is clearer, you may explicitly declare a TickleSet object.

```C++
var tickleSet = new TickleSet()
    .Join(transform.LerpPosition(startPos, endPos, duration))
    .Join(transform.LerpScale(startSize, endSize, duration));
    .OnComplete(() => Debug.Log("Finished with set"));
    .Start();
```

### 🔗 "TickleChain" - Creating a sequence of lerps

A "TickleChain" is a sequence of Tickles that run one after another. Intuitively, it is an array of TickleSets, or a 2D array of Tickles!

```c++
// Creating a TickleChain
ITickle[][] tickleChain = new ITickle[][]
{
    new ITickle[] { // TickleSet 1
        transform.LerpPosition(startPos, endPos, duration),
        transform.LerpScale(startSize, endSize, duration)
    },
    new ITickle[] { // TickleSet 2
        transform.LerpPosition(endPos, startPos, duration),
        transform.LerpScale(endSize, startSize, duration)
    }
}
.OnComplete(() => Debug.Log("Finished with sequence"))
.Start();
```

If you so choose, you may also explicitly declare a TickleChain object. You can chain together both TickleSets as well as plain Tickles.

```c++
var tickleChain = new TickleChain()
    .Chain(tickleSet)
    .Chain(transform.LerpScale(startSize, endSize, duration))
    .OnComplete(() => Debug.Log("Finished with sequence"))
    .Start();
```

Again, you are encouraged to write this the way you find to be the clearest/prettiest to you.

### 📈 Easing functions

Tickle supports a limited set of easing functions. Here is an example:

```c++
transform.LerpPosition(start, end, duration, Ease.InQuad);
```

We are currently working to support even more easing functions, as well as the ability for users to define their own. 

### ⚡ Enabling Unity's Burst + Job System

If you'd like to tap on Unity's Burst compiler and Jobs system for high performance tweening of an extremely large number of objects, you may install the Burst package from Unity's Package Manager.

After that, please enter "ENABLE_BURST" in "Project Settings" > "Player" > "Other Settings" > "Scripting Define Symbols".

### 🛠️ Want customizability / even more performance?

Tickle is actually a wrapper around a highly-performant custom lerp library that features zero-allocation and no boxing, and works with contiguous memory to avoid cache misses. In comparison, Tickle introduces just slight overhead in order to provide QoL features such as sequencing tweens (via "TickleChains") and supporting action delegates (i.e., "onComplete" callbacks).

If you wish to squeeze even more performance, or you'd like to implement your own wrapper around the lerp library, you may skip using the Tickle wrapper and work directly with the lerp library instead.

## Showcase

We are interested in showcasing projects that use Tickle! Please get in touch with me at tankangsoon@gmail.com

## Support and Contribute

- Bug reports: [Issues](https://github.com/ks-tan/Tickle/issues)
- Questions and feature requests: [Discussions](https://github.com/ks-tan/Tickle/discussions)
- To become a contributor, feel free to contact me at tankangsoon@gmail.com

For contributors, please refer to the [Discussions](https://github.com/ks-tan/Tickle/discussions) page for a list of potential tasks you could work on. Thank you.