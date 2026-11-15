using System.IO;
using MelonLoader.Utils;

namespace Schizophrenia
{
    internal static class ModAssetPaths
    {
        private const string DataFolderName = "Schizophrenia";

        public static string Root
        {
            get
            {
                string assemblyDirectory = Path.GetDirectoryName(
                    typeof(SchizophreniaMod).Assembly.Location
                );

                if (string.IsNullOrEmpty(assemblyDirectory))
                    assemblyDirectory = MelonEnvironment.ModsDirectory;

                return Path.Combine(assemblyDirectory, DataFolderName);
            }
        }
    }
}
