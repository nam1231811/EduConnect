﻿using EduConnect.DTO;
using EduConnect.Entities;
using EduConnect.Repositories;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace EduConnect.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtService _jwtService;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IStudentRepository _studentRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, JwtService jwtService, IStudentRepository studentRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _passwordHasher = new PasswordHasher<User>();
            _studentRepository = studentRepository;
            _configuration = configuration;
        }

        public async Task<LoginResponse> GoogleLoginAsync(GoogleAuthSettings request)
        {
            // Lay clientId tu appsettings
            var clientId = _configuration["GoogleAuthSettings:ClientId"];

            // Kiem tra token tu Google va xac minh dung cho app minh (clientId)
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId } 
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

            // Kiem tra co ton tai nguoi dung 
            var existingUser = await _userRepository.GetByEmailAsync(payload.Email);
            // Neu chua co thi tao moi user moi tu thong tin google tra ve
            if (existingUser == null)
            {
                // Neu chua co thi tao moi user
                var newUser = new User
                {
                    UserId = Guid.NewGuid().ToString(),
                    Email = payload.Email,
                    FullName = payload.Name,
                    FirstName = payload.FamilyName,
                    LastName = payload.GivenName,
                    UserImage = payload.Picture,
                    Role = "Parent", 
                    IsActive = true,
                    CreateAt = DateTime.Now                
                };

                await _userRepository.AddUserAsync(newUser);
                existingUser = newUser;

                // tao moi doi tuong parent tuong ung
                var newParent = new Parent
                {
                    UserId = newUser.UserId
                };
                await _userRepository.AddParentAsync(newParent);
            }
            // Tao JWT Token de client luu dang nhap
            var token = _jwtService.GenerateToken(existingUser);
            return new LoginResponse
            {
                Token = token,
                UserId = existingUser.UserId,
                FullName = existingUser.FullName,
                Email = existingUser.Email,
                Role = existingUser.Role
            };
        }

        //dang nhap
        public async Task<LoginResponse?> LoginAsync(Login request)
        {
            //tim user theo email
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null) 
            {
                return null;
            }

            //hash lai pasword de so sanh
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verifyResult != PasswordVerificationResult.Success)
            {
                return null;
            }

            ////sai mat khau tra ve null
            //if (user.PasswordHash != passwordHash)
            //    return null;

            var token = _jwtService.GenerateToken(user);
            return new LoginResponse
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
            };
        }

        //dang ki nguoi dung moi
        public async Task<string> RegisterAsync(Register request)
        {
            //check mail da co hay chua
            var existUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existUser != null)
            {
                throw new Exception("Email already exits.");
            }      

            //tao user moi
            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                FullName = $"{request.FirstName} {request.LastName}",
                Role = "Parent",
                IsActive = true, // ve sau co them xac thuc
                CreateAt = DateTime.UtcNow,
            };


            //hash password
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            //luu vao database
            await _userRepository.AddUserAsync(user);

            //tao tk parent moi
            var parent = new Parent
            {
                UserId = user.UserId,
            };
            await _userRepository.AddParentAsync(parent);

            //add parent vao student
            if (request.StudentIds != null && request.StudentIds.Count > 0)
            {
                foreach (var studentId in request.StudentIds)
                {
                    var student = await _studentRepository.GetByIdAsync(studentId);
                    if (student == null)
                    {
                        throw new Exception($"Student ID '{studentId}' not found.");
                    }

                    student.ParentId = parent.ParentId;
                    await _studentRepository.UpdateAsync(student);
                }
            }
            return "User registered successfully.";
        }

        public async Task<bool> ResetPasswordAsync(ResetPassword dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null) return false;

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
}
