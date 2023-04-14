using Microsoft.VisualBasic;
using System.IO;
using System.IO.Ports;

namespace Mehrbod_IoT
{
    public partial class IoT_ControlPanel : Form
    {
        public static readonly List<string> SupportedBaudRates = new List<string>
        {
            "300",
            "600",
            "1200",
            "2400",
            "4800",
            "9600",
            "19200",
            "38400",
            "57600",
            "115200",
            "230400",
            "460800",
            "921600"
        };

        protected SerialPort serialPort_MehrbodIoT = new();

        public IoT_ControlPanel()
        {
            InitializeComponent();

            
        }

        private void IoT_ControlPanel_Load(object sender, EventArgs e)
        {
            UpdateUI_SerialPort();
        }

        private void button_SerialPort_Settings_Click(object sender, EventArgs e)
        {
            try
            {
                serialPort_MehrbodIoT.PortName = IoT_ComboInput.RequestInput("تنظیمات درگاه اتصال", "لطفا نام درگاه را انتخاب کنید:", SerialPort.GetPortNames(), Mehrbod_IoT.Properties.Resources.img_SerialPort_Name, serialPort_MehrbodIoT.PortName);
                UpdateUI_SerialPort();
                serialPort_MehrbodIoT.BaudRate = int.Parse(IoT_ComboInput.RequestInput("تنظیمات درگاه اتصال", "لطفاً سرعت تبادل داده با درگاه را انتخاب کنید (بیت بر ثانیه).", SupportedBaudRates.ToArray(), Properties.Resources.img_SerialPort_BaudRate, serialPort_MehrbodIoT.BaudRate.ToString()));
                UpdateUI_SerialPort();
                serialPort_MehrbodIoT.Parity = (Parity)Enum.Parse(typeof(Parity), IoT_ComboInput.RequestInput("تنظیمات درگاه اتصال", "لطفاً بیت توازن درگاه را انتخاب کنید:", Enum.GetNames(typeof(Parity)), Properties.Resources.img_SerialPort_Parity, serialPort_MehrbodIoT.Parity.ToString()));
                UpdateUI_SerialPort();
                serialPort_MehrbodIoT.StopBits = (StopBits)Enum.Parse(typeof(StopBits), IoT_ComboInput.RequestInput("تنظیمات درگاه اتصال", "لطفاً بیت توقف درگاه را انتخاب کنید:", Enum.GetNames(typeof(StopBits)), Properties.Resources.img_SerialPort_StopBits, serialPort_MehrbodIoT.StopBits.ToString()));
                UpdateUI_SerialPort();
                serialPort_MehrbodIoT.DataBits = int.Parse(IoT_ComboInput.RequestInput("تنظیمات درگاه اتصال", "لطفاً تعداد بیت‌های داده را مشخص کنید.", new string[] { "5", "6", "7", "8" }, Properties.Resources.img_SerialPort_DataBits, serialPort_MehrbodIoT.DataBits.ToString()));
                UpdateUI_SerialPort();
                serialPort_MehrbodIoT.Handshake = (Handshake)Enum.Parse(typeof(Handshake), IoT_ComboInput.RequestInput("تنظیمات درگاه اتصال", "لطفاً کد تشخیص خطای سخت‌افزار را انتخاب کنید:", Enum.GetNames(typeof(Handshake)), Properties.Resources.img_SerialPort_Handshake, serialPort_MehrbodIoT.Handshake.ToString()));
                UpdateUI_SerialPort();
            }
            catch(Exception ex)
            {
                MessageBox.Show("خطا در تنظیم درگاه اتصال:\n\n" + ex.Message, "خطای تنظیم درگاه!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
            }
        }

        /// <summary>
        /// Updates UI related to SerialPort settings.
        /// </summary>
        private void UpdateUI_SerialPort()
        {
            try
            {
                label_SerialPort_PortName.Text = serialPort_MehrbodIoT.PortName;
                label_SerialPort_BaudRate.Text = serialPort_MehrbodIoT.BaudRate.ToString();
                label_SerialPort_Parity.Text = serialPort_MehrbodIoT.Parity.ToString();
                label_SerialPort_StopBits.Text = serialPort_MehrbodIoT.StopBits.ToString();
                label_SerialPort_DataBits.Text = serialPort_MehrbodIoT.DataBits.ToString();
                label_SerialPort_Handshake.Text = serialPort_MehrbodIoT.Handshake.ToString();
            }
            catch(Exception) { }
        }
    }
}