using GenerateFileHandler.Application.Interfaces;
using System.Text.Json;

public class BenchmarkFruitProvider : IFruitProvider
{
    private readonly List<string> _fruits;

    public BenchmarkFruitProvider(string contentRootPath)
    {
        var path = Path.Combine(contentRootPath, "Resources", "fruits.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("Fruit list file not found", path);

        var json = File.ReadAllText(path);
        _fruits = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    public List<string> GetFruits()
    {
        return _fruits;
    }
}