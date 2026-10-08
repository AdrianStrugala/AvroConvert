using System;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertUnitTests
{
    public class LicenseTests
    {
        [Fact]
        public void Commercial_RequiresLicensee()
        {
            Assert.Throws<ArgumentException>(() => AvroLicense.Commercial(" "));
            Assert.Equal("Acme Ltd.", AvroLicense.Commercial(" Acme Ltd. ").Licensee);
        }

        [Theory]
        [InlineData("NonCommercial", AvroLicenseKind.NonCommercial, null)]
        [InlineData("smallbusiness", AvroLicenseKind.SmallBusiness, null)]
        [InlineData("Commercial:Acme Ltd.", AvroLicenseKind.Commercial, "Acme Ltd.")]
        public void TryParse_EnvironmentFormat(string value, AvroLicenseKind kind, string licensee)
        {
            var license = AvroLicense.TryParse(value);

            Assert.NotNull(license);
            Assert.Equal(kind, license.Kind);
            Assert.Equal(licensee, license.Licensee);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Commercial")]
        [InlineData("Enterprise")]
        public void TryParse_InvalidValues_ReturnNull(string value)
        {
            Assert.Null(AvroLicense.TryParse(value));
        }

        [Fact]
        public void Declaration_DoesNotAffectSerialization()
        {
            var previous = AvroConvert.License;
            try
            {
                AvroConvert.License = AvroLicense.Commercial("Acme Ltd.");
                var bytes = AvroConvert.Serialize(new { Name = "x", Value = 1 });
                Assert.NotEmpty(bytes);
                Assert.Equal("Commercial (Acme Ltd.)", AvroConvert.License.ToString());
            }
            finally
            {
                AvroConvert.License = previous;
            }
        }
    }
}
