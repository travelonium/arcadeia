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

using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Xunit;
using Arcadeia.Configuration;

namespace Arcadeia.Tests
{
    public class ConfigurationExtensionsTests
    {
        // Every key used below (Mounts, SupportedExtensions.*, Thumbnails.*, Scanner.*) must be a
        // real property on Arcadeia.Configuration.Settings: ToJson() disambiguates an empty section
        // as an array vs. an object by reflecting the actual CLR property type at that path, so these
        // tests are exercising it against the real schema, not a synthetic one.
        private static IConfigurationRoot Configuration(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return new ConfigurationBuilder().AddJsonStream(stream).Build();
        }

        // ── Empty List<T> properties → JSON array ────────────────────────────────

        [Fact]
        public void ToJson_EmptyListProperty_SerializesAsEmptyArray()
        {
            var configuration = Configuration("""{ "Mounts": [] }""");

            var json = configuration.ToJson(["Mounts"]) as JsonObject;

            Assert.IsType<JsonArray>(json?["Mounts"]);
            Assert.Empty(json!["Mounts"]!.AsArray());
        }

        [Fact]
        public void ToJson_EmptyNestedListProperty_SerializesAsEmptyArray()
        {
            var configuration = Configuration("""{ "SupportedExtensions": { "Audio": [] } }""");

            var json = configuration.ToJson(["SupportedExtensions"]) as JsonObject;
            var audio = json?["SupportedExtensions"]?["Audio"];

            Assert.IsType<JsonArray>(audio);
            Assert.Empty(audio!.AsArray());
        }

        [Fact]
        public void ToJson_PopulatedListProperty_SerializesWithItemsInOrder()
        {
            var configuration = Configuration("""{ "SupportedExtensions": { "Video": [ ".mp4", ".mkv" ] } }""");

            var json = configuration.ToJson(["SupportedExtensions"]) as JsonObject;
            var video = json?["SupportedExtensions"]?["Video"]?.AsArray();

            Assert.NotNull(video);
            Assert.Equal([".mp4", ".mkv"], video!.Select(item => item!.GetValue<string>()));
        }

        // ── Empty Dictionary<TKey,TValue> properties → JSON object ───────────────

        [Fact]
        public void ToJson_EmptyDictionaryProperty_SerializesAsEmptyObject()
        {
            var configuration = Configuration("""{ "Thumbnails": { "Audio": {} } }""");

            var json = configuration.ToJson(["Thumbnails"]) as JsonObject;
            var audio = json?["Thumbnails"]?["Audio"];

            Assert.IsType<JsonObject>(audio);
            Assert.Empty((JsonObject)audio!);
        }

        // ── Scalars still parse to their natural JSON type ───────────────────────

        [Fact]
        public void ToJson_BooleanValue_ParsesAsJsonBoolean()
        {
            var configuration = Configuration("""{ "Scanner": { "StartupScan": true } }""");

            var json = configuration.ToJson(["Scanner"]) as JsonObject;

            Assert.True(json?["Scanner"]?["StartupScan"]?.GetValue<bool>());
        }

        [Fact]
        public void ToJson_IntegerValue_ParsesAsJsonNumber()
        {
            var configuration = Configuration("""{ "Scanner": { "ParallelScannerTasks": 4 } }""");

            var json = configuration.ToJson(["Scanner"]) as JsonObject;

            // decimal.TryParse is checked before long.TryParse, so integer-looking values are stored
            // as decimals, not longs - pre-existing behavior, unrelated to the array/object fix.
            Assert.Equal(4m, json?["Scanner"]?["ParallelScannerTasks"]?.GetValue<decimal>());
        }

        // ── Whitelist filtering ───────────────────────────────────────────────────

        [Fact]
        public void ToJson_KeyNotInWhitelist_IsExcluded()
        {
            var configuration = Configuration("""{ "Mounts": [], "Security": { "Library": { "ReadOnly": false } } }""");

            var json = configuration.ToJson(["Mounts"]) as JsonObject;

            Assert.NotNull(json);
            Assert.True(json!.ContainsKey("Mounts"));
            Assert.False(json.ContainsKey("Security"));
        }

        [Fact]
        public void ToJson_NoWhitelist_IncludesEverything()
        {
            var configuration = Configuration("""{ "Mounts": [], "Security": { "Library": { "ReadOnly": false } } }""");

            var json = configuration.ToJson() as JsonObject;

            Assert.True(json?.ContainsKey("Mounts"));
            Assert.True(json?.ContainsKey("Security"));
        }
    }
}
