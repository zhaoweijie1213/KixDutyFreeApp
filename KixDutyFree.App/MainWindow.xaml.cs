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

            //var serviceCollection = new ServiceCollection();
            //serviceCollection.AddWpfBlazorWebView();
            //serviceCollection.AddMasaBlazor();

            //#if DEBUG
            //    		serviceCollection.AddBlazorWebViewDeveloperTools();
            //#endif
            //Resources.Add("services", serviceCollection.BuildServiceProvider());

            //Resources.Add("services", _host.Services);
            Resources.Add("services", GobalObject.serviceProvider);

            //Task.Run(async () =>
            //{
            //    await _host.StartAsync();
            //});
        }
    }
}
