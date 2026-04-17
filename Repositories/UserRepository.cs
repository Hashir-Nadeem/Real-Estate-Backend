using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Real_Estate_WebAPI.Interfaces;
using Real_Estate_WebAPI.Models;
using Real_Estate_WebAPI.Settings;
using System.Security.Cryptography;
using System.Text;
using SendGrid.Helpers.Mail;


namespace Real_Estate_WebAPI.Repositories
{
    public class UserRepository : IUserRepository
    {

        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Property> _properties;

        public UserRepository(
            IMongoClient mongoClient,
            IOptions<MongoDbSettings> settings)
        {
            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);

            _users = database.GetCollection<User>(
                settings.Value.UsersCollection);

            _properties = database.GetCollection<Property>("Properties"); // ⚠️ match your collection name
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _users
                .Find(u => u.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            email = email.ToLowerInvariant().Trim();

            return await _users
                .Find(u => u.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _users
                .Find(_ => true)
                .Project(user => new User
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                })
                .ToListAsync();
        }
        public async Task<User?> GetByPhoneAsync(string phone)
        {
            return await _users
                .Find(u => u.PhoneNumber == phone)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            email = email.ToLowerInvariant().Trim();

            return await _users
                .Find(u => u.Email == email)
                .AnyAsync();
        }

        public async Task<bool> ExistsByPhoneAsync(string phone)
        {
            return await _users
                .Find(u => u.PhoneNumber == phone)
                .AnyAsync();
        }

        public async Task CreateAsync(User user)
        {
            try
            {
                user.Email = user.Email.ToLowerInvariant().Trim();

                await _users.InsertOneAsync(user);
            }
            catch (MongoWriteException ex)
                when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new Exception("A user with this email or phone already exists.");
            }
        }

        public async Task UpdateAsync(User user)
        {
            await _users.ReplaceOneAsync(
                u => u.Id == user.Id,
                user);
        }

        public async Task<User?> GetByResetTokenAsync(string token)
        {
        
            return await _users
                .Find(u => u.PasswordResetToken == token)
                .FirstOrDefaultAsync();
        }


        public RefreshToken GenerateRefreshToken(bool rememberMe)
        {
            return new RefreshToken
            {
                Token = Convert.ToBase64String(
                    RandomNumberGenerator.GetBytes(64)),

                ExpiresAt = rememberMe
                    ? DateTime.UtcNow.AddDays(30)
                    : DateTime.UtcNow.AddDays(7)
            };
        }
        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            return await _users
                .Find(u => u.RefreshTokens.Any(t => t.Token == refreshToken))
                .FirstOrDefaultAsync();
        }

        public async Task DeleteUserWithPropertiesAsync(string userId)
        {
            // ✅ Delete all properties of this user
            await _properties.DeleteManyAsync(p => p.UserId == userId);

            // ✅ Delete user
            await _users.DeleteOneAsync(u => u.Id == userId);
        }
        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _users
                .Find(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken))
                .FirstOrDefaultAsync();
        }

        public async Task RemoveRefreshTokenAsync(string userId)
        {
            var update = Builders<User>.Update
                .Set(u => u.RefreshTokens, null)
                .Set(u => u.PasswordResetTokenExpiresAt, null);

            await _users.UpdateOneAsync(
                u => u.Id == userId,
                update
            );
        }


    }

}
