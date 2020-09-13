using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bicep.Types;
using Bicep.Types.Concrete;

namespace Bicep.Types.Arm
{
    public static class TypeLoader
    {
        private static readonly Lazy<IReadOnlyDictionary<string, TypeIndexer.TypeLocation>> IndexLazy = new Lazy<IReadOnlyDictionary<string, TypeIndexer.TypeLocation>>(LoadIndex);

        private static readonly IDictionary<string, TypeBase[]> LoadedTypes = new Dictionary<string, TypeBase[]>();

        public static IReadOnlyDictionary<string, TypeIndexer.TypeLocation> LoadIndex()
        {
            var fileStream = typeof(TypeLoader).Assembly.GetManifestResourceStream("index.json");

            using (fileStream)
            using (var streamReader = new StreamReader(fileStream))
            {
                var content = streamReader.ReadToEnd();

                var dictionary = TypeIndexer.DeserializeIndex(content);
                return dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
            }
        }

        public static ResourceType? TryLoadResource(string resourceType, string apiVersion)
        {
            if (!IndexLazy.Value.TryGetValue($"{resourceType}@{apiVersion}", out var typeLocation))
            {
                return null;
            }

            if (typeLocation.RelativePath == null || typeLocation.Index == null)
            {
                return null;
            }

            var relativePath = typeLocation.RelativePath;
            var index = typeLocation.Index.Value;

            if (!LoadedTypes.TryGetValue(relativePath, out var types))
            {
                var fileStream = typeof(TypeLoader).Assembly.GetManifestResourceStream(relativePath);
                if (fileStream == null)
                {
                    return null;
                }

                using (fileStream)
                using (var streamReader = new StreamReader(fileStream))
                {
                    var content = streamReader.ReadToEnd();

                    types = TypeSerializer.Deserialize(content);
                    LoadedTypes[relativePath] = types;
                }
            }

            return types[index] as ResourceType;
        }

        public static IEnumerable<string> GetAvailableTypes()
            => IndexLazy.Value.Keys;
    }
}