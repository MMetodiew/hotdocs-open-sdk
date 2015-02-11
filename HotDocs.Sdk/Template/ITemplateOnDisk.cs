namespace HotDocs.Sdk
{
    public interface ITemplateOnDisk : ITemplate
    {
        string GetFullPath();

        /// <summary>
        /// Defines the location of the template.
        /// </summary>
        ITemplateLocationOnDisk Location { get; }
    }
}