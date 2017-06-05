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
using System.Threading;
using System.Reactive.Disposables;

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
            if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                Thread.CurrentThread.Name = "Main";

            IScheduler thread1 = new NewThreadScheduler(x => new Thread(x) { Name = "Thread1" });
            IScheduler thread2 = new NewThreadScheduler(x => new Thread(x) { Name = "Thread2" });

            var blockObs = Observable.Create((IObserver<string> observer) =>
            {
                Thread.Sleep(1000);
                observer.OnNext(string.Format("a  " + System.Threading.Thread.CurrentThread.Name));
                Thread.Sleep(1000);
                observer.OnNext(string.Format("b  " + System.Threading.Thread.CurrentThread.Name));
                observer.OnCompleted();
                Thread.Sleep(1000);
                return Disposable.Create(() => Console.WriteLine("Observer has unsubscribed"));
            });

            blockObs
                .SubscribeOn(thread1)
                .ObserveOn(thread2).
                Subscribe((string b) =>
           {
               System.Diagnostics.Debug.WriteLine(b);
               System.Diagnostics.Debug.WriteLine("Subs: " + Thread.CurrentThread.Name + " || " + Thread.CurrentThread.ManagedThreadId);
           });

            //var obs = Observable.Create((IObserver<List<Event>> observer) =>
            //    {
            //        var result = new List<Event>();
            //        System.Diagnostics.Debug.WriteLine("Start sample task: " + Thread.CurrentThread.Name + " || " + Thread.CurrentThread.ManagedThreadId);
            //        using (HttpClient httpClient = new HttpClient())
            //        {
            //            var uri = "http://setgetgo.com/randomword/get.php";
            //            var response1 = httpClient.GetStringAsync(uri);
            //            var response2 = httpClient.GetStringAsync(uri);
            //            result.Add(new Event
            //            {
            //                AddTime = DateTime.Now,
            //                Content = response1.Result,
            //                ThreadId = Thread.CurrentThread.Name + Thread.CurrentThread.ManagedThreadId
            //            });

            //            result.Add(new Event
            //            {
            //                AddTime = DateTime.Now,
            //                Content = response2.Result,
            //                ThreadId = Thread.CurrentThread.Name + Thread.CurrentThread.ManagedThreadId
            //            });
            //        }
            //        observer.OnNext(result);
            //        observer.OnCompleted();

            //        return Disposable.Create(() => System.Diagnostics.Debug.WriteLine("Done"));
            //    });

            var obs = Observable.FromAsync(x => this.SampleTaskAsync());

            obs
            .SubscribeOn(thread2)
            .ObserveOn(DispatcherScheduler.Current)
            .Subscribe(this.HandleResponses);

            //var obs2 = this.SampleSubTask()
            //    .ToObservable()
            //    .ObserveOnDispatcher()
            //    .Subscribe(this.HandleResponse);

        }

        private async Task<List<Event>> SampleTaskAsync()
        {
            var result = new List<Event>();
            System.Diagnostics.Debug.WriteLine("Start sample task: " + Thread.CurrentThread.Name + " || " + Thread.CurrentThread.ManagedThreadId);
            using (HttpClient httpClient = new HttpClient())
            {
                var uri = "http://setgetgo.com/randomword/get.php";
                var response1 = await httpClient.GetStringAsync(uri);
                var response2 = await httpClient.GetStringAsync(uri);
                result.Add(new Event
                {
                    AddTime = DateTime.Now,
                    Content = response1,
                    ThreadId = Thread.CurrentThread.Name + Thread.CurrentThread.ManagedThreadId
                });

                result.Add(new Event
                {
                    AddTime = DateTime.Now,
                    Content = response2,
                    ThreadId = Thread.CurrentThread.Name + Thread.CurrentThread.ManagedThreadId
                });
            }
            return result;
        }

        //private Task<Event> SampleSubTask()
        //{
        //    return Task.Run(async () =>
        //    {
        //        System.Diagnostics.Debug.WriteLine("Start sample sub task: " + Thread.CurrentThread.Name + " || " + Thread.CurrentThread.ManagedThreadId);
        //        using (HttpClient httpClient = new HttpClient())
        //        {
        //            var uri = "http://setgetgo.com/randomword/get.php";
        //            var response = await httpClient.GetStringAsync(uri);
        //            return new Event
        //            {
        //                AddTime = DateTime.Now,
        //                Content = response,
        //                ThreadId = System.Threading.Thread.CurrentThread.Name
        //            };
        //        }
        //    });
        //}

        private void HandleResponses(List<Event> responses)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Subs(LIST) on: " + Thread.CurrentThread.Name + " || " + Thread.CurrentThread.ManagedThreadId);
                responses.ForEach(r => this.EventList.Add(r));
            }
            catch (Exception e)
            {
                this.ResultLabel = e.Message;
                this.RaisePropertyChanged(() => this.ResultLabel);
            }
        }

        private void HandleResponse(Event response)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Subs on: " + Thread.CurrentThread.Name + " || " + Thread.CurrentThread.ManagedThreadId);
                this.EventList.Add(response);
            }
            catch (Exception e)
            {
                this.ResultLabel = e.Message;
                this.RaisePropertyChanged(() => this.ResultLabel);
            }
        }
    }
}