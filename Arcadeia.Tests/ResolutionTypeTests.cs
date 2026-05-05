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
    public class ResolutionTypeTests
    {
        // ── Constructors ──────────────────────────────────────────────────────────

        [Fact]
        public void DefaultConstructor_InitializesWithNullValues()
        {
            var r = new ResolutionType();
            Assert.Null(r.Width);
            Assert.Null(r.Height);
        }

        [Fact]
        public void WidthHeightConstructor_SetsCorrectValues()
        {
            var r = new ResolutionType(1920, 1080);
            Assert.Equal(1920, r.Width);
            Assert.Equal(1080, r.Height);
        }

        [Fact]
        public void WidthHeightConstructor_WithNullValues_HandlesCorrectly()
        {
            var r = new ResolutionType(null, null);
            Assert.Null(r.Width);
            Assert.Null(r.Height);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            var r = new ResolutionType { Width = 2560, Height = 1440 };
            Assert.Equal(2560, r.Width);
            Assert.Equal(1440, r.Height);
        }

        [Theory]
        [InlineData("1920x1080", 1920, 1080)]
        [InlineData("1280x720",  1280,  720)]
        [InlineData("3840x2160", 3840, 2160)]
        [InlineData("640x480",    640,  480)]
        public void StringConstructor_WithValidFormat_ParsesCorrectly(string resolution, long expectedWidth, long expectedHeight)
        {
            var r = new ResolutionType(resolution);
            Assert.Equal(expectedWidth,  r.Width);
            Assert.Equal(expectedHeight, r.Height);
        }

        [Theory]
        [InlineData("x1080")]
        [InlineData("invalid")]
        [InlineData("widthxheight")]
        [InlineData("")]
        [InlineData("1920")]
        [InlineData("0x0")]
        public void StringConstructor_WithInvalidFormat_SetsZeroValues(string resolution)
        {
            var r = new ResolutionType(resolution);
            Assert.Equal(0, r.Width);
            Assert.Equal(0, r.Height);
        }

        [Fact]
        public void StringConstructor_WithTrailingX_SetsZeroValues()
        {
            var r = new ResolutionType("1920x");
            Assert.Equal(0, r.Width);
            Assert.Equal(0, r.Height);
        }

        // ── ToString ──────────────────────────────────────────────────────────────

        [Fact]
        public void ToString_WithValidResolution_ReturnsFormattedString()
        {
            Assert.Equal("1920x1080", new ResolutionType(1920, 1080).ToString());
        }

        [Fact]
        public void ToString_WithNullValues_ReturnsEmptyString()
        {
            Assert.Equal("", new ResolutionType().ToString());
        }

        [Fact]
        public void ToString_WithZeroValues_ReturnsEmptyString()
        {
            Assert.Equal("", new ResolutionType(0, 0).ToString());
        }

        [Theory]
        [InlineData(1920, -1)]
        [InlineData(0,  1080)]
        [InlineData(1920,  0)]
        [InlineData(-1, 1080)]
        public void ToString_WithInvalidValues_ReturnsEmptyString(long width, long height)
        {
            Assert.Equal("", new ResolutionType(width, height).ToString());
        }

        // ── Equals / == / != ─────────────────────────────────────────────────────

        [Fact]
        public void Equals_SameValues_ReturnsTrue()
        {
            Assert.True(new ResolutionType(1920, 1080).Equals(new ResolutionType(1920, 1080)));
        }

        [Fact]
        public void Equals_SameInstance_ReturnsTrue()
        {
            var r = new ResolutionType(1920, 1080);
            Assert.True(r.Equals(r));
        }

        [Fact]
        public void Equals_DifferentValues_ReturnsFalse()
        {
            Assert.False(new ResolutionType(1920, 1080).Equals(new ResolutionType(1280, 720)));
        }

        [Fact]
        public void Equals_WithNull_ReturnsFalse()
        {
            Assert.False(new ResolutionType(1920, 1080).Equals(null));
        }

        [Fact]
        public void ObjectEquals_WithResolutionType_UsesTypedEquals()
        {
            object a = new ResolutionType(1920, 1080);
            object b = new ResolutionType(1920, 1080);
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void ObjectEquals_WithNonResolutionType_ReturnsFalse()
        {
            Assert.False(new ResolutionType(1920, 1080).Equals("1920x1080"));
        }

        [Fact]
        public void OperatorEquals_SameValues_ReturnsTrue()
        {
            Assert.True(new ResolutionType(1920, 1080) == new ResolutionType(1920, 1080));
        }

        [Fact]
        public void OperatorEquals_DifferentValues_ReturnsFalse()
        {
            Assert.False(new ResolutionType(1920, 1080) == new ResolutionType(1280, 720));
        }

        [Fact]
        public void OperatorEquals_BothNull_ReturnsTrue()
        {
            ResolutionType? a = null;
            ResolutionType? b = null;
            Assert.True(a == b);
        }

        [Fact]
        public void OperatorEquals_OneNull_ReturnsFalse()
        {
            ResolutionType? a = new ResolutionType(1920, 1080);
            ResolutionType? b = null;
            Assert.False(a == b);
            Assert.False(b == a);
        }

        [Fact]
        public void OperatorNotEquals_SameValues_ReturnsFalse()
        {
            Assert.False(new ResolutionType(1920, 1080) != new ResolutionType(1920, 1080));
        }

        [Fact]
        public void OperatorNotEquals_DifferentValues_ReturnsTrue()
        {
            Assert.True(new ResolutionType(1920, 1080) != new ResolutionType(1280, 720));
        }

        // ── GetHashCode ───────────────────────────────────────────────────────────

        [Fact]
        public void GetHashCode_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new ResolutionType(1920, 1080).GetHashCode());
        }

        // ── CompareTo ─────────────────────────────────────────────────────────────

        [Fact]
        public void CompareTo_WithNull_ReturnsPositive()
        {
            Assert.True(new ResolutionType(1920, 1080).CompareTo(null) > 0);
        }

        [Fact]
        public void CompareTo_WithNonResolutionType_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ResolutionType(1920, 1080).CompareTo("not a resolution"));
        }

        [Theory]
        [InlineData(1920, 1080, 1280, 720)]
        [InlineData(2560, 1440, 1366, 768)]
        [InlineData(3840, 2160, 1920, 1080)]
        public void CompareTo_HigherResolution_ReturnsPositive(long w1, long h1, long w2, long h2)
        {
            Assert.True(new ResolutionType(w1, h1).CompareTo(new ResolutionType(w2, h2)) > 0);
        }

        [Fact]
        public void CompareTo_SmallerSum_ReturnsNegative()
        {
            Assert.True(new ResolutionType(640, 480).CompareTo(new ResolutionType(1920, 1080)) < 0);
        }

        [Fact]
        public void CompareTo_LargerSum_ReturnsPositive()
        {
            Assert.True(new ResolutionType(1920, 1080).CompareTo(new ResolutionType(640, 480)) > 0);
        }

        [Fact]
        public void CompareTo_SameResolution_ReturnsZero()
        {
            Assert.Equal(0, new ResolutionType(1920, 1080).CompareTo(new ResolutionType(1920, 1080)));
        }

        [Fact]
        public void CompareTo_SameSum_ReturnsZero()
        {
            // 1000+200 == 600+600 — same sum, so CompareTo returns 0
            Assert.Equal(0, new ResolutionType(1000, 200).CompareTo(new ResolutionType(600, 600)));
        }

        [Fact]
        public void CompareTo_WithZeroValues_WorksCorrectly()
        {
            Assert.Equal(0, new ResolutionType(0, 0).CompareTo(new ResolutionType(0, 0)));
            Assert.True(new ResolutionType(1920, 1080).CompareTo(new ResolutionType(0, 0)) > 0);
        }
    }
}
