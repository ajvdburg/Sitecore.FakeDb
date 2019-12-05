﻿namespace Sitecore.FakeDb.AutoFixture.Tests
{
    using System;
    using FluentAssertions;
    using global::AutoFixture;
    using global::AutoFixture.Kernel;
    using global::AutoFixture.Xunit2;
    using Xunit;

    public class AddContentDbItemCommandTest
    {
        [Theory, AutoData]
        public void ExecuteThrowsIfSpecimenIsNull(AddContentDbItemCommand sut)
        {
            Action action = () => sut.Execute(null, null);
            action.ShouldThrow<ArgumentNullException>().WithMessage("*specimen");
        }

        [Theory, AutoData]
        public void ExecuteThrowsIfContextIsNull(AddContentDbItemCommand sut, object specimen)
        {
            Action action = () => sut.Execute(specimen, null);
            action.ShouldThrow<ArgumentNullException>().WithMessage("*context");
        }

        [Theory, AutoData]
        public void ExecuteIgnoresNotDbItemSpecimens(AddContentDbItemCommand sut, object specimen, SpecimenContext context)
        {
            Action action = () => sut.Execute(specimen, context);
            action.ShouldNotThrow();
        }

        [Fact, Trait("Category", "RequireLicense")]
        public void CreateAddsItemToDb()
        {
            var fixture = new Fixture();
            var db = fixture.Freeze<Db>();
            var item = fixture.Build<DbItem>().Without(x => x.ParentID).Create();
            var sut = new AddContentDbItemCommand();

            sut.Execute(item, new SpecimenContext(fixture));

            db.GetItem(item.ID).Should().NotBeNull();
        }
    }
}