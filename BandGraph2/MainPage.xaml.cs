using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Band;
using Windows.UI.Popups;
using Windows.UI.Core;
using Microsoft.Band.Notifications;
using System.Threading.Tasks;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Provider;
using Windows.UI.ViewManagement;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace BandGraph2
{

    public class Reading
    {
        public string date { get; set; }
        public int rate { get; set; } 
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public bool toggled { get; set; }

        public List<Reading> readingList = new List<Reading>();

        public MainPage()
        {
            this.InitializeComponent();
           // HeartRate = 50;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            toggled = false;

        }

        private void Bttn_Go(object sender, RoutedEventArgs e)
        {
            toggled = true;
            getBands();
        }

        private void Bttn_Export(object sender, RoutedEventArgs e)
        {
            toCSV();
        }

        private void Bttn_toggle(object sender, RoutedEventArgs e)
        {
            toggled = false;
        }

        private async void toCSV()
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("CSV", new List<string>() { ".csv" });
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "Exported_Readings";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                await FileIO.WriteTextAsync(file, "\"Time\",\"Heart Rate\"\n");

                foreach( Reading reading in readingList) {
                    await FileIO.AppendTextAsync(file, reading.date+","+reading.rate+"\n");
                }

                // Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete)
                {
                    //OutputTextBlock.Text = "File " + file.Name + " was saved.";

                    ToastTemplateType toastType = ToastTemplateType.ToastText02;

                    XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastType);

                    XmlNodeList toastTextElement = toastXml.GetElementsByTagName("text");
                    toastTextElement[0].AppendChild(toastXml.CreateTextNode("Band Graph Success"));
                    toastTextElement[1].AppendChild(toastXml.CreateTextNode("File " + file.Name + " was saved."));

                    IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                    ((XmlElement)toastNode).SetAttribute("duration", "long");

                    ToastNotification toast = new ToastNotification(toastXml);
                    ToastNotificationManager.CreateToastNotifier().Show(toast); 
                }
                else
                {
                    ToastTemplateType toastType = ToastTemplateType.ToastText02;

                    XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastType);

                    XmlNodeList toastTextElement = toastXml.GetElementsByTagName("text");
                    toastTextElement[0].AppendChild(toastXml.CreateTextNode("Band Graph Failure"));
                    toastTextElement[1].AppendChild(toastXml.CreateTextNode("File " + file.Name + " was not saved."));

                    IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                    ((XmlElement)toastNode).SetAttribute("duration", "long");

                    ToastNotification toast = new ToastNotification(toastXml);
                    ToastNotificationManager.CreateToastNotifier().Show(toast); 
                }
            }
            else
            {
                //OutputTextBlock.Text = "Operation cancelled.";
            }
        }



        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        public async void getBands()
        {
            ExportButton.IsEnabled = false;
            var bandManager = BandClientManager.Instance;
            IBandInfo[] pairedBands = await bandManager.GetBandsAsync();

            try
            {
                using (IBandClient bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {

                    if (bandClient.SensorManager.HeartRate.GetCurrentUserConsent() != UserConsent.Granted)
                    {
                        await bandClient.SensorManager.HeartRate.RequestUserConsentAsync();
                    }


                    IEnumerable<TimeSpan> supportedHeartBeatReportingIntervals = bandClient.SensorManager.HeartRate.SupportedReportingIntervals;
                    bandClient.SensorManager.HeartRate.ReportingInterval = supportedHeartBeatReportingIntervals.First<TimeSpan>();

                    bandClient.SensorManager.HeartRate.ReadingChanged += (s, args) =>
                    {
                        int heartRate = args.SensorReading.HeartRate;
                        Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {// HeartRate = args.SensorReading.HeartRate;
                                HR.Text = args.SensorReading.HeartRate.ToString();
                            });

                        readingList.Add(new Reading() { date = DateTime.Now.ToString("mm:ss"), rate = heartRate });
                    };

                   // while (true)
                    //{
                        try
                        {
                            // send a vibration request of type alert alarm to the Band
                            await
                           bandClient.NotificationManager.VibrateAsync(VibrationType.OneToneHigh);
                        }
                        catch (BandException)
                        {
                            // handle a Band connection exception
                        }
                  //  }

                    await bandClient.SensorManager.HeartRate.StartReadingsAsync();

                    while (toggled)
                    {   
                        await Task.Delay(TimeSpan.FromSeconds(1));
 
                    }
                    await bandClient.SensorManager.HeartRate.StopReadingsAsync();

                    (LineChart.Series[0] as LineSeries).ItemsSource = readingList;
                    ExportButton.IsEnabled = true;
                }
            }
            catch (BandException)
            {
                
            }

        }

    }

}
