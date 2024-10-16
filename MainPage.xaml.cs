using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Newtonsoft.Json.Linq;


namespace GpsApplication
{
	public partial class MainPage : ContentPage
	{
		private CancellationTokenSource _cancelTokenSource;
		private bool _isCheckingLocation;
		private HttpClient client;
		private bool Tools = false;
		private bool Highway = false;

		public MainPage()
		{
			client = new HttpClient();
			InitializeComponent();
		}
		private void LocateMe(object sender, EventArgs e)
		{
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
			SearchBar.IsVisible = false;

			if (json["geocoded_waypoints"] != null && json["geocoded_waypoints"].Any()) 
			{
				var geocoderStatus = json["geocoded_waypoints"][0]["geocoder_status"].ToString();
				if (geocoderStatus == "OK")
				{
					var polyline = new Polyline
					{
						StrokeColor = Colors.Purple,
						StrokeWidth = 5
					};

					foreach(var step in json["routes"][0]["legs"][0]["steps"])
					{
						var encodedPolyline = step["polyline"]["points"].ToString();
						var points = DecodePolyline(encodedPolyline);
						foreach(var point in points)
						{
							polyline.Geopath.Add(point);
						}
					}
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
				}
			} 
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
			var Startaddress = EntryAddress.Text;
			var Endaddress = EndAddress.Text;
			var coordsStart = await GetCoordinatesAsync(Startaddress);
			var coordsEnd = await GetCoordinatesAsync(Endaddress);

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
			HighwayRoads.IsChecked = false;
			PaidRoads.IsChecked = false;
		}
		private async Task<(double latitude, double longitude)?> GetCoordinatesAsync(string address)
		{
			var encodedAddress = Uri.EscapeDataString(address);
			var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={"***REMOVED***"}";

			var response = await client.GetStringAsync(url);
			var json = JObject.Parse(response);

			if (json["status"].ToString().Trim() == "OK")
			{
				var location = json["results"][0]["geometry"]["location"];
				double latitude = (double)location["lat"];
				double longitude = (double)location["lng"];
				return (latitude, longitude);
			}

			return null;
		}
		private void SearchPopup(object sender, EventArgs e)
		{
			SearchBar.IsVisible = true;
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
	}
}
