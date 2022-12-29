﻿using Chat.API.Models;
using Chat.API.Persistance;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using User = Chat.API.Persistance.User;

namespace Chat.API.Services
{

    public class LoginService
    {
        private ApiDbContext _apiDbContext { get; init; }
        private IConfiguration _configuration { get; init; }
        private IHttpContextAccessor _httpContextAccessor { get; init; }

        private string _authCookieHeaderName = "token";

        public LoginService(ApiDbContext apiDbContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _apiDbContext = apiDbContext;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Models.User> Login(UserRequest request)
        {
            var userEntity = await
                _apiDbContext.Users
                .Where(x => x.Name == request.Username && x.Password == HashPassword(request.Password))
                .SingleOrDefaultAsync();

            if (!_apiDbContext.Users.ToList().Where(x => x.Name == request.Username).Any())
            {
                throw new Exception("User does not exist!");
            }

            var userResponse = new Models.User(userEntity);

            ////read cookie from IHttpContextAccessor  
            // string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["key"];

            CreateCookieWithJWTTokenForUser(userEntity);

            return userResponse;
        }

        public async Task<Models.User> Registrate(UserRequest request)
        {
            if (_apiDbContext.Users.Where(x => x.Name == request.Username).Any())
            {
                throw new Exception("User of this username exists");
            }
            var userEntity = new User() { Name = request.Username };

            userEntity.Password = HashPassword(request.Password);

            _apiDbContext.Users.Add(userEntity);
            await _apiDbContext.SaveChangesAsync();

            var userResponse = new Models.User(userEntity);
            CreateCookieWithJWTTokenForUser(userEntity);

            return userResponse;
        }

        private string HashPassword(string secret)
        {
            var passKey = _configuration.GetValue<string>("PassKey");
            using var sha256 = SHA256.Create();
            var secretBytes = Encoding.UTF8.GetBytes(secret + passKey);
            var secretHash = sha256.ComputeHash(secretBytes);
            return Convert.ToHexString(secretHash);
        }

        private string GenerateJWTTokenForUser(User user)
        {
            var secretKey = _configuration.GetValue<string>("SecretKey");
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()), new Claim("username", user.Name) }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private void CreateCookieWithJWTTokenForUser(User user)
        {
            var response = _httpContextAccessor.HttpContext.Response;

            int? expireTime = 100;
            var key = _authCookieHeaderName;
            var value = GenerateJWTTokenForUser(user);

            CookieOptions option = new CookieOptions();

            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(10);

            response.Cookies.Append(key, value, option);
        }


    }
}
