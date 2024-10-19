using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using GpsApplication.Models;
using Newtonsoft.Json;
using Microsoft.Maui.Storage;


namespace GpsApplication
{
	public partial class MainPage : ContentPage
	{
		private CancellationTokenSource _cancelTokenSource;
		private bool _isCheckingLocation;
		private HttpClient client;
		private bool Tools = false;
		private bool Highway = false;
		//Saving file for offline
		private JObject temp;
		private string StartLocalizationOfflineTemp;
		private string EndLocalizationOfflineTemp;
		private FileManager _file;
		private bool isRouteFromSavedJson = false;

		public MainPage()
		{
			_file = new FileManager();
			client = new HttpClient();
			InitializeComponent();
		}
		private void LocateMe(object sender, EventArgs e)
		{
			MainMap.MapElements.Clear();
			MainMap.Pins.Clear();
			GetCurrentLocation();
		}
		public async Task GetCurrentLocation()
		{
			try
			{
				_isCheckingLocation = true;
				GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(0.1));
				_cancelTokenSource = new CancellationTokenSource();
				Location location = await Geolocation.Default.GetLocationAsync(request, _cancelTokenSource.Token);
				if(location != null)
				{
					Debug.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
					AddPinAndMoveMap(location.Latitude, location.Longitude);
				}
			}catch(Exception ex)
			{
				Debug.WriteLine(ex);
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
				Address = "Me",
				Type = PinType.Place,
				Location = new Location(latitude, longitude)
			};
			MainMap.Pins.Add(pin);
			MainMap.MoveToRegion(MapSpan.FromCenterAndRadius(
				new Location(latitude, longitude), Distance.FromKilometers(0.5)));
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
		public async void CalculateRoute(double StartLat, double StartLong, double EndLat, double EndLong, string avoidOptions)
		{
			MainMap.MapElements.Clear();
			MainMap.Pins.Clear();
			string StartPoint = $"{StartLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{StartLong.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
			string EndPoint = $"{EndLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{EndLong.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
			string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={StartPoint}&destination={EndPoint}{avoidOptions}&key={"***REMOVED***"}";

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

			SaveButtonResult.IsVisible = true;
			AcceptButtonResult.MinimumWidthRequest = 120;
			AcceptButtonResult.HorizontalOptions = LayoutOptions.Start;
			CancelButtonResult.MinimumWidthRequest = 120;
			CancelButtonResult.HorizontalOptions = LayoutOptions.End;

			MainMap.MapElements.Clear();
			MainMap.Pins.Clear();
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
			var Startaddress = EntryAddress.Text;
			StartLocalizationOfflineTemp = Startaddress;
			var Endaddress = EndAddress.Text;
			EndLocalizationOfflineTemp = Endaddress;
			var coordsStart = await GetCoordinatesAsync(Startaddress);
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

			CalculateRoute(StartLatitude, StartLongtitude, EndLatitude, Endlongtitude, avoid);
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
			EntryAddress.Placeholder = "Adres początkowy";
			EndAddress.Placeholder = "Adres docelowy";
			SearchBar.IsVisible = true;
			ShowSearch.IsVisible = false;
			HideSearch.IsVisible = true;
			LocateMeButton.IsVisible = false;
		}
		private void CloseSearchPopup(object sender, EventArgs e)
		{
			SearchBar.IsVisible = false;
			ShowSearch.IsVisible = true;
			HideSearch.IsVisible = false;
			LocateMeButton.IsVisible = true;
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
			string jsonString = temp.ToString();
			string filePath = Path.Combine(FileSystem.AppDataDirectory, combine);
			await File.WriteAllTextAsync(filePath, jsonString);
		}
		private async void LoadRoute(object sender, EventArgs e)
		{
			//Do zmiany aby pobierało nazwę z listy zapisanych plików, i po wyborze danej nazwy tutaj przesyłało
			string fileName = "palczowice do Wadowice.json";
			string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
			var json = await File.ReadAllTextAsync(filePath);

			if (json != null)
			{
				var jsonLocalization = JObject.Parse(json);
				isRouteFromSavedJson = true;
				AddingRouteToMap(jsonLocalization, (double)jsonLocalization["startlat"], (double)jsonLocalization["startlong"], (double)jsonLocalization["endlat"], (double)jsonLocalization["endlong"]);
			}

		}
	}
}
