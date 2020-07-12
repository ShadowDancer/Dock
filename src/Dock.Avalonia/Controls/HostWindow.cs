﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Dock.Model;

namespace Dock.Avalonia.Controls
{
    /// <summary>
    /// Interaction logic for <see cref="HostWindow"/> xaml.
    /// </summary>
    public class HostWindow : Window, IStyleable, IHostWindow
    {
        internal static bool s_useCustomDrag = true;

        /// <summary>
        /// Defines the <see cref="IsChromeVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsChromeVisibleProperty =
            AvaloniaProperty.Register<HostWindow, bool>(nameof(IsChromeVisible), true);

        /// <summary>
        /// Defines the <see cref="TitleBarContent"/> property.
        /// </summary>
        public static readonly StyledProperty<Control> TitleBarContentProperty =
            AvaloniaProperty.Register<HostWindow, Control>(nameof(TitleBarContent));

        /// <summary>
        ///  Gets or sets the flag indicating whether chrome is visible.
        /// </summary>
        public bool IsChromeVisible
        {
            get => GetValue(IsChromeVisibleProperty);
            set => SetValue(IsChromeVisibleProperty, value);
        }

        /// <summary>
        ///  Gets or sets the title bar content control.
        /// </summary>
        public Control TitleBarContent
        {
            get => GetValue(TitleBarContentProperty);
            set => SetValue(TitleBarContentProperty, value);
        }

        Type IStyleable.StyleKey => typeof(HostWindow);

        private HostWindowState _hostWindowState;

        private Control? _titleBar;
        private Grid? _bottomHorizontalGrip;
        private Grid? _bottomLeftGrip;
        private Grid? _bottomRightGrip;
        private Grid? _leftVerticalGrip;
        private Grid? _rightVerticalGrip;
        private Grid? _topHorizontalGrip;
        private Grid? _topLeftGrip;
        private Grid? _topRightGrip;
        private Button? _closeButton;
        private Button? _minimiseButton;
        private Button? _restoreButton;
        private Image? _icon;
        private bool _mouseDown;
        private Point _startPoint;

        /// <inheritdoc/>
        public bool IsTracked { get; set; }

        /// <inheritdoc/>
        public IDockWindow? Window { get; set; }

        /// <summary>
        /// Initializes new instance of the <see cref="HostWindow"/> class.
        /// </summary>
        public HostWindow()
        {
            this.AttachDevTools();

            AddHandler(InputElement.PointerPressedEvent, Pressed, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            AddHandler(InputElement.PointerReleasedEvent, Released, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            AddHandler(InputElement.PointerMovedEvent, Moved, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            AddHandler(InputElement.PointerCaptureLostEvent, CaptureLost, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

            PositionChanged += HostWindow_PositionChanged;
            LayoutUpdated += HostWindow_LayoutUpdated;
            Closing += HostWindow_Closing;

            _hostWindowState = new HostWindowState(this);
        }

        private void HostWindow_PositionChanged(object sender, PixelPointEventArgs e)
        {
            if (Window != null && IsTracked == true)
            {
                Window.Save();
            }

            _hostWindowState.Process(this.Position.ToPoint(1.0), EventType.Moved);
        }

        private void HostWindow_LayoutUpdated(object sender, EventArgs e)
        {
            if (Window != null && IsTracked == true)
            {
                Window.Save();
            }
        }

        private void HostWindow_Closing(object sender, CancelEventArgs e)
        {
            if (Window != null && IsTracked == true)
            {
                Window.Save();

                if (Window.Layout is IDock root)
                {
                    root.Close();
                }
            }
        }

        private void Pressed(object sender, PointerPressedEventArgs e)
        {
            _hostWindowState.Process(e.GetPosition(this), EventType.Pressed);
        }

        private void Released(object sender, PointerReleasedEventArgs e)
        {
            _hostWindowState.Process(e.GetPosition(this), EventType.Released);
        }

        private void Moved(object sender, PointerEventArgs e)
        {
            // Using PositionChanged event instead of PointerMoved event.
        }

        private void CaptureLost(object sender, PointerCaptureLostEventArgs e)
        {
            _hostWindowState.Process(new Point(), EventType.CaptureLost);
        }

        /// <summary>
        /// Attaches grip to chrome.
        /// </summary>
        /// <param name="chrome">The chrome control.</param>
        public void AttachGrip(DockToolChrome chrome)
        {
            Observable.FromEventPattern(chrome.CloseButton, nameof(Button.Click)).Subscribe(o =>
            {
                Close();
            });

            //Observable.FromEventPattern(chrome.MinimiseButton, nameof(Button.Click)).Subscribe(o =>
            //{
            //    WindowState = WindowState.Minimized;
            //});

            _titleBar = chrome.Grip;

            ((IPseudoClasses)chrome.Classes).Add(":floating");
            this.PseudoClasses.Set(":toolwindow", true);
        }

        /// <summary>
        /// Toggles window state.
        /// </summary>
        private void ToggleWindowState()
        {
            switch (WindowState)
            {
                case WindowState.Maximized:
                    WindowState = WindowState.Normal;
                    break;
                case WindowState.Normal:
                    WindowState = WindowState.Maximized;
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (_topHorizontalGrip != null && _topHorizontalGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.North, e);
            }
            else if (_bottomHorizontalGrip != null && _bottomHorizontalGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.South, e);
            }
            else if (_leftVerticalGrip != null && _leftVerticalGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.West, e);
            }
            else if (_rightVerticalGrip != null && _rightVerticalGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.East, e);
            }
            else if (_topLeftGrip != null && _topLeftGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.NorthWest, e);
            }
            else if (_bottomLeftGrip != null && _bottomLeftGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.SouthWest, e);
            }
            else if (_topRightGrip != null && _topRightGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.NorthEast, e);
            }
            else if (_bottomRightGrip != null && _bottomRightGrip.IsPointerOver)
            {
                BeginResizeDrag(WindowEdge.SouthEast, e);
            }
            else if (_titleBar != null && _titleBar.IsPointerOver)
            {
                _mouseDown = true;
                _startPoint = e.GetPosition(this);

                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && !s_useCustomDrag)
                {
                    BeginMoveDrag(e);
                    _mouseDown = false;
                }
            }
            else
            {
                _mouseDown = false;
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            _mouseDown = false;
        }

        /// <inheritdoc/>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (_titleBar != null && _titleBar.IsPointerOver && _mouseDown)
            {
                if (s_useCustomDrag)
                {
                    // Using custom method because BeginMoveDrag is releasing pointer capture on Windows.
                    var point = e.GetPosition(this);
                    var delta = point - _startPoint;
                    double x = this.Position.X + delta.X;
                    double y = this.Position.Y + delta.Y;
                    Position = this.Position.WithX((int)x).WithY((int)y);
                    _startPoint = new Point(point.X - delta.X, point.Y - delta.Y);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            _titleBar = e.NameScope.Find<Control>("PART_TitleBar");
            _minimiseButton = e.NameScope.Find<Button>("PART_MinimiseButton");
            _restoreButton = e.NameScope.Find<Button>("PART_RestoreButton");
            _closeButton = e.NameScope.Find<Button>("PART_CloseButton");
            _icon = e.NameScope.Find<Image>("PART_Icon");

            _topHorizontalGrip = e.NameScope.Find<Grid>("PART_TopHorizontalGrip");
            _bottomHorizontalGrip = e.NameScope.Find<Grid>("PART_BottomHorizontalGrip");
            _leftVerticalGrip = e.NameScope.Find<Grid>("PART_LeftVerticalGrip");
            _rightVerticalGrip = e.NameScope.Find<Grid>("PART_RightVerticalGrip");

            _topLeftGrip = e.NameScope.Find<Grid>("PART_TopLeftGrip");
            _bottomLeftGrip = e.NameScope.Find<Grid>("PART_BottomLeftGrip");
            _topRightGrip = e.NameScope.Find<Grid>("PART_TopRightGrip");
            _bottomRightGrip = e.NameScope.Find<Grid>("PART_BottomRightGrip");

            if (_minimiseButton != null)
            {
                _minimiseButton.Click += (sender, ee) => { WindowState = WindowState.Minimized; };
            }

            if (_restoreButton != null)
            {
                _restoreButton.Click += (sender, ee) => { ToggleWindowState(); };
            }

            if (_titleBar != null)
            {
                _titleBar.DoubleTapped += (sender, ee) => { ToggleWindowState(); };
            }

            if (_closeButton != null)
            {
                _closeButton.Click += (sender, ee) => { Close(); };
            }

            //if (_icon != null)
            //{
            //    _icon.DoubleTapped += (sender, ee) => { Close(); };
            //}
        }

        /// <inheritdoc/>
        public void Present(bool isDialog)
        {
            if (isDialog)
            {
                if (!this.IsVisible)
                {
                    this.ShowDialog(null); // FIXME: Set correct parent window.
                }
            }
            else
            {
                if (!this.IsVisible)
                {
                    this.Show();
                }
            }
        }

        /// <inheritdoc/>
        public void Exit()
        {
            this.Close();
        }

        /// <inheritdoc/>
        public void SetPosition(double x, double y)
        {
            if (!double.IsNaN(x) && !double.IsNaN(y))
            {
                Position = new PixelPoint((int)x, (int)y);
            }
        }

        /// <inheritdoc/>
        public void GetPosition(out double x, out double y)
        {
            x = this.Position.X;
            y = this.Position.Y;
        }

        /// <inheritdoc/>
        public void SetSize(double width, double height)
        {
            if (!double.IsNaN(width))
            {
                this.Width = width;
            }

            if (!double.IsNaN(height))
            {
                this.Height = height;
            }
        }

        /// <inheritdoc/>
        public void GetSize(out double width, out double height)
        {
            width = this.Width;
            height = this.Height;
        }

        /// <inheritdoc/>
        public void SetTopmost(bool topmost)
        {
            this.Topmost = topmost;
        }

        /// <inheritdoc/>
        public void SetTitle(string title)
        {
            this.Title = title;
        }

        /// <inheritdoc/>
        public void SetLayout(IDock layout)
        {
            this.DataContext = layout;
        }
    }
}
