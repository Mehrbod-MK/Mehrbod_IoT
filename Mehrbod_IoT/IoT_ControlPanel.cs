using Microsoft.VisualBasic;
using System.IO;
using System.IO.Ports;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

using AForge.Video;
using AForge.Video.DirectShow;
using Telegram.Bot.Types;

using NAudio;
using NAudio.Wave;

namespace Mehrbod_IoT
{
    public partial class IoT_ControlPanel : Form
    {
        public const uint _PROFILE_HEADER_VERSION = 1;

        public const int _NUM_LEDS_ROWS = 8, _NUM_LEDS_COLS = 8;

        public static readonly ReplyKeyboardRemove _COMMAND_REMOVE_KEYBOARD = new ReplyKeyboardRemove();

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

        [Flags]
        public enum IoT_Device_Flags : ulong
        {
            DEVICE_Flag_None = 0,

            DEVICE_Flag_Detect_Sensor_PIR = 1 << 0,
            DEVICE_Flag_Init_WS2812 = 1 << 1,
            DEVICE_Flag_Init_SSD1306 = 1 << 2,
            DEVICE_Flag_Init_Ports = 1 << 3,
            DEVICE_Flag_Init_LEDs = 1 << 4,
            DEVICE_Flag_Init_Buzzer = 1 << 5,

            DEVICE_Flag_OnState_LED_RED = 1 << 6,
            DEVICE_Flag_OnState_LED_GREEN = 1 << 7,
            DEVICE_Flag_OnState_LED_BLUE = 1 << 8,
        }

        protected SerialPort serialPort_MehrbodIoT = new();
        protected TelegramBotClient? botClient = null;

        protected List<string> list_Authorized_PhoneNumbers = new List<string>();
        protected List<long> list_Authorized_ChatIDs = new List<long>();

        protected Task? task_TelegramPolling;
        protected Task? task_SerialPolling;

        protected IoT_Device_Flags device_Flags = 0;

        // WS2812 - Current Color.
        protected Color color_WS2812_Pixel = Color.White;

        // Camera capture.
        FilterInfoCollection? filterInfoCollection_Cameras;
        VideoCaptureDevice? videoCaptureDevice;

        int deviceIndex_Camera = -1;

        public IoT_ControlPanel()
        {
            InitializeComponent();

            // Initialize external devices.
            Initialize_ExternalDevices();

            // Initialize Event Handlers.
            Initailize_EventHandlers();
        }

        protected void Initailize_EventHandlers()
        {
            // Initialize WS2812 matrix button Click event handlers.
            for(int x = 0; x < _NUM_LEDS_COLS; x++)
                for(int y = 0; y < _NUM_LEDS_ROWS; y++)
                    ((Button)Controls.Find("button_Matrix_" + x.ToString() + y.ToString(), true)[0]).MouseDown += IoT_ButtonMatirx_ControlPanel_MouseDown;
        }

        private void IoT_ButtonMatirx_ControlPanel_MouseDown(object? sender, MouseEventArgs e)
        {
            int x = 0, y = 0;

            if (sender != null)
            {
                string[] butSplit = ((Button)sender).Name.Split('_');

                if (butSplit.Length > 2)
                {
                    if (int.TryParse(butSplit[2], out int xy))
                    {
                        y = xy % 10;
                        xy /= 10;
                        x = xy % 10;
                    }
                }
            }

            // Set color if "Mouse Left" button was pressed.
            if (e.Button == MouseButtons.Left)
            {
                _ = IoT_SerialPort_SendData_Async("WS2812 SET_PIXEL " + x.ToString() + " " + y.ToString() + " " + color_WS2812_Pixel.R + " " + color_WS2812_Pixel.G + " " + color_WS2812_Pixel.B);
            }

            // Clear color from pixel if "Right button" was pressed.
            else if(e.Button == MouseButtons.Right)
            {
                _ = IoT_SerialPort_SendData_Async("WS2812 SET_PIXEL " + x.ToString() + " " + y.ToString() + " 0 0 0");
            }
        }

        protected void Initialize_ExternalDevices()
        {
            // Initialize camera devices.
            Initialize_ExternalDevices_Cameras();

            // Initialize speakers (Playback devices).
            Initialize_ExternalDevices_PlaybackDevices();

            // Initialize microphones (Recording devices).
            Initialize_ExternalDevices_RecordingDevices();
        }

        protected void Initialize_ExternalDevices_PlaybackDevices()
        {
            بلندگوهاToolStripMenuItem.DropDownItems.Clear();

            for(int n = -1; n < WaveOut.DeviceCount; n++)
            {
                var playbackCaps = WaveOut.GetCapabilities(n);

                ToolStripMenuItem menuItem_PLaybackDevice = new ToolStripMenuItem()
                {
                    Text = playbackCaps.ProductName,
                    AutoToolTip = true,
                    Tag = n
                };

                بلندگوهاToolStripMenuItem.DropDownItems.Add(menuItem_PLaybackDevice);
            }
        }

        protected void Initialize_ExternalDevices_RecordingDevices()
        {
            میکروفونهاToolStripMenuItem.DropDownItems.Clear();

            for (int n = -1; n < WaveIn.DeviceCount; n++)
            {
                var playbackCaps = WaveIn.GetCapabilities(n);

                ToolStripMenuItem menuItem_PLaybackDevice = new ToolStripMenuItem()
                {
                    Text = playbackCaps.ProductName,
                    AutoToolTip = true,
                    Tag = n
                };

                میکروفونهاToolStripMenuItem.DropDownItems.Add(menuItem_PLaybackDevice);
            }
        }

        protected void Initialize_ExternalDevices_Cameras()
        {
            دوربینهاToolStripMenuItem.DropDownItems.Clear();

            filterInfoCollection_Cameras = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            int i = 0;
            foreach (FilterInfo device in filterInfoCollection_Cameras)
            {
                ToolStripMenuItem menuItem_Cameras = new ToolStripMenuItem()
                {
                    Text = device.Name,
                    AutoToolTip = true,
                    Tag = i++,
                };
                menuItem_Cameras.Click += (sender, e) =>
                {
                    ToolStripMenuItem? menu_Camera = sender as ToolStripMenuItem;
                    if (menu_Camera != null)
                    {
                        int tag = (int)menu_Camera.Tag;

                        deviceIndex_Camera = tag;
                    }

                    for (int a = 0; a < دوربینهاToolStripMenuItem.DropDownItems.Count; a++)
                        if (a == deviceIndex_Camera)
                            ((ToolStripMenuItem)دوربینهاToolStripMenuItem.DropDownItems[a]).Checked = true;
                        else
                            ((ToolStripMenuItem)دوربینهاToolStripMenuItem.DropDownItems[a]).Checked = false;
                };
                دوربینهاToolStripMenuItem.DropDownItems.Add(menuItem_Cameras);
            }
            if (i > 0)
            {
                دوربینهاToolStripMenuItem.Enabled = true;
                deviceIndex_Camera = 0;
                ((ToolStripMenuItem)دوربینهاToolStripMenuItem.DropDownItems[0]).Checked = true;
            }
            else
            {
                دوربینهاToolStripMenuItem.Enabled = false;
            }
        }

        private void IoT_ControlPanel_Load(object sender, EventArgs e)
        {
            string[] availablePorts = SerialPort.GetPortNames();
            serialPort_MehrbodIoT.PortName = (availablePorts.Length > 0) ? availablePorts[0] : serialPort_MehrbodIoT.PortName;

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

                        // Attach "DataReceived" event handler.
                        serialPort_MehrbodIoT.DataReceived += SerialPort_MehrbodIoT_DataReceived;

                        // Start SerialPort data processing.
                        task_SerialPolling = Task.Run(() => Begin_SerialPortProcess());
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
        /// Temporary line data received from USART.
        /// </summary>
        string tempLine = "";

        /// <summary>
        /// Input commands received via USART.
        /// </summary>
        Queue<string> iot_InputCommands = new Queue<string>();
        private void SerialPort_MehrbodIoT_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = serialPort_MehrbodIoT.BytesToRead;

            for(int i = 1; i <= bytesToRead; i++)
            {
                int b = serialPort_MehrbodIoT.ReadChar();
                char c = (char)b;

                if(b == 10 || b == 13)
                {
                    if (!String.IsNullOrEmpty(tempLine) && !String.IsNullOrWhiteSpace(tempLine))
                        iot_InputCommands.Enqueue(tempLine);
                    // MessageBox.Show(tempLine);
                    tempLine = "";
                }
                else
                {
                    tempLine += c;
                }
            }
        }

        /// <summary>
        /// Polls server.
        /// </summary>
        /// <returns>This task returns nothing and runs asynchronously.</returns>
        protected async Task Begin_TelegramProcess()
        {
            int offset = 0;

            while (true)
            {
                if (botClient == null)
                    continue;

                try
                {
                    foreach(var update in await botClient.GetUpdatesAsync(offset))
                    {
                        // Discard update from web server.
                        offset = update.Id + 1;

                        // Check update data.
                        // Check if input data is an inline callback query.
                        if(update.CallbackQuery != null)
                        {
                            await Task.Run(async () =>
                            {
                                try
                                {
                                    await Begin_Bot_InlineCallbackProcess(update.CallbackQuery);
                                }
                                catch(Exception ex)
                                {
                                    await botClient.AnswerCallbackQueryAsync(update.CallbackQuery.Id, "⚠ دستور نامعتبر.\n\n" + ex.Message, true);
                                }
                            });
                        }

                        // Check if input object is a message.
                        if(update.Message != null)
                        {
                            long chatID = update.Message.Chat.Id;

                            // Forbid forwarded messages.
                            if(update.Message.ForwardDate != null || update.Message.ForwardFrom != null || update.Message.ForwardFromChat != null || update.Message.ForwardFromMessageId != null || update.Message.ForwardSenderName != null || update.Message.ForwardSignature != null)
                            {
                                await IoT_Bot_Prompt_NoForwardingAllowed_Async(chatID, update.Message);
                                continue;
                            }

                            // Check if input message is "text" and it is a / start message.
                            if (update.Message.Text != null)
                            {
                                if (update.Message.Text.ToLower() == "/start")
                                {
                                    // Check if current ChatID is unauthorized!
                                    if (list_Authorized_ChatIDs.FindIndex(x => x == chatID) == -1)
                                        await IoT_Bot_Prompt_RequestAuthorization_Async(chatID, update.Message);
                                    else
                                    {
                                        // Prompt main menu.
                                        await IoT_Bot_Prompt_MainMenu_Async(chatID, update.Message);
                                    }
                                }
                            }

                            // Check if input message is a "Contact" object.
                            else if(update.Message.Contact != null)
                            {
                                // Check fake forwarded contact object.
                                if (update.Message.ReplyToMessage == null)
                                {
                                    await IoT_Bot_Prompt_NoForwardingAllowed_Async(chatID, update.Message);
                                    continue;
                                }

                                // Check if contact is unauthorized!
                                if(list_Authorized_ChatIDs.FindIndex(x => x == chatID) == -1)
                                {
                                    string contact_PhoneNumber = update.Message.Contact.PhoneNumber;

                                    MessageBox.Show(contact_PhoneNumber);

                                    // Check if phone number is unauthorized.
                                    if (list_Authorized_PhoneNumbers.FindIndex(x => x == contact_PhoneNumber) == -1)
                                        await IoT_Bot_Prompt_UnauthorizedPhoneNumber_Async(chatID, update.Message);
                                    else
                                    {
                                        list_Authorized_ChatIDs.Add(chatID);
                                        IoT_Save_Profile(false, true);
                                        await IoT_Bot_Prompt_AddedChatID_Async(chatID, update.Message);
                                    }
                                }
                            }
                        }

                    }
                }
                catch(Exception ex) 
                {
                    IoT_Log("ارتباط با وب‌سرور با اختلال روبه‌رو شد:\t" + ex.Message);
                    continue;
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

            if(InvokeRequired)
                Invoke(() => { textBox_Log.AppendText(prompt); });
            else
                textBox_Log.AppendText(prompt);
        }

        private bool IoT_Request_PhoneNumber()
        {
            string enteredNumber = IoT_ComboInput.RequestInput("اضافه کردن شماره تلفن جدید", "لطفا شماره همراه مجاز را وارد کنید.\nنمونه:  +989019681890", new string[] { }, null, String.Empty);

            if (!String.IsNullOrEmpty(enteredNumber))
            {
                if (list_Authorized_PhoneNumbers.FindIndex(x => x == enteredNumber) == -1)
                {
                    list_Authorized_PhoneNumbers.Add(enteredNumber);
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        private bool IoT_Save_Profile(bool confirmSave = false, bool silent = false)
        {
            if (confirmSave)
                if (MessageBox.Show("آیا مایل به ذخیره تغییرات هستید؟", "سؤال", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) == DialogResult.No)
                    return false;

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

        private void IoT_Generate_Menus_AuthorizedChatIDs()
        {
            چتهایمجازToolStripMenuItem.DropDownItems.Clear();

            foreach(var chatId in list_Authorized_ChatIDs)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem()
                {
                    AutoToolTip = true,
                    Text = chatId.ToString(),
                };

                menuItem.Click += (sender, e) =>
                {
                    if (MessageBox.Show("آیا از حذف شماره چت " + chatId.ToString() + " مطمئن هستید؟", "هشدار!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) == DialogResult.Yes)
                    {
                        list_Authorized_ChatIDs.Remove(chatId);
                        IoT_Save_Profile(true);
                    }
                };

                چتهایمجازToolStripMenuItem.DropDownItems.Add(menuItem);
            }
        }

        private void IoT_Generate_Menus_AuthorizedPhoneNumbers()
        {
            شمارهتلفنهایمجازToolStripMenuItem.DropDownItems.Clear();

            foreach (var phoneNum in list_Authorized_PhoneNumbers)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem()
                {
                    AutoToolTip = true,
                    Text = phoneNum,
                };

                menuItem.Click += (sender, e) =>
                {
                    if (MessageBox.Show("آیا از حذف شماره تلفن " + phoneNum + " مطمئن هستید؟", "هشدار!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) == DialogResult.Yes)
                    {
                        list_Authorized_PhoneNumbers.Remove(phoneNum);
                        IoT_Save_Profile(true);
                    }
                };

                شمارهتلفنهایمجازToolStripMenuItem.DropDownItems.Add(menuItem);
            }

            شمارهتلفنهایمجازToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem menuItem_AddPhoneNumber = new ToolStripMenuItem()
            {
                AutoToolTip = true,
                Text = "اضافه کردن شماره تلفن جدید",
            };
            menuItem_AddPhoneNumber.Click += (sender, e) =>
            {
                if(IoT_Request_PhoneNumber())
                    IoT_Save_Profile(true);
            };
            شمارهتلفنهایمجازToolStripMenuItem.DropDownItems.Add(menuItem_AddPhoneNumber);
        }

        private async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_RequestAuthorization_Async(long chatID, Telegram.Bot.Types.Message replyTo)
        {
            string promptText_ReqAuth = "سلام و درود ویژه خدمت شما کاربر گرامی. 👋\n\n" +
                "🙏 به سامانه اینرنت اشیاء مهربد ملاکاظمی خوبده خوش آمدید.\n" +
                "⛔ نشست کاربری فعلی شما در سامانه مجاز به فعالیت نمی‌باشد.\n" +
                "👇 با زدن دکمه ذیل، شماره تلفن همراه شما بررسی شده و اگر مجاز به کار با سامانه بودید، دسترسی مربوطه به شما داده خواهد شد.";

            IoT_Log("[" + chatID.ToString() + "]\t" + "نشست کاربری غیرمجاز دکمه شروع را زد.");

            if (botClient != null)
                return await botClient.SendTextMessageAsync(chatID, promptText_ReqAuth, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, replyTo.MessageId, true, new ReplyKeyboardMarkup(KeyboardButton.WithRequestContact("📲 ارسال اطلاعات تماس به وب‌سرور")));
            else
                return null;
        }

        private async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_UnauthorizedPhoneNumber_Async(long chatID, Telegram.Bot.Types.Message replyTo)
        {
            string promptText_UnauthPhoneNum = "⛔ متأسفانه، شماره تلفن شما مجاز به کار با سامانه نیست.";

            IoT_Log("[" + chatID.ToString() + "]\t" + "شماره تلفن غیرمجاز دریافت شد!");

            if (botClient != null)
                return await botClient.SendTextMessageAsync(chatID, promptText_UnauthPhoneNum, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, replyTo.MessageId, true, _COMMAND_REMOVE_KEYBOARD);
            else
                return null;
        }

        private async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_NoForwardingAllowed_Async(long chatID, Telegram.Bot.Types.Message replyTo)
        {
            string promptText_UnauthPhoneNum = "⛔ سامانه، پیام‌های هدایت شده (فوروارد شده) را نمی‌پذیرد!";

            IoT_Log("[" + chatID.ToString() + "]\t" + "عدم پذیرش پیام هدایت شده (فوروارد شده).");

            if (botClient != null)
                return await botClient.SendTextMessageAsync(chatID, promptText_UnauthPhoneNum, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, replyTo.MessageId, true, _COMMAND_REMOVE_KEYBOARD);
            else
                return null;
        }

        private async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_AddedChatID_Async(long chatID, Telegram.Bot.Types.Message replyTo)
        {
            string promptText_AddedChatID = "✅ شماره تلفن با موفقیت تأیید و نشست کاربری شما به شماره <pre>" + chatID.ToString() + "</pre> در سامانه ثبت شد.\n\n👈 با استفاده از دستور /start می‌توانید به پنل اینترنت اشیاء دسترسی داشته باشید.";

            IoT_Log("[" + chatID.ToString() + "]\t" + "تأیید شماره تلفن و اضافه شدن نشست کاربری.");

            if (botClient != null)
                return await botClient.SendTextMessageAsync(chatID, promptText_AddedChatID, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, replyTo.MessageId, true, _COMMAND_REMOVE_KEYBOARD);
            else
                return null;
        }

        private void اینترنتToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            IoT_Generate_Menus_AuthorizedPhoneNumbers();
            IoT_Generate_Menus_AuthorizedChatIDs();
        }

        private void حذفپیکربندیToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("آیا از حذف تمامی تنظیمات برنامه، اعم از اطلاعات تماس‌ها و چت‌ها هستید؟\nاین عملیات غیر قابل بازگشت خواهد بود.", "هشدار!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) == DialogResult.Yes)
            {
                list_Authorized_ChatIDs.Clear();
                list_Authorized_PhoneNumbers.Clear();

                try
                {
                    System.IO.File.Delete(Environment.CurrentDirectory + @"\mehrbod_iot.conf");
                    MessageBox.Show("فایل پیکربندی با موفقیت حذف شد و تنظیمات از نو شدند.", "عملیات موفق", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                }
                catch(Exception ex) 
                {
                    MessageBox.Show("خطا در تازه‌سازی تنظیمات پیکربندی. جزئیات خطا به شرح ذیل می‌باشد:\n\n" + ex.Message, "خطا در انجام عملیات", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);
                }
            }
        }

        private async Task<bool> IoT_SerialPort_SendData_Async(string message, bool addHeader = true, bool appendCR = true, bool silent = false)
        {
            try
            {
                string sendData = "";
                /*if(addHeader)
                    serialPort_MehrbodIoT.Write("MEHRBOD_IOT " + message);
                else
                    serialPort_MehrbodIoT.Write(message);
                if (appendCR)
                    serialPort_MehrbodIoT.WriteLine("");*/

                if (addHeader)
                    sendData = "MEHRBOD_IOT " + message;
                else
                    sendData = message;

                foreach(char c in sendData)
                {
                    serialPort_MehrbodIoT.Write(c.ToString());
                    await Task.Delay(TimeSpan.FromMilliseconds(1));
                }
                if (appendCR)
                    serialPort_MehrbodIoT.WriteLine("");

                // Dummy delay.
                await Task.Delay(1);

                return true;
            }
            catch(Exception ex)
            {
                if(!silent)
                    MessageBox.Show("خطا در ارسال اطلاعات به درگاه. جزئیات خطا به شرح ذیل می‌باشد:\n\n" + ex.Message, "خطای ارسال اطلاعات به درگاه", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign);
                IoT_Log("خطا در ارسال اطلاعات به درگاه:  " + ex.Message);

                return false;
            }
        }

        private async Task Begin_IoT_WS2812_SetPixel_Async(int x, int y, int r, int g, int b, string handle = "")
        {
            await IoT_SerialPort_SendData_Async("WS2812 SET_PIXEL " + x.ToString() + " " + y.ToString() + " " + r.ToString() + " " + g.ToString() + " " + b.ToString() + " " + handle);
        }
        private async Task Begin_IoT_WS2812_SetPixel_Async(int x, int y, Color col, string handle = "")
        {
            await IoT_SerialPort_SendData_Async("WS2812 SET_PIXEL " + x.ToString() + " " + y.ToString() + " " + col.R.ToString() + " " + col.G.ToString() + " " + col.B.ToString() + " " + handle);
        }

        private async Task Begin_SerialPortProcess()
        {
            while (true)
            {
                // Dummy delay.
                await Task.Delay(1);

                // Check if there is data available for reading.
                if(iot_InputCommands.TryDequeue(out string? lineData))
                {
                    if(lineData != null)
                    {
                        string[] args = lineData.Split(' ');

                        if(args.Length > 0)
                        {
                            // Check reflected commands.
                            if (args[0] == "MEHRBOD_IOT")
                            {
                                // Set device flags.
                                if (args[1] == "SET_DEV_FLAG")
                                {
                                    // MessageBox.Show("");
                                    // Check PIR sensor.
                                    if (args[2] == "CHECK_PIR_SENSOR")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR;
                                }
                                // Clear device flags.
                                else if (args[1] == "CLEAR_DEV_FLAG")
                                {
                                    // Check PIR sensor.
                                    if (args[2] == "CHECK_PIR_SENSOR")
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR;
                                }

                                // Device 1: WS2812.
                                else if (args[1] == "WS2812")
                                {
                                    // Set background color.
                                    if (args[2] == "SET_BKG_COLOR")
                                    {
                                        int.TryParse(args[3], out int r);
                                        int.TryParse(args[4], out int g);
                                        int.TryParse(args[5], out int b);

                                        Invoke(() =>
                                        {
                                            foreach(var led in groupBox_Thing_WS2812.Controls)
                                            {
                                                Button ledBtn = (Button)led;
                                                ledBtn.BackColor = Color.FromArgb(r, g, b);
                                            }
                                        });
                                    }

                                    // Clear background color.
                                    else if (args[2] == "CLEAR_BKG")
                                    {
                                        Invoke(() =>
                                        {
                                            foreach (var led in groupBox_Thing_WS2812.Controls)
                                            {
                                                Button ledBtn = (Button)led;
                                                ledBtn.BackColor = Color.FromArgb(0, 0, 0);
                                            }
                                        });
                                    }

                                    // Set pixel color at a specific position.
                                    else if (args[2] == "SET_PIXEL")
                                    {
                                        int.TryParse(args[3], out int x);
                                        int.TryParse(args[4], out int y);
                                        int.TryParse(args[5], out int r);
                                        int.TryParse(args[6], out int g);
                                        int.TryParse(args[7], out int b);

                                        Invoke(() =>
                                        {
                                            string component_Name = "button_Matrix_" + x.ToString() + y.ToString();

                                            // MessageBox.Show(component_Name);
                                            Button? buttonControl = (Button)(Controls.Find(component_Name, true)[0]);
                                            if (buttonControl != null)
                                            {
                                                // MessageBox.Show("");
                                                buttonControl.BackColor = Color.FromArgb(r, g, b);
                                            }
                                        });
                                    }
                                }

                                // Device 2: LEDs.
                                else if (args[1] == "LED")
                                {
                                    // Red LED On/Off.
                                    if (args[2] == "RED")
                                    {
                                        if (args[3] == "TURN_ON")
                                            device_Flags |= IoT_Device_Flags.DEVICE_Flag_OnState_LED_RED;
                                        else if (args[3] == "TURN_OFF")
                                            device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_OnState_LED_RED;
                                    }
                                    // Green LED On/Off.
                                    else if (args[2] == "GREEN")
                                    {
                                        if (args[3] == "TURN_ON")
                                            device_Flags |= IoT_Device_Flags.DEVICE_Flag_OnState_LED_GREEN;
                                        else if (args[3] == "TURN_OFF")
                                            device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_OnState_LED_GREEN;
                                    }
                                    // Blue LED On/Off.
                                    else if (args[2] == "BLUE")
                                    {
                                        if (args[3] == "TURN_ON")
                                            device_Flags |= IoT_Device_Flags.DEVICE_Flag_OnState_LED_BLUE;
                                        else if (args[3] == "TURN_OFF")
                                            device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_OnState_LED_BLUE;
                                    }

                                    // Update IoT UI.
                                    Invoke(() => { UpdateUI_IoT(); });
                                }

                                // Device 3: PIR (Motion Detector)
                                else if (args[1] == "PIR")
                                {
                                    // End alarm state.
                                    if (args[2] == "END_ALARM")
                                    {
                                        if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR))
                                            Invoke(() => { pictureBox_Objects_PIR.Image = Properties.Resources.obj_PIR_Idle; });
                                        else
                                            Invoke(() => { pictureBox_Objects_PIR.Image = Properties.Resources.obj_PIR; });
                                    }
                                }
                            }

                            // Check for a DEVICE message.
                            else if (args[0] == "DEVICE")
                            {
                                // Check if its an alert.
                                if (args[1] == "ALERT")
                                {
                                    // Check if it's the motion detection sensor (PIR).
                                    if (args[2] == "PIR")
                                    {
                                        Invoke(() =>
                                        {
                                            pictureBox_Objects_PIR.Image = Properties.Resources.obj_PIR_Triggered;
                                        });
                                    }
                                }
                            }

                            // Check if the device is "REPORTING" something.
                            else if (args[0] == "REPORTING")
                            {
                                // MessageBox.Show("REPORTING!");
                                // Check if it is reporting sensor detection enablity state.
                                if (args[1] == "SENSOR_DETECTION_PIR")
                                {
                                    if (args[2] == "TRUE")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR;
                                    else
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR;
                                }

                                // Check if it is reporting WS2812 initialization state.
                                else if (args[1] == "INIT_WS2812")
                                {
                                    if (args[2] == "TRUE")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_Init_WS2812;
                                    else
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_Init_WS2812;
                                }
                                // Check if it is reporting SSD1306 initialization state.
                                else if (args[1] == "INIT_SSD1306")
                                {
                                    if (args[2] == "TRUE")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_Init_SSD1306;
                                    else
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_Init_SSD1306;
                                }
                                // Check if it is reporting Ports initialization state.
                                else if (args[1] == "INIT_Ports")
                                {
                                    if (args[2] == "TRUE")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_Init_Ports;
                                    else
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_Init_Ports;
                                }
                                // Check if it is reporting LEDs initialization state.
                                else if (args[1] == "INIT_LEDs")
                                {
                                    if (args[2] == "TRUE")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_Init_LEDs;
                                    else
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_Init_LEDs;
                                }
                                // Check if it is reporting Buzzer initialization state.
                                else if (args[1] == "INIT_Buzzer")
                                {
                                    if (args[2] == "TRUE")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_Init_Buzzer;
                                    else
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_Init_Buzzer;
                                }

                                // Check if RED LED is turned on.
                                else if (args[1] == "ON_LED_RED")
                                {
                                    if (args[2] == "TRUE")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_OnState_LED_RED;
                                    else
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_OnState_LED_RED;
                                }
                                // Check if GREEN LED is turned on.
                                else if (args[1] == "ON_LED_GREEN")
                                {
                                    if (args[2] == "TRUE")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_OnState_LED_GREEN;
                                    else
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_OnState_LED_GREEN;
                                }
                                // Check if BLUE LED is turned on.
                                else if (args[1] == "ON_LED_BLUE")
                                {
                                    if (args[2] == "TRUE")
                                        device_Flags |= IoT_Device_Flags.DEVICE_Flag_OnState_LED_BLUE;
                                    else
                                        device_Flags &= ~IoT_Device_Flags.DEVICE_Flag_OnState_LED_BLUE;
                                }

                                // Check if the device is reporting the color of a specific WS2812 pixel.
                                else if (args[1] == "WS2812_PIXEL")
                                {
                                    int.TryParse(args[2], out int x);
                                    int.TryParse(args[3], out int y);
                                    int.TryParse(args[4], out int r);
                                    int.TryParse(args[5], out int g);
                                    int.TryParse(args[6], out int b);

                                    // Apply color on matrix.
                                    Invoke(() =>
                                    {
                                        string component_Name = "button_Matrix_" + x.ToString() + y.ToString();

                                        // MessageBox.Show(component_Name);
                                        Button? buttonControl = (Button)(Controls.Find(component_Name, true)[0]);
                                        if (buttonControl != null)
                                        {
                                            r *= 6;
                                            g *= 6;
                                            b *= 6;

                                            if (r > 255)
                                                r = 255;
                                            if (g > 255)
                                                g = 255;
                                            if (b > 255)
                                                b = 255;
                                            // MessageBox.Show("r: " + r.ToString() + '\n' + "g: " + g.ToString() + '\n' + "b: " + b.ToString());
                                            buttonControl.BackColor = Color.FromArgb(r, g, b);
                                        }
                                    });
                                }

                                // In the end, update IoT UI.
                                if(InvokeRequired)
                                {
                                    Invoke(() =>
                                    {
                                        UpdateUI_IoT();
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        private void تازهسازیToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = IoT_SerialPort_SendData_Async("REPORT_STATUS");
        }

        /// <summary>
        /// Updates UI related to IoT and device flags, etc. Must be invoked externally if called from separate thread.
        /// </summary>
        private void UpdateUI_IoT()
        {
            if(device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR))
                pictureBox_Objects_PIR.Image = Properties.Resources.obj_PIR_Idle;
            else
                pictureBox_Objects_PIR.Image = Properties.Resources.obj_PIR;

            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_RED))
                pictureBox_Objects_LED_Red.Image = Properties.Resources.objects_RedLED_On;
            else
                pictureBox_Objects_LED_Red.Image = Properties.Resources.objects_RedLED_Off;
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_GREEN))
                pictureBox_Objects_LED_Green.Image = Properties.Resources.objects_GreenLED_On;
            else
                pictureBox_Objects_LED_Green.Image = Properties.Resources.objects_GreenLED_Off;
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_BLUE))
                pictureBox_Objects_LED_Blue.Image = Properties.Resources.objects_BlueLED_On;
            else
                pictureBox_Objects_LED_Blue.Image = Properties.Resources.objects_BlueLED_Off;

            Update_MenuItems();
        }

        /// <summary>
        /// Updates menu items.
        /// </summary>
        private void Update_MenuItems()
        {
            toolStripMenuItem_EntekhabRang.BackColor = color_WS2812_Pixel;

            قرمزToolStripMenuItem.Checked = device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_RED);
            سبزToolStripMenuItem.Checked = device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_GREEN);
            آبیToolStripMenuItem.Checked = device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_BLUE);

            فعالToolStripMenuItem.Checked = device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR);
        }

        private void pictureBox_Objects_LED_Red_Click(object sender, EventArgs e)
        {
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_RED))
                _ = IoT_SerialPort_SendData_Async("LED RED TURN_OFF", true, true, true);
            else
                _ = IoT_SerialPort_SendData_Async("LED RED TURN_ON", true, true, true);
        }

        private void pictureBox_Objects_LED_Green_Click(object sender, EventArgs e)
        {
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_GREEN))
                _ = IoT_SerialPort_SendData_Async("LED GREEN TURN_OFF", true, true, true);
            else
                _ = IoT_SerialPort_SendData_Async("LED GREEN TURN_ON", true, true, true);
        }

        private void pictureBox_Objects_LED_Blue_Click(object sender, EventArgs e)
        {
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_BLUE))
                _ = IoT_SerialPort_SendData_Async("LED BLUE TURN_OFF", true, true, true);
            else
                _ = IoT_SerialPort_SendData_Async("LED BLUE TURN_ON", true, true, true);
        }

        private void pictureBox_Objects_PIR_Click(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR))
                    await IoT_SerialPort_SendData_Async("CLEAR_DEV_FLAG CHECK_PIR_SENSOR");
                else
                    await IoT_SerialPort_SendData_Async("SET_DEV_FLAG CHECK_PIR_SENSOR");

                // await Task.Delay(500);
                await IoT_SerialPort_SendData_Async("PIR END_ALARM");
            });

            /*if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR))
                _ = IoT_SerialPort_SendData_Async("CLEAR_DEV_FLAG CHECK_PIR_SENSOR");
            else
                _ = IoT_SerialPort_SendData_Async("SET_DEV_FLAG CHECK_PIR_SENSOR");

            _ = IoT_SerialPort_SendData_Async("PIR END_ALARM");*/
        }

        private void toolStripMenuItem_EntekhabRang_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog()
            {
                AllowFullOpen = true,
                AnyColor = true,
                Color = color_WS2812_Pixel,
                ShowHelp = false,
                FullOpen = true,
            };

            if(colorDialog.ShowDialog() == DialogResult.OK)
                color_WS2812_Pixel = colorDialog.Color;

            Update_MenuItems();
        }

        private void پرکردنصفحهToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog()
            {
                AllowFullOpen = true,
                AnyColor = true,
                Color = color_WS2812_Pixel,
                ShowHelp = false,
                FullOpen = true,
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
                _ = IoT_SerialPort_SendData_Async("WS2812 SET_BKG_COLOR " + colorDialog.Color.R + " " + colorDialog.Color.G + " " + colorDialog.Color.B);
        }

        private void پاککردنصفحهToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = IoT_SerialPort_SendData_Async("WS2812 CLEAR_BKG");
        }

        private void قرمزToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox_Objects_LED_Red_Click(sender, e);
        }

        private void سبزToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox_Objects_LED_Green_Click(sender, e);
        }

        private void آبیToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox_Objects_LED_Blue_Click(sender, e);
        }

        private void فعالToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox_Objects_PIR_Click(sender, e);
            Update_MenuItems();
        }

        private async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_MainMenu_Async(long chatID, Telegram.Bot.Types.Message message, CallbackQuery? callbackQuery = null)
        {
            string prompt_MainMenu = "🏡 به منزل خود خوش آمدید.\n\n";

            prompt_MainMenu += "💡 وضعیت چراغ‌ها و لامپ‌ها:\n";
            prompt_MainMenu += "🔴 چراغ قرمز:\t";
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_RED))
                prompt_MainMenu += "✅ <b>روشن</b>";
            else
                prompt_MainMenu += "❎ <b>خاموش</b>";
            prompt_MainMenu += '\n';
            prompt_MainMenu += "🟢 چراغ سبز:\t";
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_GREEN))
                prompt_MainMenu += "✅ <b>روشن</b>";
            else
                prompt_MainMenu += "❎ <b>خاموش</b>";
            prompt_MainMenu += '\n';
            prompt_MainMenu += "🔵 چراغ آبی:\t";
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_OnState_LED_BLUE))
                prompt_MainMenu += "✅ <b>روشن</b>";
            else
                prompt_MainMenu += "❎ <b>خاموش</b>";
            prompt_MainMenu += '\n';

            prompt_MainMenu += '\n';
            prompt_MainMenu += "🕺 وضعیت سنسور تشخیص حرکت:\n";
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR))
                prompt_MainMenu += "✅ <b>آماده به کار</b>";
            else
                prompt_MainMenu += "❌ <b>غیر فعال</b>";
            prompt_MainMenu += '\n';

            prompt_MainMenu += '\n';
            prompt_MainMenu += "📸 دستگاه ضبط تصویر فعال:\n";
            if (deviceIndex_Camera < 0)
                prompt_MainMenu += "❌ <b>دوربین موجود نمی‌باشد</b>";
            else if (filterInfoCollection_Cameras != null)
                prompt_MainMenu += "👁 <b>" + filterInfoCollection_Cameras[deviceIndex_Camera].Name + "</b>";
            prompt_MainMenu += '\n';

            // Main Menu inline keyboard.
            List<List<InlineKeyboardButton>> inlineKeyboard_MainMenu = new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("🌈 ماژول تولید رنگ WS2812", "MENU_DISPLAY_PANEL_WS2812"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("🖥 نمایشگر OLED SSD1306", "MENU_DISPLAY_PANEL_SSD1306"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("🕺 حسگر تشخیص حرکت PIR", "MENU_DISPLAY_PANEL_PIR"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("💡 چراغ‌ها", "MENU_DISPLAY_PANEL_LEDS"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("📷 دوربین‌ها", "MENU_DISPLAY_PANEL_LEDS"),
                },
            };

            if (botClient != null)
            {
                if (callbackQuery == null)
                    return await botClient.SendTextMessageAsync(chatID, prompt_MainMenu, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, message.MessageId, true, new InlineKeyboardMarkup(inlineKeyboard_MainMenu));
                else if (callbackQuery.Message != null)
                    return await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, prompt_MainMenu, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, new InlineKeyboardMarkup(inlineKeyboard_MainMenu));
                else return null;
            }
            else
                return null;
        }

        protected async Task Begin_Bot_InlineCallbackProcess(CallbackQuery callbackQuery)
        {
            string? cbData = callbackQuery.Data;

            if (botClient == null || cbData == null || callbackQuery.Message == null)
                return;

            // Split arguments.
            string[] args = cbData.ToUpper().Split('~');

            // Main Menu -> Display WS2812 device Control Panel.
            if (args[0] == "MENU_DISPLAY_PANEL_WS2812")
            {
                await IoT_Bot_Prompt_WS2812_CP_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
            }
            // Main Menu -> Display SSD1306 device Control Panel.
            else if (args[0] == "MENU_DISPLAY_PANEL_SSD1306")
            {
                await IoT_Bot_Prompt_SSD1306_CP_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
            }

            // WS2812 -> Set Background with current color.
            else if (args[0] == "WS2812_SET_BKG")
            {
                _ = IoT_SerialPort_SendData_Async("WS2812 SET_BKG_COLOR " + color_WS2812_Pixel.R + " " + color_WS2812_Pixel.G + " " + color_WS2812_Pixel.B);
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "دستور رنگ‌آمیزی صفحه با موفقیت اجرا شد.");
            }
            // WS2812 -> Clear background.
            else if (args[0] == "WS2812_CLEAR_BKG")
            {
                _ = IoT_SerialPort_SendData_Async("WS2812 CLEAR_BKG");
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "دستور پاکسازی صفحه با موفقیت اجرا شد.");
            }
            // WS2812 -> Color Channel Picker.
            else if (args[0] == "WS2812_SET_COLOR_CHANNEL")
            {
                await IoT_Bot_Prompt_ColorChannelPicker_Async(callbackQuery.Message.Chat.Id, args[1], callbackQuery.Message, callbackQuery);
            }
            // WS2812 -> Display Pixels map.
            else if (args[0] == "WS2812_DISPLAY_PIXELS")
            {
                await IoT_Bot_Prompt_WS2812_DisplayPixels_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
            }
            // WS2812 -> Set Pixel.
            else if (args[0] == "WS2812_SET_PIXEL")
            {
                int.TryParse(args[1], out int x);
                int.TryParse(args[2], out int y);

                await Begin_IoT_WS2812_SetPixel_Async(x, y, color_WS2812_Pixel);
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "پیکسل در موقعیت (" + x.ToString() + ", " + y.ToString() + ") رنگ‌آمیزی شد.");
            }
            // WS2812 -> Display clearing pixels map.
            else if (args[0] == "WS2812_CLEAR_PIXELS")
            {
                await IoT_Bot_Prompt_WS2812_ClearPixels_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
            }
            // WS2812 -> Clear Pixel.
            else if (args[0] == "WS2812_CLEAR_PIXEL")
            {
                int.TryParse(args[1], out int x);
                int.TryParse(args[2], out int y);

                await Begin_IoT_WS2812_SetPixel_Async(x, y, 0, 0, 0);
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "پیکسل در موقعیت (" + x.ToString() + ", " + y.ToString() + ") خاموش شد.");
            }

            // PIR -> Display device Control Panel.
            else if (args[0] == "MENU_DISPLAY_PANEL_PIR")
            {
                await IoT_Bot_Prompt_PIR_CP_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
            }
            // PIR -> Enable motion detection.
            else if (args[0] == "SENSOR_PIR_ENABLE")
            {
                await IoT_SerialPort_SendData_Async("SET_DEV_FLAG CHECK_PIR_SENSOR");
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "حسگر تشخیص حرکت با موفقیت فعال شد.", true);
                await IoT_Bot_Prompt_PIR_CP_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
            }
            // PIR -> Disable motion detection.
            else if (args[0] == "SENSOR_PIR_DISABLE")
            {
                await IoT_SerialPort_SendData_Async("CLEAR_DEV_FLAG CHECK_PIR_SENSOR");
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "حسگر تشخیص حرکت با موفقیت غیرفعال شد.", true);
                await IoT_Bot_Prompt_PIR_CP_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
            }
            // PIR -> End alarm.
            else if (args[0] == "SENSOR_PIR_END_ALARM")
            {
                await IoT_SerialPort_SendData_Async("PIR END_ALARM");
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "هشدار با موفقیت متوقف شد.", true);
            }

            // Callback -> Set Color Channel Value.
            else if (args[0] == "CB_SET_COLOR_CHANNEL_VALUE")
            {
                byte r = color_WS2812_Pixel.R;
                byte g = color_WS2812_Pixel.G;
                byte b = color_WS2812_Pixel.B;

                if (args[1] == "R")
                    byte.TryParse(args[2], out r);
                else if (args[1] == "G")
                    byte.TryParse(args[2], out g);
                else if (args[1] == "B")
                    byte.TryParse(args[2], out b);

                color_WS2812_Pixel = Color.FromArgb(r, g, b);

                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "مقدار کانال رنگی با موفقیت تغییر پیدا کرد.");
                await IoT_Bot_Prompt_WS2812_CP_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
            }
            // Callback -> Return to a previous menu.
            else if (args[0] == "CB_RETURN_TO")
            {
                // Return to main menu.
                if (args[1] == "MAIN_MENU")
                    await IoT_Bot_Prompt_MainMenu_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
                else if (args[1] == "WS2812_CP")
                    await IoT_Bot_Prompt_WS2812_CP_Async(callbackQuery.Message.Chat.Id, callbackQuery.Message, callbackQuery);
            }
        }

        protected async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_WS2812_CP_Async(long chatID, Telegram.Bot.Types.Message? message, CallbackQuery? callbackQuery = null)
        {
            string prompt_WS2812_CP = "🖥 پنل کنترلی ماژول تولید رنگ WS2812\n\n";

            prompt_WS2812_CP += "👈 وضعیت دستگاه:\n";
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_Init_WS2812))
                prompt_WS2812_CP += "✅ <b>آماده به کار</b>";
            else
                prompt_WS2812_CP += "❓ <b>نامشخص</b>";

            prompt_WS2812_CP += "\n\n";

            prompt_WS2812_CP += "🌈 رنگ فعلی:\n";
            prompt_WS2812_CP += "🔴 " + color_WS2812_Pixel.R.ToString() + '\n';
            prompt_WS2812_CP += "🟢 " + color_WS2812_Pixel.G.ToString() + '\n';
            prompt_WS2812_CP += "🔵 " + color_WS2812_Pixel.B.ToString() + '\n';

            prompt_WS2812_CP += '\n';
            prompt_WS2812_CP += "👇 جهت کار با دستگاه، از دکمه‌های ذیل استفاده کنید:";

            // Inline buttons for WS2812 Control Panel.
            List<List<InlineKeyboardButton>> inlineKeyboard_WS2812 = new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("🔴","WS2812_SET_COLOR_CHANNEL~R"),
                    InlineKeyboardButton.WithCallbackData("🟢","WS2812_SET_COLOR_CHANNEL~G"),
                    InlineKeyboardButton.WithCallbackData("🔵","WS2812_SET_COLOR_CHANNEL~B"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("✳ پر کردن صفحه", "WS2812_SET_BKG"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("🧹 پاکسازی صفحه", "WS2812_CLEAR_BKG"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("🔲 رنگ‌آمیزی پیکسل‌ها", "WS2812_DISPLAY_PIXELS"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("⬜ پاکسازی پیکسل‌ها", "WS2812_CLEAR_PIXELS"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("بازگشت به منوی اصلی 👈", "CB_RETURN_TO~MAIN_MENU"),
                },
            };

            if (botClient != null)
            {
                if (callbackQuery == null)
                    return await botClient.SendTextMessageAsync(chatID, prompt_WS2812_CP, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, message?.MessageId, true, new InlineKeyboardMarkup(inlineKeyboard_WS2812));
                else if (callbackQuery.Message != null)
                    return await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, prompt_WS2812_CP, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, new InlineKeyboardMarkup(inlineKeyboard_WS2812));
                else return null;
            }
            else
                return null;
        }

        protected async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_ColorChannelPicker_Async(long chatID, string channelStr, Telegram.Bot.Types.Message message, CallbackQuery? callbackQuery = null)
        {
            string prompt_ColorChannelPicker = "🌈 انتخاب مقدار کانال رنگی 👇";
            string channelIndicator = "";
            if (channelStr == "R")
                channelIndicator = "🔴";
            else if (channelStr == "G")
                channelIndicator = "🟢";
            else if (channelStr == "B")
                channelIndicator = "🔵";

            // Generate 255 keyboard buttons.
            List<List<InlineKeyboardButton>> inlineKeyboard_ChannelValues = new List<List<InlineKeyboardButton>>();
            for(int i = 0; i <= 255; i += 5)
            {
                List<InlineKeyboardButton> keysPerRow = new List<InlineKeyboardButton>();

                keysPerRow.Add(InlineKeyboardButton.WithCallbackData(channelIndicator + " " + i.ToString(), "CB_SET_COLOR_CHANNEL_VALUE~" + channelStr + "~" + i.ToString()));
                i++;
                keysPerRow.Add(InlineKeyboardButton.WithCallbackData(channelIndicator + " " + i.ToString(), "CB_SET_COLOR_CHANNEL_VALUE~" + channelStr + "~" + i.ToString()));
                i++;
                keysPerRow.Add(InlineKeyboardButton.WithCallbackData(channelIndicator + " " + i.ToString(), "CB_SET_COLOR_CHANNEL_VALUE~" + channelStr + "~" + i.ToString()));
                i++;
                keysPerRow.Add(InlineKeyboardButton.WithCallbackData(channelIndicator + " " + i.ToString(), "CB_SET_COLOR_CHANNEL_VALUE~" + channelStr + "~" + i.ToString()));
                i++;

                inlineKeyboard_ChannelValues.Add(keysPerRow);
            }

            // No more spaces available for buttons! (Telegram limitation).
            /*inlineKeyboard_ChannelValues.Add(new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("بازگشت به پنل ماژول 👈", "CB_RETURN_TO~WS2812_CP"),
                });*/

            if (botClient != null)
            {
                if (callbackQuery == null)
                    return await botClient.SendTextMessageAsync(chatID, prompt_ColorChannelPicker, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, message.MessageId, true, new InlineKeyboardMarkup(inlineKeyboard_ChannelValues));
                else if (callbackQuery.Message != null)
                    return await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, prompt_ColorChannelPicker, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, new InlineKeyboardMarkup(inlineKeyboard_ChannelValues));
                else return null;
            }
            else
                return null;
        }

        protected async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_WS2812_DisplayPixels_Async(long chatID, Telegram.Bot.Types.Message message, CallbackQuery? callbackQuery = null)
        {
            string prompt_WS2812_DisplayPixels = "🔲 نقشه پیکسل‌های ماژول WS2812.\n\n👇 با کلیک بر روی دکمه‌های ذیل، پیکسلها رنگ می‌شوند.";

            List<List<InlineKeyboardButton>> inlineKeyboard_WS2812_Pixels = new List<List<InlineKeyboardButton>>();

            for(int i = 0; i < _NUM_LEDS_ROWS; i++)
            {
                List<InlineKeyboardButton> buttonsPerRow = new List<InlineKeyboardButton>();
                for(int j = 0; j < _NUM_LEDS_COLS; j++)
                {
                    buttonsPerRow.Add(InlineKeyboardButton.WithCallbackData("⬜", "WS2812_SET_PIXEL~" + j.ToString() + "~" + i.ToString()));
                }
                inlineKeyboard_WS2812_Pixels.Add(buttonsPerRow);
            }

            inlineKeyboard_WS2812_Pixels.Add(new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("بازگشت به پنل ماژول 👈", "CB_RETURN_TO~WS2812_CP"),
                });

            if (botClient != null)
            {
                if (callbackQuery == null)
                    return await botClient.SendTextMessageAsync(chatID, prompt_WS2812_DisplayPixels, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, message.MessageId, true, new InlineKeyboardMarkup(inlineKeyboard_WS2812_Pixels));
                else if (callbackQuery.Message != null)
                    return await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, prompt_WS2812_DisplayPixels, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, new InlineKeyboardMarkup(inlineKeyboard_WS2812_Pixels));
                else return null;
            }
            else
                return null;
        }

        protected async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_WS2812_ClearPixels_Async(long chatID, Telegram.Bot.Types.Message message, CallbackQuery? callbackQuery = null)
        {
            string prompt_WS2812_ClearPixels = "🔲 نقشه پیکسل‌های ماژول WS2812.\n\n👇 با کلیک بر روی دکمه‌های ذیل، پیکسلها خاموش می‌شوند.";

            List<List<InlineKeyboardButton>> inlineKeyboard_WS2812_Pixels = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < _NUM_LEDS_ROWS; i++)
            {
                List<InlineKeyboardButton> buttonsPerRow = new List<InlineKeyboardButton>();
                for (int j = 0; j < _NUM_LEDS_COLS; j++)
                {
                    buttonsPerRow.Add(InlineKeyboardButton.WithCallbackData("⬛", "WS2812_CLEAR_PIXEL~" + j.ToString() + "~" + i.ToString()));
                }
                inlineKeyboard_WS2812_Pixels.Add(buttonsPerRow);
            }

            inlineKeyboard_WS2812_Pixels.Add(new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("بازگشت به پنل ماژول 👈", "CB_RETURN_TO~WS2812_CP"),
                });

            if (botClient != null)
            {
                if (callbackQuery == null)
                    return await botClient.SendTextMessageAsync(chatID, prompt_WS2812_ClearPixels, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, message.MessageId, true, new InlineKeyboardMarkup(inlineKeyboard_WS2812_Pixels));
                else if (callbackQuery.Message != null)
                    return await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, prompt_WS2812_ClearPixels, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, new InlineKeyboardMarkup(inlineKeyboard_WS2812_Pixels));
                else return null;
            }
            else
                return null;
        }

        protected async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_SSD1306_CP_Async(long chatID, Telegram.Bot.Types.Message message, CallbackQuery? callbackQuery = null)
        {
            string prompt_SSD1306_CP = "🖥 ماژول نمایشگر OLED SSD1306\n\n";

            prompt_SSD1306_CP += "👈 وضعیت دستگاه:\n";
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_Init_SSD1306))
                prompt_SSD1306_CP += "✅ <b>آماده به کار</b>";
            else
                prompt_SSD1306_CP += "❓ <b>نامشخص</b>";

            List<List<InlineKeyboardButton>> inlineKeyboard_SSD1306 = new List<List<InlineKeyboardButton>>();
            inlineKeyboard_SSD1306.Add(new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("بازگشت به منوی اصلی 👈", "CB_RETURN_TO~MAIN_MENU"),
                });

            if (botClient != null)
            {
                if (callbackQuery == null)
                    return await botClient.SendTextMessageAsync(chatID, prompt_SSD1306_CP, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, message.MessageId, true, new InlineKeyboardMarkup(inlineKeyboard_SSD1306));
                else if (callbackQuery.Message != null)
                    return await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, prompt_SSD1306_CP, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, new InlineKeyboardMarkup(inlineKeyboard_SSD1306));
                else return null;
            }
            else
                return null;
        }

        protected async Task<Telegram.Bot.Types.Message?> IoT_Bot_Prompt_PIR_CP_Async(long chatID, Telegram.Bot.Types.Message message, CallbackQuery? callbackQuery = null)
        {
            string prompt_PIR_CP = "🕺 ماژول حسگر تشخیص حرکت PIR\n\n";
            // MessageBox.Show("");

            prompt_PIR_CP += "👈 وضعیت دستگاه:\n";
            if (device_Flags.HasFlag(IoT_Device_Flags.DEVICE_Flag_Detect_Sensor_PIR))
                prompt_PIR_CP += "✅ <b>فعال</b>";
            else
                prompt_PIR_CP += "❌ <b>غیرفعال</b>";

            List<List<InlineKeyboardButton>> inlineKeyboard_PIR = new List<List<InlineKeyboardButton>>() { 
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("✅ فعال کردن", "SENSOR_PIR_ENABLE"),
                    InlineKeyboardButton.WithCallbackData("❌ غیرفعال کردن", "SENSOR_PIR_DISABLE"),
                },
                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("🤚 توقف هشدار", "SENSOR_PIR_END_ALARM"),
                },

                new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("بازگشت به منوی اصلی 👈", "CB_RETURN_TO~MAIN_MENU"),
                },
            };

            if (botClient != null)
            {
                if (callbackQuery == null)
                    return await botClient.SendTextMessageAsync(chatID, prompt_PIR_CP, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, null, true, message.MessageId, true, new InlineKeyboardMarkup(inlineKeyboard_PIR));
                else if (callbackQuery.Message != null)
                    return await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, prompt_PIR_CP, Telegram.Bot.Types.Enums.ParseMode.Html, null, null, new InlineKeyboardMarkup(inlineKeyboard_PIR));
                else return null;
            }
            else
                return null;
        }
    }
}