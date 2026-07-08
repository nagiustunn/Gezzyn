using gezzyn.Domain.Entities;

namespace gezzyn.Tests.Unit.Common.Builders
{
    public class UserBuilder
    {
        private Guid _id = Guid.NewGuid();
        private string _name = "Test";
        private string _surname = "Kullanıcı";
        private string _userName = "testkullanici";
        private string _email = "test@gezzyn.app";
        private string _passwordHash = "hashed_password";

        public UserBuilder WithId(Guid id) { _id = id; return this; }
        public UserBuilder WithEmail(string email) { _email = email; return this; }
        public UserBuilder WithUserName(string userName) { _userName = userName; return this; }
        public UserBuilder WithPasswordHash(string hash) { _passwordHash = hash; return this; }

        public User Build() => new()
        {
            Id = _id,
            Name = _name,
            Surname = _surname,
            UserName = _userName,
            Email = _email,
            PasswordHash = _passwordHash
        };
    }
}
