﻿namespace Sitecore.FakeDb.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Web;
    using FluentAssertions;
    using Sitecore.Common;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Data.Managers;
    using Sitecore.Exceptions;
    using Sitecore.FakeDb.Data.Engines;
    using Sitecore.FakeDb.Security.AccessControl;
    using Sitecore.Globalization;
    using Sitecore.Reflection;
    using Sitecore.Security.AccessControl;
    using Sitecore.Security.Accounts;
    using Sitecore.SecurityModel;
    using Xunit;
    using Version = Sitecore.Data.Version;

    [Trait("Category", "RequireLicense")]
    public class DbTest
    {
        private readonly ID itemId = ID.NewID;

        private readonly ID templateId = ID.NewID;

        [Fact]
        public void ShouldCreateCoupleOfItemsWithFields()
        {
            // act
            using (var db = new Db
                {
                    new DbItem("item1") {{"Title", "Welcome from item 1!"}},
                    new DbItem("item2") {{"Title", "Welcome from item 2!"}}
                })
            {
                var item1 = db.Database.GetItem("/sitecore/content/item1");
                var item2 = db.Database.GetItem("/sitecore/content/item2");

                // assert
                item1["Title"].Should().Be("Welcome from item 1!");
                item2["Title"].Should().Be("Welcome from item 2!");
            }
        }

        [Fact]
        public void ShouldCreateItemHierarchyAndReadChildByPath()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("parent")
                        {
                            new DbItem("child")
                        }
                })
            {
                // assert
                db.GetItem("/sitecore/content/parent/child").Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldCreateItemInCustomLanguage()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("home") {Fields = {new DbField("Title") {{"da", "Hej!"}}}}
                })
            {
                var item = db.Database.GetItem("/sitecore/content/home", Language.Parse("da"));

                // assert
                item["Title"].Should().Be("Hej!");
                item.Language.Should().Be(Language.Parse("da"));
            }
        }

        [Fact]
        public void ShouldCreateItemInSpecificLanguage()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("home") {new DbField("Title") {{"en", "Hello!"}, {"da", "Hej!"}}}
                })
            {
                db.Database.GetItem("/sitecore/content/home", Language.Parse("en"))["Title"].Should().Be("Hello!");
                db.Database.GetItem("/sitecore/content/home", Language.Parse("da"))["Title"].Should().Be("Hej!");
            }
        }

        [Fact]
        public void ShouldCreateChildItemInSpecificLanguage()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home")
                })
            {
                var item = db.GetItem("/sitecore/content/home", Language.Parse("fr-FR").Name);

                // act
                var child = item.Add("child", item.Template);

                // assert
                child.Language.Name.Should().Be("fr-FR");
            }
        }

        [Fact]
        public void ShouldCreateItemOfPredefinedTemplate()
        {
            // act
            using (var db = new Db
                {
                    new DbTemplate("Sample", this.templateId) {"Title"},
                    new DbItem("Home", this.itemId, this.templateId)
                })
            {
                // assert
                var item = db.Database.GetItem(this.itemId);
                item.Fields["Title"].Should().NotBeNull();
                item.TemplateID.Should().Be(this.templateId);
            }
        }

        [Fact]
        public void ShouldCreateItemOfPredefinedTemplatePredefinedFields()
        {
            // act
            using (var db = new Db
                {
                    new DbTemplate("Sample", this.templateId) {"Title"},
                    new DbItem("Home", this.itemId, this.templateId) {{"Title", "Welcome!"}}
                })
            {
                // assert
                var item = db.GetItem(this.itemId);
                item.Fields["Title"].Value.Should().Be("Welcome!");
                item.TemplateID.Should().Be(this.templateId);
            }
        }

        [Fact]
        public void ShouldCreateItemOfVersionOne()
        {
            // arrange & act
            using (var db = new Db { new DbItem("home") })
            {
                var item = db.Database.GetItem("/sitecore/content/home");

                // assert
                item.Version.Should().Be(Version.First);
                item.Versions.Count.Should().Be(1);
                item.Versions[Version.First].Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldCreateItemTemplate()
        {
            // arrange & act
            using (var db = new Db { new DbTemplate("products") })
            {
                // assert
                db.Database.GetTemplate("products").Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldCreateItemWithFields()
        {
            // act
            using (var db = new Db
                {
                    new DbItem("home", this.itemId) {{"Title", "Welcome!"}}
                })
            {
                var item = db.Database.GetItem(this.itemId);

                // assert
                item["Title"].Should().Be("Welcome!");
            }
        }

        [Fact]
        public void ShouldCreateItemWithFieldsAndChildren()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("parent")
                        {
                            Fields = {{"Title", "Welcome to parent item!"}},
                            Children = {new DbItem("child") {{"Title", "Welcome to child item!"}}}
                        }
                })
            {
                // assert
                var parent = db.GetItem("/sitecore/content/parent");
                parent["Title"].Should().Be("Welcome to parent item!");
                parent.Children["child"]["Title"].Should().Be("Welcome to child item!");
            }
        }

        [Fact]
        public void ShouldCreateItemInInvariantLanguage()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("home") {new DbField("Title") {{string.Empty, "Hello!"}}}
                })
            {
                db.Database.GetItem("/sitecore/content/home", Language.Invariant)["Title"].Should().Be("Hello!");
            }
        }

        [Fact]
        public void ShouldGetItemInInvariantLanguage()
        {
            // arrange
            using (var db = new Db { new DbItem("home") })
            {
                // act
                var item = db.GetItem("/sitecore/content/home", Language.Invariant.Name);

                // assert
                item.Should().NotBeNull();
                item.Language.Should().Be(Language.Invariant);
            }
        }

        [Fact]
        public void ShouldGetItemInInvariantLanguageAndVersion()
        {
            // arrange
            using (var db = new Db { new DbItem("home") })
            {
                // act
                var item = db.GetItem("/sitecore/content/home", Language.Invariant.Name, 1);

                // assert
                item.Should().NotBeNull();
                item.Language.Should().Be(Language.Invariant);
            }
        }

        [Fact]
        public void ShouldCreateSimpleItem()
        {
            // arrange
            var id = new ID("{91494A40-B2AE-42B5-9469-1C7B023B886B}");

            // act
            using (var db = new Db { new DbItem("myitem", id) })
            {
                var i = db.Database.GetItem(id);

                // assert
                i.Should().NotBeNull();
                i.Name.Should().Be("myitem");
            }
        }

        [Fact]
        public void ShouldDenyItemCreateAccess()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home") {Access = new DbItemAccess {CanCreate = false}}
                })
            {
                var item = db.GetItem("/sitecore/content/home");

                // act
                Action action = () => item.Add("child", item.Template);

                // assert
                action.ShouldThrow<AccessDeniedException>();
            }
        }

        [Fact]
        public void ShouldDenyItemReadAccess()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("home") {Access = new DbItemAccess {CanRead = false}}
                })
            {
                // assert
                db.GetItem("/sitecore/content/home").Should().BeNull();
            }
        }

        [Fact]
        public void ShouldDenyItemWriteAccess()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home") {Access = new DbItemAccess {CanWrite = false}}
                })
            {
                var item = db.GetItem("/sitecore/content/home");

                // act
                Action action = () => new EditContext(item);

                // assert
                action.ShouldThrow<UnauthorizedAccessException>();
            }
        }

        [Fact]
        public void ShouldGenerateTemplateIdIfNotSet()
        {
            // arrange
            var template = new DbTemplate((ID)null);

            // act
            using (new Db { template })
            {
                // assert
                template.ID.Should().NotBeNull();
                template.ID.Should().NotBe(ID.Null);
            }
        }

        [Fact]
        public void ShouldGetItemById()
        {
            // arrange
            var id = ID.NewID;
            using (var db = new Db { new DbItem("my item", id) })
            {
                // act & assert
                db.GetItem(id).Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldGetItemByIdAndLanguage()
        {
            // arrange
            var id = ID.NewID;
            using (var db = new Db { new DbItem("my item", id) })
            {
                // act & assert
                db.GetItem(id, "en").Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldGetItemByIdLanguageAndVersion()
        {
            // arrange
            var id = ID.NewID;
            using (var db = new Db { new DbItem("my item", id) })
            {
                // act & assert
                db.GetItem(id, "en", 1).Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldGetItemByPath()
        {
            // arrange
            using (var db = new Db { new DbItem("my item") })
            {
                // act & assert
                db.GetItem("/sitecore/content/my item").Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldGetItemByPathAndLanguage()
        {
            // arrange
            using (var db = new Db { new DbItem("home") })
            {
                // act & assert
                db.GetItem("/sitecore/content/home", "en").Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldGetItemFromSitecoreDatabase()
        {
            // arrange
            using (var db = new Db())
            {
                // act & assert
                db.GetItem("/sitecore/content").Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldGetItemByUri()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home", this.itemId)
                        {
                            new DbField("Title")
                                {
                                    {"en", 1, "Welcome!"},
                                    {"da", 1, "Hello!"},
                                    {"da", 2, "Velkommen!"}
                                }
                        }
                })
            {
                // act & assert
                var uriEn1 = new ItemUri(this.itemId, Language.Parse("en"), Version.Parse(1), db.Database);
                Database.GetItem(uriEn1).Should().NotBeNull("the item '{0}' should not be null", uriEn1);
                Database.GetItem(uriEn1)["Title"].Should().Be("Welcome!");

                var uriDa1 = new ItemUri(this.itemId, Language.Parse("da"), Version.Parse(1), db.Database);
                Database.GetItem(uriDa1).Should().NotBeNull("the item '{0}' should not be null", uriDa1);
                Database.GetItem(uriDa1)["Title"].Should().Be("Hello!");

                var uriDa2 = new ItemUri(this.itemId, Language.Parse("da"), Version.Parse(2), db.Database);
                Database.GetItem(uriDa2).Should().NotBeNull("the item '{0}' should not be null", uriDa2);
                Database.GetItem(uriDa2)["Title"].Should().Be("Velkommen!");
            }
        }

        [Fact]
        public void ShouldGetItemParent()
        {
            // arrange
            using (var db = new Db { new DbItem("item") })
            {
                // act
                var parent = db.GetItem("/sitecore/content/item").Parent;

                // assert
                parent.Paths.FullPath.Should().Be("/sitecore/content");
            }
        }

        [Fact]
        public void ShouldHaveDefaultMasterDatabase()
        {
            // arrange
            using (var db = new Db())
            {
                // act & assert
                db.Database.Name.Should().Be("master");
            }
        }

        [Fact]
        public void ShouldInitializeDataStorage()
        {
            // arrange & act
            using (var db = new Db())
            {
                // assert
                db.DataStorage.Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldNotShareTemplateForItemsIfTemplatesSetExplicitly()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("article 1") {{"Title", "A1"}},
                    new DbItem("article 2", ID.NewID, ID.NewID) {{"Title", "A2"}}
                })
            {
                var item1 = db.GetItem("/sitecore/content/article 1");
                var item2 = db.GetItem("/sitecore/content/article 2");

                // assert
                item1.TemplateID.Should().NotBe(item2.TemplateID);

                item1["Title"].Should().Be("A1");
                item2["Title"].Should().Be("A2");
            }
        }

        [Fact]
        public void ShouldNotShareTemplateForItemsWithDifferentFields()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("some item") {{"some field", "some value"}},
                    new DbItem("another item") {{"another field", "another value"}}
                })
            {
                var template1 = db.GetItem("/sitecore/content/some item").TemplateID;
                var template2 = db.GetItem("/sitecore/content/another item").TemplateID;

                // assert
                template1.Should().NotBe(template2);
            }
        }

        [Fact]
        public void ShouldReadDefaultContentItem()
        {
            // arrange
            using (var db = new Db())
            {
                // act
                var item = db.Database.GetItem(ItemIDs.ContentRoot);

                // assert
                item.Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldReadFieldValueByIdAndName()
        {
            // arrange
            var fieldId = ID.NewID;
            using (var db = new Db
                {
                    new DbItem("home") {new DbField("Title", fieldId) {Value = "Hello!"}}
                })
            {
                // act
                var item = db.GetItem("/sitecore/content/home");

                // assert
                item[fieldId].Should().Be("Hello!");
                item["Title"].Should().Be("Hello!");
            }
        }

        [Fact]
        public void ShouldRemoveItemVersion()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home")
                        {
                            Fields = {new DbField("Title") {{"en", 1, "Hi"}, {"en", 2, "Hello"}}}
                        }
                })
            {
                var item = db.GetItem("/sitecore/content/home");

                // act
                item.Versions.RemoveVersion();

                // assert
                db.GetItem("/sitecore/content/home", "en", 1)["Title"].Should().Be("Hi");
                db.GetItem("/sitecore/content/home", "en", 2)["Title"].Should().BeEmpty();
            }
        }

        [Fact]
        public void ShouldRemoveAllVersions()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home")
                        {
                            Fields = {new DbField("Title") {{"en", 1, "Hi"}, {"da", 2, "Hey"}}}
                        }
                })
            {
                var item = db.GetItem("/sitecore/content/home");

                // act
                item.Versions.RemoveAll(true);

                // assert
                db.GetItem("/sitecore/content/home", "en", 1)["Title"].Should().BeEmpty();
                db.GetItem("/sitecore/content/home", "da", 1)["Title"].Should().BeEmpty();
                db.GetItem("/sitecore/content/home", "da", 2)["Title"].Should().BeEmpty();
            }
        }

        [Fact]
        public void ShouldRemoveSpecificVersion()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home")
                        {
                            new DbField("Title") {{"en", 1, "v1"}, {"en", 2, "v2"}}
                        }
                })
            {
                var item = db.GetItem("/sitecore/content/home", "en", 1);

                // act
                item.Versions.RemoveVersion();

                // assert
                db.GetItem("/sitecore/content/home", "en", 1)["Title"].Should().BeEmpty();
                db.GetItem("/sitecore/content/home", "en", 2)["Title"].Should().Be("v2");
            }
        }

        [Fact]
        public void ShouldRenameItem()
        {
            // arrange
            using (var db = new Db { new DbItem("home") })
            {
                var home = db.Database.GetItem("/sitecore/content/home");

                // act
                using (new EditContext(home))
                {
                    home.Name = "new home";
                }

                // assert
                db.Database.GetItem("/sitecore/content/new home").Should().NotBeNull();
                db.Database.GetItem("/sitecore/content/new home").Name.Should().Be("new home");
                db.Database.GetItem("/sitecore/content/home").Should().BeNull();
            }
        }

        [Theory]
        [InlineData("master")]
        [InlineData("web")]
        [InlineData("core")]
        public void ShouldResolveDatabaseByName(string name)
        {
            // arrange
            using (var db = new Db(name))
            {
                // act & assert
                db.Database.Name.Should().Be(name);
            }
        }

        [Fact]
        public void ShouldSetAndGetCustomSettings()
        {
            // arrange
            using (var db = new Db())
            {
                // act
                db.Configuration.Settings["my setting"] = "my new value";

                // assert
                Settings.GetSetting("my setting").Should().Be("my new value");
            }
        }

        [Fact]
        public void ShouldCleanUpSettingsAfterDispose()
        {
            // arrange
            using (var db = new Db())
            {
                // act
                db.Configuration.Settings["my setting"] = "my new value";
            }

            // assert
            Settings.GetSetting("my setting").Should().BeEmpty();
        }

        [Fact]
        public void ShouldSetChildItemFullIfParentIdIsSet()
        {
            // arrange
            var parent = new DbItem("parent");
            var child = new DbItem("child");

            // act
            using (var db = new Db { parent })
            {
                child.ParentID = parent.ID;
                db.Add(child);

                // assert
                child.FullPath.Should().Be("/sitecore/content/parent/child");
            }
        }

        [Fact]
        public void ShouldSetChildItemFullPathOnDbInit()
        {
            // arrange
            var parent = new DbItem("parent");
            var child = new DbItem("child");

            parent.Add(child);

            // act
            using (new Db { parent })
            {
                // assert
                child.FullPath.Should().Be("/sitecore/content/parent/child");
            }
        }

        [Fact]
        public void ShouldSetDatabaseInDataStorage()
        {
            // arrange & act
            using (var db = new Db())
            {
                // assert
                db.DataStorage.Database.Should().BeSameAs(db.Database);
            }
        }

        [Fact]
        public void ShouldSetDefaultLanguage()
        {
            // arrange & act
            using (var db = new Db { new DbItem("home") })
            {
                var item = db.Database.GetItem("/sitecore/content/home");

                // assert
                item.Language.Should().Be(Language.Parse("en"));
            }
        }

        [Fact]
        public void ShouldSetSitecoreContentFullPathByDefault()
        {
            // arrange
            var item = new DbItem("home");

            // act
            using (new Db { item })
            {
                // asert
                item.FullPath.Should().Be("/sitecore/content/home");
            }
        }

        [Fact]
        public void ShouldSetSitecoreContentParentIdByDefault()
        {
            // arrange
            var item = new DbItem("home");

            // act
            using (new Db { item })
            {
                // assert
                item.ParentID.Should().Be(ItemIDs.ContentRoot);
            }
        }

        [Fact]
        public void ShouldShareTemplateForItemsWithFields()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("article 1") {{"Title", "A1"}},
                    new DbItem("article 2") {{"Title", "A2"}}
                })
            {
                var template1 = db.GetItem("/sitecore/content/article 1").TemplateID;
                var template2 = db.GetItem("/sitecore/content/article 2").TemplateID;

                // assert
                template1.Should().Be(template2);
            }
        }

        [Theory]
        [InlineData("/sitecore/content/home", "/sitecore/content/site", true)]
        [InlineData("/sitecore/content/home", "/sitecore/content/site/four", true)]
        [InlineData("/sitecore/content/home", "/sitecore/content/home/one", false)]
        [InlineData("/sitecore/content/home/one", "/sitecore/content/site/two", true)]
        [InlineData("/sitecore/content/site/two", "/sitecore/content/site/three", false)]
        [InlineData("/sitecore/content/site/two", "/sitecore/content/site/four", false)]
        public void ShouldReuseGeneratedTemplateFromNotOnlySiblings(string pathOne, string pathTwo, bool match)
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home")
                        {
                            new DbItem("one") {{"Title", "One"}}
                        },
                    new DbItem("site")
                        {
                            new DbItem("two") {{"Title", "Two"}},
                            new DbItem("three") {{"Title", "Three"}, {"Name", "Three"}},
                            new DbItem("four")
                        }
                })
            {
                // act
                var one = db.GetItem(pathOne);
                var two = db.GetItem(pathTwo);

                // assert
                (one.TemplateID == two.TemplateID).Should().Be(match);
            }
        }

        [Fact]
        public void ShouldThrowIfNoDbInstanceInitialized()
        {
            // act
            Action action = () => Database.GetDatabase("master").GetItem("/sitecore/content");

            // assert
            action.ShouldThrow<InvalidOperationException>()
                .WithMessage("Sitecore.FakeDb.Db instance has not been initialized.");
        }

        [Fact]
        public void ShouldThrowIfItemIdIsInUse()
        {
            // arrange
            var id = new ID("{57289DB1-1C33-46DF-A7BA-C214B7F4C54C}");

            using (var db = new Db { new DbItem("old home", id) })
            {
                // act
                Action action = () => db.Add(new DbItem("new home", id));

                // assert
                action.ShouldThrow<InvalidOperationException>()
                    .WithMessage("An item with the same id has already been added ('{57289DB1-1C33-46DF-A7BA-C214B7F4C54C}', '/sitecore/content/new home').");
            }
        }

        [Fact]
        public void ShouldThrowIfTemplateIdIsInUseByOtherTemplate()
        {
            // arrange
            var id = new ID("{825697FD-5EED-47ED-8404-E9A47D7D6BDF}");

            using (var db = new Db { new DbTemplate("old product", id) })
            {
                // act
                Action action = () => db.Add(new DbTemplate("new product", id));

                // assert
                action.ShouldThrow<InvalidOperationException>()
                    .WithMessage("A template with the same id has already been added ('{825697FD-5EED-47ED-8404-E9A47D7D6BDF}', 'new product').");
            }
        }

        [Fact]
        public void ShouldThrowIfTemplateIdIsInUseByOtherItem()
        {
            // arrange
            var existingItemId = new ID("{61A9DB3D-8929-4472-A952-543F5304E341}");
            var newItemTemplateId = existingItemId;

            using (var db = new Db { new DbItem("existing item", existingItemId) })
            {
                // act
                Action action = () => db.Add(new DbItem("new item", ID.NewID, newItemTemplateId));

                // assert
                action.ShouldThrow<InvalidOperationException>()
                    .WithMessage("Unable to create the item based on the template '{61A9DB3D-8929-4472-A952-543F5304E341}'. An item with the same id has already been added ('/sitecore/content/existing item').");
            }
        }

        [Fact]
        public void ShouldInitializeDbConfigurationUsingFactoryConfiguration()
        {
            // arrange
            using (var db = new Db())
            {
                // act & assert
                db.Configuration.Settings.ConfigSection.Should().BeEquivalentTo(Factory.GetConfiguration());
            }
        }

        [Fact]
        public void ShouldInitializePipelineWatcherUsingFactoryConfiguration()
        {
            // arrange
            using (var db = new Db())
            {
                // act & assert
                db.PipelineWatcher.ConfigSection.Should().BeEquivalentTo(Factory.GetConfiguration());
            }
        }

        [Fact]
        public void ShouldBeEqualsButNotSame()
        {
            // arrange
            using (var db = new Db { new DbItem("home") })
            {
                // act
                var item1 = db.GetItem("/sitecore/content/Home");
                var item2 = db.GetItem("/sitecore/content/Home");

                // assert
                item1.Should().Be(item2);
                item1.Should().NotBeSameAs(item2);
            }
        }

        [Fact]
        public void ShouldCreateVersionedItem()
        {
            using (var db = new Db
                {
                    new DbItem("home")
                        {
                            Fields =
                                {
                                    new DbField("Title")
                                        {
                                            {"en", 1, "title version 1"},
                                            {"en", 2, "title version 2"}
                                        }
                                }
                        }
                })
            {
                var item1 = db.Database.GetItem("/sitecore/content/home", Language.Parse("en"), Version.Parse(1));
                item1["Title"].Should().Be("title version 1");
                item1.Version.Number.Should().Be(1);

                var item2 = db.Database.GetItem("/sitecore/content/home", Language.Parse("en"), Version.Parse(2));
                item2["Title"].Should().Be("title version 2");
                item2.Version.Number.Should().Be(2);
            }
        }

        [Fact]
        public void ShouldGetItemVersionsCount()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home")
                        {
                            Fields =
                                {
                                    new DbField("Title") {{"en", 1, "v1"}, {"en", 2, "v2"}}
                                }
                        }
                })
            {
                var item = db.GetItem("/sitecore/content/home");

                // act & assert
                item.Versions.Count.Should().Be(2);
            }
        }

        [Fact]
        public void ShouldGetLanguages()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home")
                        {
                            Fields =
                                {
                                    new DbField("Title") {{"en", 1, string.Empty}, {"da", 2, string.Empty}}
                                }
                        }
                })
            {
                var item = db.GetItem("/sitecore/content/home");

                // act & assert
                item.Languages.Length.Should().Be(2);
                item.Languages.Should().Contain(Language.Parse("en"));
                item.Languages.Should().Contain(Language.Parse("da"));
            }
        }

        [Fact]
        public void ShouldCreateItemVersion()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home") {{"Title", "hello"}}
                })
            {
                var item1 = db.GetItem("/sitecore/content/home");

                // act
                var item2 = item1.Versions.AddVersion();
                using (new EditContext(item2))
                {
                    item2["Title"] = "Hi there!";
                }

                // assert
                item1["Title"].Should().Be("hello");
                item2["Title"].Should().Be("Hi there!");

                db.GetItem("/sitecore/content/home", "en", 1)["Title"].Should().Be("hello");
                db.GetItem("/sitecore/content/home", "en", 2)["Title"].Should().Be("Hi there!");
            }
        }

        [Fact]
        public void ShouldCreateItemOfAnyVersion()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home") {{"Title", "title v1"}}
                })
            {
                var version2 = db.GetItem("/sitecore/content/home", "en", 2);

                // act
                using (new EditContext(version2))
                {
                    version2["Title"] = "title v2";
                }

                // assert
                db.GetItem("/sitecore/content/home", "en", 1)["Title"].Should().Be("title v1");
                db.GetItem("/sitecore/content/home", "en", 2)["Title"].Should().Be("title v2");
            }
        }

        [Fact]
        public void ShouldCreateAndFulfilCompositeFieldsStructure()
        {
            // arrange
            using (var db = new Db())
            {
                // act
                db.Add(new DbItem("item1") { { "field1", "item1-field1-value" }, { "field2", "item1-field2-value" } });
                db.Add(new DbItem("item2") { { "field1", "item2-field1-value" }, { "field2", "item2-field2-value" } });

                // assert
                db.GetItem("/sitecore/content/item1")["field1"].Should().Be("item1-field1-value");
                db.GetItem("/sitecore/content/item1")["field2"].Should().Be("item1-field2-value");
                db.GetItem("/sitecore/content/item2")["field1"].Should().Be("item2-field1-value");
                db.GetItem("/sitecore/content/item2")["field2"].Should().Be("item2-field2-value");
            }
        }

        [Fact]
        public void ShouldCreateItemOfFolderTemplate()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbItem("Sample") {TemplateID = TemplateIDs.Folder}
                })
            {
                // assert
                db.GetItem("/sitecore/content/sample").TemplateID.Should().Be(TemplateIDs.Folder);
            }
        }

        [Fact]
        public void ShouldCreateSampleTemplateIfTemplateIdIsSetButTemplateIsMissing()
        {
            // act
            using (var db = new Db
                {
                    new DbItem("home", ID.NewID, this.templateId)
                })
            {
                // assert
                db.GetItem("/sitecore/content/home").TemplateID.Should().Be(this.templateId);
            }
        }

        [Fact]
        public void ShouldCreateItemWithStatisticsUsingItemManager()
        {
            // arrange
            using (var db = new Db { new DbTemplate("Sample", this.templateId) })
            {
                var root = db.Database.GetItem("/sitecore/content");

                // act
                var item = ItemManager.AddFromTemplate("Home", this.templateId, root, this.itemId);

                // assert
                item[FieldIDs.Created].Should().NotBeEmpty();
                item[FieldIDs.CreatedBy].Should().NotBeEmpty();
                item[FieldIDs.Updated].Should().NotBeEmpty();
                item[FieldIDs.UpdatedBy].Should().NotBeEmpty();
                item[FieldIDs.Revision].Should().NotBeEmpty();
            }
        }

        [Fact]
        public void ShouldCheckIfItemHasChildren()
        {
            // arrange
            using (var db = new Db { new DbItem("Home") })
            {
                // act & assert
                db.GetItem("/sitecore/content").Children.Count.Should().Be(1);
                db.GetItem("/sitecore/content").HasChildren.Should().BeTrue();
            }
        }

        [Fact]
        public void ShouldDeleteItemChildren()
        {
            // arrange
            using (var db = new Db { new DbItem("Home") })
            {
                // act
                db.GetItem("/sitecore/content").DeleteChildren();

                // assert
                db.GetItem("/sitecore/content").Children.Any().Should().BeFalse();
                db.GetItem("/sitecore/content").HasChildren.Should().BeFalse();
            }
        }

        [Fact]
        public void ShouldCreateTemplateIfNoTemplateProvided()
        {
            // arrange
            using (var db = new Db { new DbItem("home") })
            {
                // act
                var item = db.GetItem("/sitecore/content/home");

                // assert
                item.TemplateID.Should().NotBeNull();
                item.Template.Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldCreateTemplateFieldsFromItemFieldsIfNoTemplateProvided()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home") {new DbField("Link") {Type = "General Link"}}
                })
            {
                // act
                var item = db.GetItem("/sitecore/content/home");
                var field = item.Template.GetField("Link");

                // assert
                field.Should().NotBeNull();
                field.Type.Should().Be("General Link");
            }
        }

        [Fact]
        public void ShouldPropagateFieldTypesFromTemplateToItem()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home") {new DbField("Link") {Type = "General Link"}}
                })
            {
                // act
                var item = db.GetItem("/sitecore/content/home");

                // assert
                item.Fields["Link"].Type.Should().Be("General Link");
            }
        }

        [Fact]
        public void ShouldMoveItem()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("old root") {new DbItem("item")},
                    new DbItem("new root")
                })
            {
                var item = db.GetItem("/sitecore/content/old root/item");
                var newRoot = db.GetItem("/sitecore/content/new root");

                // act
                item.MoveTo(newRoot);

                // assert
                db.GetItem("/sitecore/content/new root/item").Should().NotBeNull();
                db.GetItem("/sitecore/content/new root").Children["item"].Should().NotBeNull();
                db.GetItem("/sitecore/content/old root/item").Should().BeNull();
                db.GetItem("/sitecore/content/old root").Children["item"].Should().BeNull();
            }
        }

        [Fact]
        public void ShouldCleanupSettingsOnDispose()
        {
            // arrange
            using (var db = new Db())
            {
                db.Configuration.Settings["Database"] = "core";

                // act & assert
                Settings.GetSetting("Database").Should().Be("core");
            }

            Settings.GetSetting("Database").Should().BeNullOrEmpty();
        }

        [Fact]
        public void ShouldAccessTemplateAsTemplate()
        {
            // arrange
            using (var db = new Db { new DbTemplate("Main", this.templateId) })
            {
                // act & assert
                TemplateManager.GetTemplate(this.templateId, db.Database).ID.Should().Be(this.templateId);
                TemplateManager.GetTemplate("Main", db.Database).ID.Should().Be(this.templateId);
            }
        }

        [Fact]
        public void ShouldAccessTemplateAsItem()
        {
            // arrange
            using (var db = new Db { new DbTemplate("Main", this.templateId) })
            {
                // act
                var item = db.GetItem("/sitecore/templates/Main");

                // assert
                item.Should().NotBeNull();
                item.ID.Should().Be(this.templateId);
            }
        }

        [Fact]
        public void ShouldBeAbleToWorkWithTemplatesInFolders()
        {
            // arrange
            var folderId = ID.NewID;

            using (var db = new Db
                {
                    new DbItem("folder", folderId, TemplateIDs.Folder) {ParentID = ItemIDs.TemplateRoot},
                    new DbTemplate("Main", this.templateId) {ParentID = folderId}
                })
            {
                // act 
                var templateItem = db.GetItem("/sitecore/templates/folder/Main");

                // assert
                TemplateManager.GetTemplate(this.templateId, db.Database).ID.Should().Be(this.templateId);
                TemplateManager.GetTemplate("Main", db.Database).ID.Should().Be(this.templateId);
                templateItem.Should().NotBeNull();
                templateItem.ID.Should().Be(this.templateId);
            }
        }

        [Fact]
        public void TemplateShouldComeBackWithBaseTemplatesDefinedOnTemplateAndItem()
        {
            // arrange
            var baseId = ID.NewID;
            using (var db = new Db
                {
                    new DbTemplate("base", baseId),
                    new DbTemplate("main", this.templateId) {BaseIDs = new[] {baseId}}
                })
            {
                var template = TemplateManager.GetTemplate("main", db.Database);
                var templateItem = db.GetItem(this.templateId);

                // assert
                template.BaseIDs.Should().HaveCount(1);
                template.GetBaseTemplates().Should().HaveCount(2); // current 'base' + standard
                template.GetBaseTemplates().Any(t => t.ID == baseId).Should().BeTrue();

                template.GetField(FieldIDs.BaseTemplate).Should().NotBeNull();
                template.GetField("__Base template").Should().NotBeNull();

                templateItem.Fields[FieldIDs.BaseTemplate].Should().NotBeNull();
                templateItem.Fields[FieldIDs.BaseTemplate].Value.Should().Contain(baseId.ToString());

                templateItem.Fields["__Base template"].Should().NotBeNull();
                templateItem.Fields["__Base template"].Value.Should().Contain(baseId.ToString());
            }
        }

        [Theory]
        [InlineData("CanRead", true, @"au|extranet\John|pe|+item:read|")]
        [InlineData("CanRead", false, @"au|extranet\John|pe|-item:read|")]
        [InlineData("CanWrite", true, @"au|extranet\John|pe|+item:write|")]
        [InlineData("CanWrite", false, @"au|extranet\John|pe|-item:write|")]
        [InlineData("CanRename", true, @"au|extranet\John|pe|+item:rename|")]
        [InlineData("CanRename", false, @"au|extranet\John|pe|-item:rename|")]
        [InlineData("CanCreate", true, @"au|extranet\John|pe|+item:create|")]
        [InlineData("CanCreate", false, @"au|extranet\John|pe|-item:create|")]
        [InlineData("CanDelete", true, @"au|extranet\John|pe|+item:delete|")]
        [InlineData("CanDelete", false, @"au|extranet\John|pe|-item:delete|")]
        [InlineData("CanAdmin", true, @"au|extranet\John|pe|+item:admin|")]
        [InlineData("CanAdmin", false, @"au|extranet\John|pe|-item:admin|")]
        public void ShouldSetItemAccessRules(string propertyName, bool actualPermission, string expectedSecurity)
        {
            // arrange
            var user = User.FromName(@"extranet\John", false);

            using (new UserSwitcher(user))
            using (new SecurityDisabler())
            using (var db = new Db())
            {
                var dbitem = new DbItem("home");
                ReflectionUtil.SetProperty(dbitem.Access, propertyName, actualPermission);

                // act
                db.Add(dbitem);

                var item = db.GetItem("/sitecore/content/home");

                // assert
                item["__Security"].Should().Be(expectedSecurity);
            }
        }

        [Fact]
        public void ShouldAddTemplateToTemplateRecords()
        {
            // arrange & act
            using (var db = new Db { new DbTemplate(this.templateId) })
            {
                // assert
                db.Database.Templates[this.templateId].Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldAddTemplateAndResetTemplatesCache()
        {
            // arrange
            using (var db = new Db())
            {
                // cache the existing templates
                TemplateManager.GetTemplates(db.Database);

                // act
                db.Add(new DbTemplate("My Template", this.templateId));

                // assert
                TemplateManager.GetTemplate(this.templateId, db.Database).Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldGetEmptyFieldValueForInvariantLanguage()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home") {{"Title", "Hello!"}}
                })
            {
                // act
                var item = db.Database.GetItem("/sitecore/content/home", Language.Invariant);

                // assert
                item["Title"].Should().BeEmpty();
            }
        }

        [Fact]
        public void ShouldEnumerateDbItems()
        {
            // arrange
            using (var db = new Db())
            {
                // act
                foreach (var item in db)
                {
                    // assert
                    item.Should().BeAssignableTo<DbItem>();
                }
            }
        }

        [Fact]
        public void ShouldThrowIfNoParentFoundById()
        {
            // arrange
            const string ParentId = "{483AE2C1-3494-4248-B591-030F2E2C9843}";

            using (var db = new Db())
            {
                var homessItem = new DbItem("homeless") { ParentID = new ID(ParentId) };

                // act
                Action action = () => db.Add(homessItem);

                // assert
                action.ShouldThrow<ItemNotFoundException>()
                    .WithMessage("The parent item \"{483AE2C1-3494-4248-B591-030F2E2C9843}\" was not found.");
            }
        }

        [Fact]
        public void ShouldGetBaseTemplates()
        {
            // arrange
            using (var db = new Db { new DbItem("home") })
            {
                var item = db.GetItem("/sitecore/content/home");

                // act
                var baseTemplates = item.Template.BaseTemplates;

                // assert
                baseTemplates.Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldGetItemChildByPathIfAddedUsingChildrenCollection()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home") {Children = {new DbItem("sub-item")}}
                })
            {
                // act
                var item = db.GetItem("/sitecore/content/home/sub-item");

                // assert
                item.Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldCreateItemBasedOnTemplateField()
        {
            using (var db = new Db
                {
                    new DbItem("TestField", this.itemId, TemplateIDs.TemplateField)
                })
            {
                db.GetItem(this.itemId).Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldSwitchDataStorage()
        {
            // act
            using (var db = new Db())
            {
                // assert
                DataStorageSwitcher.CurrentValue(db.Database.Name).Should().BeSameAs(db.DataStorage);
            }
        }

        [Fact]
        public void ShouldDisposeDataStorageSwitcher()
        {
            // arrange
            DataStorage dataStorage;

            // act
            using (var db = new Db())
            {
                dataStorage = db.DataStorage;
            }

            // assert
            Switcher<DataStorage>.CurrentValue.Should().NotBeSameAs(dataStorage);
        }

        [Fact]
        public void ShouldSupportNestedDatabases()
        {
            // arrange
            using (var db = new Db { new DbItem("home") })
            {
                // act
                using (new Db())
                {
                }

                // assert
                db.GetItem("/sitecore/content/home").Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldRecycleItem()
        {
            using (var db = new Db { new DbItem("home") })
            {
                db.GetItem("/sitecore/content/home").Recycle();
            }
        }

        [Fact]
        public void ShouldChangeTemplate()
        {
            // arrange
            var newTemplateId = ID.NewID;

            using (var db = new Db
                {
                    new DbItem("home", this.itemId, this.templateId),
                    new DbTemplate("new template", newTemplateId)
                })
            {
                var item = db.GetItem(this.itemId);
                var newTemplate = db.GetItem(newTemplateId);

                // act
                item.ChangeTemplate(newTemplate);

                // assert
                item.TemplateID.Should().Be(newTemplate.ID);
            }
        }

        [Fact]
        public void ShouldSupportMuiltipleParallelDatabases()
        {
            // arrange
            using (var core = new Db("core") { new DbItem("core") })
            using (var master = new Db("master") { new DbItem("master") })
            {
                // act
                var coreHome = core.GetItem("/sitecore/content/core");
                var masterHome = master.GetItem("/sitecore/content/master");

                // assert
                coreHome.Should().NotBeNull(); // <-- Fails here
                masterHome.Should().NotBeNull();
            }
        }

        [Fact]
        public void ShouldNotBeAffectedByMockedHttpContext()
        {
            // arrange
            using (var db = new Db { new DbItem("home") })
            {
                var request = new HttpRequest(string.Empty, "http://mysite", string.Empty);
                var response = new HttpResponse(new StringWriter());

                HttpContext.Current = new HttpContext(request, response);
                try
                {
                    // act
                    var page = db.GetItem("/sitecore/content/home"); // <-- Fails here

                    // assert
                    page.Should().NotBeNull();
                }
                finally
                {
                    HttpContext.Current = null;
                }
            }
        }

        [Fact]
        public void ShouldCreateUnversionedField()
        {
            using (var db = new Db
                {
                    new DbItem("home")
                        {
                            new UnversionedDbField("Field")
                                {
                                    {"en", 1, "en value initial"},
                                    {"en", 2, "en value expected"},
                                    {"da", 1, "da value initial"},
                                    {"da", 2, "da value expected"}
                                }
                        }
                })
            {
                db.GetItem("/sitecore/content/home", "en", 1)["Field"].Should().Be("en value expected");
                db.GetItem("/sitecore/content/home", "en", 2)["Field"].Should().Be("en value expected");
                db.GetItem("/sitecore/content/home", "da", 1)["Field"].Should().Be("da value expected");
                db.GetItem("/sitecore/content/home", "da", 2)["Field"].Should().Be("da value expected");
            }
        }

        [Fact]
        public void ShouldAddVersionsToUnversionedField()
        {
            using (var db = new Db
                {
                    new DbItem("home")
                        {
                            new UnversionedDbField("Field")
                                {
                                    {"en", 1, "en value initial"},
                                    {"en", 2, "en value expected"},
                                }
                        }
                })
            {
                db.GetItem("/sitecore/content/home").Versions.Count.Should().Be(2);
            }
        }

        [Fact]
        public void ShouldHaveOneVersionInDefaultLanguage()
        {
            using (var db = new Db { new DbItem("home") })
            {
                db.GetItem("/sitecore/content/home").Versions.Count.Should().Be(1);
            }
        }

        [Fact]
        public void ShouldHaveNoVersionsInCustomLanguage()
        {
            using (var db = new Db { new DbItem("home") })
            {
                db.GetItem("/sitecore/content/home", "de").Versions.Count.Should().Be(0);
            }
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        public void ShouldAlwaysReturnFirstVersionEvenIfNoVersionExists(string language)
        {
            using (var db = new Db { new DbItem("home") })
            {
                db.GetItem("/sitecore/content/home", language).Version.Should().Be(Version.First);
            }
        }

        [Fact]
        public void ShouldAddVersionForCustomLanguage()
        {
            using (var db = new Db { new DbItem("home") })
            {
                var homeDe = db.GetItem("/sitecore/content/home", "de");

                homeDe.Versions.AddVersion();

                homeDe.Versions.Count.Should().Be(1);
            }
        }

        [Fact]
        public void ShouldCloneVersionedItem()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("source")
                        {
                            new DbField("Title") {{"en", "Hello!"}, {"de", "Servus!"}}
                        }
                })
            {
                var source = db.GetItem("/sitecore/content/source");

                // act
                var clone = source.CloneTo(source.Parent, "clone", false);

                // assert
                db.GetItem(clone.ID, "en")["Title"].Should().Be("Hello!");
                db.GetItem(clone.ID, "de")["Title"].Should().Be("Servus!");
            }
        }

        [Fact]
        public void ShouldCreateVersionAutomaticallyOnEditing()
        {
            // arrange
            using (var db = new Db
                {
                    new DbItem("home") {new DbField("Title")}
                })
            {
                var homeDe = db.GetItem("/sitecore/content/home", "de");
                homeDe.Versions.Count.Should().Be(0);

                // act
                using (new EditContext(homeDe))
                {
                    homeDe.Fields["Title"].SetValue("Servus!", true);
                }

                // assert
                homeDe.Versions.Count.Should().Be(1);
                homeDe["Title"].Should().Be("Servus!");
            }
        }

        [Fact]
        public void ShouldSetItemWorkflow()
        {
            // arrange & act
            using (var db = new Db
                {
                    new DbTemplate(this.templateId) {{FieldIDs.DefaultWorkflow, "{E02A54E4-1037-4569-A735-F582B8ABA8A4}"}},
                    new DbItem("home", ID.NewID, this.templateId)
                })
            {
                var home = db.GetItem("/sitecore/content/home");

                // assert
                Assert.Equal("{E02A54E4-1037-4569-A735-F582B8ABA8A4}", home[FieldIDs.DefaultWorkflow]);
                Assert.Equal("{E02A54E4-1037-4569-A735-F582B8ABA8A4}", home[FieldIDs.Workflow]);
            }
        }

        [Fact]
        public void ShouldCreateTemplateFieldWithStandardValues()
        {
            // arrange & act
            const string checkboxStandardValue = "1";
            using (var db = new Db
                {
                    new DbTemplate("Page", this.templateId)
                        {
                            {new DbField("Hide") {Type = "checkbox"}, checkboxStandardValue}
                        },
                    new DbItem("home", ID.NewID, this.templateId)
                })
            {
                var home = db.GetItem("/sitecore/content/home");

                // assert
                Assert.True(new CheckboxField(home.Fields["Hide"]).Checked);
            }
        }

        [Fact]
        public void ShouldUpdateBranchId()
        {
            var branchId = ID.NewID;
            using (var db = new Db { new DbItem("home") })
            {
                var targetItem = db.GetItem("/sitecore/content/home");
                using (new EditContext(targetItem))
                {
                    targetItem.BranchId = branchId;
                }

                targetItem.BranchId.Should().Be(branchId);
            }
        }

        [Fact]
        public void ShouldGetVersionedItemRevision()
        {
            using (var db = new Db
                {
                    new DbItem("Home")
                        {
                            new DbField("Value") {{"af-ZA", 1, "test"}}
                        }
                })
            {
                var item = db.GetItem("/sitecore/content/home", "af-ZA");
                item["__Revision"].Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void ShouldCreateDbWithEnLanguageByDefault()
        {
            using (var db = new Db())
            {
                db.Database.Languages
                    .ShouldAllBeEquivalentTo(
                        new[] { Language.Parse("en") });
            }
        }

        [Theory]
        [InlineData("core")]
        [InlineData("master")]
        [InlineData("web")]
        public void ShouldCreateDbWithSpecificLanguages(string database)
        {
            using (var db = new Db(database)
                .WithLanguages(Language.Parse("en"), Language.Parse("da")))
            {
                db.Database.Languages
                    .ShouldAllBeEquivalentTo(
                        new[] { Language.Parse("en"), Language.Parse("da") });
            }
        }

        [Fact]
        public void ShouldNotFailIfLanguageSwitcherDisposedElsewhere()
        {
            using (new Db()
                .WithLanguages(Language.Parse("da")))
            {
                Switcher<DbLanguages>.Exit();
            }
        }

        [Fact]
        public void ShouldAddWildcard()
        {
            using (var db = new Db())
            {
                var item = db.GetItem("/sitecore/content/");
                item.Add("*", new TemplateID(TemplateIDs.Folder));
            }
        }
    }
}