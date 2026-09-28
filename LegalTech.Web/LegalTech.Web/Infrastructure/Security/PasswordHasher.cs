using System;
using System.Security.Cryptography;

namespace LegalTech.Web.Infrastructure.Security;

/// <summary>
/// Utilidad criptográfica de hashing seguro con PBKDF2 (HMAC-SHA256) y salt único.
/// Incluye verificación de tiempo constante (constant-time) para prevenir ataques de temporización (timing attacks).
/// </summary>
public static class PasswordHasher
{
    private const int Iteraciones = 100_000;
    private const int LongitudSaltBytes = 16;
    private const int LongitudHashBytes = 32;

    /// <summary>
    /// Genera una sal criptográfica aleatoria de 16 bytes codificada en Base64.
    /// </summary>
    public static string GenerarSalt()
    {
        byte[] salt = RandomNumberGenerator.GetBytes(LongitudSaltBytes);
        return Convert.ToBase64String(salt);
    }

    /// <summary>
    /// Genera el hash de la contraseña usando PBKDF2 con la sal proporcionada.
    /// </summary>
    public static string HashPassword(string password, string saltBase64)
    {
        byte[] salt = Convert.FromBase64String(saltBase64);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, LongitudHashBytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifica la contraseña en tiempo constante contra el hash y la sal almacenados.
    /// </summary>
    public static bool VerificarPassword(string passwordIngresada, string hashAlmacenadoBase64, string saltBase64)
    {
        try
        {
            byte[] hashAlmacenado = Convert.FromBase64String(hashAlmacenadoBase64);
            byte[] salt = Convert.FromBase64String(saltBase64);

            byte[] hashCalculado = Rfc2898DeriveBytes.Pbkdf2(passwordIngresada, salt, Iteraciones, HashAlgorithmName.SHA256, LongitudHashBytes);

            return CryptographicOperations.FixedTimeEquals(hashAlmacenado, hashCalculado);
        }
        catch
        {
            return false;
        }
    }
}
