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
using SolrNet;
using Arcadeia.Hubs;
using Arcadeia.Solr;
using Arcadeia.Services;
using Arcadeia.Controllers;
using Arcadeia.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcadeia.Tests
{
    public class MediaFolderRenameTests : IDisposable
    {
        private readonly string _root;
        private readonly Dictionary<string, Models.MediaContainer> _index = [];
        private readonly Mock<IMediaLibrary> _mediaLibrary = new();
        private readonly Mock<IServiceProvider> _services = new();
        private readonly Mock<IOptionsMonitor<Settings>> _settings = new();
        private readonly List<ISolrQuery> _queries = [];
        private readonly PathLocks _locks = new();
        private bool _failBatchUpdates = false;
        private bool _failUpdates = false;
        private Action? _duringBatchUpdate;

        public MediaFolderRenameTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "arcadeia-tests-" + Guid.NewGuid().ToString("N")) + "/";

            // An in-memory Solr index that only supports the queries a folder rename makes.
            var solr = new Mock<ISolrIndexService<Models.MediaContainer>>();
            solr.Setup(m => m.Get(It.IsAny<ISolrQuery>(), It.IsAny<ICollection<SortOrder>>()))
                .Returns((ISolrQuery query, ICollection<SortOrder> _) => Results(query switch
                {
                    SolrQueryByField { FieldName: "id" } field => _index.Values.Where(doc => doc.Id == field.FieldValue),
                    SolrQueryByField { FieldName: "fullPath" } field => _index.Values.Where(doc => doc.FullPath == field.FieldValue),
                    _ => [],
                }));
            solr.Setup(m => m.Get(It.IsAny<ISolrQuery>()))
                .Returns((ISolrQuery query) => { _queries.Add(query); return Results(_index.Values); });
            solr.Setup(m => m.Update(It.IsAny<Models.MediaContainer>()))
                .Returns((Models.MediaContainer doc) =>
                {
                    if (_failUpdates) return false;
                    _index[doc.Id!] = doc;
                    return true;
                });
            solr.Setup(m => m.Add(It.IsAny<Models.MediaContainer>()))
                .Returns((Models.MediaContainer doc) => { _index[doc.Id!] = doc; return true; });
            solr.Setup(m => m.Update(It.IsAny<IEnumerable<Models.MediaContainer>>()))
                .Returns((IEnumerable<Models.MediaContainer> docs) =>
                {
                    // Simulate e.g. the scanner loading things while the index is being updated.
                    _duringBatchUpdate?.Invoke();
                    if (_failBatchUpdates) return false;
                    foreach (var doc in docs) _index[doc.Id!] = doc;
                    return true;
                });

            var scopedServices = new Mock<IServiceProvider>();
            scopedServices.Setup(m => m.GetService(typeof(ISolrIndexService<Models.MediaContainer>))).Returns(solr.Object);
            var scope = new Mock<IServiceScope>();
            scope.Setup(m => m.ServiceProvider).Returns(scopedServices.Object);
            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory.Setup(m => m.CreateScope()).Returns(scope.Object);

            _services.Setup(m => m.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactory.Object);
            _services.Setup(m => m.GetService(typeof(IFileSystemService))).Returns(Mock.Of<IFileSystemService>(m => m.Mounts == new List<FileSystemMount>()));

            _mediaLibrary.Setup(m => m.Id).Returns("library");
            _mediaLibrary.Setup(m => m.Type).Returns("Library");
            _mediaLibrary.Setup(m => m.LockPaths(It.IsAny<string[]>())).Returns((string[] paths) => _locks.Lock(paths));
            _mediaLibrary.Setup(m => m.IsLocked(It.IsAny<string?>())).Returns((string? path) => _locks.IsLocked(path));

            var settings = Activator.CreateInstance<Settings>();
            settings.Scanner = new ScannerSettings { Folders = [_root.TrimEnd('/')], WatchedFolders = [], IgnoredPatterns = [] };
            settings.Security = new SecuritySettings
            {
                Library = new SecurityLibrarySettings { ReadOnly = false },
                Settings = new SecuritySettingsSettings { ReadOnly = false },
            };
            _settings.Setup(m => m.CurrentValue).Returns(settings);

            // Index the chain of folders leading to the root and then the test content beneath it:
            //   Old/clip.mp4, Old/notes.txt (not indexed), Old/Sub/photo.jpg, Older/other.mp4
            var root = IndexFolders(_root);
            var old = AddFolder("old", root, "Old");
            AddFile("clip", old, "clip.mp4", "Video");
            File.WriteAllText(Path.Combine(old.FullPath!, "notes.txt"), "");
            var sub = AddFolder("sub", old, "Sub");
            AddFile("photo", sub, "photo.jpg", "Photo");
            var older = AddFolder("older", root, "Older");
            AddFile("other", older, "other.mp4", "Video");
        }

        public void Dispose()
        {
            Directory.Delete(_root, true);
            GC.SuppressFinalize(this);
        }

        private static SolrQueryResults<Models.MediaContainer> Results(IEnumerable<Models.MediaContainer> docs)
        {
            var results = new SolrQueryResults<Models.MediaContainer>();
            results.AddRange(docs.Select(Clone));
            return results;
        }

        private static Models.MediaContainer Clone(Models.MediaContainer doc) => new()
        {
            Id = doc.Id, Name = doc.Name, Type = doc.Type, Parent = doc.Parent, ParentType = doc.ParentType,
            Parents = doc.Parents?.ToArray(), Path = doc.Path, FullPath = doc.FullPath, Flags = doc.Flags?.ToArray(),
            Size = doc.Size, DateCreated = doc.DateCreated, DateModified = doc.DateModified, Checksum = doc.Checksum,
        };

        private Models.MediaContainer IndexFolders(string path)
        {
            Models.MediaContainer? parent = null;
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                parent = AddFolder("folder-" + i, parent, segments[i]);
            }
            return parent!;
        }

        private Models.MediaContainer AddFolder(string id, Models.MediaContainer? parent, string name)
        {
            var doc = Add(id, parent, name, "Folder");
            doc.FullPath += "/";
            Directory.CreateDirectory(doc.FullPath);
            return doc;
        }

        private Models.MediaContainer AddFile(string id, Models.MediaContainer parent, string name, string type)
        {
            var doc = Add(id, parent, name, type);
            File.WriteAllText(doc.FullPath!, "");
            // Match the file on disk so loading it doesn't trigger any (re)processing.
            var info = new FileInfo(doc.FullPath!);
            doc.Size = info.Length;
            doc.DateCreated = info.CreationTimeUtc;
            doc.DateModified = info.LastWriteTimeUtc;
            return doc;
        }

        private Models.MediaContainer Add(string id, Models.MediaContainer? parent, string name, string type)
        {
            var path = parent?.FullPath ?? "/";
            var doc = new Models.MediaContainer
            {
                Id = id,
                Name = name,
                Type = type,
                Parent = parent?.Id ?? "library",
                ParentType = parent?.Type ?? "Library",
                Parents = parent == null ? ["library"] : [parent.Id!, .. parent.Parents!],
                Path = path,
                FullPath = path + name,
                Flags = [],
            };
            _index[id] = doc;
            return doc;
        }

        private MediaFolder Load(string id) => Load<MediaFolder>(id: id);

        private T Load<T>(string? id = null, string? path = null) where T : MediaContainer =>
            (T)Activator.CreateInstance(typeof(T), NullLogger<MediaContainer>.Instance, _services.Object, _settings.Object, Mock.Of<IThumbnailsDatabase>(), _mediaLibrary.Object, id, path, null)!;

        private LibraryController CreateController() =>
            new(_services.Object, _mediaLibrary.Object, _settings.Object, Mock.Of<IThumbnailsDatabase>(),
                new NotificationService(Mock.Of<IHubContext<SignalRHub>>()), NullLogger<MediaContainer>.Instance);

        private IActionResult Rename(string id, string name)
        {
            var modified = Clone(_index[id]);
            modified.Name = name;
            return CreateController().Patch(modified, modified.FullPath!.TrimStart('/'));
        }

        private string Old => _root + "Old/";
        private string New => _root + "New/";

        // ── MediaFolder.Move ──────────────────────────────────────────────────────

        [Fact]
        public void Move_MovesTheWholeFolderIncludingUnindexedFiles()
        {
            using (var folder = Load("old")) folder.Move(_root + "New");

            Assert.False(Directory.Exists(Old));
            Assert.True(File.Exists(New + "clip.mp4"));
            Assert.True(File.Exists(New + "notes.txt"));
            Assert.True(File.Exists(New + "Sub/photo.jpg"));
        }

        [Fact]
        public void Move_UpdatesTheFolderKeepingItsId()
        {
            using (var folder = Load("old"))
            {
                folder.Move(_root + "New");

                Assert.Equal("New", folder.Name);
                Assert.Equal(New, folder.FullPath);
            }

            Assert.Equal("New", _index["old"].Name);
            Assert.Equal(New, _index["old"].FullPath);
        }

        [Fact]
        public void Move_UpdatesThePathsOfAllDescendantsKeepingTheirIds()
        {
            using (var folder = Load("old")) folder.Move(_root + "New");

            Assert.Equal(New, _index["clip"].Path);
            Assert.Equal(New + "clip.mp4", _index["clip"].FullPath);
            Assert.Equal(New, _index["sub"].Path);
            Assert.Equal(New + "Sub/", _index["sub"].FullPath);
            Assert.Equal(New + "Sub/", _index["photo"].Path);
            Assert.Equal(New + "Sub/photo.jpg", _index["photo"].FullPath);
            Assert.Equal(["sub", "old"], _index["photo"].Parents![..2]);
        }

        [Fact]
        public void Move_LeavesSiblingsSharingTheNamePrefixAlone()
        {
            using (var folder = Load("old")) folder.Move(_root + "New");

            Assert.Equal(_root + "Older/", _index["other"].Path);
            Assert.Equal(_root + "Older/other.mp4", _index["other"].FullPath);
        }

        [Fact]
        public void Move_QueriesDescendantsByTheEscapedPathPrefix()
        {
            using (var folder = Load("old")) folder.Move(_root + "New");

            var query = Assert.IsType<SolrQuery>(Assert.Single(_queries));
            Assert.Equal("path:" + string.Concat(Old.Select(c => "/ -".Contains(c) ? "\\" + c : c.ToString())) + "*", query.Query);
        }

        [Fact]
        public void Move_ForgetsTheCachedIdsOfTheOldPath()
        {
            using (var folder = Load("old")) folder.Move(_root + "New");

            _mediaLibrary.Verify(m => m.ClearCache(Old), Times.Once);
        }

        [Fact]
        public void Move_WhenDestinationExists_ThrowsAndChangesNothing()
        {
            using var folder = Load("old");

            Assert.Throws<IOException>(() => folder.Move(_root + "Older"));
            Assert.True(File.Exists(Old + "clip.mp4"));
            Assert.Equal(Old, _index["clip"].Path);
        }

        [Fact]
        public void Move_WhenIndexUpdateFails_RestoresTheFolder()
        {
            _failBatchUpdates = true;
            using var folder = Load("old");

            Assert.Throws<InvalidOperationException>(() => folder.Move(_root + "New"));
            Assert.True(File.Exists(Old + "clip.mp4"));
            Assert.False(Directory.Exists(New));
            Assert.Equal("Old", folder.Name);
        }

        [Fact]
        public void Move_ToAnotherParent_IsNotSupported()
        {
            using var folder = Load("old");

            Assert.Throws<NotSupportedException>(() => folder.Move(_root + "Older/Old"));
            Assert.True(Directory.Exists(Old));
        }

        [Fact]
        public void Move_ChangingOnlyTheCase_Succeeds()
        {
            using (var folder = Load("old")) folder.Move(_root + "OLD");

            Assert.Contains("OLD", Directory.GetDirectories(_root).Select(Path.GetFileName));
            Assert.Equal(_root + "OLD/clip.mp4", _index["clip"].FullPath);
        }

        [Fact]
        public void Move_UnlocksThePathsAfterwards()
        {
            using (var folder = Load("old")) folder.Move(_root + "New");

            Assert.False(_locks.IsLocked(Old));
            Assert.False(_locks.IsLocked(New));
        }

        [Fact]
        public void Move_WhenIndexUpdateFails_UnlocksThePaths()
        {
            _failBatchUpdates = true;
            using var folder = Load("old");

            Assert.Throws<InvalidOperationException>(() => folder.Move(_root + "New"));
            Assert.False(_locks.IsLocked(Old));
            Assert.False(_locks.IsLocked(New));
        }

        [Fact]
        public void Move_ContainerLoadedMidMove_IsSkippedRatherThanDeleted()
        {
            bool? skipped = null, deleted = null;
            _duringBatchUpdate = () =>
            {
                // The index still says the file is in the old folder, which is no longer on disk.
                using var clip = Load<VideoFile>(id: "clip");
                (skipped, deleted) = (clip.Skipped, clip.Deleted);
            };

            using (var folder = Load("old")) folder.Move(_root + "New");

            Assert.True(skipped);
            Assert.False(deleted);
            Assert.Equal(New + "clip.mp4", _index["clip"].FullPath);
        }

        [Fact]
        public void Move_ContainerAddedMidMove_IsSkippedRatherThanDuplicated()
        {
            bool? skipped = null;
            _duringBatchUpdate = () =>
            {
                // The file is already in the new folder on disk but not yet in the index.
                using var clip = Load<VideoFile>(path: New + "clip.mp4");
                skipped = clip.Skipped;
            };

            using (var folder = Load("old")) folder.Move(_root + "New");

            Assert.True(skipped);
            Assert.Equal("clip", Assert.Single(_index.Values, doc => doc.FullPath == New + "clip.mp4").Id);
        }

        // ── MediaFile.Move ────────────────────────────────────────────────────────

        [Fact]
        public void FileMove_RenamesTheFileAndUpdatesTheIndexKeepingItsId()
        {
            using (var clip = Load<VideoFile>(id: "clip"))
            {
                clip.Move(Old + "renamed.mp4");

                // The index is updated as part of the move rather than when the file is disposed.
                Assert.Equal("renamed.mp4", _index["clip"].Name);
                Assert.Equal(Old + "renamed.mp4", _index["clip"].FullPath);
            }

            Assert.False(File.Exists(Old + "clip.mp4"));
            Assert.True(File.Exists(Old + "renamed.mp4"));
        }

        [Fact]
        public void FileMove_ForgetsTheCachedIdAndUnlocksThePaths()
        {
            using (var clip = Load<VideoFile>(id: "clip")) clip.Move(Old + "renamed.mp4");

            _mediaLibrary.Verify(m => m.ClearCache(Old + "clip.mp4"), Times.Once);
            Assert.False(_locks.IsLocked(Old + "clip.mp4"));
            Assert.False(_locks.IsLocked(Old + "renamed.mp4"));
        }

        [Fact]
        public void FileMove_WhenIndexUpdateFails_RestoresTheFile()
        {
            using var clip = Load<VideoFile>(id: "clip");
            _failUpdates = true;

            Assert.Throws<InvalidOperationException>(() => clip.Move(Old + "renamed.mp4"));
            Assert.True(File.Exists(Old + "clip.mp4"));
            Assert.False(File.Exists(Old + "renamed.mp4"));
            Assert.Equal("clip.mp4", clip.Name);
            Assert.False(_locks.IsLocked(Old + "clip.mp4"));
        }

        // ── LibraryController.Patch ───────────────────────────────────────────────

        [Fact]
        public void Patch_RenamesTheFolder()
        {
            var result = Assert.IsType<OkObjectResult>(Rename("old", "New"));

            Assert.Equal(New, Assert.IsType<Models.MediaContainer>(result.Value).FullPath);
            Assert.True(File.Exists(New + "clip.mp4"));
            Assert.Equal(New + "clip.mp4", _index["clip"].FullPath);
        }

        [Theory]
        [InlineData("..")]
        [InlineData("../Escaped")]
        [InlineData("Older/Old")]
        [InlineData("")]
        public void Patch_WithInvalidName_Returns400(string name)
        {
            var result = Assert.IsAssignableFrom<ObjectResult>(Rename("old", name));

            Assert.Equal(400, result.StatusCode);
            Assert.True(File.Exists(Old + "clip.mp4"));
        }

        [Fact]
        public void Patch_WhenNameIsTaken_Returns409()
        {
            var result = Assert.IsAssignableFrom<ObjectResult>(Rename("old", "Older"));

            Assert.Equal(409, result.StatusCode);
            Assert.True(File.Exists(Old + "clip.mp4"));
        }

        [Fact]
        public void Patch_RenamesAFile()
        {
            Assert.IsType<OkObjectResult>(Rename("clip", "renamed.mp4"));

            Assert.True(File.Exists(Old + "renamed.mp4"));
            Assert.Equal(Old + "renamed.mp4", _index["clip"].FullPath);
        }

        [Theory]
        [InlineData("../escaped.mp4")]
        [InlineData("Sub/clip.mp4")]
        public void Patch_WithInvalidFileName_Returns400(string name)
        {
            var result = Assert.IsAssignableFrom<ObjectResult>(Rename("clip", name));

            Assert.Equal(400, result.StatusCode);
            Assert.True(File.Exists(Old + "clip.mp4"));
        }

        [Fact]
        public void Patch_WhenFileNameIsTaken_Returns409()
        {
            File.WriteAllText(Old + "taken.mp4", "");

            var result = Assert.IsAssignableFrom<ObjectResult>(Rename("clip", "taken.mp4"));

            Assert.Equal(409, result.StatusCode);
            Assert.True(File.Exists(Old + "clip.mp4"));
        }
    }
}
