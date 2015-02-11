/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using HotDocs.Sdk;
using HotDocs.Sdk.Server.Contracts;
using HotDocs.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HotDocs.Sdk.Server
{

	internal delegate string TemplateFilesRetriever(string templateKey, string state);

	internal delegate Stream TemplatePackageRetriever(string packageID, string state, out bool closeStream);

	/// <summary>
	/// The <c>Util</c> class provides some methods needed for this class. Some of those methods are used for a specific implementation of IServices.
	/// </summary>
	public class Util
	{
		internal static void SafeDeleteFolder(string folder)
		{
			if (folder != null && folder != "" && folder[0] != '\\')
				Directory.Delete(folder, true);
		}

		internal static void ParseHdAsmCmdLine(string cmdLine, out string path, out string switches)
		{
			path = "";
			switches = "";
			cmdLine = cmdLine.Trim();

			if (cmdLine.Length > 0)
			{
				if (cmdLine[0] == '\"')
				{
					//Handle the case where the path is defined by quotation marks.
					int pathEnd = cmdLine.IndexOf('\"');
					if (pathEnd == -1)
					{
						path = cmdLine.Substring(1, cmdLine.Length - 1);
					}
					else
					{
						path = cmdLine.Substring(1, pathEnd - 1);
						switches = cmdLine.Substring(pathEnd + 1, cmdLine.Length - pathEnd - 1);
					}
					return;
				}
				else
				{
					//Finally, we look for switches in the path.
					int switchStart = cmdLine.IndexOf('/');
					if (switchStart == -1)
					{
						path = cmdLine;
					}
					else
					{
						path = cmdLine.Substring(0, switchStart);
						switches = cmdLine.Substring(switchStart, cmdLine.Length - switchStart);
					}
				}

				////Remove any folder information from the path, just in case. It's not allowed.
				//int lastBackSlash = path.LastIndexOf('\\');
				//int lastFwdSlash = path.LastIndexOf('/');
				//if (lastBackSlash > -1 || lastFwdSlash > -1)
				//{
				//	int lastSlash = lastBackSlash > lastFwdSlash ? lastBackSlash : lastFwdSlash;
				//	if (lastSlash >= path.Length - 1)
				//		path = "";
				//	else
				//		path = path.Substring(lastSlash + 1, path.Length - lastSlash - 1);
				//}

				//Remove any extra whitespace.
				path = path.Trim();
				switches = switches.Trim();
			}
		}

		internal static string DecryptServerString(string templateLocator)
		{
			string decryptedString = UtilityTools.DecryptString(templateLocator);
			int separatorIndex = decryptedString.IndexOf('|');
			return (separatorIndex >= 0) ? decryptedString.Substring(0, separatorIndex) : decryptedString;
		}

		internal static string ExtractString(BinaryObject obj)
		{
			string extractedString;
			Encoding enc = null;

			if (obj.DataEncoding != null)
			{
				try
				{
					enc = System.Text.Encoding.GetEncoding(obj.DataEncoding);
				}
				catch (ArgumentException)
				{
					enc = null;
				}
			}

			// BinaryObjects containing textual information from the HotDocs web service
			// should always have DataEncoding set to the official IANA name of a text encoding.
			// Therefore enc should always be non-null at this point.  However, in case of
			// unexpected input, we include some alternative methods below.
			System.Diagnostics.Debug.Assert(enc != null);

			if (enc != null)
			{
				extractedString = enc.GetString(obj.Data);
			}
			else
			{
				using (var ms = new MemoryStream(obj.Data))
				{
					using (var tr = (TextReader)new StreamReader(ms))
					{
						extractedString = tr.ReadToEnd();
					}
				}
			}
			// discard BOM if there is one
			if (extractedString[0] == 0xFEFF)
				return extractedString.Substring(1);
			else
				return extractedString;
		}

		internal static void AppendSdkScriptBlock(StringBuilder interview, ITemplate template, InterviewSettings settings)
		{
			// Append the SDK specific script block begin
			interview.AppendLine();
			interview.AppendLine("<script type=\"text/javascript\">");

			// Append the template locator variable.
			interview.AppendFormat("HDTemplateLocator=\"{0}\";", template.CreateLocator());
			interview.AppendLine();

			// Append the interview locale (if the host app has overridden the server default)
			if (!String.IsNullOrEmpty(settings.Locale))
			{
				interview.AppendFormat("HDInterviewLocale=\"{0}\";", settings.Locale);
				interview.AppendLine();
			}
			// Append the "Next follows outline" setting (if the host app has overridden the server default)
			if (settings.NextFollowsOutline != Tristate.Default)
			{
				interview.AppendFormat("HDOutlineInOrder={0};", settings.NextFollowsOutline == Tristate.True ? "true" : "false");
				interview.AppendLine();
			}
			// Append the "Show all resource buttons" setting (if the host app has overridden the server default)
			if (settings.ShowAllResourceButtons != Tristate.Default)
			{
				interview.AppendFormat("HDShowAllResourceBtns={0};", settings.ShowAllResourceButtons == Tristate.True ? "true" : "false");
				interview.AppendLine();
			}

			if ((!string.IsNullOrEmpty(settings.AnswerFileDataServiceUrl) || (settings.CustomDataSources != null)))
			{
				// The SDK user has configured some data sources addresses to send down to the browser interview. Append a table
				// that maps data source names to data service addresses so that the browser will know how to request data for
				// a data source.

				// Get the data sources used by the template and all its dependencies.
				TemplateManifest templateManifest = template.GetManifest(ManifestParseFlags.ParseDataSources | ManifestParseFlags.ParseRecursively);

				Dictionary<string, string> dataSourceDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				string address;
				foreach (DataSource dataSource in templateManifest.DataSources)
				{
					address = null;
					switch (dataSource.Type)
					{
						case DataSourceType.AnswerFile:
							{
								address = settings.AnswerFileDataServiceUrl;
								break;
							}
						case DataSourceType.Custom:
							{
								if (settings.CustomDataSources != null)
								{
									settings.CustomDataSources.TryGetValue(dataSource.Name, out address);
								}
								break;
							}
					}

					if ((address != null) && !dataSourceDict.ContainsKey(dataSource.Name))
						dataSourceDict.Add(dataSource.Name, address);
				}

				// Now write out all unique data sources.
				interview.AppendLine("HDDataSources=[");

				int i = 0;
				foreach (var dataSource in dataSourceDict)
				{
					interview.AppendFormat("\t{{Name:\"{0}\", Address:\"{1}\"", dataSource.Key, dataSource.Value);
					interview.AppendLine((i++ < dataSourceDict.Count - 1) ? "}," : "}");
				}

				interview.AppendLine("];");
			}

			// Append the SDK specific script block end
			interview.AppendLine("</script>");
		}

		internal static string GetInterviewDefinitionUrl(InterviewSettings settings, ITemplate template)
		{
			// Start with the base InterviewFilesUrl and see if it already has a query string.
			//  If so, we will just be adding parameters. Otherwise, we will be appending a new query string to the base url.
			string url = settings.InterviewFilesUrl;
			url += url.Contains("?") ? "&" : "?";
			url += ("loc=" + template.CreateLocator());
			return url;
		}

		internal static string GetInterviewImageUrl(InterviewSettings settings, ITemplate template)
		{
			// This is the same as the interview definition URL, but we also add the type and template parameters to the query string.
			return GetInterviewDefinitionUrl(settings, template) + "&type=img&template=";
		}

		internal static string EmbedImagesInURIs(string fileName)
		{
			string html = null;
			// Loads the html file content from a byte[]
			html = File.ReadAllText(fileName);
			string targetFilenameNoExtention = Path.GetFileName(fileName).Replace(Path.GetExtension(fileName), "");
			// Iterates looking for images associated with the html file requested.
			foreach (string img in Directory.EnumerateFiles(Path.GetDirectoryName(fileName)))
			{
				string ext = Path.GetExtension(img).ToLower().Remove(0, 1);
				// Encode only the images that are related to the html file
				if (Path.GetFileName(img).StartsWith(targetFilenameNoExtention) && ((ext == "jpg") || (ext == "jpeg") || (ext == "gif") || (ext == "png") || (ext == "bmp")))
				{
					// Load the content of the image file
					byte[] data = File.ReadAllBytes(img);
					// Replace the html src attribute that points to the image file to its base64 content
					html = html.Replace(@"src=""" + Path.GetFileName(img), @"src=""data:" + HotDocs.Sdk.Util.GetMimeType(img) + ";base64," + Convert.ToBase64String(data));
					//					html = html.Replace(@"src=""file://" + img, @"src=""data:" + getImageMimeType(ext) + ";base64," + Convert.ToBase64String(data));
				}
			}
			return html;
		}

		internal static string CreateMimePart(string contentBoundary, string contentType, string contentID, string content, string transferEncoding)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(contentBoundary);
			sb.AppendLine("Content-Type: " + contentType);
			if (!String.IsNullOrEmpty(transferEncoding))
				sb.AppendLine("Content-Transfer-Encoding: " + transferEncoding);
			if (!String.IsNullOrEmpty(contentID))
				sb.AppendLine("Content-ID: " + contentID);
			sb.AppendLine();
			sb.AppendLine(content);
			sb.AppendLine();
			return sb.ToString();
		}
		/// <summary>
		/// Converts an HTML file into a multi-part MIME string.
		/// </summary>
		/// <param name="htmlFileName">The name of the html file that will be converted to a multi-part MIME string.</param>
		/// <returns>A multi-part MIME string.</returns>
		internal static string HtmlToMultiPartMime(string htmlFileName)
		{
			/*
			 * MIME-Version: 1.0
			 * Content-Type: multipart/mixed; boundary="frontier"
			 * 
			 * This is a message with multiple parts in MIME format.
			 * --frontier
			 * Content-Type: text/plain
			 * 
			 * This is the body of the message.
			 * --frontier
			 * Content-Type: application/octet-stream
			 * Content-Transfer-Encoding: base64
			 *
			 * PGh0bWw+CiAgPGhlYWQ+CiAgPC9oZWFkPgogIDxib2R5PgogICAgPHA+VGhpcyBpcyB0aGUg
			 * Ym9keSBvZiB0aGUgbWVzc2FnZS48L3A+CiAgPC9ib2R5Pgo8L2h0bWw+Cg==
			 * --frontier--
			 */
			StringBuilder result = new StringBuilder();
			string html = File.ReadAllText(htmlFileName);
			// Extract all images from the html document.
			string imageFilesExpression = @"src=""(.+[.](png|jpg|jpeg|jpe|jfif|gif|bmp|tif|tiff|wmf|ico))[""]";

			// Extracts and maps image files to mime multipart sections
			Dictionary<string, string> imageMapping = (from Match m in Regex.Matches(html, imageFilesExpression, RegexOptions.IgnoreCase)
													   select m.Groups[1].Value).Distinct().ToDictionary(v => v, v => Regex.Replace(Guid.NewGuid().ToString(), "{|}|-", "") + Path.GetExtension(v).ToLower());
			// Updates the html image references to use the multipart mapping
			foreach (KeyValuePair<string, string> pair in imageMapping)
				html = html.Replace(pair.Key, "cid:" + pair.Value);

			// Writes the header
			string boundary = "------=_NextPart_" + Regex.Replace(Guid.NewGuid().ToString(), "{|}", "");
			result.AppendLine("MIME-Version: 1.0");
			result.AppendLine("Content-Type: multipart/related;    boundary=\"" + boundary + "\";");
			result.AppendLine();
			result.AppendLine("This is a multi-part message in MIME format.");
			result.AppendLine();
			// Write the content parts
			boundary = "--" + boundary;
			// Appends the HTML
			result.Append(CreateMimePart(boundary, "text/html; charset=utf-8", null, html, null));
			// Appends all the image files
			foreach (KeyValuePair<string, string> pair in imageMapping)
			{
				result.Append(CreateMimePart(boundary,
												HotDocs.Sdk.Util.GetMimeType(Path.GetExtension(pair.Key)),
												"<" + pair.Value + ">",
												Convert.ToBase64String(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(htmlFileName), pair.Key))),
												"base64"
												));
			}
			// End of the content
			result.Append(boundary + "--");

			return result.ToString();
		}

		private class DataSourceNameEqualityCompararer : IEqualityComparer<string>
		{
			#region IEqualityComparer<string> Members

			public bool Equals(string x, string y)
			{
				return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
			}

			public int GetHashCode(string obj)
			{
				return obj.ToLower().GetHashCode();
			}

			#endregion
		}

		/// <summary>
		/// Reads the bytes from a text reader and returns them in a BinaryObject.
		/// </summary>
		/// <param name="textReader">A text reader.</param>
		/// <returns>A BinaryObject containing the contents of the text reader.</returns>
		internal static BinaryObject GetBinaryObjectFromTextReader(TextReader textReader)
		{
			string allText = textReader.ReadToEnd();
			return new BinaryObject
			{
				Data = Encoding.UTF8.GetBytes(allText),
				DataEncoding = "UTF-8"
			};
		}

		/// <summary>
		/// This method returns the requested runtime file from the ServerFiles cache. If the file can be found in either the cache or the
		/// source URL, it is returned in the response. Otherwise, nothing is done with the response and the method returns false to indicate failure.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="cacheFolder">The folder where the file is cached.</param>
		/// <param name="sourceUrl">The URL where the file can be found if it does not exist in the cache.</param>
		/// <param name="contentType">Output parameter containing the MIME type of requested runtime file.</param>
		/// <returns>A <c>Stream</c> containing the runtime file.</returns>
		public static Stream GetInterviewRuntimeFile(string fileName, string cacheFolder, string sourceUrl, out string contentType)
		{
			// Validate and normalize input parameters.

			if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(cacheFolder) || string.IsNullOrEmpty(sourceUrl))
				throw new ArgumentNullException();

			try
			{
				// Construct the full file path to the cached file, and create its parent folder(s) if necessary.
				string cachedFilePath = Path.Combine(cacheFolder, fileName.Replace('/', '\\'));
				string cachedFileDirectory = Path.GetDirectoryName(cachedFilePath);
				if (!Directory.Exists(cachedFileDirectory))
				{
					Directory.CreateDirectory(cachedFileDirectory);
				}

				// Construct the full source URL of the requested file. 
				// The source of cloud services files (files.hotdocs.ws) is case-sensitive, so always make the folders in the
				// URL lowercase if we are getting it from there! We don't make everything lowercase because some files (e.g., Silverlight.js)
				// contain capital letters.
				string sourceFileUrl = sourceUrl.TrimEnd('/') + "/" + fileName;
				if (sourceFileUrl.IndexOf("files.hotdocs.ws") > 0)
				{
					int lastForwardSlash = sourceFileUrl.LastIndexOf('/');
					sourceFileUrl = sourceFileUrl.Substring(0, lastForwardSlash).ToLower() + sourceFileUrl.Substring(lastForwardSlash);
				}

				System.Net.WebRequest myRequest;

				if (File.Exists(cachedFilePath))
				{
					// There is a matching file in the cache. See if it is up to date or not.
					FileInfo fInfo = new FileInfo(cachedFilePath);
					if (fInfo.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-1))
					{
						// The file is more than 1 day old, so we can re-check now.
						// Check to see if we need to get a new version of the file from the server.
						myRequest = System.Net.WebRequest.Create(sourceFileUrl);
						myRequest.Method = "HEAD";

						using (System.Net.WebResponse myResponse = myRequest.GetResponse())
						{
							string lastMod = myResponse.Headers["Last-Modified"];
							DateTime serverFileDate = DateTime.Parse(lastMod);
							if (fInfo.CreationTimeUtc != serverFileDate.ToUniversalTime())
							{
								// The creation time of our local file is not the same as the last modification date
								// of the server file, so we are out of sync. Delete the local file and get a new one.
								File.SetAttributes(cachedFilePath, FileAttributes.Normal);
								File.Delete(cachedFilePath);
							}
							else
							{
								// Set the modification date to Now so we won't check for at least 24 more hours.
								File.SetLastWriteTimeUtc(cachedFilePath, DateTime.UtcNow);
							}
						}
					}
				}

				if (!File.Exists(cachedFilePath))
				{
					// Either the file did not exist in the cache from the start, or we deleted it because it was out of date.
					// In any case, it does not exist, so we need to request it from the source URL and save it to our cache folder.
					myRequest = System.Net.WebRequest.Create(sourceFileUrl);
					myRequest.Method = "GET";
					using (System.Net.WebResponse myResponse = myRequest.GetResponse())
					{
						using (FileStream writeStream = new FileStream(cachedFilePath, FileMode.Create, FileAccess.Write))
						using (Stream readStream = myResponse.GetResponseStream())
						{
							readStream.CopyTo(writeStream);
						}

						// Set the creation time of the locally cached file to the last modification date from the server file.
						// We use this time to compare with the server's file during future requests to see if it matches or not.
						File.SetCreationTimeUtc(cachedFilePath, DateTime.Parse(myResponse.Headers["Last-Modified"]).ToUniversalTime());
					}
				}

				contentType = HotDocs.Sdk.Util.GetMimeType(cachedFilePath);
				if (File.Exists(cachedFilePath))
				{
					// The locally cached file is either less than 24 hours old or we got the latest one from the source URL.
					// We can return it in the response.
					return new FileStream(cachedFilePath, FileMode.Open, FileAccess.Read);
				}
				else
				{
					// If we could not find the file, give the user a HTTP_STATUS_NOT_FOUND error.
					return null;
				}
			}
			catch
			{
				// Something really bad happened.
				contentType = null;
				return null;
			}

		}

		/// <summary>
		/// This method is used by both WS and Cloud implementations of AssembleDocument to convert an AssemblyResult to an AssembleDocumentResult.
		/// </summary>
		/// <param name="template">The template associated with the <c>AssemblyResult</c>.</param>
		/// <param name="asmResult">The <c>AssemblyResult</c> to convert.</param>
		/// <param name="docType">The type of document contained in the result.</param>
		/// <returns>An <c>AssembleDocumentResult</c>, which contains the same document as the <c>asmResult</c>.</returns>
		internal static OffDiskAssembleDocumentResult ConvertAssemblyResult(ITemplate template, AssemblyResult asmResult, DocumentType docType)
		{
			OffDiskAssembleDocumentResult result = null;
			MemoryStream document = null;
			StreamReader ansRdr = null;
			List<NamedStream> supportingFiles = new List<NamedStream>();

			// Create the list of pending assemblies.
			IEnumerable<ITemplate> pendingAssemblies =
				asmResult.PendingAssemblies == null
				? new List<Template>()
				: from pa in asmResult.PendingAssemblies
				  select new Template(
					  Path.GetFileName(pa.TemplateName), template.Location.Duplicate(), pa.Switches);

			for (int i = 0; i < asmResult.Documents.Length; i++)
			{
				switch (asmResult.Documents[i].Format)
				{
					case OutputFormat.Answers:
						ansRdr = new StreamReader(new MemoryStream(asmResult.Documents[i].Data));
						break;
					case OutputFormat.JPEG:
					case OutputFormat.PNG:
						// If the output document is plain HTML, we might also get additional image files in the 
						// AssemblyResult that we need to pass on to the caller.
						supportingFiles.Add(new NamedStream(asmResult.Documents[i].FileName, new MemoryStream(asmResult.Documents[i].Data)));
						break;
					default:
						document = new MemoryStream(asmResult.Documents[i].Data);
						if (docType == DocumentType.Native)
						{
							docType = Document.GetDocumentType(asmResult.Documents[i].FileName);
						}
						break;
				}
			}

			result = new OffDiskAssembleDocumentResult( document == null ? null :
				new Document(template, document, docType, supportingFiles.ToArray(), asmResult.UnansweredVariables),
				ansRdr == null ? null : ansRdr.ReadToEnd(),
				pendingAssemblies.ToArray(),
				asmResult.UnansweredVariables
			);

			return result;
		}
        /// <summary>
        /// This method is used by both WS and Cloud implementations of AssembleDocument to convert an AssemblyResult to an AssembleDocumentResult.
        /// </summary>
        /// <param name="template">The template associated with the <c>AssemblyResult</c>.</param>
        /// <param name="asmResult">The <c>AssemblyResult</c> to convert.</param>
        /// <param name="docType">The type of document contained in the result.</param>
        /// <returns>An <c>AssembleDocumentResult</c>, which contains the same document as the <c>asmResult</c>.</returns>
        internal static OnDiskAssembleDocumentResult ConvertAssemblyResult(IOnDiskTemplate template, AssemblyResult asmResult, DocumentType docType)
        {
            OnDiskAssembleDocumentResult result = null;
            MemoryStream document = null;
            StreamReader ansRdr = null;
            List<NamedStream> supportingFiles = new List<NamedStream>();

            // Create the list of pending assemblies.
            IEnumerable<IOnDiskTemplate> pendingAssemblies =
                asmResult.PendingAssemblies == null
                ? new List<DiskAccessibleTemplate>()
                : from pa in asmResult.PendingAssemblies
                  select new DiskAccessibleTemplate(
                      Path.GetFileName(pa.TemplateName), template.Location.Duplicate(), pa.Switches);

            for (int i = 0; i < asmResult.Documents.Length; i++)
            {
                switch (asmResult.Documents[i].Format)
                {
                    case OutputFormat.Answers:
                        ansRdr = new StreamReader(new MemoryStream(asmResult.Documents[i].Data));
                        break;
                    case OutputFormat.JPEG:
                    case OutputFormat.PNG:
                        // If the output document is plain HTML, we might also get additional image files in the 
                        // AssemblyResult that we need to pass on to the caller.
                        supportingFiles.Add(new NamedStream(asmResult.Documents[i].FileName, new MemoryStream(asmResult.Documents[i].Data)));
                        break;
                    default:
                        document = new MemoryStream(asmResult.Documents[i].Data);
                        if (docType == DocumentType.Native)
                        {
                            docType = Document.GetDocumentType(asmResult.Documents[i].FileName);
                        }
                        break;
                }
            }

            result = new OnDiskAssembleDocumentResult(document == null ? null :
                new Document(template, document, docType, supportingFiles.ToArray(), asmResult.UnansweredVariables),
                ansRdr == null ? null : ansRdr.ReadToEnd(),
                pendingAssemblies.ToArray(),
                asmResult.UnansweredVariables
            );

            return result;
        }
	}


}
