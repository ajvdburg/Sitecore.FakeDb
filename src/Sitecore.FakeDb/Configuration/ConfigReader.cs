﻿namespace Sitecore.FakeDb.Configuration
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using Sitecore.Data;
    using Sitecore.Data.Events;
    using Sitecore.Diagnostics;
    using Sitecore.FakeDb.Data.Engines.DataCommands.Prototypes;
    using Sitecore.IO;
    using Sitecore.Xml;
    using Sitecore.Xml.Patch;

    public class ConfigReader : IConfigurationSectionHandler
    {
        static ConfigReader()
        {
            SetAppDomainAppPath();
            Database.InstanceCreated += DatabaseInstanceCreated;
        }

        private static void DatabaseInstanceCreated(object sender, InstanceCreatedEventArgs e)
        {
            SetDataEngineCommands(e);
        }

        private static void SetDataEngineCommands(InstanceCreatedEventArgs e)
        {
            var commands = e.Database.Engines.DataEngine.Commands;
            commands.GetItemPrototype = new GetItemCommandPrototype(e.Database);
            commands.GetVersionsPrototype = new GetVersionsCommandPrototype(e.Database);
        }

        private static void SetAppDomainAppPath()
        {
            var directoryName = Path.GetDirectoryName(FileUtil.GetFilePathFromFileUri(Assembly.GetExecutingAssembly().CodeBase));
            Assert.IsNotNull(directoryName, "Unable to set the 'HttpRuntime.AppDomainAppPath' property.");

            while ((directoryName.Length > 0) && (directoryName.IndexOf('\\') >= 0))
            {
                if (directoryName.EndsWith(@"\bin", StringComparison.InvariantCulture))
                {
                    directoryName = directoryName.Substring(0, directoryName.LastIndexOf('\\'));
                    break;
                }

                directoryName = directoryName.Substring(0, directoryName.LastIndexOf('\\'));
            }

            Sitecore.Configuration.State.HttpRuntime.AppDomainAppPath = directoryName;
        }

        public object Create(object parent, object configContext, XmlNode section)
        {
            using (var stream = typeof(Db).Assembly.GetManifestResourceStream("Sitecore.FakeDb.Sitecore.config"))
            using (var reader = new StreamReader(stream))
            {
                var main = XmlUtil.GetXmlNode(reader.ReadToEnd()).SelectSingleNode("sitecore");
                MergeConfigs(main, section);
                var configReader = (IConfigurationSectionHandler) new Sitecore.Configuration.ConfigReader();
                return configReader.Create(parent, configContext, main);
            }
        }

        private static void MergeConfigs(XmlNode main, XmlNode section)
        {
            SetZeroConfigurationPropertyIfExists(true);
            new XmlPatcher("s", "p").Merge(main, section);
            SetZeroConfigurationPropertyIfExists(false);
        }

        /// <summary>
        /// In Sitecore 8.2 Update-1 config merging logic has been changed. 
        /// Without setting property 'Sitecore.Configuration.ConfigReader.ZeroConfiguration'
        /// method 'Sitecore.Xml.Patch.ElementIdentification.ReadSignificantAttributesFromConfiguration()'
        /// causes StackOverflowException.
        /// </summary>
        private static void SetZeroConfigurationPropertyIfExists(bool value)
        {
            var propertyInfo = typeof(Sitecore.Configuration.ConfigReader)
                .GetProperty("ZeroConfiguration", BindingFlags.Static | BindingFlags.Public);
            if (propertyInfo == null)
            {
                return;
            }

            propertyInfo.SetValue(null, value);
        }
    }
}