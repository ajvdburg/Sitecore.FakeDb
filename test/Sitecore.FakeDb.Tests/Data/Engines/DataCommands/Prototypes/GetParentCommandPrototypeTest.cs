namespace Sitecore.FakeDb.Tests.Data.Engines.DataCommands.Prototypes
{
    using System;
    using FluentAssertions;
    using Sitecore.FakeDb.Data.Engines;
    using Sitecore.FakeDb.Data.Engines.DataCommands;
    using Sitecore.FakeDb.Data.Engines.DataCommands.Prototypes;
    using Sitecore.Reflection;
    using Xunit;

    [Obsolete]
    public class GetParentCommandPrototypeTest
    {
        [Theory, DefaultAutoData]
        public void ShouldCreateInstance(
            GetParentCommandPrototype sut,
            DataStorage dataStorage)
        {
            using (new DataStorageSwitcher(dataStorage))
            {
                ReflectionUtil.CallMethod(sut, "CreateInstance").Should().BeOfType<GetParentCommand>();
            }
        }
    }
}