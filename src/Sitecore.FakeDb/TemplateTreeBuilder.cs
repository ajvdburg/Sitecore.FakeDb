﻿namespace Sitecore.FakeDb
{
    using System.Linq;
    using Sitecore.Data;

    public class TemplateTreeBuilder
    {
        public void Build(DbItem template)
        {
            if ((template as DbTemplate) == null)
            {
                return;
            }

            var dataSection = new DbItem("Data", ID.NewID, TemplateIDs.TemplateSection);
            template.Children.Add(dataSection);

            foreach (var field in template.Fields.Where(field => !field.IsStandard()))
            {
                dataSection.Children.Add(
                    new DbItem(field.Name, field.ID, TemplateIDs.TemplateField)
                        {
                            new DbField(TemplateFieldIDs.Type) {Value = field.Type},
                            new DbField(TemplateFieldIDs.Shared) {Value = field.Shared ? "1" : string.Empty},
                            new DbField(TemplateFieldIDs.Source) {Value = field.Source}
                        });
            }
        }
    }
}