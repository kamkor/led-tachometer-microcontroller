using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Collections;

using LFS_External;
using LFS_External.OutGauge;
using KamkorTachometerApi;

namespace TachometerLFSClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string configFileName = "tachometer.xml";

        string[] carNames = new string[] { 
            "UF1",
            "XFG",
            "XRG",
            "LX4",
            "LX6",
            "RB4",
            "FXO",
            "XRT",
            "RAC",
            "FZ5",
            "UFR",
            "XFR",
            "FXR",
            "XRR",
            "FZR",
            "MRT",
            "FBM",
            "FOX",
            "FO8",
            "BF1"
        };

        // Main OutGauge object
        OutGaugeInterface OutGauge;

        // Tachometer Api sends messages to tachometer device
        Tachometer tachometer;        

        // Settings can't be binded directly from xml to datagrid, because the view
        // doesn't support 2 way editing. That's why settings are loaded from xml to 
        // carSettings collection and then binded to datagrid. 
        // Also it's synchronized, because carSettings are used by two threads (main window and outgauge)        
        ArrayList carSettings = ArrayList.Synchronized(new ArrayList());
        string currentCar = "";
        XElement modeSetting = new XElement("Mode");

        // used by OutGauge_Received (which is executed in another thread) to change car settings
        public delegate void triggerComboBoxSelectionChangedCallback(string carName);

        public MainWindow()
        {
            InitializeComponent();            
            foreach (string carName in carNames)
            {
                carsComboBox.Items.Add(carName);
            }            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {                
                var config = XElement.Load(configFileName);
                tachometer = new Tachometer(
                    config.Element("Port").Value,
                    System.Convert.ToChar(config.Element("DefaultMode").Value));

                // Initialize OutGauge
                ushort outgauge_reply_port = 35555;
                OutGauge = new OutGaugeInterface(outgauge_reply_port);

                // Add OutGauge event handler so we can catch the incoming OutGauge packet
                OutGauge.OutGauge_Received += new OutGaugeInterface.OutGauge_EventHandler(OutGauge_Received);

                // Start listening for OutGauge packets
                OutGauge.Listen();

                triggerComboBoxSelectionChanged("XRT");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // This event will be triggered when an OutGauge packet is received
        private void OutGauge_Received(Packets.OutGaugePack og)
        {
            if (currentCar != og.Car)
            {                
                carsComboBox.Dispatcher.Invoke(
                    new triggerComboBoxSelectionChangedCallback(this.triggerComboBoxSelectionChanged), new object[] { og.Car });
            }
            var rpm = og.RPM;
            lock(carSettings)
            {
                foreach (XElement setting in carSettings)
                {
                    try
                    {
                        var settingRpm = System.Convert.ToSingle(setting.Attribute("Rpm").Value);
                        var isSettingEnabled = System.Convert.ToBoolean(setting.Attribute("Enabled").Value);
                        if (rpm > (settingRpm - 95) && rpm < (settingRpm + 95) && isSettingEnabled)
                        {
                            tachometer.sendData(System.Convert.ToByte(setting.Attribute("Leds").Value));
                        }
                    }
                    catch (FormatException ex)
                    {
                        MessageBox.Show("Attributes of  " + og.Car + " may not be correctly formated. Exception " +
                            ex.Message);
                    }
                }              
            }
        }

        /// <summary>
        /// Changes cars combo box text, which then fires carsComboBox_SelectionChanged event
        /// </summary>
        /// <param name="car"></param>
        private void triggerComboBoxSelectionChanged(string car)
        {
            carsComboBox.Text = car;
        }

        private void carsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            saveSettings();
            loadSettings((string)carsComboBox.SelectedItem);
        }

        private void saveSettings()
        {
            var config = XElement.Load(configFileName);
            var carToBeSaved = currentCar;
            try
            {
                var carEl = config.Elements("Car").Single(match => match.Attribute("Name").Value.Equals(currentCar));
                carEl.RemoveNodes();
                carEl.Add(modeSetting);
                carEl.Add(carSettings);
            }
            catch (InvalidOperationException ex)
            {
                System.Console.WriteLine(ex.Message);
            }
            config.Save(configFileName);
        }

        /// <summary>
        /// Loads settings from xml for specified car
        /// </summary>
        /// <param name="car"></param>
        private void loadSettings(string car)
        {
            XElement config = XElement.Load(configFileName);           
            try
            {
                XElement carEl = config.Elements("Car").Single(match => match.Attribute("Name").Value.Equals(car));
                if (carEl.Elements("Mode").Count() == 0)
                {
                    carEl.Add(new XElement("Mode", config.Element("DefaultMode").Value));
                }
                changeSettings(carEl);
            } 
            catch (InvalidOperationException) // element not found
            {
                XElement newCar = new XElement("Car");
                newCar.Add(new XAttribute("Name", car));
                newCar.Add(new XElement("Mode", config.Element("DefaultMode").Value));
                config.Add(newCar);
                config.Save(configFileName);
                changeSettings(newCar);                                
            }            
        }

        /// <summary>
        /// Changes car settings - clears previous car settings, and loads current car settings
        /// </summary>
        /// <param name="car"></param>
        private void changeSettings(XElement car)
        {
            carSettings.Clear();
            modeSetting = car.Element("Mode");
            currentCar = car.Attribute("Name").Value;
            byte mode = System.Convert.ToByte(car.Element("Mode").Value);
            switch (mode)
            {
                case 0:
                    modeZero.IsChecked = true;
                    break;
                case 1:
                    modeOne.IsChecked = true;
                    break;
                case 2:
                    modeTwo.IsChecked = true;
                    break;
            }
            // tachometer understands '0' char as 0 mode, '1' char as 1 mode and '2' as 2 mode. 
            tachometer.sendData(System.Convert.ToByte(System.Convert.ToChar(car.Element("Mode").Value)));                               
            carSettings.AddRange(car.Elements("Setting").ToList());
            settingsGrid.DataContext = carSettings;
            settingsGrid.Items.Refresh();
        }

        private void addSetting_Click(object sender, RoutedEventArgs e)
        {
            XElement el = new XElement("Setting");
            el.Add(new XAttribute("Rpm", 0));
            el.Add(new XAttribute("Leds", 0));
            el.Add(new XAttribute("Enabled", "True"));
            lock (carSettings)
            {
                carSettings.Add(el);
            }
            settingsGrid.Items.Refresh();
            saveSettings();
        }

        private void deleteSetting_Click(object sender, RoutedEventArgs e)
        {
            lock (carSettings)
            {
                carSettings.Remove((XElement)settingsGrid.SelectedItem);
            }
            settingsGrid.Items.Refresh();
            saveSettings();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (tachometer != null)
            {
                saveSettings();
                tachometer.closeConnection();
                OutGauge.Close();
            }
            Application.Current.Shutdown(0);
           
        }

        private void settingsGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            saveSettings();
        }
        
        private void settingsGrid_KeyDown(object sender, KeyEventArgs e)
        {            
            switch (e.Key)
            {
                case Key.Delete:
                    foreach (XElement setting in settingsGrid.SelectedItems)
                    {
                        lock (carSettings)
                            carSettings.Remove(setting);
                    }
                    break;
                case Key.E:
                    foreach (XElement setting in settingsGrid.SelectedItems)
                    {
                        setting.Attribute("Enabled").SetValue("True");
                    }
                    break;
                case Key.D:
                    foreach (XElement setting in settingsGrid.SelectedItems)
                    {
                        setting.Attribute("Enabled").SetValue("False");
                    }
                    break;
            }
            saveSettings();
        }

        private void modeZero_Checked(object sender, RoutedEventArgs e)
        {
            modeSetting.Value = "0";
            changeMode('0');
        }

        private void modeOne_Checked(object sender, RoutedEventArgs e)
        {
            modeSetting.Value = "1";
            changeMode('1');
        }

        private void modeTwo_Checked(object sender, RoutedEventArgs e)
        {
            modeSetting.Value = "2";
            changeMode('2');
        }

        private void changeMode(char mode)
        {
            tachometer.sendData(System.Convert.ToByte(mode));
            saveSettings();           
        }
    }
}