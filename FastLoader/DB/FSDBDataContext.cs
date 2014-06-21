using FastLoader.Data;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastLoader.Interfaces;
using System.Collections.ObjectModel;
using Microsoft.Phone.Data.Linq;
using System.Diagnostics;
using System.Threading;

namespace FastLoader.DB
{
	public class FSDBManager : DataContext
	{
		static FSDBManager _dbManager;
		Mutex _mutex = new Mutex();
		public static FSDBManager Instance
		{
			get 
			{ 
				return _dbManager ?? (_dbManager = new FSDBManager()); 
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_dbManager = null;
		}

		public FSDBManager(string connection = "Data source=isostore:/hystory.sdf") :
            base(connection)
        {            
			if (this.DatabaseExists() == false)
            { 
				this.CreateDatabase();
				DatabaseSchemaUpdater schemaUpdater = this.CreateDatabaseSchemaUpdater();
				schemaUpdater.DatabaseSchemaVersion = this.SchemaVersion;
                schemaUpdater.Execute();
            }
            else
            {
                DatabaseSchemaUpdater dbUpdater = this.CreateDatabaseSchemaUpdater();
                if (dbUpdater.DatabaseSchemaVersion < this.SchemaVersion)
                {					
                    this.DeleteDatabase();
                    this.CreateDatabase();
                    /*dbUpdater.AddTable<CachedItem>();*/
					dbUpdater.DatabaseSchemaVersion = this.SchemaVersion;
                    dbUpdater.Execute();
                }
            }
        }

        public int SchemaVersion
        {
            get { return 5; }
        }

		
		public Task<ObservableCollection<ItemsGroup<T>>> GetSortedItems<T>() where T : class, IWebItem
		{
			return Task.Factory.StartNew<ObservableCollection<ItemsGroup<T>>>(() =>
			{
				_mutex.WaitOne();
				//Dictionary<String, List<T>>
				List<ItemsGroup<T>> dates = FSDBManager.Instance.GetTable<T>().OrderByDescending(x => x.OpenTime).GroupBy(x=>x.OpenTime.Date).ToList().
					Select(s => new ItemsGroup<T>(s.Key.ToString("dd MMMM yyyy"),s.ToList())).OrderByDescending(ob=>ob.Key).ToList();
				//select new ItemsGroup<T>(gr.Key.ToString("dd MMMM yyyy"), gr.ToList())).ToList();
				_mutex.ReleaseMutex();
				return new ObservableCollection<ItemsGroup<T>>(dates);
			});
		}

		public Task<ObservableCollection<ItemsGroup<T>>> GetSortedItemsWhichContain<T>(string contain) where T : class, IWebItem
		{
			return Task.Factory.StartNew<ObservableCollection<ItemsGroup<T>>>(() =>
			{
				_mutex.WaitOne();
				//Dictionary<String, List<T>>
				List<ItemsGroup<T>> dates = FSDBManager.Instance.GetTable<T>().Where(item=>item.Title.ToLower().Contains(contain))
					.OrderByDescending(x => x.OpenTime).GroupBy(x => x.OpenTime.Date).ToList().
					Select(s => new ItemsGroup<T>(s.Key.ToString("dd MMMM yyyy"), s.ToList())).ToList();
				//select new ItemsGroup<T>(gr.Key.ToString("dd MMMM yyyy"), gr.ToList())).ToList();
				_mutex.ReleaseMutex();
				return new ObservableCollection<ItemsGroup<T>>(dates);
			});			
		}

		Dictionary<Type, int> _loadedCount = new Dictionary<Type, int>();
		int GetLoadedItemsCount(Type t)
		{
			if (_loadedCount.ContainsKey(t))
				return _loadedCount[t];
			else
				return 0;
		}
		
		public System.Data.Linq.Table<CachedItem> Cache
		{
			get
			{
				return this.GetTable<CachedItem>();
			}
		}        

		public System.Data.Linq.Table<HistoryItem> History
		{
			get
			{
				return this.GetTable<HistoryItem>();
			}
		}
	}
}
