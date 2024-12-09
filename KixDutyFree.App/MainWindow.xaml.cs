using KixDutyFree.App.Models.Config;
using KixDutyFree.App.Models;
using KixDutyFree.Shared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using QYQ.Base.Common.IOCExtensions;
using System.Runtime.InteropServices.JavaScript;
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
using Quartz.AspNetCore;
using Serilog;

namespace KixDutyFree.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private IHost _host;

        public MainWindow()
        {
            InitializeComponent();
            Resources.Add("services", GobalObject.serviceProvider);

            AppNotifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
            // 创建上下文菜单
            var contextMenu = new ContextMenu();

            var showItem = new MenuItem();
            showItem.Header = "打开";
            showItem.Click += ShowItem_Click;
            var exitItem = new MenuItem();
            exitItem.Header = "退出";
            exitItem.Click += ExitItem_Click;
            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(exitItem);
            //AppNotifyIcon.ContextContent = contextMenu;
            AppNotifyIcon.ContextMenu = contextMenu;
        }

        private void NotifyIcon_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ShowItem_Click(object? sender, RoutedEventArgs e)
        {
            ShowMainWindow();
        }

        private void ExitItem_Click(object? sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // 阻止窗口真正关闭，隐藏到托盘
            e.Cancel = true;
            this.Hide();
            // 可选：在此处记录日志或执行其他操作
            base.OnClosing(e);
        }
    }
}
