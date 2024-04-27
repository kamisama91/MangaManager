using NStack;
using System;
using Terminal.Gui;

namespace MangaManager.View
{
    public class ScrollableMultiColorTextView : Terminal.Gui.View
    {
        private ustring _text = string.Empty;
        private MultiColorTextView _textView ;
        private ScrollBarView _scrollbar;

        public bool Multiline
        {
            get => _textView.Multiline;
            set => _textView.Multiline = value;
        }
        public bool ScrollBarVisible
        {
            get => _scrollbar.Visible;
            set => _scrollbar.Visible = value;
        }
        public override ustring Text
        {
            get => _textView.Text;
            set
            {
                if (_textView != null) 
                    _textView.Text = value;
            }
        }
        public ustring UncoloredText => _textView.UncoloredText;

        public ScrollableMultiColorTextView()
        {
            _textView = new MultiColorTextView
            {
                X = 0,
                Y = 0,
                Height = Dim.Fill(),
                Width = Dim.Fill()
            };
            _textView.DrawContent += OnTextViewDrawContent;
            Add(_textView);

            _scrollbar = new ScrollBarView(_textView, true);
            _scrollbar.VisibleChanged += OnScrollbarVisibleChanged; 
            _scrollbar.ChangedPosition += OnScrollbarChangedPosition;

            //ReadOnly = true;
            Multiline = false;
            ScrollBarVisible = false;
        }

        private void OnTextViewDrawContent(Rect obj)
        {
            _scrollbar.Size = _textView.Lines;
            _scrollbar.Position = _textView.TopRow;
            _scrollbar.LayoutSubviews();
            _scrollbar.Refresh();
        }
        private void OnScrollbarVisibleChanged()
        {
            _textView.RightOffset = _scrollbar.Visible
                ? 1
                : 0;
        }
        private void OnScrollbarChangedPosition()
        {
            _textView.TopRow = _scrollbar.Position;
            if (_textView.TopRow != _scrollbar.Position)
            {
                _scrollbar.Position = _textView.TopRow;
            }
            SetNeedsDisplay();
        }

        public void AddLine(string message)
        {
            Application.MainLoop.Invoke(() =>
            {
                Text += $"{message}{Environment.NewLine}";
            });
        }
    }
}
