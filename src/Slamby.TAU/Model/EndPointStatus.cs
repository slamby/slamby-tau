using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using Slamby.SDK.Net.Models;

namespace Slamby.TAU.Model
{
    public class EndPointStatus : ObservableObject
    {
        private bool _isAlive;

        public bool IsAlive
        {
            get { return _isAlive; }
            set { Set(() => IsAlive, ref _isAlive, value); }
        }

        private Status _status;

        public Status Status
        {
            get { return _status; }
            set { Set(() => Status, ref _status, value); }
        }
    }
}
