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
using System.Globalization;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Maui.Layouts;
using System.Xml.Linq;
using GpsApplication.Pages.Popups;


namespace GpsApplication
{
	public partial class MainPage : ContentPage
	{
		//
		//Zrobić porządek z funkcjami
		//


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
		private string timeString;
		private string distanceString;
		private bool navigationStart = false;
		private bool cancel = false;
		//Rozmiar ekranu urządzenia
		private double screenWidth;
		private double screenHeight;
		//Popup
		private InfoPopup _popup;
		public ObservableCollection<Routes> Route { get; set; }
		public ObservableCollection<Pointt> GamePointsDB { get; set; }
		private readonly AppDbContext _context;
		private readonly Auth _auth;
		public MainPage()
		{
			Route = new ObservableCollection<Routes>();
			GamePointsDB = new ObservableCollection<Pointt>();
			client = new HttpClient();
			_context = new AppDbContext();
			_auth = new Auth(_context);
			screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
			screenHeight = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
			Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
			InitializeComponent();
			//Przycisk Zmień Mapę
			AbsoluteLayout.SetLayoutBounds(ChangeMapLayerButton, new Rect(0.01, screenHeight * 0.75, 70, 45));
			AbsoluteLayout.SetLayoutFlags(ChangeMapLayerButton, AbsoluteLayoutFlags.None);
			//Przycisk Szukaj trasę
			AbsoluteLayout.SetLayoutBounds(ShowSearch, new Rect(0.01, screenHeight * 0.68, 70, 45));
			AbsoluteLayout.SetLayoutFlags(ShowSearch, AbsoluteLayoutFlags.None);
			//Przycisk Zamknij szukaj trasę
			AbsoluteLayout.SetLayoutBounds(HideSearch, new Rect(0.01, screenHeight * 0.68, 70, 45));
			AbsoluteLayout.SetLayoutFlags(HideSearch, AbsoluteLayoutFlags.None);
			//Przycisk Anulowania trasy offline
			AbsoluteLayout.SetLayoutBounds(CancelOfflineButton, new Rect(0.01, screenHeight * 0.68, 70, 45));
			AbsoluteLayout.SetLayoutFlags(CancelOfflineButton, AbsoluteLayoutFlags.None);
			//Przycisk Sprawdzenie pobliskich punktów
			AbsoluteLayout.SetLayoutBounds(CheckNearbyPointsButton, new Rect(0.01, screenHeight * 0.02, 70, 45));
			AbsoluteLayout.SetLayoutFlags(CheckNearbyPointsButton, AbsoluteLayoutFlags.None);
			//Zapisane trasy
			AbsoluteLayout.SetLayoutBounds(FlagShowing, new Rect(0.01, screenHeight * 0.61, 70, 45));
			AbsoluteLayout.SetLayoutFlags(FlagShowing, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(FlagClosing, new Rect(0.01, screenHeight * 0.61, 70, 45));
			AbsoluteLayout.SetLayoutFlags(FlagClosing, AbsoluteLayoutFlags.None);

			Refresh(null, null);
			CheckUser();
		}
		//Zmiana ikony uzytkownika po wylogowaniu/zalogowaniu
		protected override async void OnNavigatedTo(NavigatedToEventArgs e)
		{
			await CheckUser();
		}
		private async Task ShowPopup(string error)
		{
			_popup = new InfoPopup(error);
			await Navigation.PushModalAsync(_popup);
		}
		public async void ScanForNearbyPoints(object sender, EventArgs e)
		{
			var userlogged = await SecureStorage.GetAsync("user_login");
			if(userlogged != null)
			{
				if (GamePointsDB.Count == 0)
				{
					GamePointsDB = await LoadGamePointsFromFile();
				}
				var location = await Geolocation.GetLastKnownLocationAsync();
				foreach (var point in GamePointsDB)
				{
					double distance = CalculateDistance(location.Latitude, location.Longitude, point.lat, point.lon);

					if (distance <= nearbyDistance)
					{
						DownloadQuiz(point.Name);
						return;
					}
				}
				ShowPopup("Nie znaleziono w okolicy punktów gry terenowej");
			} else
			{
				ShowPopup("Musisz się zalogować, jeżeli chcesz wykonać quiz");
			}
			
		}
		public async void DownloadQuiz(string name)
		{
			List<Quiz> list;
			var network = Connectivity.Current.NetworkAccess;
			if (network == NetworkAccess.Internet)
			{
				list = _context.Quiz.AsEnumerable().Where(p => p.PlaceName == name).ToList();
			} else
			{
				list = await LoadQuizFromJsonOffline(name);
			}
			await Navigation.PushAsync(new QuizPage(list));
		}
		//Odczytywanie zapisywanego quizu.json na List<Quiz>
		private async Task<List<Quiz>> LoadQuizFromJsonOffline(string filename)
		{
			var filepath = Path.Combine(FileSystem.AppDataDirectory, filename + "quiz.json");
			if(File.Exists(filepath))
			{
				var json = await File.ReadAllTextAsync(filepath);
				var quizlist = JsonConvert.DeserializeObject<List<Quiz>>(json);
				return quizlist;
			}
			return new List<Quiz>();
		}
		public void SetNoUser()
		{
			userimage.IconImageSource = "user.png";
		}
		public void SetUser()
		{
			userimage.IconImageSource = "userlogged.png";
		}
		private async void GoToUserManager(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new UserManager());
		}
		public async Task CheckUser()
		{
			var userLogin = await SecureStorage.GetAsync("user_login");
			if(userLogin != null)
			{
				userimage.IconImageSource = "userlogged.png";
			} else
			{
				userimage.IconImageSource = "user.png";
			}
		}
		private async void Refresh(object sender, EventArgs e)
		{
			var network = Connectivity.Current.NetworkAccess;
			if(network == NetworkAccess.Internet)
			{
				try
				{
					await LoadRoutesFromDatabase();
					UpdateDataBaseOfflineTxt();
				}
				catch(Exception ex)
				{

				}
				WifiIcon.IconImageSource = "wifi.png";
			} else
			{
				WifiIcon.IconImageSource = "nowifi.png";
			}
			CheckUser();
		}
		//Aktualizowanie bazy danych punktów na podstawie pliku offline
		public async Task UpdateDataBaseOfflineTxt()
		{
			var userLogin = await SecureStorage.GetAsync("user_login");
			if(userLogin != null)
			{
				var useridstring = await SecureStorage.GetAsync("user_id");
				var filePath = Path.Combine(FileSystem.AppDataDirectory, "Score.txt");
				var lines = File.ReadAllLines(filePath);
				if (lines.Length != 1)
				{
					int id = int.Parse(useridstring);
					int userDatabasePoints =  _auth.ReturnUserScore(id);
					int userScoreOffline = int.Parse(lines[0]);
					userScoreOffline += userDatabasePoints;
					await _auth.UpdateUserScore(id, userScoreOffline);
					for(int i = 1; i < lines.Length; i++)
					{
						await _auth.AddingUserToQuizHistory(id, lines[i]);
					}
					await File.WriteAllTextAsync(filePath, "0");
				}
			}
		}
		private async void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
		{
			Refresh(null,null);
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
		public async void CalculateRoute(double StartLat, double StartLong, double EndLat, double EndLong)
		{
			//MainMap.MapElements.Clear();
			//MainMap.Pins.Clear();
			string StartPoint = $"{StartLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{StartLong.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
			string EndPoint = $"{EndLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{EndLong.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
			string url = $"https://maps.googleapis.com/maps/api/directions/json?origin={StartPoint}&destination={EndPoint}&mode=walking&key={"***REMOVED***"}";

			var response = await client.GetStringAsync(url);
			var json = JObject.Parse(response);
			temp = json;
			temp["startlat"] = StartLat;
			temp["startlong"] = StartLong;
			temp["endlat"] = EndLat;
			temp["endlong"] = EndLong;

			await AddingRouteToMap(json, StartLat, StartLong, EndLat, EndLong);
		}
		public async Task AddingRouteToMap(JObject json, double StartLat, double StartLong, double EndLat, double EndLong)
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
						if (navigationStart == false)
						{
							ResultPop(timeText, distanceText);
						}
						else
						{
							timeText = timeText.Replace("hours", "godz");
							timeString = timeText.Remove(timeText.Length - 1);
							distanceString = distanceText;
						}
					
				}
			}
		}
		public async void ResultPop(string time, string distance)
		{
			TestPop.WidthRequest = screenWidth;
			TestPop.IsVisible = true;
			if (isRouteFromSavedJson == true)
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
			timeString = time.Remove(time.Length - 1);
			TimeTriptest.Text = timeString; 
			distanceString = distance;
			DistanceTriptest.Text = distance;
			isRouteFromSavedJson = false;
		}
		public void CloseResultPopup(object sender, EventArgs e)
		{
			TestPop.IsVisible = false;
			LocateMeButton.IsVisible = true;
			CheckNearbyPointsButton.IsVisible = true;
			Title = "Mapa";
			SaveButtonResult.IsVisible = true;
			ShowSearch.IsVisible = true;
			HideSearch.IsVisible = false;
			AcceptButtonResult.MinimumWidthRequest = 120;
			AcceptButtonResult.HorizontalOptions = LayoutOptions.Start;
			CancelButtonResult.MinimumWidthRequest = 120;
			CancelButtonResult.HorizontalOptions = LayoutOptions.End;

			AcceptButtonResult.IsVisible = true;
			Grid.SetColumnSpan(SaveButtonResult, 2);
			SaveButtonResult.MinimumWidthRequest = 0;
			SaveButtonResult.HorizontalOptions = LayoutOptions.Center;


			nearbyEndLat = 0;
			nearbyEndLong = 0;
			//Zapisane trasy
			FlagClosing.IsVisible = false;
			FlagShowing.IsVisible = true;
			StackLayoutContainer.IsVisible = false;

			SaveButtonResult.Text = "Zapisz";
			SaveButtonResult.IsEnabled = true;
			Route.Clear();

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
			CheckNearbyPointsButton.IsVisible = false;
			FlagShowing.IsVisible = false;
			FlagClosing.IsVisible = false;
			SaveButtonResult.Text = "Zapisz";
			SaveButtonResult.IsEnabled = true;
			navigationStart = true;
			TestPop.IsVisible = false;
			cancel = false;
			var location = await Geolocation.GetLastKnownLocationAsync();
			int delay = 1000;
			do
			{
				location = await Geolocation.GetLastKnownLocationAsync();
				var pin = new Pin
				{
					Label = "Moja lokalizacja",
					Type = PinType.Generic,
					Location = new Location(location.Latitude, location.Longitude),
				};
				MainMap.Pins.Add(pin);
				var current = Connectivity.NetworkAccess;
				if (current == NetworkAccess.Internet)
				{
					MainMap.MapElements.Clear();
					CalculateRoute(location.Latitude, location.Longitude, nearbyEndLat, nearbyEndLong);
					NavigationOnlineTest.WidthRequest = screenWidth;
					NavigationOnlineTest.IsVisible = true;
					DistanceOnlineLabel.Text = distanceString;
					TimeOnlineLabel.Text = timeString;
				} else
				{
					CancelOfflineButton.IsVisible = true;
				}
				if (cancel) break;
				await Task.Delay(delay);
				delay = 10000;
				MainMap.Pins.Clear();
			} while (CheckIfRouteEnded(location, nearbyEndLat, nearbyEndLong) == false);
			Title = "Mapa";
			CancelOfflineButton.IsVisible = false;
			if (!cancel) RouteEnded.IsVisible = true;
			cancel = false;
			NavigationOnlineTest.IsVisible = false;	
			MainMap.MapElements.Clear();
			navigationStart = false;
			var pinsToRemove = MainMap.Pins.Where(pin => pin.Type == PinType.Place).ToList();
			foreach (var pin in pinsToRemove)
			{
				MainMap.Pins.Remove(pin);
			}
			TestPop.IsVisible = false;
			ShowSearch.IsVisible = true;
			LocateMeButton.IsVisible = true;
			CheckNearbyPointsButton.IsVisible = true;
			FlagShowing.IsVisible = true;
		}
		private void CancelNavigationButton(object sender, EventArgs e)
		{
			cancel = true;
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
		public async void SearchAddress(string Endaddress, double lat, double lon)
		{
			var localization = await Geolocation.GetLastKnownLocationAsync();
			var coordsStart = (localization.Latitude, localization.Longitude);
			EndLocalizationOfflineTemp = Endaddress;
			var coordsEnd = ( lat, lon );
			double StartLatitude = coordsStart.Latitude;
			double StartLongtitude = coordsStart.Longitude;
			double EndLatitude = coordsEnd.lat;
			double Endlongtitude = coordsEnd.lon;

			CalculateRoute(StartLatitude, StartLongtitude, EndLatitude, Endlongtitude);
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
			//Szerokość okna
			SearchBar.WidthRequest = screenWidth;
			//Zapisane trasy
			FlagClosing.IsVisible = false;
			FlagShowing.IsVisible = false;
			Title = "Wyszukaj cel";
			SearchBar.IsVisible = true;
			ShowSearch.IsVisible = false;
			HideSearch.IsVisible = true;
			LocateMeButton.IsVisible = false;
			CheckNearbyPointsButton.IsVisible = false;
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
			CheckNearbyPointsButton.IsVisible = true;
			Title = "Mapa";
			RouteEnded.IsVisible = false;
		}
		//Saving json
		private async void SaveRoute(object sender, EventArgs e)
		{
			string combine = EndLocalizationOfflineTemp + ".json";
			//Tworzenie pliku jeżeli nie istnieje
			string fileRoutesPath = Path.Combine(FileSystem.AppDataDirectory, "Routes.txt");
			if (!File.Exists(fileRoutesPath))
			{
				File.Create(fileRoutesPath).Close();
			}
			string[] existingRoutes = await File.ReadAllLinesAsync(fileRoutesPath);
			bool routeExists = existingRoutes.Contains(EndLocalizationOfflineTemp);
			if(!routeExists)
			{
				string jsonString = temp.ToString();
				string filePath = Path.Combine(FileSystem.AppDataDirectory, combine);
				var list = _context.Quiz.AsEnumerable().Where(p => p.PlaceName == EndLocalizationOfflineTemp).ToList();
				var json = JsonConvert.SerializeObject(list);
				var filePath2 = Path.Combine(FileSystem.AppDataDirectory, EndLocalizationOfflineTemp + "quiz.json");
				await File.WriteAllTextAsync(filePath, jsonString);
				await File.AppendAllTextAsync(fileRoutesPath, $"{EndLocalizationOfflineTemp}" + Environment.NewLine);
				//Zapisywanie quizu żeby był dostępny offline
				await File.WriteAllTextAsync(filePath2, json);
				//Tworzenie pliku z wynikami jeżeli nie istnieje
				string fileScorePath = Path.Combine(FileSystem.AppDataDirectory, "Score.txt");
				if (!File.Exists(fileScorePath))
				{
					File.Create(fileScorePath).Close();
					File.WriteAllText(fileScorePath, "0");
				}
				//Komunikat o zapisywaniu
				SaveButtonResult.Text = "Zapisano!";
				SaveButtonResult.IsEnabled = false;
			} else
			{
				SaveButtonResult.Text = "Zapisano!";
				SaveButtonResult.IsEnabled = false;
			}
			
		}
		private async void LoadRoute(string start, string end)
		{
			Title = "Wczytana trasa";
			string fileName = $"{end}" + ".json";
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
			StackLayoutContainer.WidthRequest = screenWidth;
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
					Route.Add(new Routes
					{
						CityEnd = line.Trim(),
					});
				}
				CollectionRoutes.ItemsSource = Route;
			}
		}
		public async Task LoadRoutesFromDatabase()
		{
			GamePointsDB.Clear();
			var routes = await _context.GamePoints.ToListAsync();
			foreach(var points in routes)
			{
				GamePointsDB.Add(new Pointt
				{
					Name = points.Name,
					lat = points.Latitude,
					lon = points.Longitude,
				});
			}
			await SaveGamePointsToFile(GamePointsDB);
			RoutesFromDB.ItemsSource = GamePointsDB;
		}
		private async Task SaveGamePointsToFile(ObservableCollection<Pointt> gamePoints)
		{
			var json = JsonConvert.SerializeObject(gamePoints, Formatting.Indented);
			var filepath = Path.Combine(FileSystem.AppDataDirectory, "GamePointsOffline.json");
			await File.WriteAllTextAsync(filepath, json);
		}
		private async Task<ObservableCollection<Pointt>> LoadGamePointsFromFile()
		{
			var filepath = Path.Combine(FileSystem.AppDataDirectory, "GamePointsOffline.json");
			if(File.Exists(filepath))
			{
				var json = await File.ReadAllTextAsync(filepath);
				var result = JsonConvert.DeserializeObject<ObservableCollection<Pointt>>(json);
				return result;
			}
			return new ObservableCollection<Pointt>();
		}
		public void SearchRouteFromDB(object sender, EventArgs e)
		{
			ImageButton button = (ImageButton)sender;
			var point = button.CommandParameter as Models.Pointt;
			SearchAddress(point.Name,point.lat, point.lon);
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
			string jsonpath = Path.Combine(FileSystem.AppDataDirectory, $"{route.CityEnd}" + ".json");
			if(File.Exists(jsonpath))
			{
				File.Delete(jsonpath);
			}
			//Usuwanie linijki z routes.txt
			string filepath = Path.Combine(FileSystem.AppDataDirectory, "Routes.txt");
			if(File.Exists(filepath))
			{
				string lineToRemove = $"{route.CityEnd}";
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
	}
}
