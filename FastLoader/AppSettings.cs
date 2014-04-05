using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.IsolatedStorage;

namespace FastLoader
{
	public class AppSettings
	{
		static AppSettings _instance;
		public static AppSettings Instance
		{
			get 
			{
				return _instance ?? (_instance = new AppSettings());
			}
		}

		//public delegate void SaveAutocompletionsListValueChanged(bool currentState);
		public event Action<bool> SaveAutoCompletionsListValueCahnged;

		private AppSettings()
		{

		}
		
		const string SAVE_AUTOCOMPLETIONS_LIST_KEY = "savecompletionslist";
		public bool SaveAutocompletionsList
		{
			get 
			{
				if (IsolatedStorageSettings.ApplicationSettings.Contains(SAVE_AUTOCOMPLETIONS_LIST_KEY))
					return (bool)IsolatedStorageSettings.ApplicationSettings[SAVE_AUTOCOMPLETIONS_LIST_KEY];
				else
					return false;
			}

			set
			{
				IsolatedStorageSettings.ApplicationSettings[SAVE_AUTOCOMPLETIONS_LIST_KEY] = value;
				if (SaveAutoCompletionsListValueCahnged != null)
					SaveAutoCompletionsListValueCahnged(value);
			}
		}
	}
}
