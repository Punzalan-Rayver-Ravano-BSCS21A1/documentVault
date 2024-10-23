using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using DocumentManagementSystem.Data;
using DocumentManagementSystem.Models;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DocumentManagementSystem.Services
{
    public class UserService
    {
        public async Task<User> Authenticate(string username, string password)
        {
            using (MySqlConnection conn = Database.GetConnection())
            {
                await conn.OpenAsync(); // Open connection asynchronously
                string hash = ComputeHash(password);
                string query = "SELECT * FROM Users WHERE Username = @Username AND PasswordHash = @PasswordHash";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);

                // Cast the DbDataReader to MySqlDataReader
                using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    if (reader.Read())
                    {
                        return new User
                        {
                            UserId = (int)reader["UserId"],
                            Username = reader["Username"].ToString(),
                            Email = reader["Email"].ToString(),
                            IsAdmin = (bool)reader["IsAdmin"],
                            IsLocked = (bool)reader["IsLocked"]
                        };
                    }
                }
            }
            return null; // Return null if authentication fails
        }


        public List<User> GetAllUsers()
        {
            List<User> users = new List<User>();

            using (MySqlConnection connection = Database.GetConnection())
            {
                string query = "SELECT UserId, Username, IsLocked, IsAdmin FROM Users";
                MySqlCommand cmd = new MySqlCommand(query, connection);

                try
                {
                    connection.Open();
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new User
                            {
                                UserId = reader.GetInt32("UserId"),
                                Username = reader.GetString("Username"),
                                IsLocked = reader.GetBoolean("IsLocked"),
                                IsAdmin = reader.GetBoolean("IsAdmin")
                            };
                            users.Add(user);
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show($"An error occurred while retrieving users: {ex.Message}");
                }
            }

            return users;
        }

        public void RegisterUser(string username, string password, string email)
        {
            using (MySqlConnection conn = Database.GetConnection())
            {
                conn.Open();

                // Check if email already exists
                string checkEmailQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                MySqlCommand checkEmailCmd = new MySqlCommand(checkEmailQuery, conn);
                checkEmailCmd.Parameters.AddWithValue("@Email", email);

                int emailCount = Convert.ToInt32(checkEmailCmd.ExecuteScalar());
                if (emailCount > 0)
                {
                    throw new Exception("Email already exists. Please use a different email.");
                }

                string hash = ComputeHash(password);
                string query = "INSERT INTO Users (Username, PasswordHash, Email, IsAdmin) VALUES (@Username, @PasswordHash, @Email, 0)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@PasswordHash", hash);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.ExecuteNonQuery();
            }
        }

        public bool DoesUserExist(string username)
        {
            using (MySqlConnection conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        private string ComputeHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            using (MySqlConnection conn = Database.GetConnection())
            {
                conn.Open();
                string oldPasswordHash = ComputeHash(oldPassword);
                string newPasswordHash = ComputeHash(newPassword);

                // Verify the old password
                string query = "SELECT COUNT(*) FROM Users WHERE UserId = @UserId AND PasswordHash = @OldPasswordHash";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@OldPasswordHash", oldPasswordHash);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    return false;
                }

                // Update to the new password
                query = "UPDATE Users SET PasswordHash = @NewPasswordHash WHERE UserId = @UserId";
                cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.ExecuteNonQuery();

                return true;
            }
        }

        public async Task<bool> IsEmailRegisteredAsync(string email)
        {
            using (MySqlConnection conn = Database.GetConnection())
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", email);

                int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                return count > 0;
            }
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            using (MySqlConnection conn = Database.GetConnection())
            {
                await conn.OpenAsync();
                string newPasswordHash = ComputeHash(newPassword);

                // Update to the new password
                string query = "UPDATE Users SET PasswordHash = @NewPasswordHash WHERE Email = @Email";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);
                cmd.Parameters.AddWithValue("@Email", email);
                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                return rowsAffected > 0;
            }
        }

        // Lock user by UserId
        public async Task LockUser(int userId)
        {
            using (MySqlConnection conn = Database.GetConnection())
            {
                await conn.OpenAsync();
                string query = "UPDATE Users SET IsLocked = 1 WHERE UserId = @UserId";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync(); // Execute the command

                if (rowsAffected > 0)
                {
                    MessageBox.Show("User locked successfully.");
                }
                else
                {
                    MessageBox.Show("Failed to lock the user. No rows were affected.");
                }
            }
        }

        public async Task UnlockUser(int userId)
        {
            using (MySqlConnection conn = Database.GetConnection())
            {
                await conn.OpenAsync();
                string query = "UPDATE Users SET IsLocked = 0 WHERE UserId = @UserId";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync(); // Execute the command

                if (rowsAffected > 0)
                {
                    MessageBox.Show("User unlocked successfully.");
                }
                else
                {
                    MessageBox.Show("Failed to unlock the user. No rows were affected.");
                }
            }
        }
    }
}
