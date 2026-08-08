using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace ChatVox.Twitch;

public enum AuthBlobLoadKind { Missing, Loaded, Unreadable }
public sealed record AuthBlobLoadResult(AuthBlobLoadKind Kind, TokenResponse? Auth = null);

public sealed class DpapiAuthStore
{
    private readonly string path;
    private readonly Action<string>? diagnostic;
    public DpapiAuthStore(string? file = null, Action<string>? diagnostic = null)
    {
        path = file ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatVox", "auth", "auth.bin");
        this.diagnostic = diagnostic;
    }

    public AuthBlobLoadResult TryLoad()
    {
        if (!File.Exists(path)) { diagnostic?.Invoke("auth blob missing"); return new(AuthBlobLoadKind.Missing); }
        try
        {
            var text = Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser));
            var auth = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(text);
            if (auth is null || string.IsNullOrWhiteSpace(auth.AccessToken) || string.IsNullOrWhiteSpace(auth.RefreshToken)) throw new InvalidDataException();
            diagnostic?.Invoke("auth blob loaded");
            return new(AuthBlobLoadKind.Loaded, auth);
        }
        catch { diagnostic?.Invoke("auth blob unreadable"); return new(AuthBlobLoadKind.Unreadable); }
    }

    public TokenResponse? Load() => TryLoad().Auth;

    public bool TrySave(TokenResponse auth)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(auth)), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(temporary, data);
            File.Move(temporary, path, true);
            diagnostic?.Invoke("auth blob write succeeded");
            return true;
        }
        catch { diagnostic?.Invoke("auth blob write failed"); return false; }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
    }

    public void Save(TokenResponse auth)
    {
        if (!TrySave(auth)) throw new IOException("Unable to persist authorization state.");
    }

    public void Clear()
    {
        if (File.Exists(path)) File.Delete(path);
        diagnostic?.Invoke("auth blob cleared after permanent authorization failure");
    }
}
