/* Copyright (c) 2013, HotDocs Limited
   Use, modification and redistribution of this source is subject
   to the New BSD License as set out in LICENSE.TXT. */

using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using SamplePortal;
using SamplePortal.Data;

public partial class answers : System.Web.UI.Page
{
    protected DataView _ansData;
    public string _siteName = Settings.SiteName;
    protected void Page_Load(object sender, EventArgs e)
    {
        if (this.IsPostBack == false)
        {
            ViewState["sortExpression"] = Settings.DefaultAnswerTableSortExpression;
            BindData(null);


        }
    }

    public void BindData(string newSortExpression)
    {
        string sortExpression = Util.ToggleSortOrder(ansGrid, newSortExpression, ViewState["sortExpression"].ToString());

        _ansData = (new Answers()).SelectAll(sortExpression, txtSearch.Text);

        ansGrid.DataSource = _ansData;
        Util.SetNewPageSize(ansGrid, Settings.AnswerGridPageSize, _ansData.Count);
        ansGrid.DataBind();
        ViewState["sortExpression"] = sortExpression;
    }

    protected void btnHome_Click(object sender, EventArgs e)
    {
        Response.Redirect("Default.aspx");
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
    protected void ansGrid_ItemCreated(object sender, DataGridItemEventArgs e)
    {
        if ((e.Item.ItemType == ListItemType.Item) ||
                (e.Item.ItemType == ListItemType.AlternatingItem))
        {
            LinkButton lnkDelete = (LinkButton)e.Item.Cells[1].Controls[0];
            lnkDelete.Attributes.Add("onclick", "return confirm('Are you sure you want to delete this record?');");
        }

		if (e.Item.ItemType == ListItemType.EditItem)
		{
			// TFS #5532: Enforce limits on the length of text in the title and description fields.
			((TextBox)e.Item.Cells[3].Controls[0]).Attributes.Add("maxlength", Settings.MaxTitleLength.ToString());
			((TextBox)e.Item.Cells[4].Controls[0]).Attributes.Add("maxlength", Settings.MaxDescriptionLength.ToString());
		}

        if (e.Item.ItemType == ListItemType.Pager)
            Util.CustomizePager(e);
    }
    protected void ansGrid_SortCommand(object source, DataGridSortCommandEventArgs e)
    {
        BindData(e.SortExpression);
    }
    protected void ansGrid_PageIndexChanged(object source, DataGridPageChangedEventArgs e)
    {
        ansGrid.CurrentPageIndex = e.NewPageIndex;
        BindData(null);
    }
    protected void ansGrid_DeleteCommand(object source, DataGridCommandEventArgs e)
    {
        string ansfname = e.Item.Cells[2].Text;
        using (Answers answers = new Answers())
        {
            answers.DeleteAnswerFile(ansfname);
        }
        System.IO.File.Delete(System.IO.Path.Combine(Settings.AnswerPath, ansfname));
        BindData(null);
    }
    protected void ansGrid_EditCommand(object source, DataGridCommandEventArgs e)
    {
        ViewState["editf"] = ansGrid.Items[e.Item.ItemIndex].Cells[2].Text;
        ansGrid.EditItemIndex = e.Item.ItemIndex;
        BindData(null);
    }
    protected void ansGrid_UpdateCommand(object source, DataGridCommandEventArgs e)
    {
        using (Answers answers = new Answers())
        {
            answers.UpdateAnswerFile(ViewState["editf"].ToString(),
                ((TextBox)e.Item.Cells[3].Controls[0]).Text,
                ((TextBox)e.Item.Cells[4].Controls[0]).Text);
        }
        ansGrid.EditItemIndex = -1;
        BindData(null);
    }
    protected void ansGrid_CancelCommand(object source, DataGridCommandEventArgs e)
    {
        ansGrid.EditItemIndex = -1;
        BindData(null);
    }
}