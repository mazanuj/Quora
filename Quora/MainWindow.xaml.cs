using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Microsoft.Win32;
using Quora.Annotations;
using Quora.Properties;
using QuoraLib;
using QuoraLib.CaptchaHelper;
using QuoraLib.DataTypes;
using QuoraLib.Utilities;
using QuoraLib.XlsxHelper;

namespace Quora
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            Height = SystemParameters.WorkArea.Height;
            Width = SystemParameters.WorkArea.Width;
            DataItemsLog = new ObservableCollection<string>();
            DataContext = this;

            CaptchaTimer = new Timer
            {
                AutoReset = true,
                Interval = 1000
            };

            Informer.OnQueueChanged += result => CaptchaCount = result.ToString();
            Informer.OnResultStr +=
                async result =>
                {
                    await
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => { DataItemsLog.Insert(0, result); }));
                };
        }

        private string regCount = "0";
        private string captchaCount = "0";
        private static Timer CaptchaTimer { get; set; }
        public static ObservableCollection<string> DataItemsLog { get; set; }

        public string CaptchaCount
        {
            get { return captchaCount; }
            set
            {
                captchaCount = value;
                OnPropertyChanged();
            }
        }

        public string RegCount
        {
            get { return regCount; }
            set
            {
                regCount = value;
                OnPropertyChanged();
            }
        }

        private static async void GetNoCaptcha()
        {
            try
            {
                var result = await GetCaptcha.GetNoCaptcha(Settings.Default.ApiKey);
                if (result.Answer.StartsWith("Error message"))
                {
                    if (result.Answer.Contains("Error message: Stopped"))
                        return;
                    //Informer.RaiseOnResultReceived(result.Answer);
                }

                Utils.CaptchaQueue = result;
            }
            catch (Exception)
            {
            }
        }

        private static void RaiseOnTimerElapsed(object obj, ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(Settings.Default.ApiKey))
                        return;

                    CaptchaTimer.Elapsed -= RaiseOnTimerElapsed;

                    await Utils.RemoveOldCaptcha(DateTime.Now.AddSeconds(-Utils.CaptchaLife));

                    if (Utils.CaptchaQueueCount < Utils.MaxCaptchaQueue)
                    {
                        while (Utils.CaptchaQueueCount < Utils.MaxCaptchaQueue)
                        {
                            if (!Utils.IsPermit)
                                return;

                            GetNoCaptcha();
                            var result = await GetCaptcha.GetNoCaptchaQueue(Settings.Default.ApiKey);

                            if (result.Answer.StartsWith("Error message"))
                            {
                                if (result.Answer.Contains("Error message: Stopped"))
                                    return;
                                //Informer.RaiseOnResultReceived(result.Answer);
                                continue;
                            }

                            Utils.CaptchaQueue = result;
                            await
                                Utils.RemoveOldCaptcha(
                                    DateTime.Now.AddSeconds(-Utils.CaptchaLife));
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Informer.RaiseOnResultReceived(ex.Message);
                }
                finally
                {
                    CaptchaTimer.Elapsed += RaiseOnTimerElapsed;
                }
            }).Wait();
        }

        private void ButtonIsEnable(bool value)
        {
            ButtonXlsReg.IsEnabled = value;
            RegStart.IsEnabled = value;
            RegStop.IsEnabled = !value;
            ConfStart.IsEnabled = value;
        }

        private void LaunchQuora_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/mazanuj/Quora/");
        }

        private void ButtonXlsReg_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonIsEnable(false);
            var sfd = new OpenFileDialog
            {
                Filter = "Excel 2016 (*.xlsx)|*.xlsx",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                RestoreDirectory = true
            };

            if (sfd.ShowDialog() == false)
            {
                ButtonIsEnable(true);
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    Utils.PersonsList = await XlsxRegistration.ParsePersonsList(sfd.FileName);
                    RegCount = Utils.PersonsList.Count.ToString();
                }
                catch (Exception ex)
                {
                    Informer.RaiseOnResultReceived(ex.Message);
                }
            }).Wait();
            ButtonIsEnable(true);
        }

        private async void ButtonRegStart_OnClick(object sender, RoutedEventArgs e)
        {
            if (Utils.PersonsList.Count < 1)
                return;

            ButtonIsEnable(false);
            Utils.CaptchaLife = 120;
            Utils.MaxCaptchaQueue = 10;
            Utils.IsPermit = true;
            Utils.AntigateService = "rucaptcha.com";
            CaptchaTimer.Elapsed += RaiseOnTimerElapsed;
            CaptchaTimer.Start();
            Utils.ResultPers = new List<PersonStruct>();

            var list = Utils.PersonsList;

            await Task.Run(async () =>
            {
                while (list.Count > 0 && Utils.IsPermit)
                {
                    var current = new PersonStruct();
                    try
                    {
                        while (Utils.CaptchaQueueCount < 1 && Utils.IsPermit)
                            await Task.Delay(1000);

                        if (!Utils.IsPermit)
                            break;

                        current = list[0];
                        Utils.PersonsList.Remove(current);

                        var result = await MainCycle.GetResult(list[0], Utils.CaptchaQueue);

                        if (result == "Compleate")
                        {
                            Informer.RaiseOnResultReceived($"Account for {list[0].Mail} successfully registered");
                            current.Result = "registered but not confirmed";
                        }
                        else throw new Exception(result);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("There is already"))
                            current.Result = "confirmed";
                        else if (ex.Message.Contains("Не удалось соединиться с HTTP-сервером"))
                            current.Result = "proxy fail";
                        else current.Result = "fail register";
                        Informer.RaiseOnResultReceived(ex.Message.Contains("Не удалось соединиться с HTTP-сервером")
                            ? $"Bad proxy {current.Proxy.Host} for {current.Mail}"
                            : $"{ex.Message} for {current.Mail}");
                    }
                    Utils.ResultPers.Add(current);
                }

                //CaptchaTimer.Stop();
                //CaptchaTimer.Elapsed -= RaiseOnTimerElapsed;

                await XlsxSave.SaveInXls(Utils.ResultPers, "ResultReg.xlsx");
                await XlsxSave.SaveInXls(list, "RestReg.xlsx");

                Utils.DisposeWebDrivers();
            });

            ButtonIsEnable(true);
        }

        private void ButtonRegStop_OnClick(object sender, RoutedEventArgs e)
        {
            CaptchaTimer.Stop();
            CaptchaTimer.Elapsed -= RaiseOnTimerElapsed;
            Utils.IsPermit = false;
            RegStop.IsEnabled = false;
        }

        private async void ButtonConfStart_OnClick(object sender, RoutedEventArgs e)
        {
            if (Utils.PersonsList.Count < 1)
                return;

            ButtonIsEnable(false);
            Utils.IsPermit = true;
            Utils.ResultPers = new List<PersonStruct>();

            var list = Utils.PersonsList;

            await Task.Run(async () =>
            {
                while (list.Count > 0 && Utils.IsPermit)
                {
                    var current = list[0];
                    Utils.PersonsList.Remove(list[0]);

                    try
                    {
                        var result = await MailConfirm.AcceptConfirm(current.Mail, current.Pass);
                        if (result)
                        {
                            Informer.RaiseOnResultReceived($"{list[0].Mail} successfully confirmed");
                            current.Result = "confirmed";
                        }
                        else throw new Exception();
                    }
                    catch (Exception)
                    {
                        Informer.RaiseOnResultReceived($"{list[0].Mail} is not confirmed");
                        current.Result = "fail confirm";
                    }

                    Utils.ResultPers.Add(current);
                }

                await XlsxSave.SaveInXls(Utils.ResultPers, "ResultConf.xlsx");
                await XlsxSave.SaveInXls(list, "RestConf.xlsx");
                Utils.DisposeWebDrivers();
            });

            ButtonIsEnable(true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}