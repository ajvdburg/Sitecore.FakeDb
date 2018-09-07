﻿namespace Sitecore.FakeDb.Data.DataProviders
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Sitecore.Collections;
    using Sitecore.Data;
    using Sitecore.Data.DataProviders;
    using Sitecore.Data.Items;
    using Sitecore.Data.Query;
    using Sitecore.Data.Templates;
    using Sitecore.Diagnostics;
    using Sitecore.FakeDb.Data.Engines;
    using Sitecore.Globalization;
    using CallContext = Sitecore.Data.DataProviders.CallContext;
    using Version = Sitecore.Data.Version;

    public class FakeDataProvider : DataProvider
    {
        private readonly ThreadLocal<Dictionary<string, string>> properties = new ThreadLocal<Dictionary<string, string>>();

        private readonly ThreadLocal<List<PublishQueueItem>> publishQueue = new ThreadLocal<List<PublishQueueItem>>();

        private readonly DataStorage dataStorage;

        public FakeDataProvider()
        {
        }

        public FakeDataProvider(DataStorage dataStorage)
        {
            this.dataStorage = dataStorage;
        }

        public virtual DataStorage DataStorage => this.dataStorage ?? DataStorageSwitcher.CurrentValue(this.Database.Name);

        public override bool AddToPublishQueue(ID itemId, string action, DateTime date, CallContext context)
        {
            if (this.publishQueue.Value == null)
            {
                this.publishQueue.Value = new List<PublishQueueItem>();
            }

            this.publishQueue.Value.Add(new PublishQueueItem(itemId, date));
            return true;
        }

        public override bool AddToPublishQueue(ID itemId, string action, DateTime date, string language, CallContext context)
        {
            return this.AddToPublishQueue(itemId, action, date, context);
        }

        public override int AddVersion(ItemDefinition itemDefinition, VersionUri baseVersion, CallContext context)
        {
            Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
            Assert.ArgumentNotNull(baseVersion, "baseVersion");

            var item = this.DataStorage.GetFakeItem(itemDefinition.ID);
            Assert.IsNotNull(item, "Unable to add item version. The item '{0}' is not found.", itemDefinition.ID);

            item.AddVersion(baseVersion.Language.Name, baseVersion.Version.Number);
            return item.GetVersionCount(baseVersion.Language.Name);
        }

        public override bool ChangeTemplate(ItemDefinition itemDefinition, TemplateChangeList changes, CallContext context)
        {
            Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
            Assert.ArgumentNotNull(changes, "changes");

            var item = this.DataStorage.GetFakeItem(itemDefinition.ID);
            Assert.IsNotNull(item, "Unable to change item template. The item '{0}' is not found.", itemDefinition.ID);
            Assert.IsNotNull(changes.Target, "Unable to change item template. The target template is not found.");

            item.TemplateID = changes.Target.ID;
            return true;
        }

        public override bool CopyItem(ItemDefinition source, ItemDefinition destination, string copyName, ID copyId, CallContext context)
        {
            Assert.ArgumentNotNull(source, "source");
            Assert.ArgumentNotNull(destination, "destination");
            Assert.ArgumentNotNull(copyName, "copyName");
            Assert.ArgumentNotNull(copyId, "copyId");

            var copy = new DbItem(copyName, copyId, source.TemplateID) { ParentID = destination.ID };
            var sourceDbItem = this.DataStorage.GetFakeItem(source.ID);
            Assert.IsNotNull(sourceDbItem, "Unable to copy item '{0}'. The source item '{1}' is not found.", copyName, source.ID);

            CopyFields(sourceDbItem, copy);
            this.DataStorage.AddFakeItem(copy);

            return true;
        }

        public override bool CreateItem(ID itemId, string itemName, ID templateId, ItemDefinition parent, CallContext context)
        {
            Assert.ArgumentNotNull(itemId, "itemId");
            Assert.ArgumentNotNull(itemName, "itemName");
            Assert.ArgumentNotNull(templateId, "templateId");
            Assert.ArgumentNotNull(parent, "parent");

            var item = new DbItem(itemName, itemId, templateId) { ParentID = parent.ID };
            this.DataStorage.AddFakeItem(item);

            // TODO: Should not require the version removing.
            item.RemoveVersion(Language.Current.Name);

            return true;
        }

        public override bool DeleteItem(ItemDefinition itemDefinition, CallContext context)
        {
            Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

            var item = this.DataStorage.GetFakeItem(itemDefinition.ID);
            if (item == null)
            {
                return false;
            }

            return this.DataStorage.RemoveFakeItem(item.ID);
        }

        public override Stream GetBlobStream(Guid blobId, CallContext context)
        {
            return this.DataStorage.GetBlobStream(blobId);
        }

        public override ID GetParentID(ItemDefinition itemDefinition, CallContext context)
        {
            Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

            if (itemDefinition.ID == ItemIDs.RootID)
            {
                return null;
            }

            var fakeItem = this.DataStorage.GetFakeItem(itemDefinition.ID);
            return fakeItem?.ParentID;
        }

        public override IDList GetPublishQueue(DateTime @from, DateTime to, CallContext context)
        {
            if (this.publishQueue.Value == null)
            {
                return new IDList();
            }

            return IDList.Build(
                this.publishQueue.Value
                    .Where(i => i.Date >= @from && i.Date <= to)
                    .Select(i => i.ItemId)
                    .Distinct()
                    .ToArray());
        }

        public override IDList GetChildIDs(ItemDefinition itemDefinition, CallContext context)
        {
            Assert.ArgumentNotNull(itemDefinition, "itemDefinition");

            var childIds = new IDList();
            var item = this.DataStorage.GetFakeItem(itemDefinition.ID);
            if (item == null)
            {
                return childIds;
            }

            foreach (var child in item.Children)
            {
                childIds.Add(child.ID);
            }

            return childIds;
        }

        public override IdCollection GetTemplateItemIds(CallContext context)
        {
            if (this.DataStorage == null)
            {
                return new IdCollection();
            }

            var ids = this.DataStorage.GetFakeTemplates().Select(t => t.ID).ToArray();

            return new IdCollection { ids };
        }

        public override ItemDefinition GetItemDefinition(ID itemId, CallContext context)
        {
            var item = this.DataStorage?.GetFakeItem(itemId);
            return item != null ? new ItemDefinition(itemId, item.Name, item.TemplateID, ID.Null) : null;
        }

        public override VersionUriList GetItemVersions(ItemDefinition itemDefinition, CallContext context)
        {
            var list = new List<VersionUri>();
            var versions = new VersionUriList();

            var item = this.DataStorage.GetFakeItem(itemDefinition.ID);
            if (item == null)
            {
                return versions;
            }

            foreach (var field in item.Fields)
            {
                foreach (var fieldLang in field.Values)
                {
                    var language = fieldLang.Key;

                    foreach (var fieldVer in fieldLang.Value)
                    {
                        var version = fieldVer.Key;

                        if (list.Any(l => l.Language.Name == language && l.Version.Number == version))
                        {
                            continue;
                        }

                        list.Add(new VersionUri(Language.Parse(language), Version.Parse(version)));
                    }
                }
            }

            foreach (var version in list)
            {
                versions.Add(version);
            }

            return versions;
        }

        public override FieldList GetItemFields(ItemDefinition itemDefinition, VersionUri versionUri, CallContext context)
        {
            var storage = this.DataStorage;
            var item = storage.GetFakeItem(itemDefinition.ID);
            if (item == null)
            {
                return null;
            }

            return storage.BuildItemFieldList(item, itemDefinition.TemplateID, versionUri.Language, versionUri.Version);
        }

        public override TemplateCollection GetTemplates(CallContext context)
        {
            var templates = new TemplateCollection();

            if (this.DataStorage == null)
            {
                return templates;
            }

            foreach (var ft in this.DataStorage.GetFakeTemplates())
            {
                templates.Add(this.BuildTemplate(ft, templates));
            }

            return templates;
        }

        public override LanguageCollection GetLanguages(CallContext context)
        {
            return new LanguageCollection();
        }

        public override bool MoveItem(ItemDefinition itemDefinition, ItemDefinition destination, CallContext context)
        {
            Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
            Assert.ArgumentNotNull(destination, "destination");

            var item = this.DataStorage.GetFakeItem(itemDefinition.ID);
            Assert.IsNotNull(item, "Unable to move item. The item '{0}' is not found.", itemDefinition.ID);

            var newDestination = this.DataStorage.GetFakeItem(destination.ID);
            Assert.IsNotNull(newDestination, "Unable to move item. The destination item '{0}' is not found.", destination.ID);

            var oldParent = this.DataStorage.GetFakeItem(item.ParentID);
            oldParent?.Children.Remove(item);
            newDestination.Children.Add(item);

            return true;
        }

        public override bool RemoveVersion(ItemDefinition itemDefinition, VersionUri version, CallContext context)
        {
            Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
            Assert.ArgumentNotNull(version, "version");

            var item = this.DataStorage.GetFakeItem(itemDefinition.ID);
            Assert.IsNotNull(item, "Unable to remove item version. The item '{0}' is not found.", itemDefinition.ID);

            return item.RemoveVersion(version.Language.Name, version.Version.Number);
        }

        public override ID ResolvePath(string itemPath, CallContext context)
        {
            var storage = this.DataStorage;
            // TODO: Move the validation to a global place
            Assert.IsNotNull(storage, "Sitecore.FakeDb.Db instance has not been initialized.");

            if (ID.IsID(itemPath))
            {
                return new ID(itemPath);
            }

            itemPath = StringUtil.RemovePostfix("/", itemPath);
            var item = storage.GetFakeItems().FirstOrDefault(fi => string.Compare(fi.FullPath, itemPath, StringComparison.OrdinalIgnoreCase) == 0);

            return item?.ID;
        }

        public override bool SaveItem(ItemDefinition itemDefinition, ItemChanges changes, CallContext context)
        {
            Assert.ArgumentNotNull(itemDefinition, "itemDefinition");
            Assert.ArgumentNotNull(changes, "changes");

            var item = this.DataStorage.GetFakeItem(itemDefinition.ID);
            if (item == null)
            {
                return false;
            }

            var newName = changes.Properties
                .Where(p => p.Key == "name")
                .Select(p => p.Value.Value.ToString()).ToList();
            if (newName.Any())
            {
                var fullPath = item.FullPath;
                if (!string.IsNullOrEmpty(fullPath) && fullPath.Contains(item.Name))
                {
                    item.FullPath = fullPath.Substring(0, fullPath.LastIndexOf(item.Name, StringComparison.Ordinal)) + newName.First();
                }

                item.Name = newName.First();
            }

            if (changes.HasFieldsChanged)
            {
                foreach (FieldChange change in changes.FieldChanges)
                {
                    if (item.Fields.ContainsKey(change.FieldID))
                    {
                        item.Fields[change.FieldID]
                            .SetValue(change.Language.Name, change.Version.Number, change.Value);
                    }
                    else
                    {
                        item.Fields.Add(new DbField(change.FieldID)
                        {
                            Value = change.Value
                        });
                    }
                }
            }

            return false;
        }

        public override ID SelectSingleID(string query, CallContext context)
        {
            query = query.Replace("fast:", string.Empty);
            var item = Query.SelectSingleItem(query, this.Database);

            return item != null ? item.ID : ID.Null;
        }

        public override IDList SelectIDs(string query, CallContext context)
        {
            query = query.Replace("fast:", string.Empty);
            var items = Query.SelectItems(query, this.Database);

            return items != null ? IDList.Build(items.Select(i => i.ID).ToArray()) : new IDList();
        }

        public override bool SetBlobStream(Stream stream, Guid blobId, CallContext context)
        {
            this.DataStorage.SetBlobStream(blobId, stream);

            return true;
        }

        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The property value.</param>
        /// <param name="context">The context. Ignored.</param>
        /// <returns>Always True.</returns>
        [Obsolete]
        public override bool SetProperty(string name, string value, CallContext context)
        {
            Assert.ArgumentNotNull(name, "name");
            var currentProp = this.properties.Value;
            if (currentProp == null)
            {
                this.properties.Value = new Dictionary<string, string> { { name, value } };
            }
            else
            {
                this.properties.Value[name] = value;
            }

            return true;
        }

        /// <summary>
        /// Get the property.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="context">The context. Ignored.</param>
        /// <returns>The property value if exists. Otherwise null.</returns>
        [Obsolete]
        public override string GetProperty(string name, CallContext context)
        {
            Assert.ArgumentNotNull(name, "name");
            var currentProp = this.properties.Value;
            if (currentProp == null)
            {
                return null;
            }

            return currentProp.ContainsKey(name) ? currentProp[name] : null;
        }

        protected virtual Template BuildTemplate(DbTemplate ft, TemplateCollection templates)
        {
            var builder = new Template.Builder(ft.Name, ft.ID, templates);

            var sectionName = "Data";
            var sectionId = ID.NewID;

            var sectionItem = ft.Children.FirstOrDefault(i => i.TemplateID == TemplateIDs.TemplateSection);
            if (sectionItem != null)
            {
                sectionName = sectionItem.Name;
                sectionId = sectionItem.ID;
            }

            var section = builder.AddSection(sectionName, sectionId);

            foreach (var field in ft.Fields)
            {
                if (ft.ID != TemplateIDs.StandardTemplate && field.IsStandard())
                {
                    continue;
                }

                var newField = section.AddField(field.Name, field.ID);
                newField.SetShared(field.Shared);
                newField.SetType(field.Type);
                newField.SetSource(field.Source);
            }

            if (ft.ID != TemplateIDs.StandardTemplate)
            {
                builder.SetBaseIDs(ft.BaseIDs.Any() ? string.Join("|", ft.BaseIDs as IEnumerable<ID>) : TemplateIDs.StandardTemplate.ToString());
            }

            return builder.Template;
        }


        private static void CopyFields(DbItem source, DbItem copy)
        {
            foreach (var field in source.Fields)
            {
                copy.Fields.Add(new DbField(field.Name, field.ID)
                {
                    Shared = field.Shared,
                    Type = field.Type
                });

                if (field.Shared)
                {
                    copy.Fields[field.ID].Value = field.Value;
                }
                else
                {
                    foreach (var fieldValue in field.Values)
                    {
                        var language = fieldValue.Key;
                        var versions = fieldValue.Value.ToDictionary(v => v.Key, v => v.Value);

                        copy.Fields[field.ID].Values[language] = versions;
                    }
                }
            }
        }

        private class PublishQueueItem
        {
            public PublishQueueItem(ID itemId, DateTime date)
            {
                this.ItemId = itemId;
                this.Date = date;
            }

            public ID ItemId { get; }

            public DateTime Date { get; }
        }
    }
}