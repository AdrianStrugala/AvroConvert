using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertUnitTests
{
    public class LicenseTests
    {
        private static readonly ECDsa SigningKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        private static readonly byte[] PublicKey = SigningKey.ExportSubjectPublicKeyInfo();

        private static string IssueKey(string payloadJson, ECDsa signer = null)
        {
            var signedPart = "AVC1." + Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payloadJson));
            var signature = (signer ?? SigningKey).SignData(Encoding.ASCII.GetBytes(signedPart), HashAlgorithmName.SHA256);
            return signedPart + "." + Base64Url.EncodeToString(signature);
        }

        private const string Payload = """{"id":"AVC-2026-7K3M9Q","licensee":"Acme Ltd.","product":"avroconvert","plan":"business","issued":"2026-10-08","expires":"2027-10-08"}""";

        [Fact]
        public void Parse_ValidKey_ExposesPayloadAndVerifiesSignature()
        {
            var license = AvroLicense.Parse(IssueKey(Payload), PublicKey);

            Assert.Equal(AvroLicenseKind.Commercial, license.Kind);
            Assert.Equal("AVC-2026-7K3M9Q", license.Id);
            Assert.Equal("Acme Ltd.", license.Licensee);
            Assert.Equal("business", license.Plan);
            Assert.Equal(new DateOnly(2027, 10, 8), license.ValidUntil);
            Assert.True(license.IsSignatureValid);
            Assert.Equal("Commercial (Acme Ltd., AVC-2026-7K3M9Q, valid until 2027-10-08)", license.ToString());
        }

        [Fact]
        public void Parse_WrongSigner_IsAcceptedButMarkedInvalid()
        {
            using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var license = AvroLicense.Parse(IssueKey(Payload, other), PublicKey);

            Assert.Equal("Acme Ltd.", license.Licensee);
            Assert.False(license.IsSignatureValid);
            Assert.Contains("could not be verified", AvroConvert.LicenseNotice(license, new DateOnly(2026, 10, 8)));
        }

        [Fact]
        public void Parse_TamperedPayload_IsMarkedInvalid()
        {
            var key = IssueKey(Payload);
            var parts = key.Split('.');
            parts[1] = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(Payload.Replace("Acme Ltd.", "Evil Corp.")));

            var license = AvroLicense.Parse(string.Join('.', parts), PublicKey);

            Assert.Equal("Evil Corp.", license.Licensee);
            Assert.False(license.IsSignatureValid);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("Acme Ltd.")]
        [InlineData("AVC1.not-base64!.sig")]
        [InlineData("AVC2.eyJpZCI6IngifQ.AAAA")]
        public void Parse_Malformed_Throws(string value)
        {
            Assert.Throws<ArgumentException>(() => AvroLicense.Parse(value, PublicKey));
        }

        [Fact]
        public void Parse_PayloadWithoutLicensee_Throws()
        {
            Assert.Throws<ArgumentException>(() => AvroLicense.Parse(IssueKey("""{"id":"AVC-2026-X"}"""), PublicKey));
        }

        [Fact]
        public void Commercial_UsesEmbeddedPublicKey_RejectsForeignSignature()
        {
            var license = AvroLicense.Commercial(IssueKey(Payload));

            Assert.False(license.IsSignatureValid);
        }

        [Theory]
        [InlineData("NonCommercial", AvroLicenseKind.NonCommercial)]
        [InlineData(" smallbusiness ", AvroLicenseKind.SmallBusiness)]
        public void TryParse_FreeLicences(string value, AvroLicenseKind kind)
        {
            Assert.Equal(kind, AvroLicense.TryParse(value).Kind);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Commercial")]
        [InlineData("Commercial:Acme Ltd.")]
        [InlineData("Enterprise")]
        public void TryParse_InvalidValues_ReturnNull(string value)
        {
            Assert.Null(AvroLicense.TryParse(value));
        }

        [Fact]
        public void LicenseNotice_NoLicence_AsksForDeclaration()
        {
            Assert.Contains("AvroConvert.License", AvroConvert.LicenseNotice(null, new DateOnly(2026, 10, 8)));
            Assert.Null(AvroConvert.LicenseNotice(AvroLicense.SmallBusiness, new DateOnly(2026, 10, 8)));
        }

        [Fact]
        public void LicenseNotice_SubscriptionEnded_OnlyForVersionsReleasedAfterwards()
        {
            var license = AvroLicense.Parse(IssueKey(Payload), PublicKey);

            Assert.Null(AvroConvert.LicenseNotice(license, new DateOnly(2027, 10, 8)));
            Assert.Contains("Renew", AvroConvert.LicenseNotice(license, new DateOnly(2027, 10, 9)));
        }

        [Fact]
        public void Declaration_DoesNotAffectSerialization()
        {
            var previous = AvroConvert.License;
            try
            {
                AvroConvert.License = AvroLicense.Parse(IssueKey(Payload), PublicKey);
                var bytes = AvroConvert.Serialize(new { Name = "x", Value = 1 });
                Assert.NotEmpty(bytes);
            }
            finally
            {
                AvroConvert.License = previous;
            }
        }
    }
}
