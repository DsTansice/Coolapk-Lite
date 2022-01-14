﻿using CoolapkLite.Helpers.Providers;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace CoolapkLite.Controls
{
    [TemplatePart(Name = "Header", Type = typeof(Grid))]
    [TemplatePart(Name = "PivotHeader", Type = typeof(PivotHeader))]
    public sealed class ShyHeaderPivot : ContentControl
    {
        private Pivot _pivot;
        private Grid _header;
        private PivotHeader _pivotHeader;

        private HashSet<ScrollViewer> ScrollViewers;
        private ScrollProgressProvider Provider;
        private SpinLock SpinLock = new SpinLock();
        private CancellationTokenSource Token;

        public ShyHeaderPivot()
        {
            this.DefaultStyleKey = typeof(ShyHeaderPivot);
        }

        protected override void OnApplyTemplate()
        {
            if (_pivot != null)
            {
                _pivot.PivotItemLoaded -= Pivot_PivotItemLoaded;
                _pivot.SelectionChanged -= Pivot_SelectionChanged;
                _pivot.PivotItemUnloading -= Pivot_PivotItemUnloading;
            }
            if (_header != null)
            {
                _header.SizeChanged -= Header_SizeChanged;
            }
            if (_pivotHeader != null)
            {
                _pivotHeader.Loaded -= PivotHeader_Loaded;
            }

            Provider = new ScrollProgressProvider();
            ScrollViewers = new HashSet<ScrollViewer>();
            Provider.ProgressChanged += Provider_ProgressChanged;
            _header = (Grid)GetTemplateChild("Header");
            _pivotHeader = (PivotHeader)GetTemplateChild("PivotHeader");
            _pivot = (Content as FrameworkElement).FindDescendant<Pivot>();

            if (_pivot != null)
            {
                _pivot.PivotItemLoaded += Pivot_PivotItemLoaded;
                _pivot.SelectionChanged += Pivot_SelectionChanged;
                _pivot.PivotItemUnloading += Pivot_PivotItemUnloading;
            }
            if (_header != null)
            {
                _header.SizeChanged += Header_SizeChanged;
            }
            if (_pivotHeader != null)
            {
                _pivotHeader.Loaded += PivotHeader_Loaded;
            }

            base.OnApplyTemplate();
        }

        private async void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PivotItem PivotItem = _pivot.ContainerFromItem(_pivot.SelectedItem) as PivotItem;

            if (Token != null) Token.Cancel();
            Token = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            FrameworkElement ContentTemplateRoot = await WaitForLoaded(PivotItem, () => PivotItem.ContentTemplateRoot as FrameworkElement, c => c != null, Token.Token);

            Provider.ScrollViewer = ContentTemplateRoot.FindDescendant<ScrollViewer>();

            bool lockTaken = false;
            try
            {
                SpinLock.Enter(ref lockTaken);
                ScrollViewers.Remove(Provider.ScrollViewer);
            }
            finally
            {
                if (lockTaken)
                { SpinLock.Exit(); }
            }
        }

        private void Provider_ProgressChanged(object sender, double args)
        {
            _header.Translation = _pivotHeader.Translation = new Vector3(0f, (float)(-Provider.Threshold * Provider.Progress), 0f);
            bool lockTaken = false;
            try
            {
                SpinLock.Enter(ref lockTaken);
                foreach (ScrollViewer sv in ScrollViewers)
                {
                    sv.ChangeView(null, Provider.Progress * Provider.Threshold, null, true);
                }
            }
            finally
            {
                if (lockTaken)
                    SpinLock.Exit();
            }
        }

        private void Pivot_PivotItemLoaded(Pivot sender, PivotItemEventArgs args)
        {
            ScrollViewer ScrollViewer = (args.Item.ContentTemplateRoot as FrameworkElement).FindDescendant<ScrollViewer>();
            FrameworkElement FrameworkElement = ScrollViewer.FindDescendant<ScrollContentPresenter>().FindChild<FrameworkElement>();
            if (FrameworkElement != null)
            {
                FrameworkElement.Margin = new Thickness(0, _header.ActualHeight + _pivotHeader.ActualHeight, 0, 0);
            }
            if (ScrollViewer != Provider.ScrollViewer)
            {
                ScrollViewer.ChangeView(null, Provider.Progress * Provider.Threshold, null, true);

                var lockTaken = false;
                try
                {
                    SpinLock.Enter(ref lockTaken);
                    ScrollViewers.Add(ScrollViewer);
                }
                finally
                {
                    if (lockTaken)
                        SpinLock.Exit();
                }
            }
        }

        private void Pivot_PivotItemUnloading(Pivot sender, PivotItemEventArgs args)
        {
            var sv = (args.Item.ContentTemplateRoot as FrameworkElement).FindDescendant<ScrollViewer>();
            if (sv != null)
            {
                var lockTaken = false;
                try
                {
                    SpinLock.Enter(ref lockTaken);
                    ScrollViewers.Remove(sv);
                }
                finally
                {
                    if (lockTaken)
                        SpinLock.Exit();
                }
            }
        }

        private void PivotHeader_Loaded(object sender, RoutedEventArgs e)
        {
            Provider.Threshold = _header.ActualHeight;
        }

        private void Header_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _pivotHeader.Margin = new Thickness(0, e.NewSize.Height, 0, 0);
            bool lockTaken = false;
            try
            {
                SpinLock.Enter(ref lockTaken);
                foreach (ScrollViewer ScrollViewer in ScrollViewers)
                {
                    FrameworkElement FrameworkElement = ScrollViewer.FindDescendant<ScrollContentPresenter>().FindChild<FrameworkElement>();
                    if (FrameworkElement != null)
                    {
                        FrameworkElement.Margin = new Thickness(0, _header.ActualHeight + _pivotHeader.ActualHeight, 0, 0);
                    }
                }
            }
            finally
            {
                if (lockTaken)
                    SpinLock.Exit();
            }
        }

        private async Task<T> WaitForLoaded<T>(FrameworkElement element, Func<T> func, Predicate<T> pre, CancellationToken cancellationToken)
        {
            TaskCompletionSource<T> tcs = null;
            try
            {
                tcs = new TaskCompletionSource<T>();
                cancellationToken.ThrowIfCancellationRequested();
                var result = func.Invoke();
                if (pre(result)) return result;


                element.Loaded += Element_Loaded;

                return await tcs.Task;

            }
            catch
            {
                element.Loaded -= Element_Loaded;
                var result = func.Invoke();
                if (pre(result)) return result;
            }

            return default;


            void Element_Loaded(object sender, RoutedEventArgs e)
            {
                if (tcs == null) return;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    element.Loaded -= Element_Loaded;
                    var _result = func.Invoke();
                    if (pre(_result)) tcs.SetResult(_result);
                    else tcs.SetCanceled();
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("canceled");
                }
            }

        }
    }
}
