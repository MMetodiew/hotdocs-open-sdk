/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HotDocs.Sdk;
using HotDocs.Sdk.Server.Contracts;

namespace HotDocs.Sdk.Server
{
	/// <summary>
	/// This is a stateless, object-oriented interface for basic communications with HotDocs Server or Cloud Services, including
	/// getting interviews, assembling documents, and other simple operations.
	/// There are three implementations of <c>IServices</c>:
	/// <list type="bullet">
	/// <item><c>Local.Services</c> - talks to a locally-running copy of HotDocs Server;</item>
	/// <item><c>WebService.Services</c> - talks to a remotely-running copy of HotDocs Server via the HotDocs Server web services interface;</item>
	/// <item><c>Cloud.Services</c> - talks to HotDocs Cloud Services running in Windows Azure.</item>
	/// </list>
	/// </summary>
	public interface IServices
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
		/// This method overlays any answer collections passed into it, into a single XML answer collection.
		/// It has two primary uses: it can be used to combine multiple answer collections into a single
		/// answer collection; and/or it can be used to "resolve" or standardize an answer collection
		/// submitted from a browser interview (which may be specially encoded) into standard XML answers.
		/// </summary>
		/// <param name="answers">A sequence of answer collections. Each member of this sequence
		/// must be either an (encoded) interview answer collection or a regular XML answer collection.
		/// Each member will be successively overlaid (overlapped) on top of the prior members to
		/// form one consolidated answer collection.</param>
		/// <include file="../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
		/// <returns>The consolidated XML answer collection.</returns>
		string GetAnswers(IEnumerable<TextReader> answers, string logRef);

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

		// Get the template manifest for the specified template. Can optionally parse an entire template manifest spanning tree.
		//TemplateManifest GetManifest(string templateLocator, string templateFileName, ManifestParseFlags parseFlags);
		// Q: should TemplateManifest be enhanced to have everything GetComponentInfo needs?  (in other words, dialogs?)
	}
}
