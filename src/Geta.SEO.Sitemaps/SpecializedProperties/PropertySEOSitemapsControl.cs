// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

/*
 * Code below originally comes from https://www.coderesort.com/p/epicode/wiki/SearchEngineSitemaps
 * Author: Jacob Khan
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using EPiServer.Web.PropertyControls;

namespace Geta.SEO.Sitemaps.SpecializedProperties
{
    public class PropertySEOSitemapsControl : PropertyStringControl
    {
        protected DropDownList changefreq;

        protected CheckBox enabled;

        protected TextBox oLoaded;

        protected DropDownList priority;

        public override void ApplyEditChanges()
        {
            var pgs = this.PropertyData as PropertySEOSitemaps;
            if (pgs == null)
            {
                throw new InvalidOperationException("PropertyData is not of type 'PropertySEOSitemaps'.");
            }

            pgs.Enabled = this.enabled.Checked;
            pgs.ChangeFreq = this.changefreq.SelectedValue;
            pgs.Priority = this.priority.SelectedValue;
            pgs.Serialize();
        }

        public override void CreateEditControls()
        {
            this.oLoaded = new TextBox { Visible = false, EnableViewState = true };

            this.Controls.Add(this.oLoaded);
            this.Controls.Add(new LiteralControl("<table border=\"0\">"));
            this.enabled = new CheckBox { ID = this.Name + "_enabled", CssClass = "EPEdit-inputBoolean" };

            this.AddSection("Enabled", this.enabled);

            this.changefreq = new DropDownList
                {
                   ID = this.Name + "_changefreq", Width = 140, CssClass = "EPEdit-inputDropDownList" 
                };

            var frequencyValues = new Dictionary<string, string> {
                    { "always", "Always" }, 
                    { "hourly", "Hourly" }, 
                    { "daily", "Daily" }, 
                    { "weekly", "Weekly" }, 
                    { "monthly", "Monthly" }, 
                    { "yearly", "Yearly" }, 
                    { "never", "Never" }
                };

            this.changefreq.Items.AddRange(frequencyValues.Select(x => new ListItem(x.Value, x.Key)).ToArray());

            this.AddSection("Change frequency", this.changefreq);

            this.priority = new DropDownList
                {
                   ID = this.Name + "_priority", Width = 140, CssClass = "EPEdit-inputDropDownList" 
                };

            var priorityValues = new Dictionary<string, string[]> {
                    { "0.0", new[] { "low", "Low (0.0)" } }, 
                    { "0.25", new[] { "medium-low", "Medium-Low (0.25)" } }, 
                    { "0.5", new[] { "medium", "Medium (0.5)" } }, 
                    { "0.75", new[] { "medium-high", "Medium-High (0.75)" } }, 
                    { "1.0", new[] { "high", "High (1.0)" } }
                };

            this.priority.Items.AddRange(
                priorityValues.Select(
                    pv =>
                    new ListItem(pv.Value[1], pv.Key)).ToArray());

            this.AddSection("Priority", this.priority);
            this.Controls.Add(new LiteralControl("</table>"));

            // if this is not a postback, preload controls
            if (string.IsNullOrEmpty(this.oLoaded.Text))
            {
                this.oLoaded.Text = "loaded";
                var pgs = this.PropertyData as PropertySEOSitemaps;
                if (pgs == null)
                {
                    throw new InvalidOperationException("PropertyData is not of type 'PropertySEOSitemaps'");
                }

                this.enabled.Checked = pgs.Enabled;
                this.changefreq.Items.FindByValue(pgs.ChangeFreq).Selected = true;
                this.priority.Items.FindByValue(pgs.Priority).Selected = true;
            }
        }

        private void AddSection(string name, Control c)
        {
            this.Controls.Add(new LiteralControl(string.Format("<tr><td>{0}</td><td>", name)));
            this.Controls.Add(c);
            this.Controls.Add(new LiteralControl("</td></tr>"));
        }
    }
}
