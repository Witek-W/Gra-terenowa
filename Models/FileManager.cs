using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GpsApplication.Models
{
	public class FileManager
	{
		public async Task SaveJsonFile<T>(T data, string fileName)
		{
			string jsonString = System.Text.Json.JsonSerializer.Serialize(data);
			string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
			await File.WriteAllTextAsync(filePath,jsonString);
		}

		public async Task<dynamic> LoadJsonFile(string fileName)
		{
			var json = await File.ReadAllTextAsync(fileName);
			dynamic data = JsonConvert.DeserializeObject(json);
			return data;
		}
	}
}
