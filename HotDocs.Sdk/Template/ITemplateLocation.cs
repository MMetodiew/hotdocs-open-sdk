namespace HotDocs.Sdk
{
    public interface ITemplateLocation
    {
        /// <summary>
        /// Overrides Object.Equals. Calls into IEquatable&lt;TemplateLocation&gt;.Equals() to
        /// determine if instances of derived types are equal.
        /// </summary>
        /// <param name="obj">An object to use in the equality comparison.</param>
        /// <returns>A value indicating whether or not the two locations are equal.</returns>
        bool Equals(object obj);

        /// <summary>
        /// <c>GetHashCode</c> is needed wherever Equals(object) is defined.
        /// </summary>
        /// <returns>An integer hash code for the object.</returns>
        int GetHashCode();

        /// <summary>
        /// Implements IEquatable&lt;TemplateLocation&gt;. Used to determine equality/equivalency
        /// between TemplateLocations.
        /// </summary>
        /// <param name="other">The other location to compare with when testing equality.</param>
        /// <returns>A value indicating whether or not the two locations are equal.</returns>
        bool Equals(TemplateLocation other);

        /// <summary>
        /// Returns a Stream for a file living at the same location as the template.
        /// </summary>
        /// <param name="fileName">The name of the file (without any path information).</param>
        /// <returns>A Stream containing the file.</returns>
        System.IO.Stream GetFile(string fileName);

        /// <summary>
        /// Return an encrypted locator string.
        /// </summary>
        /// <returns></returns>
        string CreateLocator();

        /// <summary>
        /// Get an updated file name for a template. Return true if the file name needed updating.
        /// If this method returns true, then fileName contains the updated file name. This method
        /// should be overridden for file storage systems where the template is stored in a database
        /// such that a file name is created on demand.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="fileName">The updated file name for the specified template.</param>
        /// <returns>True if the file name was updated, or false otherwise.</returns>
        bool GetUpdatedFileName(ITemplate template, out string fileName);

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns>A new <c>TemplateLocation</c> object, which is a copy of the original.</returns>
        ITemplateLocation Duplicate();
    }
}