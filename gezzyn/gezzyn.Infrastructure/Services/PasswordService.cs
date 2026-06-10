using gezzyn.Domain.Interfaces;
using Isopoh.Cryptography.Argon2;

namespace gezzyn.Infrastructure.Services
{
    public class PasswordService : IPasswordService
    {
        public string Hash(string password)
        {
            var config = new Argon2Config
            {
                Type = Argon2Type.HybridAddressing,  
                Version = Argon2Version.Nineteen,
                TimeCost = 3,         
                MemoryCost = 65536,    
                Lanes = 4,           
                Threads = 4,
                Password = System.Text.Encoding.UTF8.GetBytes(password),
                HashLength = 32
            };

            using var argon2 = new Argon2(config);
            using var hash = argon2.Hash();
            return config.EncodeString(hash.Buffer);
        }

        public bool Verify(string password, string hash)
        {
            return Argon2.Verify(hash, password);
        }
    }
}
