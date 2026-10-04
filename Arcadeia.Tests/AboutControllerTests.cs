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
using Arcadeia.Controllers;

namespace Arcadeia.Tests
{
    public class AboutControllerTests
    {
        [Theory]
        [InlineData("ffmpeg version 8.1.1 Copyright (c) 2000-2026 the FFmpeg developers\nbuilt with gcc 14", "8.1.1")]
        [InlineData("ffmpeg version n8.1.1-static https://johnvansickle.com/ffmpeg/", "n8.1.1-static")]
        [InlineData("2026.07.04\n", "2026.07.04")]
        [InlineData("whisper.cpp version: 1.9.2", "1.9.2")]
        [InlineData("\n\nv1.9.1\n", "1.9.1")]
        public void ParseVersion_ExtractsTheVersion(string output, string expected)
        {
            Assert.Equal(expected, AboutController.ParseVersion(output));
        }

        [Theory]
        [InlineData("")]
        [InlineData("error: unknown argument: --version")]
        public void ParseVersion_ReturnsNullWithoutAVersion(string output)
        {
            Assert.Null(AboutController.ParseVersion(output));
        }
    }
}
