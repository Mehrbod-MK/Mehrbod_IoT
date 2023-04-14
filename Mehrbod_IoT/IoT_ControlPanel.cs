using System.IO;
using System.IO.Ports;

namespace Mehrbod_IoT
{
    public partial class IoT_ControlPanel : Form
    {
        protected SerialPort serialPort_MehrbodIoT = new();

        public IoT_ControlPanel()
        {
            InitializeComponent();

            
        }

        private void IoT_ControlPanel_Load(object sender, EventArgs e)
        {
            
        }
    }
}