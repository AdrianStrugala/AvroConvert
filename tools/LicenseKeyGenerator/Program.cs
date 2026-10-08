using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SolTechnology.Avro;

// Issues AvroConvert commercial licence keys. Format and verification: src/AvroConvert/AvroConvert.License.cs.
//
//   keygen [--out <pem path>]                      create the ECDSA P-256 signing key (private key stays outside the repo)
//   issue  --licensee "<name on invoice>" --plan business|enterprise [--expires yyyy-MM-dd] [--id AVC-...] [--key <pem path>]
//   verify <licence key>                           parse with the public key compiled into AvroConvert

var defaultKeyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "soltechnology", "avroconvert-signing.pem");
var args_ = new Args(args);

switch (args_.Command)
{
    case "keygen":
        return KeyGen(args_.Option("out") ?? defaultKeyPath);
    case "issue":
        return Issue(args_);
    case "verify":
        return Verify(args_.Positional(1));
    default:
        Console.Error.WriteLine("usage: keygen [--out path] | issue --licensee NAME --plan PLAN [--expires DATE] [--id ID] [--key path] | verify KEY");
        return 2;
}

int KeyGen(string path)
{
    if (File.Exists(path))
    {
        Console.Error.WriteLine($"{path} already exists; delete it first if you really want a new signing key (all issued keys would stop verifying).");
        return 1;
    }

    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, ecdsa.ExportPkcs8PrivateKeyPem());
    if (!OperatingSystem.IsWindows())
    {
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    Console.WriteLine($"Private key written to {path} (move it to a password manager; never commit or print it).");
    Console.WriteLine($"Licensing Worker secret: wrangler secret put SIGNING_KEY_PEM < {path}");
    Console.WriteLine();
    Console.WriteLine("Public key for AvroConvert.License.cs:");
    Console.WriteLine($"    private const string SigningPublicKey = \"{Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo())}\";");
    return 0;
}

int Issue(Args a)
{
    var licensee = a.Option("licensee")?.Trim();
    var plan = a.Option("plan")?.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(licensee) || plan is not ("business" or "enterprise"))
    {
        Console.Error.WriteLine("issue requires --licensee \"<name on invoice>\" and --plan business|enterprise");
        return 2;
    }

    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var expires = a.Option("expires") is { } e ? DateOnly.ParseExact(e, "yyyy-MM-dd") : today.AddYears(1);
    var id = a.Option("id") ?? NewId(today.Year);
    var keyPath = a.Option("key") ?? defaultKeyPath;

    using var ecdsa = ECDsa.Create();
    ecdsa.ImportFromPem(File.ReadAllText(keyPath));

    var payload = JsonSerializer.SerializeToUtf8Bytes(new
    {
        id,
        licensee,
        product = "avroconvert",
        plan,
        issued = today.ToString("yyyy-MM-dd"),
        expires = expires.ToString("yyyy-MM-dd")
    });

    var signedPart = "AVC1." + Base64Url.EncodeToString(payload);
    var signature = ecdsa.SignData(Encoding.ASCII.GetBytes(signedPart), HashAlgorithmName.SHA256);

    Console.WriteLine(signedPart + "." + Base64Url.EncodeToString(signature));
    return 0;
}

int Verify(string? key)
{
    if (key == null)
    {
        Console.Error.WriteLine("verify requires the licence key as argument");
        return 2;
    }

    var license = AvroLicense.Commercial(key);
    Console.WriteLine($"Id:          {license.Id}");
    Console.WriteLine($"Licensee:    {license.Licensee}");
    Console.WriteLine($"Plan:        {license.Plan}");
    Console.WriteLine($"Valid until: {license.ValidUntil:yyyy-MM-dd}");
    Console.WriteLine($"Signature:   {(license.IsSignatureValid ? "valid" : "INVALID")}");
    return license.IsSignatureValid ? 0 : 1;
}

// Crockford base32 without look-alike characters, e.g. AVC-2026-7K3M9Q.
static string NewId(int year)
{
    const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    var bytes = RandomNumberGenerator.GetBytes(6);
    return $"AVC-{year}-{new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray())}";
}

sealed class Args
{
    private readonly string[] _args;
    public Args(string[] args) => _args = args;
    public string Command => _args.Length > 0 ? _args[0] : "";
    public string? Positional(int index) => _args.Length > index && !_args[index].StartsWith("--") ? _args[index] : null;

    public string? Option(string name)
    {
        var i = Array.IndexOf(_args, "--" + name);
        return i >= 0 && i + 1 < _args.Length ? _args[i + 1] : null;
    }
}
