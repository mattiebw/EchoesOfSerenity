using System.Numerics;
using EchoesOfSerenity.Core.Content;
using Raylib_cs;

namespace EchoesOfSerenity.Core.Tilemap;

public class Tileset
{
    public Texture2D TilesetTexture { get; private set; }
    public int TileWidth, TileHeight;
    public int TileCount => (TilesetTexture.Width / TileWidth) * (TilesetTexture.Height / TileHeight);
    public int TileColumns => TilesetTexture.Width / TileWidth;
    public int TileRows => TilesetTexture.Height / TileHeight;

    public Tileset(string filepath, int tileWidth, int tileHeight)
    {
        TilesetTexture = ContentManager.GetTexture(filepath);
        TileWidth = tileWidth;
        TileHeight = tileHeight;

        if (TilesetTexture.Width % TileWidth != 0
            || TilesetTexture.Height % TileHeight != 0)
        {
            Utility.WriteLineColour(ConsoleColor.Red, $"Tileset image size does not match tile size.");
        }
    }

    public void RenderTile(int x, int y, int tileX, int tileY, float rot = 0)
    {
        switch (rot)
        {
            case 90:
                x += TileWidth;
                break;
            case 180:
                x += TileWidth;
                y += TileHeight;
                break;
            case 270:
                y += TileHeight;
                break;
        }
        
        Raylib.DrawTexturePro(TilesetTexture,
            new Rectangle(tileX * TileWidth, tileY * TileHeight, TileWidth, TileHeight), new Rectangle(x, y, TileWidth, TileHeight), Vector2.Zero, 
            rot, Color.White);
    }
    
    public (int, int) GetTileCoordinates(int tileIndex)
    {
        int x = tileIndex % TileColumns;
        int y = tileIndex / TileColumns;
        return (x, y);
    }
}
