/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using HotDocs.Sdk.Cloud;
using HotDocs.Sdk.Server.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HotDocs.Sdk.Server.Cloud
{
	/// <summary>
	/// This is the implementation of IServices that utilizes HotDocs Cloud Services to perform its work.
	/// </summary>
	public class Services : IServices
	{
		#region Private Members

		private string _subscriberID;
		private string _signingKey;

		#endregion

		#region Public Constructors

		/// <summary>
		/// Constructor for Services, which requires the subscriber's ID and signing key.
		/// These are necessary to authenticate requests sent to HotDocs Cloud Services.
		/// </summary>
		/// <param name="subscriberId">A HotDocs Cloud Services subscriber ID.</param>
		/// <param name="signingKey">The signing key associated with the subscriber ID.</param>
		public Services(string subscriberId, string signingKey)
		{
			if (string.IsNullOrWhiteSpace(subscriberId))
				throw new ArgumentNullException("The subscriber ID is missing. Please check the value for SubscriberID in the config file and try again.");

			if (string.IsNullOrWhiteSpace(signingKey))
				throw new ArgumentNullException("The signing key is missing. Please check the value for SigningKey in the config file and try again.");

			_subscriberID = subscriberId;
			_signingKey = signingKey;
		}

		#endregion

		#region Public Properties
		/// <summary>
		/// Specifies the Cloud Services host address.  Leave as null for the default address.
		/// </summary>
		public string HostAddress { get; set; } // null by default

		/// <summary>
		/// Specifies the proxy address.  Leave as null for no proxy.
		/// </summary>
		public string ProxyAddress { get; set; } // null by default
		#endregion

		#region Public IServices Members

		/// <summary>
		/// Returns an HTML fragment suitable for inclusion in any standards-mode web page, which embeds a HotDocs interview
		/// directly in that web page.
		/// </summary>
		/// <param name="template">The template for which the interview will be requested.</param>
		/// <param name="answers">The initial set of answers to include in the interview.</param>
		/// <param name="settings">Settings that define various interview behavior.</param>
		/// <param name="markedVariables">A collection of variables that should be marked with special formatting in the interview.</param>
		/// <param name="logRef">A string to display in logs related to this request.</param>
		/// <returns>An object which contains an HTML fragment to be inserted in a web page to display the interview.</returns>
		public InterviewResult GetInterview(ITemplate template, TextReader answers, InterviewSettings settings, IEnumerable<string> markedVariables, string logRef)
		{
			// Validate input parameters, creating defaults as appropriate.
			string logStr = logRef == null ? string.Empty : logRef;

			if (template == null)
				throw new ArgumentNullException("template", string.Format(@"Cloud.Services.GetInterview: the ""template"" parameter passed in was null, logRef: {0}", logStr));

			if (settings == null)
				settings = new InterviewSettings();

			// Configure interview settings
			settings.Settings["OmitImages"] = "true"; // Instructs HDS not to return images used by the interview; we'll get them ourselves from the template folder.
			settings.Settings["OmitDefinitions"] = "true"; // Instructs HDS not to return interview definitions; we'll get them ourselves from the template folder.
			settings.Settings["TempInterviewUrl"] = Util.GetInterviewImageUrl(settings, template);
			settings.Settings["InterviewDefUrl"] = Util.GetInterviewDefinitionUrl(settings, template);
			settings.MarkedVariables = (string[])(markedVariables ?? new string[0]);

			// Get the interview.
			InterviewResult result = new InterviewResult();
			BinaryObject[] interviewFiles = null;
			using (var client = new SoapClient(_subscriberID, _signingKey, HostAddress, ProxyAddress))
			{
				interviewFiles = client.GetInterview(
					template,
					answers == null ? "" : answers.ReadToEnd(),
					settings,
					logRef
				);

				// Throw an exception if we do not have exactly one interview file.
				// Although interviewFiles could potentially contain more than one item, the only one we care about is the
				// first one, which is the HTML fragment. All other items, such as interview definitions (.JS and .DLL files)
				// or dialog element images are not needed, because we can get them out of the package file instead. 
				// We enforce this by setting the OmitImages and OmitDefinitions values above, so we will always have exactly one item here.
				if (interviewFiles.Length != 1)
					throw new Exception();

				StringBuilder htmlFragment = new StringBuilder(Util.ExtractString(interviewFiles[0]));

				Util.AppendSdkScriptBlock(htmlFragment, template, settings);
				result.HtmlFragment = htmlFragment.ToString();
			}

			return result;
		}

		/// <summary>
		/// Assembles a document from the given template, answers and settings.
		/// </summary>
		/// <param name="template">The template to assemble.</param>
		/// <param name="answers">The answers to use during the assembly.</param>
		/// <param name="settings">The settings for the assembly.</param>
		/// <include file="../../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
		/// <returns>An <c>AssembleDocumentResult</c> that contains the results of the assembly.</returns>
		public AssembleDocumentResult AssembleDocument(ITemplate template, System.IO.TextReader answers, AssembleDocumentSettings settings, string logRef)
		{
			// Validate input parameters, creating defaults as appropriate.
			string logStr = logRef == null ? string.Empty : logRef;

			if (template == null)
				throw new ArgumentNullException("template", string.Format(@"Cloud.Services.AssembleDocument: the ""template"" parameter passed in was null, logRef: {0}", logStr));

			if (settings == null)
				settings = new AssembleDocumentSettings();

			AssembleDocumentResult result = null;
			AssemblyResult asmResult = null;

			using (var client = new SoapClient(_subscriberID, _signingKey, HostAddress, ProxyAddress))
			{
				asmResult = client.AssembleDocument(
					template,
					answers == null ? "" : answers.ReadToEnd(),
					settings,
					logRef
				);
			}

			if (asmResult != null)
			{
				result = Util.ConvertAssemblyResult(template, asmResult, settings.Format);
			}

			return result;
		}

		/// <summary>
		/// Returns metadata about the variables/types (and optionally dialogs &amp; mapping info)
		/// for the indicated template's interview.
		/// </summary>
		/// <param name="template">The template for which to retrieve component information.</param>
		/// <param name="includeDialogs">Indicates whether or not information about dialogs should be included.</param>
		/// <param name="logRef">This parameter lets you specify information that will be included in usage logs for this call. For example, you can use a string to uniquely identify the end user that initiated the request and/or the context in which the call was made. When you review usage logs, you can then see which end users initiated each request. That information could then be used to pass costs on to those end users if desired.</param>
		/// <returns></returns>
		public ComponentInfo GetComponentInfo(ITemplate template, bool includeDialogs, string logRef)
		{
			// Validate input parameters, creating defaults as appropriate.
			string logStr = logRef == null ? string.Empty : logRef;
			if (template == null)
				throw new ArgumentNullException("template", @"Cloud.Services.GetComponentInfo: The ""template"" parameter must not be null, logRef: " + logStr);

			ComponentInfo result;
			using (var client = new SoapClient(_subscriberID, _signingKey, HostAddress, ProxyAddress))
			{
				result = client.GetComponentInfo(template, includeDialogs, logRef);
			}
			return result;
		}

		/// <summary>
		/// This method overlays any answer collections passed into it, into a single XML answer collection.
		/// </summary>
		/// <param name="answers">The answers to be overlayed.</param>
		/// <param name="logRef">This parameter lets you specify information that will be included in usage logs for this call. For example, you can use a string to uniquely identify the end user that initiated the request and/or the context in which the call was made. When you review usage logs, you can then see which end users initiated each request. That information could then be used to pass costs on to those end users if desired.</param>
		/// <returns>The consolidated XML answer collection.</returns>
		public string GetAnswers(IEnumerable<System.IO.TextReader> answers, string logRef)
		{
			// Validate input parameters, creating defaults as appropriate.
			string logStr = logRef == null ? string.Empty : logRef;
			if (answers == null)
				throw new ArgumentNullException("answers", "The answers collection must not be null, logRef: " + logStr);

			BinaryObject combinedAnswers;
			using (SoapClient client = new SoapClient(_subscriberID, _signingKey, HostAddress, ProxyAddress))
			{
				var answerObjects = (from answer in answers select Util.GetBinaryObjectFromTextReader(answer)).ToArray();
				combinedAnswers = client.GetAnswers(answerObjects, logRef);
			}
			return Util.ExtractString(combinedAnswers);
		}

		/// <summary>
		/// This method does nothing in the case of HotDocs Cloud Services because the template package already contains all 
		/// of the interview runtime ("support") files required to display an interview for the template. These files are built
		/// by HotDocs Developer at the time the package is created, and Cloud Services does not have the ability to re-create them.
		/// </summary>
		/// <param name="template">The template for which support files will be built.</param>
		/// <param name="flags">Indicates what types of support files to build.</param>
		public void BuildSupportFiles(ITemplate template, HDSupportFilesBuildFlags flags)
		{
			if (template == null)
				throw new ArgumentNullException("template", @"Cloud.Services.BuildSupportFiles: the ""template"" parameter passed in was null");
			// no op
		}

		/// <summary>
		/// This method does nothing in the case of HotDocs Cloud Services because the template package already contains all 
		/// of the interview runtime ("support") files required to display an interview for the template. These files are built
		/// by HotDocs Developer at the time the package is created, and Cloud Services simply uses the files from the package
		/// rather than building and caching them separately.
		/// </summary>
		/// <param name="template">The template for which support files will be removed.</param>
		public void RemoveSupportFiles(ITemplate template)
		{
			if (template == null)
				throw new ArgumentNullException("template", @"Cloud.Services.RemoveSupportFiles: the ""template"" parameter passed in was null");
			// no op
		}

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
		public Stream GetInterviewFile(ITemplate template, string fileName, string fileType)
		{
			// Validate input parameters, creating defaults as appropriate.
			if (template == null)
				throw new ArgumentNullException("template", @"Cloud.Services.GetInterviewFile: the ""template"" parameter passed in was null");

			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("fileName", @"Cloud.Services.GetInterviewFile: the ""fileName"" parameter passed in was null or empty");

			if (string.IsNullOrEmpty(fileType))
				throw new ArgumentNullException("fileType", @"Cloud.Services.GetInterviewFile: the ""fileType"" parameter passed in was null or empty");

			// Return an image or interview definition from the template.
			return template.Location.GetFile(fileName + (fileType.ToLower() == "img" ? "" : "." + fileType));
		}

		#endregion

	}
}
