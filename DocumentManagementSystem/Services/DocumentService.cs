using System;
using System.IO;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using DocumentManagementSystem.Models;
using DocumentManagementSystem.Data;

namespace DocumentManagementSystem.Services
{
    public class DocumentService
    {
        private readonly AuditTrailService _auditTrailService;

        public DocumentService()
        {
            _auditTrailService = new AuditTrailService();
        }

        private const string StoragePath = @"C:\Documents\Data\";

        // Method to retrieve documents uploaded by a specific user
        public List<Document> GetDocumentsByUser(int userId)
        {
            List<Document> documents = new List<Document>();

            using (MySqlConnection conn = Database.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM Document WHERE UserId = @UserId";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        documents.Add(new Document
                        {
                            DocumentId = (int)reader["DocumentId"],
                            UserId = (int)reader["UserId"],
                            DocumentName = reader["DocumentName"].ToString(),
                            FilePath = reader["FilePath"].ToString(),
                            UploadedAt = (DateTime)reader["UploadedAt"]
                        });
                    }
                }
            }

            return documents;
        }

        // Method to upload a document
        public void UploadDocument(int userId, string fileName, string filePath)
        {
            try
            {
                // Ensure the storage directory exists
                if (!Directory.Exists(StoragePath))
                {
                    Directory.CreateDirectory(StoragePath);
                }

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(fileName);
                string destinationPath = Path.Combine(StoragePath, uniqueFileName);

                // Copy the file to the storage path
                File.Copy(filePath, destinationPath);

                using (MySqlConnection conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = "INSERT INTO Document (UserId, DocumentName, FilePath, UploadedAt) VALUES (@UserId, @DocumentName, @FilePath, @UploadedAt)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@DocumentName", fileName);
                    cmd.Parameters.AddWithValue("@FilePath", destinationPath);
                    cmd.Parameters.AddWithValue("@UploadedAt", DateTime.Now);

                    cmd.ExecuteNonQuery();
                }

                _auditTrailService.LogAction(userId, "Upload", "Documents", 0, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during document upload: " + ex.Message);
            }
        }

        // Method to download a document
        public void DownloadDocument(int userId, Document document, string destinationPath)
        {
            try
            {
                if (File.Exists(document.FilePath))
                {
                    string destinationFilePath = Path.Combine(destinationPath, document.DocumentName);

                    if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }

                    File.Copy(document.FilePath, destinationFilePath, overwrite: true);

                    _auditTrailService.LogAction(userId, "Download", "Documents", document.DocumentId, document.DocumentName);
                }
                else
                {
                    throw new FileNotFoundException($"Document not found at {document.FilePath}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during document download: " + ex.Message);
                throw;
            }
        }

        // Method to delete a document
        public void DeleteDocument(int userId, int documentId, string DocumentName)
        {
            string documentName = string.Empty;
            string filePath = string.Empty;

            try
            {
                using (MySqlConnection conn = Database.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT DocumentName, FilePath FROM Document WHERE DocumentId = @DocumentId";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DocumentId", documentId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            documentName = reader["DocumentName"].ToString();
                            filePath = reader["FilePath"].ToString();
                        }
                    }
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);

                    using (MySqlConnection conn = Database.GetConnection())
                    {
                        conn.Open();
                        string query = "DELETE FROM Documents WHERE DocumentId = @DocumentId";
                        MySqlCommand cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@DocumentId", documentId);
                        cmd.ExecuteNonQuery();
                    }

                    _auditTrailService.LogAction(userId, "Delete", "Documents", documentId, documentName);
                }
                else
                {
                    Console.WriteLine($"File not found at {filePath}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during document deletion: " + ex.Message);
            }
        }
    }
}
