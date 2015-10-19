using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace PowerPlanChanger
{
    public partial class AddServiceDialog : Form
    {
        public IEnumerable<Service> SelectedServices { get; private set; }

        public AddServiceDialog()
        {
            InitializeComponent();

            Service.RefreshAllProperties();
            serviceListBox.DataSource = Service.GetServices();
            serviceListBox.ValueMember = null;
            serviceListBox.DisplayMember = "ServiceName";
        }

        private IEnumerable<Service> GetCheckedServices()
        {
            return serviceListBox.CheckedItems.Cast<Service>();
        }

        private Service GetSelectedService()
        {
            return (Service)serviceListBox.SelectedItem;
        }

        private void serviceListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string dbr = Environment.NewLine + Environment.NewLine;
            var service = GetSelectedService();
            string info = "Service name: " + service.ServiceName + dbr + 
                          "Display name: " + service.DisplayName;
            string desc = service.Description;
            if (desc != string.Empty) info += dbr + "Service description:" + dbr + desc;
            serviceInfoTextBox.Text = info;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            SelectedServices = GetCheckedServices();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
