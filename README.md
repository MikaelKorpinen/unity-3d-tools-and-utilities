# Targeting system plugin 1.0.
Plugin is under plugins folder.

Project contains plugin for getting closest targets.
This repo is made for demo purposes of the plugin and the full version will include much more in the future.
Such as multiple targeting systems support.

-Enhance user experience by giving tools and utilities to things like easy object picking from distance.
-Get data about entities/gameobjects and also to give data about their whereabouts to the user.

The first version is to include targeting system.
Targeting system is based on vector mathematics and can give information about gameobjects and entities for easy object location.
It also has object culling in frustum space, which means that the targeting system only processes stuff that happen inside its own area rather than at scene level.
