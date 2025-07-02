namespace Sorter.Models
{
    /// <summary>
    /// Configuration options for file sorting process.
    /// </summary>
    public class FileSortingOptions
    {
        /// <summary>
        /// Gets or sets the directory path where generated files are saved.
        /// </summary>
        public string InputDirectory { get; set; } = "../GeneratedFiles";

        /// <summary>
        /// Gets or sets the directory path where sorted files are saved.
        /// </summary>
        public string OutputDirectory { get; set; } = "../SortedFiles";

        /// <summary>
        /// Gets or sets the directory path for temp files.
        /// </summary>
        public string TempDirectory { get; set; } = "../Temp";

        /// <summary>
        /// Gets or sets the size of chunks in megabytes for splitting files.
        /// </summary>
        public int ChunkSizeMb { get; set; } = 100;
    }
}
