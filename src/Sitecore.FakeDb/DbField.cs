﻿namespace Sitecore.FakeDb
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;

    /// <summary>
    /// Represents a lightweight version of the <see cref="Field"/> class.
    /// If the field name or id match one of the standard fields, additional field properties 
    /// such as Shared or Type are filled automatically. 
    /// </summary>
    [DebuggerDisplay("ID = {ID}, Name = {Name}, Value = {Value}")]
    public class DbField : IEnumerable
    {
        private static readonly StandardFieldsReference StandardFields = new StandardFieldsReference();

        private readonly IDictionary<string, IDictionary<int, string>> values = new Dictionary<string, IDictionary<int, string>>();

        public DbField(ID id)
            : this(Builder.FromId().Build(id))
        {
        }

        public DbField(string name)
            : this(Builder.FromName().Build(name))
        {
        }

        public DbField(string name, ID id)
            : this(Builder.FromNameAndId().Build(new object[] {name, id}))
        {
        }

        protected DbField(FieldInfo fieldInfo)
        {
            this.ID = fieldInfo.Id;
            this.Name = fieldInfo.Name;
            this.Shared = fieldInfo.Shared;
            this.Type = fieldInfo.Type;
        }

        public ID ID { get; internal set; }

        public string Name { get; set; }

        public bool Shared { get; set; }

        public string Type { get; set; }

        public string Source { get; set; }

        public string Value
        {
            get { return this.GetValue(Language.Current.Name, Sitecore.Data.Version.Latest.Number); }
            set { this.SetValue(Language.Current.Name, value); }
        }

        internal IDictionary<string, IDictionary<int, string>> Values
        {
            get { return this.values; }
        }

        public virtual void Add(string language, string value)
        {
            var version = this.GetLatestVersion(language) + 1;

            this.Add(language, version, value);
        }

        public virtual void Add(string language, int version, string value)
        {
            Assert.ArgumentNotNull(language, "language");

            if (version <= 0)
            {
                throw new ArgumentOutOfRangeException("version", "Version cannot be zero or negative.");
            }

            if (this.values.ContainsKey(language) && this.values[language].ContainsKey(version))
            {
                throw new ArgumentException("An item with the same version has already been added.");
            }

            if (this.values.ContainsKey(language))
            {
                this.values[language].Add(version, value);
            }
            else
            {
                this.values[language] = new SortedDictionary<int, string> {{version, value}};
            }

            if (this.Shared)
            {
                foreach (var langVersions in this.values.Select(l => l.Value))
                {
                    for (var i = langVersions.Count; i > 0; --i)
                    {
                        langVersions[i] = value;
                    }
                }
            }

            this.WireUpDefaultFieldValues(language, version);
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable) this.values).GetEnumerator();
        }

        public virtual string GetValue(string language, int version)
        {
            Assert.ArgumentNotNull(language, "language");

            if (version == 0)
            {
                version = this.GetLatestVersion(language);
            }

            if (this.Shared)
            {
                foreach (var lv in this.Values.SelectMany(l => l.Value))
                {
                    return lv.Value;
                }
            }

            var hasValueForLanguage = this.values.ContainsKey(language);
            if (!hasValueForLanguage)
            {
                return string.Empty;
            }

            var langValues = this.values[language];
            var hasValueForVersion = langValues.ContainsKey(version);
            if (!hasValueForVersion)
            {
                return string.Empty;
            }

            return langValues[version];
        }

        public virtual void SetValue(string language, string value)
        {
            Assert.ArgumentNotNull(language, "language");

            var latestVersion = this.GetLatestVersion(language);
            if (latestVersion == 0)
            {
                latestVersion = 1;
            }

            this.SetValue(language, latestVersion, value);
        }

        public virtual void SetValue(string language, int version, string value)
        {
            if (!this.values.ContainsKey(language))
            {
                this.Add(language, version, value);
            }

            this.values[language][version] = value;
        }

        public bool IsStandard()
        {
            return StandardFields[this.Name] != FieldInfo.Empty;
        }

        private int GetLatestVersion(string language)
        {
            Assert.ArgumentNotNull(language, "language");

            if (!this.values.ContainsKey(language))
            {
                return 0;
            }

            var langValues = this.values[language];

            return langValues.Any() ? langValues.Last().Key : 0;
        }

        private void WireUpDefaultFieldValues(string language, int version)
        {
            for (var i = version - 1; i > 0; --i)
            {
                if (this.values[language].ContainsKey(i))
                {
                    break;
                }

                this.values[language].Add(i, string.Empty);
            }
        }

        private static class Builder
        {
            private static readonly StandardFieldsReference FieldReference = new StandardFieldsReference();

            public static IDbFieldBuilder FromId()
            {
                return new CompositeFieldBuilder(
                    new IdBasedStandardFieldResolver(FieldReference),
                    new IdBasedFieldGenerator());
            }

            public static IDbFieldBuilder FromName()
            {
                return new CompositeFieldBuilder(
                    new NameBasedStandardFieldResolver(FieldReference),
                    new NameBasedFieldGenerator());
            }

            public static IDbFieldBuilder FromNameAndId()
            {
                return new IdNameFieldBuilder(FromName(), FromId());
            }
        }
    }
}