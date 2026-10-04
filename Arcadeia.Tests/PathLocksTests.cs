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

namespace Arcadeia.Tests
{
    public class PathLocksTests
    {
        // ── IsWithin ──────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("/a/Old/",           "/a/Old/",      true)]
        [InlineData("/a/Old/clip.mp4",   "/a/Old/",      true)]
        [InlineData("/a/Old/Sub/x.jpg",  "/a/Old/",      true)]
        [InlineData("/a/Older/x.jpg",    "/a/Old/",      false)]
        [InlineData("/a/",               "/a/Old/",      false)]
        [InlineData("/a/clip.mp4",       "/a/clip.mp4",  true)]
        [InlineData("/a/clip.mp4.part",  "/a/clip.mp4",  false)]
        [InlineData("/a/OLD/x.jpg",      "/a/Old/",      false)]
        [InlineData("C:\\Old\\x.jpg",    "C:\\Old\\",    true)]
        public void IsWithin_ReturnsExpectedResult(string path, string other, bool expected)
        {
            Assert.Equal(expected, PathLocks.IsWithin(path, other));
        }

        // ── Lock / IsLocked ───────────────────────────────────────────────────────

        [Fact]
        public void IsLocked_WithNothingLocked_ReturnsFalse()
        {
            var locks = new PathLocks();

            Assert.False(locks.IsLocked("/a/Old/"));
            Assert.False(locks.IsLocked(null));
        }

        [Fact]
        public void Lock_LocksAllThePathsUntilDisposed()
        {
            var locks = new PathLocks();

            using (locks.Lock("/a/Old/", "/a/New/"))
            {
                Assert.True(locks.IsLocked("/a/Old/clip.mp4"));
                Assert.True(locks.IsLocked("/a/New/clip.mp4"));
                Assert.False(locks.IsLocked("/a/Other/clip.mp4"));
            }

            Assert.False(locks.IsLocked("/a/Old/clip.mp4"));
            Assert.False(locks.IsLocked("/a/New/clip.mp4"));
        }

        [Fact]
        public void Lock_OverlappingLocksOfTheSamePath_StayLockedUntilAllAreReleased()
        {
            var locks = new PathLocks();
            var first = locks.Lock("/a/Old/");
            var second = locks.Lock("/a/Old/");

            first.Dispose();
            Assert.True(locks.IsLocked("/a/Old/clip.mp4"));

            second.Dispose();
            Assert.False(locks.IsLocked("/a/Old/clip.mp4"));
        }

        [Fact]
        public void Lock_DisposingTwice_ReleasesOnlyOnce()
        {
            var locks = new PathLocks();
            var first = locks.Lock("/a/Old/");
            using var second = locks.Lock("/a/Old/");

            first.Dispose();
            first.Dispose();

            Assert.True(locks.IsLocked("/a/Old/clip.mp4"));
        }
    }
}
