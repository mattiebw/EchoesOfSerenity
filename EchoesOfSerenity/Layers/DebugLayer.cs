using System.Numerics;
using EchoesOfSerenity.Core;
using EchoesOfSerenity.Core.Content;
using EchoesOfSerenity.Core.Tilemap;
using EchoesOfSerenity.World.Tiles;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

namespace EchoesOfSerenity.Layers;

public class DebugLayer : ILayer
{
    private bool _fpsVisible;
    private const int FontSize = 18;
    private Font _font, _boldFont;
    private Vector2 _titleSize;
    private bool _debugMenuVisible;
    private int _tileX, _tileY;
    private Vector2 _tileLocation;
    private int _tilemapChunkPreviewIndex = 0;

    public void OnAttach()
    {
        _font = ContentManager.GetFont("Content/Fonts/OpenSans-Regular.ttf");
        _boldFont = ContentManager.GetFont("Content/Fonts/OpenSans-Bold.ttf");
        _titleSize = Raylib.MeasureTextEx(_boldFont, "EoS Stats", 18, 1);
    }

    public void Update()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F1))
            _fpsVisible = !_fpsVisible;
        if (Raylib.IsKeyPressed(KeyboardKey.F2))
            _debugMenuVisible = !_debugMenuVisible;
    }

    public void RenderUI()
    {
        if (_fpsVisible)
        {
            // Proper UI system would make this way better, but this is a jam baybee
            string fpsString = $"FPS: {Raylib.GetFPS()}\nEntities: 0";
            Vector2 size = Raylib.MeasureTextEx(_font, fpsString, FontSize, 1);
            Raylib.DrawRectangle(15, 15, (int)(MathF.Max(size.X, _titleSize.X) + 20), (int)(size.Y + _titleSize.Y + 15),
                new Color(0, 0, 0, 130));
            Raylib.DrawTextEx(_boldFont, "EoS Stats", new Vector2(25, 20), FontSize, 1, Color.White);
            Raylib.DrawTextEx(_font, fpsString, new Vector2(25, 25 + _titleSize.Y), FontSize, 1, Color.White);
        }

        if (_debugMenuVisible)
        {
            Tiles.TerrainTileset.RenderTile((int)_tileLocation.X, (int)_tileLocation.Y, _tileX, _tileY);
        }
    }

    public void RenderImGUI()
    {
        if (_debugMenuVisible)
        {
            ImGui.Begin("EoS Debug Menu", ref _debugMenuVisible);

            if (ImGui.Button("Close"))
                Game.Instance.CloseGame();

            float camZoom = Game.Instance.CameraZoom;
            if (ImGui.DragFloat("Camera Zoom", ref camZoom, 0.1f, 4f))
                Game.Instance.CameraZoom = camZoom;

            if (ImGui.CollapsingHeader("Tileset Debugging"))
            {
                if (ImGui.CollapsingHeader("View Tileset"))
                {
                    rlImGui.Image(Tiles.TerrainTileset.TilesetTexture);
                }

                ImGui.SliderInt("Tile tile X", ref _tileX, 0, Tiles.TerrainTileset.TileColumns - 1);
                ImGui.SliderInt("Test tile Y", ref _tileY, 0, Tiles.TerrainTileset.TileRows - 1);

                ImGui.SliderFloat2("Tile Location", ref _tileLocation, 0, 1000);
            }

            if (ImGui.CollapsingHeader("Tilemap Debugging"))
            {
                void TilemapDebugger(string name, Tilemap tilemap)
                {
                    if (ImGui.CollapsingHeader(name))
                    {
                        ImGui.Text($"Rendered Chunks: {tilemap.RenderedChunks}");
                        if (ImGui.CollapsingHeader("Tilemap Chunk Preview"))
                        {
                            ImGui.SliderInt("Chunk Index", ref _tilemapChunkPreviewIndex, 0, tilemap.Chunks.Count - 1);
                            rlImGui.ImageSize(tilemap.Chunks[_tilemapChunkPreviewIndex].Texture, 256, 256);
                        }
                    }
                }
                
                TilemapDebugger("Base Layer", Echoes.EchoesInstance.World.BaseLayer);
                TilemapDebugger("Top Layer", Echoes.EchoesInstance.World.TopLayer);
                
#if DEBUG
                ImGui.Checkbox("Draw Chunk Outlines", ref Tilemap.DrawChunkOutlines);
#endif
            }

            if (ImGui.CollapsingHeader("World Gen Debugging"))
            {
                if (ImGui.Button("View Full Map"))
                {
                    Game.Instance.Camera.Target = Echoes.EchoesInstance.World.GetCenterPoint();
                    Game.Instance.CameraZoom = 0.1f;
                }
            }

            ImGui.End();
        }
    }
}
