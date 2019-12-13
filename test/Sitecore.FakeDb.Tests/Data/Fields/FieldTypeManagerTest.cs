namespace Sitecore.FakeDb.Tests.Data.Fields
{
    using System;
    using FluentAssertions;
    using Sitecore.Data.Fields;
    using Xunit;

    [Trait("Category", "RequireLicense")]
    public class FieldTypeManagerTest
    {
        [Theory]

        // Simple Types
        [InlineData("Checkbox", typeof(CheckboxField))]
        [InlineData("Date", typeof(DateField))]
        [InlineData("Datetime", typeof(DateField))]
        [InlineData("File", typeof(FileField))]
        [InlineData("Image", typeof(ImageField))]
        [InlineData("Rich Text", typeof(HtmlField))]
        [InlineData("Single-Line Text", typeof(TextField))]
        [InlineData("Multi-Line Text", typeof(TextField))]

        // List Types
        [InlineData("Checklist", typeof(MultilistField))]
        [InlineData("Droplist", typeof(ValueLookupField))]
        [InlineData("Grouped Droplink", typeof(GroupedDroplinkField))]
        [InlineData("Grouped Droplist", typeof(GroupedDroplistField))]
        [InlineData("Multilist", typeof(MultilistField))]
        [InlineData("Multilist with Search", typeof(MultilistField))]
        [InlineData("Name Value List", typeof(NameValueListField))]
        [InlineData("Treelist", typeof(MultilistField))]
        [InlineData("Treelist with Search", typeof(MultilistField))]
        [InlineData("TreelistEx", typeof(MultilistField))]

        // Link Types
        [InlineData("Droplink", typeof(LookupField))]
        [InlineData("Droptree", typeof(ReferenceField))]
        [InlineData("General Link", typeof(LinkField))]
        [InlineData("General Link with Search", typeof(LinkField))]
        [InlineData("Version Link", typeof(VersionLinkField))]

        // Developer Types
        [InlineData("Frame", typeof(TextField))]
        [InlineData("Rules", typeof(RulesField))]

        // System Types 
        [InlineData("Datasource", typeof(DatasourceField))]
        [InlineData("Custom", typeof(CustomCustomField))]
        [InlineData("Internal Link", typeof(InternalLinkField))]
        [InlineData("Template Field Source", typeof(TemplateFieldSourceField))]
        [InlineData("File Drop Area", typeof(FileDropAreaField))]
        [InlineData("Page Preview", typeof(PagePreviewField))]

        // [InlineData("Rendering Datasource", typeof(RenderingDatasourceField))]
        [InlineData("Thumbnail", typeof(ThumbnailField))]
        [InlineData("Security", typeof(TextField))]
        [InlineData("UserList", typeof(TextField))]
        public void ShouldGetField(string name, Type type)
        {
            using (var db = new Db
                {
                    new DbItem("home") {new DbField("field") {Type = name}}
                })
            {
                var home = db.GetItem("/sitecore/content/home");

                FieldTypeManager.GetField(home.Fields["field"]).Should().BeOfType(type);
                FieldTypeManager.GetFieldType(name).Type.Should().Be(type);
            }
        }

        [Fact]
        public void ShouldGetLayoutField()
        {
            using (var db = new Db { new DbItem("home") })
            {
                var home = db.GetItem("/sitecore/content/home");

                FieldTypeManager.GetField(home.Fields[FieldIDs.LayoutField]).Should().BeOfType<LayoutField>();
                FieldTypeManager.GetFieldType("Layout").Type.Should().Be<LayoutField>();
            }
        }

        [Fact]
        public void ShouldNotThrowOnGetDefaultFieldTypeItem()
        {
            using (new Db("core"))
            {
                FieldTypeManager.GetDefaultFieldTypeItem();
            }
        }

        [Fact]
        public void ShouldNotThrowOnGetFieldTypeItem()
        {
            using (new Db("core"))
            {
                FieldTypeManager.GetFieldTypeItem("text");
            }
        }

        [Fact]
        public void ShouldNotThrowOnGetTemplateFieldItem()
        {
            using (var db = new Db
                {
                    new DbItem("home") {new DbField("field")}
                })
            {
                var home = db.GetItem("/sitecore/content/home");

                FieldTypeManager.GetTemplateFieldItem(home.Fields["field"]);
            }
        }
    }
}
