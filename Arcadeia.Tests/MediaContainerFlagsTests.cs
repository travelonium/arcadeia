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
using Arcadeia;

namespace Arcadeia.Tests
{
    public class MediaContainerFlagsTests
    {
        // ── Flag enum values ──────────────────────────────────────────────────────

        [Theory]
        [InlineData(MediaContainerFlags.Flag.None,     0)]
        [InlineData(MediaContainerFlags.Flag.Deleted,  1)]
        [InlineData(MediaContainerFlags.Flag.Favorite, 2)]
        public void Flag_EnumValues_HaveCorrectBitValues(MediaContainerFlags.Flag flag, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)flag);
        }

        // ── Default constructor ───────────────────────────────────────────────────

        [Fact]
        public void DefaultConstructor_InitializesWithNoFlags()
        {
            var flags = new MediaContainerFlags();
            Assert.False(flags.Favorite);
            Assert.False(flags.Deleted);
            Assert.Empty(flags.All);
        }

        // ── String-array constructor ──────────────────────────────────────────────

        [Fact]
        public void StringArrayConstructor_WithNullArray_CreatesEmptyFlags()
        {
            var flags = new MediaContainerFlags(null);
            Assert.False(flags.Favorite);
            Assert.False(flags.Deleted);
        }

        [Fact]
        public void StringArrayConstructor_WithEmptyArray_CreatesEmptyFlags()
        {
            var flags = new MediaContainerFlags(Array.Empty<string>());
            Assert.False(flags.Favorite);
            Assert.False(flags.Deleted);
        }

        [Fact]
        public void StringArrayConstructor_WithValidFlags_SetsFlags()
        {
            var flags = new MediaContainerFlags(new[] { "Favorite", "Deleted" });
            Assert.True(flags.Favorite);
            Assert.True(flags.Deleted);
        }

        [Theory]
        [InlineData("favorite", true,  false)]
        [InlineData("FAVORITE", true,  false)]
        [InlineData("deleted",  false, true)]
        [InlineData("DELETED",  false, true)]
        public void StringArrayConstructor_IsCaseInsensitive(string flagString, bool expectedFavorite, bool expectedDeleted)
        {
            var flags = new MediaContainerFlags(new[] { flagString });
            Assert.Equal(expectedFavorite, flags.Favorite);
            Assert.Equal(expectedDeleted,  flags.Deleted);
        }

        [Fact]
        public void StringArrayConstructor_WithDuplicates_OnlySetsOnce()
        {
            var flags = new MediaContainerFlags(new[] { "Favorite", "Favorite" });
            Assert.Single(flags.All);
        }

        [Fact]
        public void StringArrayConstructor_WithMixedValidAndInvalidFlags_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new MediaContainerFlags(new[] { "Favorite", "InvalidFlag" }));
        }

        // ── Favorite ─────────────────────────────────────────────────────────────

        [Fact]
        public void Favorite_CanBeSetAndUnset()
        {
            var flags = new MediaContainerFlags();
            flags.Favorite = true;
            Assert.True(flags.Favorite);
            flags.Favorite = false;
            Assert.False(flags.Favorite);
        }

        // ── Deleted ───────────────────────────────────────────────────────────────

        [Fact]
        public void Deleted_CanBeSetAndUnset()
        {
            var flags = new MediaContainerFlags();
            flags.Deleted = true;
            Assert.True(flags.Deleted);
            flags.Deleted = false;
            Assert.False(flags.Deleted);
        }

        // ── Multiple flags ────────────────────────────────────────────────────────

        [Fact]
        public void MultipleFlagsCanBeSetSimultaneously()
        {
            var flags = new MediaContainerFlags();
            flags.Favorite = true;
            flags.Deleted  = true;
            Assert.True(flags.Favorite);
            Assert.True(flags.Deleted);
            Assert.Equal(2, flags.All.Count);
        }

        [Fact]
        public void SettingFlagToSameValueHasNoEffect()
        {
            var flags = new MediaContainerFlags();
            flags.Favorite = true;
            flags.Favorite = true;
            Assert.Single(flags.All);
        }

        // ── All property ─────────────────────────────────────────────────────────

        [Fact]
        public void All_Property_ReflectsCurrentState()
        {
            var flags = new MediaContainerFlags();
            flags.Favorite = true;
            Assert.Contains(MediaContainerFlags.Flag.Favorite, flags.All);
            flags.Favorite = false;
            Assert.DoesNotContain(MediaContainerFlags.Flag.Favorite, flags.All);
        }

        // ── ToArray ───────────────────────────────────────────────────────────────

        [Fact]
        public void ToArray_WithNoFlags_ReturnsEmptyArray()
        {
            Assert.Empty(new MediaContainerFlags().ToArray());
        }

        [Fact]
        public void ToArray_WithSingleFlag_ReturnsArrayWithOneElement()
        {
            var flags = new MediaContainerFlags();
            flags.Favorite = true;
            var arr = flags.ToArray();
            Assert.Single(arr);
            Assert.Contains("Favorite", arr);
        }

        [Fact]
        public void ToArray_WithMultipleFlags_ReturnsAllFlags()
        {
            var flags = new MediaContainerFlags();
            flags.Favorite = true;
            flags.Deleted  = true;
            var arr = flags.ToArray();
            Assert.Equal(2, arr.Length);
            Assert.Contains("Favorite", arr);
            Assert.Contains("Deleted",  arr);
        }

        [Fact]
        public void ToArray_AfterUnsettingFlag_DoesNotIncludeUnsetFlag()
        {
            var flags = new MediaContainerFlags();
            flags.Favorite = true;
            flags.Deleted  = true;
            flags.Deleted  = false;
            var arr = flags.ToArray();
            Assert.Single(arr);
            Assert.DoesNotContain("Deleted", arr);
        }

        // ── Isolation ─────────────────────────────────────────────────────────────

        [Fact]
        public void IndependentInstances_DoNotAffectEachOther()
        {
            var a = new MediaContainerFlags();
            var b = new MediaContainerFlags();
            a.Favorite = true;
            Assert.False(b.Favorite);
        }
    }
}
