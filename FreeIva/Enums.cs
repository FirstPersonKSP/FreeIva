namespace FreeIva
{
    // Stock collision matrix (X = collisions enabled)
    /*
                                1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
            0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        0	X X X X X _ X X _ _ _ _ _ _ _ X _ X _ X _ _ _ X _ _ X X _ _ _ _ 
        1	X X X X X _ X X _ _ _ _ _ _ _ X _ X _ _ _ _ _ X _ _ X X _ _ _ _ 
        2	X X X X X _ X X _ _ _ _ _ _ _ X _ X _ _ _ _ _ X _ _ X X _ _ _ _ 
        3	X X X X X X X X X X X X X X X X X X X X X X X X X X X X X X X X 
        4	X X X X X _ X X _ _ _ _ _ _ _ X _ X _ _ _ _ _ X _ _ X X _ _ _ _ 
        5	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        6	X X X X X X X X X X X X X X X X X X X X X X X X X X X X X X X X 
        7	X X X X X X X X X X X X X X X X X X X X X X X X X X X X X X X X 
        8	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        9	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        10	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        11	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        12	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        13	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        14	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        15	X X X X X _ X X _ _ _ _ _ _ _ X _ X _ X _ _ _ X _ _ X X X _ _ _ 
        16	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ X _ _ _ _ _ _ _ _ _ _ _ 
        17	X X X X X _ X X _ _ _ _ _ _ _ X _ X _ X _ _ _ X _ _ _ _ _ _ _ _ 
        18	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        19	X _ _ X _ _ X X _ _ _ _ _ _ _ X _ X _ X _ _ _ X _ _ X X _ _ _ _ 
        20	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ X _ _ _ X _ _ _ _ _ _ _ _ _ _ _ 
        21	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ X _ _ _ _ _ _ _ _ _ _ 
        22	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        23	X X X X X _ X X _ _ _ _ _ _ _ X _ X _ X _ _ _ X _ _ X X _ _ _ _ 
        24	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        25	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        26	X X X X X _ X X _ _ _ _ _ _ _ X _ _ _ X _ _ _ X _ _ X _ _ _ _ _ 
        27	X X X X X _ X X _ _ _ _ _ _ _ X _ _ _ X _ _ _ X _ _ _ _ _ _ _ _ 
        28	_ _ _ X _ _ X X _ _ _ _ _ _ _ X _ _ _ _ _ _ _ _ _ _ _ _ X _ _ _ 
        29	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        30	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
        31	_ _ _ X _ _ X X _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ _ 
     */
    public enum Layers
    {
        Default = 0, // "Camera 00", "Camera 01", "FXCamera"
        TransparentFX = 1, // "Camera"
        IgnoreRaycast = 2,
        Layer3 = 3,
        Water = 4,
        UI = 5, // "UIMainCamera"
        Layer6 = 6,
        Layer7 = 7, // Gizmo handles in the SPH and VAB
        PartsList_Icons = 8,
        Atmosphere = 9, // "Camera ScaledSpace"
        ScaledScenery = 10, // "Camera ScaledSpace"
        UIDialog = 11,
        UIVectors = 12, // "UIVectorCamera"
        UI_Mask = 13,
        Screens = 14,
        LocalScenery = 15,
        Kerbals = 16, // "kerbalCam", "pilotCam", "InternalCamera". Interaction colliders.
        EVA = 17, // "FXCamera"
        SkySphere = 18, // "GalaxyCamera"
        PhysicalObjects = 19,
        InternalSpace = 20,
        PartTriggers = 21,
        KerbalInstructors = 22,
        AeroFXIgnore = 23,
        MapFX = 24, // "MapFX Camera 88", "MapFX Camera 89". Try this for collisions, but reset on switching to the map view.
        UIAdditional = 25, // "UIMainCamera"
        WheelCollidersIgnore = 26,
        WheelColliders = 27,
        TerrainColliders = 28,
        DragRender = 29, // "AeroCamera"
        SurfaceFX = 30,
        Vectors = 31
    }
}
