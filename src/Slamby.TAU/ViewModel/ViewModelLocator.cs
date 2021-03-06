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
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
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
                    case UpdateType.NewProcessCreated:
                        break;
                }
            });
            Initialize();
        }

        public async Task EndpointUpdate()
        {
            if (SimpleIoc.Default.IsRegistered<ResourcesMonitorViewModel>())
                SimpleIoc.Default.GetInstance<ResourcesMonitorViewModel>().Dispose();
            if (SimpleIoc.Default.IsRegistered<ManageServiceViewModel>())
                SimpleIoc.Default.GetInstance<ManageServiceViewModel>().Dispose();
            SimpleIoc.Default.Reset();
            Initialize();
            await Task.Delay(500);
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
                    SimpleIoc.Default.Register<IStatusManager>(() => new StatusManager(GlobalStore.SelectedEndpoint));
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

        public ManageDataSetViewModel ManageDataSet
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<ManageDataSetViewModel>();
                instance.Reload();
                return instance;
            }
        }

        public ManageServiceViewModel ManageService
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<ManageServiceViewModel>();
                instance.Reload();
                return instance;
            }
        }
        public ManageProcessViewModel ManageProcess
        {
            get
            {
                var instance = ServiceLocator.Current.GetInstance<ManageProcessViewModel>();
                instance.Reload();
                return instance;
            }
        }
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