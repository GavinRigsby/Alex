namespace Alex.Blocks.Minecraft
{
    public class Log : Block
    {
        public Log()
        {
            Transparent = false;
            Solid = true;

            BlockMaterial = Material.Wood.Clone().SetHardness(2);
           // Hardness = 2;
        }
    }
}