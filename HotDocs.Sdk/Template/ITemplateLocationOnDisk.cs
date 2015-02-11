namespace HotDocs.Sdk
{
    public interface ITemplateLocationOnDisk : ITemplateLocation
    {
        /// <summary>
        /// Returns the directory for the template.
        /// </summary>
        /// <remarks>
        /// <para>In a system where all template file names are unique, one directory
        /// may be used for all templates. Generally, however, each main template (e.g. a package's main template) should
        /// reside in a directory separate from other main templates. Each dependent template (e.g. a package template that is not the
        /// main template) should reside in the same directory as its main template.</para>
        /// 
        /// <para>A <c>TemplateLocation</c> class may be implemented by the host application, and may represent templates
        /// stored on the file system, in a DMS, in some other database, etc. However, when an actual template file path
        /// is needed, the <c>TemplateLocation</c> implementation is expected to provide a full path to the template directory, and
        /// ensure that the template itself and all of its dependencies exist in that directory. </para>
        /// 
        /// <para>The directory where the template exists should survive the serialization and deserialization of this object.
        /// If the file name needs to be updated at deserialization, the class should override the <see cref="GetUpdatedFileName"/>
        /// method.</para>
        /// </remarks>
        /// <returns></returns>
        string GetTemplateDirectory();

        /// <summary>
        /// Returns a copy of this object.
        /// </summary>
        /// <returns>A new <c>TemplateLocation</c> object, which is a copy of the original.</returns>
        ITemplateLocationOnDisk Duplicate();
    }
}