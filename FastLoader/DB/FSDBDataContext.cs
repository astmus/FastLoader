using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastLoader.DB
{
	public class FSDBManager : DataContext
	{
		static FSDBManager _dbManager;
		public static FSDBManager Instance
		{
			get { return _dbManager ?? (_dbManager = new FSDBManager()); }
			set { _dbManager = value; }
		}

		public FSDBManager(string connection = "Data source=isostore:/hystory.sdf") :
            base(connection)
        {
            if (this.DatabaseExists() == false)
                this.CreateDatabase();
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
