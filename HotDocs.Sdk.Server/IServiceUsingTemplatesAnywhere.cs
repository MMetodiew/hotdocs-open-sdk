using System.Collections.Generic;
using System.IO;
using HotDocs.Sdk.Server.Contracts;

namespace HotDocs.Sdk.Server
{
    public interface IServiceUsingTemplatesAnywhere
    {
        ///<summary>
        ///	GetInterview returns an HTML fragment suitable for inclusion in any standards-mode web page, which embeds a HotDocs interview
        ///	directly in that web page.
        ///</summary>
        /// <param name="template">The template for which to return an interview.</param>
        /// <param name="answers">The answers to use when building an interview.</param>
        /// <param name="settings">The <see cref="InterviewSettings"/> to use when building an interview.</param>
        /// <param name="markedVariables">The variables to highlight to the user as needing special attention.
        /// 	This is usually populated with <see cref="AssembleDocumentResult.UnansweredVariables" />
        /// 	from <see cref="AssembleDocument" />.</param>
        /// <include file="../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
        /// <returns>Returns the results of building the interview as an <see cref="InterviewResult"/> object.</returns>
        InterviewResult GetInterview(ITemplate template, TextReader answers, InterviewSettings settings, IEnumerable<string> markedVariables, string logRef);

        /// <summary>
        /// Assemble a document from the given template, answers and settings.
        /// </summary>
        /// <param name="template">An instance of the Template class.</param>
        /// <param name="answers">Either an XML answer string, or a string containing encoded
        /// interview answers as posted from a HotDocs browser interview.</param>
        /// <param name="settings">An instance of the AssembleDocumentResult class.</param>
        /// <include file="../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
        /// <returns>An AssemblyResult object containing all the files and data resulting from the request.</returns>
        AssembleDocumentResult AssembleDocument(ITemplate template, TextReader answers, AssembleDocumentSettings settings, string logRef);

        /// <summary>
        /// GetComponentInfo returns metadata about the variables/types (and optionally dialogs and mapping info)
        /// for the indicated template's interview.
        /// </summary>
        /// <param name="template">An instance of the Template class, for which you are requesting component information.</param>
        /// <param name="includeDialogs">Whether to include dialog &amp; mapping information in the returned results.</param>
        /// <include file="../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
        /// <returns>The requested component information.</returns>
        ComponentInfo GetComponentInfo(ITemplate template, bool includeDialogs, string logRef);

        /// <summary>
        /// Build the server files for the specified template.
        /// </summary>
        /// <param name="template">The template for which support files will be built.</param>
        /// <param name="flags">Indicates what types of support files to build.</param>
        void BuildSupportFiles(ITemplate template, HDSupportFilesBuildFlags flags);

        /// <summary>
        /// Remove the server files for the specified template.
        /// </summary>
        /// <param name="template">The template for which support files will be removed.</param>
        void RemoveSupportFiles(ITemplate template);

        /// <summary>
        /// Retrieves a file required by the interview. This could be either an interview definition that contains the 
        /// variables and logic required to display an interview (questionaire) for the main template or one of its 
        /// inserted templates, or it could be an image file displayed on a dialog within the interview.
        /// </summary>
        /// <param name="template">The template related to the requested file.</param>
        /// <param name="fileName">The file name of the image, or the file name of the template for which the interview
        /// definition is being requested. In either case, this value is passed as "template" on the query string by the browser interview.</param>
        /// <param name="fileType">The type of file being requested: img (image file), js (JavaScript interview definition), 
        /// or dll (Silverlight interview definition).</param>
        /// <returns>A stream containing the requested interview file, to be returned to the caller.</returns>
        Stream GetInterviewFile(ITemplate template, string fileName, string fileType);
    }
}