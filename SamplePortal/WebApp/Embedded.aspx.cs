/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using HotDocs.Sdk.Cloud;
using HotDocs.Sdk.Server.Contracts;
using SamplePortal;
using System;
using System.Configuration;
using System.IO;
using System.Data;
using System.Web.UI.WebControls;

/// <summary>
/// This page is an example of using an "embedded" interview from HotDocs Cloud Services. 
/// </summary>
public partial class Embedded : System.Web.UI.Page
{
	public enum EmbeddedPageMode { SelectTemplate, SelectAnswers, ShowInterview }
	protected bool _resume = false;
	protected string _packageID;
	protected EmbeddedPageMode _pageMode;
	protected static string _javascriptUrl = Settings.JavaScriptUrl;
	protected DataView _tplData;
	protected DataView ansData;

	protected void Page_Load(object sender, EventArgs e)
	{
		Header1.SiteName = "HotDocs Embedded Example";

		_packageID = (string)Session["PackageID"];

		if (!IsPostBack)
			SwitchMode(EmbeddedPageMode.SelectTemplate);

		System.Net.ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) =>
		{
			return true;
		};
	}

	protected void SwitchMode(EmbeddedPageMode m)
	{
		switch (m)
		{
			case EmbeddedPageMode.SelectAnswers:
				ansData = (new SamplePortal.Data.Answers()).SelectAll(null, null);
				ansGrid.DataSource = ansData;
				ansGrid.DataBind();

				tplGrid.Visible = false;
				ansGrid.Visible = true;
				break;
			case EmbeddedPageMode.SelectTemplate:
				_tplData = (new SamplePortal.Data.Templates()).SelectAll(null);
				tplGrid.DataSource = _tplData;
				tplGrid.DataBind();

				tplGrid.Visible = true;
				ansGrid.Visible = false;
				break;
			case EmbeddedPageMode.ShowInterview:
				tplGrid.Visible = false;
				ansGrid.Visible = false;
				break;
		}
	}

	protected string GetSessionID()
	{
	

		var client = new RestClient(Settings.SubscriberID, Settings.SigningKey, null, SamplePortal.Settings.CloudServicesAddress);

		if (_resume)
		{
			// Resume a previously-saved session.
			_resume = false;
			return client.ResumeSession(SnapshotField.Value);
		}
		else
		{
			// Make sure we have a packageID to use with the new session.
			if (string.IsNullOrEmpty(_packageID))
				return null;

			// Create the new session.
			InterviewFormat format = HotDocs.Sdk.Util.ReadConfigurationEnum<InterviewFormat>("InterviewFormat", InterviewFormat.Unspecified);
			HotDocs.Sdk.ITemplate template = new HotDocs.Sdk.Template(new HotDocs.Sdk.PackagePathTemplateLocation(_packageID, Path.Combine(Settings.TemplatePath, _packageID + ".pkg")));
			return client.CreateSession(template, null, null, null, format);
		}
	}

	#region DataGrid

	protected void tplGrid_ItemDataBound(object sender, DataGridItemEventArgs e)
	{
		if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
		{
			LinkButton lnkSelect = (LinkButton)e.Item.Cells[0].Controls[0];
			lnkSelect.Text = e.Item.Cells[2].Text;
		}
	}

	protected void tplGrid_SelectedIndexChanged(object sender, EventArgs e)
	{
		_packageID = tplGrid.SelectedItem.Cells[5].Text;
		Session["PackageID"] = _packageID;

		if (string.IsNullOrEmpty(_packageID) || _packageID.IndexOfAny(new char[] { '/', '\\', ':' }) >= 0) //Don't allow path control elements in a package ID.
		{
			MessageBox.Show("Invalid template path");
			return;
		}

		if (!PackageCache.PackageExists(_packageID))
		{
			MessageBox.Show("The selected template could not be found. Please contact your Sample Portal site administrator for assistance.");
			return;
		}
		SwitchMode(EmbeddedPageMode.SelectAnswers);
	}

	#endregion

	#region Answer Grid

	protected void ansGrid_ItemDataBound(object sender, DataGridItemEventArgs e)
	{
		if ((e.Item.ItemType == ListItemType.Item) || (e.Item.ItemType == ListItemType.AlternatingItem))
		{
			LinkButton lnkSelect = (LinkButton)e.Item.Cells[0].Controls[0];
			lnkSelect.Text = e.Item.Cells[2].Text;
		}
	}

	protected void ansGrid_SelectedIndexChanged(object sender, EventArgs e)
	{
		SwitchMode(EmbeddedPageMode.ShowInterview);
	}

	#endregion

	protected void ResumeSessionButton_Click(object sender, EventArgs e)
	{
		_resume = true;
		SwitchMode(EmbeddedPageMode.ShowInterview);
	}
}
