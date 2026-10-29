# Trial X — The First Five

A 2D trials prototype for Tuanjie 1.11 / Unity 2022.3 and the built-in render pipeline.

## Play

1. Open this project in Tuanjie.
2. Open `Assets/Scenes/Canyon.unity` and press Play.
3. Choose **Ride** beside Quiet Canyon. Finish it to unlock the next trail.

The scene generates its environment, motorcycle, camera and interface at runtime; Edit mode is intentionally empty. A landscape Game view is recommended. **Trial X → Create Trail Pack Scene** recreates the scene and registers it in Build Settings.

## The first five

| Track | Length | Focus |
| --- | --- | --- |
| Quiet Canyon | 56 m | Original canyon course and balance practice |
| Rolling Hills | 76 m | Flowing slopes and small takeoffs |
| Rock Garden | 66 m | Short rock steps and precise throttle |
| Broken Ridge | 88 m | Taller ramps, drops and landings |
| Summit Run | 104 m | Sustained climbs and elevated ridges |

Each trail has a distinct terrain profile, mountain layout, and palette. These courses use a continuous heightfield; takeoffs come from the crests and descending terrain. Completing a trail unlocks the next. Unlocked tracks can be replayed from the selector, and the finish screen offers **Next Trail**.

Best times (to hundredths of a second) and unlocks are saved locally with Unity's `PlayerPrefs`, under `trialx.unlocked` and `trialx.best.<track-id>`. A slower finish never overwrites a personal best. Each new attempt resets the ride timer; retries count toward the current track's attempt total. Pause and track selection freeze the run.

## Controls

- **W / Up**: throttle
- **S / Down**: brake
- **A / Left**: lean back
- **D / Right**: lean forward
- **R**: restart the current run
- **P / Escape**: pause or resume
- **M**: open or close the track selector
- Touch devices show controls supporting simultaneous throttle and balance input.

Balance input ramps gently, has a maximum rotation speed, and settles when released. Light bumps and brief helmet scrapes allow recovery. Hard head impacts, sustained helmet/body contact and falling below the track cause crashes. These handling settings are exposed on `BikeController` in the Inspector.

## Main files

- `TrialLevels.cs`: five track definitions, sampling, unlocks and saved times
- `TrialArt.cs`: palette, vector helpers and generated mesh cleanup
- `TrialWorld.cs`: automatic setup, themed scenery and parallax
- `TerrainGenerator.cs`: active track mesh, surface and trail markers
- `BikeController.cs`: suspension, input, artwork, crash logic and follow camera
- `GameManager.cs`: selector, HUD, touch input, results, pause and attempts

All artwork is generated from meshes and lines; no external assets or packages are required. To add another course, append a `TrialLevel` definition with a permanent unique save ID, increasing X coordinates, a flat start and a landing area beyond the finish.

## Verification

Run `python3 Tools/verify_tracks.py` on a Mac with Tuanjie installed at `/Applications/Tuanjie/Tuanjie.app`.

The check compiles all runtime/editor scripts against the installed assemblies. A separate harness uses the actual track/progress code with small engine shims to check continuity, safe starts/finishes, selection guards, sequential unlocks and isolated best times. It also exercises the controller's numerical physics methods with automated balance/throttle input to reach the finish on all five tracks.

Headless checks do not verify rendering, GUI events, disk persistence or the feel of riding. The editor's licensing client failed to connect during the earlier batch launch; interactive testing of the selector, track transitions and difficulty remains necessary.
