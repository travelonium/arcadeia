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

using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace Arcadeia.Configuration
{
    public partial class Codecs
    {
        [JsonPropertyName("Encoders")]
        public Encoders Encoders { get; set; } = new();

        [JsonPropertyName("Decoders")]
        public Decoders Decoders { get; set; } = new();

        [GeneratedRegex(@"^\s+([A-Z.]+)\s+([\w.]+)\s+(.*)$", RegexOptions.Multiline)]
        private static partial Regex CodecRegex();

        public Codecs(string codecs)
        {
            var matches = CodecRegex().Matches(codecs);

            foreach (Match match in matches)
            {
                string capabilities = match.Groups[1].Value.Trim();
                string name = match.Groups[2].Value.Trim();
                string description = match.Groups[3].Value.Trim();

                string? type = capabilities[2] switch
                {
                    'V' => "Video",
                    'A' => "Audio",
                    'S' => "Subtitle",
                    'D' => "Data",
                    'T' => "Attachment",
                    _ => null
                };

                // Add to Decoders if decoding is supported ("D" in the first position)
                if (capabilities[0] == 'D')
                {
                    Decoders.Add(type, new Codec { Name = name, Description = description });
                }

                // Add to Encoders if encoding is supported ("E" in the second position)
                if (capabilities[1] == 'E')
                {
                    Encoders.Add(type, new Codec { Name = name, Description = description });
                }
            }
        }

        public Codecs(string encoders, string decoders)
        {
            var matches = CodecRegex().Matches(encoders);

            foreach (Match match in matches)
            {
                string capabilities = match.Groups[1].Value.Trim();
                string name = match.Groups[2].Value.Trim();
                string description = match.Groups[3].Value.Trim();

                string? type = capabilities[0] switch
                {
                    'V' => "Video",
                    'A' => "Audio",
                    'S' => "Subtitle",
                    _ => null
                };

                Encoders.Add(type, new Codec { Name = name, Description = description });
            }

            matches = CodecRegex().Matches(decoders);

            foreach (Match match in matches)
            {
                string capabilities = match.Groups[1].Value.Trim();
                string name = match.Groups[2].Value.Trim();
                string description = match.Groups[3].Value.Trim();

                string? type = capabilities[0] switch
                {
                    'V' => "Video",
                    'A' => "Audio",
                    'S' => "Subtitle",
                    _ => null
                };

                Decoders.Add(type, new Codec { Name = name, Description = description });
            }
        }
    }

    public class Codec : IComparable<Codec>
    {
        public required string Name { get; set; }
        public required string Description { get; set; }

        public int CompareTo(Codec? other)
        {
            if (other is null)
            {
                return 1;
            }

            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }
    }

    public class Encoders
    {
        public SortedSet<Codec> Video { get; set; } = [];
        public SortedSet<Codec> Audio { get; set; } = [];
        public SortedSet<Codec> Subtitle { get; set; } = [];
        public SortedSet<Codec> Data { get; set; } = [];
        public SortedSet<Codec> Attachment { get; set; } = [];

        public void Add(string? type, Codec codec)
        {
            Get(type)?.Add(codec);
        }

        public SortedSet<Codec>? Get(string? type)
        {
            return type switch
            {
                "Video" => Video,
                "Audio" => Audio,
                "Subtitle" => Subtitle,
                "Data" => Data,
                "Attachment" => Attachment,
                _ => null
            };
        }
    }

    public class Decoders
    {
        public SortedSet<Codec> Video { get; set; } = [];
        public SortedSet<Codec> Audio { get; set; } = [];
        public SortedSet<Codec> Subtitle { get; set; } = [];
        public SortedSet<Codec> Data { get; set; } = [];
        public SortedSet<Codec> Attachment { get; set; } = [];

        public void Add(string? type, Codec codec)
        {
            Get(type)?.Add(codec);
        }

        public SortedSet<Codec>? Get(string? type)
        {
            return type switch
            {
                "Video" => Video,
                "Audio" => Audio,
                "Subtitle" => Subtitle,
                "Data" => Data,
                "Attachment" => Attachment,
                _ => null
            };
        }
    }
}