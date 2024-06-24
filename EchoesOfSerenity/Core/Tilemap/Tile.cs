namespace EchoesOfSerenity.Core.Tilemap;

public class Tile
{
    public bool IsSolid = false;
    public bool HasBorder = false;
    public int TileSetIndex = 0;
    public bool RandomRotation = false;

    public bool Animated = false;
    public int Frames = 1; // Should be stored sequentially in the tileset, in the X direction 
}
