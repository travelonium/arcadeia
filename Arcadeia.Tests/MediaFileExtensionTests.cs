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
using Microsoft.AspNetCore.StaticFiles;

namespace Arcadeia.Tests
{
    /// <summary>
    /// Tests the extension and content-type logic used by MediaFile.
    /// The properties are tested via the same inline logic rather than through
    /// the full MediaFile class (which requires complex DI).
    /// </summary>
    public class MediaFileExtensionUnitTests
    {
        // Mirrors MediaFile.Extension
        private static string? GetExtension(string? name)
        {
            var ext = System.IO.Path.GetExtension(name);
            if (string.IsNullOrEmpty(ext)) return null;
            if (string.IsNullOrEmpty(System.IO.Path.GetFileNameWithoutExtension(name))) return null;
            return ext.ToLower().TrimStart('.');
        }

        // Mirrors MediaFile.ContentType
        private static string? GetContentType(string? fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            new FileExtensionContentTypeProvider().TryGetContentType(fullPath, out string? contentType);
            return contentType;
        }

        // ── Extension logic ───────────────────────────────────────────────────────

        [Fact]
        public void ExtensionLogic_WithNoExtension_ReturnsNull()
        {
            Assert.Null(GetExtension("filename"));
            Assert.Null(GetExtension(""));
            Assert.Null(GetExtension(null));
        }

        [Fact]
        public void ExtensionLogic_WithValidFileName_ReturnsExtensionWithoutDot()
        {
            Assert.Equal("jpg", GetExtension("photo.jpg"));
            Assert.Equal("mp4", GetExtension("video.mp4"));
        }

        [Fact]
        public void ExtensionLogic_WithMultipleDots_ReturnsLastExtension()
        {
            Assert.Equal("gz",  GetExtension("archive.tar.gz"));
            Assert.Equal("txt", GetExtension("my.file.name.txt"));
        }

        [Fact]
        public void ExtensionLogic_WithUppercaseExtension_ReturnsLowercase()
        {
            Assert.Equal("jpg", GetExtension("photo.JPG"));
            Assert.Equal("mp4", GetExtension("video.MP4"));
        }

        [Fact]
        public void ExtensionLogic_DotFileHasNoExtension()
        {
            Assert.Null(GetExtension(".hidden"));
            Assert.Null(GetExtension(".gitignore"));
        }

        // ── Content-type logic ────────────────────────────────────────────────────

        [Fact]
        public void ContentTypeLogic_WithJpgFile_ReturnsImageJpeg()
        {
            Assert.Equal("image/jpeg", GetContentType("/photos/photo.jpg"));
        }

        [Fact]
        public void ContentTypeLogic_WithMp4File_ReturnsVideoMp4()
        {
            Assert.Equal("video/mp4", GetContentType("/videos/clip.mp4"));
        }

        [Fact]
        public void ContentTypeLogic_WithPngFile_ReturnsImagePng()
        {
            Assert.Equal("image/png", GetContentType("image.png"));
        }

        [Fact]
        public void ContentTypeLogic_WithNullPath_ReturnsNull()
        {
            Assert.Null(GetContentType(null));
        }

        [Fact]
        public void ContentTypeLogic_WithEmptyPath_ReturnsNull()
        {
            Assert.Null(GetContentType(""));
        }

        [Fact]
        public void ContentTypeLogic_WithUnknownExtension_ReturnsNull()
        {
            Assert.Null(GetContentType("file.unknownextension123"));
        }
    }
}
