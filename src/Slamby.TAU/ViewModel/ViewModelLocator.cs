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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Practices.ServiceLocation;
using Slamby.SDK.Net.Managers;
using Slamby.SDK.Net.Managers.Interfaces;
using Slamby.TAU.Enum;
using Slamby.TAU.Helper;
using Slamby.TAU.Model;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            SimpleIoc.Default.Reset();
            Messenger.Default.Register<UpdateMessage>(this, m =>
            {
                switch (m.UpdateType)
                {
                    case UpdateType.EndPointUpdate:
                        if (SimpleIoc.Default.IsRegistered<ResourcesMonitorViewModel>())
                            SimpleIoc.Default.GetInstance<ResourcesMonitorViewModel>().Dispose();
                        SimpleIoc.Default.Reset();
                        Initialize();
                        break;
                    case UpdateType.NewProcessCreated:
                        break;
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
                if (GlobalStore.IsInTestMode)
                {
                    SimpleIoc.Default.Register<DialogHandler>(() => new TestDialogHandler());
                }
                else
                {
                    SimpleIoc.Default.Register<DialogHandler>();
                    SimpleIoc.Default.Register<IDataSetManager>(() => new DataSetManager(GlobalStore.SelectedEndpoint));
                    SimpleIoc.Default.Register<IServiceManager>(() => new ServiceManager(GlobalStore.SelectedEndpoint));
                    SimpleIoc.Default.Register<IClassifierServiceManager>(() => new ClassifierServiceManager(GlobalStore.SelectedEndpoint));
                    SimpleIoc.Default.Register<IPrcServiceManager>(() => new PrcServiceManager(GlobalStore.SelectedEndpoint));
                    SimpleIoc.Default.Register<IProcessManager>(() => new ProcessManager(GlobalStore.SelectedEndpoint));
                    SimpleIoc.Default.Register<StatusManager>(() => new StatusManager(GlobalStore.SelectedEndpoint));
                }
            }

            InitializeViewModels();
        }

        private void InitializeViewModels()
        {
            Cleanup();

            SimpleIoc.Default.Register<ManageDataSetViewModel>();
            SimpleIoc.Default.Register<ManageServiceViewModel>();
            SimpleIoc.Default.Register<ManageProcessViewModel>();
            SimpleIoc.Default.Register<ResourcesMonitorViewModel>();
            SimpleIoc.Default.Register<MainViewModel>();
            OnPropertyChanged("Main");
        }

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();

        public ManageDataSetViewModel ManageDataSet => ServiceLocator.Current.GetInstance<ManageDataSetViewModel>();

        public ManageServiceViewModel ManageService => ServiceLocator.Current.GetInstance<ManageServiceViewModel>();
        public ManageProcessViewModel ManageProcess => ServiceLocator.Current.GetInstance<ManageProcessViewModel>();
        public ResourcesMonitorViewModel ResourcesMonitor => ServiceLocator.Current.GetInstance<ResourcesMonitorViewModel>();

        public static void Cleanup()
        {
            SimpleIoc.Default.Unregister<ManageDataSetViewModel>();
            SimpleIoc.Default.Unregister<ManageDataViewModel>();
            SimpleIoc.Default.Unregister<ManageServiceViewModel>();
            SimpleIoc.Default.Unregister<ManageProcessViewModel>();
            SimpleIoc.Default.Unregister<ResourcesMonitorViewModel>();
            SimpleIoc.Default.Unregister<MainViewModel>();
        }

        public event PropertyChangedEventHandler PropertyChanged;


        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}