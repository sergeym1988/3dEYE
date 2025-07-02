namespace Common.Models
{
    /// <summary>
    /// Represents the various statuses a file processing task can have.
    /// </summary>
    public enum FileStatusEnum
    {
        NotFound,
        InProgress,
        Completed,
        Failed
    }
}
