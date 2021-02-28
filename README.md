# FreeIva

Work in progress mod for Kerbal Space Program which allows getting out of your seat and moving around the vessel while on IVA.

## Controls
These can be customised by editing FreeIva\settings.cfg.
Keys | Action | Setting Name
---- | ---- | ----
Y | Get out of seat (unbuckle) while on IVA | UnbuckleKey
W | Move forward | ForwardKey
S | Move backward | BackwardKey
A | Strafe left | LeftKey
D | Strafe right | RightKey
Space | Jump | JumpKey
Q | Roll counterclockwise (when in low gravity) | RollCCWKey
E | Roll clockwise (when in low gravity) | RollCWKey
Left Shift | Move upwards (when in low gravity) | UpKey
Ctrl | Move downwards (when in low gravity) | DownKey
F | Open or close a hatch that you're looking at | OpenHatchKey
Alt + F | When two hatches are connected together, open or close the furthest hatch | ModifierKey + OpenHatchKey
Alt + Y | Lock or unlock mouselook. Allows you to move the mouse without moving the camera view, e.g. for clicking on UI items. | ModifierKey + UnbuckleKey

## For part modders
Checklist for setting up a part for use with FreeIVA:
- [X] Hatches are modelled separately to the IVA mesh. These can be meshes or props, and can contain animations and interactive triggers, e.g. clicking a handle to swing the hatch open.
- [X] IVA mesh has holes modelled to see out of when the hatches are open. These should include the door frame running from the IVA to where it meets the external part. The door frame can be a separate mesh or prop that is unhidden when the hatch is open.
- [X] IVA has colliders on the InternalSpace layer (20). The entire IVA should be enclosed in colliders to prevent players from falling out of the part. Hatch parts can have moveable colliders, or can be set up to disable the collider when the hatch is open.
- [X] IVA has a mask mesh hiding non-transparent walls from being seen through from the outside. This is similar to the stock cutaway view, but without the actual cutaway. Only windows and hatches should be transparent here.
- [X] Mask mesh for hatches. When this is enabled, only the external part mesh will be seen. When it is disabled, the player can see into the part while outside it, through the hatch hole in the previous step.
