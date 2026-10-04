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
using Arcadeia.Configuration;

namespace Arcadeia.Tests
{
    public class ThumbnailCountTests
    {
        private static ThumbnailSettings Thumbnail(int count = 0, bool sprite = false) => new() { Count = count, Sprite = sprite };

        // The default thumbnail settings from appsettings.json.
        private static readonly ThumbnailSettings[] Photo = [Thumbnail(), Thumbnail(), Thumbnail(), Thumbnail(1)];
        private static readonly ThumbnailSettings[] Video = [Thumbnail(), Thumbnail(), Thumbnail(), Thumbnail(60, sprite: true), Thumbnail(24)];

        // ── PhotoFile ─────────────────────────────────────────────────────────────

        [Fact]
        public void Photo_CountsOneThumbnailForEachSizeWithoutACount()
        {
            // Previously counted as 1 while 4 were generated, reporting up to 400% progress.
            Assert.Equal(4, PhotoFile.CountThumbnails(Photo));
        }

        [Fact]
        public void Photo_CountsEveryThumbnailOfASizeWithACount()
        {
            Assert.Equal(5, PhotoFile.CountThumbnails([Thumbnail(), Thumbnail(4)]));
        }

        // ── VideoFile ─────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(3600.0, 3 + 60 + 24)]
        [InlineData(30.0,   3 + 30 + 24)]
        [InlineData(10.5,   3 + 10 + 10)]
        [InlineData(0.5,    3 + 1 + 1)]
        [InlineData(0.0,    3 + 1 + 1)]
        public void Video_CountsTheThumbnailsTheDurationAllows(double duration, int expected)
        {
            Assert.Equal(expected, VideoFile.CountThumbnails(Video, duration));
        }
    }
}
