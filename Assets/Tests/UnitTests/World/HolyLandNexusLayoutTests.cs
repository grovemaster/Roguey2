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
        public void Arrival_from_dimension_square_is_walkable()
        {
            Vector3Int arrival = DistrictSquareHolyNexusTransition.NexusArrivalCell;
            Assert.That(HolyLandNexusLayout.IsWalkableCell(arrival.x, arrival.y), Is.True);
        }
    }
}
