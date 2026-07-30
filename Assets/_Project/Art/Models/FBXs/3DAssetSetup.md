## Blender Model Setup ##

- After Modeling, Clean Up Topology. Vertices must not exceed **400**.
- Layout UVs; 
    1. Average island scale.
    2. Stack similar islands using UV Toolkit addon.
    3. Adjust Texel Density using Texel Density Checker addon. Texel Density must be 128 pixels per meter, 128px/m.
- Export as FBX.
- In Unity, use "FBXImporter" preset