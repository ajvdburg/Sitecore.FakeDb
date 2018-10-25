namespace Sitecore.FakeDb.Tests.Data.Engines.DataCommands.Prototypes
{
    using FluentAssertions;
    using Sitecore.FakeDb.Data.Engines;
    using Sitecore.FakeDb.Data.Engines.DataCommands;
    using Sitecore.FakeDb.Data.Engines.DataCommands.Prototypes;
    using Sitecore.Reflection;
    using Xunit;

    public class GetVersionsCommandPrototypeTest
    {
        [Theory, DefaultAutoData]
        public void ShouldCreateInstance(
            GetVersionsCommandPrototype sut,
            DataStorage dataStorage)
        {
            using (new DataStorageSwitcher(dataStorage))
            {
                ReflectionUtil.CallMethod(sut, "CreateInstance").Should().BeOfType<GetVersionsCommand>();
            }
        }
    }
}