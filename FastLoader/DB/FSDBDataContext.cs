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

namespace FastLoader.DB
{
	public class FSDBManager : DataContext
	{
		static FSDBManager _dbManager;
		public static FSDBManager Instance
		{
			get 
			{ 
				return _dbManager ?? (_dbManager = new FSDBManager()); 
			}
			set { _dbManager = value; }
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

		public ObservableCollection<ItemsGroup<T>> GetSortedItems<T>() where T : class, IWebItem
		{
			ObservableCollection<ItemsGroup<T>> res = new ObservableCollection<ItemsGroup<T>>();
			Dictionary<DateTime, List<T>> dates = (from item in FSDBManager.Instance.GetTable<T>()
															group item by item.OpenTime.Date).ToDictionary(g => g.Key,
															 g => g.OrderByDescending(x => x.OpenTime).ToList());
			foreach (DateTime dt in dates.Keys)
			{
				ItemsGroup<T> g = new ItemsGroup<T>(dt.ToString("dd MMMM yyyy"), dates[dt]);
				res.Insert(0, g);
			}
			return res;
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
