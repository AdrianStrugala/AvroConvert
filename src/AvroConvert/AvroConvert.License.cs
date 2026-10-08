using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace SolTechnology.Avro
{
    /// <summary>
    /// The licence an application relies on when using AvroConvert. See LICENSE.md in the package or at
    /// https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md.
    /// </summary>
    /// <remarks>
    /// The declaration is informational: functionality is never restricted. Commercial licence keys are
    /// signed and parsed so that the licensee, plan and subscription end are visible, but an invalid or
    /// expired key only produces a <see cref="Trace"/> warning.
    /// Set it through <see cref="AvroConvert.License"/> or the <c>AVROCONVERT_LICENSE</c> environment variable
    /// (<c>NonCommercial</c>, <c>SmallBusiness</c> or the licence key).
    /// </remarks>
    public sealed class AvroLicense
    {
        private const string KeyPrefix = "AVC1";

        // ECDSA P-256 public key (SubjectPublicKeyInfo) matching the private key used to issue licence keys.
        private const string SigningPublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE8g1QMibUwtAc1sYGvm9bGWrijoURey+6jWhCoDKaomwdlszB2ex9NyuWqd8jY6smbdSa90aFlf1LNs+bRkliVg==";

        /// <summary>PolyForm Noncommercial 1.0.0 – individuals, non-profits, public institutions, research, evaluation.</summary>
        public static AvroLicense NonCommercial { get; } = new(AvroLicenseKind.NonCommercial);

        /// <summary>PolyForm Small Business 1.0.0 – companies under 100 people and 1,000,000 USD annual revenue.</summary>
        public static AvroLicense SmallBusiness { get; } = new(AvroLicenseKind.SmallBusiness);

        /// <summary>SolTechnology Commercial Licence – <paramref name="licenseKey"/> is the key received by e-mail after purchase.</summary>
        /// <exception cref="ArgumentException">The value is not an AvroConvert licence key.</exception>
        public static AvroLicense Commercial(string licenseKey) => Parse(licenseKey, Convert.FromBase64String(SigningPublicKey));

        public AvroLicenseKind Kind { get; }

        /// <summary>Licence number printed on the certificate; null for the free licences.</summary>
        public string Id { get; private init; }

        /// <summary>Organisation named on the invoice; null for the free licences.</summary>
        public string Licensee { get; private init; }

        /// <summary>Purchased plan (business, enterprise); null for the free licences.</summary>
        public string Plan { get; private init; }

        /// <summary>End of the subscription term; versions released before this date stay licensed for ever.</summary>
        public DateOnly? ValidUntil { get; private init; }

        /// <summary>True when the key signature was verified with the SolTechnology public key.</summary>
        public bool IsSignatureValid { get; private init; }

        private AvroLicense(AvroLicenseKind kind)
        {
            Kind = kind;
        }

        public override string ToString() =>
            Licensee == null ? Kind.ToString() : $"{Kind} ({Licensee}, {Id}, valid until {ValidUntil:yyyy-MM-dd})";

        /// <summary>Parses the <c>AVROCONVERT_LICENSE</c> environment variable format.</summary>
        internal static AvroLicense TryParse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            if (value.Equals(nameof(NonCommercial), StringComparison.OrdinalIgnoreCase)) return NonCommercial;
            if (value.Equals(nameof(SmallBusiness), StringComparison.OrdinalIgnoreCase)) return SmallBusiness;

            try
            {
                return Commercial(value);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Key format: <c>AVC1.&lt;base64url JSON payload&gt;.&lt;base64url ECDSA P-256/SHA-256 signature (IEEE P1363)&gt;</c>;
        /// the signature covers the ASCII bytes of <c>AVC1.&lt;payload&gt;</c>.
        /// </summary>
        internal static AvroLicense Parse(string licenseKey, ReadOnlySpan<byte> publicKeySpki)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                throw new ArgumentException("The licence key received after purchase is required.", nameof(licenseKey));
            }

            var parts = licenseKey.Trim().Split('.');
            if (parts.Length != 3 || parts[0] != KeyPrefix)
            {
                throw Malformed();
            }

            JObject payload;
            byte[] signature;
            try
            {
                payload = JObject.Parse(Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[1])));
                signature = Base64Url.DecodeFromChars(parts[2]);
            }
            catch (Exception e) when (e is FormatException || e is Newtonsoft.Json.JsonException)
            {
                throw Malformed();
            }

            var licensee = (string)payload["licensee"];
            var id = (string)payload["id"];
            if (string.IsNullOrWhiteSpace(licensee) || string.IsNullOrWhiteSpace(id))
            {
                throw Malformed();
            }

            DateOnly? validUntil = DateOnly.TryParseExact((string)payload["expires"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

            bool signatureValid;
            using (var ecdsa = ECDsa.Create())
            {
                ecdsa.ImportSubjectPublicKeyInfo(publicKeySpki, out _);
                signatureValid = ecdsa.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), signature, HashAlgorithmName.SHA256);
            }

            return new AvroLicense(AvroLicenseKind.Commercial)
            {
                Id = id,
                Licensee = licensee,
                Plan = (string)payload["plan"],
                ValidUntil = validUntil,
                IsSignatureValid = signatureValid
            };

            static ArgumentException Malformed() =>
                new("The value is not an AvroConvert licence key. Paste the key exactly as received by e-mail (it starts with 'AVC1.').", nameof(licenseKey));
        }
    }

    public enum AvroLicenseKind
    {
        NonCommercial,
        SmallBusiness,
        Commercial
    }

    public static partial class AvroConvert
    {
        private static AvroLicense _license;
        private static int _noticeShown;

        /// <summary>
        /// Declares the licence this application relies on (see <see cref="AvroLicense"/>). Optional; when not set,
        /// a single warning is traced per process and the library keeps working normally.
        /// </summary>
        public static AvroLicense License
        {
            get => _license ??= AvroLicense.TryParse(Environment.GetEnvironmentVariable("AVROCONVERT_LICENSE"));
            set => _license = value;
        }

        /// <summary>Build date from AssemblyMetadata("ReleaseDate"); decides whether an ended subscription still covers this version.</summary>
        internal static DateOnly ReleaseDate { get; } = ReadReleaseDate();

        /// <summary>Traces the licence notice once per process when no or a problematic licence has been declared.</summary>
        internal static void EnsureLicenseNotice()
        {
            if (_noticeShown != 0)
            {
                return;
            }

            var message = LicenseNotice(License, ReleaseDate);
            if (message == null || Interlocked.Exchange(ref _noticeShown, 1) != 0)
            {
                return;
            }

            Trace.TraceWarning(message);
        }

        internal static string LicenseNotice(AvroLicense license, DateOnly releaseDate)
        {
            if (license == null)
            {
                return "AvroConvert is free for noncommercial use and for companies under 100 people / 1M USD revenue; " +
                       "other companies need a commercial licence (https://soltechnology.dev/avroconvert/). " +
                       "Declare your licence with AvroConvert.License = AvroLicense.NonCommercial / SmallBusiness / Commercial(\"licence key\") " +
                       "or the AVROCONVERT_LICENSE environment variable to silence this notice.";
            }

            if (license.Kind != AvroLicenseKind.Commercial)
            {
                return null;
            }

            if (!license.IsSignatureValid)
            {
                return $"AvroConvert licence key {license.Id} ({license.Licensee}) could not be verified. " +
                       "Paste the key exactly as received by e-mail or contact support@soltechnology.dev.";
            }

            if (license.ValidUntil is { } validUntil && validUntil < releaseDate)
            {
                return $"AvroConvert licence {license.Id} ({license.Licensee}) covers versions released until {validUntil:yyyy-MM-dd}; " +
                       $"this version was released on {releaseDate:yyyy-MM-dd}. Renew at https://soltechnology.dev/avroconvert/ to use it under the commercial licence.";
            }

            return null;
        }

        private static DateOnly ReadReleaseDate()
        {
            var value = typeof(AvroConvert).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "ReleaseDate")?.Value;

            return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : DateOnly.MinValue;
        }
    }
}
