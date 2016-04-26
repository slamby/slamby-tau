/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:Slamby.TAU"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using Slamby.SDK.Net;
using Slamby.SDK.Net.Managers;
using Slamby.TAU.Enum;
using Slamby.TAU.Model;
using Slamby.TAU.Properties;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            Messenger.Default.Register<UpdateMessage>(this, m =>
            {
                if (m.UpdateType == UpdateType.EndPointUpdate)
                {
                    SimpleIoc.Default.Reset();
                    Initialize();
                }
            });
            Initialize();
        }

        private void Initialize()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            if (ViewModelBase.IsInDesignModeStatic)
            {
            }
            else
            {
                SimpleIoc.Default.Register<IProcessManager>(() => new ProcessManager(_endpointConfiguration));
            }


            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<ManageProcessViewModel>();
        }

        public MainViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainViewModel>();
            }
        }

        private static Configuration _endpointConfiguration = new Configuration
        {
            ApiBaseEndpoint = new Uri(Settings.Default["EndpointUri"].ToString()),
            ApiSecret = Settings.Default["EndpointSecret"].ToString()
        };

        public ManageProcessViewModel ManageProcess {
            get
            {
                return ServiceLocator.Current.GetInstance<ManageProcessViewModel>();
            }
        }

        public static void Cleanup()
        {
            SimpleIoc.Default.Unregister<MainViewModel>();
        }
    }
}