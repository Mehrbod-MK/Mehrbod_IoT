using Microsoft.VisualBasic;
using System.IO;
using System.IO.Ports;
using Telegram.Bot;

namespace Mehrbod_IoT
{
    public partial class IoT_ControlPanel : Form
    {
        public const uint _PROFILE_HEADER_VERSION = 1;

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
        protected TelegramBotClient? botClient = null;

        protected List<string> list_Authorized_PhoneNumbers = new List<string>();
        protected List<long> list_Authorized_ChatIDs = new List<long>();

        protected Task? task_TelegramPolling;

        public IoT_ControlPanel()
        {
            InitializeComponent();

            
        }

        private void IoT_ControlPanel_Load(object sender, EventArgs e)
        {
            UpdateUI_SerialPort();

            if(!IoT_Load_Profile())
            {
                if (MessageBox.Show("به نرم‌افزار اینترنت اشیاء مهربد ملاکاظمی خوبده خوش آمدید!\n\nبه نظر می‌رسد که برای اولین بار است که از نرم‌افزار استفاده می‌کنید.\nتنظیمات نرم‌افزار در فایل پیکربندی ذخیره خواهند شد. آیا مایل به ساخت یک فایل پیکربندی جدید هستید؟", "خوش آمدید!", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) == DialogResult.Yes)
                    IoT_Save_Profile();
            }
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

        private void button_SerialPort_Connect_Click(object sender, EventArgs e)
        {
            if(!serialPort_MehrbodIoT.IsOpen)
            {
                groupBox_Settings_SerialPort.Enabled = false;
                button_SerialPort_Connect.TextAlign = ContentAlignment.MiddleRight;
                button_SerialPort_Connect.Text = "در حال اتصال";

                Task.Run(() => {
                    try
                    {
                        // Thread.Sleep(500);

                        serialPort_MehrbodIoT.Open();
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(() => {
                            MessageBox.Show("خطا در اتصال به درگاه. جزئیات خطا به شرح ذیل می‌باشد:\n\n" + ex.Message, "خطا در باز کردن اتصال درگاه!", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                        });
                    }
                })
                    .ContinueWith((x) => {
                        if (serialPort_MehrbodIoT.IsOpen)
                        {
                            Invoke(() =>
                            {
                                groupBox_Settings_Internet.Enabled = true;
                                groupBox_Things.Enabled = true;
                                groupBox_Reports.Enabled = true;

                                groupBox_Settings_SerialPort.Enabled = true;
                                button_SerialPort_Connect.TextAlign = ContentAlignment.MiddleRight;
                                button_SerialPort_Connect.Text = "قطع اتصال";
                                button_SerialPort_Connect.Image = Properties.Resources.ico_NO_30x30;
                                button_SerialPort_Connect.BackColor = Color.NavajoWhite;
                                button_SerialPort_Settings.Enabled = false;
                                button_SerialPort_AutoConnect.Enabled = false;
                            });
                        }
                        else
                        {
                            Invoke(() =>
                            {
                                groupBox_Settings_SerialPort.Enabled = true;
                                button_SerialPort_Connect.TextAlign = ContentAlignment.MiddleCenter;
                                button_SerialPort_Connect.Text = "اتصال";
                            });
                        }
                    });
            }
            else
            {

            }
        }

        /// <summary>
        /// Polls server.
        /// </summary>
        /// <returns>This task returns nothing and runs asynchronously.</returns>
        protected async Task Begin_TelegramProcess()
        {
            while(true)
            {
                if (botClient == null)
                    continue;

                try
                {
                    int offset = 0;

                    foreach(var update in await botClient.GetUpdatesAsync(offset))
                    {
                        // Discard update from web server.
                        offset = update.Id + 1;

                        // Check update data.
                        // Check if input message is "text" and it is a /start message.
                        if(update.Message != null)
                            if (update.Message.Text != null)
                                if (update.Message.Text.ToLower() == "/start")
                                {

                                }
                    }
                }
                catch(Exception ex) 
                {
                    IoT_Log("ارتباط با وب‌سرور با اختلال روبه‌رو شد:\t" + ex.Message);
                }
            }
        }

        private void button_Internet_EstablishConnection_Click(object sender, EventArgs e)
        {
            if(botClient == null)
            {
                groupBox_Settings_Internet.Enabled = false;
                button_Internet_EstablishConnection.Text = "در حال ارتباط با بستر اینترنت...";

                Task.Run(() =>
                {
                    try
                    {
                        botClient = new(textBox_InternetSettings_BotToken.Text.Trim());
                        string? userName = botClient.GetMeAsync().Result.Username;

                        Invoke(() =>
                        {
                            IoT_Log("ارتباط با وب‌سرور برقرار شد.", true);
                            IoT_Log(@"https://api.telegram.org/" + userName);

                            // Run Telegram polling process.
                            task_TelegramPolling = Task.Run(() => Begin_TelegramProcess());

                            groupBox_Settings_Internet.Enabled = true;
                            textBox_InternetSettings_BotToken.Hide();
                            button_Internet_EstablishConnection.Image = Properties.Resources.ico_NO_30x30;
                            button_Internet_EstablishConnection.Text = "قطع اتصال وب‌سرور";
                            button_Internet_EstablishConnection.BackColor = Color.NavajoWhite;
                        });
                    }
                    catch (Exception ex)
                    {
                        groupBox_Settings_Internet.Enabled = true;
                        button_Internet_EstablishConnection.Text = "ارسال درخواست HTTPS به وب‌سرور";

                        botClient = null;

                        Invoke(() =>
                        {
                            MessageBox.Show("خطا در ارسال درخواست HTTPS به وب‌سرور. جزئیات خطا به شرح ذیل می‌باشد:\n\n" + ex.Message, "خطا در ارتباط با بستر اینترنت!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                        });
                    }
                });
            }
        }

        private void IoT_Log(string message, bool appendDate = true, bool appendCR = true)
        {
            string prompt = "";
            if (appendDate)
                prompt += DateTime.Now.ToString() + '\t';

            prompt += message;

            if (appendCR)
                prompt += "\r\n";

            textBox_Log.AppendText(prompt);
        }

        private void IoT_Request_PhoneNumber()
        {
            string enteredNumber = IoT_ComboInput.RequestInput("اضافه کردن شماره تلفن جدید", "لطفا شماره همراه مجاز را وارد کنید.\nنمونه:  981234567890", new string[] { }, null, String.Empty);

            if (!String.IsNullOrEmpty(enteredNumber))
            {
                list_Authorized_PhoneNumbers.Add(enteredNumber);
            }
        }

        private bool IoT_Save_Profile(bool silent = false)
        {
            try
            {
                FileStream stream = new FileStream(Environment.CurrentDirectory + @"\mehrbod_iot.conf", FileMode.Create, FileAccess.Write);
                BinaryWriter binWriter = new BinaryWriter(stream);

                // Write header data.
                binWriter.Write('M');
                binWriter.Write('M');
                binWriter.Write('K');
                binWriter.Write(_PROFILE_HEADER_VERSION);

                // Write authorized phone numbers.
                binWriter.Write(list_Authorized_PhoneNumbers.Count);
                foreach (var authPhoneNum in list_Authorized_PhoneNumbers)
                    binWriter.Write(authPhoneNum);

                // Write authorized chat IDs.
                binWriter.Write(list_Authorized_ChatIDs.Count);
                foreach (var authChatID in list_Authorized_ChatIDs)
                    binWriter.Write(authChatID);

                binWriter.Close();

                return true;
            }
            catch(Exception ex)
            {
                if(!silent)
                    MessageBox.Show("خطا در نوشتن فایل پیکربندی برنامه. جزئیات خطا به شرح ذیل می‌باشد:\n\n" + ex.Message, "خطا در نوشتن فایل پیکربندی!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                return false;
            }
        }

        private bool IoT_Load_Profile(bool silent = true)
        {
            FileStream? stream;
            BinaryReader? binReader;

            try
            {
                stream = new FileStream(Environment.CurrentDirectory + @"\mehrbod_iot.conf", FileMode.Open, FileAccess.Read);
                binReader = new BinaryReader(stream);
            }
            catch(Exception ex)
            {
                if (!silent)
                    MessageBox.Show("خطا در دسترسی به فایل پیکربندی برنامه. جزئیات خطا به شرح ذیل می‌باشد:\n\n" + ex.Message, "خطا در نوشتن فایل پیکربندی!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                return false;
            }

            try
            {
                // Read header data.
                if (binReader.ReadChar() != 'M' || binReader.ReadChar() != 'M' || binReader.ReadChar() != 'K')
                    throw new Exception("هدر فایل پیکربندی آسیب دیده است!");
                uint config_ProfileVersion = binReader.ReadUInt32();

                // Read authorized phone numbers.
                list_Authorized_PhoneNumbers = new List<string>();
                int config_Count_AuthorizedPhoneNumbers = binReader.ReadInt32();
                for (int i = 1; i <= config_Count_AuthorizedPhoneNumbers; i++)
                    list_Authorized_PhoneNumbers.Add(binReader.ReadString());

                // Read authorized chat IDs.
                list_Authorized_ChatIDs = new List<long>();
                int config_Count_AuthorizedChatIDs = binReader.ReadInt32();
                for (int i = 1; i <= config_Count_AuthorizedChatIDs; i++)
                    list_Authorized_ChatIDs.Add(binReader.ReadInt64());

                binReader.Close();

                return true;
            }
            catch(Exception ex)
            {
                if(!silent)
                    MessageBox.Show("خطا در خواندن فایل پیکربندی برنامه. جزئیات خطا به شرح ذیل می‌باشد:\n\n" + ex.Message, "خطا در نوشتن فایل پیکربندی!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                stream.Dispose();
                return false;
            }
        }
    }
}