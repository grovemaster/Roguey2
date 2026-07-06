using JRogue.World.Town;
using NUnit.Framework;
using UnityEngine;

namespace JRogue.Tests.World
{
    public sealed class HolyLandNexusLayoutTests
    {
        [Test]
        public void Decagon_includes_center_and_north_transition_strip()
        {
            Assert.That(HolyLandNexusLayout.IsInsideDecagon(HolyLandNexusLayout.Center, HolyLandNexusLayout.Center), Is.True);
            Assert.That(
                DistrictSquareHolyNexusTransition.IsNexusNorthTransitionCell(
                    new Vector3Int(DimensionSquareLayout.Center, DistrictSquareHolyNexusTransition.NexusNorthEdgeY, 0)),
                Is.True);
        }

        [Test]
        public void Decagon_excludes_far_corner_of_bounding_box()
        {
            Assert.That(HolyLandNexusLayout.IsInsideDecagon(0, 0), Is.False);
            Assert.That(HolyLandNexusLayout.IsInsideDecagon(39, 39), Is.False);
        }

        [Test]
        public void Holy_land_gate_is_at_13_35_with_approach_path()
        {
            Assert.That(HolyLandNexusLayout.HolyLandGateCell, Is.EqualTo(new Vector3Int(13, 35, 0)));
            Assert.That(HolyLandNexusLayout.IsWalkableCell(13, 35), Is.True);
            Assert.That(HolyLandNexusLayout.IsWalkableCell(14, 35), Is.True);
        }

        [Test]
        public void North_hub_connection_links_interior_to_north_portal_strip()
        {
            Assert.That(HolyLandNexusLayout.IsWalkableCell(20, 20), Is.True, "decagon center");
            Assert.That(HolyLandNexusLayout.IsWalkableCell(20, 37), Is.True, "corridor mid");
            Assert.That(HolyLandNexusLayout.IsWalkableCell(20, 39), Is.True, "north portal strip");
        }

        [Test]
        public void Elf_holy_land_gate_is_at_7_35_with_approach_path()
        {
            Assert.That(HolyLandNexusLayout.ElfHolyLandGateCell, Is.EqualTo(new Vector3Int(7, 35, 0)));
            Assert.That(HolyLandNexusLayout.IsWalkableCell(7, 35), Is.True);
            Assert.That(HolyLandNexusLayout.IsWalkableCell(8, 35), Is.True);
            Assert.That(HolyLandNexusLayout.IsWalkableCell(12, 35), Is.True);
        }

        [Test]
        public void Elf_and_barbarian_gates_are_at_least_five_tiles_from_north_portal_strip()
        {
            for (int x = DistrictSquareHolyNexusTransition.StripMinX;
                 x <= DistrictSquareHolyNexusTransition.StripMaxX;
                 x++)
            {
                var stripCell = new Vector3Int(x, DistrictSquareHolyNexusTransition.NexusNorthEdgeY, 0);
                if (!DistrictSquareHolyNexusTransition.IsNexusNorthTransitionCell(stripCell))
                    continue;

                int barbarianDist = Manhattan(
                    stripCell.x,
                    stripCell.y,
                    HolyLandNexusLayout.HolyLandGateCell.x,
                    HolyLandNexusLayout.HolyLandGateCell.y);
                int elfDist = Manhattan(
                    stripCell.x,
                    stripCell.y,
                    HolyLandNexusLayout.ElfHolyLandGateCell.x,
                    HolyLandNexusLayout.ElfHolyLandGateCell.y);

                Assert.That(barbarianDist, Is.GreaterThanOrEqualTo(5));
                Assert.That(elfDist, Is.GreaterThanOrEqualTo(5));
            }
        }

        [Test]
        public void Nexus_exit_arrival_cells_are_inward_from_gate_portals()
        {
            Assert.That(
                HolyLandNexusLayout.BarbarianHolyLandNexusArrivalCell,
                Is.EqualTo(new Vector3Int(13, 34, 0)));
            Assert.That(
                HolyLandNexusLayout.ElfHolyLandNexusArrivalCell,
                Is.EqualTo(new Vector3Int(7, 34, 0)));
            Assert.That(
                HolyLandTransitionIds.GetNexusReturnAnchorForExit(HolyLandTransitionIds.ElfHolyLandToNexus),
                Is.EqualTo(HolyLandNexusLayout.ElfHolyLandNexusArrivalCell));
            Assert.That(
                HolyLandTransitionIds.GetNexusReturnAnchorForExit(HolyLandTransitionIds.HolyLandToNexus),
                Is.EqualTo(HolyLandNexusLayout.BarbarianHolyLandNexusArrivalCell));
        }

        [Test]
        public void Elf_holy_land_exit_arrival_cell_is_walkable()
        {
            Vector3Int arrival = HolyLandNexusLayout.ElfHolyLandNexusArrivalCell;
            Assert.That(HolyLandNexusLayout.IsWalkableCell(arrival.x, arrival.y), Is.True);
        }

        [Test]
        public void Holy_land_exit_stand_cells_are_inward_from_gate_markers()
        {
            Assert.That(HolyLandNexusLayout.ElfHolyLandExitStandCell, Is.EqualTo(new Vector3Int(8, 35, 0)));
            Assert.That(HolyLandNexusLayout.BarbarianHolyLandExitStandCell, Is.EqualTo(new Vector3Int(12, 35, 0)));
            Assert.That(
                HolyLandNexusLayout.TryGetHolyLandExitStandCell(
                    HolyLandTransitionIds.ElfHolyLandToNexus,
                    out Vector3Int elfStand),
                Is.True);
            Assert.That(elfStand, Is.EqualTo(HolyLandNexusLayout.ElfHolyLandExitStandCell));
            Assert.That(
                HolyLandNexusLayout.IsHolyLandExitActivationCell(HolyLandNexusLayout.ElfHolyLandExitStandCell),
                Is.True);
        }

        [Test]
        public void Beastman_holy_land_gate_is_at_1_35_with_approach_path()
        {
            Assert.That(HolyLandNexusLayout.BeastmanHolyLandGateCell, Is.EqualTo(new Vector3Int(1, 35, 0)));
            Assert.That(HolyLandNexusLayout.IsWalkableCell(1, 35), Is.True);
            Assert.That(HolyLandNexusLayout.IsWalkableCell(2, 35), Is.True);
            Assert.That(HolyLandNexusLayout.IsWalkableCell(6, 35), Is.True);
        }

        [Test]
        public void Beastman_gate_is_at_least_five_tiles_from_north_portal_strip_and_other_gates()
        {
            for (int x = DistrictSquareHolyNexusTransition.StripMinX;
                 x <= DistrictSquareHolyNexusTransition.StripMaxX;
                 x++)
            {
                var stripCell = new Vector3Int(x, DistrictSquareHolyNexusTransition.NexusNorthEdgeY, 0);
                if (!DistrictSquareHolyNexusTransition.IsNexusNorthTransitionCell(stripCell))
                    continue;

                int beastmanDist = Manhattan(
                    stripCell.x,
                    stripCell.y,
                    HolyLandNexusLayout.BeastmanHolyLandGateCell.x,
                    HolyLandNexusLayout.BeastmanHolyLandGateCell.y);

                Assert.That(beastmanDist, Is.GreaterThanOrEqualTo(5));
            }

            Assert.That(
                Manhattan(
                    HolyLandNexusLayout.BeastmanHolyLandGateCell.x,
                    HolyLandNexusLayout.BeastmanHolyLandGateCell.y,
                    HolyLandNexusLayout.ElfHolyLandGateCell.x,
                    HolyLandNexusLayout.ElfHolyLandGateCell.y),
                Is.GreaterThanOrEqualTo(5));
            Assert.That(
                Manhattan(
                    HolyLandNexusLayout.BeastmanHolyLandGateCell.x,
                    HolyLandNexusLayout.BeastmanHolyLandGateCell.y,
                    HolyLandNexusLayout.HolyLandGateCell.x,
                    HolyLandNexusLayout.HolyLandGateCell.y),
                Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void Beastman_holy_land_exit_arrival_and_stand_cells_are_configured()
        {
            Assert.That(
                HolyLandNexusLayout.BeastmanHolyLandNexusArrivalCell,
                Is.EqualTo(new Vector3Int(1, 34, 0)));
            Assert.That(
                HolyLandTransitionIds.GetNexusReturnAnchorForExit(HolyLandTransitionIds.BeastmanHolyLandToNexus),
                Is.EqualTo(HolyLandNexusLayout.BeastmanHolyLandNexusArrivalCell));
            Assert.That(HolyLandNexusLayout.BeastmanHolyLandExitStandCell, Is.EqualTo(new Vector3Int(2, 35, 0)));
            Assert.That(
                HolyLandNexusLayout.TryGetHolyLandExitStandCell(
                    HolyLandTransitionIds.BeastmanHolyLandToNexus,
                    out Vector3Int beastmanStand),
                Is.True);
            Assert.That(beastmanStand, Is.EqualTo(HolyLandNexusLayout.BeastmanHolyLandExitStandCell));
            Assert.That(
                HolyLandNexusLayout.IsHolyLandExitActivationCell(HolyLandNexusLayout.BeastmanHolyLandExitStandCell),
                Is.True);
            Assert.That(
                HolyLandNexusLayout.IsWalkableCell(
                    HolyLandNexusLayout.BeastmanHolyLandNexusArrivalCell.x,
                    HolyLandNexusLayout.BeastmanHolyLandNexusArrivalCell.y),
                Is.True);
        }

        static int Manhattan(int x1, int y1, int x2, int y2) =>
            Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2);

        [Test]
        public void Arrival_from_dimension_square_is_walkable()
        {
            Vector3Int arrival = DistrictSquareHolyNexusTransition.NexusArrivalCell;
            Assert.That(HolyLandNexusLayout.IsWalkableCell(arrival.x, arrival.y), Is.True);
        }
    }
}
