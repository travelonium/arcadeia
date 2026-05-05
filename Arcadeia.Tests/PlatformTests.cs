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
using System.Runtime.InteropServices;

namespace Arcadeia.Tests
{
    public class PlatformTests
    {
        // ── Type checks ───────────────────────────────────────────────────────────

        [Fact]
        public void Platform_Properties_ReturnExpectedTypes()
        {
            Assert.IsType<string>(Platform.Extension.Executable);
            Assert.IsType<string>(Platform.Separator.Path);
            Assert.IsType<string>(Platform.Separator.Root);
        }

        // ── Extension.Executable ─────────────────────────────────────────────────

        [Fact]
        public void Extension_Executable_ReturnsCorrectValueForCurrentPlatform()
        {
            string expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
            Assert.Equal(expected, Platform.Extension.Executable);
        }

        [Fact]
        public void Extension_Executable_IsConsistentAcrossMultipleCalls()
        {
            Assert.Equal(Platform.Extension.Executable, Platform.Extension.Executable);
        }

        [Theory]
        [InlineData("",      ".exe", "")]
        [InlineData("tool",  "tool.exe", "tool")]
        [InlineData("myapp", "myapp.exe", "myapp")]
        public void Extension_Executable_CanBeUsedToBuildExecutableName(string baseName, string expectedWindows, string expectedUnix)
        {
            string expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? expectedWindows : expectedUnix;
            Assert.Equal(expected, baseName + Platform.Extension.Executable);
        }

        // ── Separator.Path ────────────────────────────────────────────────────────

        [Fact]
        public void Separator_Path_ReturnsCorrectValueForCurrentPlatform()
        {
            string expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";
            Assert.Equal(expected, Platform.Separator.Path);
        }

        [Fact]
        public void Separator_Path_IsConsistentAcrossMultipleCalls()
        {
            Assert.Equal(Platform.Separator.Path, Platform.Separator.Path);
        }

        [Theory]
        [InlineData("",       "root.config")]
        [InlineData("folder", "file.txt")]
        [InlineData("path",   "document.pdf")]
        public void Separator_Path_CanBeUsedToBuildPaths(string folder, string filename)
        {
            string result = folder + Platform.Separator.Path + filename;
            Assert.Contains(filename, result);
        }

        // ── Separator.Root ────────────────────────────────────────────────────────

        [Fact]
        public void Separator_Root_ReturnsCorrectValueForCurrentPlatform()
        {
            string expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "" : "/";
            Assert.Equal(expected, Platform.Separator.Root);
        }

        [Fact]
        public void Separator_Root_IsConsistentAcrossMultipleCalls()
        {
            Assert.Equal(Platform.Separator.Root, Platform.Separator.Root);
        }
    }
}
