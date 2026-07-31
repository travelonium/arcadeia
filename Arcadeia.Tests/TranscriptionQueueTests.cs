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
using Arcadeia.Services;

namespace Arcadeia.Tests
{
    public class TranscriptionQueueTests
    {
        private static CancellationToken Timeout(int milliseconds = 2000) => new CancellationTokenSource(milliseconds).Token;

        // ── Enqueue / DequeueAllAsync ────────────────────────────────────────────

        [Fact]
        public async Task Enqueue_ThenDequeueAllAsync_YieldsTheId()
        {
            var queue = new TranscriptionQueue();
            queue.Enqueue("abc123");

            await using var enumerator = queue.DequeueAllAsync(Timeout()).GetAsyncEnumerator();

            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal("abc123", enumerator.Current);
        }

        [Fact]
        public async Task Enqueue_MultipleIds_YieldsThemInOrder()
        {
            var queue = new TranscriptionQueue();
            queue.Enqueue("first");
            queue.Enqueue("second");

            await using var enumerator = queue.DequeueAllAsync(Timeout()).GetAsyncEnumerator();

            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal("first", enumerator.Current);
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal("second", enumerator.Current);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Enqueue_NullOrEmpty_DoesNotThrow(string? id)
        {
            var queue = new TranscriptionQueue();

            queue.Enqueue(id!);
        }

        // ── Deduplication ─────────────────────────────────────────────────────────

        [Fact]
        public async Task Enqueue_SameIdTwiceWithoutComplete_OnlyYieldsOnce()
        {
            var queue = new TranscriptionQueue();
            queue.Enqueue("dup");
            queue.Enqueue("dup");
            queue.Enqueue("other");

            await using var enumerator = queue.DequeueAllAsync(Timeout()).GetAsyncEnumerator();

            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal("dup", enumerator.Current);
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal("other", enumerator.Current);

            // The duplicate "dup" enqueue must not have produced a second channel entry.
            await using var probe = queue.DequeueAllAsync(Timeout(200)).GetAsyncEnumerator();
            await Assert.ThrowsAsync<OperationCanceledException>(() => probe.MoveNextAsync().AsTask());
        }

        [Fact]
        public async Task Complete_AllowsTheSameIdToBeEnqueuedAgain()
        {
            var queue = new TranscriptionQueue();
            queue.Enqueue("id");

            await using var enumerator = queue.DequeueAllAsync(Timeout()).GetAsyncEnumerator();
            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal("id", enumerator.Current);

            queue.Complete("id");
            queue.Enqueue("id");

            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal("id", enumerator.Current);
        }

        [Fact]
        public void Complete_UnknownId_DoesNotThrow()
        {
            var queue = new TranscriptionQueue();

            queue.Complete("never-enqueued");
        }
    }
}
