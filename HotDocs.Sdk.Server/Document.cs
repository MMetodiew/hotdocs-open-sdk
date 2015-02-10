/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HotDocs.Sdk.Server
{
	/// <summary>
	/// Return result of WorkSession.AssembleDocuments and IServices.ConvertDocument.  Encapsulates an assembled
	/// or converted document, together with any supporting files that document may require.
	/// </summary>
	public class Document : IDisposable
	{
		// internal constructor
		internal Document(ITemplate source, Stream document, DocumentType docType, NamedStream[] supportingFiles, string[] unansweredVariables)
		{
			if (docType == DocumentType.Native)
				throw new InvalidOperationException("DocumentResults and AssemblyResults must specify a DocumentType (if known).");

			_source = source;
			Content = document;
			Type = docType;
			SupportingFiles = supportingFiles;
			UnansweredVariables = unansweredVariables;
		}

		/// <summary>
		/// Retrieve the converted document as a stream. If the requested format consists only of a list of like entities,
		/// they will be in SupportingFiles and this will be null.
		/// </summary>
		public Stream Content { get; protected set; }

		/// <summary>
		/// The file type of the converted document
		/// </summary>
		public DocumentType Type { get; private set; }

		/// <summary>
		/// If Content is in a format that has external dependencies, this contains references to those supporting files.
		/// For example, the images or external style sheets referred to by an HTML document.
		/// </summary>
		public IEnumerable<NamedStream> SupportingFiles { get; protected set; }

		/// <summary>
		/// <c>FileExtension</c> returns the file extension of the current document, given its <c>Type</c>
		/// </summary>
		public string FileExtension
		{
			get
			{
				switch (Type)
				{
					case DocumentType.WordDOCX : return ".docx";
					case DocumentType.WordRTF : return ".rtf";
					case DocumentType.WordDOC : return ".doc";
					case DocumentType.WordPerfect : return ".wpd";
					case DocumentType.PDF : return ".pdf";
					case DocumentType.HPD : return ".hpd";
					case DocumentType.HFD : return ".hfd";
					case DocumentType.PlainText : return ".txt";
					case DocumentType.HTML : return ".htm";
					case DocumentType.HTMLwDataURIs : return ".htm";
					case DocumentType.MHTML : return ".mht";
					case DocumentType.XML : return ".xml";
					default: return String.Empty;
				}
			}
		}

		/// <summary>
		/// <c>ContentType</c> returns the MIME type according to the <c>Type</c> property
		/// </summary>
		public string ContentType 
		{
			get
			{
				switch (Type)
				{
					case DocumentType.WordDOCX: return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
					case DocumentType.WordRTF: return "application/msword";
					case DocumentType.WordDOC: return "application/msword";
					case DocumentType.WordPerfect: return "application/wordperfect";
					case DocumentType.PDF: return "application/pdf";
					case DocumentType.HPD: return "application/x-hotdocs-hpd";
					case DocumentType.HFD: return "application/x-hotdocs-hfd";
					case DocumentType.PlainText: return "text/plain";
					case DocumentType.HTML: return "text/html";
					case DocumentType.HTMLwDataURIs: return "text/html";
					case DocumentType.MHTML: return "message/rfc822"; // rfc822 required for IE to display the MHTML; "multipart/related" doesn't work
					case DocumentType.XML: return "text/xml";
					default: return "application/octet-stream";
				}
			}
		}

		/// <summary>
		/// Retrieve the Template object representing the template the document was assembled from.
		/// </summary>
		public ITemplate Source
		{
			get
			{
				return _source;
			}
		}

		/// <summary>
		/// Returns unanswered variables in the current document.
		/// </summary>
		public string[] UnansweredVariables { get; protected set; }

		/// <summary>
		/// Returns the DocumentType for the given file path.
		/// </summary>
		/// <param name="docPath">The file path of the document.</param>
		/// <returns>The type of document referred to in the file path.</returns>
		internal static DocumentType GetDocumentType(string docPath)
		{
			string docExtension = Path.GetExtension(docPath).Trim('.').ToLower();

			switch (docExtension)
			{
				case "hfd":
					return DocumentType.HFD;
				case "hpd":
					return DocumentType.HPD;
				case "htm":
				case "html":
					// We have no way of distinguishing between regular HTML and HTML "with data URIs" based
					// on the file name extension. So we just say the document type is regular HTML.
					// This is OK since this method is only ever called when the document type is native and
					// we want to find out what native resolved to based on the output file; HTML is never a native
					// output format.
					return DocumentType.HTML;
				case "mhtml":
					return DocumentType.MHTML;
				case "pdf":
					return DocumentType.PDF;
				case "txt":
					return DocumentType.PlainText;
				case "doc":
					return DocumentType.WordDOC;
				case "docx":
					return DocumentType.WordDOCX;
				case "wpt":
					return DocumentType.WordPerfect;
				case "rtf":
					return DocumentType.WordRTF;
				case "xml":
					return DocumentType.XML;
			}

			return DocumentType.Unknown;
		}

		private bool disposed = false; // to detect redundant calls
		private ITemplate _source = null;

		/// <summary>
		/// Calls IDisposable.Dispose
		/// </summary>
		/// <param name="disposing">Indicates whether or not managed resources should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					if (Content != null)
					{
						Content.Dispose();
						Content = null;
					}
					if (SupportingFiles != null)
					{
						foreach (NamedStream ns in SupportingFiles)
							ns.Dispose();
						SupportingFiles = null;
					}
				}
				disposed = true;
			}
		}

		/// <summary>
		/// Implements IDisposable.Dispose
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}
	}
}
