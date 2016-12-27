﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HentaiViewer.Common;
using HentaiViewer.Models;
using HentaiViewer.Sites;
using PropertyChanged;
using RestSharp;

namespace HentaiViewer.ViewModels {
	[ImplementPropertyChanged]
	public class HentaiViewerWindowViewModel : IDisposable {
		public static HentaiViewerWindowViewModel Instance;
		public HentaiModel _hentai;
		public ObservableCollection<object> _imageObjects = new ObservableCollection<object>();

		private bool _isClosing;

		public HentaiViewerWindowViewModel(HentaiModel hentai) {
			Instance = this;
			_hentai = hentai;
			ImageObjects = new ReadOnlyObservableCollection<object>(_imageObjects);
			GetImagesCommand = new ActionCommand(async () => {
				FetchButtonVisibility = Visibility.Collapsed;
				PregressBarVisibility = Visibility.Visible;
				var links = await SelectSite(hentai);
				PregressBarVisibility = Visibility.Collapsed;
				for (var i = 0; i < links.Count; i++) {
					if (_isClosing) break;
					_imageObjects.Add(new ImageModel{PageNumber = i, Source = links[i]});
					await Task.Delay(200);
				}
			});
			SaveImagesCommand = new ActionCommand(SaveImages);
		}

		public ReadOnlyObservableCollection<object> ImageObjects { get; }

		public string Pages { get; set; } = "0 : 0";

		public ICommand GetImagesCommand { get; }
		public ICommand SaveImagesCommand { get; }

		public Visibility FetchButtonVisibility { get; set; } = Visibility.Visible;

		public Visibility PregressBarVisibility { get; set; } = Visibility.Collapsed;

		public Visibility SaveProgress { get; set; } = Visibility.Collapsed;

		public async void Dispose() {
			_isClosing = true;
			await Task.Delay(100);
			_imageObjects.Clear();
			await Task.Delay(100);
			GC.Collect();
		}

		private async Task<List<object>> SelectSite(HentaiModel hentai) {
			Tuple<List<object>, int> tpl;
			switch (hentai.Site) {
				case "hentaicafe":
					tpl = await Sites.HentaiCafe.CollectImagesTaskAsync(hentai);
					Pages = $"{tpl.Item1.Count} : {tpl.Item2}";
					return tpl.Item1;
				case "nhentai":
					tpl = await nHentai.CollectImagesTaskAsync(hentai);
					Pages = $"{tpl.Item1.Count} : {tpl.Item2}";
					return tpl.Item1;
				case "exhentai":
					tpl = await ExHentai.CollectImagesTaskAsync(hentai);
					Pages = $"{tpl.Item1.Count} : {tpl.Item2}";
					return tpl.Item1;
			}
			return null;
		}

		private async void SaveImages() {
			var folder = Path.Combine(Directory.GetCurrentDirectory(), "Saves", _hentai.Site, MD5Converter.MD5Hash(_hentai.Title));
			
			if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
			SaveProgress = Visibility.Visible;
			for (var i = 0; i < _imageObjects.Count; i++) {
				if (_isClosing) break;
				var img = (ImageModel)ImageObjects[i];
				if (img.Source is string) {
					var client = new RestClient {BaseUrl = new Uri((string) img.Source)};
					var imgBytes = await client.ExecuteGetTaskAsync(new RestRequest());
					img.Source = ExHentai.BytesToBitmapImage(imgBytes.RawBytes);
				}
				var encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create((BitmapSource) img.Source));
				using (var stream = new FileStream($"{Path.Combine(folder, $"{i}.png")}", FileMode.Create)) {
					encoder.Save(stream);
				}
			}
			File.WriteAllText(Path.Combine(folder, "INFO.txt"), $"{_hentai.Title}\n" +
			                                                    $"{_hentai.Link}\n" +
			                                                    $"Created: {DateTime.Now}\n" +
			                                                    $"{_imageObjects.Count} images\n" +
			                                                    $"");
			SaveProgress = Visibility.Collapsed;
		}
	}
}