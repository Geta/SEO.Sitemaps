using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EPiServer;
using EPiServer.Data;
using EPiServer.PlugIn;
using EPiServer.Security;
using EPiServer.UI;
using EPiServer.UI.Admin.SiteMgmt;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Services;

namespace Geta.SEO.Sitemaps.Modules.Geta.SEO.Sitemaps
{
    [GuiPlugIn(Area = PlugInArea.AdminMenu, 
        DisplayName = "Search engine sitemap settings",
        Description = "Manage the sitemap module settings and content",
        Url = "~/Modules/Geta.SEO.Sitemaps/AdminManageSitemap.aspx", 
        RequiredAccess = AccessLevel.Administer)]
    public partial class AdminManageSitemap : SystemPageBase
    {
        private readonly ISitemapRepository sitemapRepository;

        protected const string SitemapHostPostfix = "Sitemap.xml";

        public AdminManageSitemap()
        {
            sitemapRepository = new SitemapRepository();
        }

        protected override void OnPreInit(EventArgs e)
        {
            base.OnPreInit(e);

            MasterPageFile = ResolveUrlFromUI("MasterPages/EPiServerUI.master");
            SystemMessageContainer.Heading = "Search engine sitemap settings";
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (!IsPostBack)
            {
                BindList();
            }
        }

        private void BindList()
        {
            lvwSitemapData.DataSource = sitemapRepository.GetAllSitemapData();
            lvwSitemapData.DataBind();   
        }

        protected void btnNew_Click(object sender, EventArgs e)
        {
            lvwSitemapData.EditIndex = -1;
            lvwSitemapData.InsertItemPosition = InsertItemPosition.LastItem;

            BindList();

            PopulateHostListControl(lvwSitemapData.InsertItem);
        }

        private void PopulateHostListControl(ListViewItem containerItem)
        {
            var siteHosts = GetSiteHosts();
            if (siteHosts.Count() > 1)
            {
                var ddl = containerItem.FindControl("ddlHostUrls") as DropDownList;

                if (ddl != null)
                {
                    ddl.DataSource = siteHosts;
                    ddl.DataBind();
                    ddl.Visible = true;

                    if (containerItem.DataItem != null)
                    {
                        var data = containerItem.DataItem as SitemapData;
                        if (data != null && data.SiteUrl != null && siteHosts.Contains(data.SiteUrl))
                        {
                            ddl.SelectedIndex = siteHosts.IndexOf(data.SiteUrl);
                        }
                    }
                }
            }
            else
            {
                var label = containerItem.FindControl("lblHostUrl") as Label;
                if (label != null)
                {
                    label.Text = siteHosts.ElementAt(0);
                    label.Visible = true;
                }
            }
        }

        protected void lvwSitemapData_ItemCommand(object sender, ListViewCommandEventArgs e)
        {
            switch (e.CommandName)
            {
                case "Insert":
                    {
                        InsertSitemapData(e.Item);
                        break;
                    }
                case "Update":
                    {
                        UpdateSitemapData(Identity.Parse(e.CommandArgument.ToString()), e.Item);
                        break;
                    }
                case "Delete":
                    {
                        DeleteSitemapData(Identity.Parse(e.CommandArgument.ToString()));
                        break;
                    }
                case "ViewSitemap":
                    {
                        ViewSitemap(Identity.Parse(e.CommandArgument.ToString()));
                        break;
                    }
            }
        }

        private void InsertSitemapData(ListViewItem insertItem)
        {
            var sitemapData = new SitemapData
                {
                    SiteUrl = GetSelectedSiteUrl(insertItem),
                    Host = ((TextBox) insertItem.FindControl("txtHost")).Text + SitemapHostPostfix,
                    PathsToAvoid = GetDirectoryList(insertItem, "txtDirectoriesToAvoid"),
                    PathsToInclude = GetDirectoryList(insertItem, "txtDirectoriesToInclude"),
                    IncludeDebugInfo = ((CheckBox) insertItem.FindControl("cbIncludeDebugInfo")).Checked,
                    SitemapFormat = IsMobileSitemapFormatChecked(insertItem) ? SitemapFormat.Mobile : SitemapFormat.Standard,
                    RootPageId = TryParse(((TextBox) insertItem.FindControl("txtRootPageId")).Text)
                };

            sitemapRepository.Save(sitemapData);

            CloseInsert();
            BindList();
        }

        private static string GetSelectedSiteUrl(Control containerControl)
        {
            var ddl = containerControl.FindControl("ddlHostUrls") as DropDownList;

            if (ddl != null && ddl.Items.Count > 0)
            {
                return ddl.SelectedItem.Text;
            }
                 
            return null;    
        }

        private static int TryParse(string strValue)
        {
            int rv;
            int.TryParse(strValue, out rv);

            return rv;
        }

        private IList<string> GetDirectoryList(Control containerControl, string fieldName)
        {
            string strValue = ((TextBox) containerControl.FindControl(fieldName)).Text.Trim();

            if (string.IsNullOrEmpty(strValue))
            {
                return null;
            }

            return new List<string>(Url.Encode(strValue).Split(';'));
        }

         protected string GetDirectoriesString(Object directoryListObject)
        {
            if (directoryListObject == null)
            {
                return string.Empty;
            }

            return String.Join(";", ((IList<string>) directoryListObject));
        }

        private static bool IsMobileSitemapFormatChecked(Control container)
        {
            if (((RadioButton) container.FindControl("rbMobile")).Checked)
            {
                return true;
            }

            return false;
        }

        private void UpdateSitemapData(Identity id, ListViewItem item)
        {                                                                
            var sitemapData = sitemapRepository.GetSitemapData(id);

            if (sitemapData == null)
            {
                return;
            }

            sitemapData.Host = ((TextBox)item.FindControl("txtHost")).Text + SitemapHostPostfix;
            sitemapData.PathsToAvoid = GetDirectoryList(item, "txtDirectoriesToAvoid");
            sitemapData.PathsToInclude = GetDirectoryList(item, "txtDirectoriesToInclude");
            sitemapData.IncludeDebugInfo = ((CheckBox) item.FindControl("cbIncludeDebugInfo")).Checked;
            sitemapData.SitemapFormat = IsMobileSitemapFormatChecked(item)
                                            ? SitemapFormat.Mobile
                                            : SitemapFormat.Standard;
            sitemapData.RootPageId = TryParse(((TextBox) item.FindControl("txtRootPageId")).Text);
            sitemapData.SiteUrl = GetSelectedSiteUrl(item);

            sitemapRepository.Save(sitemapData);

            lvwSitemapData.EditIndex = -1;
            BindList();
        }

        private void DeleteSitemapData(Identity id)
        {
            sitemapRepository.Delete(id);
            BindList();
        }

        private void ViewSitemap(Identity id)
        {
            var data = sitemapRepository.GetSitemapData(id).Data;

            Response.ContentType = "text/xml";
            Response.BinaryWrite(data);
            Response.End();
        }

        protected void lvwSitemapData_ItemDataBound(object sender, ListViewItemEventArgs e)
        {
            PopulateHostListControl(e.Item);
        }

        protected IList<string> GetSiteHosts()
        {
            var hosts = SiteInformationHandler.GetSitesInformation(false);

            var siteUrls = new List<string>(hosts.Count);

            foreach (var siteInformation in hosts)
            {
                siteUrls.Add(siteInformation.SiteUrl.ToString());
            }

            return siteUrls;
        }

        protected string GetSiteUrl(Object evaluatedUrl)
        {
            if (evaluatedUrl != null)
            {
                return evaluatedUrl.ToString();
            }

            //get current site url
            return SiteInformationHandler.GetCurrentSiteInformation().SiteUrl.ToString();
        }

        protected void lvwSitemapData_ItemInserting(object sender, ListViewInsertEventArgs e)
        {
        }

        protected void lvwSitemapData_ItemUpdating(object sender, ListViewUpdateEventArgs e)
        {
        }

        protected void lvwSitemapData_ItemEditing(object sender, ListViewEditEventArgs e)
        {
            CloseInsert();
            lvwSitemapData.EditIndex = e.NewEditIndex;
            BindList();
        }

        protected void lvwSitemapData_ItemCanceling(object sender, ListViewCancelEventArgs e)
        {
            if (e.CancelMode == ListViewCancelMode.CancelingInsert)
            {
                CloseInsert();
            }
            else
            {
                lvwSitemapData.EditIndex = -1;
            }

            BindList();
        }

        private void CloseInsert()
        {
            lvwSitemapData.InsertItemPosition = InsertItemPosition.None;
        }

        protected void lvwSitemapData_ItemDeleting(object sender, ListViewDeleteEventArgs e)
        {
        }

        protected static string GetHostNameEditPart(string hostName)
        {
            return hostName.Substring(0, hostName.IndexOf(SitemapHostPostfix, StringComparison.InvariantCulture));
        }
    }
}