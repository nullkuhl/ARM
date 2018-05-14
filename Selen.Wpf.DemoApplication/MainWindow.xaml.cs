using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using MahApps.Metro.Controls;
using System.Windows.Data;
using Ionic.Zip;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Management;
using System.Text;

namespace Airmech.Replays.ARM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
   
    public partial class MainWindow
    {
        static int fullScreenHandle = -1;
        List<Replay> replayData;
        Storyboard videoPanelSB;
        Storyboard sideBarSB;
        Storyboard logoSB;
        Storyboard backButtonSB;
        Storyboard steamPanelSB;

        string[] replayFilesSteam;
        string[] replayFiles;

        bool steamGameMode = false;
        bool windowsGameMode = false;
        string selectedReplayGlobal = "";

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);


        [DllImport("User32.Dll")]
        private static extern void GetClassName(int hWnd, StringBuilder s, int nMaxCount);


        [DllImport("user32.dll")]
        private static extern int EnumWindows(CallBackPtr callPtr, int lPar);

        public delegate bool CallBackPtr(int hwnd, int lParam);
        private static CallBackPtr callBackPtr;


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern int GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(int hwnd);

        [DllImport("user32.dll")]
        public static extern long GetWindowRect(int hWnd, ref Rectangle lpRect);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out uint processId);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hwnd);

        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };



        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public static bool Report(int hWnd, int lParam)
        {
            int size = 256;
            StringBuilder sb = new StringBuilder(size);
            GetClassName(hWnd, sb, size);
            if ((sb.ToString().Contains("AirMech")) && (sb.ToString().Contains("LetterBox")))
                fullScreenHandle = hWnd;
            return true;
        }
        public MainWindow()
        {
            System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMechSteam\.debug\replay");
            System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMech\.debug\replay");
            InitializeComponent();
            int steamVersion = GetSteamGameVersion();
            int gameVersion = GetGameVersion();
            int steamReplays = GetSteamReplaysCount();
            int gameReplays = GetGameReplaysCount();

            videoPanelSB = new Storyboard();


            if(steamVersion==-1)
            {
                panelSteam.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/images/panel_bw.png"));
                btnSteamApp.Visibility = Visibility.Hidden;
                steamAppLogo.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/images/steamLogo_bw.png"));
            }
            else
            {
                steamLabelVersionValue.Content = steamVersion;
                steamReplaysCount.Content = steamReplays;
            }
            if (gameVersion == -1)
            {
                panelApp.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/images/panel_bw.png"));
                btnWinApp.Visibility = Visibility.Hidden;
                winAppLogo.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/images/airmechLogo_bw.png"));
            }
            else
            {
                winLabelVersionValue.Content = gameVersion;
                winReplaysCount.Content = gameReplays;
            }
            if(((gameVersion == -1))&& (steamVersion == -1))
            {
                panelArm.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/images/panel_bw.png"));
                btnArmApp.Visibility = Visibility.Hidden;
            }

            videoDisplayGrid.UpdateLayout();
            DoubleAnimation slideUp = new DoubleAnimation();
            slideUp.From = 1800;
            slideUp.To = 810;
            slideUp.Duration = new Duration(TimeSpan.FromMilliseconds(1000.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideUp, videoDisplayGrid);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            videoPanelSB.Children.Add(slideUp);

            logoSB = new Storyboard();

            DoubleAnimation slideDown = new DoubleAnimation();
            slideDown.From = 0.0;
            slideDown.To = 150;
            slideDown.Duration = new Duration(TimeSpan.FromMilliseconds(1000.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDown, appLogo);
            Storyboard.SetTargetProperty(slideDown, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            logoSB.Children.Add(slideDown);

            sideBarSB = new Storyboard();

            DoubleAnimation slideRight = new DoubleAnimation();
            slideRight.From = 0.0;
            slideRight.To = 1027;
            slideRight.Duration = new Duration(TimeSpan.FromMilliseconds(1000.0));
            // Set the target of the animation
            Storyboard.SetTarget(slideRight, sideBarGrid);
            Storyboard.SetTargetProperty(slideRight, new PropertyPath("RenderTransform.(TranslateTransform.X)"));

            // Kick the animation off
            sideBarSB.Children.Add(slideRight);
           
            replayFilesSteam = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMechSteam\.debug\replay", "*.replayInfo");
            replayFiles = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMech\.debug\replay", "*.replayInfo");

            

         
        }

        private int GetSteamReplaysCount()
        {
            try
            { 
                return Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMechSteam\.debug\replay", "*.replayInfo").Length;
            }
            catch
            {
                return 0;
            }
        }

        private int GetGameReplaysCount()
        {
            try
            {
                return Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMech\.debug\replay", "*.replayInfo").Length;
            }
            catch
            {
                return 0;
            }
        }
        private void uploadButton_Click(object sender, RoutedEventArgs e)
        {
            

            var selectedReplay = replayList.SelectedItem;
            if (selectedReplay != null)
            {
                String replayName = ((replayItem)selectedReplay).Name;
                using (ZipFile zip = new ZipFile())
                {
                    zip.AddFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMechSteam\.debug\replay\" + replayName + ".replay");
                    zip.AddFile(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMechSteam\.debug\replay\" + replayName + ".replayInfo");
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    //zip.UseZip64WhenSaving = Zip64Option.Always;
                    zip.Save("test_"+replayName+".arm");
                }
            }

        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            playButton.IsEnabled = true;
            uploadButton.IsEnabled = true;
            versusText.Visibility = Visibility.Visible;
            player1Hero.Visibility = Visibility.Visible;
            player1Name.Visibility = Visibility.Visible;
            player1NameBar.Visibility = Visibility.Visible;
            player1Shadow.Visibility = Visibility.Visible;

            player2Hero.Visibility = Visibility.Visible;
            player2Name.Visibility = Visibility.Visible;
            player2NameBar.Visibility = Visibility.Visible;
            player2Shadow.Visibility = Visibility.Visible;
            videoPanelScanLines.Opacity = 0.8;

            GradientStopCollection BlueGrad = new GradientStopCollection();
            BlueGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF0049A0"), 1));
            BlueGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF177BF3"), 0.808));
            BlueGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7F023572"), 0.8));

            GradientStopCollection RedGrad = new GradientStopCollection();
            RedGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFA00202"), 1));
            RedGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFF31717"), 0.808));
            RedGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7F720202"), 0.8));

            GradientStopCollection GreenGrad = new GradientStopCollection();
            GreenGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF43531E"), 1));
            GreenGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF579144"), 0.808));
            GreenGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7F152905"), 0.8));

            GradientStopCollection YellowGrad = new GradientStopCollection();
            YellowGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF71470C"), 1));
            YellowGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFB2762D"), 0.808));
            YellowGrad.Add(new GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7F231403"), 0.8));


            var images = typeof(Properties.Resources)
            .GetProperties(BindingFlags.Static | BindingFlags.NonPublic |
                                                 BindingFlags.Public)
            .Where(p => p.PropertyType == typeof(Bitmap))
            .Select(x => new { Name = x.Name, Image = x.GetValue(null, null) })
            .ToList();
            var list = sender as ListBox;
            var selectedReplay = list.SelectedItem;
            if (selectedReplay != null)
            {
                String replayName = ((replayItem)selectedReplay).Name;
                selectedReplayGlobal = replayName;
                Replay currentReplay = new Replay();
                foreach (Replay r in replayData)
                {
                    if (r.fileName == replayName)
                    {
                        currentReplay = r;
                        break;
                    }
                }
                player1Name.Content = currentReplay.p1Data["name"].ToUpper();
                player2Name.Content = currentReplay.p2Data["name"].ToUpper();
                player3Name.Content = currentReplay.p3Data["name"].ToUpper();
                player4Name.Content = currentReplay.p4Data["name"].ToUpper();
                bool p3 = false, p4 = false;
                if (player3Name.Content.ToString().EndsWith("*"))
                {
                    p3 = false;
                    player3Hero.Visibility = Visibility.Hidden;
                    player3NameBar.Visibility = Visibility.Hidden;
                    player3Name.Visibility = Visibility.Hidden;
                    player3Shadow.Visibility = Visibility.Hidden;
                }
                else
                {
                    p3 = true;
                    player3Hero.Visibility = Visibility.Visible;
                    player3NameBar.Visibility = Visibility.Visible;
                    player3Name.Visibility = Visibility.Visible;
                    player3Shadow.Visibility = Visibility.Visible;
                }
                if (player4Name.Content.ToString().EndsWith("*"))
                {
                    p4 = false;
                    player4Hero.Visibility = Visibility.Hidden;
                    player4NameBar.Visibility = Visibility.Hidden;
                    player4Name.Visibility = Visibility.Hidden;
                    player4Shadow.Visibility = Visibility.Hidden;
                }
                else
                {
                    p4 = true;
                    player4Hero.Visibility = Visibility.Visible;
                    player4NameBar.Visibility = Visibility.Visible;
                    player4Name.Visibility = Visibility.Visible;
                    player4Shadow.Visibility = Visibility.Visible;
                }
                mapName.Visibility = Visibility.Visible;
                if (currentReplay.map != "Simple")
                {
                    mapName.Content = currentReplay.map.ToUpper();
                }
                else
                {
                    mapName.Content = "DUST";
                }
                mapPreview.Visibility = Visibility.Visible;
                mapPreview.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/images/" + mapName.Content.ToString().ToLower() + ".jpg"));
                miniMap.Visibility = Visibility.Visible;
                miniMap.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/images/" + mapName.Content.ToString().ToLower() + "_mini.jpg"));
                mapBar.Visibility = Visibility.Visible;
                miniMapBorder.Visibility = Visibility.Visible;

                string p1MechColor = currentReplay.p1Data["color"];
                int outInt = 0;
                bool isLastCharNumeric = int.TryParse(p1MechColor[p1MechColor.Length - 1].ToString(), out outInt);
                if (isLastCharNumeric)
                {
                    //chop off the last char
                    p1MechColor = p1MechColor.Remove(p1MechColor.Length - 1);
                }
                string p1AirMech = (currentReplay.p1Data["airmech"] + "_" + p1MechColor + ".png").ToLower().Replace("_super", "u"); ;

                if (ResourceExists(("mechs/" + p1AirMech.ToLower())))
                {
                    player1Hero.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/mechs/" + p1AirMech.ToLower()));
                }
                else
                {
                    p1AirMech = p1AirMech.Substring(0, p1AirMech.LastIndexOf('_')) + "_unique.png";
                    if (ResourceExists(("mechs/" + p1AirMech.ToLower())))
                    {
                        player1Hero.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/mechs/" + p1AirMech.ToLower()));
                    }
                    else
                    {
                        MessageBox.Show(p1AirMech.ToLower());
                    }
                }

                if(p1MechColor=="Blue")
                {
                    LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(BlueGrad)
                    {
                        StartPoint = new System.Windows.Point(0.5, 0),
                        EndPoint = new System.Windows.Point(0.5, 1)
                    };

                    player1NameBar.Fill = backgroundLinearBrush;
                }
                else if (p1MechColor == "Red")
                {
                    LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(RedGrad)
                    {
                        StartPoint = new System.Windows.Point(0.5, 0),
                        EndPoint = new System.Windows.Point(0.5, 1)
                    };

                    player1NameBar.Fill = backgroundLinearBrush;
                }
                else if (p1MechColor == "Green")
                {
                    LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(GreenGrad)
                    {
                        StartPoint = new System.Windows.Point(0.5, 0),
                        EndPoint = new System.Windows.Point(0.5, 1)
                    };

                    player1NameBar.Fill = backgroundLinearBrush;
                }
                else if (p1MechColor == "Yellow")
                {
                    LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(YellowGrad)
                    {
                        StartPoint = new System.Windows.Point(0.5, 0),
                        EndPoint = new System.Windows.Point(0.5, 1)
                    };

                    player1NameBar.Fill = backgroundLinearBrush;
                }

                //player 2
                string p2MechColor = "";
                try
                {
                    p2MechColor = currentReplay.p2Data["color"];
                }
                catch
                {
                    p2MechColor = "Blue";
                }
                outInt = 0;
                isLastCharNumeric = int.TryParse(p2MechColor[p2MechColor.Length - 1].ToString(), out outInt);
                if (isLastCharNumeric)
                {
                    //chop off the last char
                    p2MechColor = p2MechColor.Remove(p2MechColor.Length - 1);
                }
                string p2AirMech = (currentReplay.p2Data["airmech"] + "_" + p2MechColor + ".png").ToLower().Replace("_super", "u"); ;

                if (ResourceExists(("mechs/" + p2AirMech.ToLower())))
                {
                    player2Hero.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/mechs/" + p2AirMech.ToLower()));
                }
                else
                {
                    p2AirMech = p2AirMech.Substring(0, p2AirMech.LastIndexOf('_')) + "_unique.png";
                    if (ResourceExists(("mechs/" + p2AirMech.ToLower())))
                    {
                        player2Hero.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/mechs/" + p2AirMech.ToLower()));
                    }
                    else
                    {
                        MessageBox.Show(p2AirMech.ToLower());
                    }
                }

                if (p2MechColor == "Blue")
                {
                    LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(BlueGrad)
                    {
                        StartPoint = new System.Windows.Point(0.5, 0),
                        EndPoint = new System.Windows.Point(0.5, 1)
                    };

                    player2NameBar.Fill = backgroundLinearBrush;
                }
                else if (p2MechColor == "Red")
                {
                    LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(RedGrad)
                    {
                        StartPoint = new System.Windows.Point(0.5, 0),
                        EndPoint = new System.Windows.Point(0.5, 1)
                    };

                    player2NameBar.Fill = backgroundLinearBrush;
                }
                else if (p2MechColor == "Green")
                {
                    LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(GreenGrad)
                    {
                        StartPoint = new System.Windows.Point(0.5, 0),
                        EndPoint = new System.Windows.Point(0.5, 1)
                    };

                    player2NameBar.Fill = backgroundLinearBrush;
                }
                else if (p2MechColor == "Yellow")
                {
                    LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(YellowGrad)
                    {
                        StartPoint = new System.Windows.Point(0.5, 0),
                        EndPoint = new System.Windows.Point(0.5, 1)
                    };

                    player2NameBar.Fill = backgroundLinearBrush;
                }

                //player 3
                if (p3)
                {
                    string p3MechColor = "";
                    try
                    {
                        p3MechColor = currentReplay.p3Data["color"];
                    }
                    catch
                    {
                        p3MechColor = "Blue";
                    }
                    outInt = 0;
                    isLastCharNumeric = int.TryParse(p3MechColor[p3MechColor.Length - 1].ToString(), out outInt);
                    if (isLastCharNumeric)
                    {
                        //chop off the last char
                        p3MechColor = p3MechColor.Remove(p3MechColor.Length - 1);
                    }
                    string p3AirMech = (currentReplay.p3Data["airmech"] + "_" + p3MechColor + ".png").ToLower().Replace("_super", "u"); ;

                    if (ResourceExists(("mechs/" + p3AirMech.ToLower())))
                    {
                        player3Hero.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/mechs/" + p3AirMech.ToLower()));
                    }
                    else
                    {
                        p3AirMech = p3AirMech.Substring(0, p3AirMech.LastIndexOf('_')) + "_unique.png";
                        if (ResourceExists(("mechs/" + p3AirMech.ToLower())))
                        {
                            player3Hero.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/mechs/" + p3AirMech.ToLower()));
                        }
                        else
                        {
                            MessageBox.Show(p3AirMech.ToLower());
                        }
                    }
                    if (p3MechColor == "Blue")
                    {
                        LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(BlueGrad)
                        {
                            StartPoint = new System.Windows.Point(0.5, 0),
                            EndPoint = new System.Windows.Point(0.5, 1)
                        };

                        player3NameBar.Fill = backgroundLinearBrush;
                    }
                    else if (p3MechColor == "Red")
                    {
                        LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(RedGrad)
                        {
                            StartPoint = new System.Windows.Point(0.5, 0),
                            EndPoint = new System.Windows.Point(0.5, 1)
                        };

                        player3NameBar.Fill = backgroundLinearBrush;
                    }
                    else if (p3MechColor == "Green")
                    {
                        LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(GreenGrad)
                        {
                            StartPoint = new System.Windows.Point(0.5, 0),
                            EndPoint = new System.Windows.Point(0.5, 1)
                        };

                        player3NameBar.Fill = backgroundLinearBrush;
                    }
                    else if (p3MechColor == "Yellow")
                    {
                        LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(YellowGrad)
                        {
                            StartPoint = new System.Windows.Point(0.5, 0),
                            EndPoint = new System.Windows.Point(0.5, 1)
                        };

                        player3NameBar.Fill = backgroundLinearBrush;
                    }

                }

                //player 4
                if (p4)
                {
                    string p4MechColor = "";
                    try
                    {
                        p4MechColor = currentReplay.p4Data["color"];
                    }
                    catch
                    {
                        p4MechColor = "Blue";
                    }
                    outInt = 0;
                    isLastCharNumeric = int.TryParse(p4MechColor[p4MechColor.Length - 1].ToString(), out outInt);
                    if (isLastCharNumeric)
                    {
                        //chop off the last char
                        p4MechColor = p4MechColor.Remove(p4MechColor.Length - 1);
                    }
                    string p4AirMech = (currentReplay.p4Data["airmech"] + "_" + p4MechColor + ".png").ToLower().Replace("_super", "u"); ;

                    if (ResourceExists(("mechs/" + p4AirMech.ToLower())))
                    {
                        player4Hero.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/mechs/" + p4AirMech.ToLower()));
                    }
                    else
                    {
                        p4AirMech = p4AirMech.Substring(0, p4AirMech.LastIndexOf('_')) + "_unique.png";
                        if (ResourceExists(("mechs/" + p4AirMech.ToLower())))
                        {
                            player4Hero.Source = new BitmapImage(new Uri("pack://application:,,,/ARM;component/mechs/" + p4AirMech.ToLower()));
                        }
                        else
                        {
                            MessageBox.Show(p4AirMech.ToLower());
                        }
                    }
                    if (p4MechColor == "Blue")
                    {
                        LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(BlueGrad)
                        {
                            StartPoint = new System.Windows.Point(0.5, 0),
                            EndPoint = new System.Windows.Point(0.5, 1)
                        };

                        player4NameBar.Fill = backgroundLinearBrush;
                    }
                    else if (p4MechColor == "Red")
                    {
                        LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(RedGrad)
                        {
                            StartPoint = new System.Windows.Point(0.5, 0),
                            EndPoint = new System.Windows.Point(0.5, 1)
                        };

                        player4NameBar.Fill = backgroundLinearBrush;
                    }
                    else if (p4MechColor == "Green")
                    {
                        LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(GreenGrad)
                        {
                            StartPoint = new System.Windows.Point(0.5, 0),
                            EndPoint = new System.Windows.Point(0.5, 1)
                        };

                        player4NameBar.Fill = backgroundLinearBrush;
                    }
                    else if (p4MechColor == "Yellow")
                    {
                        LinearGradientBrush backgroundLinearBrush = new LinearGradientBrush(YellowGrad)
                        {
                            StartPoint = new System.Windows.Point(0.5, 0),
                            EndPoint = new System.Windows.Point(0.5, 1)
                        };

                        player4NameBar.Fill = backgroundLinearBrush;
                    }

                }
            }
            else
            {
                playButton.IsEnabled = false;
                uploadButton.IsEnabled = false;
                versusText.Visibility = Visibility.Hidden;
                player1Hero.Visibility = Visibility.Hidden;
                player1Name.Visibility = Visibility.Hidden;
                player1NameBar.Visibility = Visibility.Hidden;
                player1Shadow.Visibility = Visibility.Hidden;

                player2Hero.Visibility = Visibility.Hidden;
                player2Name.Visibility = Visibility.Hidden;
                player2NameBar.Visibility = Visibility.Hidden;
                player2Shadow.Visibility = Visibility.Hidden;

                player3Hero.Visibility = Visibility.Hidden;
                player3Name.Visibility = Visibility.Hidden;
                player3NameBar.Visibility = Visibility.Hidden;
                player3Shadow.Visibility = Visibility.Hidden;

                player4Hero.Visibility = Visibility.Hidden;
                player4Name.Visibility = Visibility.Hidden;
                player4NameBar.Visibility = Visibility.Hidden;
                player4Shadow.Visibility = Visibility.Hidden;

                mapBar.Visibility = Visibility.Hidden;
                mapName.Visibility = Visibility.Hidden;
                mapPreview.Visibility = Visibility.Hidden;
                miniMap.Visibility = Visibility.Hidden;
                miniMapBorder.Visibility = Visibility.Hidden;
                videoPanelScanLines.Opacity = 0.6;
            }

            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // setting cancel to true will cancel the close request
            // so the application is not closed
            e.Cancel = true;

            this.Hide();

            base.OnClosing(e);
        }

        public static bool ResourceExists(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();

            return ResourceExists(assembly, resourcePath);
        }

        public static bool ResourceExists(Assembly assembly, string resourcePath)
        {
            return GetResourcePaths(assembly)
                .Contains(resourcePath);
        }

        public static IEnumerable<object> GetResourcePaths(Assembly assembly)
        {
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var resourceName = assembly.GetName().Name + ".g";
            var resourceManager = new ResourceManager(resourceName, assembly);

            try
            {
                var resourceSet = resourceManager.GetResourceSet(culture, true, true);

                foreach (System.Collections.DictionaryEntry resource in resourceSet)
                {
                    yield return resource.Key;
                }
            }
            finally
            {
                resourceManager.ReleaseAllResources();
            }
        }

        private void filterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            string[] replayFilesSteam = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMechSteam\.debug\replay", "*.replayInfo");
            string[] replayFiles = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMech\.debug\replay", "*.replayInfo");

            string[] replayFilesData = new string[0];
            if(steamGameMode)
            {
                replayFilesData = replayFilesSteam;
            }
            else
            {
                replayFilesData = replayFiles;
            }

            replayData = new List<Replay>();
            for (int i = 0; i < replayFilesData.Length; i++)
            {
                Replay r = new Replay();
                r.parseReplayInfo(replayFilesData[i]);
                string filterText = filterTextBox.Text.ToString().ToLower();
                if (r.maxPlayers == r.netPlayers)
                    if(filterText.Length>0)
                    {
                        if(r.map.ToLower().Contains(filterText))
                        {
                            replayData.Add(r);
                        }
                        else if(r.p1Data["name"].ToLower().Contains(filterText))
                        {
                            replayData.Add(r);
                        }
                        else if (r.p2Data["name"].ToLower().Contains(filterText))
                        {
                            replayData.Add(r);
                        }
                        else if (r.p3Data["name"].ToLower().Contains(filterText))
                        {
                            replayData.Add(r);
                        }
                        else if (r.p4Data["name"].ToLower().Contains(filterText))
                        {
                            replayData.Add(r);
                        }
                    }
                    else
                    {
                        replayData.Add(r);
                    }
                    
            }

            replayData.Sort((x, y) => DateTime.Compare(y.localtime, x.localtime));

            List<replayItem> items = new List<replayItem>();
            foreach (Replay r in replayData)
            {
                items.Add(new replayItem() { Name = r.fileName, Version = r.version,maxPlayers=r.maxPlayers+"",netPlayers=r.netPlayers+"", Time = r.localtime.ToShortTimeString(), Date = r.localtime.Date.ToString("ddd, dd MMM yyy") });
            }

            replayList.ItemsSource = items;
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            filterTextBox_TextChanged(null, null);
        }

        public int GetSteamGameVersion()
        {
            try
            {
                
              RegistryKey productKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 206500");

                string[] list= productKey.GetValueNames();


            string airMechSteamPath = "";
                if (productKey != null)
                {                                     
                        string path = productKey.GetValue("InstallLocation").ToString();
                    //airMechSteamPath = path.Substring(0, path.LastIndexOf("\\"));
                    airMechSteamPath = path;
                    string[] lines = System.IO.File.ReadAllLines(airMechSteamPath+"\\.version");
                        return int.Parse(lines[0]);                                     
                }
                else
                {
                    return -1;
                 }
           
            }
            catch
            {
                return -1;
            }
        }

        public int GetGameVersion()
        {

            try
            { 
                string[] lines = System.IO.File.ReadAllLines(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMech\.version");
                return int.Parse(lines[0]);
            }
            catch
            {
                return -1;
            }
           
        }

        private void btnWinApp_Click(object sender, RoutedEventArgs e)
        {
            //  WindowsGrid.Visibility = Visibility.Hidden;
            //WindowsGrid.set = Visibility.Hidden;
            steamGameMode = false;
            windowsGameMode = true;

            Storyboard winAppPanelSB = new Storyboard();
           
            DoubleAnimation fadeOutAnimation = new DoubleAnimation()
            { From = 1.0, To = 0.0, Duration = new Duration(TimeSpan.FromMilliseconds(500)) };

            Storyboard.SetTarget(fadeOutAnimation, WindowsGrid);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(Control.OpacityProperty));

            fadeOutAnimation.Completed += new EventHandler(fadeOutWinPanelCompleted);
            winAppPanelSB.Children.Add(fadeOutAnimation);
         //   winAppPanelSB.Begin();

            fadeOutWinPanelCompleted(null, null);
            replayData = new List<Replay>();
            for (int i = 0; i < replayFiles.Length; i++)
            {
                Replay r = new Replay();
                r.parseReplayInfo(replayFiles[i]);
                if (r.maxPlayers == r.netPlayers)
                    replayData.Add(r);
            }

            replayData.Sort((x, y) => DateTime.Compare(y.localtime, x.localtime));

            List<replayItem> items = new List<replayItem>();
            foreach (Replay r in replayData)
            {
                items.Add(new replayItem() { Name = r.fileName, Version = r.version, maxPlayers = r.maxPlayers + "", netPlayers = r.netPlayers + "", Time = r.localtime.ToShortTimeString(), Date = r.localtime.Date.ToString("ddd, dd MMM yyy") });
            }

            replayList.ItemsSource = items;


        }
        private void fadeOutWinPanelCompleted(object sender, EventArgs e)
        {
            DoubleAnimation slideUp = new DoubleAnimation();
            slideUp.From = 0.0;
            slideUp.To = -530;
            slideUp.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            Storyboard airMechLogoSB = new Storyboard();
            Storyboard.SetTarget(slideUp, appLogoLarge);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));
            airMechLogoSB.Children.Add(slideUp);
            airMechLogoSB.Begin();

            videoPanelSB.Begin();
            sideBarSB.Begin();
            logoSB.Begin();


            Storyboard steamSB = new Storyboard();

            DoubleAnimation slideDown = new DoubleAnimation();
            slideDown.From = 0.0;
            slideDown.To = 530;
            slideDown.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDown, SteamGrid);
            Storyboard.SetTargetProperty(slideDown, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            steamSB.Children.Add(slideDown);
            steamSB.Begin();

            Storyboard armSB = new Storyboard();

            slideDown = new DoubleAnimation();
            slideDown.From = 0.0;
            slideDown.To = 530;
            slideDown.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDown, ArmGrid);
            Storyboard.SetTargetProperty(slideDown, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            armSB.Children.Add(slideDown);
            armSB.Begin();


            Storyboard winSB = new Storyboard();

            slideDown = new DoubleAnimation();
            slideDown.From = 0.0;
            slideDown.To = 530;
            slideDown.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDown, WindowsGrid);
            Storyboard.SetTargetProperty(slideDown, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            winSB.Children.Add(slideDown);
            winSB.Begin();
        }
        private void fadeOutSteamPanelCompleted(object sender, EventArgs e)
        {
            DoubleAnimation slideUp = new DoubleAnimation();
            slideUp.From = 0.0;
            slideUp.To = -530;
            slideUp.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            Storyboard airMechLogoSB = new Storyboard();
            Storyboard.SetTarget(slideUp, appLogoLarge);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));
            airMechLogoSB.Children.Add(slideUp);
            airMechLogoSB.Begin();

            videoPanelSB.Begin();
            sideBarSB.Begin();
            logoSB.Begin();


            Storyboard winSB = new Storyboard();

            DoubleAnimation slideDown = new DoubleAnimation();
            slideDown.From = 0.0;
            slideDown.To = 530;
            slideDown.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDown, WindowsGrid);
            Storyboard.SetTargetProperty(slideDown, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            winSB.Children.Add(slideDown);
            winSB.Begin();

            Storyboard armSB = new Storyboard();

            slideDown = new DoubleAnimation();
            slideDown.From = 0.0;
            slideDown.To = 530;
            slideDown.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDown, ArmGrid);
            Storyboard.SetTargetProperty(slideDown, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            armSB.Children.Add(slideDown);
            armSB.Begin();


            Storyboard steamSB = new Storyboard();

            slideDown = new DoubleAnimation();
            slideDown.From = 0.0;
            slideDown.To = 530;
            slideDown.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDown, SteamGrid);
            Storyboard.SetTargetProperty(slideDown, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            steamSB.Children.Add(slideDown);
            steamSB.Begin();
        }
        private void btnSteamApp_Click(object sender, RoutedEventArgs e)
        {

            steamGameMode = true;
            windowsGameMode = false;

            steamPanelSB = new Storyboard();

            DoubleAnimation fadeOutAnimation = new DoubleAnimation()
            { From = 1.0, To = 0.0, Duration = new Duration(TimeSpan.FromMilliseconds(500)) };

            Storyboard.SetTarget(fadeOutAnimation, SteamGrid);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(Control.OpacityProperty));

            fadeOutAnimation.Completed += new EventHandler(fadeOutSteamPanelCompleted);
            steamPanelSB.Children.Add(fadeOutAnimation);
            // steamPanelSB.Begin();
            fadeOutSteamPanelCompleted(null,null);

             replayData = new List<Replay>();
            for (int i = 0; i < replayFilesSteam.Length; i++)
            {
                Replay r = new Replay();
                r.parseReplayInfo(replayFilesSteam[i]);
                if (r.maxPlayers == r.netPlayers)
                    replayData.Add(r);
            }

            replayData.Sort((x, y) => DateTime.Compare(y.localtime, x.localtime));

            List<replayItem> items = new List<replayItem>();
            foreach (Replay r in replayData)
            {
                items.Add(new replayItem() { Name = r.fileName, Version = r.version, maxPlayers = r.maxPlayers + "", netPlayers = r.netPlayers + "", Time = r.localtime.ToShortTimeString(), Date = r.localtime.Date.ToString("ddd, dd MMM yyy") });
            }

            replayList.ItemsSource = items;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            selectedReplayGlobal = "";
            filterTextBox.Text = "";
            //btnBack.Margin.Left.ToString();
            //steamPanelSB.Pause();
            //steamPanelSB.Seek(TimeSpan.FromMilliseconds(00.0));

            backButtonSB = new Storyboard();
            DoubleAnimation slideRight = new DoubleAnimation();
            slideRight.To = 195;
            slideRight.Duration = new Duration(TimeSpan.FromMilliseconds(250.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideRight, btnBack);
            Storyboard.SetTargetProperty(slideRight, new PropertyPath("RenderTransform.(TranslateTransform.X)"));

            // Kick the animation off
            backButtonSB.Children.Add(slideRight);

            Storyboard videoPanelOut= new Storyboard();
            DoubleAnimation slideUp = new DoubleAnimation();
            slideUp.From = 810;
            slideUp.To = 1800;
            slideUp.Duration = new Duration(TimeSpan.FromMilliseconds(1000.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideUp, videoDisplayGrid);
            Storyboard.SetTargetProperty(slideUp, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            videoPanelOut.Children.Add(slideUp);


     

            Storyboard sideBarOut = new Storyboard();

            slideRight = new DoubleAnimation();
            slideRight.From = 1027;
            slideRight.To = 0;
            slideRight.Duration = new Duration(TimeSpan.FromMilliseconds(1000.0));
            // Set the target of the animation
            Storyboard.SetTarget(slideRight, sideBarGrid);
            Storyboard.SetTargetProperty(slideRight, new PropertyPath("RenderTransform.(TranslateTransform.X)"));

            // Kick the animation off
            sideBarOut.Children.Add(slideRight);


            Storyboard logoOut = new Storyboard();

            DoubleAnimation slideDown = new DoubleAnimation();
            slideDown.From = 130;
            slideDown.To = -130;
            slideDown.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDown, appLogo);
            Storyboard.SetTargetProperty(slideDown, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            logoOut.Children.Add(slideDown);

            logoOut.Begin();
            sideBarOut.Begin();
            videoPanelOut.Begin();

            ////////////////////////////////////////////////////////// BRING THEM BACK


            WindowsGrid.Visibility = Visibility.Visible;
            WindowsGrid.Opacity =1;
            SteamGrid.Visibility = Visibility.Visible;
            SteamGrid.Opacity = 1;
            SteamGrid.UpdateLayout();

            

            DoubleAnimation slideUpBack = new DoubleAnimation();
            slideUpBack.From = -530;
            slideUpBack.To = 0;
            slideUpBack.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            Storyboard airMechLogoBack = new Storyboard();
            Storyboard.SetTarget(slideUpBack, appLogoLarge);
            Storyboard.SetTargetProperty(slideUpBack, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));
            airMechLogoBack.Children.Add(slideUpBack);
            airMechLogoBack.Begin();

            Storyboard winBack = new Storyboard();

            DoubleAnimation slideDownBack = new DoubleAnimation();
            slideDownBack.From = 530;
            slideDownBack.To = 0;
            slideDownBack.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDownBack, WindowsGrid);
            Storyboard.SetTargetProperty(slideDownBack, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            winBack.Children.Add(slideDownBack);
            winBack.Begin();

            Storyboard armBack = new Storyboard();

            slideDownBack = new DoubleAnimation();
            slideDownBack.From = 530;
            slideDownBack.To = 0;
            slideDownBack.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));

            // Set the target of the animation
            Storyboard.SetTarget(slideDownBack, ArmGrid);
            Storyboard.SetTargetProperty(slideDownBack, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            armBack.Children.Add(slideDownBack);
            armBack.Begin();


            Storyboard steamBack = new Storyboard();

            slideDownBack = new DoubleAnimation();
            slideDownBack.From = 530;
            slideDownBack.To = 0;
            slideDownBack.Duration = new Duration(TimeSpan.FromMilliseconds(500.0));
            slideDownBack.Completed += new EventHandler(panelsResetToInit);
            // Set the target of the animation
            Storyboard.SetTarget(slideDownBack, SteamGrid);
            Storyboard.SetTargetProperty(slideDownBack, new PropertyPath("RenderTransform.(TranslateTransform.Y)"));

            // Kick the animation off
            steamBack.Children.Add(slideDownBack);
            steamBack.Begin(); 
        }

        private void panelsResetToInit(object sender, EventArgs e)
        {

            backButtonSB.Stop();
            
            //   TranslateTransform myTranslate = new TranslateTransform();
            //     myTranslate.X = -150;
            //  btnBack.Margin.Left.ToString();// = 973;
            //btnBack.Margin.Right// = 38;


        }

        private async void playButton_Click(object sender, RoutedEventArgs e)
        {


            IntPtr hWnd = FindWindow(null, "AirMech Strike");
            int ptr = hWnd.ToInt32();
            if (ptr != 0)
            {
                uint pid = 0;
                GetWindowThreadProcessId(hWnd, out pid);
                Process proc = Process.GetProcessById((int)pid);
                string pathToProcess = proc.MainModule.FileName;
                if(pathToProcess.Contains(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))) 
                {
                    if(steamGameMode)
                    {
                        try
                        {
                            proc.Kill();
                        }
                        catch { }
                        playButton_Click(null, null);
                            return;
                    }
                }
                else
                {
                    if(!steamGameMode)
                    {
                        try
                        {
                            proc.Kill();
                        }
                        catch { }
                        playButton_Click(null, null);
                        return;
                    }
                }

                WindowsInput.InputSimulator inpsim = new WindowsInput.InputSimulator();
                WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                GetWindowPlacement(proc.MainWindowHandle, ref placement);
                if (placement.showCmd==2)
                {
                    ShowWindow(proc.MainWindowHandle, ShowWindowEnum.ShowMaximized);
                   
                }
                SetForegroundWindow(hWnd.ToInt32());
                
                //check if app is already in fullscreen mode
                Rectangle fullScreenRect = new Rectangle();
                callBackPtr = new CallBackPtr(Report);
                EnumWindows(callBackPtr, 0);

                GetWindowRect(fullScreenHandle, ref fullScreenRect);

                if (!(fullScreenRect.Width > 0))
                {
                    inpsim.Keyboard.ModifiedKeyStroke(WindowsInput.Native.VirtualKeyCode.MENU, WindowsInput.Native.VirtualKeyCode.RETURN);
                    await System.Threading.Tasks.Task.Delay(1000);
                    System.Threading.Thread.Sleep(1000);
                }
                //app now must be in full screen 
                Rectangle rect = new Rectangle();
                GetWindowRect(ptr, ref rect);

                int twoPercentHeight = (int)(rect.Height * 0.04) ;
                int onePercentWidth = (int)(rect.Width * 0.005);

                //Find out which monitor
                int ScreenOfAppWidth = rect.Width;
                int diffInHeight = 0;
                foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
                {
                    if (screen.Bounds.IntersectsWith(rect))
                    {
                        ScreenOfAppWidth = screen.Bounds.Width;
                        diffInHeight = screen.Bounds.Height - screen.WorkingArea.Height;
                    }
                }
                int diffInWidth = ScreenOfAppWidth - rect.Width;

                //detection of black edges
                BitmapSource btsrc;
                using (var screenBmp = new Bitmap(
                (int)SystemParameters.VirtualScreenWidth,
                (int)SystemParameters.VirtualScreenHeight,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (var bmpGraphics = Graphics.FromImage(screenBmp))
                    {
                        bmpGraphics.CopyFromScreen(0, 0, 0, 0, screenBmp.Size);
                        btsrc = Imaging.CreateBitmapSourceFromHBitmap(
                           screenBmp.GetHbitmap(),
                           IntPtr.Zero,
                           Int32Rect.Empty,
                           BitmapSizeOptions.FromEmptyOptions());
                    }
                }
                screenShot.Source = btsrc;
                 Bitmap bitmap;
                using (var outStream = new MemoryStream())
                {
                    BitmapEncoder enc = new BmpBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(btsrc));
                    enc.Save(outStream);
                    bitmap = new Bitmap(outStream);

                }

                int blackMarginWidth = onePercentWidth;
                for (int i = 0; i < rect.Width; i++)
                {
                    try { 
                        System.Drawing.Color curPixel = bitmap.GetPixel( rect.Width - i,  rect.Height - rect.Height / 3);
                        //  Console.Write("(" + curPixel.R + "," + curPixel.G + "," + curPixel.B + ") ");
                        
                        if ((curPixel.R + curPixel.G + +curPixel.B) == 0)
                        {
                            blackMarginWidth++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch {
                    }
                }
                int blackMarginHeight = 0;
                for (int i = 0; i < rect.Height; i++)
                {
                    try
                    {
                        System.Drawing.Color curPixel = bitmap.GetPixel(rect.X + rect.Width / 2, rect.Y + rect.Height - i);
                        // Console.Write("(" + curPixel.R + "," + curPixel.G + "," + curPixel.B + ") ");
                        if ((curPixel.R + curPixel.G + +curPixel.B) == 0)
                        {
                            blackMarginHeight++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch { }

                }
                Console.WriteLine("\n\n");
                int mouseMoveX = rect.X + rect.Width - onePercentWidth - blackMarginWidth + diffInWidth;
                int mouseMoveY = rect.Y + rect.Height - twoPercentHeight - blackMarginHeight + diffInHeight;
                if ((fullScreenRect.Width > 0))
                {
                    mouseMoveY -= rect.Y;
                }

                System.Threading.Thread.Sleep(500);
                InputManager.Mouse.Move(mouseMoveX, mouseMoveY);

                InputManager.Keyboard.KeyPress(System.Windows.Forms.Keys.Escape, 300);
                System.Threading.Thread.Sleep(200);
                InputManager.Keyboard.KeyPress(System.Windows.Forms.Keys.Escape, 300);
                System.Threading.Thread.Sleep(200);
          
                InputManager.Mouse.Move(mouseMoveX, mouseMoveY);
                inpsim.Mouse.LeftButtonDoubleClick();
                InputManager.Mouse.Move(mouseMoveX, mouseMoveY);
          
                inpsim.Mouse.LeftButtonDoubleClick();

                InputManager.Keyboard.KeyPress(System.Windows.Forms.Keys.Enter, 500);
                await System.Threading.Tasks.Task.Delay(600);
                InputManager.Keyboard.KeyPress(System.Windows.Forms.Keys.Enter, 500);
                await System.Threading.Tasks.Task.Delay(600);

                System.Threading.Thread.Sleep(500);

                inpsim.Keyboard.TextEntry("/replay " + selectedReplayGlobal);
                System.Threading.Thread.Sleep(200);

                InputManager.Keyboard.KeyPress(System.Windows.Forms.Keys.Enter, 500);
                await System.Threading.Tasks.Task.Delay(600);
                InputManager.Keyboard.KeyPress(System.Windows.Forms.Keys.Enter, 500);
                await System.Threading.Tasks.Task.Delay(600);
            }
            else
            {

                ProcessStartInfo pInfo = new ProcessStartInfo("AirMech.exe");
                pInfo.Arguments = "/replay " + selectedReplayGlobal;

                if (steamGameMode)
                {
                    try
                    {

                        RegistryKey productKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 206500");

                        string[] list = productKey.GetValueNames();


                        string airMechSteamPath = "";
                        if (productKey != null)
                        {
                            string path = productKey.GetValue("InstallLocation").ToString();
                            // airMechSteamPath = path.Substring(0, path.LastIndexOf("\\"));
                            airMechSteamPath = path;
                            pInfo.WorkingDirectory = airMechSteamPath;
                            Process p = Process.Start(pInfo);
                            await System.Threading.Tasks.Task.Delay(500);
                            IntPtr SteamhWnd = FindWindow(null, "AirMech Strike - Steam");
                            SetForegroundWindow(SteamhWnd.ToInt32());
                            await System.Threading.Tasks.Task.Delay(200);
                            InputManager.Keyboard.KeyPress(System.Windows.Forms.Keys.Tab, 200);
                            await System.Threading.Tasks.Task.Delay(500);
                            
                            InputManager.Keyboard.KeyPress(System.Windows.Forms.Keys.Enter, 500);
                            InputManager.Keyboard.KeyPress(System.Windows.Forms.Keys.Enter, 500);
                        }
                    }
                    catch
                    {

                    }
                }
                else
                {
                    try
                    {
                        pInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Carbon\AirMech";
                        Process p = Process.Start(pInfo);
                    }
                    catch { }
                }


            }
        }

    }
    public class replayItem
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string netPlayers { get; set; }
        public string maxPlayers { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
    }
    /// <summary>
    /// Basic implementation of the <see cref="ICommand"/>
    /// interface, which is also accessible as a markup
    /// extension.
    /// </summary>
    public abstract class CommandBase<T> : MarkupExtension, ICommand
        where T : class, ICommand, new()
    {
        /// <summary>
        /// A singleton instance.
        /// </summary>
        private static T command;

        /// <summary>
        /// Gets a shared command instance.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (command == null) command = new T();
            return command;
        }

        /// <summary>
        /// Fires when changes occur that affect whether
        /// or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.
        /// If the command does not require data to be passed,
        /// this object can be set to null.
        /// </param>
        public abstract void Execute(object parameter);

        /// <summary>
        /// Defines the method that determines whether the command
        /// can execute in its current state.
        /// </summary>
        /// <returns>
        /// This default implementation always returns true.
        /// </returns>
        /// <param name="parameter">Data used by the command.  
        /// If the command does not require data to be passed,
        /// this object can be set to null.
        /// </param>
        public virtual bool CanExecute(object parameter)
        {
            return IsDesignMode ? false : true;
        }


        public static bool IsDesignMode
        {
            get
            {
                return (bool)
                    DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty,
                        typeof(FrameworkElement))
                        .Metadata.DefaultValue;
            }
        }


        /// <summary>
        /// Resolves the window that owns the TaskbarIcon class.
        /// </summary>
        /// <param name="commandParameter"></param>
        /// <returns></returns>
        protected Window GetTaskbarWindow(object commandParameter)
        {
            if (IsDesignMode) return null;

            //get the showcase window off the taskbaricon
            var tb = commandParameter as TaskbarIcon;
            return tb == null ? null : TryFindParent<Window>(tb);
        }

        #region TryFindParent helper

        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the
        /// queried item.</param>
        /// <returns>The first parent item that matches the submitted
        /// type parameter. If not matching item can be found, a null
        /// reference is being returned.</returns>
        public static T TryFindParent<T>(DependencyObject child)
            where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = GetParentObject(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                //use recursion to proceed with next level
                return TryFindParent<T>(parentObject);
            }
        }

        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Keep in mind that for content element,
        /// this method falls back to the logical tree of the element!
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject GetParentObject(DependencyObject child)
        {
            if (child == null) return null;
            ContentElement contentElement = child as ContentElement;

            if (contentElement != null)
            {
                DependencyObject parent = ContentOperations.GetParent(contentElement);
                if (parent != null) return parent;

                FrameworkContentElement fce = contentElement as FrameworkContentElement;
                return fce != null ? fce.Parent : null;
            }

            //if it's not a ContentElement, rely on VisualTreeHelper
            return VisualTreeHelper.GetParent(child);
        }

        #endregion
    }
    public class ShowWindowCommand : ICommand
    {
        public void Execute(object parameter)
        {
            
            ((MetroWindow)parameter).WindowState = WindowState.Normal;
            ((MetroWindow)parameter).BringIntoView();
            ((MetroWindow)parameter).Show();
            ((MetroWindow)parameter).Activate();
            ((MetroWindow)parameter).Visibility = Visibility.Visible;
            SystemCommands.RestoreWindow((MetroWindow)parameter);
            Application.Current.MainWindow.Focus();
          //  MessageBox.Show(parameter.ToString());
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
    public class ExitApp : CommandBase<ExitApp>
    {
        public override void Execute(object parameter)
        {
            System.Windows.Application.Current.Shutdown();
            GetTaskbarWindow(parameter).Show();
            CommandManager.InvalidateRequerySuggested();
        }


    }
    public class TextInputToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Always test MultiValueConverter inputs for non-null 
            // (to avoid crash bugs for views in the designer) 
            if (values[0] is bool && values[1] is bool)
            {
                bool hasText = !(bool)values[0];
                bool hasFocus = (bool)values[1];
                if (hasFocus || hasText)
                    return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class WindowHandleInfo
    {
        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        private IntPtr _MainHandle;

        public WindowHandleInfo(IntPtr handle)
        {
            this._MainHandle = handle;
        }

        public List<IntPtr> GetAllChildHandles()
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(this._MainHandle, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }
    }
}