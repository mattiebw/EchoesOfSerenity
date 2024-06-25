using System.Diagnostics;
using System.Numerics;
using Raylib_cs;

namespace EchoesOfSerenity.Core.Tilemap;

public class Tilemap : IDisposable
{
    public Tileset Tileset;

    public int Width { get; private set; }
    public int Height { get; private set; }
    private Tile[,] _tiles;
    private const int ChunkSize = 16;
    public List<RenderTexture2D> Chunks { get; private set; } = new();
    private HashSet<int> _dirtyChunks = new();

    public static bool DrawChunkOutlines = false;
    public static int RenderedChunks { get; private set; } = 0;

    public Tilemap(int width, int height, Tileset tileset)
    {
        if (width % ChunkSize != 0 || height % ChunkSize != 0)
        {
            Utility.WriteLineColour(ConsoleColor.Red, $"Tilemap size must be divisible by the chunk size ({ChunkSize}");
        }

        Tileset = tileset;
        Width = width;
        Height = height;
        _tiles = new Tile[width, height];

        int chunkCount = (width / ChunkSize) * (height / ChunkSize);
        for (int i = 0; i < chunkCount; i++)
            RenderChunk(i);
    }

    public void SetTile(int x, int y, Tile tile)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            Utility.WriteLineColour(ConsoleColor.Red, $"Tile position ({x}, {y}) is out of bounds.");
            return;
        }

        _tiles[x, y] = tile;
        _dirtyChunks.Add((x / ChunkSize) + (y / ChunkSize) * (Width / ChunkSize));
    }

    public void PreRender()
    {
        if (_dirtyChunks.Count == 0) return;
        Stopwatch sw = new();
        sw.Start();
        
        foreach (var chunk in _dirtyChunks)
            RenderChunk(chunk);
        
        sw.Stop();
        Utility.WriteLineColour(ConsoleColor.Green, $"Rendered {_dirtyChunks.Count} chunks in {sw.Elapsed.TotalSeconds:F}s.");
        
        _dirtyChunks.Clear();
    }

    public void Render()
    {
        RenderedChunks = 0;
        
        // Make camera rectangle
        var mat = Raylib.GetCameraMatrix2D(Game.Instance.Camera);
        mat = Raymath.MatrixInvert(mat);
        var topLeft = Raymath.Vector2Transform(new(0, 0), mat);
        var bottomRight = Raymath.Vector2Transform(new(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()), mat);
        Rectangle cameraRect = new(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        
        // Render the chunks
        int x = 0, y = 0;
        Rectangle source = new Rectangle(0, 0, ChunkSize * Tileset.TileWidth, -ChunkSize * Tileset.TileHeight);
        for (var index = 0; index < Chunks.Count; index++)
        {
            // Make chunk bounding box
            Rectangle chunkRect = new(x, y, ChunkSize * Tileset.TileWidth, ChunkSize * Tileset.TileHeight);
            // And check if it's in the camera
            if (Raylib.CheckCollisionRecs(cameraRect, chunkRect))
            {
                Raylib.DrawTextureRec(Chunks[index].Texture, source, new Vector2(x, y), Color.White);
#if DEBUG
                if (DrawChunkOutlines)
                    Raylib.DrawRectangleLines(x, y, ChunkSize * Tileset.TileWidth, ChunkSize * Tileset.TileHeight,
                        Color.Red);
#endif

                // Draw animated tiles
                // int chunkX = (index % (Width / ChunkSize)) * ChunkSize;
                // int chunkY = (index / (Width / ChunkSize)) * ChunkSize;
                int chunkX = x / Tileset.TileWidth;
                int chunkY = y / Tileset.TileHeight;
                for (int cy = chunkY; cy < chunkY + ChunkSize; cy++) // Loop y first for cache efficiency
                {
                    for (int cx = chunkX; cx < chunkX + ChunkSize; cx++)
                    {
                        Tile tile = _tiles[cx, cy];
                        if (tile is null || !tile.Animated) continue;
                        var (tilesetX, tilesetY) = Tileset.GetTileCoordinates(tile.TileSetIndex);
                        Tileset.RenderTile(cx * Tileset.TileWidth, cy * Tileset.TileHeight, tilesetX + (int)((Raylib.GetTime() * tile.FPS) % tile.Frames),
                            tilesetY);
                    }
                }

                RenderedChunks++;
            }

            x += ChunkSize * Tileset.TileWidth;
            if (x >= Width * Tileset.TileWidth)
            {
                x = 0;
                y += ChunkSize * Tileset.TileHeight;
            }
        }
    }

    public void RenderChunk(int index)
    {
        int chunkX = (index % (Width / ChunkSize)) * ChunkSize;
        int chunkY = (index / (Width / ChunkSize)) * ChunkSize;

        if (!Chunks.IsValidIndex(index))
        {
            if (Chunks.Count != index)
                Utility.WriteLineColour(ConsoleColor.Red, "Chunk index is out of order.");

            Chunks.Add(Raylib.LoadRenderTexture(ChunkSize * Tileset.TileWidth, ChunkSize * Tileset.TileHeight));
        }

        Raylib.BeginTextureMode(Chunks[index]);
        Raylib.ClearBackground(Color.Blank);

        for (int y = chunkY; y < chunkY + ChunkSize; y++) // Loop y first for cache efficiency
        {
            for (int x = chunkX; x < chunkX + ChunkSize; x++)
            {
                Tile tile = _tiles[x, y];
                if (tile is null || tile.Animated) continue;
                var (tilesetX, tilesetY) = Tileset.GetTileCoordinates(tile.TileSetIndex);

                float rot = 0;
                if (tile.RandomRotation)
                {
                    // Random rnd = new(Utility.GetSeedXY(x, y));
                    // rot = rnd.Next(0, 4) * 90;
                    
                    rot = Utility.ChaosHash(x, y) % 4 * 90;
                }
                
                Tileset.RenderTile((x - chunkX) * Tileset.TileWidth, (y - chunkY) * Tileset.TileHeight, tilesetX,
                    tilesetY, rot);
            }
        }

        Raylib.EndTextureMode();
    }

    public void Dispose()
    {
        foreach (var texture in Chunks)
            Raylib.UnloadRenderTexture(texture);
    }
}
