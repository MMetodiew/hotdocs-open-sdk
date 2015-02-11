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
	/// The results of assembling a document. An object of this type is returned by AssembleDocument().
	/// </summary>
	public abstract class AssembleDocumentResult : IDisposable
	{


		/// <summary>
		/// Returns the document that is the result of a document assembly operation.
		/// </summary>
		public Document Document { get; protected set; }

		/// <summary>
		/// The post-assembly answer file (potentially modified from before the assembly, since assembly can have side effects)
		/// </summary>
        public string Answers { get; protected set; }

	    /// <summary>
		/// An collection of variable names for which answers were called for during assembly,
		/// but for which no answer was included in the answer collection.
		/// </summary>
        public IEnumerable<string> UnansweredVariables { get; protected set; }

	    /// <summary>
		/// "Extracts" a Document object from this AssemblyResult instance.  This essentially
		/// shifts responsibility for disposing of the Content and SupportingFiles members from
		/// this AssemblyResult to the returned Document.  This is used when a WorkSession
		/// aggregates the AssemblyResults for one or more documents into an array of DocumentResults
		/// and persists the rest of the AssemblyResult members elsewhere.
		/// </summary>
		/// <returns>The "extracted" Document, now the caller's responsibility to Dispose().</returns>
		internal Document ExtractDocument()
		{
			var result = Document;
			// pass ownership of the document & supporting files over to the returned Document
			// (so they are disposed when the new Document is disposed, not this object)
			Document = null;
			return result;
		}

        protected bool disposed = false; // to detect redundant calls

	    /// <summary>
		/// <c>Dispose</c> Frees up and deallocates everything associated with the current object. 
		/// This is called by Dispose (from IDisposable) of this class.
		/// </summary>
		/// <param name="disposing">Indicates whether or not managed resources should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
					if (Document != null)
					{
						Document.Dispose();
						Document = null;
					}
				}
				disposed = true;
			}
		}

		/// <summary>
		/// <c>Dispose</c> implements the IDisposable interface
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}
	}

}
