using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using GpsApplication.Models;
using System.Windows.Input;
using System.Text;


namespace GpsApplication
{
	public partial class MainPage : ContentPage
	{
		private CancellationTokenSource _cancelTokenSource;
		private bool _isCheckingLocation;
		private HttpClient client;
		//Warunki do directions api
		private bool Tools = false;
		private bool Highway = false;
		private int TransportTypeIndex = 0;
		//Saving file for offline
		private JObject temp;
		private string StartLocalizationOfflineTemp;
		private string EndLocalizationOfflineTemp;
		private bool isRouteFromSavedJson = false;
		//Sprawdznie koncowej trasy
		private double nearbyDistance = 18;
		private double nearbyEndLat = 0;
		private double nearbyEndLong = 0;
		//Uruchamianie trasy
		private bool isStartingAtCurrentLocalization = false;

		public ObservableCollection<Routes> Route { get; set; }

		public MainPage()
		{
			Route = new ObservableCollection<Routes>();
			client = new HttpClient();
			InitializeComponent();
			CheckInternet();
		}
		private void ResetIcon(object sender, EventArgs e)
		{
			CheckInternet();
		}
		private void RouteChanged(object sender, TextChangedEventArgs e)
		{
			if(!string.IsNullOrEmpty(EntryAddress.Text) && !string.IsNullOrEmpty(EndAddress.Text))
			{
				SearchAddressButton.IsEnabled = true;
			} else
			{
				SearchAddressButton.IsEnabled = false;
			}
		}
		private async void LocateMe(object sender, EventArgs e)
		{
			MainMap.MapElements.Clear();
			MainMap.Pins.Clear();
			await GetCurrentLocation();
		}
		public async Task GetCurrentLocation()
		{
			try
			{
				_isCheckingLocation = true;
				GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1));
				_cancelTokenSource = new CancellationTokenSource();
				Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
				if(location != null)
				{
					AddPinAndMoveMap(location.Latitude, location.Longitude);
				}
				else
				{
					Debug.WriteLine("Lokalizacja jest null.");
				}
			}
			catch(Exception ex)
			{
				Debug.WriteLine($"Błąd podczas uzyskiwania lokalizacji: {ex.Message}");
			} finally
			{
				_isCheckingLocation = false;
			}
		}
		private void AddPinAndMoveMap(double latitude, double longitude)
		{
			var pin = new Pin
			{
				Label = "Moja lokalizacja",
				Type = PinType.Generic,
				Location = new Location(latitude, longitude),
			};
			MainMap.Pins.Add(pin);
			MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(
				new Location(latitude, longitude), Distance.FromKilometers(0.1)));
		}
		public void CancelRequest()
		{
			if (_isCheckingLocation && _cancelTokenSource != null && _cancelTokenSource.IsCancellationRequested == false)
				_cancelTokenSource.Cancel();
		}
		private void ChangeMap(object sender, EventArgs e)
		{
			if(MainMap.MapType == MapType.Street)
			{
				MainMap.MapType = MapType.Hybrid;
			} else
			{
				MainMap.MapType = MapType.Street;
			}
		}
		public async void CalculateRoute(double StartLat, double StartLong, double EndLat, double EndLong, string avoidOptions, string transportMethod)
		{
			//MainMap.MapElements.Clear();
			//MainMap.Pins.Clear();
			string StartPoint = $"{StartLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{StartLong.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
			string EndPoint = $"{EndLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{EndLong.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
			string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={StartPoint}&destination={EndPoint}{avoidOptions}{transportMethod}&key={"***REMOVED***"}";

			var response = await client.GetStringAsync(url);
			var json = JObject.Parse(response);
			temp = json;
			temp["startlat"] = StartLat;
			temp["startlong"] = StartLong;
			temp["endlat"] = EndLat;
			temp["endlong"] = EndLong;

			AddingRouteToMap(json, StartLat, StartLong, EndLat, EndLong);
		}
		public void AddingRouteToMap(JObject json, double StartLat, double StartLong, double EndLat, double EndLong)
		{
			if (json["geocoded_waypoints"] != null && json["geocoded_waypoints"].Any())
			{
				var geocoderStatus = json["geocoded_waypoints"][0]["geocoder_status"].ToString();
				if (geocoderStatus == "OK")
				{
					var polyline = new Polyline
					{
						StrokeColor = Colors.Purple,
						StrokeWidth = 12
					};

					foreach (var step in json["routes"][0]["legs"][0]["steps"])
					{
						var encodedPolyline = step["polyline"]["points"].ToString();
						var points = DecodePolyline(encodedPolyline);
						foreach (var point in points)
						{
							polyline.Geopath.Add(point);
						}
					}
					var route = json["routes"][0]["legs"][0];
					//Distance
					var distanceText = route["distance"]["text"].ToString();
					//Time
					var timeText = route["duration"]["text"].ToString();

					var pinstart = new Pin
					{
						Label = "Punkt startu",
						Type = PinType.Place,
						Location = new Location(StartLat, StartLong)
					};
					var pinEnd = new Pin
					{
						Label = "Punkt docelowy",
						Type = PinType.Place,
						Location = new Location(EndLat, EndLong)
					};
					MainMap.MapElements.Add(polyline);
					MainMap.Pins.Add(pinstart);
					MainMap.Pins.Add(pinEnd);
					MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(
										new Location(StartLat, StartLong), Distance.FromKilometers(0.3)));
					nearbyEndLat = EndLat;
					nearbyEndLong = EndLong;
					ResultPop(timeText, distanceText);
				}
			}
		}
		public async void ResultPop(string time, string distance)
		{
			AskPop.IsVisible = true;
			if(isRouteFromSavedJson == true)
			{
				SaveButtonResult.IsVisible = false;
				AcceptButtonResult.MinimumWidthRequest = 0;
				AcceptButtonResult.HorizontalOptions = LayoutOptions.FillAndExpand;
				CancelButtonResult.MinimumWidthRequest = 0;
				CancelButtonResult.HorizontalOptions = LayoutOptions.FillAndExpand;
				isRouteFromSavedJson = false;
			}
			SearchBar.IsVisible = false;
			time = time.Replace("hours", "godz");
			TimeTrip.Text = time.Remove(time.Length-1);
			DistanceTrip.Text = distance;
		}
		public void CloseResultPopup(object sender, EventArgs e)
		{
			AskPop.IsVisible = false;
			LocateMeButton.IsVisible = true;
			Title = "Mapa";
			SaveButtonResult.IsVisible = true;
			ShowSearch.IsVisible = true;
			HideSearch.IsVisible = false;
			AcceptButtonResult.MinimumWidthRequest = 120;
			AcceptButtonResult.HorizontalOptions = LayoutOptions.Start;
			CancelButtonResult.MinimumWidthRequest = 120;
			CancelButtonResult.HorizontalOptions = LayoutOptions.End;
			nearbyEndLat = 0;
			nearbyEndLong = 0;
			//Zapisane trasy
			FlagClosing.IsVisible = false;
			FlagShowing.IsVisible = true;
			StackLayoutContainer.IsVisible = false;

			MainMap.MapElements.Clear();
			MainMap.Pins.Clear();
		}
		public async void AcceptButtonClicked(object sender, EventArgs e)
		{
			Title = "W trakcie nawigacji";
			//Wyłączanie wszystkich guzików
			ShowSearch.IsVisible = false;
			HideSearch.IsVisible = false;
			LocateMeButton.IsVisible = false;
			FlagShowing.IsVisible = false;
			FlagClosing.IsVisible = false;

			AskPop.IsVisible = false;
			var location = await Geolocation.GetLastKnownLocationAsync();
			int delay = 1000;
			do
			{
				//Jeżeli nie ma nawigacji z bieżącej lokalizacji, to zablokowac przycisk "Rozpocznij"
				//Jedynie jak jest od lokalizacji uzytkownika to wtedy rozpocznij
				RemoveGenericPins();
				location = await Geolocation.GetLastKnownLocationAsync();
				var pin = new Pin
				{
					Label = "Moja lokalizacja",
					Type = PinType.Generic,
					Location = new Location(location.Latitude, location.Longitude),
				};
				MainMap.Pins.Add(pin);
				//CalculateRoute(location.Latitude, location.Longitude, nearbyEndLat, nearbyEndLong, "","");
				await Task.Delay(delay);
				delay = 5000;



				//await GetCurrentLocation();
			} while (CheckIfRouteEnded(location, nearbyEndLat, nearbyEndLong) == false);
			Debug.WriteLine("Dotarłes na miejsce");
			Title = "Mapa";
			RouteEnded.IsVisible = true;
			MainMap.MapElements.Clear();
			var pinsToRemove = MainMap.Pins.Where(pin => pin.Type == PinType.Place).ToList();
			foreach (var pin in pinsToRemove)
			{
				MainMap.Pins.Remove(pin);
			}
		}
		private void RemoveGenericPins()
		{
			var pinsToRemove = MainMap.Pins.Where(pin => pin.Type == PinType.Generic).ToList();
			foreach (var pin in pinsToRemove)
			{
				MainMap.Pins.Remove(pin);
			}
		}
		private bool CheckIfRouteEnded(Location location,double lat, double longg)
		{
			if (location != null)
			{
				double userLatitude = location.Latitude;
				double userLongitude = location.Longitude;
				double distance = CalculateDistance(userLatitude, userLongitude, lat, longg);
				if (distance <= nearbyDistance)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			return false;
		}
		//Obliczanie odległości od punktu docelowego
		public double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
		{
			var R = 6371e3; 
			var φ1 = lat1 * Math.PI / 180; 
			var φ2 = lat2 * Math.PI / 180;
			var Δφ = (lat2 - lat1) * Math.PI / 180;
			var Δλ = (lon2 - lon1) * Math.PI / 180;

			var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
					Math.Cos(φ1) * Math.Cos(φ2) *
					Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

			var distance = R * c;
			return distance;
		}
		public List<Location> DecodePolyline(string encodedPolyline)
		{
			var polylineChars = encodedPolyline.ToCharArray();
			var index = 0;
			var currentLat = 0;
			var currentLng = 0;
			var locations = new List<Location>();

			while (index < polylineChars.Length)
			{
				int sum = 0;
				int shift = 0;
				int b;
				do
				{
					b = polylineChars[index++] - 63;
					sum |= (b & 0x1F) << shift;
					shift += 5;
				} while (b >= 0x20);

				var deltaLat = ((sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1));
				currentLat += deltaLat;

				sum = 0;
				shift = 0;
				do
				{
					b = polylineChars[index++] - 63;
					sum |= (b & 0x1F) << shift;
					shift += 5;
				} while (b >= 0x20);

				var deltaLng = ((sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1));
				currentLng += deltaLng;

				var lat = currentLat / 1E5;
				var lng = currentLng / 1E5;
				locations.Add(new Location(lat, lng));
			}

			return locations;
		}
		public async void SearchAddress(object sender, EventArgs e)
		{
			SearchAddressButton.Text = "Szukanie...";

			SearchAddressButton.IsEnabled = false;
			TransportTypeIndex = PickerValues.SelectedIndex;

			var Startaddress = EntryAddress.Text.Trim();
			StartLocalizationOfflineTemp = Startaddress;

			var Endaddress = EndAddress.Text.Trim();
			EndLocalizationOfflineTemp = Endaddress;
			(double latitude, double longitude)? coordsStart;
			if (Startaddress == "Moja lokalizacja")
			{
				var localization = await Geolocation.GetLastKnownLocationAsync();
				coordsStart = (localization.Latitude, localization.Longitude);
			} else
			{

				coordsStart = await GetCoordinatesAsync(Startaddress);
			}
			var coordsEnd = await GetCoordinatesAsync(Endaddress);
			if((coordsEnd.Value.latitude == 1000 && coordsStart.Value.latitude == 1000) || coordsStart == coordsEnd)
			{
				EntryAddress.Text = "";
				EntryAddress.Placeholder = "Błędny adres";
				EndAddress.Text = "";
				EndAddress.Placeholder = "Błędny adres";
				SearchAddressButton.Text = "Szukaj";
				SearchAddressButton.IsEnabled = true;
				return;
			}
			else if (coordsEnd.Value.latitude == 1000)
			{
				EndAddress.Text = "";
				EndAddress.Placeholder = "Błędny adres";
				SearchAddressButton.Text = "Szukaj";
				SearchAddressButton.IsEnabled = true;
				return;
			} else if(coordsStart.Value.latitude == 1000)
			{
				EntryAddress.Text = "";
				EntryAddress.Placeholder = "Błędny adres";
				SearchAddressButton.Text = "Szukaj";
				SearchAddressButton.IsEnabled = true;
				return;
			}
			double StartLatitude = coordsStart.Value.latitude;
			double StartLongtitude = coordsStart.Value.longitude;
			double EndLatitude = coordsEnd.Value.latitude;
			double Endlongtitude = coordsEnd.Value.longitude;

			string avoid = "";
			string transportType = "";
			if(Tools && Highway)
			{
				avoid = "&avoid=tolls|highways";
			} else if(Tools)
			{
				avoid = "&avoid=tolls";
			} else if(Highway)
			{
				avoid = "&avoid=highways";
			}
			if(TransportTypeIndex == 0)
			{

			} else if(TransportTypeIndex == 1)
			{
				transportType = "&mode=bicycling";
			} else if(TransportTypeIndex == 2)
			{
				transportType = "&mode=walking";
			}

			CalculateRoute(StartLatitude, StartLongtitude, EndLatitude, Endlongtitude, avoid, transportType);
			EndAddress.Text = "";
			EntryAddress.Text = "";
			//Chowanie klawiatury "na siłe"
			EndAddress.IsEnabled = false;
			EntryAddress.IsEnabled = false;
			EndAddress.IsEnabled = true;
			EntryAddress.IsEnabled = true;
			HighwayRoads.IsChecked = false;
			PaidRoads.IsChecked = false;
			SearchAddressButton.IsEnabled = true;
			SearchAddressButton.Text = "Szukaj";
		}
		private async Task<(double latitude, double longitude)?> GetCoordinatesAsync(string address)
		{
			var encodedAddress = Uri.EscapeDataString(address);
			var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={"***REMOVED***"}";
			try
			{
				var response = await client.GetStringAsync(url);
				var json = JObject.Parse(response);
				if (json["status"].ToString().Trim() == "OK")
				{
					var location = json["results"][0]["geometry"]["location"];
					double latitude = (double)location["lat"];
					double longitude = (double)location["lng"];
					return (latitude, longitude);
				}
				else
				{
					return (1000, 1000);
				}
			} catch(Exception e)
			{
				Debug.WriteLine(e);
				return (1000, 1000);
			}
		}
		private void SearchPopup(object sender, EventArgs e)
		{
			//Zapisane trasy
			FlagClosing.IsVisible = false;
			FlagShowing.IsVisible = false;

			PickerValues.SelectedIndex = 0;
			EntryAddress.Placeholder = "Adres początkowy";
			EndAddress.Placeholder = "Adres docelowy";
			EntryAddress.Text = "Moja lokalizacja";
			Title = "Wyszukaj połączenie";
			SearchBar.IsVisible = true;
			ShowSearch.IsVisible = false;
			HideSearch.IsVisible = true;
			LocateMeButton.IsVisible = false;
		}
		private void CloseSearchPopup(object sender, EventArgs e)
		{
			//Zapisane trasy
			FlagClosing.IsVisible = false;
			FlagShowing.IsVisible = true;

			SearchBar.IsVisible = false;
			ShowSearch.IsVisible = true;
			HideSearch.IsVisible = false;
			LocateMeButton.IsVisible = true;
			Title = "Mapa";
			RouteEnded.IsVisible = false;
		}
		//Checkbox
		private void PaidRoadsCheckBox(object sender, CheckedChangedEventArgs e)
		{
			Tools = e.Value; 
		}
		private void HighwayRoadsCheckBox(object sender, CheckedChangedEventArgs e)
		{
			Highway = e.Value;
		}
		//Saving json
		private async void SaveRoute(object sender, EventArgs e)
		{
			string combine = StartLocalizationOfflineTemp + " do " + EndLocalizationOfflineTemp + ".json";
			//Tworzenie pliku jeżeli nie istnieje
			string fileRoutesPath = Path.Combine(FileSystem.AppDataDirectory, "Routes.txt");
			if (!File.Exists(fileRoutesPath))
			{
				File.Create(fileRoutesPath).Close();
			}

			string jsonString = temp.ToString();
			string filePath = Path.Combine(FileSystem.AppDataDirectory, combine);
			await File.WriteAllTextAsync(filePath, jsonString);
			await File.AppendAllTextAsync(fileRoutesPath, $"{StartLocalizationOfflineTemp}" + " do " + $"{EndLocalizationOfflineTemp}" + Environment.NewLine);
		}
		private async void LoadRoute(string start, string end)
		{
			Title = "Wczytana trasa";
			string fileName = $"{start}" + " do " + $"{end}" + ".json";
			string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
			var json = await File.ReadAllTextAsync(filePath);

			if (json != null)
			{
				var jsonLocalization = JObject.Parse(json);
				isRouteFromSavedJson = true;
				AddingRouteToMap(jsonLocalization, (double)jsonLocalization["startlat"], (double)jsonLocalization["startlong"], (double)jsonLocalization["endlat"], (double)jsonLocalization["endlong"]);
			}
		}
		public void LoadLinesFromFile(object sender, EventArgs e)
		{
			StackLayoutContainer.IsVisible = true;
			ShowSearch.IsVisible = false;
			HideSearch.IsVisible = false;
			FlagShowing.IsVisible = false;
			FlagClosing.IsVisible = true;
			Title = "Zapisane trasy";
			string filePath = Path.Combine(FileSystem.AppDataDirectory, "Routes.txt");
			if (File.Exists(filePath))
			{
				var lines = File.ReadAllLines(filePath);
				foreach (var line in lines)
				{
					var cities = line.Split(new string[] { " do " }, StringSplitOptions.None);
					Route.Add(new Routes
					{
						CityStart = cities[0].Trim(),
						CityEnd = cities[1].Trim(),
					});
				}
				CollectionRoutes.ItemsSource = Route;
			}
		}
		public void NavigateCommand(object sender, EventArgs e)
		{
			ImageButton button = (ImageButton)sender;
			var route = button.CommandParameter as Models.Routes;
			//Logika nawigacji
			StackLayoutContainer.IsVisible = false;
			FlagShowing.IsVisible = false;
			FlagClosing.IsVisible = false;
			LoadRoute(route.CityStart, route.CityEnd);
		}
		public void DeleteRouteCommand(object sender, EventArgs e)
		{
			ImageButton button = (ImageButton)sender;
			var route = button.CommandParameter as Models.Routes;
			//Usuwanie json
			string jsonpath = Path.Combine(FileSystem.AppDataDirectory, $"{route.CityStart}" + " do " + $"{route.CityEnd}" + ".json");
			if(File.Exists(jsonpath))
			{
				File.Delete(jsonpath);
			}
			//Usuwanie linijki z routes.txt
			string filepath = Path.Combine(FileSystem.AppDataDirectory, "Routes.txt");
			if(File.Exists(filepath))
			{
				string lineToRemove = $"{route.CityStart}" + " do " + $"{route.CityEnd}";
				var lines = File.ReadAllLines(filepath).ToList();
				lines.RemoveAll(line => line.Contains(lineToRemove));
				File.WriteAllLines(filepath, lines);
				CloseLoadedLines(null,null);
				LoadLinesFromFile(null, null);
			}
		}
		public void CloseLoadedLines(object sender, EventArgs e)
		{
			StackLayoutContainer.IsVisible = false;
			FlagShowing.IsVisible = true;
			FlagClosing.IsVisible = false;
			ShowSearch.IsVisible = true;
			HideSearch.IsVisible = false;
			Title = "Mapa";
			Route.Clear();
		}
		public async void CheckInternet()
		{
			var current = Connectivity.NetworkAccess;
			if(current == NetworkAccess.Internet)
			{
				WifiIcon.IconImageSource = "wifi.png";
			} else
			{
				WifiIcon.IconImageSource = "nowifi.png";
			}
		}
	}
}
