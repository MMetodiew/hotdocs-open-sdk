namespace HotDocs.Sdk
{
    /// <summary>
    /// This  represents a template that is managed by the host application, and
    /// (optionally) some assembly parameters (as specified by switches) for that template.
    /// The location of the template is defined by Template.Location.
    /// </summary>
    public interface ITemplate
    {
        /// <summary>
        /// Returns a locator string to recreate the template object at a later time.
        /// Use the Locate method to recreate the object.
        /// </summary>
        /// <returns></returns>
        string CreateLocator();

        /// <summary>
        /// The template title, which comes from the template's manifest file by default.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// The file name (including extension) of the template. No path information is included.
        /// </summary>
        string FileName { get; }

        /// <summary>
        /// Defines the location of the template.
        /// </summary>
        TemplateLocation Location { get; }

        /// <summary>
        /// Command line switches that may be applicable when assembling the template, as provided to the ASSEMBLE instruction.
        /// </summary>
        string Switches { get; set; }

        /// <summary>
        /// A key identifying the template. When using a template management scheme where the template file itself is temporary
        /// (such as with a DMS) set this key to help HotDocs Server to keep track of which server files are for which template.
        /// If not empty, this key is also used internally by HotDocs Server for caching purposes.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// If the host app wants to know, this property does what's necessary to
        /// tell you the type of template you're dealing with.
        /// </summary>
        TemplateType TemplateType { get; }

        /// <summary>
        /// Parses command-line switches to inform the host app whether or not
        /// an interview should be displayed for this template.
        /// </summary>
        bool HasInterview { get; }

        /// <summary>
        /// Based on TemplateType, tells the host app whether this type of template
        /// generates a document or not (although regardless, ALL template types
        /// need to be assembled in order to participate in assembly queues)
        /// </summary>
        bool GeneratesDocument { get; }

        /// <summary>
        /// Based on the template file extension, get the document type native to the template type.
        /// </summary>
        DocumentType NativeDocumentType { get; }

        /// <summary>
        /// Gets the template manifest for this template. Can optionally parse an entire template manifest spanning tree.
        /// See <see cref="ManifestParseFlags"/> for details.
        /// </summary>
        /// <param name="parseFlags">See <see cref="ManifestParseFlags"/>.</param>
        /// <returns></returns>
        TemplateManifest GetManifest(ManifestParseFlags parseFlags);

        /// <summary>
        /// Request that the Template.Location update the file name as needed.
        /// </summary>
        /// <remarks>Call this method to request that the file name</remarks>
        void UpdateFileName();

        /// <summary>
        /// Returns the full path (based on the directory specified by Location.GetTemplateDirectory) of the template.
        /// </summary>
        /// <returns></returns>
        string GetFullPath();

        /// <summary>
        /// Returns the assembled document extension associated with the NativeDocumentType property.
        /// </summary>
        /// <returns></returns>
        string GetDocExtension();
    }
}