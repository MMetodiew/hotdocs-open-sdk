/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HotDocs.Sdk.Server.Contracts;
using hdsi = HotDocs.Server.Interop;

namespace HotDocs.Sdk.Server.Local
{
	/// <summary>
	/// <c>The Local.Services class provides the local implementation of IServices, meaning that it provides
	/// an implementation that expects HotDocs Server to be installed on the same machine as the host application.</c>
	/// </summary>
	public class Services : IServicesUsingTemplatesOnDisk
	{
		private HotDocs.Server.Application _app;
		private string _tempPath = null;

		/// <summary>
		/// Construct a new instance of <c>Local.Services</c>.
		/// </summary>
		/// <param name="tempPath">A path to a folder for storing temporary files.</param>
		public Services(string tempPath)
		{
			//Parameter validation.
			if (string.IsNullOrEmpty(tempPath))
				throw new Exception("Non-empty path expected.");
			if (!Directory.Exists(tempPath))
				throw new Exception("The folder \"" + tempPath + "\" does not exist.");

			_app = new HotDocs.Server.Application();
			_tempPath = tempPath;
		}

		#region IServices implementation

		/// <summary>
		/// GetComponentInfo returns metadata about the variables/types (and optionally dialogs and mapping info)
		/// for the indicated template's interview.
		/// </summary>
		/// <param name="template">The template for which to return a ComponentInfo object.</param>
		/// <param name="includeDialogs">True if dialog components are to be included in the returned <c>ComponentInfo</c>.</param>
		/// <include file="../../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
		/// <returns></returns>
        public ComponentInfo GetComponentInfo(IOnDiskTemplate template, bool includeDialogs, string logRef)
		{
			string logStr = logRef == null ? string.Empty : logRef;

			// Validate input parameters, creating defaults as appropriate.
			if (template == null)
				throw new ArgumentNullException("template", @"Local.Services.GetInterviewFile: the ""template"" parameter passed in was null or empty, logRef: " + logStr);

			string templateFilePath = template.GetFullPath();

			//Get the hvc path. GetHvcPath will also validate the template file name.
			string hvcPath;
			GetHvcPath(templateFilePath, out hvcPath);
			if (hvcPath.Length == 0)
				throw new Exception("Invalid template path.");
			if (!File.Exists(hvcPath))
				throw new HotDocs.Server.HotDocsServerException("You do not have read access to the variable collection file (.hvc)");

			// Instantiate a ComponentInfo object to hold the data to be returned
			ComponentInfo cmpInfo = new ComponentInfo();

			// Load the HVC variable collection
			using (HotDocs.Server.VariableCollection varCol = new HotDocs.Server.VariableCollection(hvcPath))
			{
				for (int i = 0; i < varCol.Count; i++)
					cmpInfo.AddVariable(new Contracts.VariableInfo { Name = varCol.VarName(i), Type = AnsTypeToString(varCol.VarType(i)) });
			}

			// Load the Dialogs (if requested)
			if (includeDialogs)
			{
				// Load the component collection from disk
				using (var cmpColl = new HotDocs.Server.ComponentCollection())
				{
					cmpColl.Open(templateFilePath, hdsi.CMPOpenOptions.LoadAllCompLibs);
					// Iterate through the component collection and add dialogs to ComponentInfo
					foreach (HotDocs.Server.Component cmp in cmpColl)
					{
						if (cmp.Type == hdsi.hdCmpType.hdDialog)
						{
							// Fetch dialog properties (AnswerSource, Repeat, variable list and mappingNames) from the component collection
							object[] variableNames = null;
							object[] mappingNames = null;
							DialogInfo dlgInfo = new DialogInfo();
							dlgInfo.Name = cmp.Name;

							using (HotDocs.Server.ComponentProperties properties = cmp.Properties)
							{
								using (HotDocs.Server.ComponentProperty p = (HotDocs.Server.ComponentProperty)properties["Variables"])
								{
									variableNames = (object[])p.Value;
								};
								using (HotDocs.Server.ComponentProperty p = (HotDocs.Server.ComponentProperty)properties["IsRepeated"])
								using (HotDocs.Server.ComponentProperty p2 = (HotDocs.Server.ComponentProperty)properties["IsSpread"])
								{
									dlgInfo.Repeat = (bool)p.Value || (bool)p2.Value;
								};
								using (HotDocs.Server.ComponentProperty p = (HotDocs.Server.ComponentProperty)properties["AnswerSource"])
								{
									dlgInfo.AnswerSource = ((p != null) && (!String.IsNullOrWhiteSpace((string)p.Value))) ? (string)p.Value : null;
								};
								if (dlgInfo.AnswerSource != null)
								{
									using (HotDocs.Server.ComponentProperty p = (HotDocs.Server.ComponentProperty)properties["MappingNames"])
									{
										mappingNames = (object[])p.Value;
									};
								}
							}

							// Add the mapping information to the dialog
							// Note that variableNames.Length is always equal to mappingNames.Length if mappingNames.Length is not null.
							for (int i = 0; i < variableNames.Length; i++)
							{
								string variableName = variableNames[i].ToString();
								if (cmpInfo.IsDefinedVariable(variableName))
								{
									//The i < mappingNames.Length test here is strictly defensive.
									string mappingName = (mappingNames != null && i < mappingNames.Length) ? mappingNames[i].ToString() : null;

									dlgInfo.Items.Add(new DialogItemInfo
									{
										Name = variableName,
										Mapping = String.IsNullOrWhiteSpace(mappingName) ? null : mappingName
									});
								}
							}

							// Adds the dialog to ComponentInfo object IF it contains any variables that were in the HVC
							if (dlgInfo.Items.Count > 0)
								cmpInfo.AddDialog(dlgInfo);
						}
					}
				}
			}
			return cmpInfo;
		}
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
        public InterviewResult GetInterview(IOnDiskTemplate template, TextReader answers, InterviewSettings settings, IEnumerable<string> markedVariables, string logRef)
		{
			// Validate input parameters, creating defaults as appropriate.
			string logStr = logRef == null ? string.Empty : logRef;

			if (template == null)
				throw new ArgumentNullException("template", string.Format(@"Local.Services.GetInterview: the ""template"" parameter passed in was null, logRef: {0}", logStr));

			if (settings == null)
				settings = new InterviewSettings();

			// HotDocs Server reads the following settings out of the registry all the time; therefore these items are ignored when running against Server:
			//		settings.AddHdMainDiv
			//		settings.AnswerSummary.*
			//		settings.DefaultDateFormat
			//		settings.DefaultUnansweredFormat
			//		settings.HonorCmpUnansweredFormat
			//		settings.DisableAnswerSummary

			// HotDocs Server does not include the following settings in its .NET or COM APIs, so Util.AppendSdkScriptBlock (below)
			// includes them with the interview script block:
			//		settings.Locale
			//		settings.NextFollowsOutline
			//		settings.ShowAllResourceButtons

			hdsi.interviewFormat fmt;
			switch (settings.Format)
			{
				case InterviewFormat.JavaScript:
					fmt = hdsi.interviewFormat.javascript;
					break;
				case InterviewFormat.Silverlight:
					fmt = hdsi.interviewFormat.Silverlight;
					break;
				default:
					fmt = hdsi.interviewFormat.Unspecified;
					break;
			}

			// Configure the interview options
			hdsi.HDInterviewOptions itvOpts = hdsi.HDInterviewOptions.intOptNoImages; // Instructs HDS not to return images used by the interview; we'll get them ourselves from the template folder.

			if (settings.DisableDocumentPreview)
				itvOpts |= hdsi.HDInterviewOptions.intOptNoPreview; // Disables (omits) the Document Preview button on the interview toolbar.
			if (settings.DisableSaveAnswers)
				itvOpts |= hdsi.HDInterviewOptions.intOptNoSave; // Disables (omits) the Save Answers button on the interview toolbar.
			if (settings.RoundTripUnusedAnswers)
				itvOpts |= hdsi.HDInterviewOptions.intOptStateless; // Prevents original answer file from being encrypted and sent to the interview and then posted back at the end.

			// Get the interview.
			InterviewResult result = new InterviewResult();

			StringBuilder htmlFragment;
			using (var ansColl = new HotDocs.Server.AnswerCollection())
			{
				if (answers != null)
				{
					if (answers.Peek() == 0xFEFF)
						answers.Read(); // discard BOM if present
					ansColl.XmlAnswers = answers.ReadToEnd();
				}

				if (markedVariables == null)
					_app.UnansweredVariablesList = new string[0];
				else
					_app.UnansweredVariablesList = markedVariables;

				htmlFragment = new StringBuilder(
					_app.GetInterview(
						template.GetFullPath(),
						template.Key,
						fmt,
						itvOpts,
						settings.InterviewRuntimeUrl,
						settings.StyleSheetUrl + "/" + settings.ThemeName + ".css",
						ansColl,
						settings.PostInterviewUrl,
						settings.Title,
						Util.GetInterviewDefinitionUrl(settings, template),
						null, // the path to which HDS should copy interview images; also the path that may become part of the DocumentPreviewStateString & passed to document preview handler
						Util.GetInterviewImageUrl(settings, template),
						settings.SaveAnswersUrl,
						settings.DocumentPreviewUrl)
					);
			}
			Util.AppendSdkScriptBlock(htmlFragment, template, settings);

			result.HtmlFragment = htmlFragment.ToString();
			return result;
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
		public Stream GetInterviewFile(IOnDiskTemplate template, string fileName, string fileType)
		{
			// Validate input parameters, creating defaults as appropriate.
			if (template == null)
				throw new ArgumentNullException("template", @"Local.Services.GetInterviewFile: the ""template"" parameter passed in was null");

			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentNullException("fileName", @"Local.Services.GetInterviewFile: the ""fileName"" parameter passed in was null or empty");

			if (string.IsNullOrEmpty(fileType))
				throw new ArgumentNullException("fileType", @"Local.Services.GetInterviewFile: the ""fileType"" parameter passed in was null or empty");

			// Return an image or interview definition from the template.
			switch (fileType.ToUpper())
			{
				case "IMG":
					return template.Location.GetFile(fileName);
				default:
					string interviewDefPath = _app.GetInterviewDefinitionFromTemplate(
						Path.Combine(Path.GetDirectoryName(template.GetFullPath()), fileName),
						fileName,
						fileType == "dll" ? hdsi.interviewFormat.Silverlight : hdsi.interviewFormat.javascript
						);
					return File.OpenRead(interviewDefPath);
			}
		}
		/// <summary>
		/// Assemble a document from the given template, answers and settings.
		/// </summary>
		/// <param name="template">An instance of the Template class.</param>
		/// <param name="answers">Either an XML answer string, or a string containing encoded
		/// interview answers as posted from a HotDocs browser interview.</param>
		/// <param name="settings">An instance of the AssembleDocumentResult class.</param>
		/// <include file="../Shared/Help.xml" path="Help/string/param[@name='logRef']"/>
		/// <returns>An AssemblyResult object containing all the files and data resulting from the request.</returns>
        public OnDiskAssembleDocumentResult AssembleDocument(IOnDiskTemplate template, TextReader answers, AssembleDocumentSettings settings, string logRef)
		{
			// Validate input parameters, creating defaults as appropriate.
			string logStr = logRef == null ? string.Empty : logRef;
			if (template == null)
				throw new ArgumentNullException("template", string.Format(@"Local.Services.AssembleDocument: the ""template"" parameter passed in was null, logRef: {0}", logStr));

			if (settings == null)
				settings = new AssembleDocumentSettings();


			HotDocs.Server.AnswerCollection ansColl = new HotDocs.Server.AnswerCollection();
			ansColl.OverlayXMLAnswers(answers == null ? "" : answers.ReadToEnd());
			HotDocs.Server.OutputOptions outputOptions = ConvertOutputOptions(settings.OutputOptions);

			string docPath = CreateTempDocDirAndPath(template, settings.Format);
			_app.AssembleDocument(
				template.GetFullPath(),//Template path
				settings.UseMarkupSyntax ? hdsi.HDAssemblyOptions.asmOptMarkupView : hdsi.HDAssemblyOptions.asmOptNone,
				ansColl,
				docPath,
				outputOptions);

			//Prepare the post-assembly answer set (dropping transient ("don't save") answers when appropriate)
			HotDocs.Sdk.AnswerCollection resultAnsColl = new AnswerCollection();
			resultAnsColl.ReadXml(new StringReader(ansColl.XmlAnswers));
			string resultAnsXml = resultAnsColl.GetXMLString(false, settings.RetainTransientAnswers || _app.PendingAssemblyCmdLineStrings.Count > 0);

			//Build the list of pending assemblies.
			List<IOnDiskTemplate> pendingAssemblies = new List<IOnDiskTemplate>();
			for (int i = 0; i < _app.PendingAssemblyCmdLineStrings.Count; i++)
			{
				string cmdLine = _app.PendingAssemblyCmdLineStrings[i];
				string path, switches;
				Util.ParseHdAsmCmdLine(cmdLine, out path, out switches);
				pendingAssemblies.Add(new DiskAccessibleTemplate(Path.GetFileName(path), template.Location.Duplicate(), switches));
			}

			//Prepare the document stream and image information for the browser.
			DocumentType docType = settings.Format;
			List<NamedStream> supportingFiles = new List<NamedStream>();
			MemoryStream docStream;
			if (docType == DocumentType.Native)
			{
				docType = Document.GetDocumentType(docPath);
				docStream = LoadFileIntoMemStream(docPath);
			}
			else if (docType == DocumentType.HTMLwDataURIs)
			{
				//If the consumer requested both HTML and HTMLwDataURIs, they'll only get the latter.
				string content = Util.EmbedImagesInURIs(docPath);
				docStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
			}
			else if (docType == DocumentType.MHTML)
			{
				string content = Util.HtmlToMultiPartMime(docPath);
				docStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
			}
			else if (docType == DocumentType.HTML)
			{
				string targetFilenameNoExtention = Path.GetFileNameWithoutExtension(docPath);
				foreach (string img in Directory.EnumerateFiles(Path.GetDirectoryName(docPath)))
				{
					string ext = Path.GetExtension(img).ToLower();
					if (Path.GetFileName(img).StartsWith(targetFilenameNoExtention) && (ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".png" || ext == ".bmp"))
						supportingFiles.Add(LoadFileIntoNamedStream(img));
				}

				docStream = LoadFileIntoMemStream(docPath);
			}
			else
			{
				docStream = LoadFileIntoMemStream(docPath);
			}

			//Now that we've loaded all of the assembly results into memory, remove the assembly files.
			FreeTempDocDir(docPath);

			//Return the results.
			Document document = new Document(template, docStream, docType, supportingFiles.ToArray(), _app.UnansweredVariablesList.ToArray());
            OnDiskAssembleDocumentResult result = new OnDiskAssembleDocumentResult(document, resultAnsXml, pendingAssemblies.ToArray(), _app.UnansweredVariablesList.ToArray());
			return result;
		}
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
		public string GetAnswers(IEnumerable<TextReader> answers, string logRef)
		{
			// Validate input parameters, creating defaults as appropriate.
			string logStr = logRef == null ? string.Empty : logRef;
			if (answers == null)
				throw new ArgumentNullException("answers", @"Local.Services.GetAnswers: The ""answers"" parameter must not be null, logRef: " + logStr);

			string result = "";
			using (HotDocs.Server.AnswerCollection hdsAnsColl = new HotDocs.Server.AnswerCollection())
			{
				foreach (TextReader tr in answers)
					hdsAnsColl.OverlayXMLAnswers(tr.ReadToEnd());
				result = hdsAnsColl.XmlAnswers;
			}
			return result;
		}
		/// <summary>
		/// Build the server files for the specified template.
		/// </summary>
		/// <param name="template">The template for which support files will be built.</param>
		/// <param name="flags">Indicates what types of support files to build.</param>
		public void BuildSupportFiles(IOnDiskTemplate template, HDSupportFilesBuildFlags flags)
		{
			if (template == null)
				throw new ArgumentNullException("template", @"Local.Services.BuildSupportFiles: the ""template"" parameter passed in was null");
			using (HotDocs.Server.Application app = new HotDocs.Server.Application())
			{
				hdsi.HDSupportFilesBuildFlags hdBuildFlags = 0;
				if ((flags & HDSupportFilesBuildFlags.BuildJavaScriptFiles) != 0)
					hdBuildFlags |= hdsi.HDSupportFilesBuildFlags.BuildJavaScriptFiles;
				if ((flags & HDSupportFilesBuildFlags.BuildSilverlightFiles) != 0)
					hdBuildFlags |= hdsi.HDSupportFilesBuildFlags.BuildSilverlightFiles;
				if ((flags & HDSupportFilesBuildFlags.ForceRebuildAll) != 0)
					hdBuildFlags |= hdsi.HDSupportFilesBuildFlags.ForceRebuildAll;
				if ((flags & HDSupportFilesBuildFlags.IncludeAssembleTemplates) != 0)
					hdBuildFlags |= hdsi.HDSupportFilesBuildFlags.IncludeAssembleTemplates;

				app.BuildSupportFiles(template.GetFullPath(), template.Key, hdBuildFlags);
			}
		}
		/// <summary>
		/// Remove the server files for the specified template.
		/// </summary>
		/// <param name="template">The template for which support files will be removed.</param>
		public void RemoveSupportFiles(IOnDiskTemplate template)
		{
			if (template == null)
				throw new ArgumentNullException("template", @"Local.Services.RemoveSupportFiles: the ""template"" parameter passed in was null");
			using (HotDocs.Server.Application app = new HotDocs.Server.Application())
			{
				app.RemoveSupportFiles(template.GetFullPath(), template.Key);
			}
		}

		#endregion

		/// <summary>
		/// Create a new directory and a new temporary file in that directory.
		/// Use this method in conjunction with FreeTempDocDir to free the folder and its contents.
		/// </summary>
		/// <param name="template">The template for which to create a temporary document directory.</param>
		/// <param name="docType">The type of document for which to create the temporary directory (and file).</param>
		/// <returns>The file name and path of the temporary file.</returns>
		private string CreateTempDocDirAndPath(IOnDiskTemplate template, DocumentType docType)
		{
			string dirPath;
			string ext = Template.GetDocExtension(docType, template);
			do
			{
				dirPath = Path.Combine(_tempPath, Path.GetRandomFileName());
			} while (Directory.Exists(dirPath));
			Directory.CreateDirectory(dirPath);
			string filePath = Path.Combine(dirPath, Path.GetRandomFileName() + ext);
			using (File.Create(filePath)) { }
			return filePath;
		}
		/// <summary>
		/// Free a folder and its content.
		/// </summary>
		/// <param name="docPath">A temporary document path returned by CreateTempDocDirAndPath.</param>
		private void FreeTempDocDir(string docPath)
		{
			string dir = Path.GetDirectoryName(docPath);
			Directory.Delete(dir, true);
		}

		private MemoryStream LoadFileIntoMemStream(string filePath)
		{
			MemoryStream memStream = new MemoryStream();
			using (FileStream fs = File.OpenRead(filePath))
			{
				fs.CopyTo(memStream);
				memStream.Position = 0;
			}
			return memStream;
		}

		private NamedStream LoadFileIntoNamedStream(string filePath)
		{
			return new NamedStream(filePath, LoadFileIntoMemStream(filePath));
		}

		private const string c_templateIDRegEx = @"^[^\\/:*?""<>|\r\n]+\.(?:docx|rtf|wpt|hpt|hft|ttx|cmp){1}\z";

		private static bool CheckTemplateId(string val)
		{
			return (val != null
				&& Regex.IsMatch(val, c_templateIDRegEx, RegexOptions.IgnoreCase));
		}

		private static void GetHvcPath(string templateFilePath, out string hvcPath)
		{
			string templateID = Path.GetFileName(templateFilePath);

			if (!CheckTemplateId(templateID))
				throw new HotDocs.Server.HotDocsServerException("Invalid template ID");

			//Calculate the hvc path. This file should be in the same directory
			// and have the same name as the template file. They should only differ by extension.
			hvcPath = System.IO.Path.ChangeExtension(templateFilePath, ".hvc");
		}

		private static string AnsTypeToString(hdsi.ansType type)
		{
			switch (type)
			{
				case hdsi.ansType.ansTypeText:
					return "Text";
				case hdsi.ansType.ansTypeNumber:
					return "Number";
				case hdsi.ansType.ansTypeDate:
					return "Date";
				case hdsi.ansType.ansTypeTF:
					return "True/False";
				case hdsi.ansType.ansTypeMC:
					return "Multiple Choice";
				case hdsi.ansType.ansTypeDocText://Not needed in this context.
				default:
					return "Unknown";
			}
		}

		private HotDocs.Server.OutputOptions ConvertOutputOptions(OutputOptions sdkOpts)
		{
			HotDocs.Server.OutputOptions hdsOpts = null;
			if (sdkOpts is PdfOutputOptions)
			{
				PdfOutputOptions sdkPdfOpts = (PdfOutputOptions)sdkOpts;
				HotDocs.Server.PdfOutputOptions hdsPdfOpts = new HotDocs.Server.PdfOutputOptions();

				hdsPdfOpts.Author = sdkPdfOpts.Author;
				hdsPdfOpts.Comments = sdkPdfOpts.Comments;
				hdsPdfOpts.Company = sdkPdfOpts.Company;
				hdsPdfOpts.Keywords = sdkPdfOpts.Keywords;
				hdsPdfOpts.Subject = sdkPdfOpts.Subject;
				hdsPdfOpts.Title = sdkPdfOpts.Title;

				//Remember that these are PDF passwords which are not highly secure.
				hdsPdfOpts.OwnerPassword = sdkPdfOpts.OwnerPassword;
				hdsPdfOpts.UserPassword = sdkPdfOpts.UserPassword;

				hdsi.PdfOutputFlags hdsFlags = 0;
				if (sdkPdfOpts.EmbedFonts)
					hdsFlags |= hdsi.PdfOutputFlags.pdfOut_EmbedFonts;
				if (sdkPdfOpts.KeepFillablePdf)
					hdsFlags |= hdsi.PdfOutputFlags.pdfOut_KeepFillablePdf;
				if (sdkPdfOpts.PdfA)
					hdsFlags |= hdsi.PdfOutputFlags.pdfOut_PdfA;
				if (sdkPdfOpts.TaggedPdf)
					hdsFlags |= hdsi.PdfOutputFlags.pdfOut_TaggedPdf;
				if (sdkPdfOpts.TruncateFields)
					hdsFlags |= hdsi.PdfOutputFlags.pdfOut_TruncateFields;
				hdsPdfOpts.PdfOutputFlags = hdsFlags;

				hdsi.PdfPermissions hdsPerm = 0;
				if (sdkPdfOpts.Permissions.HasFlag(PdfPermissions.Copy))
					hdsPerm |= hdsi.PdfPermissions.COPY;
				if (sdkPdfOpts.Permissions.HasFlag(PdfPermissions.Modify))
					hdsPerm |= hdsi.PdfPermissions.MOD;
				if (sdkPdfOpts.Permissions.HasFlag(PdfPermissions.Print))
					hdsPerm |= hdsi.PdfPermissions.PRINT;
				hdsPdfOpts.PdfPermissions = hdsPerm;

				hdsOpts = hdsPdfOpts;
			}
			else if (sdkOpts is HtmlOutputOptions)
			{
				HtmlOutputOptions sdkHtmOpts = (HtmlOutputOptions)sdkOpts;
				HotDocs.Server.HtmlOutputOptions hdsHtmOpts = new HotDocs.Server.HtmlOutputOptions();

				hdsHtmOpts.Author = sdkHtmOpts.Author;
				hdsHtmOpts.Comments = sdkHtmOpts.Comments;
				hdsHtmOpts.Company = sdkHtmOpts.Company;
				hdsHtmOpts.Keywords = sdkHtmOpts.Keywords;
				hdsHtmOpts.Subject = sdkHtmOpts.Subject;
				hdsHtmOpts.Title = sdkHtmOpts.Title;

				hdsHtmOpts.Encoding = sdkHtmOpts.Encoding;

				hdsOpts = hdsHtmOpts;
			}
			else if (sdkOpts is TextOutputOptions)
			{
				TextOutputOptions sdkTxtOpts = (TextOutputOptions)sdkOpts;
				HotDocs.Server.TextOutputOptions hdsTxtOpts = new HotDocs.Server.TextOutputOptions();
				hdsTxtOpts.Encoding = sdkTxtOpts.Encoding;
				hdsOpts = hdsTxtOpts;
			}

			return hdsOpts;
		}
	}
}
