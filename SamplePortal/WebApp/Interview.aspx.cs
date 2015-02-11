/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using SamplePortal;
using System;
using System.Configuration;
using System.Web;

public partial class Interview : System.Web.UI.Page
{
	private String _interviewContent;

	protected HotDocs.Sdk.Server.WorkSession _session;

	protected void Page_Load(object sender, EventArgs e)
	{
		//don't cache interviews
		Response.Cache.SetCacheability(HttpCacheability.NoCache);

		

		_session = SamplePortal.Factory.GetWorkSession(this.Session);
		if (_session == null)
			Response.Redirect("Default.aspx");

		HotDocs.Sdk.Server.DiskAccessibleWorkItem workItem = _session.CurrentWorkItem;
		if (!(workItem is HotDocs.Sdk.Server.DiskAccessibleInterviewWorkItem))
		{
			MessageBox.Show("Only interview work items are allowed at the interview page.");
			return;
		}

		_interviewContent = "";
		// Call GetInterview and return the result
		try
		{
			//TODO: Consider skipping this step given that the SelAns page already skips forward to the disposition page if needed.
			// Check the switches in case this assembly resulted from an ASSEMBLE instruction in another template.
			// In this case, it is possible that the template author indicated that no interview should be asked using a
			// "/nw" or "/naw" or "/ni" switch. If this is the case, we skip this page and redirect to the disposition page
			// [LRS:] Also, the WorkSession should have already checked the switches. If one of "/ni", etc. is found, the
			// session should not even be creating an interview work item at all. So checking them here is redundant.
			if (ShouldSkipInterviewUI(workItem.Template.Switches))
				Response.Redirect("Disposition.aspx");

			// Determine which interview format to use, depending on the values for InterviewFormat and InterviewFallback in web.config.
			HotDocs.Sdk.Server.Contracts.InterviewFormat format = HotDocs.Sdk.Server.Contracts.InterviewFormat.Unspecified;
			string formatString = ConfigurationManager.AppSettings["InterviewFormat"];
			if (!String.IsNullOrWhiteSpace(formatString)) {
				if (formatString.ToUpper() == "SILVERLIGHT")
				{
					// The interview fallback option only comes into play if the specified format in web.config is Silverlight.
					// (We never "fallback" to Silverlight since it is the more restrictive interview format; JavaScript will work in any browser.)

					// See if fallback is allowed and then fallback to JavaScript if Silverlight is unavailable.
					if (Settings.AllowInterviewFallback)
					{
						// Get the cookie we set that indicates whether or not Silverlight 5.0 is installed.
						HttpCookie slCookie = Request.Cookies["SilverlightAvailable"];
						if (slCookie != null ? !bool.Parse(Request.Cookies["SilverlightAvailable"].Value) : true)
							format = HotDocs.Sdk.Server.Contracts.InterviewFormat.JavaScript;
					}
				}
				else if (formatString.ToUpper() == "JAVASCRIPT")
				{
					format = HotDocs.Sdk.Server.Contracts.InterviewFormat.JavaScript;
				}
			}

			// Note for HotDocs host application developers: an informational log entry could be added to a 
			// log file before (here) and after the call to _session.GetCurrentInterview.

			//Get an interview using the default interview options from the web.config file.
			HotDocs.Sdk.Server.InterviewResult interviewResult = _session.GetCurrentInterview(format);
			_interviewContent = interviewResult.HtmlFragment;
		}
		catch (Exception ex)
		{
			// Note for HotDocs host application developers: An error entry should be added to a log file here
			// with information about the current exception, including a stack trace:
			_interviewContent = "Unable to return interview:<br><br>" + ex.Message; //interview error message
		}
	}

	protected string GetInterview()
	{
		return _interviewContent;
	}

	private bool ShouldSkipInterviewUI(string switches)
	{
		//check the switches for the assembly and check for interview-skipping switches
		switches = String.IsNullOrEmpty(switches) ? String.Empty : switches.ToLower();
		return (-1 != switches.IndexOf("/nw") || -1 != switches.IndexOf("/naw") || -1 != switches.IndexOf("/ni") ||
			-1 != switches.IndexOf("-nw") || -1 != switches.IndexOf("-naw") || -1 != switches.IndexOf("-ni"));
	}
}
