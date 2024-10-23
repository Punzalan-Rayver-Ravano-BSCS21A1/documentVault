using DocumentManagementSystem.Models;
using DocumentManagementSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DocumentManagementSystem
{
    public partial class UserManagementForm : Form
    {
        private List<User> users;  // List to hold users

        public UserManagementForm()
        {
            InitializeComponent();
            LoadUsersFromService();
        }

        // Method to load users from the UserService
        private void LoadUsersFromService()
        {
            UserService userService = new UserService();
            try
            {
                users = userService.GetAllUsers(); // Retrieve users from the database
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving users: {ex.Message}");
            }
        }

        // Method to load users into DataGridView
        private void LoadUsers()
        {
            dataGridViewUsers.Rows.Clear();  // Clear existing rows
            foreach (var user in users)
            {
                dataGridViewUsers.Rows.Add(user.Username, user.IsLocked ? "Locked" : "Unlocked");
            }
            lblTotalUsers.Text = $"Total Users: {users.Count}";
        }


        // Lock the selected user
        private async void btnLock_Click(object sender, EventArgs e)
        {
            if (dataGridViewUsers.SelectedRows.Count == 0) return;  // No user selected

            var selectedUsername = dataGridViewUsers.SelectedRows[0].Cells[0].Value.ToString();
            var selectedUser = users.FirstOrDefault(u => u.Username == selectedUsername);
            if (selectedUser != null && !selectedUser.IsLocked)
            {
                selectedUser.IsLocked = true;
                UserService userService = new UserService();

                try
                {
                    await userService.LockUser(selectedUser.UserId); // Lock user in the database
                    LoadUsers();  // Refresh the user list only after successful lock
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error locking user: {ex.Message}");
                }
            }
            else if (selectedUser != null && selectedUser.IsLocked)
            {
                MessageBox.Show($"{selectedUsername} is already locked.");
            }
        }

        // Unlock the selected user
        private async void btnUnlock_Click(object sender, EventArgs e)
        {
            if (dataGridViewUsers.SelectedRows.Count == 0) return;  // No user selected

            var selectedUsername = dataGridViewUsers.SelectedRows[0].Cells[0].Value.ToString();
            var selectedUser = users.FirstOrDefault(u => u.Username == selectedUsername);
            if (selectedUser != null && selectedUser.IsLocked)
            {
                selectedUser.IsLocked = false;
                UserService userService = new UserService();

                try
                {
                    await userService.UnlockUser(selectedUser.UserId); // Unlock user in the database
                    LoadUsers();  // Refresh the user list only after successful unlock
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error unlocking user: {ex.Message}");
                }
            }
            else if (selectedUser != null && !selectedUser.IsLocked)
            {
                MessageBox.Show($"{selectedUsername} is already unlocked.");
            }
        }

        private void UserManagementForm_Load(object sender, EventArgs e)
        {
            // You can initialize additional components here if needed
        }

        private void dataGridViewUsers_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridViewUsers.SelectedRows.Count > 0)
            {
                var selectedUsername = dataGridViewUsers.SelectedRows[0].Cells[0].Value.ToString();
                var selectedUser = users.FirstOrDefault(u => u.Username == selectedUsername);
                btnLock.Enabled = selectedUser != null && !selectedUser.IsLocked;
                btnUnlock.Enabled = selectedUser != null && selectedUser.IsLocked;
            }
        }

        private void lblTotalUsers_Click(object sender, EventArgs e)
        {
            // Handle label click if needed
        }

        private void dataGridViewUsers_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
