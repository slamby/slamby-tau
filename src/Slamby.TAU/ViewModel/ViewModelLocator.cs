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
using Slamby.SDK.Net.Models;
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
            DispatcherHelper.Initialize();
            Messenger.Default.Register<UpdateMessage>(this, m =>
            {
                switch (m.UpdateType)
                {
                    case UpdateType.SelectedDataSetChange:
                        SimpleIoc.Default.Unregister<IDocumentManager>();
                        SimpleIoc.Default.Unregister<ITagManager>();
                        SimpleIoc.Default.Unregister<ManageDataViewModel>();
                        if (m.Parameter is DataSet && !string.IsNullOrEmpty(((DataSet)m.Parameter).Name))
                        {
                            _selectedDataSet = (DataSet)m.Parameter;
                            SimpleIoc.Default.Register<IDocumentManager>(
                                () => new DocumentManager(GlobalStore.EndpointConfiguration, _selectedDataSet.Name));
                            SimpleIoc.Default.Register<ITagManager>(
                                () => new TagManager(GlobalStore.EndpointConfiguration, _selectedDataSet.Name));
                            SimpleIoc.Default.Register<ManageDataViewModel>();
                            OnPropertyChanged("ManageData");
                        }
                        break;
                    case UpdateType.EndPointUpdate:
                        SimpleIoc.Default.Reset();
                        Initialize();
                        break;
                    case UpdateType.SelectedMenuItemChange:
                        break;
                    case UpdateType.NewProcessCreated:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
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
                SimpleIoc.Default.Register<IDataSetManager>(() => new DataSetManager(GlobalStore.EndpointConfiguration));
                SimpleIoc.Default.Register<IServiceManager>(() => new ServiceManager(GlobalStore.EndpointConfiguration));
                SimpleIoc.Default.Register<IClassifierServiceManager>(() => new ClassifierServiceManager(GlobalStore.EndpointConfiguration));
                SimpleIoc.Default.Register<IPrcServiceManager>(() => new PrcServiceManager(GlobalStore.EndpointConfiguration));
                SimpleIoc.Default.Register<IProcessManager>(() => new ProcessManager(GlobalStore.EndpointConfiguration));
            }

            InitializeViewModels();
        }

        private void InitializeViewModels()
        {
            Cleanup();

            SimpleIoc.Default.Register<MainViewModel>();
            OnPropertyChanged("Main");
            SimpleIoc.Default.Register<ManageDataSetViewModel>();
            SimpleIoc.Default.Register<ManageServiceViewModel>();
            SimpleIoc.Default.Register<ManageProcessViewModel>();
        }

        private static DataSet _selectedDataSet;

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();

        public ManageDataSetViewModel ManageDataSet => ServiceLocator.Current.GetInstance<ManageDataSetViewModel>();

        public ManageDataViewModel ManageData => SimpleIoc.Default.IsRegistered<ManageDataViewModel>() ? ServiceLocator.Current.GetInstance<ManageDataViewModel>() : null;

        public ManageServiceViewModel ManageService => ServiceLocator.Current.GetInstance<ManageServiceViewModel>();
        public ManageProcessViewModel ManageProcess => ServiceLocator.Current.GetInstance<ManageProcessViewModel>();

        public static void Cleanup()
        {
            SimpleIoc.Default.Unregister<MainViewModel>();
            SimpleIoc.Default.Unregister<ManageDataSetViewModel>();
            SimpleIoc.Default.Unregister<ManageDataViewModel>();
            SimpleIoc.Default.Unregister<ManageServiceViewModel>();
            SimpleIoc.Default.Unregister<ManageProcessViewModel>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}