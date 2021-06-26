namespace Alex.Blocks.Minecraft
{
	public class OakDoor : Door
	{
		public OakDoor() : base(3028)
		{
			Solid = true;
			Transparent = true;
			IsFullCube = false;
			
			BlockMaterial = Material.Wood;
		}
	}
}