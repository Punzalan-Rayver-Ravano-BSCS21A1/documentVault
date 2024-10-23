using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using DocumentManagementSystem.Models; // Ensure you have this namespace to reference the User model

namespace DocumentManagementSystem.Data
{
    public class Database
    {
        // Corrected connection string format for C#
        private static string connectionString = "Server=sql12.freesqldatabase.com;Database=sql12739920;User Id=sql12739920;Password=MCUe2PIpv7;Port=3306;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }

        public static void TestConnection()
        {
            using (MySqlConnection connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Connection to the database was successful!");
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        // New method to retrieve users from the database
        public static List<User> GetUsers()
        {
            List<User> users = new List<User>();
            using (MySqlConnection connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "SELECT UserId, Username, PasswordHash, Email, IsAdmin, IsLocked FROM Users"; // Update table name and fields if needed
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                User user = new User
                                {
                                    UserId = reader.GetInt32("UserId"),
                                    Username = reader.GetString("Username"),
                                    PasswordHash = reader.GetString("PasswordHash"),
                                    Email = reader.GetString("Email"),
                                    IsAdmin = reader.GetBoolean("IsAdmin"),
                                    IsLocked = reader.GetBoolean("IsLocked")
                                };
                                users.Add(user);
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"An error occurred while fetching users: {ex.Message}");
                }
            }
            return users;
        }
        public static void UpdateUserLockedStatus(int userId, bool isLocked)
        {
            using (MySqlConnection connection = GetConnection())
            {
                try
                {
                    connection.Open();
                    string query = "UPDATE users SET IsLocked = @IsLocked WHERE UserId = @UserId"; // Replace 'users' with your actual table name
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@IsLocked", isLocked);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }
    }
}
