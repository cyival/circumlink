# Level Generation

Level Generation is controlled by a level generator. All the level generation logic is encapsulated in this class.

By starting, it will load the level generation configuration and level scenes.

When starting a new play, the level generator will check through all the levels which are available for spawning and select one at random. By default, the level generator will not use a same level as spawning point from the previous play.

The generation is dynamically run when player entered the next level.

## Policy

Policy is used to control the level generation process. It defines which levels are available for spawning and how they are selected. It is defined in the level manifest.

`<policy>[:<id>]` or `<policy>[#<tag>]`

e.g. `once`, `once:level_1`, `once#levels`

Some policies cannot be used with tags or ids.

## Tags

Tags are used to categorize levels and control their spawning behavior. It is defined in the level manifest.

## Direction

Direction is used to control the direction when generating level. It is defined in the level manifest.

----

## Enemy Spawning

Level generator will automatically determine whether to spawn enemies or not. It is based on previous level generation results and current player state (health, for example).

Basically, these are meters for determining when to spawn enemies:

- Previous level generation results. When the spawned enemies count reaches a certain threshold, next level will not spawn enemies.
- Current player state.
  - Health. When the player's health reaches a certain threshold, enemies will not be spawned.
- Player spawn. If the level is used for player spawn, enemies will not be spawned.

Enemies will be spawned when the player enter level.

## Related class
