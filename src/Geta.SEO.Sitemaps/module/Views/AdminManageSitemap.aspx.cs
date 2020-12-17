// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Helpers;
using System.Web.UI;
using System.Web.UI.WebControls;
using EPiServer;
using EPiServer.Data;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Geta.SEO.Sitemaps.Configuration;
using Geta.SEO.Sitemaps.Entities;
using Geta.SEO.Sitemaps.Repositories;
using Geta.SEO.Sitemaps.Utils;

namespace Geta.SEO.Sitemaps.Modules.Geta.SEO.Sitemaps
{
    [GuiPlugIn(Area = PlugInArea.AdminMenu,
        DisplayName = "Search engine sitemap settings",
        Description = "Manage the sitemap module settings and content",
        UrlFromModuleFolder = "Views/AdminManageSitemap.aspx",
        RequiredAccess = AccessLevel.Administer)]
    public partial class AdminManageSitemap : SimplePage
    {
        public Injected<ISitemapRepository> SitemapRepository { get; set; }
        public Injected<ISiteDefinitionRepository> SiteDefinitionRepository { get; set; }
        public Injected<ILanguageBranchRepository> LanguageBranchRepository { get; set; }
        protected IList<string> SiteHosts { get; set; }
        protected bool ShowLanguageDropDown { get; set; }
        protected IList<LanguageBranchData> LanguageBranches { get; set; }

        protected SitemapData CurrentSitemapData
        {
            get { return this.GetDataItem() as SitemapData; }
        }

        protected const string SitemapHostPostfix = "Sitemap.xml";

        protected override void OnPreInit(EventArgs e)
        {
            base.OnPreInit(e);

            MasterPageFile = ResolveUrlFromUI("MasterPages/EPiServerUI.master");
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (IsPostBack)
            {
                // will throw exception if invalid
                AntiForgery.Validate();
            }

            SiteHosts = GetSiteHosts();
            ShowLanguageDropDown = ShouldShowLanguageDropDown();

            LanguageBranches = LanguageBranchRepository.Service.ListEnabled().Select(x => new LanguageBranchData
            {
                DisplayName = x.URLSegment,
                Language = x.Culture.Name
            }).ToList();

            LanguageBranches.Insert(0, new LanguageBranchData
            {
                DisplayName = "*",
                Language = ""
            });

            if (!PrincipalInfo.HasAdminAccess)
            {
                AccessDenied();
            }

            if (!IsPostBack)
            {
                BindList();
            }

            SystemPrefixControl.Heading = "Search engine sitemap settings";
        }

        private void BindList()
        {
            lvwSitemapData.DataSource = SitemapRepository.Service.GetAllSitemapData();
            lvwSitemapData.DataBind();
        }

        protected void btnNew_Click(object sender, EventArgs e)
        {
            lvwSitemapData.EditIndex = -1;
            lvwSitemapData.InsertItemPosition = InsertItemPosition.LastItem;

            BindList();

            PopulateHostListControl(lvwSitemapData.InsertItem);
        }

        private void PopulateLanguageListControl(ListViewItem containerItem)
        {
            var ddl = containerItem.FindControl("ddlLanguage") as DropDownList;

            if (ddl == null || containerItem.DataItem == null)
            {
                return;
            }

            var data = containerItem.DataItem as SitemapData;
            if (data == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(data.Language))
            {
                var selectedItem = ddl.Items.FindByValue(data.Language);

                if (selectedItem != null)
                {
                    ddl.SelectedValue = selectedItem.Value;
                }
            }
        }

        private void PopulateHostListControl(ListViewItem containerItem)
        {
            var siteHosts = SiteHosts;

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
                Host = ((TextBox)insertItem.FindControl("txtHost")).Text + SitemapHostPostfix,
                Language = ((DropDownList)insertItem.FindControl("ddlLanguage")).SelectedValue,
                EnableLanguageFallback = ((CheckBox)insertItem.FindControl("cbEnableLanguageFallback")).Checked,
                IncludeAlternateLanguagePages = ((CheckBox)insertItem.FindControl("cbIncludeAlternateLanguagePages")).Checked,
                EnableSimpleAddressSupport = ((CheckBox)insertItem.FindControl("cbEnableSimpleAddressSupport")).Checked,
                PathsToAvoid = GetDirectoryList(insertItem, "txtDirectoriesToAvoid"),
                PathsToInclude = GetDirectoryList(insertItem, "txtDirectoriesToInclude"),
                IncludeDebugInfo = ((CheckBox)insertItem.FindControl("cbIncludeDebugInfo")).Checked,
                SitemapFormat = GetSitemapFormat(insertItem),
                RootPageId = TryParse(((TextBox)insertItem.FindControl("txtRootPageId")).Text)
            };

            SitemapRepository.Service.Save(sitemapData);

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

            var label = containerControl.FindControl("lblHostUrl") as Label;

            return label != null ? label.Text : null;
        }

        private static int TryParse(string strValue)
        {
            int rv;
            int.TryParse(strValue, out rv);

            return rv;
        }

        private IList<string> GetDirectoryList(Control containerControl, string fieldName)
        {
            string strValue = ((TextBox)containerControl.FindControl(fieldName)).Text.Trim();

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

            return String.Join(";", ((IList<string>)directoryListObject));
        }

        protected string GetLanguage(string language)
        {
            if (!string.IsNullOrWhiteSpace(language) && SiteDefinition.WildcardHostName.Equals(language) == false)
            {
                var languageBranch = LanguageBranchRepository.Service.Load(language);
                return string.Format("{0}/", languageBranch.URLSegment);
            }

            return string.Empty;
        }

        protected bool ShouldShowLanguageDropDown()
        {
            return SitemapSettings.Instance.EnableLanguageDropDownInAdmin;
        }

        private SitemapFormat GetSitemapFormat(Control container)
        {
            if (((RadioButton)container.FindControl("rbMobile")).Checked)
            {
                return SitemapFormat.Mobile;
            }

            if (((RadioButton)container.FindControl("rbCommerce")).Checked)
            {
                return SitemapFormat.Commerce;
            }

            if (((RadioButton)container.FindControl("rbStandardAndCommerce")).Checked)
            {
                return SitemapFormat.StandardAndCommerce;
            }

            return SitemapFormat.Standard;
        }

        private void UpdateSitemapData(Identity id, ListViewItem item)
        {
            var sitemapData = SitemapRepository.Service.GetSitemapData(id);

            if (sitemapData == null)
            {
                return;
            }

            sitemapData.Host = ((TextBox)item.FindControl("txtHost")).Text + SitemapHostPostfix;
            sitemapData.Language = ((DropDownList)item.FindControl("ddlLanguage")).SelectedValue;
            sitemapData.EnableLanguageFallback = ((CheckBox)item.FindControl("cbEnableLanguageFallback")).Checked;
            sitemapData.IncludeAlternateLanguagePages = ((CheckBox) item.FindControl("cbIncludeAlternateLanguagePages")).Checked;
            sitemapData.EnableSimpleAddressSupport = ((CheckBox)item.FindControl("cbEnableSimpleAddressSupport")).Checked;
            sitemapData.PathsToAvoid = GetDirectoryList(item, "txtDirectoriesToAvoid");
            sitemapData.PathsToInclude = GetDirectoryList(item, "txtDirectoriesToInclude");
            sitemapData.IncludeDebugInfo = ((CheckBox)item.FindControl("cbIncludeDebugInfo")).Checked;
            sitemapData.SitemapFormat = GetSitemapFormat(item);
            sitemapData.RootPageId = TryParse(((TextBox)item.FindControl("txtRootPageId")).Text);
            sitemapData.SiteUrl = GetSelectedSiteUrl(item);

            SitemapRepository.Service.Save(sitemapData);

            lvwSitemapData.EditIndex = -1;
            BindList();
        }

        private void DeleteSitemapData(Identity id)
        {
            SitemapRepository.Service.Delete(id);
            BindList();
        }

        private void ViewSitemap(Identity id)
        {
            var data = SitemapRepository.Service.GetSitemapData(id).Data;

            Response.ContentType = "text/xml";
            Response.BinaryWrite(data);
            Response.End();
        }

        protected void lvwSitemapData_ItemDataBound(object sender, ListViewItemEventArgs e)
        {
            PopulateHostListControl(e.Item);
            PopulateLanguageListControl(e.Item);
        }

        protected IList<string> GetSiteHosts()
        {
            var siteDefinitionRepository = SiteDefinitionRepository.Service;

            IList<SiteDefinition> hosts = siteDefinitionRepository.List().ToList();

            var siteUrls = new List<string>(hosts.Count);

            foreach (var siteInformation in hosts)
            {
                siteUrls.Add(siteInformation.SiteUrl.ToString());

                foreach (var host in siteInformation.Hosts)
                {
                    if (ShouldAddToSiteHosts(host, siteInformation))
                    {
                        var hostUri = host.GetUri();
                        siteUrls.Add(hostUri.ToString());
                    }
                }
            }

            return siteUrls;
        }

        private static bool ShouldAddToSiteHosts(HostDefinition host, SiteDefinition siteInformation)
        {
            if (host.Name == "*") return false;
            return !UriComparer.SchemeAndServerEquals(host.GetUri(), siteInformation.SiteUrl);
        }

        protected string GetSiteUrl(Object evaluatedUrl)
        {
            if (evaluatedUrl != null)
            {
                return evaluatedUrl.ToString();
            }

            //get current site url
            return SiteDefinition.Current.SiteUrl.ToString();
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

    public class LanguageBranchData
    {
        public string Language { get; set; }
        public string DisplayName { get; set; }
    }
}