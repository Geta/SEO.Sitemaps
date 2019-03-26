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
