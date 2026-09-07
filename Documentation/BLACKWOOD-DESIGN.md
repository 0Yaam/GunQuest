# Blackwood / First Signal

## Art direction

A damp conifer valley at first light. Cool grey air, olive undergrowth, dark wet bark, brown leaf litter, and a few warm practical lamps. Human structures are small and weathered. No asphalt avenue, shipping-crate arena, perimeter wall, luminous fantasy plants or geometric mountains.

The principal view is a narrow trail framed by mature trunks. Ground cover has three scales: leaf-litter terrain, fern clumps/saplings, then canopy. Photogrammetric mossy rocks and deadwood sit at erosion banks and path edges rather than being scattered across the combat route. Distant hills and tree layers close the horizon.

## Authored traversal

1. **Southern trailhead:** deployment and later extraction. Trail marker and abandoned barrier establish the quarantine boundary.
2. **Ranger checkpoint:** relay one, a shelter and the first evidence of the missing patrol. Open ground teaches movement and interaction.
3. **Creek crossing:** a narrow timber bridge with a shallow western ford as a flank. Water and banks separate the encounter spaces.
4. **Ranger station:** relay two, timber cabin, local supplies and a defensible clearing. The building is functional shelter, not a factory dropped into a forest.
5. **Signal ridge:** relay three beneath a communications mast. The visible mast provides a persistent landmark above the canopy.
6. **Return route:** extracted signal data must be carried back to the southern trailhead; a western footpath provides an alternate route.

Objective locations, spawn entrances, bridges and walkable route geometry are authored together and validated using complete navigation paths.

Collision is sized from the final mesh footprint, not just the source model's height. This matters for wide, flat scanned rocks: a height-scaled boulder initially extended into the bridge exit. A character-controller crossing test now covers both approaches and the exit without jumping, in addition to NavMesh reachability.

Terrain alpha maps are painted on the persistent TerrainData asset so the path survives save/reload. Terrain smoothness uses explicit constants instead of an opaque JPEG's alpha channel. Physical sign lettering is fitted to its board bounds. These checks prevent presentation regressions when regenerating the chapter.

## Campaign logic

**01 Blackwood — First Signal.** The ranger network went silent while transporting a research sample. The operator restores local relays to recover the patrol's transmission. Hostile armed units are a quarantine force ordered to prevent the data leaving the valley, consistent with the current soldier enemies. Radio logs point to the refinery where the sample was processed.

**02 Outpost — Chain of Custody.** The refinery's processing logs identify the covert transfer route. Securing the station yields the address of the transmitter controlling the blockade.

**03 Skyline — Dead Frequency.** The operator cuts the district's command transmitter and extracts the evidence. The three-map campaign therefore follows a traceable signal and supply chain, not unrelated arenas.

Creature variants, a boss encounter, additional weapons and inventory remain follow-up work after this environment milestone. The story does not claim these unimplemented systems already exist.

## Assets and regeneration

The mature firs are real geometry, with two tree variants and three LODs each derived from Poly Haven's CC0 Fir Tree 01. Ferns, pine saplings, mossy rocks, deadwood, forest litter and old planks also come from Poly Haven; source URLs are in `Assets/ThirdParty/ForestScans/LICENSE.txt`. Wind and creek rendering are native URP shaders, not animated background images.

`ForestChapterBuilder.Generate` rebuilds the authored forest and navigation, then updates chapter order, narrative and expedition HUD on all three scenes. It preserves Outpost and Skyline geometry. Regeneration replaces generated scene work, so save manual scene edits separately first.

`ForestValidation.Run` checks the saved trail paint, opening chapter, relay paths and physical bridge traversal. `RuntimeVisualCheck` runs only when the standalone executable receives `-gunquest-visual-check <output-directory>`; it checks deployment/menu actions and renders the real camera and UGUI to offscreen targets at the four vista positions. A blank-frame guard rejects failed captures. Offscreen rendering is necessary because a hidden Windows player does not present its swapchain. This is a visual smoke test, not a complete standalone campaign playthrough or a check of desktop presentation/input.
