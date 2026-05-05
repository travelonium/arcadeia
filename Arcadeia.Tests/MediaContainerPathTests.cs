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
    public class MediaContainerPathTests
    {
        // ── Null / empty ──────────────────────────────────────────────────────────

        [Fact]
        public void GetPathComponents_WithNullPath_ReturnsNullComponents()
        {
            var (parent, child) = MediaContainer.GetPathComponents(null);
            Assert.Null(parent);
            Assert.Null(child);
        }

        [Fact]
        public void GetPathComponents_WithEmptyPath_ReturnsNullComponents()
        {
            var (parent, child) = MediaContainer.GetPathComponents("");
            Assert.Null(parent);
            Assert.Null(child);
        }

        // ── Linux / OSX paths ─────────────────────────────────────────────────────

        [Theory]
        [InlineData("/file.txt",              null,            "file.txt")]
        [InlineData("/folder/file.txt",        "/folder/",      "file.txt")]
        [InlineData("/folder/",               null,            "folder/")]
        [InlineData("/folder1/folder2/file.txt", "/folder1/folder2/", "file.txt")]
        public void GetPathComponents_ReturnsCorrectComponents(string path, string? expectedParent, string? expectedSelf)
        {
            var (parent, child) = MediaContainer.GetPathComponents(path);
            Assert.Equal(expectedParent, parent);
            Assert.Equal(expectedSelf,   child);
        }

        // ── Windows paths ─────────────────────────────────────────────────────────

        [Theory]
        [InlineData("C:\\",                   null,            "C:\\")]
        [InlineData("C:\\Test\\file.txt",      "C:\\Test\\",    "file.txt")]
        [InlineData("\\\\Server1\\",           null,            "\\\\Server1\\")]
        [InlineData("\\\\Server1\\file.txt",   "\\\\Server1\\", "file.txt")]
        public void GetPathComponents_ReturnsCorrectComponents_Windows(string path, string? expectedParent, string? expectedSelf)
        {
            var (parent, child) = MediaContainer.GetPathComponents(path);
            Assert.Equal(expectedParent, parent);
            Assert.Equal(expectedSelf,   child);
        }

        // ── Deeper nesting ────────────────────────────────────────────────────────

        [Fact]
        public void GetPathComponents_DeepUnixPath_ReturnsDirectParent()
        {
            var (parent, child) = MediaContainer.GetPathComponents("/a/b/c/d/file.txt");
            Assert.Equal("/a/b/c/d/", parent);
            Assert.Equal("file.txt",  child);
        }

        [Fact]
        public void GetPathComponents_UnixFolderPath_ParentIsNull()
        {
            var (parent, child) = MediaContainer.GetPathComponents("/toplevel/");
            Assert.Null(parent);
            Assert.Equal("toplevel/", child);
        }

        [Fact]
        public void GetPathComponents_NestedUnixFolderPath_ReturnsParent()
        {
            var (parent, child) = MediaContainer.GetPathComponents("/a/b/");
            Assert.Equal("/a/", parent);
            Assert.Equal("b/",  child);
        }

        // ── Filename-only (no leading slash) ─────────────────────────────────────

        [Fact]
        public void GetPathComponents_FilenameWithNoSeparator_ReturnsBothNull()
        {
            var (parent, child) = MediaContainer.GetPathComponents("file.txt");
            Assert.Null(parent);
            Assert.Null(child);
        }

        // ── Spaces in paths ───────────────────────────────────────────────────────

        [Fact]
        public void GetPathComponents_PathWithSpaces_ParsesCorrectly()
        {
            var (parent, child) = MediaContainer.GetPathComponents("/my photos/vacation.jpg");
            Assert.Equal("/my photos/", parent);
            Assert.Equal("vacation.jpg", child);
        }
    }
}
