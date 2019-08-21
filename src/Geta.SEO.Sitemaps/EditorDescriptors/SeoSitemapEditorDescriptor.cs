// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Shell.ObjectEditing.EditorDescriptors;

namespace Geta.SEO.Sitemaps.EditorDescriptors
{
    [EditorDescriptorRegistration(TargetType = typeof(string), UIHint = "SeoSitemap")]
    public class SeoSitemapEditorDescriptor : EditorDescriptor
    {
        public SeoSitemapEditorDescriptor()
        {
            ClientEditingClass = "seositemaps/Editor";
        }
    }
}
