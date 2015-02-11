/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

<%@ WebHandler Language="C#" Class="GetDocPreview" %>

using System;
using System.Web;
using System.Text;
using System.IO;
using System.Web.SessionState;
using SamplePortal;

public class GetDocPreview : IHttpHandler, IRequiresSessionState
{
	public void ProcessRequest(HttpContext context)
	{
		// Get the user's session
		HotDocs.Sdk.Server.WorkSession session = SamplePortal.Factory.GetWorkSession(context.Session);
		if (session == null)
			return;

		// Get the current work item from the session.
		HotDocs.Sdk.Server.DiskAccessibleWorkItem workItem = session.CurrentWorkItem;
		if (workItem == null)
			return;

		HttpRequest req = context.Request;
		HttpResponse resp = context.Response;

		switch (req.HttpMethod)
		{
			case "POST":

				// This is the initial POSTing of answers from the interview. We will take the posted answers and overlay them on
				// the session's answer file to bring it up to date. Then we will redirect to the same file handler to GET the
				// assembled document preview.

				// Merge the posted answers with those stored in the session.
				System.IO.StringReader sr = new StringReader(HotDocs.Sdk.Server.InterviewResponse.GetAnswers(req.Form));
				session.AnswerCollection.OverlayXml(HotDocs.Sdk.Server.InterviewAnswerSet.GetDecodedInterviewAnswers(sr));

				// Determine what kind of file to assemble.
				string previewType;
				if (workItem.Template.FileName.EndsWith(".hpt", StringComparison.OrdinalIgnoreCase))
				{
					previewType = "pdf/" + System.IO.Path.GetFileNameWithoutExtension(workItem.Template.FileName);
				}
				else if (SamplePortal.Util.BrowserSupportsInlineImages(req))
				{
					previewType = "html";
				}
				else
				{
					previewType = "mhtml";
				}

				// Redirect to the GetDocPreview page to GET the assembled document preview. We used to just return it in the response,
				// but some browsers did bad things if we were returning a PDF from a POST when the user went to save the file.
				//resp.Redirect(req.Url.AbsolutePath + "?type=" + previewType);
				resp.Redirect(req.Url.AbsolutePath + "/" + previewType); // + "/HelloWorld");
				resp.End();
				break;

			case "GET":

				// The answers were previously POSTed, and the preview was created in the temporary folder. This GET request
				// will return the actual file. We do not return it as a response to the POST because that causes problems in
				// certain browsers, especially when returning PDF files. See TFS #5526.

				// Get the document preview type and title from the URL. The URL should look like this: 
				// http://...GetDocPreview.ashx/type/title
				// We include the parameters (type and title) in the url rather than in the query string to make IE happy so that it
				// will actually use the title when the user goes to save a PDF from the document preview page. (It uses whatever is
				// after the last forward slash.) Otherwise it uses GetDocPreview as the filename.
				string tmpFileExt = null;
				string title = "DocumentPreview"; // Default title in case we do not get one from the URL for some reason.
				string[] requestUrlArray = req.Url.AbsolutePath.Split('/');
				for (int i = 0; i < requestUrlArray.Length; i++)
				{
					if (requestUrlArray[i].IndexOf("GetDocPreview.ashx") == 0)
					{
						if (requestUrlArray.Length > i + 1)
							tmpFileExt = requestUrlArray[i + 1];
						if (requestUrlArray.Length > i + 2)
							title = requestUrlArray[i + 2];
						break;
					}
				}

				// Ensure that we were able to determine the type of preview being requested.
				if (string.IsNullOrEmpty(tmpFileExt))
				{
					resp.Write("Error: The URL did not specify the type of document preview to return.");
				}
				else
				{
					StringReader mergedAnswersReader = new StringReader(session.AnswerCollection.XmlAnswers);
					HotDocs.Sdk.AssembleDocumentSettings settings = session.DefaultAssemblySettings;

					// Set the appropriate mime types and headers for the requested file.
					switch (tmpFileExt)
					{
						case "pdf":
							resp.AddHeader("Content-Disposition", "inline; filename=\"" + title + ".pdf\"");
							resp.ContentType = "application/pdf";
							settings.Format = HotDocs.Sdk.DocumentType.PDF;
							break;
						case "html":
							resp.ContentType = "text/html; charset=utf-8";
							settings.Format = HotDocs.Sdk.DocumentType.HTMLwDataURIs;
							break;
						case "mhtml":
							resp.ContentType = "message/rfc822"; // rfc822 required for IE to display the MHTML; "multipart/related" doesn't work
							settings.Format = HotDocs.Sdk.DocumentType.MHTML;
							break;
					}

					// Assemble the document and write it to the temp folder.
					using (HotDocs.Sdk.Server.AssembleDocumentResult asmResult = session.Service.AssembleDocument(
						workItem.Template, mergedAnswersReader, settings, "Assembly for document preview."))
					{
						asmResult.Document.Content.CopyTo(resp.OutputStream);
						resp.Flush();
					}
				}
				resp.End();
				break;
		}
	}

	/// <summary>
	/// Indicates whether another request can reuse this IHttpHandler instance.
	/// </summary>
	public bool IsReusable
	{
		// This file handler has no state, so the handler can be reused as needed.
		get { return true; }
	}

}