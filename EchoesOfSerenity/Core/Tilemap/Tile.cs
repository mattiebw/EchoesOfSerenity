using EchoesOfSerenity.World.Item;

namespace EchoesOfSerenity.Core.Tilemap;

public class Tile
{
    public bool IsSolid = false;
    public bool HasBorder = false;
    public int TileSetIndex = 0;
    public bool RandomRotation = false;

    public bool Animated = false;
    public int Frames = 1; // Should be stored sequentially in the tileset, in the X direction
    public int FPS = 3;

    public int Strength = 5, MinimumToolStrength = 0;
    public ToolType RequiredTool = ToolType.None;
    public bool CanBePunched = false;
    public List<(Item, int, int)> Drops = [];
}
