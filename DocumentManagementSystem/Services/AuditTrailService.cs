using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Data;

namespace DocumentManagementSystem.Services
{
    public class AuditTrailService
    {
        // Retrieves the audit trail for a specific user
        public List<AuditTrail> GetAuditTrailByUser(int userId)
        {
            List<AuditTrail> auditTrails = new List<AuditTrail>();
            try
            {
                using (MySqlConnection conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT * FROM AuditTrail WHERE UserId = @UserId ORDER BY Timestamp DESC";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            auditTrails.Add(new AuditTrail
                            {
                                AuditId = (int)reader["AuditId"],
                                UserId = (int)reader["UserId"],
                                Action = reader["Action"].ToString(),
                                TableName = reader["TableName"].ToString(),
                                RecordId = (int)reader["RecordId"],
                                Timestamp = (DateTime)reader["Timestamp"],
                                Details = reader["Details"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while retrieving the audit trail: " + ex.Message);
                // Consider logging the exception or rethrowing as necessary
            }
            return auditTrails;
        }

        // Logs an action into the audit trail
        public void LogAction(int userId, string action, string tableName, int recordId, string details)
        {
            try
            {
                using (MySqlConnection conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = "INSERT INTO AuditTrail (UserId, Action, TableName, RecordId, Timestamp, Details) " +
                                   "VALUES (@UserId, @Action, @TableName, @RecordId, @Timestamp, @Details)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@TableName", tableName);
                        cmd.Parameters.AddWithValue("@RecordId", recordId);
                        cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Details", details);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while logging the action: " + ex.Message);
                // Consider logging the exception or rethrowing as necessary
            }
        }
    }
}
