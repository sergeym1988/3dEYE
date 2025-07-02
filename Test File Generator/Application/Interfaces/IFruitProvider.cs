namespace GenerateFileHandler.Application.Interfaces
{
    /// <summary>
    /// Provides a collection of fruits.
    /// </summary>
    public interface IFruitProvider
    {
        /// <summary>
        /// Retrieves the list of fruits.
        /// </summary>
        /// <returns>List of fruit names.</returns>
        List<string> GetFruits();
    }
}