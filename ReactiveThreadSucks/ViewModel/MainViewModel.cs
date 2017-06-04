using GalaSoft.MvvmLight;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Command;
using System.Threading.Tasks;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Concurrency;
using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace ReactiveThreadSucks.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private ObservableCollection<Event> evenList;

        public MainViewModel()
        {
            this.EventList = new ObservableCollection<Event>();
            this.StartSuckerCommand = new RelayCommand(this.StartSuck);
        }

        public ObservableCollection<Event> EventList
        {
            get
            {
                return this.evenList;
            }
            set
            {
                this.evenList = value;
                this.RaisePropertyChanged(() => this.EventList);
            }
        }

        public RelayCommand StartSuckerCommand { get; private set; }

        private void StartSuck()
        {
           
            var obs = this.SampleTask().ToObservable(NewThreadScheduler.Default);

            obs
                .SubscribeOn(NewThreadScheduler.Default)
                .ObserveOn(DispatcherScheduler.Current)
                .Subscribe(HandleResponse);
        }

        private async Task<List<Event>> SampleTask()
        {
            var result = new List<Event>();
            System.Diagnostics.Debug.WriteLine("Start new task on thread id: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
            for (int i = 1; i < 15; ++i)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    System.Threading.Thread.Sleep(1000);
                    var uri = "http://setgetgo.com/randomword/get.php";
                    var response = await httpClient.GetStringAsync(uri);
                    result.Add(new Event
                    {
                        AddTime = DateTime.Now,
                        Content = response,
                        ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId
                    });
                }
            }

            return result;
        }

        private void HandleResponse(List<Event> responses)
        {
            System.Diagnostics.Debug.WriteLine("Received on Thread id: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
            responses.ForEach(r => this.EventList.Add(r));
        }
    }
}