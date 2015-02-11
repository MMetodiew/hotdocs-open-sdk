/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using SamplePortal;
using System;
using System.Data;
using System.IO;
using System.Web;
using System.Web.UI.WebControls;

public partial class SelAns : System.Web.UI.Page
{
	protected DataView ansData;
	protected HotDocs.Sdk.Server.WorkSession _session;
	protected static string _javascriptUrl = Settings.JavaScriptUrl;

	protected void Page_Load(object sender, EventArgs e)
	{
		_session = SamplePortal.Factory.GetWorkSession(this.Session);

		if (_session == null)
			Response.Redirect("Default.aspx");

		if (!IsPostBack)
		{
			ViewState["sortExpression"] = Settings.DefaultAnswerTableSortExpression;
			BindData(null);		   
		}
	}

	#region Search buttons
	protected void btnSearch_Click(object sender, EventArgs e)
	{
		ansGrid.CurrentPageIndex = 0;
		BindData(null);
	}
	protected void btnSearchClear_Click(object sender, EventArgs e)
	{
		txtSearch.Text = "";
		BindData(null);
	}
	#endregion

	public void BindData(string newSortExpression)
	{
		string sortExpression = Util.ToggleSortOrder(ansGrid, newSortExpression, ViewState["sortExpression"].ToString());

		ansData = (new SamplePortal.Data.Answers()).SelectAll(sortExpression,txtSearch.Text);
		ansGrid.DataSource = ansData;

		Util.SetNewPageSize(ansGrid, Settings.AnswerGridPageSize, ansData.Count);
		ansGrid.DataBind();
		ViewState["sortExpression"] = sortExpression;
	}
	protected void btnContinue_Click(object sender, EventArgs e)
	{
		if (Request.Form["AnsFileType"] == "upload")
		{
			// upload existing answer file:
			HttpPostedFile file = fileUpload.PostedFile;
			if (file != null)
			{
				string ext = Path.GetExtension(file.FileName).ToLower();
				if (ext == ".anx")
				{
					_session.AnswerCollection.ReadFile(file.InputStream);
					Advance();
					return;
				}
			}
		}
		//else use new answer file:
		Advance();
	}

	protected void ansGrid_PageIndexChanged(object source, DataGridPageChangedEventArgs e)
	{
		ansGrid.CurrentPageIndex = e.NewPageIndex;
		BindData(null);
	}
	protected void ansGrid_SortCommand(object source, DataGridSortCommandEventArgs e)
	{
		BindData(e.SortExpression);
	}
	protected void ansGrid_ItemCreated(object sender, DataGridItemEventArgs e)
	{
		if (e.Item.ItemType == ListItemType.Pager)
			Util.CustomizePager(e);
	}
	protected void ansGrid_ItemDataBound(object sender, DataGridItemEventArgs e)
	{
		if ((e.Item.ItemType == ListItemType.Item) ||
				(e.Item.ItemType == ListItemType.AlternatingItem))
		{
			LinkButton lnkSelect = (LinkButton)e.Item.Cells[0].Controls[0];
			lnkSelect.Text = e.Item.Cells[2].Text;
		}
	}
	protected void ansGrid_SelectedIndexChanged(object sender, EventArgs e)
	{
		string answerfile = ansGrid.SelectedItem.Cells[1].Text;

		if (answerfile == "")
			answerfile = null;
		if (answerfile != null) // check for invalid answer file name
		{
			int delimpos = answerfile.LastIndexOfAny(new char[] { '/', '\\' });
			// answer filename must either contain no slashes, or else only slash must be "tmp\"
			if (delimpos >= 0)
				answerfile = null; // invalid answer file -- ignore it
		}

		if (answerfile != null)
		{
			string filePath = Path.Combine(Settings.AnswerPath, answerfile);
			_session.AnswerCollection.ReadFile(filePath);
		}

		Advance();
	}
	private void Advance()
	{
		HotDocs.Sdk.Server.DiskAccessibleWorkItem wi = _session.CurrentWorkItem;
		if (wi is HotDocs.Sdk.Server.DiskAccessibleInterviewWorkItem)
			Response.Redirect("Interview.aspx");
		else
			Response.Redirect("Disposition.aspx");
	}
}
