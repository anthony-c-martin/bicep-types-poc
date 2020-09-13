using System.IO;
using Bicep.Types;

namespace BicepTypeGenerator.IndexBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var baseDir = args[0];
            var indexContent = TypeIndexer.BuildSerializedIndex(baseDir);
            var indexPath = Path.Combine(baseDir, "index.json");

            File.WriteAllText(indexPath, indexContent);
        }
    }
}
