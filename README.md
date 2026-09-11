# Solar System — Interactive Atlas

Open `Assets/_Scenes/SolarSystem.unity` in Unity **6000.3.15f1** and press **Play**.

A complete interactive model with all eight planets, the Sun, Moon, and Pluto (labeled as a dwarf planet). Includes 2K surface textures, Earth clouds and night lights, Saturn and Uranus rings, elliptical inclined orbits, axial rotation, a Milky Way backdrop, asteroid and Kuiper belts, solar bloom, passing comets, and optional atmospheric audio.

## Controls

- Click a planet or its destination button to focus; keys **1–8** select Mercury through Neptune.
- **Right-drag** rotates the camera; **scroll** zooms.
- **Home / Escape** returns to the overview.
- **Space** pauses; the time slider adjusts orbital speed.
- **O** toggles orbit paths, **L** labels, and **M** audio.
- Click a world marker in the map to focus it.

The top-down second camera renders the map. Comets are capped at four, have curved flybys and anti-solar ion tails, and pause with the simulation. Audio is an artistic ambience, not sound propagating through a vacuum. The original project's `dronehum.aif` and `burning.aif` are reused; no new audio was downloaded.

## Model and scale

This is an educational visualization, not a date-specific ephemeris or N-body simulation. Planet order, approximate eccentricities, inclinations, tilts, and orbital period ratios follow astronomical data. Initial phases are composed for readability. Distances and radii are independently compressed/exaggerated to keep all worlds visible. Display orbit radii are 8, 11, 15, 21, 34, 46, 62, and 78 Unity units; actual distances and mean radii appear in the profiles.

Planet surface rotation is slowed 40 times relative to the orbital clock for readable textures, while retaining relative rotation periods and retrograde Venus/Uranus/Pluto. The Moon uses synchronous orbital rotation without that slowdown. Solar illumination is evaluated from the Sun's position in the surface shader, with a small artistic night-side fill. Rings include an approximate planetary shadow. Cloud maps are composited with the surface. Comet motion and belt densities are illustrative; comets use a separate visual flight clock.

Astronomical references: [JPL approximate orbital elements](https://ssd.jpl.nasa.gov/planets/approx_pos.html), [JPL physical parameters](https://ssd.jpl.nasa.gov/planets/phys_par.html).

## Editing and assets

Planet properties are editable on each `CelestialBody` component. Materials and the smooth shared sphere are saved in `Assets/SolarSystem/Materials`. Procedural rings, orbit paths, particles, and belts are generated when Play begins and cleaned up when it ends. The **Solar System** menu offers **Build Complete Model**, **Validate Model**, and **Capture Preview**. Rebuilding replaces the scene from the builder's data; make custom changes directly in the scene unless deliberately rebuilding.

The supplied scene is backed up at `Tools/OriginalScene/SolarSystem.unity`. No version control work is required. Validation output is written to `Tools/UnityValidation.txt`; captures go to `Screenshots` inside this project. No code added by this work writes preferences or files outside this project.

Downloaded textures are by Solar System Scope, licensed **CC BY 4.0**, acquired over certificate-validated HTTPS. Full attribution and exact source URLs: [texture credits](Assets/SolarSystem/Textures/SOURCES.md). Pluto uses the pre-existing project texture. Existing assets retain their original terms.
