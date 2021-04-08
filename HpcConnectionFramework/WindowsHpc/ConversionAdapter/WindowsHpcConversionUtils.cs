using System;
using System.Collections.Generic;
using System.Reflection;
using HaaSMiddleware.DomainObjects.JobManagement;
using Microsoft.Hpc.Scheduler;

namespace HaaSMiddleware.HpcConnectionFramework.WindowsHpc.ConversionAdapter {
	public class WindowsHpcConversionUtils {
		public static DateTime? GetNullableDateTime(DateTime dateTime) {
			if (dateTime == DateTime.MinValue)
				return null;
			return dateTime;
		}

		public static IStringCollection ConvertStringNameArrayToMsStringCollection(string[] stringNames) {
			IStringCollection stringCollection = new StringCollection();
			if (stringNames != null) {
				foreach (string stringName in stringNames) {
					stringCollection.Add(stringName);
				}
			}
			return stringCollection;
		}

		public static string[] ConvertMsStringCollectionToStringNameArray(IStringCollection stringCollection) {
			if (stringCollection != null) {
				string[] stringNames = new string[stringCollection.Count];
				int i = 0;
				foreach (string str in stringCollection) {
					stringNames[i] = str;
					i++;
				}
				return stringNames;
			}
			return new string[0];
		}

		public static Dictionary<string, string> GetAllPropertiesFromSource(object source) {
			Dictionary<string, string> parameters = new Dictionary<string, string>();
			foreach (PropertyInfo property in source.GetType().GetProperties()) {
				object value = property.GetValue(source, null);
				if (value != null)
					parameters[property.Name] = value.ToString();
				else
					parameters[property.Name] = null;
			}
			return parameters;
		}

		internal static IStringCollection ConvertTaskDependenciesToMsStringCollection(ICollection<TaskSpecification> dependencies) {
			IStringCollection stringCollection = new StringCollection();
			if (dependencies != null) {
				foreach (TaskSpecification dependency in dependencies) {
					stringCollection.Add(dependency.Name);
				}
			}
			return stringCollection;
		}
	}
}