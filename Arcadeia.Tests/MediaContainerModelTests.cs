/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

using Xunit;
using Model = Arcadeia.Models.MediaContainer;

namespace Arcadeia.Tests
{
    public class MediaContainerModelTests
    {
        private static Model Sample() => new()
        {
            Id          = "abc123",
            Name        = "video.mp4",
            Type        = "VideoFile",
            Parent      = "folder-id",
            ParentType  = "MediaFolder",
            Parents     = new[] { "root-id", "folder-id" },
            Path        = "/videos/",
            FullPath    = "/videos/video.mp4",
            Size        = 1024,
            ContentType = "video/mp4",
            Extension   = "mp4",
            Views       = 5,
            DateAdded   = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        // ── Equals ────────────────────────────────────────────────────────────────

        [Fact]
        public void Equals_SameInstance_ReturnsTrue()
        {
            var m = Sample();
            Assert.True(m.Equals(m));
        }

        [Fact]
        public void Equals_IdenticalObjects_ReturnsTrue()
        {
            Assert.True(Sample().Equals(Sample()));
        }

        [Fact]
        public void Equals_DifferentName_ReturnsFalse()
        {
            var a = Sample();
            var b = Sample();
            b.Name = "other.mp4";
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentSize_ReturnsFalse()
        {
            var a = Sample();
            var b = Sample();
            b.Size = 9999;
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            Assert.False(Sample().Equals(null));
        }

        [Fact]
        public void ObjectEquals_WithMediaContainer_ReturnsTrue()
        {
            object a = Sample();
            object b = Sample();
            Assert.True(a.Equals(b));
        }

        // ── Operators ─────────────────────────────────────────────────────────────

        [Fact]
        public void OperatorEquals_IdenticalObjects_ReturnsTrue()
        {
            Assert.True(Sample() == Sample());
        }

        [Fact]
        public void OperatorNotEquals_DifferentObjects_ReturnsTrue()
        {
            var a = Sample();
            var b = Sample();
            b.Name = "different.mp4";
            Assert.True(a != b);
        }

        [Fact]
        public void OperatorEquals_BothNull_ReturnsTrue()
        {
            Model? a = null;
            Model? b = null;
            Assert.True(a == b);
        }

        // ── GetHashCode ───────────────────────────────────────────────────────────

        [Fact]
        public void GetHashCode_SameInstance_IsStable()
        {
            var m = Sample();
            Assert.Equal(m.GetHashCode(), m.GetHashCode());
        }

        [Fact]
        public void GetHashCode_EqualObjects_WithoutArrayFields_ReturnSameHash()
        {
            // Models without string[] fields produce stable, equal hash codes.
            // Objects with string[] fields (e.g. Parents) produce different hashes even
            // when Equals() returns true, because arrays use reference identity in GetHashCode.
            var a = new Model { Id = "x", Name = "video.mp4", Type = "VideoFile", Size = 1024 };
            var b = new Model { Id = "x", Name = "video.mp4", Type = "VideoFile", Size = 1024 };
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void GetHashCode_EqualObjects_ReturnSameHash()
        {
            Assert.Equal(Sample().GetHashCode(), Sample().GetHashCode());
        }

        // ── DateTime truncation ───────────────────────────────────────────────────

        [Fact]
        public void Equals_DateTimesWithDifferentMilliseconds_AreConsideredEqual()
        {
            var a = Sample();
            var b = Sample();
            a.DateAdded = new DateTime(2024, 1, 1, 12, 0, 0, 100, DateTimeKind.Utc);
            b.DateAdded = new DateTime(2024, 1, 1, 12, 0, 0, 999, DateTimeKind.Utc);
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_DateTimesWithDifferentSeconds_AreNotEqual()
        {
            var a = Sample();
            var b = Sample();
            a.DateAdded = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            b.DateAdded = new DateTime(2024, 1, 1, 12, 0, 1, DateTimeKind.Utc);
            Assert.False(a.Equals(b));
        }

        // ── Array fields ──────────────────────────────────────────────────────────

        [Fact]
        public void Equals_NullAndEmptyStringArrays_AreConsideredEqual()
        {
            var a = Sample();
            var b = Sample();
            a.Flags = null;
            b.Flags = Array.Empty<string>();
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_DifferentParentsArrays_ReturnsFalse()
        {
            var a = Sample();
            var b = Sample();
            b.Parents = new[] { "root-id" };
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_SameParentsArrayInDifferentOrder_ReturnsFalse()
        {
            var a = Sample();
            var b = Sample();
            a.Parents = new[] { "root-id", "folder-id" };
            b.Parents = new[] { "folder-id", "root-id" };
            Assert.False(a.Equals(b));
        }

        // ── Differences ───────────────────────────────────────────────────────────

        [Fact]
        public void Differences_IdenticalObjects_ReturnsEmpty()
        {
            Assert.Empty(Sample().Differences(Sample()));
        }

        [Fact]
        public void Differences_SingleFieldChanged_ReturnsOneDifference()
        {
            var a = Sample();
            var b = Sample();
            b.Name = "changed.mp4";
            var diffs = a.Differences(b).ToList();
            Assert.Single(diffs);
            Assert.Contains("Name", diffs[0]);
        }

        [Fact]
        public void Differences_MultipleFieldsChanged_ReturnsAllDifferences()
        {
            var a = Sample();
            var b = Sample();
            b.Name = "changed.mp4";
            b.Size = 9999;
            Assert.Equal(2, a.Differences(b).Count());
        }

        [Fact]
        public void Differences_OutputContainsOldAndNewValues()
        {
            var a = Sample();
            var b = Sample();
            b.Name = "new.mp4";
            var diff = a.Differences(b).First();
            Assert.Contains("video.mp4", diff);
            Assert.Contains("new.mp4",   diff);
        }
    }
}
