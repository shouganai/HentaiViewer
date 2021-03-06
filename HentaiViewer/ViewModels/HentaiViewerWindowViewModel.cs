﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using HentaiViewer.Common;
using HentaiViewer.Models;
using HentaiViewer.Sites;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PropertyChanged;
using RestSharp;

namespace HentaiViewer.ViewModels {
    [ImplementPropertyChanged]
    public class HentaiViewerWindowViewModel : IDisposable {
        private readonly ObservableCollection<object> _imageObjects = new ObservableCollection<object>();
        private ObservableCollection<object> _images = new ObservableCollection<object>();
        private bool _adding;

        //private List<object> _images;

        private bool _isClosing;
        //private bool _saving;

        public HentaiViewerWindowViewModel(HentaiModel hentai, bool saveEnabled = true) {
            Hentai = hentai;
            CopyLinkCommand = new ActionCommand(() => Clipboard.SetText(Hentai.Link));
            ChangeModeCommand = new ActionCommand(ChangeMode);
            JumpCommand = new ActionCommand(Jump);
            ImageObjects = new ReadOnlyObservableCollection<object>(_imageObjects);
            GetImagesCommand = new ActionCommand(async () => {
                FetchButtonVisibility = Visibility.Collapsed;
                PregressBarVisibility = Visibility.Visible;
                var links = await SelectSiteAsync(hentai);
                _images = new ObservableCollection<object>(links);
                Images = new ReadOnlyObservableCollection<object>(_images);
                PagesList = new ReadOnlyObservableCollection<int>(_pagesList);
                PageIntList();
                PregressBarVisibility = Visibility.Collapsed;
                foreach (var link in links) {
                    if (_isClosing || _imageObjects.Count == 9) break;
                    _imageObjects.Add(new ImageModel {
                        PageNumber = links.IndexOf(link) + 1,
                        Source = link,
                        IsGif = link.ToString().Contains(".gif")
                    });
                    await Task.Delay(200);
                    Loaded++;
                }
                SaveEnabled = saveEnabled;
            });
            SaveImagesCommand = new ActionCommand(() => {
                if (SaveProgress == Visibility.Collapsed) SaveImagesAsync();
            });
            Setting = SettingsController.Settings;
            if (Setting.Other.InstantFetch) GetImagesCommand.Execute(null);

        }

        public int SelectedPage { get; set; }

        public int TransIndex { get; set; } = 1;

        public string Mode { get; set; } = "Singe Page";

        private void ChangeMode()
        {
            if (TransIndex == 1)
            {
                TransIndex = 0;
                Mode = "Singe Page";
                return;
            }
            TransIndex = 1;
            Mode = "Long Strip";
        }

        public ReadOnlyObservableCollection<int> PagesList { get; set; }
        private readonly ObservableCollection<int> _pagesList = new ObservableCollection<int>();
        private void PageIntList()
        {
            for (var i = 0; i < _images.Count; i++)
            {
                _pagesList.Add(i);
            }
        }

        public SettingsModel Setting { get; set; }

        public HentaiModel Hentai { get; set; }

        public int Loaded { get; private set; }
        public bool SaveEnabled { get; set; }

        public ReadOnlyObservableCollection<object> ImageObjects { get; }
        public ReadOnlyObservableCollection<object> Images { get; set; }

        public string Pages { get; set; } = "0 : 0";

        public ICommand GetImagesCommand { get; }
        public ICommand SaveImagesCommand { get; }
        public ICommand ChangeModeCommand { get; }

        public Visibility FetchButtonVisibility { get; set; } = Visibility.Visible;

        public Visibility PregressBarVisibility { get; set; } = Visibility.Collapsed;

        public Visibility SaveProgress { get; set; } = Visibility.Collapsed;

        public int ProgressValue { get; set; }

        public ICommand CopyLinkCommand { get; }

        public int JumpTonumber { get; set; }

        public ICommand JumpCommand { get; }

        private async void Jump() {
            _imageObjects?.Clear();
            Loaded = JumpTonumber;
            await LoadMoreImagesAsync();
        }

        public async void Dispose() {
            _isClosing = true;
            await Task.Delay(100);
            _imageObjects.Clear();
            await Task.Delay(100);
            GC.Collect();
        }

        private async Task<List<object>> SelectSiteAsync(HentaiModel hentai) {
            Tuple<List<object>, int> tpl;
            switch (hentai.Site) {
                case "Hentai.cafe":
                    tpl = await HentaiCafe.CollectImagesTaskAsync(hentai, SetPages);
                    Pages = $"{tpl.Item1.Count} : {tpl.Item2}";
                    return tpl.Item1;
                case "nHentai.net":
                    tpl = await nHentai.CollectImagesTaskAsync(hentai, SetPages);
                    Pages = $"{tpl.Item1.Count} : {tpl.Item2}";
                    return tpl.Item1;
                case "ExHentai.org":
                    tpl = await ExHentai.CollectImagesTaskAsync(hentai, SetPages);
                    Pages = $"{tpl.Item1.Count} : {tpl.Item2}";
                    return tpl.Item1;
                case "Pururin.us":
                    tpl = await Pururin.CollectImagesTaskAsync(hentai, SetPages);
                    Pages = $"{tpl.Item1.Count} : {tpl.Item2}";
                    return tpl.Item1;
                case "Imgur.com":
                    tpl = await Sites.Imgur.CollectImagesTaskAsync(hentai, SetPages);
                    Pages = $"{tpl.Item1.Count} : {tpl.Item2}";
                    return tpl.Item1;
            }
            return null;
        }

        private void SetPages(int current, int max) {
            Pages = $"{current} : {max}";
        }

        private async void SaveImagesAsync() {
            if (Hentai.Title == "lul" || !SaveEnabled) return;
            SaveEnabled = false;
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "Saves", Hentai.Site,
                MD5Converter.MD5Hash(Hentai.Title));

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            SaveProgress = Visibility.Visible;
            for (var i = 0; i < _images.Count; i++) {
                if (_isClosing) break;
                var img = _images[i];
                ProgressValue = i + 1;
                await SaveImage(img.ToString(), i + 1, folder);
                await Task.Delay(50);
                //if (img is string) {
                //    var client = new RestClient {BaseUrl = new Uri((string) img)};
                //    var imgBytes = await client.ExecuteGetTaskAsync(new RestRequest());
                //    img = ExHentai.BytesToBitmapImage(imgBytes.RawBytes);
                //}
                //var encoder = new PngBitmapEncoder();
                //encoder.Frames.Add(BitmapFrame.Create((BitmapSource) img));
                //using (var stream = new FileStream($"{Path.Combine(folder, $"{i + 1}.png")}", FileMode.Create)) {
                //    encoder.Save(stream);
                //}
            }
            var output = JsonConvert.SerializeObject(new InfoModel(Hentai, _images.Count), Formatting.Indented);

            File.WriteAllText(Path.Combine(folder, "INFO.json"), output);
            SaveProgress = Visibility.Collapsed;
            SaveEnabled = true;
        }

        public async Task LoadMoreImagesAsync() {
            if (_adding || _images == null || _images.Count <= 9) return;
            _adding = true;
            if (_imageObjects.Count > 50) {
                _imageObjects.Clear();
                GC.Collect();
                GC.Collect();
                //_imageObjects.Add(new ImageModel { PageNumber = _loaded, Source = _images[_loaded-3] });
                //await Task.Delay(100);
                //_imageObjects.Add(new ImageModel { PageNumber = _loaded, Source = _images[_loaded-2] });
                //await Task.Delay(100);
                _imageObjects.Add(new ImageModel {
                    PageNumber = Loaded - 1,
                    Source = _images[Loaded - 1],
                    IsGif = _images[Loaded - 1].ToString().Contains(".gif")
                });
                await Task.Delay(100);
            }
            for (var i = 0; i < _images.Count; i++) {
                if (Loaded == _images.Count || i == 9) break;
                _imageObjects.Add(new ImageModel {
                    PageNumber = Loaded,
                    Source = _images[Loaded],
                    IsGif = _images[Loaded].ToString().Contains(".gif")
                });
                Loaded++;
                await Task.Delay(100);
            }
            _adding = false;
        }

        private async Task SaveImage(string url, int num, string folder) {
            try {
                var lastSlash = url.LastIndexOf('/');
                var guid = url.Substring(lastSlash + 1);
                var client = new WebClient();
                var extension = guid.Substring(guid.LastIndexOf(".", StringComparison.Ordinal) + 1);
                await client.DownloadFileTaskAsync(url, Path.Combine(folder, $"{num}.{extension}"));
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        //			await Task.Delay(100);
        //			_loaded++;
        //			currentImages[i] = _loaded;
        //			_imageObjects.Add(new ImageModel { PageNumber = _loaded, Source = _images[_loaded] });
        //			if (_loaded == _images.Count || i == 9 || currentImages.Contains(_loaded)) break;
        //		for (var i = 0; i < _images.Count; i++) {
        //		}
        //			await Task.Delay(100);
        //			_imageObjects.Add(new ImageModel { PageNumber = _loaded-1, Source = _images[_loaded - 1] });
        //			//await Task.Delay(100);
        //			//_imageObjects.Add(new ImageModel { PageNumber = _loaded, Source = _images[_loaded - 2] });
        //			//await Task.Delay(100);
        //			//_imageObjects.Add(new ImageModel { PageNumber = _loaded, Source = _images[_loaded - 3] });
        //			_imageObjects.Clear();
        //		if (_imageObjects.Count >= 9) {
        //	if (!reverse) {
        //	_adding = true;
        //	if (_adding || _images == null) return;

        //public async Task ImageLoader(bool reverse = false, HentaiViewerWindow win=null) {
        //	}
        //	else {

        //		var endpoint = _loaded - 9;

        //		}
        //		var i = _loaded - 18;
        //		if (_imageObjects.Count >= 9) {
        //			_imageObjects.Clear();
        //			//_imageObjects.Add(new ImageModel { PageNumber = _loaded, Source = _images[_loaded - 3] });
        //			//await Task.Delay(100);
        //			//_imageObjects.Add(new ImageModel { PageNumber = _loaded, Source = _images[_loaded - 2] });
        //			//await Task.Delay(100);
        //			_imageObjects.Insert(0, new ImageModel { PageNumber = endpoint+1, Source = _images[endpoint+1] });
        //			await Task.Delay(100);
        //		}
        //		for (var startpoint = endpoint; startpoint > i; startpoint--) {
        //			if (startpoint == -1|| endpoint<=0) break;
        //			_imageObjects.Insert(0,new ImageModel { PageNumber = startpoint, Source = _images[startpoint] });
        //			//_imageObjects.RemoveAt(_imageObjects.Count-1);
        //			//currentImages[i] = ll;
        //			await Task.Delay(50);
        //		}
        //		if (endpoint>=9) {
        //			_loaded = endpoint;
        //		}
        //		//if (_imageObjects.Count >9 || _loaded >9) {

        //		//	for (var j = _imageObjects.Count; j > 9; j--) {
        //		//		if (_imageObjects.Count <=9) {
        //		//			break;
        //		//		}
        //		//		_imageObjects.RemoveAt(j-1);
        //		//	}
        //		//}
        //	}
        //	_adding = false;
        //}
    }
}