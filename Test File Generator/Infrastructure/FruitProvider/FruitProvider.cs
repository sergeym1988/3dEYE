using GenerateFileHandler.Application.Interfaces;
using System.Text.Json;

namespace TestFileGenerator.Infrastructure.FruitProvider
{
    /// <summary>
    /// Provides a list of fruits loaded from a JSON file.
    /// </summary>
    public class FruitProvider : IFruitProvider
    {
        private readonly List<string> _fruits;

        /// <summary>
        /// Initializes a new instance of the <see cref="FruitProvider"/> class by loading fruits from a JSON resource file.
        /// </summary>
        /// <param name="env">Web host environment to locate the content root path.</param>
        /// <exception cref="FileNotFoundException">Thrown if the fruits JSON file is not found.</exception>
        public FruitProvider(IWebHostEnvironment env)
        {
            string path = Path.Combine(env.ContentRootPath, "Resources", "fruits.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("Fruit list file not found", path);

            var json = File.ReadAllText(path);
            _fruits = JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }

        /// <summary>
        /// Returns the list of fruits.
        /// </summary>
        /// <returns>A list of fruit names.</returns>
        public List<string> GetFruits() => _fruits;
    }
}