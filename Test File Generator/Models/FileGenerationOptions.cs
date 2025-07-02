namespace TestFileGenerator.Models
{
    /// <summary>
    /// Configuration options for file generation.
    /// </summary>
    public class FileGenerationOptions
    {
        /// <summary>
        /// Directory where generated files will be saved.
        /// Default is "../GeneratedFiles".
        /// </summary>
        public string OutputDirectory { get; set; } = "../GeneratedFiles";

        /// <summary>
        /// Size of each chunk file in megabytes.
        /// Default is 50 MB.
        /// </summary>
        public int ChunkSizeMb { get; set; } = 50;

        /// <summary>
        /// Gets or sets the maximum file size mb.
        /// </summary>
        /// <value>
        /// The maximum file size mb.
        /// </value>
        public int MaxFileSizeMb { get; set; } = 111111;
    }
}
