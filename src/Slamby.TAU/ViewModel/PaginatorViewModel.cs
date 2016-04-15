using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Slamby.SDK.Net.Models;

namespace Slamby.TAU.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class PaginatorViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the PaginatorViewModel class.
        /// </summary>
        public PaginatorViewModel()
        {
            FirstCommand = new RelayCommand(() =>
            {
                if (CurrentPage != 1)
                {
                    PaginatedList.Pagination.Offset = 0;
                    _loadCommand.Execute(PaginatedList.Pagination);
                }
            });
            PreviousCommand = new RelayCommand(() =>
            {
                if (CurrentPage > 1)
                {
                    PaginatedList.Pagination.Offset -= Limit;
                    _loadCommand.Execute(PaginatedList.Pagination);
                }
            });
            NextCommand = new RelayCommand(() =>
            {
                if (CurrentPage < TotalPagesCount)
                {
                    PaginatedList.Pagination.Offset += Limit;
                    _loadCommand.Execute(PaginatedList.Pagination);
                }
            });
            LastCommand = new RelayCommand(() =>
            {
                if (CurrentPage != TotalPagesCount)
                {
                    PaginatedList.Pagination.Offset = Limit * (TotalPagesCount - 1);
                    _loadCommand.Execute(PaginatedList.Pagination);
                }
            });
            ForceLoadCommand = new RelayCommand(() =>
            {
                if (CurrentPage > 0 && CurrentPage <= TotalPagesCount && Limit > 0 &&
                 (PaginatedList.Pagination.Offset != Limit * (CurrentPage - 1) || PaginatedList.Pagination.Limit != Limit))
                {
                    PaginatedList.Pagination.Offset = Limit * (CurrentPage - 1);
                    PaginatedList.Pagination.Limit = Limit;
                    _loadCommand.Execute(PaginatedList.Pagination);
                }
            });

        }

        private RelayCommand<Pagination> _loadCommand;

        public RelayCommand<Pagination> LoadCommand
        {
            get { return _loadCommand; }
            set { Set(() => LoadCommand, ref _loadCommand, value); }
        }


        public RelayCommand FirstCommand { get; private set; }
        public RelayCommand PreviousCommand { get; private set; }
        public RelayCommand NextCommand { get; private set; }
        public RelayCommand LastCommand { get; private set; }
        public RelayCommand ForceLoadCommand { get; private set; }



        private PaginatedList<object> _paginatedList;

        public PaginatedList<object> PaginatedList
        {
            get { return _paginatedList; }
            set
            {
                _paginatedList = value;
                Total = _paginatedList.Total;
                Offset = _paginatedList.Pagination.Offset;
                Limit = _paginatedList.Pagination.Limit;
                TotalPagesCount = Total / Limit + (Total % Limit == 0 ? 0 : 1);
                CurrentPage = (Offset / Limit) + 1;
            }
        }


        private int _offset;

        public int Offset
        {
            get { return _offset; }
            private set { Set(() => Offset, ref _offset, value); }
        }

        private int _limit;

        public int Limit
        {
            get { return _limit; }
            set { Set(() => Limit, ref _limit, value); }
        }


        private int _total;

        public int Total
        {
            get { return _total; }
            set { Set(() => Total, ref _total, value); }
        }


        private int _totalPagesCount;

        public int TotalPagesCount
        {
            get { return _totalPagesCount; }
            set { Set(() => TotalPagesCount, ref _totalPagesCount, value); }
        }


        private int _currentPage;

        public int CurrentPage
        {
            get { return _currentPage; }
            set { Set(() => CurrentPage, ref _currentPage, value); }
        }

    }
}