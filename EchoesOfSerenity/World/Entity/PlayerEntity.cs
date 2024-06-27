using System.Numerics;
using EchoesOfSerenity.Core;
using EchoesOfSerenity.Core.Entity;
using EchoesOfSerenity.Core.Tilemap;
using EchoesOfSerenity.UI;
using EchoesOfSerenity.UI.Menus;
using EchoesOfSerenity.World.Item;
using Raylib_cs;

namespace EchoesOfSerenity.World.Entity;

public struct TileBreakInfo
{
    public int X, Y, RemainingHits, MaxHits;
}

public class PlayerEntity : LivingEntity
{
    public float MoveSpeed = 96;
    public float SpeedMultiplier = 1;
    public float PlaceRange = 48;
    public int SelectedHotbarSlot = 0;
    public Inventory Inventory = new(18);
    private Vector2 _lastMovement, _lastLerpedMovement;

    public static float IntroAnimInitialZoom = 0.3f,
        IntroAnimTargetZoom = 1.4f,
        IntroAnimZoomSpeed = 0.4f,
        IntroAnimZoomDelay = 2.5f;

    private float _introAnimTimer = 0;
    private bool _introAnimActive = true;
    private bool _drowned = false;
    private Animation _breakAnimation;
    private TileBreakInfo? _breakInfo = null;
    private float _useTimer = 0;

    public PlayerEntity()
    {
        Size = new(15, 15);
        _breakAnimation = Spritesheets.Breaking.Animations["break"];
        Spritesheet = Spritesheets.Player;
        SetAnimation("idle");
        _introAnimTimer = IntroAnimZoomDelay;
        Game.Instance.CameraZoom = IntroAnimInitialZoom;
        Echoes.EchoesInstance.HUD!.Player = this;

        Inventory.Contents[0] = (Items.Bomb, 5);
        Inventory.Contents[1] = (Items.JakeVoodooDoll, 2);
    }

    public override void OnAddedToWorld()
    {
        World.Player = this;
    }

    public override void Update()
    {
        base.Update();

        if (Health <= 0)
            return;

        // Check if we're in water
        bool inWater = World.BaseLayer.TileAtWorldCoord(Center) == Tiles.Tiles.Water;
        if (inWater)
        {
            SetAnimation("in_water");
            SpeedMultiplier = 0.3f;
        }
        else SpeedMultiplier = 1;

        if (World.BaseLayer.TileAtWorldCoord(Center) == Tiles.Tiles.DeepWater)
        {
            Drown();
            return;
        }

        if (_introAnimActive)
        {
            float howClose = MathF.Abs(Game.Instance.CameraZoom - IntroAnimTargetZoom);
            if (_introAnimTimer > 0)
            {
                _introAnimTimer -= Raylib.GetFrameTime();
            }
            else
            {
                Game.Instance.CameraZoom =
                    Utility.LerpSmooth(Game.Instance.CameraZoom, IntroAnimTargetZoom, IntroAnimZoomSpeed);
                if (howClose < 0.01f)
                {
                    Game.Instance.CameraZoom = IntroAnimTargetZoom;
                    _introAnimActive = false;
                }
            }

            if (howClose > 0.25f)
                return;
        }

        Vector2 movement = Vector2.Zero;
        if (Raylib.IsKeyDown(KeyboardKey.W))
            movement.Y -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.S))
            movement.Y += 1;
        if (Raylib.IsKeyDown(KeyboardKey.A))
            movement.X -= 1;
        if (Raylib.IsKeyDown(KeyboardKey.D))
            movement.X += 1;

        if (Raylib.IsKeyDown(KeyboardKey.One))
            SelectedHotbarSlot = 0;
        if (Raylib.IsKeyDown(KeyboardKey.Two))
            SelectedHotbarSlot = 1;
        if (Raylib.IsKeyDown(KeyboardKey.Three))
            SelectedHotbarSlot = 2;
        if (Raylib.IsKeyDown(KeyboardKey.Four))
            SelectedHotbarSlot = 3;
        if (Raylib.IsKeyDown(KeyboardKey.Five))
            SelectedHotbarSlot = 4;
        if (Raylib.IsKeyDown(KeyboardKey.Six))
            SelectedHotbarSlot = 5;

        SelectedHotbarSlot += (int)Raylib.GetMouseWheelMove();
        SelectedHotbarSlot = Math.Clamp(SelectedHotbarSlot, 0, Inventory.RowSize - 1);

        if (Raylib.IsKeyPressed(KeyboardKey.H))
            Hurt(1);

        if (Raylib.IsKeyPressed(KeyboardKey.B))
        {
            BombEntity bomb = new();
            bomb.Position = Position;
            var mousePos = Game.Instance.ScreenPosToWorld(Raylib.GetMousePosition());
            bomb.Velocity = Vector2.Normalize(mousePos - Position) * 100;
            World.AddEntity(bomb);
        }

        if (_useTimer > 0)
            _useTimer -= Raylib.GetFrameTime();

        var hasUsed = false;
        if (_useTimer <= 0)
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
                hasUsed = UseSelectedItem();

            if (!hasUsed)
                UpdateTileBreak();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Tab))
        {
            Game.Instance.AttachLayer(new MenuLayer(new PlayerMenu(this)), Game.Instance.GetLayerCount() - 1);
        }

        if (movement != Vector2.Zero)
        {
            movement = Vector2.Normalize(movement) * (MoveSpeed * SpeedMultiplier * Raylib.GetFrameTime());
            _lastMovement = movement;

            var newPos = Position;
            newPos.X += movement.X;
            if (World.CheckCollision(new Rectangle(newPos.X, newPos.Y, Size.X, Size.Y)))
                newPos.X = Position.X;
            newPos.Y += movement.Y;
            if (World.CheckCollision(new Rectangle(newPos.X, newPos.Y, Size.X, Size.Y)))
                newPos.Y = Position.Y;
            Position = newPos;

            if (!inWater && Health > 0) SetAnimation("walk");
        }
        else
        {
            if (!inWater && Health > 0) SetAnimation("idle");
        }

        Vector2 lerpedMovement = Utility.LerpSmooth(_lastLerpedMovement, _lastMovement, 0.02f);
        _lastLerpedMovement = lerpedMovement;
        Rot = (float)Math.Atan2(lerpedMovement.Y, lerpedMovement.X) * (180 / MathF.PI);

        Game.Instance.CameraTarget = Center;
    }

    public void UpdateTileBreak()
    {
        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            float useTime = 0.5f;
            int strength = 1;
            ToolType toolType = ToolType.None;

            (Item.Item? selected, _) = Inventory.Contents[SelectedHotbarSlot];
            if (selected is not null && selected.UseType == UseType.Tool)
            {
                useTime = selected.UseSpeed;
                strength = selected.ToolStrength;
                toolType = selected.ToolType;
            }

            Vector2 mousePos = Game.Instance.ScreenPosToWorld(Raylib.GetMousePosition());
            int tileX = (int)(mousePos.X / 16);
            int tileY = (int)(mousePos.Y / 16);
            Tile? tile = World.TopLayer.TileAtTileCoord(tileX, tileY);
            if (tile is null)
            {
                _breakInfo = null;
                return;
            }

            if (_breakInfo is null || tileX != _breakInfo.Value.X || tileY != _breakInfo.Value.Y)
            {
                // Switched tile
                if (Raymath.Vector2DistanceSqr(mousePos, Center) > PlaceRange * PlaceRange)
                {
                    _breakInfo = null;
                }
                else if (tile.CanBePunched || (selected is not null && selected.UseType == UseType.Tool))
                {
                    int maxHits = tile.Strength;
                    if (tile.RequiredTool == toolType)
                        maxHits = Math.Clamp(maxHits - strength, 1, maxHits);
                    _breakInfo = new TileBreakInfo
                    {
                        X = tileX,
                        Y = tileY,
                        RemainingHits = maxHits,
                        MaxHits = maxHits
                    };
                }
            }
            
            if (_breakInfo is not null)
            {
                _breakInfo = new TileBreakInfo
                {
                    X = _breakInfo.Value.X,
                    Y = _breakInfo.Value.Y,
                    RemainingHits = Math.Max(0, _breakInfo.Value.RemainingHits - 1),
                    MaxHits = _breakInfo.Value.MaxHits
                };

                if (_breakInfo.Value.RemainingHits == 0)
                {
                    if (tile.BreakSound.FrameCount != 0)
                        SoundManager.PlaySound(tile!.BreakSound);
                    World.TopLayer.DestroyTile(_breakInfo.Value.X, _breakInfo.Value.Y);
                    _breakInfo = null;
                }
                else
                {
                    if (tile.HitSound.FrameCount != 0)
                        SoundManager.PlaySound(tile.HitSound);
                }
                
                _useTimer = useTime;
            }
        }
        else
        {
            _breakInfo = null;
        }
    }

    public bool UseSelectedItem()
    {
        (Item.Item? heldItem, int count) = Inventory.Contents[SelectedHotbarSlot];
        if (heldItem is null) return false;
        var used = heldItem.OnUsed(this);

        if (used)
            _useTimer = heldItem.UseSpeed;
        
        if (used && heldItem.Consumable)
        {
            count--;
            if (count <= 0)
                Inventory.Contents[SelectedHotbarSlot] = (null, 0);
            else
                Inventory.Contents[SelectedHotbarSlot] = (heldItem, count);
        }

        return used && heldItem.UseType != UseType.Tool;
    }

    public override void Render()
    {
        base.Render();

        if (_breakInfo is not null)
        {
            float progress = 1 - (float)_breakInfo.Value.RemainingHits / _breakInfo.Value.MaxHits;
            Vector2 pos = new(_breakInfo.Value.X * 16, _breakInfo.Value.Y * 16);
            Raylib.DrawTexturePro(Spritesheets.Breaking.Texture,
                _breakAnimation.Frames[
                    Math.Clamp((int)(progress * _breakAnimation.Frames.Length), 0, _breakAnimation.Frames.Length - 1)],
                new Rectangle(pos, World.TopLayer.Tileset.TileWidth, World.TopLayer.Tileset.TileHeight), Vector2.Zero,
                0, Color.Black);
        }
    }

    public void Drown()
    {
        _drowned = true;
        Hurt(Health);
    }

    public override void Die()
    {
        SetAnimation(_drowned ? "drowned" : "dead");

        // Drop all items
        foreach (var (item, count) in Inventory.Contents)
        {
            if (item is not null)
            {
                ItemEntity itemEntity = new(item, count);
                itemEntity.Position = Position;
                itemEntity.Velocity *= 4;
                World.AddEntity(itemEntity);
            }
        }

        Inventory.Empty();

        if (_drowned)
            Game.Instance.AttachLayer(new MenuLayer(new DeadMenu(this, "YOU DROWNED", "Deep water will kill you")),
                Game.Instance.GetLayerCount() - 1);
        else
            Game.Instance.AttachLayer(new MenuLayer(new DeadMenu(this)), Game.Instance.GetLayerCount() - 1);
    }

    public void Respawn()
    {
        Center = World.SpawnPoint;
        Health = 10;
        _drowned = false;
        ImmunityTimer = 3f;
    }
}
