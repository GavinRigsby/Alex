using Alex.Blocks.Materials;

namespace Alex.Blocks.Minecraft
{
    public class Carpet : Block
    {
        public Carpet()
        {
            Solid = true;
            Transparent = true;
            
            BlockMaterial = Material.Carpet;
            IsFullCube = false;
        }
    }
}