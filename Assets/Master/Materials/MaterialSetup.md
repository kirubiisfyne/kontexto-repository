## Procedural Toon Material Setup ##

- Create new material, follow naming convention: mat_SampleMaterial0
- Set Shader to "Procedural Toon"
- Pick Channel Mask, in relation to the material purpose (eg. ChannelMask_Stone for materials like Concrete Walls, Floors, etc.).
- Adjust Colors to your liking.
    - Base Color is self explanatory.
    - Detail Color are the darker spots (Multiply blend mode) in the surface to show detail.
    - Highlight Color are the lighter spots (Add blend mode) in the surface.
- As much as possible, reuse materials, for optimization purposes.
- Try not to modify tiling as possible, it denies material reusability.

## Making Channel Mask From Scratch in Krita (or any other software)

- Create new image in your prefered software, I recommend using Krita, but any other software will do.
- Set image size to 512x512.
- Utilize Krita's Wrap Around Mode to make tiling easier.
- Add 3 Layers, Base Color Texture, Detail/Shadow Texture, Highlights Texture, follow order, from top to bottom.
- In Layer Properties, use only one RGB channel for each layer, R for Base, G, for Detail, and B for Highlight.
- Set Blending mode to Add for all layers.
- Make sure theres no other layer, inlcuding background layer.
- Fill the Base layer with white, if it shows red, you're on the right path.
- Draw your details and hightligts on their respective layers with white, using the Digital Pen brush.
- After, Export as PNG.


## Textured Toon Material Setup ##

- Create new material, follow naming convention: mat_SampleMaterial0
- Set Shader to "Textured Toon"
- Set Base Texture
- As much as possible, reuse materials, for optimization purposes.
- Try not to modify tiling as possible, it denies material reusability.