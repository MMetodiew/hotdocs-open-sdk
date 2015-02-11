namespace HotDocs.Sdk
{
    /// <summary>
    /// The type of HotDocs template.
    /// </summary>
    public enum TemplateType
    {
        /// <summary></summary>
        Unknown,
        /// <summary>
        /// Templates that only include an interview. (.cmp files.) No document is assembled from an interview-only template.
        /// </summary>
        InterviewOnly,
        /// <summary></summary>
        WordDOCX,
        /// <summary></summary>
        WordRTF,
        /// <summary></summary>
        WordPerfect,
        /// <summary></summary>
        HotDocsHFT,
        /// <summary></summary>
        HotDocsPDF,
        /// <summary></summary>
        PlainText
    }
}