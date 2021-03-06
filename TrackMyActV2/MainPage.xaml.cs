﻿using System;
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
using TrackMyActV2.Pages;
using Windows.Storage;
using TrackMyActV2.Libraries;
using TrackMyActV2.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Collections;
using Windows.Graphics.Display;
using Microsoft.ApplicationInsights;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TrackMyActV2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Library library;
        public ObservableCollection<ActivityData> activity { get; set; }
        public ObservableCollection<TimerData> tmdata { get; set; }
        public Hashtable activityPos { get; set; }
        private RootObjectTrackAct rtrackact;
        public MainPage()
        {
            this.InitializeComponent();
            library = new Library();
            activity = new ObservableCollection<ActivityData>();
            tmdata = new ObservableCollection<TimerData>();
            tmdata.CollectionChanged += Tmdata_CollectionChanged;
            rtrackact = new RootObjectTrackAct();
            activityPos = new Hashtable();
            
            Loaded += MainPage_Loaded;
            ApplicationData.Current.RoamingSettings.Values["NewActivity"] = false;
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
        }

        private void Tmdata_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine("tmdata collectionchanged");            
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            tmdata.Clear();
            activity.Clear();
            activityPos.Clear();

            resume.Text = ApplicationData.Current.LocalSettings.Values["Resuming"] == null ? "Not initialized" : (string)ApplicationData.Current.LocalSettings.Values["Resuming"];
            ApplicationData.Current.LocalSettings.Values["Resuming"] = "temp_empty";
            //dataListView.ManipulationDelta += DataListView_ManipulationDelta;
            if (!await library.checkIfFileExists("activityDB"))
            {
                ApplicationData.Current.RoamingSettings.Values["FirstLaunch"] = true;
                Frame rootFrame = Window.Current.Content as Frame;
                ApplicationData.Current.LocalSettings.Values["TimerRunning"] = false;
                rootFrame.Navigate(typeof(TimerPage));
            }
            else
            {
                if (await library.checkIfFileExists("activityDB"))
                {
                    ApplicationData.Current.RoamingSettings.Values["FirstLaunch"] = false;
                    if (ApplicationData.Current.LocalSettings.Values["TimerRunning"] != null)
                    {
                        if ((bool)ApplicationData.Current.LocalSettings.Values["TimerRunning"] == true)
                        {
                            ApplicationData.Current.RoamingSettings.Values["FirstLaunch"] = false;
                            Frame rootFrame = Window.Current.Content as Frame;
                            rootFrame.Navigate(typeof(TimerPage));
                        }
                    }
                    string fileres = await library.readFile("activityDB");
                    rtrackact = TrackAct.trackactDataDeserializer(fileres);
                    updateUI(rtrackact);
                }
                else
                {
                    ApplicationData.Current.RoamingSettings.Values["FirstLaunch"] = false;
                    Frame rootFrame = Window.Current.Content as Frame;
                    rootFrame.Navigate(typeof(TimerPage));
                }              
                               
            }
        }

        private void dataListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ActivityData actd = (ActivityData)e.ClickedItem;
            Frame rootFrame = Window.Current.Content as Frame;
            ApplicationData.Current.RoamingSettings.Values["NewActivity"] = false;
            rootFrame.Navigate(typeof(TimerPage), actd.name);            
        }

        private void addNewAct_Click(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.RoamingSettings.Values["NewActivity"] = true;
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(TimerPage));
        }

        private async void deleteItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            int pos = -1;
            //Debug.WriteLine("In Delete Item");
            Button bt = sender as Button;
            ActivityData actd = (ActivityData)bt.DataContext;
            pos = (int)activityPos[actd.name];
            rtrackact.activity[pos].isDelete = true;
            //Debug.WriteLine("In Delete Item");
            tmdata.Clear();
            activity.Clear();
            activityPos.Clear();
            updateUI(rtrackact);
            rtrackact.activity.RemoveAt(pos);
            if(rtrackact.activity.Count > 0)
            {               
                string res = TrackAct.trackactSerializer(rtrackact);
                await library.writeFile("activityDB", res);
               
            }
            else
            {
                ApplicationData.Current.RoamingSettings.Values["FirstLaunch"] = true;
                library.deleteFile("activityDB");
                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(TimerPage));
            }
                    
        }

        private  void updateUI(RootObjectTrackAct rtact)
        {
            var tc = new TelemetryClient();
            var activityD = rtact.activity;
            int i = 0;
            foreach (var actv in activityD)
            {
                if (actv.isDelete == false)
                {
                    activity.Add(actv);
                    activityPos.Add(actv.name, i);
                    i++;
                }
            }
            //var timedata = rtrackact.activity[0].timer_data;
            //foreach (var tdata in timedata)
            //{
            //    tmdata.Add(tdata);
            //}
            tc.TrackMetric("Number of Activities", rtact.activity.Count);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(Settings));
        }
    }

    public class BooleanToVisibilityConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((string)value == "")
            {
                return Visibility.Collapsed;
            }
            else if (value == null)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

    }

    public class secondsToStringConverter : IValueConverter
    {
        private Library lib = new Library();
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ("We have spent " + lib.convertSecondsToString((long)value) + " on this activity");                                     
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

    }

}
