using System.Security.Cryptography;

namespace CitasMedicas.Core.Services;

internal static class ContrasenaHasher
{
    private const string Version = "v1";
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public static string Crear(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var hash = Derivar(contrasena, sal, Iteraciones);
        return $"{Version}.{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string contrasena, string? guardado)
    {
        if (string.IsNullOrEmpty(guardado))
        {
            return false;
        }

        var partes = guardado.Split('.');
        if (partes.Length != 4 || partes[0] != Version || !int.TryParse(partes[1], out var iteraciones) || iteraciones < 1)
        {
            return false;
        }

        try
        {
            var sal = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Derivar(contrasena, sal, iteraciones);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derivar(string contrasena, byte[] sal, int iteraciones) =>
        Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, TamanoHash);
}
