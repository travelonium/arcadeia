using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace MediaCurator.Configuration
{
    public partial class Codecs
    {
        [JsonPropertyName("Encoders")]
        public Encoders Encoders { get; set; } = new();

        [JsonPropertyName("Decoders")]
        public Decoders Decoders { get; set; } = new();

        [GeneratedRegex(@"^\s+([A-Z.]+)\s+([\w.]+)\s+(.*)$", RegexOptions.Multiline)]
        private static partial Regex CodecRegex();

        public Codecs(string input)
        {
            var matches = CodecRegex().Matches(input);

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
    }

    public class Codec
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
    }

    public class Encoders
    {
        public List<Codec> Video { get; set; } = [];
        public List<Codec> Audio { get; set; } = [];
        public List<Codec> Subtitle { get; set; } = [];
        public List<Codec> Data { get; set; } = [];
        public List<Codec> Attachment { get; set; } = [];

        public void Add(string? type, Codec codec)
        {
            Get(type)?.Add(codec);
        }

        public List<Codec>? Get(string? type)
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
        public List<Codec> Video { get; set; } = [];
        public List<Codec> Audio { get; set; } = [];
        public List<Codec> Subtitle { get; set; } = [];
        public List<Codec> Data { get; set; } = [];
        public List<Codec> Attachment { get; set; } = [];

        public void Add(string? type, Codec codec)
        {
            Get(type)?.Add(codec);
        }

        public List<Codec>? Get(string? type)
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