/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HotDocs.Sdk;

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
	/// 
	public interface IServices
	{
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
	}
}
