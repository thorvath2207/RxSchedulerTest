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
using System.Reactive;

namespace ReactiveThreadSucks.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private ObservableCollection<Event> evenList;
        private Dictionary<string, IScheduler> schedulers;
        private KeyValuePair<string, IScheduler> observeOnScheduler;
        private KeyValuePair<string, IScheduler> subscribeOnScheduler;

        public MainViewModel()
        {
            this.EventList = new ObservableCollection<Event>();
            this.StartSuckerCommand = new RelayCommand(this.StartSuck);
            this.schedulers = new Dictionary<string, IScheduler>();
            this.schedulers.Add("Dispatcher", DispatcherScheduler.Current);
            this.schedulers.Add("TaskPool", TaskPoolScheduler.Default);
            this.schedulers.Add("ThreadPool", ThreadPoolScheduler.Instance);
            this.schedulers.Add("NewThread", NewThreadScheduler.Default);

        }

        public string ResultLabel { get; set; }

        public KeyValuePair<string, IScheduler> ObserveOnScheduler
        {
            get
            {
                return this.observeOnScheduler;
            }

            set
            {
                this.observeOnScheduler = value;
                this.RaisePropertyChanged(() => this.ObserveOnScheduler);
            }
        }

        public KeyValuePair<string, IScheduler> SubscireOnScheduler
        {
            get
            {
                return this.subscribeOnScheduler;
            }

            set
            {
                this.subscribeOnScheduler = value;
                this.RaisePropertyChanged(() => this.SubscireOnScheduler);
            }
        }

        public Dictionary<string, IScheduler> Schedulers => this.schedulers;

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
            this.ResultLabel = string.Empty;

            var obs = this.SampleTask()
                .ToObservable()
                .ObserveOnDispatcher()
                .Subscribe(this.HandleResponse);

            //obs.SubscribeOn(this.SubscireOnScheduler.Value)
            //    .ObserveOn(this.ObserveOnScheduler.Value)
            //    .Subscribe(HandleResponse);

            var obs2 = Task.Delay(TimeSpan.FromSeconds(35)).ToObservable();

            obs2.SubscribeOn(this.SubscireOnScheduler.Value)
                .ObserveOn(this.ObserveOnScheduler.Value)
                .Subscribe(HandleTaskDelay);
        }

        private async Task<List<Event>> SampleTask()
        {
            var result = new List<Event>();
            System.Diagnostics.Debug.WriteLine("Start new task on thread id: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
            using (HttpClient httpClient = new HttpClient())
            {
                var uri = "http://setgetgo.com/randomword/get.php";
                var response = await httpClient.GetStringAsync(uri);
                result.Add(new Event
                {
                    AddTime = DateTime.Now,
                    Content = response,
                    ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId
                });
            }

            await this.SampleSubTask();
            return result;
        }

        private Task<string> SampleSubTask()
        {
            return Task.Run<string>(async () =>
            {
                System.Diagnostics.Debug.WriteLine("Start new sub task on thread id: {0}",
                    System.Threading.Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(5000);
                System.Threading.Thread.Sleep(10000);
                return "lofaszt a seggedbe";
            });
        }

        private void HandleResponse(List<Event> responses)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Received on Thread id: {0}",
                    System.Threading.Thread.CurrentThread.ManagedThreadId);
                responses.ForEach(r => this.EventList.Add(r));
            }
            catch (Exception e)
            {
                this.ResultLabel = e.Message;
                this.RaisePropertyChanged(() => this.ResultLabel);
            }
        }

        private void HandleTaskDelay(Unit n)
        {
            this.EventList.Add(new Event
            {
                AddTime = DateTime.Now,
                Content = "From TaskDelay",
                ThreadId = 0
            });
        }
    }
}