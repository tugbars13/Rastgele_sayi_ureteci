using System;
using System.Security.Cryptography;
using System.Text;

class Program
{
    // Çoklu hash (hash chain)
    static byte[] MultiHash(byte[] data, int rounds)
    {
        using var sha = SHA256.Create();
        byte[] result = data;
        for (int i = 0; i < rounds; i++)
            result = sha.ComputeHash(result);
        return result;
    }

    // Byte birleştirme
    static byte[] Concat(params byte[][] arrays)
    {
        int total = 0;
        foreach (var a in arrays) total += a.Length;

        byte[] merged = new byte[total];
        int offset = 0;
        foreach (var a in arrays)
        {
            Buffer.BlockCopy(a, 0, merged, offset, a.Length);
            offset += a.Length;
        }
        return merged;
    }

    // Güvenli byte üret
    static byte[] SecureBytes(int len)
    {
        byte[] b = new byte[len];
        RandomNumberGenerator.Fill(b);
        return b;
    }

    // Token üret (kısa)
    static string MakeToken(byte[] bytes, int length = 12)
    {
        // URL-safe base64
        string s = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return s.Length > length ? s[..length] : s;
    }

    static void Main()
    {
        Console.Write("Rastgele bir sey yaz: ");
        string userInput = Console.ReadLine() ?? "";

        // İnsan entropisi: tuşa basma süresi
        Console.WriteLine("Simdi ENTER'a basin (sure olculuyor)...");
        long t1 = DateTime.UtcNow.Ticks;
        Console.ReadLine();
        long t2 = DateTime.UtcNow.Ticks;

        long delta = t2 - t1;                    // tuşa basma süresi
        long timeA = DateTime.UtcNow.Ticks;      // ekstra zaman noktası
        long timeB = Environment.TickCount64;    // başka bir zaman kaynağı

        byte[] sysEntropy1 = SecureBytes(32);
        byte[] sysEntropy2 = SecureBytes(32);

        byte[] inputBytes = Encoding.UTF8.GetBytes(userInput);

        byte[] seedMaterial = Concat(
            inputBytes,
            BitConverter.GetBytes(timeA),
            BitConverter.GetBytes(timeB),
            BitConverter.GetBytes(delta),
            sysEntropy1,
            sysEntropy2
        );

        // Seed’i sertleştir
        byte[] state = MultiHash(seedMaterial, rounds: 8);

        // Üretim sayısı (istersen artır)
        Console.Write("Kac adet uretelim? (ornegin 3): ");
        string countStr = Console.ReadLine() ?? "3";
        if (!int.TryParse(countStr, out int count) || count < 1) count = 3;

        Console.WriteLine("\n--- URETIM ---");

        for (int i = 0; i < count; i++)
        {
            // Her turda ekstra entropi ekleyip state güncelle (forward/backward tahmini zorlaştırır)
            byte[] fresh = SecureBytes(32);
            state = MultiHash(Concat(state, fresh, BitConverter.GetBytes(DateTime.UtcNow.Ticks)), rounds: 4);

            // 6 haneli sayı
            int n = (int)(BitConverter.ToUInt32(state, 0) % 1_000_000);

            // token (kısa)
            string token = MakeToken(state, length: 14);

            Console.WriteLine($"[{i + 1}] Sayi: {n:D6}    Token: {token}");
        }

        // State'i "mantıksal" olarak yok et (GC garanti değil ama iyi pratik)
        Array.Clear(state, 0, state.Length);
    }
}
