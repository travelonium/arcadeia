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

using Moq;
using Xunit;
using Arcadeia.Hubs;
using Arcadeia.Services;
using Arcadeia.Controllers;
using Arcadeia.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcadeia.Tests
{
    public class LibraryControllerFolderTests : IDisposable
    {
        private readonly string _root;
        private readonly string _outside;

        public LibraryControllerFolderTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "arcadeia-tests-" + Guid.NewGuid().ToString("N"));
            _outside = Path.Combine(Path.GetTempPath(), "arcadeia-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            Directory.CreateDirectory(_outside);
        }

        public void Dispose()
        {
            Directory.Delete(_root, true);
            Directory.Delete(_outside, true);
            GC.SuppressFinalize(this);
        }

        private static Settings CreateSettings(string folder, bool readOnly)
        {
            // Bypass the required members as only the scanner and security settings are relevant here.
            var settings = Activator.CreateInstance<Settings>();
            settings.Scanner = new ScannerSettings { Folders = [folder], WatchedFolders = [], IgnoredPatterns = [] };
            settings.Security = new SecuritySettings
            {
                Library = new SecurityLibrarySettings { ReadOnly = readOnly },
                Settings = new SecuritySettingsSettings { ReadOnly = false },
            };
            return settings;
        }

        private LibraryController CreateController(Mock<IMediaLibrary>? mediaLibrary = null, bool readOnly = false)
        {
            var settings = new Mock<IOptionsMonitor<Settings>>();
            settings.Setup(m => m.CurrentValue).Returns(CreateSettings(_root, readOnly));

            return new LibraryController(Mock.Of<IServiceProvider>(),
                                         (mediaLibrary ?? new Mock<IMediaLibrary>()).Object,
                                         settings.Object,
                                         Mock.Of<IThumbnailsDatabase>(),
                                         new NotificationService(Mock.Of<IHubContext<SignalRHub>>()),
                                         NullLogger<MediaContainer>.Instance);
        }

        // The controller prepends the root separator to the route path.
        private static string Route(string path) => path.TrimStart(Path.DirectorySeparatorChar);

        private static int? StatusCode(IActionResult result) => (result as ObjectResult)?.StatusCode;

        // ── GetUniqueFolderName ───────────────────────────────────────────────────

        [Fact]
        public void GetUniqueFolderName_WhenNameIsFree_ReturnsName()
        {
            Assert.Equal("New Folder", LibraryController.GetUniqueFolderName(_root, "New Folder"));
        }

        [Fact]
        public void GetUniqueFolderName_WhenFolderExists_AppendsIndex()
        {
            Directory.CreateDirectory(Path.Combine(_root, "New Folder"));

            Assert.Equal("New Folder (1)", LibraryController.GetUniqueFolderName(_root, "New Folder"));
        }

        [Fact]
        public void GetUniqueFolderName_WhenIndexedFoldersExist_ReturnsNextFreeIndex()
        {
            Directory.CreateDirectory(Path.Combine(_root, "New Folder"));
            Directory.CreateDirectory(Path.Combine(_root, "New Folder (1)"));
            Directory.CreateDirectory(Path.Combine(_root, "New Folder (2)"));

            Assert.Equal("New Folder (3)", LibraryController.GetUniqueFolderName(_root, "New Folder"));
        }

        [Fact]
        public void GetUniqueFolderName_WhenFileHasTheName_AppendsIndex()
        {
            File.WriteAllText(Path.Combine(_root, "New Folder"), "");

            Assert.Equal("New Folder (1)", LibraryController.GetUniqueFolderName(_root, "New Folder"));
        }

        // ── IsNameValid ─────────────────────────────────────────────────────

        [Theory]
        [InlineData("New Folder", true)]
        [InlineData("Holiday 2024", true)]
        [InlineData("..hidden", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(".", false)]
        [InlineData("..", false)]
        [InlineData("a/b", false)]
        [InlineData("a\\b", false)]
        [InlineData("../escape", false)]
        [InlineData("a\0b", false)]
        public void IsNameValid_ReturnsExpectedResult(string name, bool expected)
        {
            Assert.Equal(expected, LibraryController.IsNameValid(name));
        }

        // ── CreateFolder ──────────────────────────────────────────────────────────

        [Fact]
        public void CreateFolder_WhenLibraryIsReadOnly_Returns403()
        {
            var result = CreateController(readOnly: true).CreateFolder(null, Route(_root));

            Assert.Equal(403, StatusCode(result));
            Assert.Empty(Directory.EnumerateFileSystemEntries(_root));
        }

        [Fact]
        public void CreateFolder_WhenPathIsOutsideTheLibrary_Returns400()
        {
            var result = CreateController().CreateFolder(null, Route(_outside));

            Assert.Equal(400, StatusCode(result));
            Assert.Empty(Directory.EnumerateFileSystemEntries(_outside));
        }

        [Fact]
        public void CreateFolder_WhenParentDoesNotExist_Returns404()
        {
            var result = CreateController().CreateFolder(null, Route(Path.Combine(_root, "missing")));

            Assert.Equal(404, StatusCode(result));
        }

        [Theory]
        [InlineData("..")]
        [InlineData("a/b")]
        [InlineData("../escape")]
        public void CreateFolder_WithInvalidName_Returns400(string name)
        {
            var result = CreateController().CreateFolder(new LibraryController.CreateFolderRequest(name), Route(_root));

            Assert.Equal(400, StatusCode(result));
            Assert.Empty(Directory.EnumerateFileSystemEntries(_root));
        }

        [Fact]
        public void CreateFolder_WhenNamedFolderExists_Returns409()
        {
            Directory.CreateDirectory(Path.Combine(_root, "Photos"));

            var result = CreateController().CreateFolder(new LibraryController.CreateFolderRequest("Photos"), Route(_root));

            Assert.Equal(409, StatusCode(result));
            Assert.Single(Directory.EnumerateFileSystemEntries(_root));
        }

        // The MediaLibrary mock throws after capturing the path to avoid instantiating a real
        // MediaFolder (which needs Solr), so these only verify what's created on disk and inserted.

        [Theory]
        [InlineData(null, "New Folder")]
        [InlineData("", "New Folder")]
        [InlineData("  Photos  ", "Photos")]
        public void CreateFolder_CreatesAndInsertsTheFolder(string? name, string expected)
        {
            string? inserted = null;
            var mediaLibrary = new Mock<IMediaLibrary>();
            mediaLibrary.Setup(m => m.InsertMediaFolder(It.IsAny<string>()))
                        .Callback<string>(path => inserted = path)
                        .Throws(new InvalidOperationException("Not indexed in tests."));

            var request = name == null ? null : new LibraryController.CreateFolderRequest(name);
            CreateController(mediaLibrary).CreateFolder(request, Route(_root));

            Assert.True(Directory.Exists(Path.Combine(_root, expected)));
            Assert.Equal(Path.Combine(_root, expected), inserted?.TrimEnd(Path.DirectorySeparatorChar));
        }

        [Fact]
        public void CreateFolder_WithoutName_WhenNewFolderExists_CreatesIndexedFolder()
        {
            Directory.CreateDirectory(Path.Combine(_root, "New Folder"));
            var mediaLibrary = new Mock<IMediaLibrary>();
            mediaLibrary.Setup(m => m.InsertMediaFolder(It.IsAny<string>()))
                        .Throws(new InvalidOperationException("Not indexed in tests."));

            CreateController(mediaLibrary).CreateFolder(null, Route(_root));

            Assert.True(Directory.Exists(Path.Combine(_root, "New Folder (1)")));
        }

        [Fact]
        public void CreateFolder_WhenIndexingFails_Returns500()
        {
            var mediaLibrary = new Mock<IMediaLibrary>();
            mediaLibrary.Setup(m => m.InsertMediaFolder(It.IsAny<string>()))
                        .Throws(new InvalidOperationException("Solr is down."));

            var result = CreateController(mediaLibrary).CreateFolder(null, Route(_root));

            Assert.Equal(500, StatusCode(result));
        }
    }
}
