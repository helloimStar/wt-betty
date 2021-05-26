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

        private ConnectionManager m_ConnectionManager = new ConnectionManager();
        private Settings Settings { get => Settings.Instance; }
        private VoiceProcessor VoiceProcessor { get; set; }
        private AircraftProfile CurrentProfile
        {
            get => (AircraftProfile)cmb_profile.SelectedItem;
            set
            {
                var profile = value;
                var oldValue = CurrentProfile;

                if (profile != oldValue) {
                    cmb_profile.SelectedItem = profile;
                    UpdateProfileUI(profile);
                }
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
            tbx_msgs.Text = (connected  ? "Running" : "Connection failed");
        }

        private void OnStateUpdated(object sender, EventArgs e)
        {
            var stateArgs = e as StateEventArgs;
            if (stateArgs.ErrorDetails == null)
                UpdateData(stateArgs.Indicator, stateArgs.State);
            else
                tbx_msgs.Text = stateArgs.ErrorDetails;
        }
        
        private void UpdateData(Indicator indicator, State state)
        {
            try
            {
                myIndicator = indicator;
                myState = state;

                var currentAircraft = CurrentAircraft;
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

                    //Console.Write(myState.AoS);
                    //tbx_msgs.Text = myState.AoS;
                    decimal AoS = Convert.ToDecimal(myState.AoS, culture);
                    int TAS = Convert.ToInt32(myState.TAS, culture);
                    label.Content = myIndicator.type;

                    //BINGO FUEL
                    if (cbx_fuel.IsChecked == true)
                    {
                        bool bingoFuel = Fuel / FuelFull < 103 && Fuel / FuelFull > 100 && Throttle > 0;
                        VoiceProcessor.BingoFuel(bingoFuel);
                    }
                    

                    //STALL WARNING
                    if (cbx_a.IsChecked == true)
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
                    if (cbx_g.IsChecked == true)
                    {
                        bool gOverload = G > currentProfile.GForce || (double)G < -0.4 * currentProfile.GForce;
                        if (gOverload)
                        {
                            if (G > currentProfile.GForce + 3 - currentProfile.GForce / (decimal)3)
                                VoiceProcessor.GMaximum();
                            else
                                VoiceProcessor.GOverLimit();
                        }
                        else
                        {
                            VoiceProcessor.GMaximum(false);
                            VoiceProcessor.GOverLimit(false);
                        }
                    }

                    //PULL UP Ground/sea level Proximity Warning
                    //desirable to have about 3 seconds before crash
                    if (cbx_pullup.IsChecked == true)
                    {
                        bool pullUp = 0 - Vspeed * (2 + (decimal)Math.Pow(IAS / 100, 0.7)) > Alt;
                        VoiceProcessor.PullUp(pullUp);
                    }

                    //Overspeed
                    if (cbx_overSpeed.IsChecked == true)
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
                tbx_msgs.Text = ex.Message;
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

            VoiceProcessor?.Stop();
            VoiceProcessor = VoiceProcessorFactory.GetProcessor(voice);
            VoiceProcessor?.Start();
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
                                        Voice = rb_rita.IsChecked.Value ? VoiceTemplate.RU_Rita : VoiceTemplate.US_Betty
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

                    Settings.Save();
                }
            }
            catch (Exception ex)
            {
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
