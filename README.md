# Tickle

A performant, yet lightweight and intuitive tween library for Unity. Optionally uses Unity's Job System to unlock even more performance!

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

> Tickle is not yet production-ready. We are looking for [contributors](##Support/Contribute)!

## Why "Tickle"

There are already many tween libraries out there, but we want to create a library that

- Has no bloat: offers only a light set of API for the most intuitive and common features found in most tween libraries - no obscure features that you will never use

- Does not contain compulsory external dependencies

- Is beautiful to read and easy to reason, even if at the low cost of very little GC allocation, and yet...

- ...could still be trusted for relatively high performance, which could optionally be made even better with Unity's Job System!

The name "Tickle" reflects our focus on improving developer experience by building something that is **"small, simple, fast"**.

## Getting Started

```c#
private Transform player;

private void Start()
{
    player.LerpPosition(
        start:      new Vector3(-5, 0, 0),
        end:        new Vector(5, 0, 0),
        duration:   5,
    )
    .OnComplete()
    .Start();
}

private void 
```

## To-dos

- Unit testing and QA
- Add more easing functions
- Implement the Sparse Set data structure
- Object pooling for near zero-allocation
- Benchmark performance against other tween libraries
- Create demo scenes

## Support / Contribute

Bug reports: "Issues"<br>
Questions and feature requests: "Discussions"

To become a contributor, feel free to contact me at tankangsoon@gmail.com
