using AwesomeAssertions;
using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertUnitTests
{
    public class LicenseTests
    {
        private static readonly ECDsa SigningKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        private static readonly byte[] PublicKey = SigningKey.ExportSubjectPublicKeyInfo();

        [OneTimeTearDown]
        public static void DisposeSigningKey() => SigningKey.Dispose();

        private static string IssueKey(string payloadJson, ECDsa signer = null)
        {
            var signedPart = "AVC1." + Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payloadJson));
            var signature = (signer ?? SigningKey).SignData(Encoding.ASCII.GetBytes(signedPart), HashAlgorithmName.SHA256);
            return signedPart + "." + Base64Url.EncodeToString(signature);
        }

        private const string Payload = """{"id":"AVC-2026-7K3M9Q","licensee":"Acme Ltd.","product":"avroconvert","plan":"business","issued":"2026-10-08","expires":"2027-10-08"}""";

        [Test]
        public void Parse_ValidKey_ExposesPayloadAndVerifiesSignature()
        {
            var license = AvroLicense.Parse(IssueKey(Payload), PublicKey);

            license.Kind.Should().Be(AvroLicenseKind.Commercial);
            license.Id.Should().Be("AVC-2026-7K3M9Q");
            license.Licensee.Should().Be("Acme Ltd.");
            license.Plan.Should().Be("business");
            license.ValidUntil.Should().Be(new DateOnly(2027, 10, 8));
            license.IsSignatureValid.Should().BeTrue();
            license.ToString().Should().Be("Commercial (Acme Ltd., AVC-2026-7K3M9Q, valid until 2027-10-08)");
        }

        [Test]
        public void Parse_WrongSigner_IsAcceptedButMarkedInvalid()
        {
            using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var license = AvroLicense.Parse(IssueKey(Payload, other), PublicKey);

            license.Licensee.Should().Be("Acme Ltd.");
            license.IsSignatureValid.Should().BeFalse();
            AvroConvert.LicenseNotice(license, new DateOnly(2026, 10, 8)).Should().Contain("could not be verified");
        }

        [Test]
        public void Parse_TamperedPayload_IsMarkedInvalid()
        {
            var key = IssueKey(Payload);
            var parts = key.Split('.');
            parts[1] = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(Payload.Replace("Acme Ltd.", "Evil Corp.")));

            var license = AvroLicense.Parse(string.Join('.', parts), PublicKey);

            license.Licensee.Should().Be("Evil Corp.");
            license.IsSignatureValid.Should().BeFalse();
        }

        [TestCase(" ")]
        [TestCase("Acme Ltd.")]
        [TestCase("AVC1.not-base64!.sig")]
        [TestCase("AVC2.eyJpZCI6IngifQ.AAAA")]
        public void Parse_Malformed_Throws(string value)
        {
            FluentActions.Invoking(() => AvroLicense.Parse(value, PublicKey)).Should().ThrowExactly<ArgumentException>();
        }

        [Test]
        public void Parse_PayloadWithoutLicensee_Throws()
        {
            FluentActions.Invoking(() => AvroLicense.Parse(IssueKey("""{"id":"AVC-2026-X"}"""), PublicKey)).Should().ThrowExactly<ArgumentException>();
        }

        [Test]
        public void Commercial_UsesEmbeddedPublicKey_RejectsForeignSignature()
        {
            var license = AvroLicense.Commercial(IssueKey(Payload));

            license.IsSignatureValid.Should().BeFalse();
        }

        [TestCase("NonCommercial", AvroLicenseKind.NonCommercial)]
        [TestCase(" smallbusiness ", AvroLicenseKind.SmallBusiness)]
        public void TryParse_FreeLicences(string value, AvroLicenseKind kind)
        {
            AvroLicense.TryParse(value).Kind.Should().Be(kind);
        }

        [TestCase("")]
        [TestCase("Commercial")]
        [TestCase("Commercial:Acme Ltd.")]
        [TestCase("Enterprise")]
        public void TryParse_InvalidValues_ReturnNull(string value)
        {
            AvroLicense.TryParse(value).Should().BeNull();
        }

        [Test]
        public void LicenseNotice_NoLicence_AsksForDeclaration()
        {
            AvroConvert.LicenseNotice(null, new DateOnly(2026, 10, 8)).Should().Contain("AvroConvert.License");
            AvroConvert.LicenseNotice(AvroLicense.SmallBusiness, new DateOnly(2026, 10, 8)).Should().BeNull();
        }

        [Test]
        public void LicenseNotice_SubscriptionEnded_OnlyForVersionsReleasedAfterwards()
        {
            var license = AvroLicense.Parse(IssueKey(Payload), PublicKey);

            AvroConvert.LicenseNotice(license, new DateOnly(2027, 10, 8)).Should().BeNull();
            AvroConvert.LicenseNotice(license, new DateOnly(2027, 10, 9)).Should().Contain("Renew");
        }

        [Test, NonParallelizable]
        public void EnsureLicenseNotice_NoLicence_WritesToStandardErrorOncePerProcess()
        {
            var previousLicense = AvroConvert.License;
            var previousError = Console.Error;
            var stderr = new System.IO.StringWriter();
            try
            {
                AvroConvert.License = null;
                Console.SetError(stderr);
                AvroConvert.ResetLicenseNoticeForTesting();

                AvroConvert.EnsureLicenseNotice();
                AvroConvert.EnsureLicenseNotice();

                var output = stderr.ToString();
                output.Should().StartWith("[AvroConvert] ").And.Contain("https://soltechnology.dev/avroconvert/");
                output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(1);
            }
            finally
            {
                Console.SetError(previousError);
                AvroConvert.License = previousLicense;
            }
        }

        [Test, NonParallelizable]
        public void EnsureLicenseNotice_DeclaredLicence_WritesNothing()
        {
            var previousLicense = AvroConvert.License;
            var previousError = Console.Error;
            var stderr = new System.IO.StringWriter();
            try
            {
                AvroConvert.License = AvroLicense.SmallBusiness;
                Console.SetError(stderr);
                AvroConvert.ResetLicenseNoticeForTesting();

                AvroConvert.EnsureLicenseNotice();

                stderr.ToString().Should().BeEmpty();
            }
            finally
            {
                Console.SetError(previousError);
                AvroConvert.License = previousLicense;
            }
        }

        [Test]
        public void Declaration_DoesNotAffectSerialization()
        {
            var previous = AvroConvert.License;
            try
            {
                AvroConvert.License = AvroLicense.Parse(IssueKey(Payload), PublicKey);
                var bytes = AvroConvert.Serialize(new { Name = "x", Value = 1 });
                bytes.Should().NotBeEmpty();
            }
            finally
            {
                AvroConvert.License = previous;
            }
        }
    }
}
