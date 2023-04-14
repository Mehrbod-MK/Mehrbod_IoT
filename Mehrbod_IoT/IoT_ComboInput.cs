using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mehrbod_IoT
{
    public partial class IoT_ComboInput : Form
    {
        public IoT_ComboInput()
        {
            InitializeComponent();
        }

        public IoT_ComboInput(string[] comboItems, string defaltResponse = "")
        {
            InitializeComponent();

            comboBox_Options.Items.Clear();
            comboBox_Options.Items.AddRange(comboItems);
            comboBox_Options.Text = defaltResponse;
        }

        private void IoT_ComboInput_Load(object sender, EventArgs e)
        {
            
        }

        public static string RequestInput(string windowTitle, string promptText, string[] comboItems, Image? pic = null, string defaultResponse = "")
        {
            IoT_ComboInput comboInput = new IoT_ComboInput(comboItems, defaultResponse);
            comboInput.Text = windowTitle;
            comboInput.label_PromptText.Text = promptText;
            if (pic != null)
                comboInput.pictureBox_Pic.Image = pic;

            if (comboInput.ShowDialog() == DialogResult.OK)
                return comboInput.comboBox_Options.Text;
            else
                return defaultResponse;
        }
    }
}
