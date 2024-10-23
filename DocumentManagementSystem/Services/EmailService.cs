using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace DocumentManagementSystem.Services // Replace with your actual namespace
{
    public class EmailService
    {
        private readonly string _apiKey;

        public EmailService()
        {
            //_apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            _apiKey = "";

            // Check if the API key is null or empty
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("SENDGRID_API_KEY environment variable is not set.");

                
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent, string verificationPin)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("karlnathanr2@gmail.com", "DocuVault"); // Your email
            var to = new EmailAddress(toEmail);

            // Update the plainTextContent and htmlContent to include the PIN
            plainTextContent += $" Your Verification PIN is: {verificationPin}";
            htmlContent += $"Your Verification PIN is: <strong>{verificationPin}</strong>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            // Check the response for success/failure
            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                Console.WriteLine("Email sent successfully."); // Email accepted for processing
            }
            else
            {
                // Log error for other statuses
                Console.WriteLine($"Error sending email: {response.StatusCode}"); // Consider using logging here
            }
        }
    }
}
