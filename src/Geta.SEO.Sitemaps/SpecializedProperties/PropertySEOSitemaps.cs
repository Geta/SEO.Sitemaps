// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

/*
 * Code below originally comes from https://www.coderesort.com/p/epicode/wiki/SearchEngineSitemaps
 * Author: Jacob Khan
 */

using System.IO;
using System.Xml;
using System.Xml.Serialization;
using EPiServer.Core;
using EPiServer.PlugIn;

namespace Geta.SEO.Sitemaps.SpecializedProperties
{
    [PropertyDefinitionTypePlugIn(DisplayName = "SEOSitemaps")]
    public class PropertySEOSitemaps : PropertyString
    {
        public static string PropertyName = "SEOSitemaps";

        protected string changeFrequency = "weekly";

        protected bool enabled = true;

        protected string priority = "0.5";

        public string ChangeFreq
        {
            get
            {
                return this.changeFrequency;
            }

            set
            {
                this.changeFrequency = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return this.enabled;
            }

            set
            {
                this.enabled = value;
            }
        }

        public string Priority
        {
            get
            {
                return this.priority;
            }

            set
            {
                this.priority = value;
            }
        }

        [XmlIgnore]
        protected override string String
        {
            get
            {
                return base.String;
            }

            set
            {
                this.Deserialize(value);
                base.String = value;
            }
        }

        public void Deserialize(string xml)
        {
            StringReader s = new StringReader(xml);
            XmlTextReader reader = new XmlTextReader(s);

            reader.ReadStartElement(PropertyName);

            this.enabled = bool.Parse(reader.ReadElementString("enabled"));
            this.changeFrequency = reader.ReadElementString("changefreq");
            this.priority = reader.ReadElementString("priority");

            reader.ReadEndElement();

            reader.Close();
        }

        public override PropertyData ParseToObject(string str)
        {
            return Parse(str);
        }

        public void Serialize()
        {
            StringWriter s = new StringWriter();
            XmlTextWriter writer = new XmlTextWriter(s);

            writer.WriteStartElement(PropertyName);

            writer.WriteElementString("enabled", this.enabled.ToString());
            writer.WriteElementString("changefreq", this.changeFrequency);
            writer.WriteElementString("priority", this.priority);

            writer.WriteEndElement();

            writer.Flush();
            writer.Close();

            this.String = s.GetStringBuilder().ToString();
        }
    }
}