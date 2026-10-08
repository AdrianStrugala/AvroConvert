using System;
using System.Diagnostics;
using System.Threading;

namespace SolTechnology.Avro
{
    /// <summary>
    /// The licence an application relies on when using AvroConvert. See LICENSE.md in the package or at
    /// https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md.
    /// </summary>
    /// <remarks>
    /// The declaration is informational: nothing is validated and functionality is never restricted.
    /// Set it through <see cref="AvroConvert.License"/> or the <c>AVROCONVERT_LICENSE</c> environment variable
    /// (<c>NonCommercial</c>, <c>SmallBusiness</c> or <c>Commercial:Your Company Ltd.</c>).
    /// </remarks>
    public sealed class AvroLicense
    {
        /// <summary>PolyForm Noncommercial 1.0.0 – individuals, non-profits, public institutions, research, evaluation.</summary>
        public static AvroLicense NonCommercial { get; } = new(AvroLicenseKind.NonCommercial, null);

        /// <summary>PolyForm Small Business 1.0.0 – companies under 100 people and 1,000,000 USD annual revenue.</summary>
        public static AvroLicense SmallBusiness { get; } = new(AvroLicenseKind.SmallBusiness, null);

        /// <summary>SolTechnology Commercial Licence – <paramref name="licensee"/> is the organisation named on the invoice.</summary>
        public static AvroLicense Commercial(string licensee)
        {
            if (string.IsNullOrWhiteSpace(licensee))
            {
                throw new ArgumentException("The licensee (organisation named on the invoice) is required.", nameof(licensee));
            }

            return new AvroLicense(AvroLicenseKind.Commercial, licensee.Trim());
        }

        public AvroLicenseKind Kind { get; }

        /// <summary>Organisation named on the invoice; null for the free licences.</summary>
        public string Licensee { get; }

        private AvroLicense(AvroLicenseKind kind, string licensee)
        {
            Kind = kind;
            Licensee = licensee;
        }

        public override string ToString() => Licensee == null ? Kind.ToString() : $"{Kind} ({Licensee})";

        /// <summary>Parses the <c>AVROCONVERT_LICENSE</c> environment variable format.</summary>
        internal static AvroLicense TryParse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var separator = value.IndexOf(':');
            var kind = (separator < 0 ? value : value[..separator]).Trim();
            var licensee = separator < 0 ? null : value[(separator + 1)..].Trim();

            if (kind.Equals(nameof(NonCommercial), StringComparison.OrdinalIgnoreCase)) return NonCommercial;
            if (kind.Equals(nameof(SmallBusiness), StringComparison.OrdinalIgnoreCase)) return SmallBusiness;
            if (kind.Equals(nameof(Commercial), StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(licensee)) return Commercial(licensee);

            return null;
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

        /// <summary>Traces the licence notice once per process when no licence has been declared.</summary>
        internal static void EnsureLicenseNotice()
        {
            if (_noticeShown != 0 || License != null || Interlocked.Exchange(ref _noticeShown, 1) != 0)
            {
                return;
            }

            Trace.TraceWarning(
                "AvroConvert is free for noncommercial use and for companies under 100 people / 1M USD revenue; " +
                "other companies need a commercial licence (https://soltechnology.dev/avroconvert/). " +
                "Declare your licence with AvroConvert.License = AvroLicense.NonCommercial / SmallBusiness / Commercial(\"name on invoice\") " +
                "or the AVROCONVERT_LICENSE environment variable to silence this notice.");
        }
    }
}
