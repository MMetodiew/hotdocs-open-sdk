namespace HotDocs.Sdk
{
    /// <summary>
    /// A type of document that can be produced by assembling a document from a template.
    /// </summary>
    public enum DocumentType
    {
        /// <summary></summary>
        Unknown = 0,
        /// <summary>
        /// Document type native to the template. See Template.NativeDocumentType. For example,
        /// a WordPerfect WPD file is native to a WordPerfect WPT template.
        /// </summary>
        Native,
        /// <summary></summary>
        WordDOCX,
        /// <summary></summary>
        WordRTF,
        /// <summary></summary>
        WordDOC,
        /// <summary></summary>
        WordPerfect,
        /// <summary></summary>
        PDF,
        /// <summary></summary>
        HPD,
        /// <summary></summary>
        HFD,
        /// <summary></summary>
        PlainText,
        /// <summary></summary>
        HTML,
        /// <summary>
        /// HTML with images included as embedded URIs.
        /// </summary>
        HTMLwDataURIs,
        /// <summary>
        /// MIME HTML
        /// </summary>
        MHTML,
        /// <summary></summary>
        XML
    }
}