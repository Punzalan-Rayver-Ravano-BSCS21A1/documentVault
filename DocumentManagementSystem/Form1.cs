using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DocumentManagementSystem
{
    public partial class Form1 : Form
    {
        // Store the logged-in user ID
        private int _loggedInUserId = 0;
        private string _verificationPin; // The verification PIN
        private DateTime _pinGeneratedTime; // The time the PIN was generated
        private readonly TimeSpan _pinValidityDuration = TimeSpan.FromMinutes(5);

        public Form1()
        {
            InitializeComponent();
            InitializeAuditTrailListView();
        }

        private async void btnLogin_Click_1(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            UserService userService = new UserService();

            // Await the Authenticate method
            User authenticatedUser = await userService.Authenticate(username, password);

            if (authenticatedUser != null)
            {
                if (authenticatedUser.IsLocked)
                {
                    MessageBox.Show("Your account is locked. Please contact an administrator.");
                    txtUsername.Clear();
                    txtPassword.Clear();
                    return; // Exit if account is locked
                }

                // Handle successful login
                MessageBox.Show("Login successful!");

                if (authenticatedUser.IsAdmin)
                {
                    MessageBox.Show("Admin login successful!");
                    UserManagementForm userManagementForm = new UserManagementForm();
                    userManagementForm.ShowDialog();
                }
                else
                {
                    LoadDocuments();
                    LoadAuditTrail();
                }
            }
            else
            {
                txtUsername.Clear();
                txtPassword.Clear();
                MessageBox.Show("Invalid username or password.");
            }
        }


        private void btnUpload_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Documents|*.pdf;*.doc;*.ppt;*.csv;*.xls";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(filePath);

                    // Ensure the user is logged in
                    if (_loggedInUserId != 0)
                    {
                        // Call the upload method
                        DocumentService documentService = new DocumentService();
                        documentService.UploadDocument(_loggedInUserId, fileName, filePath);
                        LoadDocuments(); // Refresh document list after upload
                        LoadAuditTrail();
                    }
                    else
                    {
                        MessageBox.Show("Please log in before uploading documents.");
                    }
                }
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (lvDocuments.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = lvDocuments.SelectedItems[0];
                string documentName = selectedItem.Text;
                string filePath = selectedItem.SubItems[2].Text; // Assuming this is the file path

                // Create and configure the SaveFileDialog
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.FileName = documentName; // Set the default file name
                    saveFileDialog.Filter = "Documents|*.pdf;*.doc;*.ppt;*.csv;*.xls"; // Set the filter for file types

                    // Show the dialog and check if the user clicked 'Save'
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Document document = new Document { DocumentName = documentName, FilePath = filePath };
                        DocumentService documentService = new DocumentService();

                        try
                        {
                            // Pass the logged-in user ID and the directory chosen by the user to the download method
                            documentService.DownloadDocument(_loggedInUserId, document, Path.GetDirectoryName(saveFileDialog.FileName));
                            MessageBox.Show("Download initiated successfully.");
                            LoadAuditTrail();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error during download: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a document to download.");
            }
        }


        private void LoadDocuments()
        {
            // Ensure the user is logged in
            if (_loggedInUserId != 0)
            {
                DocumentService documentService = new DocumentService();
                List<Document> documents = documentService.GetDocumentsByUser(_loggedInUserId);

                lvDocuments.Items.Clear(); // Clear existing items
                foreach (var doc in documents)
                {
                    ListViewItem item = new ListViewItem(new[] { doc.DocumentName, doc.UploadedAt.ToString(), doc.FilePath });
                    item.Tag = doc.DocumentId; // Store DocumentId in the Tag property
                    lvDocuments.Items.Add(item);
                }
            }
            else
            {
                MessageBox.Show("Please log in to view documents.");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtRegisterUsername.Text.Trim();
            string password = txtRegisterPassword.Text.Trim();
            string confirmPassword = txtConfirmPassword.Text.Trim();
            string email = txtEmail.Text.Trim(); // Get email input

            // Validate inputs
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("All fields are required.");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Passwords do not match.");
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Please enter a valid email address.");
                return;
            }

            // Password strength validation
            if (!IsValidPassword(password))
            {
                MessageBox.Show("Password must be at least 8 characters long and contain at least one number.");
                return;
            }

            UserService userService = new UserService();

            // Check if username already exists
            if (userService.DoesUserExist(username))
            {
                MessageBox.Show("Username already exists. Please choose another.");
                return;
            }

            try
            {
                // Register the new user
                userService.RegisterUser(username, password, email);
                MessageBox.Show("Registration successful! You can now log in.");

                // Clear input fields
                txtRegisterUsername.Clear();
                txtRegisterPassword.Clear();
                txtEmail.Clear();
                txtConfirmPassword.Clear();
            }
            catch (Exception ex)
            {
                // Check if the exception is due to email already existing
                if (ex.Message.Contains("Email already exists"))
                {
                    MessageBox.Show("Email already exists. Please use a different email.");
                }
                else
                {
                    MessageBox.Show("An error occurred during registration. Please try again.");
                }
            }
        }

        private bool IsValidPassword(string password)
        {
            // Check that the password is at least 8 characters long
            if (password.Length < 8)
            {
                return false;
            }

            // Check for at least one number
            return System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]");
        }


        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            if (_loggedInUserId != 0)
            {
                string currentPassword = txtCurrentPassword.Text.Trim();
                string newPassword = txtNewPassword.Text.Trim();
                string confirmPassword = txtConfirmNewPassword.Text.Trim();

                // Validate inputs
                if (string.IsNullOrWhiteSpace(currentPassword) ||
                    string.IsNullOrWhiteSpace(newPassword) ||
                    string.IsNullOrWhiteSpace(confirmPassword))
                {
                    MessageBox.Show("All fields are required.");
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("New passwords do not match.");
                    return;
                }

                // Password strength validation for new password
                if (!IsValidPassword(newPassword))
                {
                    MessageBox.Show("New password must be at least 8 characters long and contain at least one number.");
                    return;
                }

                UserService userService = new UserService();

                // Attempt to change the password
                try
                {
                    if (userService.ChangePassword(_loggedInUserId, currentPassword, newPassword))
                    {
                        MessageBox.Show("Password changed successfully.");
                    }
                    else
                    {
                        MessageBox.Show("Current password is incorrect.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Password change failed: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please log in before changing your password.");
                txtCurrentPassword.Clear();
                txtNewPassword.Clear();
                txtConfirmNewPassword.Clear();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (lvDocuments.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = lvDocuments.SelectedItems[0];
                int documentId = Convert.ToInt32(selectedItem.Tag); // Assuming you set the DocumentId as the Tag
                string filePath = selectedItem.SubItems[2].Text; // Assuming this is the file path

                DocumentService documentService = new DocumentService();

                try
                {
                    // Call the DeleteDocument method using _loggedInUserId
                    documentService.DeleteDocument(_loggedInUserId, documentId, filePath);
                    MessageBox.Show("Document deleted successfully.");
                    LoadDocuments(); // Refresh the document list after deletion
                    LoadAuditTrail();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during deletion: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please select a document to delete.");
            }
        }


        private void btnLogout_Click(object sender, EventArgs e)
        {
            // Reset the logged-in user ID
            _loggedInUserId = 0;

            // Clear the document list
            lvDocuments.Items.Clear();

            lvAuditTrail.Items.Clear(); // Clear existing items

            // Optionally, show a message box or update the UI to indicate the user has logged out
            MessageBox.Show("You have logged out successfully.");
            txtUsername.Clear();
            txtPassword.Clear();
        }

        private async void btnSendEmail_Click(object sender, EventArgs e)
        {
            string recipientEmail = txtSendEmail.Text.Trim(); // Get the input and trim whitespace

            UserService userService = new UserService();

            // Check if the email is registered
            if (await userService.IsEmailRegisteredAsync(recipientEmail))
            {
                _verificationPin = GenerateVerificationPin(); // Generate a random PIN
                _pinGeneratedTime = DateTime.Now; // Store the generation time

                EmailService emailService = new EmailService();
                try
                {
                    await emailService.SendEmailAsync(recipientEmail, "VERIFICATION PIN", "OOP DMS PIN", "<strong></strong>", _verificationPin);
                    MessageBox.Show("Email sent with your verification PIN!");
                }
                catch (Exception ex)
                {
                    // Log or handle the error
                    MessageBox.Show($"Error sending email: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Email address not registered.");
            }
        }



        // Method to generate a random 6-digit PIN
        private string GenerateVerificationPin()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // Generates a PIN between 100000 and 999999
        }

        // Simple email validation method (optional)
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false; // Early exit for null or whitespace
            }

            // Regular expression for validating an email address
            var emailRegex = new System.Text.RegularExpressions.Regex(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Check if the email matches the regex pattern
            if (!emailRegex.IsMatch(email))
            {
                return false;
            }

            try
            {
                // Use MailAddress to ensure it's a well-formed email
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }


        private async void btnVerificationChangePassword_Click(object sender, EventArgs e)
        {
            string enteredPin = txtVerificationPin.Text.Trim();
            string newPassword = txtVerificationNewPassword.Text.Trim();
            string confirmNewPassword = txtConfirmVerificationNewPassword.Text.Trim();

            // Check if the PIN is still valid
            if (DateTime.Now - _pinGeneratedTime <= _pinValidityDuration)
            {
                if (enteredPin == _verificationPin)
                {
                    if (newPassword == confirmNewPassword)
                    {
                        UserService userService = new UserService();

                        // Here you should ideally retrieve the user's email or user ID to update their password
                        string userEmail = txtSendEmail.Text.Trim(); // Assume this field is available

                        // Reset the password in the database
                        bool isPasswordChanged = await userService.ResetPasswordAsync(userEmail, newPassword);

                        if (isPasswordChanged)
                        {
                            MessageBox.Show("Password changed successfully!");
                        }
                        else
                        {
                            MessageBox.Show("Failed to change the password. Please try again.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Passwords do not match. Please try again.");
                    }
                }
                else
                {
                    MessageBox.Show("Invalid PIN. Please try again.");
                }
            }
            else
            {
                MessageBox.Show("The PIN has expired. Please request a new one.");
            }
        }


        private void LoadAuditTrail()
        {
            AuditTrailService auditTrailService = new AuditTrailService();
            List<AuditTrail> auditTrails = auditTrailService.GetAuditTrailByUser(_loggedInUserId);

            lvAuditTrail.Items.Clear(); // Clear existing items

            foreach (var audit in auditTrails)
            {
                ListViewItem item = new ListViewItem(audit.Action);
                item.SubItems.Add(audit.Details); // Add details as the second subitem
                item.SubItems.Add(audit.Timestamp.ToString("g")); // General date/time pattern
                lvAuditTrail.Items.Add(item);
            }
        }

        private void InitializeAuditTrailListView()
        {
            lvAuditTrail.Columns.Add("Action", 100, HorizontalAlignment.Left);
            lvAuditTrail.Columns.Add("Details", 300, HorizontalAlignment.Left); // Details column
            lvAuditTrail.Columns.Add("Timestamp", 150, HorizontalAlignment.Left);
        }

        private void lvDocuments_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void txtRegisterUsername_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
