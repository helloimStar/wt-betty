using System;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;
using System.Windows.Documents;
using System.IO;
using System.Windows.Data;
using wt_betty.Entities;
using System.Collections.Generic;
using WPFCustomMessageBox;
using System.Collections.ObjectModel;
using System.Threading;
using System.Linq;
using System.Reflection;

namespace wt_betty
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// NEW BETTY
    ///
    //todo: fuel warning (Done)
    //todo: eula, help,

    //TODO (ZdrytchX):
    // read/write json data per-aircraft type into sub folder
    // Rewrite code in python or lua or something for multi-OS friendliness

    public partial class MainWindow : Window
    {
        private Indicator myIndicator = new Indicator();
        private State myState = new State();
        CultureInfo culture = new CultureInfo("en-US");
        FlowDocument myFlowDoc = new FlowDocument();
        Paragraph par = new Paragraph();

        Queue<KeyValuePair<DateTime, int>> FuelTimeMonitor = new Queue<KeyValuePair<DateTime, int>>();

        private ConnectionManager m_ConnectionManager = new ConnectionManager();
        private Settings Settings { get => Settings.Instance; }
        private VoiceProcessor VoiceProcessor { get; set; }
        private AircraftProfile CurrentProfile
        {
            get => (AircraftProfile)cmb_profile.SelectedItem;
            set
            {
                var profile = value;
                UpdateUIThreadSafe(() =>
                {
                    var oldValue = CurrentProfile;
                    if (profile != oldValue)
                    {
                        cmb_profile.SelectedItem = profile;
                        UpdateProfileUI(profile);
                    }
                });

                
            }
        }
        private string CurrentAircraft
        {
            get
            {
                if (myState != null && myIndicator != null)
                {
                    bool valid = myState.valid == "true" && myIndicator.valid == "true" && myIndicator.type != null && myIndicator.type != "dummy_plane";
                    if (valid)
                        return myIndicator.type;
                }
                return null;
            }
        }
        private ObservableCollection<AircraftProfile> Profiles { get; set; } = new ObservableCollection<AircraftProfile>();

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                if (!File.Exists(Settings.PATH))
                    Settings.Save();
                Settings.Load();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to load settings.txt", "Error");
            }

            Profiles.Add(Settings.Default);
            foreach (var profile in Settings.Profiles.Values)
                Profiles.Add(profile);

            cmb_profile.ItemsSource = Profiles;
            cmb_profile.DisplayMemberPath = "Name";
            CurrentProfile = Settings.Default;

            m_ConnectionManager.OnConnectionChanged += OnConnectionChanged;
            m_ConnectionManager.OnStateUpdated += OnStateUpdated;
        }

        private void OnConnectionChanged(object sender, EventArgs e)
        {
            var connectionArgs = e as ConnectionEventArgs;
            bool connected = connectionArgs.Connected;

            UpdateUIThreadSafe(() => tbx_msgs.Text = (connected ? "Running" : "Connection failed"));
        }

        private void OnStateUpdated(object sender, EventArgs e)
        {
            var stateArgs = e as StateEventArgs;
            if (stateArgs.ErrorDetails == null)
                UpdateData(stateArgs.Indicator, stateArgs.State);
            else
                UpdateUIThreadSafe(() => tbx_msgs.Text = stateArgs.ErrorDetails);
        }
        
        private void UpdateData(Indicator indicator, State state)
        {
            try
            {
                myIndicator = indicator;
                myState = state;

                var currentAircraft = CurrentAircraft;

                string monitorMessage = "";


                if (currentAircraft != null)
                {
                    var currentProfile = Settings.Profiles.ContainsKey(currentAircraft) ? Settings.Profiles[currentAircraft] : Settings.Default;
                    CurrentProfile = currentProfile;




                    decimal G = Convert.ToDecimal(myState.Ny, culture);
                    decimal AoA = Convert.ToDecimal(myState.AoA, culture);
                    decimal Vspeed = Convert.ToDecimal(myState.Vy, culture);
                    int Fuel = Convert.ToInt32(myState.Mfuel) * 1000;//MFuel and MFuel0 are given in integers
                    int FuelFull = Convert.ToInt32(myState.Mfuel0);
                    int Throttle = Convert.ToInt32(Convert.ToDecimal(myIndicator.throttle, culture) * 100);//TODO throttle variable only avialble in single engine aircraft
                    int gear = Convert.ToInt32(myState.gear);
                    int Alt = Convert.ToInt32(myState.H, culture);
                    int IAS = Convert.ToInt32(myState.IAS);
                    int flaps = Convert.ToInt32(myState.flaps);

                    //Console.WriteLine(myState.AoS);
                    //tbx_msgs.Text = myState.AoS;
                    decimal AoS = Convert.ToDecimal(myState.AoS, culture);
                    int TAS = Convert.ToInt32(myState.TAS, culture);
                    UpdateUIThreadSafe(() => label.Content = myIndicator.type);


                    //Monitoring
                    if (currentProfile.Monitoring == "")
                    {
                        monitorMessage = "";
                    }
                    else if (currentProfile.Monitoring == "G-Force")
                    {
                        monitorMessage = G.ToString();
                    }
                    else if (currentProfile.Monitoring == "AoA")
                    {
                        monitorMessage = AoA.ToString();
                    }
                    else if (currentProfile.Monitoring == "Speed")
                    {
                        monitorMessage = IAS.ToString();
                    }
                    else if (currentProfile.Monitoring == "Fuel Time")
                    {
                        //int queueLength = 25;

                        //KeyValuePair<DateTime, int> newDataPoint = new KeyValuePair<DateTime, int>(DateTime.Now, (int)(Fuel / FuelFull));
                        //FuelTimeMonitor.Enqueue(newDataPoint);

                        //if (FuelTimeMonitor.Count == queueLength)
                        //{
                        //    KeyValuePair<DateTime, int> oldDataPoint = FuelTimeMonitor.Dequeue();
                        //    double timeElapsed = (newDataPoint.Key - oldDataPoint.Key).TotalSeconds;
                        //    int fuelUsed = oldDataPoint.Value - newDataPoint.Value;

                        //    double secondsPerFuel = timeElapsed / fuelUsed;
                        //    double fuelTimeRemaining = secondsPerFuel * newDataPoint.Value;

                        //    TimeSpan timeSpan = TimeSpan.FromSeconds(fuelTimeRemaining);
                        //    string formattedTime = timeSpan.ToString("mm\\:ss");
                        //    monitorMessage = formattedTime;
                        //}
                        //else
                        //{
                        //    monitorMessage = "...";
                        //    while (FuelTimeMonitor.Count > queueLength)
                        //    {
                        //        FuelTimeMonitor.Dequeue();
                        //    }
                        //}

                    }
                    else
                    {
                        monitorMessage = "hmmmmmmm";
                    }

                    UpdateUIThreadSafe(() => label_monitoring.Content = monitorMessage);


                    //BINGO FUEL
                    if (currentProfile.EnableFuel)
                    {
                        bool bingoFuel = Fuel / FuelFull < 105 && Fuel / FuelFull > 100 && Throttle > 0;
                        VoiceProcessor.BingoFuel(bingoFuel);
                    }

                    //STALL WARNING
                    if (currentProfile.EnableAoA)
                    {
                        bool stallWarning = AoA > Convert.ToDecimal(currentProfile.AoA * 0.8) && AoA < currentProfile.AoA + 10 && (myIndicator.gears_lamp == "1" || IAS > 100);
                        if (stallWarning)
                        {
                            if (AoA < currentProfile.AoA)
                                VoiceProcessor.AoAMaximum();
                            else
                                VoiceProcessor.AoAOverLimit();
                        }
                        else
                        {
                            VoiceProcessor.AoAMaximum(false);
                            VoiceProcessor.AoAOverLimit(false);
                        }
                    }
                    
                    //G OVERLOAD
                    if (currentProfile.EnableG)
                    {
                        bool gOverload = G > currentProfile.GForce || (double)G < -0.4 * currentProfile.GForce;
                        if (gOverload)
                        {
                            if (G > currentProfile.GForce * (decimal)1.2)
                                VoiceProcessor.GOverLimit();
                            else
                                VoiceProcessor.GMaximum();
                        }
                        else
                        {
                            VoiceProcessor.GMaximum(false);
                            VoiceProcessor.GOverLimit(false);
                        }
                    }

                    //PULL UP Ground/sea level Proximity Warning
                    //desirable to have about 3 seconds before crash
                    if (currentProfile.EnablePullUp)
                    {
                        bool pullUp = 0 - Vspeed * (2 + (decimal)Math.Pow(IAS / 100, 0.7)) > Alt;
                        VoiceProcessor.PullUp(pullUp);
                    }

                    //Overspeed
                    if (currentProfile.EnableOverSpeed)
                    {
                        bool overSpeed = IAS > currentProfile.OverSpeed;
                        VoiceProcessor.Overspeed(overSpeed);
                    }

                    //GEAR UP/DOWN
                    if (currentProfile.EnableGear == true)
                    {
                        bool gearUp = gear == 100 && IAS > currentProfile.GearUp && myIndicator.gears_lamp == "0";
                        VoiceProcessor.GearUp(gearUp);

                        if ((AoA < 20 || Vspeed > -10) &&
                            IAS < currentProfile.GearDown && IAS > 40/*Alt < 500 && flaps > 20*/)
                        {
                            float Deg2Rad = (float)(Math.PI / 180f);
                            float driftSpeed = (float)(TAS * Math.Sin(Deg2Rad * (float)AoS));

                            bool gearDown = gear == 0 && myIndicator.gears_lamp != "0" && Throttle < 20;
                            VoiceProcessor.GearDown(gearDown);
                            //Sink rate warning: WT has a global vertical gear speed limit of 10m/s
                            bool sinkRate = !gearDown && (Vspeed < -8 && Throttle < 60);
                            VoiceProcessor.SinkRate(sinkRate);
                            //drift april fools
                            /*
                            else if (driftSpeed < -50)
                            {
                                tbx_msgs.Text = "ドリフト! ";
                                System.Media.SoundPlayer myPlayer;
                                myPlayer = new System.Media.SoundPlayer(Properties.Resources.Running90s);
                                myPlayer.PlaySync();
                            }
                            else if (driftSpeed > 50)
                            {
                                tbx_msgs.Text = "ドリフト! ";
                                System.Media.SoundPlayer myPlayer;
                                myPlayer = new System.Media.SoundPlayer(Properties.Resources.ManuelGGG);
                                myPlayer.PlaySync();
                            }
                            */
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                UpdateUIThreadSafe(() => tbx_msgs.Text = ex.Message);
            }
        }

        private void UpdateProfileUI(AircraftProfile profile)
        {
            cbx_g.IsChecked = profile.EnableG;
            slider_G.Value = Convert.ToDouble(profile.GForce);
            textBox_gSlider.Text = slider_G.Value.ToString();
            cbx_a.IsChecked = profile.EnableAoA;
            slider_A.Value = Convert.ToDouble(profile.AoA);
            textBox_aSlider.Text = slider_A.Value.ToString();
            cbx_overSpeed.IsChecked = profile.EnableOverSpeed;
            tbx_overSpeed.Text = profile.OverSpeed.ToString();
            cbx_pullup.IsChecked = profile.EnablePullUp;
            cbx_fuel.IsChecked = profile.EnableFuel;
            cbx_gear.IsChecked = profile.EnableGear;
            tbx_gearDown.Text = profile.GearDown.ToString();
            tbx_gearUp.Text = profile.GearUp.ToString();
            var voice = profile.Voice;
            rb_betty.IsChecked = voice == VoiceTemplate.US_Betty;
            rb_rita.IsChecked = voice == VoiceTemplate.RU_Rita;
            cmb_Monitoring.SelectedValue = Convert.ToString(profile.Monitoring);

            button_remove.IsEnabled = profile != Settings.Default;

            VoiceProcessor?.Stop();
            VoiceProcessor = VoiceProcessorFactory.GetProcessor(voice);
            VoiceProcessor?.Start();
        }

        private void UpdateUIThreadSafe(Action action)
        {
            if (!Dispatcher.CheckAccess())
                Dispatcher.BeginInvoke(action);
            else
                action();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_ConnectionManager.Stop();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbx_msgs.Text = "Connecting";
            m_ConnectionManager.Start();
            button_start.IsEnabled = false;
            button_stop.IsEnabled = true;
        }

        private void button_start_Click(object sender, RoutedEventArgs e)
        {
            VoiceProcessor?.Start();

            if (!m_ConnectionManager.Running)
            {
                tbx_msgs.Text = "Connecting";
                button_start.IsEnabled = false;
                button_stop.IsEnabled = true;
                m_ConnectionManager.Start();
            }
        }

        //TODO: User-assigned key binding for toggle of the program
        private void button_stop_Click(object sender, RoutedEventArgs e)
        {
            VoiceProcessor?.Stop();
            m_ConnectionManager.Stop();
            button_start.IsEnabled = true;
            button_stop.IsEnabled = false;
            tbx_msgs.Text = "Stopped";
        }

        private void button_save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentProfile = CurrentProfile;
                if (currentProfile != null)
                {
                    if (currentProfile == Settings.Default)
                    {
                        var currentAircraft = CurrentAircraft;

                        if (!string.IsNullOrEmpty(currentAircraft) && !Settings.Profiles.ContainsKey(currentAircraft))
                        {
                            //is there any way to omit MessageBox usage?
                            var result = CustomMessageBox.ShowYesNoCancel(string.Format("Aircraft {0} profile not found. Would you like to create it or update default profile?", currentAircraft)
                                , "Select action"
                                , "Create new"
                                , "Update default"
                                , "Cancel"
                                , MessageBoxImage.Question
                            );
                            switch (result)
                            {
                                case MessageBoxResult.Yes:
                                    var newProfile = new AircraftProfile()
                                    {
                                        Name = currentAircraft,
                                        EnableG = cbx_g.IsChecked.Value,
                                        GForce = Convert.ToInt32(slider_G.Value),
                                        EnableAoA = cbx_a.IsChecked.Value,
                                        AoA = Convert.ToInt32(slider_A.Value),
                                        EnablePullUp = cbx_pullup.IsChecked.Value,
                                        EnableOverSpeed = cbx_overSpeed.IsChecked.Value,
                                        OverSpeed = Convert.ToInt32(tbx_overSpeed.Text),
                                        EnableFuel = cbx_fuel.IsChecked.Value,
                                        EnableGear = cbx_gear.IsChecked.Value,
                                        GearDown = Convert.ToInt32(tbx_gearDown.Text),
                                        GearUp = Convert.ToInt32(tbx_gearUp.Text),
                                        Voice = rb_rita.IsChecked.Value ? VoiceTemplate.RU_Rita : VoiceTemplate.US_Betty,
                                        Monitoring = cmb_Monitoring.Text
                                    };
                                    Settings.Profiles.Add(currentAircraft, newProfile);
                                    Profiles.Add(newProfile);
                                    CurrentProfile = newProfile;
                                    break;
                                case MessageBoxResult.No:
                                    break;
                                default:
                                    return;
                            }
                        }
                    }
                    currentProfile.EnableG = cbx_g.IsChecked.Value;
                    currentProfile.GForce = Convert.ToInt32(slider_G.Value);
                    currentProfile.EnableAoA = cbx_a.IsChecked.Value;
                    currentProfile.AoA = Convert.ToInt32(slider_A.Value);
                    currentProfile.EnablePullUp = cbx_pullup.IsChecked.Value;
                    currentProfile.EnableOverSpeed = cbx_overSpeed.IsChecked.Value;
                    currentProfile.OverSpeed = Convert.ToInt32(tbx_overSpeed.Text);
                    currentProfile.EnableFuel = cbx_fuel.IsChecked.Value;
                    currentProfile.EnableGear = cbx_gear.IsChecked.Value;
                    currentProfile.GearDown = Convert.ToInt32(tbx_gearDown.Text);
                    currentProfile.GearUp = Convert.ToInt32(tbx_gearUp.Text);
                    currentProfile.Voice = rb_rita.IsChecked.Value ? VoiceTemplate.RU_Rita : VoiceTemplate.US_Betty;
                    currentProfile.Monitoring = cmb_Monitoring.Text;

                    Settings.Save();
                }
            }
            catch (Exception ex)
            {
                //is there any way to omit MessageBox usage?
                MessageBox.Show(ex.Message);
            }
        }

        private void button_reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentProfile = CurrentProfile;
                if (currentProfile != null)
                {
                    currentProfile.Reset();
                    Settings.Save();
                    UpdateProfileUI(currentProfile);
                }
            }
            catch (Exception ex)
            {
                //is there any way to omit MessageBox usage?
                MessageBox.Show(ex.Message);
            }
        }

        private void button_help_Click(object sender, RoutedEventArgs e)
        {
            var helpFile = Path.Combine(Path.GetTempPath(), "wt-betty-help.txt");
            File.WriteAllText(helpFile, Properties.Resources.wt_betty_help);
            System.Diagnostics.Process.Start(helpFile);
        }

        private void cmb_profile_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateProfileUI(CurrentProfile);
        }

        private void button_remove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var toRemove = CurrentProfile;
                if (toRemove != null && toRemove != Settings.Default)
                {
                    CurrentProfile = Settings.Default;
                    Settings.Profiles.Remove(toRemove.Name);

                    Settings.Save();
                }
            }
            catch (Exception ex)
            {
                //is there any way to omit MessageBox usage?
                MessageBox.Show(ex.Message);
            }
        }
    }

    public class kphTomphConversion : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                int kph = System.Convert.ToInt32(value.ToString());
                double mph = kph / 1.609;
                double kts = kph / 1.852;
                String rounded_result = "(" + String.Format("{0:0}", Math.Truncate(mph * 10) / 10) + "mph / " + String.Format("{0:0}", Math.Truncate(kts * 10) / 10) +"kts)";
                return rounded_result;
            }
            catch (Exception/* ex*/)
            {
                //MessageBox.Show(ex.Message); //Annoying to have in a practical sense, keep it out of the builds
                return "ERROR";
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}
