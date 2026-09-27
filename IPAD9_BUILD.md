# MeloNX 2.5.3 iPad 9 test build

This fork applies a one-time low-memory preset on iPad12,1 and iPad12,2. It keeps
the normal app identifier, so TrollStore can install it over an existing MeloNX
installation without intentionally deleting games, keys, firmware, or saves.

The preset uses handheld mode, 0.75x resolution, Vulkan, bilinear scaling, no
anti-aliasing, shader cache off, async shader compilation off, texture
recompression on, safe host-mapped memory, backend threading off, and guest RAM
expansion off. The forced backend threading override was removed for iPad 9.
The app requests increased memory limit and extended virtual addressing at
signing time. These requests cannot increase the device's physical 3 GB RAM.

The preset is applied once to the global `Documents/config.json`. Existing
per-game configurations may still override it. The source and IPA can be built
by GitHub Actions in `.github/workflows/build-ipa.yml`; the IPA carries an ad hoc
signature with JIT and memory entitlements and must be installed through
TrollStore. The build reuses the prebuilt MeloNX 2.5 core from the upstream
release because its NuGet package feed is unavailable; the core IPA's SHA-256
is checked before extraction. The build only proves packaging and iOS
deployment version; gameplay compatibility requires device testing.

The 2.5.2 update made new per-game settings inherit the iPad 9 profile and
migrated earlier per-game settings to its safe memory mode once. Device testing
confirmed that 2.5.2 still reached iPadOS's approximately 1.94 GB resident
process limit while loading Super Mario 3D World + Bowser's Fury.

The 2.5.3 test gives that title a one-time per-game 0.5x resolution preset in
handheld mode, with anisotropic filtering and anti-aliasing disabled, bilinear
scaling, texture recompression enabled, and shader cache and asynchronous shader
compilation disabled. The preset is saved in the game's settings and can be
changed later. The app log records the applied Mario profile at launch. Lower
resolution is unsupported in some games and may cause a different crash; this
is a targeted memory test, not a claim that the title is confirmed playable on
a 3 GB iPad. No firmware, keys, games, or saves are included in the IPA.
