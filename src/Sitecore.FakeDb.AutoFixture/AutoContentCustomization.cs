﻿namespace Sitecore.FakeDb.AutoFixture
{
  using global::AutoFixture;
  using Sitecore.Data;
  using Sitecore.Diagnostics;

  /// <summary>
  /// A customization that adds items to the current <see cref="Database"/>.
  /// The default <see cref="Database"/> is "master".
  /// </summary>
  public class AutoContentCustomization : ICustomization
  {
    /// <summary>
    /// Customizes the specified fixture by adding items to the current <see cref="Database"/>.
    /// </summary>
    /// <param name="fixture">The fixture to customize.</param>
    public void Customize(IFixture fixture)
    {
      Assert.ArgumentNotNull(fixture, "fixture");

      new CompositeCustomization(
        new AutoContentItemCustomization(),
        new AutoContentDbItemCustomization(),
        new AutoContentTemplateItemCustomization())
        .Customize(fixture);
    }
  }
}