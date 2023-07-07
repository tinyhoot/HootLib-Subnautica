# Tinyhoot's Common Utilities for Subnautica

This is a [shared project](https://learn.microsoft.com/en-us/xamarin/cross-platform/app-fundamentals/shared-projects) 
for very common things that I use across several of my mods, with some QoL classes to make modding easier.

Other authors in the modding community like to have all their mods under one repository for this reason. Personally,
I'm not a fan of those. Instead I use this repository as a git submodule. This also means that, should you for
some reason want to include these utils in your own project, you absolutely can (and you're welcome to!).
Since submodules are always pinned to a particular commit, there is no need to worry about updates. Every mod can
pick and choose the version of this shared project they need/want to use.

### Setup
Written down just as much for me to remember later as for anyone else to follow.

1. Open the terminal and navigate to the root folder of your project.
2. If you don't already have a repository for your mod, create a local one: `git init`
3. Add this repository as a submodule. `git submodule add https://github.com/tinyhoot/HootLib-Subnautica.git`
4. Git has now downloaded and set things up for you. Now you need to reference this shared project in your mod.
5. Click on your mod's _solution_ and choose `Add -> Existing Project...`
6. Choose `HootLib.shproj`
7. Click on your mod's _project_ and add a new reference to HootLib.
8. Done!
