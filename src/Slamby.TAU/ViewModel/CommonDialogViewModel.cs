using System;
using GalaSoft.MvvmLight;
using Slamby.TAU.Enum;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class CommonDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the CommonDialogViewModel class.
        /// </summary>
        public CommonDialogViewModel()
        {
            YesButtonIsVisible = true;
            CancelButtonIsVisible = true;
            NoButtonIsVisible = true;
            OkButtonIsVisible = false;
        }

        private object _content;

        public object Content
        {
            get { return _content; }
            set { Set(() => Content, ref _content, value); }
        }

        private string _errorMessage;

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { Set(() => ErrorMessage, ref _errorMessage, value); }
        }

        private bool _showError;

        public bool ShowError
        {
            get { return _showError; }
            set { Set(() => ShowError, ref _showError, value); }
        }

        private bool _yesButtonIsVisible;

        public bool YesButtonIsVisible
        {
            get { return _yesButtonIsVisible; }
            set { Set(() => YesButtonIsVisible, ref _yesButtonIsVisible, value); }
        }


        private bool _noButtonIsVisible;

        public bool NoButtonIsVisible
        {
            get { return _noButtonIsVisible; }
            set { Set(() => NoButtonIsVisible, ref _noButtonIsVisible, value); }
        }


        private bool _cancelButtonIsVisible;

        public bool CancelButtonIsVisible
        {
            get { return _cancelButtonIsVisible; }
            set { Set(() => CancelButtonIsVisible, ref _cancelButtonIsVisible, value); }
        }


        private bool _okButtonIsVisible;

        public bool OkButtonIsVisible
        {
            get { return _okButtonIsVisible; }
            set { Set(() => OkButtonIsVisible, ref _okButtonIsVisible, value); }
        }


        private string _header = "?";

        public string Header
        {
            get { return _header; }
            set { Set(() => Header, ref _header, value); }
        }

        private ButtonsEnum _buttons;
        public ButtonsEnum Buttons
        {
            get { return _buttons; }
            set
            {
                _buttons = value;
                switch (value)
                {
                    case ButtonsEnum.Ok:
                        YesButtonIsVisible = false;
                        CancelButtonIsVisible = false;
                        NoButtonIsVisible = false;
                        OkButtonIsVisible = true;
                        break;
                    case ButtonsEnum.OkCancel:
                        YesButtonIsVisible = false;
                        CancelButtonIsVisible = true;
                        NoButtonIsVisible = false;
                        OkButtonIsVisible = true;
                        break;
                    case ButtonsEnum.YesNo:
                        YesButtonIsVisible = true;
                        CancelButtonIsVisible = false;
                        NoButtonIsVisible = true;
                        OkButtonIsVisible = false;
                        break;
                    case ButtonsEnum.YesNoCancel:
                        YesButtonIsVisible = true;
                        CancelButtonIsVisible = true;
                        NoButtonIsVisible = true;
                        OkButtonIsVisible = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }
    }
}