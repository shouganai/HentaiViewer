﻿using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using HentaiViewer.Common;
using HentaiViewer.Models;
using HentaiViewer.Sites;
using HentaiViewer.Views;
using PropertyChanged;

namespace HentaiViewer.ViewModels {
    [ImplementPropertyChanged]
    public class ExHentaiViewModel {
        public static ExHentaiViewModel Instance;

        private readonly ObservableCollection<HentaiModel> _exHentai = new ObservableCollection<HentaiModel>();

        public ExHentaiViewModel() {
            Instance = this;
            ExHentaiItems = new ReadOnlyObservableCollection<HentaiModel>(_exHentai);

            //RefreshExHentaiAsync();
            RefreshExHentaiCommand = new ActionCommand(RefreshExHentaiAsync);
            LoadMoreExHentaiCommand = new ActionCommand(async () => { await LoadExHentaiPageAsync(1); });
            LoadPrevExHentaiCommand = new ActionCommand(async () => {
                if (ExHentaiLoadedPage == 0) return;
                await LoadExHentaiPageAsync(-1);
            });
            HomeCommand = new ActionCommand(async () => {
                if (ExHentaiPageLoading) return;
                ExHentaiLoadedPage = 0;
                NextExHentaiPage = 1;
                await LoadExHentaiPageAsync(0);
            });
            Setting = SettingsController.Settings;
        }

        public SettingsModel Setting { get; set; }

        public int ExHentaiLoadedPage { get; set; }
        public int NextExHentaiPage { get; set; } = 1;
        public bool ExHentaiPageLoading { get; set; }
        public ReadOnlyObservableCollection<HentaiModel> ExHentaiItems { get; }

        public ICommand RefreshExHentaiCommand { get; }
        public ICommand LoadMoreExHentaiCommand { get; }
        public ICommand LoadPrevExHentaiCommand { get; }

        public ICommand HomeCommand { get; }

        private async void RefreshExHentaiAsync() {
            await LoadExHentaiPageAsync(0);
        }

        private async Task LoadExHentaiPageAsync(int value) {
            SettingsController.Save();
            var pass = SettingsController.Settings.ExHentai.IpbPassHash;
            var memid = SettingsController.Settings.ExHentai.IpbMemberId;
            var igneous = SettingsController.Settings.ExHentai.Igneous;
            if (string.IsNullOrEmpty(memid) || string.IsNullOrEmpty(igneous) || string.IsNullOrEmpty(pass)) {
                MessageBox.Show("Need Cookies for Exhentai", "Cookies missing", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            if (ExHentaiPageLoading) return;
            ExHentaiPageLoading = true;
            NextExHentaiPage = NextExHentaiPage + value;
            ExHentaiLoadedPage = ExHentaiLoadedPage + value;
            if (_exHentai.Count > 0) _exHentai.Clear();
            ExHentaiView.Instance.ScrollViewer.ScrollToTop();
            var searchQuery = string.Empty;
            if (!string.IsNullOrEmpty(SettingsController.Settings.ExHentai.SearchQuery))
                searchQuery = SettingsController.Settings.ExHentai.SearchQuery.Replace(" ", "" + "+");
            var i = await ExHentai.GetLatestAsync($"https://exhentai.org/?page={ExHentaiLoadedPage}" +
                                                  $"&f_doujinshi={SettingsController.Settings.ExHentai.Doujinshi}" +
                                                  $"&f_manga={SettingsController.Settings.ExHentai.Manga}" +
                                                  $"&f_artistcg={SettingsController.Settings.ExHentai.ArtistCg}" +
                                                  $"&f_gamecg={SettingsController.Settings.ExHentai.GameCg}" +
                                                  $"&f_western={SettingsController.Settings.ExHentai.Western}" +
                                                  $"&f_non-h={SettingsController.Settings.ExHentai.NonH}" +
                                                  $"&f_imageset={SettingsController.Settings.ExHentai.ImageSet}" +
                                                  $"&f_cosplay={SettingsController.Settings.ExHentai.Cosplay}" +
                                                  $"&f_asianporn={SettingsController.Settings.ExHentai.AsianPorn}" +
                                                  $"&f_misc={SettingsController.Settings.ExHentai.Misc}" +
                                                  $"&f_search={searchQuery}&f_apply=Apply+Filter" +
                                                  $"&advsearch=1&f_sname=on&f_stags=on&f_sr=on" +
                                                  $"&f_srdd={SettingsController.Settings.ExHentai.MinRating}");
            Console.WriteLine(SettingsController.Settings.ExHentai.MinRating);
            foreach (var hentaiModel in i) {
                if (FavoritesController.FavoriteMd5s.Contains(hentaiModel.Md5)) hentaiModel.Favorite = true;
                _exHentai.Add(hentaiModel);
                await Task.Delay(10);
            }
            ExHentaiPageLoading = false;
        }
    }
}