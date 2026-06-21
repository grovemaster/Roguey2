using JRogue.World.Generation.Vaults;
using NUnit.Framework;

namespace JRogue.Tests.UnitTests.World
{
    [TestFixture]
    public sealed class VaultFileParserTests
    {
        const string ShrineVault = @"
VAULT vault_shrine_5x5
MIN_DISTANCE_FROM_PLAYER_START 8
ORIGIN 2 2
TILES floor=SandTheme:32;wall=SandTheme:50
MAP
WWWWW
W...W
W...W
W...W
WWWWW
ENDMAP
ITEM healing_potion AT 1 3
INTERACTABLE lever_shrine AT 2 2
END";

        [Test]
        public void TryParse_Shrine_SucceedsWithExpectedLayout()
        {
            Assert.IsTrue(VaultFileParser.TryParse(ShrineVault, out VaultBlueprint blueprint, out string error), error);
            Assert.AreEqual("vault_shrine_5x5", blueprint.VaultId);
            Assert.AreEqual(5, blueprint.Width);
            Assert.AreEqual(5, blueprint.Height);
            Assert.AreEqual(new UnityEngine.Vector2Int(2, 2), blueprint.Origin);
            Assert.AreEqual(8, blueprint.MinDistanceFromPlayerStart);
            Assert.AreEqual(1, blueprint.Items.Count);
            Assert.AreEqual(1, blueprint.Interactables.Count);
            Assert.Greater(blueprint.Cells.Count, 0);
        }

        [Test]
        public void TryParse_AmbushDoorRow_ParsesDoorCells()
        {
            const string ambush = @"
VAULT vault_ambush_corridor_7x4
ORIGIN 3 1
TILES floor=SnowTheme:32;wall=SnowTheme:48
MAP
WWWWWWW
W.....W
WD...DW
W.....W
WWWWWWW
ENDMAP
HAZARD lava AT 2 2
END";

            Assert.IsTrue(VaultFileParser.TryParse(ambush, out VaultBlueprint blueprint, out string error), error);
            Assert.AreEqual(7, blueprint.Width);
            Assert.AreEqual(4, blueprint.Height);
            Assert.AreEqual(2, blueprint.Hazards.Count);

            bool hasDoor = false;
            for (int i = 0; i < blueprint.Cells.Count; i++)
            {
                if (blueprint.Cells[i].Kind == VaultCellKind.Door)
                    hasDoor = true;
            }

            Assert.IsTrue(hasDoor);
        }

        [Test]
        public void TryParse_PerGlyphTiles_BindsMultipleFloorAndWallKeys()
        {
            const string multi = @"
VAULT vault_multi_tile
ORIGIN 0 0
TILES .=SandTheme:32;+=SandTheme:40;W=SandTheme:50;x=SnowTheme:48
MAP
WxW
+.+
WxW
ENDMAP
END";

            Assert.IsTrue(VaultFileParser.TryParse(multi, out VaultBlueprint blueprint, out string error), error);
            Assert.IsTrue(blueprint.Glyphs.ContainsKey('.'));
            Assert.IsTrue(blueprint.Glyphs.ContainsKey('+'));
            Assert.IsTrue(blueprint.Glyphs.ContainsKey('W'));
            Assert.IsTrue(blueprint.Glyphs.ContainsKey('x'));
            Assert.AreEqual("SandTheme:32", blueprint.Glyphs['.'].TileKey);
            Assert.AreEqual("SandTheme:40", blueprint.Glyphs['+'].TileKey);
            Assert.AreEqual("SandTheme:50", blueprint.Glyphs['W'].TileKey);
            Assert.AreEqual("SnowTheme:48", blueprint.Glyphs['x'].TileKey);

            bool hasAccentFloor = false;
            for (int i = 0; i < blueprint.Cells.Count; i++)
            {
                VaultMapCell cell = blueprint.Cells[i];
                if (cell.Glyph.TileKey == "SandTheme:40")
                    hasAccentFloor = true;
            }

            Assert.IsTrue(hasAccentFloor);
        }

        [Test]
        public void TryParse_TileLine_SetsExplicitGlyph()
        {
            const string tileLine = @"
VAULT vault_tile_line
TILES floor=SandTheme:32
TILE + floor SandTheme:40
MAP
...
ENDMAP
END";

            Assert.IsTrue(VaultFileParser.TryParse(tileLine, out VaultBlueprint blueprint, out string error), error);
            Assert.IsTrue(blueprint.Glyphs.ContainsKey('+'));
            Assert.AreEqual("SandTheme:40", blueprint.Glyphs['+'].TileKey);
        }

        [Test]
        public void TryParse_UnboundMapCharacter_Fails()
        {
            const string bad = @"
VAULT bad
TILES floor=SandTheme:32;wall=SandTheme:50
MAP
W?W
ENDMAP
END";

            Assert.IsFalse(VaultFileParser.TryParse(bad, out _, out string error));
            Assert.That(error, Does.Contain("?"));
        }

        [Test]
        public void TryParse_EnemyAtLines_RecordsPlacements()
        {
            const string withEnemies = @"
VAULT vault_ambush
ORIGIN 0 0
TILES .=SnowTheme:32;W=SnowTheme:48
MAP
...
ENDMAP
ENEMY skeleton AT 1 2
ENEMY skeleton AT 5 2
END";

            Assert.IsTrue(VaultFileParser.TryParse(withEnemies, out VaultBlueprint blueprint, out string error), error);
            Assert.AreEqual(2, blueprint.Enemies.Count);
            Assert.AreEqual("skeleton", blueprint.Enemies[0].EnemyId);
            Assert.AreEqual(1, blueprint.Enemies[0].X);
            Assert.AreEqual(2, blueprint.Enemies[0].Y);
            Assert.AreEqual("skeleton", blueprint.Enemies[1].EnemyId);
            Assert.AreEqual(5, blueprint.Enemies[1].X);
            Assert.AreEqual(2, blueprint.Enemies[1].Y);
        }

        [Test]
        public void TryParse_InvalidHeader_Fails()
        {
            Assert.IsFalse(VaultFileParser.TryParse("NOPE", out _, out string error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void TryParse_ProductionMonumentVault_Succeeds()
        {
            const string monument = @"
VAULT vault_monument_8x8
ORIGIN 3 3
TILES .=DcssCavern:grey_dirt_0_new;g1=DcssCavern:floor_nerves_2_cyan;g2=DcssCavern:floor_nerves_4_cyan;W=DcssCavern:wall_stone2_gray_2_new
MAP
........
........
..g1.g2..
...WW...
...WW...
..g2.g1..
........
........
ENDMAP
INTERACTABLE bump_monument_inscription AT 3 3
INTERACTABLE bump_monument_inscription AT 4 3
INTERACTABLE bump_monument_inscription AT 3 4
INTERACTABLE bump_monument_inscription AT 4 4
END";

            Assert.IsTrue(VaultFileParser.TryParse(monument, out VaultBlueprint blueprint, out string error), error);
            Assert.AreEqual("vault_monument_8x8", blueprint.VaultId);
            Assert.AreEqual(8, blueprint.Width);
            Assert.AreEqual(8, blueprint.Height);
            Assert.AreEqual(4, blueprint.Interactables.Count);
        }
    }
}
