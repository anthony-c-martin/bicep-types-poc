using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Bicep.Types.Concrete;

namespace Bicep.Types
{
    public static class TypeIndexer
    {
        public class TypeLocation
        {
            public string? RelativePath { get; set; }

            public int? Index { get; set; }
        }

        private static (TypeBase type, int index) GetTypeWithIndex(TypeBase type, int index)
            => (type, index);

        private static IReadOnlyDictionary<string, TypeLocation> BuildIndex(string baseDir)
        {
            var typeFiles = Directory.GetFiles(baseDir, "types.json", SearchOption.AllDirectories);
            var typeDictionary = new Dictionary<string, TypeLocation>(StringComparer.OrdinalIgnoreCase);

            foreach (var typeFile in typeFiles)
            {
                var content = File.ReadAllText(typeFile);

                var relativePath = Path.GetFullPath(typeFile).Substring(Path.GetFullPath(baseDir).Length + 1)
                    .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .ToLowerInvariant();

                var indexedTypes = TypeSerializer.Deserialize(content).Select(GetTypeWithIndex);

                foreach (var (type, index) in indexedTypes)
                {
                    if (!(type is ResourceType resourceType))
                    {
                        continue;
                    }

                    if (resourceType.Name == null)
                    {
                        throw new ArgumentException($"Found resource with null resource name");
                    }

                    if (typeDictionary.ContainsKey(resourceType.Name))
                    {
                        Console.WriteLine($"WARNING: Found duplicate type {resourceType.Name}");
                        continue;
                    }

                    typeDictionary.Add(resourceType.Name, new TypeLocation
                    {
                        RelativePath = relativePath,
                        Index = index,
                    });
                }
            }

            return typeDictionary;
        }

        public static string BuildSerializedIndex(string baseDir)
        {
            var output = BuildIndex(baseDir);
            
            return JsonSerializer.Serialize(output);
        }

        public static IReadOnlyDictionary<string, TypeLocation> DeserializeIndex(string content)
        {
            return JsonSerializer.Deserialize<Dictionary<string, TypeLocation>>(content);
        }
    }
}