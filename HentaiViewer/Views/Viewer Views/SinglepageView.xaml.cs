﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HentaiViewer.Views.Viewer_Views
{
    /// <summary>
    /// Interaktionslogik für SinglepageView.xaml
    /// </summary>
    public partial class SinglepageView : UserControl
    {
        public SinglepageView()
        {
            InitializeComponent();
        }
        private void Flip_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D:
                case Key.Space:
                    Flip.GoForward();
                    break;
                case Key.A:
                case Key.Back:
                    Flip.GoBack();
                    break;

            }
        }
    }
}
