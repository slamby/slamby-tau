using System;
using System.Collections.Generic;
using System.Globalization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MaterialDesignThemes.Wpf;
using Slamby.SDK.Net.Models;
using Slamby.TAU.Helper;
using Newtonsoft.Json;
using Slamby.TAU.Logger;
using Slamby.TAU.Resources;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class AddDataSetDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the AddDataSetDialogViewModel class.
        /// </summary>
        public AddDataSetDialogViewModel(DataSet dataSet)
        {
            AcceptCommand = new RelayCommand<DataSet>(ds =>
            {
                Log.Info(string.Format(LogMessages.AddDataSetAcceptCommand, ds.Name));
                DataSet.SampleDocument = JsonConvert.DeserializeObject(SampleDocument);
                DialogHost.CloseDialogCommand.Execute(true, null);
            }, Validate);

            DataSet = dataSet;
            Name = dataSet.Name;
            NGramCount = dataSet.NGramCount;
            SampleDocument = JsonConvert.SerializeObject(dataSet.SampleDocument, Formatting.Indented);
            InterpretedFields = dataSet.InterpretedFields;
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                Set(() => Name, ref _name, value);
                DataSet.Name = Name;
                AcceptCommand.RaiseCanExecuteChanged();
            }
        }


        private string _sampleDocument;

        public string SampleDocument
        {
            get { return _sampleDocument; }
            set
            {
                Set(() => SampleDocument, ref _sampleDocument, value);
                AcceptCommand.RaiseCanExecuteChanged();
            }
        }


        private List<string> _interpretedFields;

        public List<string> InterpretedFields
        {
            get { return _interpretedFields; }
            set
            {
                Set(() => InterpretedFields, ref _interpretedFields, value);
                DataSet.InterpretedFields = InterpretedFields;
                AcceptCommand.RaiseCanExecuteChanged();
            }
        }


        private int _nGramCount;

        public int NGramCount
        {
            get { return _nGramCount; }
            set
            {
                Set(() => NGramCount, ref _nGramCount, value);
                DataSet.NGramCount = NGramCount;
                AcceptCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand<DataSet> AcceptCommand { get; private set; }

        public DataSet DataSet { get; private set; }

        private static readonly string[] ValidatedProperties = { "Name", "NgramCount", "SampleDocument" };

        private bool Validate(DataSet dataSet)
        {
            if (dataSet == null) return false;
            foreach (string property in ValidatedProperties)
            {

                if (GetValidationError(property, dataSet) != null)
                    return false;
            }

            return true;
        }

        private string GetValidationError(string propertyName, DataSet dataSet)
        {
            string error = null;

            switch (propertyName)
            {
                case "Name":
                    var nameValidationResult = new DatasetNameValidationRule().Validate(dataSet.Name, CultureInfo.CurrentCulture);
                    error = nameValidationResult.IsValid ? null : nameValidationResult.ErrorContent.ToString();
                    break;

                case "NgramCount":
                    var ngramValidationResult = new NgramCountValidationRule().Validate(dataSet.NGramCount, CultureInfo.CurrentCulture);
                    error = ngramValidationResult.IsValid ? null : ngramValidationResult.ErrorContent.ToString();
                    break;
                case "SampleDocument":
                    var sampleDocValidationResult = new JsonValidationRule().Validate(SampleDocument, CultureInfo.CurrentCulture);
                    error = sampleDocValidationResult.IsValid ? null : sampleDocValidationResult.ErrorContent.ToString();
                    break;
                default:
                    error = null;
                    throw new Exception("Unexpected property being validated on Service");
            }

            return error;
        }
    }
}