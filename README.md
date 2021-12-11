###### Plugin provides targeting capabilities for anyone interested in getting some speed in development of new content.
###### Copyright © 2020-2021 Mikael Korpinen(Finland). All Rights Reserved.
###### GPL 3.0

# Targeting system plugin 1.0.
Plugin is under plugins folder.


Project contains plugin for getting closest targets.
This repo is made for demo purposes of the plugin and the full version will include much more in the future.
Such as multiple targeting systems support. 

##Why targeting system? What is the proble it is solving?

If you play or have played games you might know aim assist in fps games. Aim assist provides a way to get the opponent or enemy closest to the cursor.
There are many ways to achieve this, but the targeting system 1.0 uses the most accurate method I know. Vector 3 projection and distance comparison in 3d-space.
Other way to do this kind of thing would be to project point to screen and measure distance to middle of the screen in flat sceen fps games. This only works in some cases,
but in case where the closest point needs to be measured for example form a users hand it stops providing accurate result. For this reason the targeting system 1.0 provides the best and performant way to get the closest target.

##Basic usage
Move plugin to your project folder. Add component called GeometricVision to your game object. The geometric vision component will then initialize the system.
After that you can use the familiar unity way.
```csharp
###gameObject.GetComponen<GeometricVision>.GetClosestTarget();
```


-Enhance user experience by giving tools and utilities to things like easy object picking from distance.
-Get data about entities/gameobjects and also to give data about their whereabouts to the user.

The first version is to include targeting system.
Targeting system is based on vector mathematics and can give information about gameobjects and entities for easy object location.
It also has object culling in frustum space, which means that the targeting system only processes stuff that happen inside its own area rather than at scene level.
