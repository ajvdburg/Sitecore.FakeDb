﻿namespace Sitecore.FakeDb.Tests.Data.Engines
{
  using System;
  using System.IO;
  using System.Text;
  using FluentAssertions;
  using global::AutoFixture.Xunit2;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.FakeDb.Data.Engines;
  using Sitecore.Globalization;
  using Xunit;

  public class DataStorageTest
  {
    private readonly DataStorage dataStorage;

    private const string ItemIdsRootId = "{11111111-1111-1111-1111-111111111111}";

    private const string ItemIdsContentRoot = "{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}";

    private const string ItemIdsTemplateRoot = "{3C1715FE-6A13-4FCF-845F-DE308BA9741D}";

    private const string ItemIdsBranchesRoot = "{BAD98E0E-C1B5-4598-AC13-21B06218B30C}";

    private const string ItemIdsSystemRoot = "{13D6D6C6-C50B-4BBD-B331-2B04F1A58F21}";

    private const string ItemIdsMediaLibraryRoot = "{3D6658D8-A0BF-4E75-B3E2-D050FABCF4E1}";

    private const string TemplateIdsTemplate = "{AB86861A-6030-46C5-B394-E8F99E8B87DB}";

    private const string ItemIdsTemplateSection = "{E269FBB5-3750-427A-9149-7AA950B49301}";

    private const string ItemIdsTemplateField = "{455A3E98-A627-4B40-8035-E683A0331AC7}";

    private const string TemplateIdsBranch = "{35E75C72-4985-4E09-88C3-0EAC6CD1E64F}";

    private const string RootParentId = "{00000000-0000-0000-0000-000000000000}";

    private const string TemplateIdSitecore = "{C6576836-910C-4A3D-BA03-C277DBD3B827}";

    public const string TemplateIdMainSection = "{E3E2D58C-DF95-4230-ADC9-279924CECE84}";

    public const string TemplateIdBranchFolder = "{85ADBF5B-E836-4932-A333-FE0F9FA1ED1E}";

    public DataStorageTest()
    {
      this.dataStorage = new DataStorage(Database.GetDatabase("master"));
    }

    [Theory]
    [InlineData(ItemIdsRootId, "sitecore", TemplateIdSitecore, RootParentId, "/sitecore")]
    [InlineData(ItemIdsContentRoot, "content", TemplateIdMainSection, ItemIdsRootId, "/sitecore/content")]
    [InlineData(ItemIdsTemplateRoot, "templates", TemplateIdMainSection, ItemIdsRootId, "/sitecore/templates")]
    [InlineData(ItemIdsBranchesRoot, "Branches", TemplateIdBranchFolder, ItemIdsTemplateRoot, "/sitecore/templates/Branches")]
    [InlineData(ItemIdsSystemRoot, "system", TemplateIdMainSection, ItemIdsRootId, "/sitecore/system")]
    [InlineData(ItemIdsMediaLibraryRoot, "media library", TemplateIdMainSection, ItemIdsRootId, "/sitecore/media library")]
    [InlineData(TemplateIdsTemplate, "Template", TemplateIdsTemplate, ItemIdsTemplateRoot, "/sitecore/templates/template")]
    [InlineData(ItemIdsTemplateSection, "Template section", ItemIdsTemplateSection, ItemIdsTemplateRoot, "/sitecore/templates/template section")]
    [InlineData(ItemIdsTemplateField, "Template field", ItemIdsTemplateField, ItemIdsTemplateRoot, "/sitecore/templates/template field")]
    [InlineData(TemplateIdsBranch, "Branch", TemplateIdsTemplate, ItemIdsTemplateRoot, "/sitecore/templates/branch")]
    public void ShouldInitializeDefaultFakeItems(string itemId, string itemName, string templateId, string parentId, string fullPath)
    {
      // assert
      this.dataStorage.GetFakeItem(ID.Parse(itemId)).ID.ToString().Should().Be(itemId);
      this.dataStorage.GetFakeItem(ID.Parse(itemId)).Name.Should().Be(itemName);
      this.dataStorage.GetFakeItem(ID.Parse(itemId)).TemplateID.ToString().Should().Be(templateId);
      this.dataStorage.GetFakeItem(ID.Parse(itemId)).ParentID.ToString().Should().Be(parentId);
      this.dataStorage.GetFakeItem(ID.Parse(itemId)).FullPath.Should().Be(fullPath);
    }

    [Fact]
    public void ShouldCreateDefaultFakeTemplate()
    {
      this.dataStorage.GetFakeItem(new TemplateID(new ID(TemplateIdSitecore))).Should().BeEquivalentTo(new DbTemplate("Main Section", new TemplateID(new ID(TemplateIdSitecore))));
      this.dataStorage.GetFakeItem(new TemplateID(new ID(TemplateIdMainSection))).Should().BeEquivalentTo(new DbTemplate("Main Section", new TemplateID(new ID(TemplateIdMainSection))));

      this.dataStorage.GetFakeItem(TemplateIDs.Template).Should().BeEquivalentTo(new DbTemplate("Template", TemplateIDs.Template));
      this.dataStorage.GetFakeItem(TemplateIDs.Folder).Should().BeEquivalentTo(new DbTemplate("Folder", TemplateIDs.Folder));
    }

    [Fact]
    public void ShouldGetExistingItem()
    {
      // act & assert
      this.dataStorage.GetFakeItem(ItemIDs.ContentRoot).Should().NotBeNull();
      this.dataStorage.GetFakeItem(ItemIDs.ContentRoot).Should().BeOfType<DbItem>();

      this.dataStorage.GetSitecoreItem(ItemIDs.ContentRoot, Language.Current).Should().NotBeNull();
      this.dataStorage.GetSitecoreItem(ItemIDs.ContentRoot, Language.Current).Should().BeAssignableTo<Item>();
    }

    [Fact]
    public void ShouldGetNullIdIfNoItemPresent()
    {
      // act & assert
      this.dataStorage.GetFakeItem(ID.NewID).Should().BeNull();
      this.dataStorage.GetSitecoreItem(ID.NewID, Language.Current).Should().BeNull();
    }

    [Fact]
    public void ShouldGetSitecoreItemFieldIdsFromTemplateAndValuesFromItems()
    {
      // arrange
      var itemId = ID.NewID;
      var templateId = ID.NewID;
      var fieldId = ID.NewID;

      this.dataStorage.AddFakeItem(new DbTemplate("Sample", templateId) { Fields = { new DbField("Title", fieldId) } });
      this.dataStorage.AddFakeItem(new DbItem("Sample", itemId, templateId) { Fields = { new DbField("Title", fieldId) { Value = "Welcome!" } } });

      // act
      var item = this.dataStorage.GetSitecoreItem(itemId, Language.Current);

      // assert
      item[fieldId].Should().Be("Welcome!");
    }

    [Fact]
    public void ShouldGetSitecoreItemWithEmptyFieldIfNoItemFieldFound()
    {
      // arrange
      var itemId = ID.NewID;
      var templateId = ID.NewID;
      var fieldId = ID.NewID;

      this.dataStorage.AddFakeItem(new DbTemplate("Sample", templateId) { Fields = { new DbField("Title", fieldId) } });
      this.dataStorage.AddFakeItem(new DbItem("Sample", itemId, templateId));

      // act
      var item = this.dataStorage.GetSitecoreItem(itemId, Language.Current);

      // assert
      item.InnerData.Fields[fieldId].Should().BeNull();

      // We have changed the way we create ItemData to give more control to Sitecore
      // and in order for the default string.Empty to come back from Field.Value
      // Sitecore needs to be able to make a trip up the templates path 
      // and it in turn requires the Db context

      // item[fieldId].Should().BeEmpty();
    }

    [Fact]
    public void ShouldSetSecurityFieldForRootItem()
    {
      // assert
      this.dataStorage.GetFakeItem(ItemIDs.RootID).Fields[FieldIDs.Security].Value.Should().Be("ar|Everyone|p*|+*|");
    }

    [Theory]
    [InlineData("core", true)]
    [InlineData("master", false)]
    [InlineData("web", false)]
    [Trait("Category", "RequireLicense")]
    public void ShouldCreateFieldTypesRootInCoreDatabase(string database, bool exists)
    {
      // arrange
      using (var db = new Db(database))
      {
        // act
        var result = db.GetItem("/sitecore/system/Field Types") != null;

        // assert
        result.Should().Be(exists);
      }
    }

    [Theory, AutoData]
    public void GetBlobStreamReturnsNullIfNoStreamFound(Guid someBlobId)
    {
      this.dataStorage.GetBlobStream(someBlobId).Should().BeNull();
    }

    [Theory, AutoData]
    public void GetBlobStreamReturnsOpenStreamCopy(
      Guid blobId,
      [NoAutoProperties] MemoryStream stream)
    {
      this.dataStorage.SetBlobStream(blobId, stream);

      var copy1 = this.dataStorage.GetBlobStream(blobId);
      copy1.Close();

      var copy2 = this.dataStorage.GetBlobStream(blobId);
      copy2.CanRead.Should().BeTrue();
    }

    [Theory, AutoData]
    public void SetBlobStreamOverridesExistingStream(
      Guid blobId,
      [NoAutoProperties] MemoryStream existing,
      [NoAutoProperties] MemoryStream @new)
    {
      this.dataStorage.SetBlobStream(blobId, existing);
      this.dataStorage.SetBlobStream(blobId, @new);

      var actual = (MemoryStream)this.dataStorage.GetBlobStream(blobId);

      actual.ToArray().Should().BeEquivalentTo(@new.ToArray());
    }

    [Theory, AutoData]
    public void SetBlobStreamPositionAndLengthNotChanged(
        Guid blobId,
        [NoAutoProperties] MemoryStream @new,
        string streamData)
    {
      var bytes = Encoding.UTF8.GetBytes(streamData);
      @new.Write(bytes, 0, bytes.Length);
      var position = bytes.Length / 2;
      var length = bytes.Length;

      @new.Seek(position, SeekOrigin.Begin);

      this.dataStorage.SetBlobStream(blobId, @new);

      @new.Length.ShouldBeEquivalentTo(length);
      @new.Position.ShouldBeEquivalentTo(position);
    }


    [Theory, AutoData]
    public void SetBlobStreamFollowedByGetBlobStreamReturnStreamAtPosition0(
        Guid blobId,
        [NoAutoProperties] MemoryStream @new,
        string streamData)
    {
      var bytes = Encoding.UTF8.GetBytes(streamData);
      @new.Write(bytes, 0, bytes.Length);

      this.dataStorage.SetBlobStream(blobId, @new);

      var copy1 = this.dataStorage.GetBlobStream(blobId);

      copy1.Position.ShouldBeEquivalentTo(0);
      copy1.Length.ShouldBeEquivalentTo(bytes.Length);
      copy1.Should().NotBe(@new);
    }

    [Theory, AutoData]
    public void SetBlobStreamThrowsIfStreamIsNull(Guid blobId)
    {
      Assert.Throws<ArgumentNullException>(() => this.dataStorage.SetBlobStream(blobId, null));
    }

    [Theory, AutoData]
    public void RemoveFakeItemReturnsFalseIfNoItemFound(DbItem item)
    {
      this.dataStorage.RemoveFakeItem(item.ID).Should().BeFalse();
    }

    [Theory, AutoData]
    public void RemoveFakeItemReturnsTrueIfRemoved([NoAutoProperties] DbItem item)
    {
      this.dataStorage.AddFakeItem(item);
      this.dataStorage.RemoveFakeItem(item.ID).Should().BeTrue();
      this.dataStorage.GetFakeItem(item.ID).Should().BeNull();
    }

    [Theory, AutoData]
    public void RemoveFakeItemRemovesDescendants(
      [NoAutoProperties] DbItem item,
      [NoAutoProperties] DbItem child1,
      [NoAutoProperties] DbItem grandChild1,
      [NoAutoProperties] DbItem child2)
    {
      item.Children.Add(child1);
      item.Children.Add(grandChild1);
      item.Children.Add(child2);
      this.dataStorage.AddFakeItem(item);

      this.dataStorage.RemoveFakeItem(item.ID).Should().BeTrue();

      this.dataStorage.GetFakeItem(child1.ID).Should().BeNull();
      this.dataStorage.GetFakeItem(grandChild1.ID).Should().BeNull();
      this.dataStorage.GetFakeItem(child2.ID).Should().BeNull();
    }
  }
}