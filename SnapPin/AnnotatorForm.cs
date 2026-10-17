using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnapPin
{
    internal enum AnnotationTool
    {
        Select,
        Pen,
        Arrow,
        Rectangle,
        Mosaic,
        Text
    }

    internal enum AnnotationKind
    {
        Pen,
        Arrow,
        Rectangle,
        Mosaic,
        Text
    }

    internal enum AnnotatorResult
    {
        Pin,
        Copy,
        Save,
        Cancel
    }

    internal sealed class Annotation
    {
        public AnnotationKind Kind;
        public Color Color;
        public float StrokeWidth;
        public float FontSize;
        public string Text;
        public List<Point> Points = new List<Point>();

        public Annotation Clone()
        {
            var clone = new Annotation
            {
                Kind = Kind,
                Color = Color,
                StrokeWidth = StrokeWidth,
                FontSize = FontSize,
                Text = Text
            };
            clone.Points.AddRange(Points);
            return clone;
        }
    }

    internal sealed class AnnotatorForm : Form
    {
        private const int UndoLimit = 30;
        private const int MosaicCellSize = 10;
        private const int HandleSize = 8;

        private readonly Bitmap _baseImage;
        private readonly List<Annotation> _annotations = new List<Annotation>();
        private readonly Stack<List<Annotation>> _undo = new Stack<List<Annotation>>();
        private readonly ToolStrip _toolbar = new ToolStrip();
        private readonly Dictionary<AnnotationTool, ToolStripButton> _toolButtons =
            new Dictionary<AnnotationTool, ToolStripButton>();
        private readonly Dictionary<Color, ToolStripButton> _colorButtons =
            new Dictionary<Color, ToolStripButton>();
        private readonly List<KeyValuePair<float, ToolStripMenuItem>> _strokeItems =
            new List<KeyValuePair<float, ToolStripMenuItem>>();
        private readonly List<KeyValuePair<float, ToolStripMenuItem>> _fontItems =
            new List<KeyValuePair<float, ToolStripMenuItem>>();
        private ToolStripDropDownButton _strokeMenu;
        private ToolStripDropDownButton _fontMenu;
        private TransparentTextBox _textEditor;
        private Point _textEditorImagePoint;

        private Bitmap _render;
        private AnnotationTool _tool = AnnotationTool.Pen;
        private Color _color = Color.Red;
        private float _strokeWidth = 4f;
        private float _textSize = 28f;
        private float _scale = 1f;
        private Point _offset;
        private Size _displaySize;
        private int _selected = -1;
        private int _movingIndex = -1;
        private AnnotatorResult _resultAction = AnnotatorResult.Cancel;

        private bool _drawing;
        private Annotation _preview;

        private bool _moving;
        private Point _moveStart;
        private List<Point> _moveOriginalPoints;

        private bool _resizing;
        private int _resizeHandle = -1;
        private Rectangle _resizeOldRect;
        private List<Point> _resizeOriginalPoints;
        private float _resizeOriginalFont;

        private AnnotatorForm(Bitmap source)
        {
            _baseImage = new Bitmap(source);

            Text = "标注 - 贴图 / 复制 / 保存（Esc 取消）";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            KeyPreview = true;
            BackColor = Color.FromArgb(44, 44, 48);
            DoubleBuffered = true;
            MinimumSize = new Size(360, 140);

            BuildToolbar();
            Controls.Add(_toolbar);
            ReRender();
        }

        public static Bitmap Edit(Bitmap source, out AnnotatorResult action)
        {
            using (var form = new AnnotatorForm(source))
            {
                form.ShowDialog();
                action = form._resultAction;
                if (action == AnnotatorResult.Pin || action == AnnotatorResult.Copy)
                {
                    Bitmap result = form._render;
                    form._render = null;
                    return result;
                }
                return null;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ComputeLayout();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_CTLCOLOREDIT = 0x0133;
            const int TRANSPARENT = 1;
            const int NULL_BRUSH = 5;

            if (m.Msg == WM_CTLCOLOREDIT && _textEditor != null && m.LParam == _textEditor.Handle)
            {
                SetBkMode(m.WParam, TRANSPARENT);
                m.Result = GetStockObject(NULL_BRUSH);
                return;
            }

            base.WndProc(ref m);
        }

        [DllImport("gdi32.dll")]
        private static extern int SetBkMode(IntPtr hdc, int mode);

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int fnObject);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.Low;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            if (_render != null)
            {
                g.DrawImage(
                    _render,
                    new Rectangle(_offset.X, _offset.Y, _displaySize.Width, _displaySize.Height));
            }

            if (_movingIndex >= 0 && _movingIndex < _annotations.Count)
            {
                DrawAnnotationScaled(g, _annotations[_movingIndex]);
            }

            DrawPreviewOverlay(g);
            DrawSelection(g);

            if (_textEditor != null)
            {
                using (var pen = new Pen(Color.FromArgb(0, 170, 255), 1f)
                {
                    DashStyle = DashStyle.Dash
                })
                {
                    Rectangle bounds = _textEditor.Bounds;
                    bounds.Inflate(2, 2);
                    g.DrawRectangle(pen, bounds);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (_textEditor != null)
            {
                CommitInlineText();
                return;
            }

            int handle = _selected >= 0
                ? HitHandle(GetBounds(_annotations[_selected]), e.Location)
                : -1;
            if (handle >= 0)
            {
                BeginResize(handle);
                return;
            }

            if (_tool == AnnotationTool.Select)
            {
                Point raw = ClientToImage(e.Location);
                int index = IsInsideImage(raw) ? HitTest(raw) : -1;
                if (index >= 0)
                {
                    _selected = index;
                    BeginMove(raw);
                }
                else
                {
                    _selected = -1;
                }
                Invalidate();
                return;
            }

            Point point = ClientToImage(e.Location);
            if (!IsInsideImage(point))
            {
                return;
            }

            if (_tool == AnnotationTool.Text)
            {
                BeginInlineText(point);
                return;
            }

            BeginDraw(point);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Point point = ClampToImage(ClientToImage(e.Location));
            if (_resizing)
            {
                UpdateResize(point);
            }
            else if (_moving)
            {
                UpdateMove(point);
            }
            else if (_drawing)
            {
                UpdateDraw(point);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (_drawing)
            {
                CommitDraw();
            }
            else if (_moving)
            {
                _moving = false;
                _moveOriginalPoints = null;
                _movingIndex = -1;
                ReRender();
                Invalidate();
            }
            else if (_resizing)
            {
                _resizing = false;
                _resizeOriginalPoints = null;
                _movingIndex = -1;
                ReRender();
                Invalidate();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                Finish(AnnotatorResult.Cancel, DialogResult.Cancel);
            }
            else if (e.Control && e.KeyCode == Keys.Z)
            {
                Undo();
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                Finish(AnnotatorResult.Copy, DialogResult.OK);
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                SaveAndFinish();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                DeleteSelected();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                Finish(AnnotatorResult.Pin, DialogResult.OK);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_textEditor != null)
                {
                    TextBox editor = _textEditor;
                    _textEditor = null;
                    editor.Dispose();
                }
                if (_baseImage != null)
                {
                    _baseImage.Dispose();
                }
                if (_render != null)
                {
                    _render.Dispose();
                }
                _undo.Clear();
            }

            base.Dispose(disposing);
        }

        private void BuildToolbar()
        {
            _toolbar.Dock = DockStyle.Top;
            _toolbar.GripStyle = ToolStripGripStyle.Hidden;
            _toolbar.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            _toolbar.LayoutStyle = ToolStripLayoutStyle.Flow;
            _toolbar.Padding = new Padding(4, 3, 4, 3);
            _toolbar.ImageScalingSize = new Size(16, 16);

            AddToolButton("选择", AnnotationTool.Select, "选择和移动标注");
            AddToolButton("画笔", AnnotationTool.Pen, "画笔");
            AddToolButton("箭头", AnnotationTool.Arrow, "箭头");
            AddToolButton("矩形", AnnotationTool.Rectangle, "矩形");
            AddToolButton("马赛克", AnnotationTool.Mosaic, "马赛克涂抹");
            AddToolButton("文字", AnnotationTool.Text, "文字");
            _toolbar.Items.Add(new ToolStripSeparator());

            AddColorButton(Color.Red, "红色");
            AddColorButton(Color.Yellow, "黄色");
            AddColorButton(Color.Blue, "蓝色");
            AddColorButton(Color.Green, "绿色");
            AddColorButton(Color.White, "白色");
            AddColorButton(Color.Black, "黑色");
            _toolbar.Items.Add(new ToolStripSeparator());

            _strokeMenu = new ToolStripDropDownButton
            {
                Image = CreateStrokeSizeIcon(),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "线宽"
            };
            AddStrokeItem("细", 2f);
            AddStrokeItem("中", 4f);
            AddStrokeItem("粗", 8f);
            _toolbar.Items.Add(_strokeMenu);

            _fontMenu = new ToolStripDropDownButton
            {
                Image = CreateTextSizeIcon(),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = "字号"
            };
            AddFontItem("小", 16f);
            AddFontItem("中", 28f);
            AddFontItem("大", 44f);
            _toolbar.Items.Add(_fontMenu);
            _toolbar.Items.Add(new ToolStripSeparator());

            var undo = AddTextButton("撤销", "撤销 (Ctrl+Z)");
            undo.Click += (s, e) => Undo();
            _toolbar.Items.Add(new ToolStripSeparator());

            var pinButton = AddTextButton("贴图", "贴到屏幕最前并复制 (Enter)");
            pinButton.Click += (s, e) => Finish(AnnotatorResult.Pin, DialogResult.OK);
            var copyButton = AddTextButton("复制", "仅复制到剪贴板 (Ctrl+C)");
            copyButton.Click += (s, e) => Finish(AnnotatorResult.Copy, DialogResult.OK);
            var saveButton = AddTextButton("保存", "保存为图片 (Ctrl+S)");
            saveButton.Click += (s, e) => SaveAndFinish();
            var cancelButton = AddTextButton("取消", "放弃截图 (Esc)");
            cancelButton.Click += (s, e) => Finish(AnnotatorResult.Cancel, DialogResult.Cancel);

            SelectTool(AnnotationTool.Pen);
            SelectColor(Color.Red);
            SelectStroke(4f);
            SelectFont(28f);
        }

        private void AddToolButton(string text, AnnotationTool tool, string tip)
        {
            var button = new ToolStripButton(text)
            {
                ForeColor = Color.White,
                ToolTipText = tip,
                Image = CreateToolIcon(tool),
                DisplayStyle = ToolStripItemDisplayStyle.Image
            };
            button.CheckOnClick = true;
            button.Click += (s, e) => SelectTool(tool);
            _toolButtons[tool] = button;
            _toolbar.Items.Add(button);
        }

        private static Image CreateToolIcon(AnnotationTool tool)
        {
            var bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (var brush = new SolidBrush(Color.White))
                using (var pen = new Pen(Color.White, 1.5f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    switch (tool)
                    {
                        case AnnotationTool.Select:
                            var cursor = new[]
                            {
                                new Point(2, 2),
                                new Point(2, 12),
                                new Point(5, 9),
                                new Point(8, 14),
                                new Point(10, 12),
                                new Point(7, 8),
                                new Point(13, 7)
                            };
                            g.FillPolygon(brush, cursor);
                            break;

                        case AnnotationTool.Pen:
                            g.DrawLine(pen, 3, 13, 12, 4);
                            g.DrawLine(pen, 12, 4, 14, 2);
                            break;

                        case AnnotationTool.Arrow:
                            g.DrawLine(pen, 2, 8, 12, 8);
                            g.DrawLine(pen, 12, 8, 9, 5);
                            g.DrawLine(pen, 12, 8, 9, 11);
                            break;

                        case AnnotationTool.Rectangle:
                            g.DrawRectangle(pen, 2, 2, 12, 12);
                            break;

                        case AnnotationTool.Mosaic:
                            for (int row = 0; row < 3; row++)
                            {
                                for (int column = 0; column < 3; column++)
                                {
                                    g.FillRectangle(
                                        brush,
                                        2 + column * 5,
                                        2 + row * 5,
                                        3,
                                        3);
                                }
                            }
                            break;

                        case AnnotationTool.Text:
                            using (Font font = new Font("Segoe UI", 10f, FontStyle.Bold, GraphicsUnit.Pixel))
                            {
                                g.DrawString("A", font, brush, 2, 2);
                            }
                            break;
                    }
                }
            }
            return bitmap;
        }

        private void AddColorButton(Color color, string tip)
        {
            var button = new ToolStripButton
            {
                Image = CreateSwatch(color),
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                ToolTipText = tip
            };
            button.CheckOnClick = true;
            button.Click += (s, e) => SelectColor(color);
            _colorButtons[color] = button;
            _toolbar.Items.Add(button);
        }

        private void AddStrokeItem(string label, float width)
        {
            var item = new ToolStripMenuItem(label);
            item.Click += (s, e) => SelectStroke(width);
            _strokeMenu.DropDownItems.Add(item);
            _strokeItems.Add(new KeyValuePair<float, ToolStripMenuItem>(width, item));
        }

        private void AddFontItem(string label, float size)
        {
            var item = new ToolStripMenuItem(label);
            item.Click += (s, e) => SelectFont(size);
            _fontMenu.DropDownItems.Add(item);
            _fontItems.Add(new KeyValuePair<float, ToolStripMenuItem>(size, item));
        }

        private static Image CreateStrokeSizeIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var pen = new Pen(Color.White, 2f)
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round
                })
                {
                    g.DrawLine(pen, 3, 8, 13, 8);
                }
            }
            return bitmap;
        }

        private static Image CreateTextSizeIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(Color.White))
                using (var font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    g.DrawString("A", font, brush, 2, 1);
                }
            }
            return bitmap;
        }

        private ToolStripButton AddTextButton(string text, string tip)
        {
            var button = new ToolStripButton(text)
            {
                ForeColor = Color.White,
                ToolTipText = tip
            };
            _toolbar.Items.Add(button);
            return button;
        }

        private void SelectTool(AnnotationTool tool)
        {
            CommitInlineText();
            _tool = tool;
            foreach (KeyValuePair<AnnotationTool, ToolStripButton> pair in _toolButtons)
            {
                pair.Value.Checked = pair.Key == tool;
            }
            Cursor = tool == AnnotationTool.Select ? Cursors.Default : Cursors.Cross;
        }

        private void SelectColor(Color color)
        {
            _color = color;
            foreach (KeyValuePair<Color, ToolStripButton> pair in _colorButtons)
            {
                pair.Value.Checked = pair.Key.ToArgb() == color.ToArgb();
            }
        }

        private void SelectStroke(float width)
        {
            _strokeWidth = width;
            foreach (KeyValuePair<float, ToolStripMenuItem> pair in _strokeItems)
            {
                pair.Value.Checked = Math.Abs(pair.Key - width) < 0.5f;
            }
        }

        private void SelectFont(float size)
        {
            _textSize = size;
            foreach (KeyValuePair<float, ToolStripMenuItem> pair in _fontItems)
            {
                pair.Value.Checked = Math.Abs(pair.Key - size) < 0.5f;
            }
        }

        private void BeginDraw(Point point)
        {
            PushUndo();
            _drawing = true;
            _preview = new Annotation
            {
                Kind = ToKind(_tool),
                Color = _color,
                StrokeWidth = _strokeWidth,
                FontSize = _textSize
            };
            _preview.Points.Add(point);
            Invalidate();
        }

        private void UpdateDraw(Point point)
        {
            if (_preview.Kind == AnnotationKind.Pen || _preview.Kind == AnnotationKind.Mosaic)
            {
                Point last = _preview.Points[_preview.Points.Count - 1];
                if (Distance(last, point) >= 2)
                {
                    _preview.Points.Add(point);
                }
            }
            else
            {
                if (_preview.Points.Count < 2)
                {
                    _preview.Points.Add(point);
                }
                else
                {
                    _preview.Points[1] = point;
                }
            }
            Invalidate();
        }

        private void CommitDraw()
        {
            _drawing = false;
            if (_preview == null)
            {
                return;
            }

            Annotation annotation = _preview;
            _preview = null;

            bool freehand = annotation.Kind == AnnotationKind.Pen ||
                annotation.Kind == AnnotationKind.Mosaic;
            if (!freehand &&
                (annotation.Points.Count < 2 || Distance(annotation.Points[0], annotation.Points[1]) < 2))
            {
                return;
            }

            _annotations.Add(annotation);
            _selected = _annotations.Count - 1;
            AppendAnnotationToRender(annotation);
            Invalidate();
        }

        private void BeginMove(Point imagePoint)
        {
            PushUndo();
            _moving = true;
            _moveStart = imagePoint;
            _moveOriginalPoints = ClonePoints(_annotations[_selected].Points);
            _movingIndex = _selected;
            ReRenderExcluding(_selected);
        }

        private void UpdateMove(Point point)
        {
            Rectangle oldBounds = GetBounds(_annotations[_selected]);
            int dx = point.X - _moveStart.X;
            int dy = point.Y - _moveStart.Y;
            _annotations[_selected].Points = ShiftPoints(_moveOriginalPoints, dx, dy);
            InvalidateAnnotationRegion(oldBounds, GetBounds(_annotations[_selected]));
        }

        private void BeginResize(int handle)
        {
            PushUndo();
            _resizing = true;
            _resizeHandle = handle;
            _resizeOldRect = GetBounds(_annotations[_selected]);
            _resizeOriginalPoints = ClonePoints(_annotations[_selected].Points);
            _resizeOriginalFont = _annotations[_selected].FontSize;
            _movingIndex = _selected;
            ReRenderExcluding(_selected);
        }

        private void UpdateResize(Point point)
        {
            Rectangle newRect = ComputeResizeRect(_resizeHandle, _resizeOldRect, point);
            if (newRect.Width < 2 || newRect.Height < 2)
            {
                return;
            }

            Rectangle oldBounds = GetBounds(_annotations[_selected]);
            Annotation annotation = _annotations[_selected];
            if (annotation.Kind == AnnotationKind.Text)
            {
                float sy = _resizeOldRect.Height == 0
                    ? 1f
                    : (float)newRect.Height / _resizeOldRect.Height;
                annotation.FontSize = Clamp(_resizeOriginalFont * sy, 8f, 256f);
                annotation.Points[0] = new Point(newRect.Left, newRect.Top);
            }
            else
            {
                annotation.Points = ScalePoints(_resizeOriginalPoints, _resizeOldRect, newRect);
            }

            InvalidateAnnotationRegion(oldBounds, GetBounds(annotation));
        }

        private void BeginInlineText(Point imagePoint)
        {
            CommitInlineText();

            _textEditorImagePoint = imagePoint;
            var editor = new TransparentTextBox
            {
                ForeColor = _color,
                Width = 120
            };
            editor.Font = new Font(
                "Microsoft YaHei",
                Math.Max(10f, _textSize * _scale),
                FontStyle.Bold,
                GraphicsUnit.Pixel);
            editor.KeyDown += TextEditorKeyDown;
            editor.TextChanged += (s, e) => AutoSizeEditor(editor);

            Point client = ImageToClient(imagePoint);
            int width = editor.Width;
            int height = editor.Height;
            int x = Math.Max(0, Math.Min(client.X, ClientSize.Width - width));
            int y = Math.Max(0, Math.Min(client.Y, ClientSize.Height - height));
            editor.Location = new Point(x, y);

            _textEditor = editor;
            UpdateEditorBackground();
            Controls.Add(editor);
            editor.BringToFront();
            editor.Focus();
        }

        private void AutoSizeEditor(TextBox editor)
        {
            int textWidth = TextRenderer.MeasureText(editor.Text, editor.Font).Width + 18;
            int maxWidth = Math.Max(100, ClientSize.Width - editor.Left - 4);
            editor.Width = Math.Max(100, Math.Min(maxWidth, textWidth));
            UpdateEditorBackground();
        }

        private void UpdateEditorBackground()
        {
            if (_textEditor == null || _render == null)
            {
                return;
            }

            _textEditor.BackgroundImageSource = _render;
            int x = _textEditor.Left - _offset.X;
            int y = _textEditor.Top - _offset.Y;
            _textEditor.BackgroundSourceRect = new Rectangle(
                (int)Math.Round(x / _scale),
                (int)Math.Round(y / _scale),
                Math.Max(1, (int)Math.Round(_textEditor.Width / _scale)),
                Math.Max(1, (int)Math.Round(_textEditor.Height / _scale)));

            if (_textEditor.IsHandleCreated)
            {
                _textEditor.Invalidate();
            }
        }

        private void TextEditorKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                CommitInlineText();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                CancelInlineText();
            }
        }

        private void CommitInlineText()
        {
            if (_textEditor == null)
            {
                return;
            }

            TextBox editor = _textEditor;
            _textEditor = null;
            string text = editor.Text;
            Point imagePoint = _textEditorImagePoint;
            editor.Dispose();

            if (string.IsNullOrEmpty(text))
            {
                Invalidate();
                return;
            }

            PushUndo();
            var annotation = new Annotation
            {
                Kind = AnnotationKind.Text,
                Color = _color,
                FontSize = _textSize
            };
            annotation.Text = text;
            annotation.Points.Add(imagePoint);
            _annotations.Add(annotation);
            _selected = _annotations.Count - 1;
            AppendAnnotationToRender(annotation);
            Invalidate();
        }

        private void CancelInlineText()
        {
            if (_textEditor == null)
            {
                return;
            }

            TextBox editor = _textEditor;
            _textEditor = null;
            editor.Dispose();
            Invalidate();
        }

        private void DeleteSelected()
        {
            if (_selected < 0)
            {
                return;
            }

            PushUndo();
            _annotations.RemoveAt(_selected);
            _selected = -1;
            ReRender();
            Invalidate();
        }

        private void Finish(AnnotatorResult action, DialogResult dialogResult)
        {
            CommitInlineText();
            _resultAction = action;
            DialogResult = dialogResult;
            Close();
        }

        private void SaveAndFinish()
        {
            if (SaveImage())
            {
                Finish(AnnotatorResult.Save, DialogResult.OK);
            }
        }

        private bool SaveImage()
        {
            return ImageSaveHelper.SaveWithDialog(_render, this);
        }

        private void PushUndo()
        {
            if (_undo.Count >= UndoLimit)
            {
                _undo.Pop();
            }
            _undo.Push(CloneAnnotations());
        }

        private List<Annotation> CloneAnnotations()
        {
            var list = new List<Annotation>(_annotations.Count);
            foreach (Annotation annotation in _annotations)
            {
                list.Add(annotation.Clone());
            }
            return list;
        }

        private void Undo()
        {
            if (_undo.Count == 0)
            {
                return;
            }

            _annotations.Clear();
            _annotations.AddRange(_undo.Pop());
            if (_selected >= _annotations.Count)
            {
                _selected = -1;
            }
            ReRender();
            Invalidate();
        }

        private void ReRender()
        {
            ReRenderInternal(-1);
        }

        private void ReRenderExcluding(int excludeIndex)
        {
            ReRenderInternal(excludeIndex);
        }

        private void ReRenderInternal(int excludeIndex)
        {
            if (_render != null)
            {
                _render.Dispose();
            }

            _render = new Bitmap(_baseImage);
            using (Graphics g = Graphics.FromImage(_render))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                for (int i = 0; i < _annotations.Count; i++)
                {
                    if (i == excludeIndex)
                    {
                        continue;
                    }

                    Annotation annotation = _annotations[i];
                    if (annotation.Kind == AnnotationKind.Mosaic)
                    {
                        ApplyMosaicStroke(_render, annotation);
                    }
                    else
                    {
                        DrawAnnotation(g, annotation);
                    }
                }
            }
        }

        private void AppendAnnotationToRender(Annotation annotation)
        {
            if (annotation.Kind == AnnotationKind.Mosaic)
            {
                ApplyMosaicStroke(_render, annotation);
            }
            else
            {
                using (Graphics g = Graphics.FromImage(_render))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    DrawAnnotation(g, annotation);
                }
            }
        }

        private static void DrawAnnotation(Graphics g, Annotation annotation)
        {
            switch (annotation.Kind)
            {
                case AnnotationKind.Pen:
                    if (annotation.Points.Count == 1)
                    {
                        using (Brush brush = new SolidBrush(annotation.Color))
                        {
                            float radius = Math.Max(1f, annotation.StrokeWidth / 2f);
                            Point p = annotation.Points[0];
                            g.FillEllipse(brush, p.X - radius, p.Y - radius, radius * 2, radius * 2);
                        }
                    }
                    else
                    {
                        using (Pen pen = CreatePen(annotation.Color, annotation.StrokeWidth))
                        {
                            for (int i = 1; i < annotation.Points.Count; i++)
                            {
                                g.DrawLine(pen, annotation.Points[i - 1], annotation.Points[i]);
                            }
                        }
                    }
                    break;

                case AnnotationKind.Arrow:
                    if (annotation.Points.Count >= 2)
                    {
                        using (Pen pen = CreatePen(annotation.Color, annotation.StrokeWidth))
                        {
                            DrawArrowShape(
                                g,
                                annotation.Points[0],
                                annotation.Points[1],
                                pen,
                                Math.Max(8f, annotation.StrokeWidth * 4f));
                        }
                    }
                    break;

                case AnnotationKind.Rectangle:
                    if (annotation.Points.Count >= 2)
                    {
                        using (Pen pen = CreatePen(annotation.Color, annotation.StrokeWidth))
                        {
                            g.DrawRectangle(pen, NormalizeRect(annotation.Points[0], annotation.Points[1]));
                        }
                    }
                    break;

                case AnnotationKind.Text:
                    using (Font font = new Font(
                        "Microsoft YaHei",
                        Math.Max(8f, annotation.FontSize),
                        FontStyle.Bold,
                        GraphicsUnit.Pixel))
                    using (Brush brush = new SolidBrush(annotation.Color))
                    {
                        g.DrawString(annotation.Text, font, brush, annotation.Points[0].X, annotation.Points[0].Y);
                    }
                    break;
            }
        }

        private void DrawPreviewOverlay(Graphics g)
        {
            if (_preview != null)
            {
                DrawAnnotationScaled(g, _preview);
            }
        }

        private void DrawAnnotationScaled(Graphics g, Annotation annotation)
        {
            switch (annotation.Kind)
            {
                case AnnotationKind.Pen:
                    if (annotation.Points.Count == 1)
                    {
                        Point client = ImageToClient(annotation.Points[0]);
                        float radius = Math.Max(1f, annotation.StrokeWidth * _scale / 2f);
                        using (Brush brush = new SolidBrush(annotation.Color))
                        {
                            g.FillEllipse(brush, client.X - radius, client.Y - radius, radius * 2, radius * 2);
                        }
                    }
                    else if (annotation.Points.Count > 1)
                    {
                        var points = new Point[annotation.Points.Count];
                        for (int i = 0; i < points.Length; i++)
                        {
                            points[i] = ImageToClient(annotation.Points[i]);
                        }
                        using (Pen pen = CreatePen(annotation.Color, Math.Max(1f, annotation.StrokeWidth * _scale)))
                        {
                            g.DrawLines(pen, points);
                        }
                    }
                    break;

                case AnnotationKind.Arrow:
                    if (annotation.Points.Count >= 2)
                    {
                        Point a = ImageToClient(annotation.Points[0]);
                        Point b = ImageToClient(annotation.Points[1]);
                        using (Pen pen = CreatePen(annotation.Color, Math.Max(1f, annotation.StrokeWidth * _scale)))
                        {
                            DrawArrowShape(g, a, b, pen, Math.Max(8f, annotation.StrokeWidth * _scale * 4f));
                        }
                    }
                    break;

                case AnnotationKind.Rectangle:
                    if (annotation.Points.Count >= 2)
                    {
                        Point a = ImageToClient(annotation.Points[0]);
                        Point b = ImageToClient(annotation.Points[1]);
                        using (Pen pen = CreatePen(annotation.Color, Math.Max(1f, annotation.StrokeWidth * _scale)))
                        {
                            g.DrawRectangle(pen, NormalizeRect(a, b));
                        }
                    }
                    break;

                case AnnotationKind.Mosaic:
                    if (annotation.Points.Count == 1)
                    {
                        Point client = ImageToClient(annotation.Points[0]);
                        float radius = Math.Max(1f, MosaicRadius(annotation.StrokeWidth) * _scale);
                        using (Brush brush = new SolidBrush(Color.FromArgb(90, 90, 90, 90)))
                        {
                            g.FillEllipse(brush, client.X - radius, client.Y - radius, radius * 2, radius * 2);
                        }
                    }
                    else if (annotation.Points.Count > 1)
                    {
                        var points = new Point[annotation.Points.Count];
                        for (int i = 0; i < points.Length; i++)
                        {
                            points[i] = ImageToClient(annotation.Points[i]);
                        }
                        float width = Math.Max(2f, MosaicRadius(annotation.StrokeWidth) * 2f * _scale);
                        using (var pen = new Pen(Color.FromArgb(90, 90, 90, 90), width)
                        {
                            StartCap = LineCap.Round,
                            EndCap = LineCap.Round,
                            LineJoin = LineJoin.Round
                        })
                        {
                            g.DrawLines(pen, points);
                        }
                    }
                    break;

                case AnnotationKind.Text:
                    if (annotation.Points.Count >= 1)
                    {
                        Point client = ImageToClient(annotation.Points[0]);
                        using (Font font = new Font(
                            "Microsoft YaHei",
                            Math.Max(1f, annotation.FontSize * _scale),
                            FontStyle.Bold,
                            GraphicsUnit.Pixel))
                        using (Brush brush = new SolidBrush(annotation.Color))
                        {
                            g.DrawString(annotation.Text, font, brush, client.X, client.Y);
                        }
                    }
                    break;
            }
        }

        private void DrawSelection(Graphics g)
        {
            if (_selected < 0)
            {
                return;
            }

            Rectangle bounds = GetBounds(_annotations[_selected]);
            if (bounds.IsEmpty)
            {
                return;
            }

            Point tl = ImageToClient(bounds.Location);
            Point br = ImageToClient(new Point(bounds.Right, bounds.Bottom));
            Rectangle clientBounds = Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);

            using (var pen = new Pen(Color.FromArgb(0, 170, 255), 1f)
            {
                DashStyle = DashStyle.Dash
            })
            {
                g.DrawRectangle(pen, clientBounds);
            }

            using (Brush handleBrush = new SolidBrush(Color.FromArgb(0, 170, 255)))
            {
                foreach (Rectangle handle in GetHandleRectsClient(bounds))
                {
                    g.FillRectangle(handleBrush, handle);
                }
            }
        }

        private void InvalidateAnnotationRegion(Rectangle a, Rectangle b)
        {
            if (a.IsEmpty && b.IsEmpty)
            {
                Invalidate();
                return;
            }

            Rectangle dirty = Rectangle.Union(a, b);
            dirty.Inflate(HandleSize * 2 + 16, HandleSize * 2 + 16);
            Point tl = ImageToClient(dirty.Location);
            Point br = ImageToClient(new Point(dirty.Right, dirty.Bottom));
            Rectangle clientDirty = Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            clientDirty.Intersect(new Rectangle(Point.Empty, ClientSize));
            if (clientDirty.Width > 0 && clientDirty.Height > 0)
            {
                Invalidate(clientDirty);
            }
        }

        private int HitTest(Point point)
        {
            for (int i = _annotations.Count - 1; i >= 0; i--)
            {
                if (GetBounds(_annotations[i]).Contains(point))
                {
                    return i;
                }
            }
            return -1;
        }

        private int HitHandle(Rectangle imageBounds, Point clientPoint)
        {
            Rectangle[] handles = GetHandleRectsClient(imageBounds);
            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i].Contains(clientPoint))
                {
                    return i;
                }
            }
            return -1;
        }

        private Rectangle[] GetHandleRectsClient(Rectangle imageBounds)
        {
            Point tl = ImageToClient(imageBounds.Location);
            Point br = ImageToClient(new Point(imageBounds.Right, imageBounds.Bottom));
            Rectangle clientBounds = Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);
            int h = HandleSize;
            return new[]
            {
                new Rectangle(clientBounds.Left - h / 2, clientBounds.Top - h / 2, h, h),
                new Rectangle(clientBounds.Right - h / 2, clientBounds.Top - h / 2, h, h),
                new Rectangle(clientBounds.Left - h / 2, clientBounds.Bottom - h / 2, h, h),
                new Rectangle(clientBounds.Right - h / 2, clientBounds.Bottom - h / 2, h, h)
            };
        }

        private Rectangle GetBounds(Annotation annotation)
        {
            switch (annotation.Kind)
            {
                case AnnotationKind.Pen:
                case AnnotationKind.Arrow:
                    return BoundsOfPoints(annotation.Points, annotation.StrokeWidth);
                case AnnotationKind.Rectangle:
                    if (annotation.Points.Count < 2)
                    {
                        return Rectangle.Empty;
                    }
                    return NormalizeRect(annotation.Points[0], annotation.Points[1]);
                case AnnotationKind.Mosaic:
                    return BoundsOfPoints(annotation.Points, MosaicRadius(annotation.StrokeWidth));
                case AnnotationKind.Text:
                    return GetTextBounds(annotation);
                default:
                    return Rectangle.Empty;
            }
        }

        private Rectangle GetTextBounds(Annotation annotation)
        {
            Bitmap measureTarget = _render ?? _baseImage;
            using (Font font = new Font(
                "Microsoft YaHei",
                Math.Max(8f, annotation.FontSize),
                FontStyle.Bold,
                GraphicsUnit.Pixel))
            using (Graphics g = Graphics.FromImage(measureTarget))
            {
                SizeF size = g.MeasureString(annotation.Text, font);
                int width = Math.Max(1, (int)Math.Ceiling(size.Width));
                int height = Math.Max(1, (int)Math.Ceiling(size.Height));
                return new Rectangle(annotation.Points[0], new Size(width, height));
            }
        }

        private void ComputeLayout()
        {
            Size work = Screen.PrimaryScreen.WorkingArea.Size;
            int availWidth = work.Width - 80;
            int availHeight = work.Height - 180;

            _scale = Math.Min(
                1f,
                Math.Min(
                    (float)availWidth / _baseImage.Width,
                    (float)availHeight / _baseImage.Height));
            if (_scale < 0.05f)
            {
                _scale = 0.05f;
            }

            int imageWidth = Math.Max(1, (int)Math.Round(_baseImage.Width * _scale));
            int imageHeight = Math.Max(1, (int)Math.Round(_baseImage.Height * _scale));
            _displaySize = new Size(imageWidth, imageHeight);

            int clientWidth = Math.Max(imageWidth, 420);

            ClientSize = new Size(clientWidth, 24);
            _toolbar.PerformLayout();
            int toolbarHeight = Math.Max(24, _toolbar.Height);

            _offset = new Point((clientWidth - imageWidth) / 2, toolbarHeight);
            ClientSize = new Size(clientWidth, toolbarHeight + imageHeight);

            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(
                workArea.X + (workArea.Width - Width) / 2,
                workArea.Y + (workArea.Height - Height) / 2);
        }

        private Point ClientToImage(Point client)
        {
            return new Point(
                (int)Math.Round((client.X - _offset.X) / _scale),
                (int)Math.Round((client.Y - _offset.Y) / _scale));
        }

        private Point ImageToClient(Point image)
        {
            return new Point(
                _offset.X + (int)Math.Round(image.X * _scale),
                _offset.Y + (int)Math.Round(image.Y * _scale));
        }

        private bool IsInsideImage(Point point)
        {
            return point.X >= 0 && point.Y >= 0 &&
                point.X < _baseImage.Width && point.Y < _baseImage.Height;
        }

        private Point ClampToImage(Point point)
        {
            return new Point(
                Math.Max(0, Math.Min(_baseImage.Width - 1, point.X)),
                Math.Max(0, Math.Min(_baseImage.Height - 1, point.Y)));
        }

        private static AnnotationKind ToKind(AnnotationTool tool)
        {
            switch (tool)
            {
                case AnnotationTool.Arrow:
                    return AnnotationKind.Arrow;
                case AnnotationTool.Rectangle:
                    return AnnotationKind.Rectangle;
                case AnnotationTool.Mosaic:
                    return AnnotationKind.Mosaic;
                case AnnotationTool.Text:
                    return AnnotationKind.Text;
                default:
                    return AnnotationKind.Pen;
            }
        }

        private static int Distance(Point a, Point b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (int)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static Rectangle NormalizeRect(Point a, Point b)
        {
            int x = Math.Min(a.X, b.X);
            int y = Math.Min(a.Y, b.Y);
            int width = Math.Abs(b.X - a.X);
            int height = Math.Abs(b.Y - a.Y);
            return new Rectangle(x, y, width, height);
        }

        private static Rectangle BoundsOfPoints(List<Point> points, float pad)
        {
            if (points.Count == 0)
            {
                return Rectangle.Empty;
            }

            int minX = points[0].X;
            int minY = points[0].Y;
            int maxX = points[0].X;
            int maxY = points[0].Y;
            foreach (Point point in points)
            {
                if (point.X < minX) minX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.X > maxX) maxX = point.X;
                if (point.Y > maxY) maxY = point.Y;
            }

            Rectangle rect = Rectangle.FromLTRB(minX, minY, maxX, maxY);
            rect.Inflate(Math.Max(6, (int)Math.Ceiling(pad) + 4), Math.Max(6, (int)Math.Ceiling(pad) + 4));
            return rect;
        }

        private static float MosaicRadius(float strokeWidth)
        {
            return Math.Max(6f, strokeWidth * 3f);
        }

        private static void ApplyMosaicStroke(Bitmap target, Annotation annotation)
        {
            if (annotation.Points.Count == 0)
            {
                return;
            }

            float radius = MosaicRadius(annotation.StrokeWidth);
            Rectangle bounds = Rectangle.Intersect(
                BoundsOfPoints(annotation.Points, radius),
                new Rectangle(0, 0, target.Width, target.Height));
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            using (Bitmap backup = target.Clone(bounds, target.PixelFormat))
            {
                MosaicEffect.Apply(target, bounds, MosaicCellSize);

                using (var stroke = new GraphicsPath())
                {
                    if (annotation.Points.Count == 1)
                    {
                        Point point = annotation.Points[0];
                        stroke.AddEllipse(point.X - radius, point.Y - radius, radius * 2, radius * 2);
                    }
                    else
                    {
                        stroke.AddLines(annotation.Points.ToArray());
                        using (var widenPen = new Pen(Color.Black, radius * 2f)
                        {
                            StartCap = LineCap.Round,
                            EndCap = LineCap.Round,
                            LineJoin = LineJoin.Round
                        })
                        {
                            stroke.Widen(widenPen);
                        }
                    }

                    using (Graphics g = Graphics.FromImage(target))
                    {
                        g.SetClip(stroke, CombineMode.Exclude);
                        g.DrawImage(backup, bounds);
                    }
                }
            }
        }

        private static List<Point> ClonePoints(List<Point> source)
        {
            return new List<Point>(source);
        }

        private static List<Point> ShiftPoints(List<Point> source, int dx, int dy)
        {
            var result = new List<Point>(source.Count);
            foreach (Point point in source)
            {
                result.Add(new Point(point.X + dx, point.Y + dy));
            }
            return result;
        }

        private static List<Point> ScalePoints(List<Point> source, Rectangle oldRect, Rectangle newRect)
        {
            float sx = oldRect.Width == 0 ? 1f : (float)newRect.Width / oldRect.Width;
            float sy = oldRect.Height == 0 ? 1f : (float)newRect.Height / oldRect.Height;

            var result = new List<Point>(source.Count);
            foreach (Point point in source)
            {
                result.Add(new Point(
                    (int)Math.Round(newRect.X + (point.X - oldRect.X) * sx),
                    (int)Math.Round(newRect.Y + (point.Y - oldRect.Y) * sy)));
            }
            return result;
        }

        private static Rectangle ComputeResizeRect(int handle, Rectangle oldRect, Point point)
        {
            switch (handle)
            {
                case 0:
                    return NormalizeRect(point, new Point(oldRect.Right, oldRect.Bottom));
                case 1:
                    return NormalizeRect(new Point(oldRect.Left, point.Y), new Point(point.X, oldRect.Bottom));
                case 2:
                    return NormalizeRect(new Point(point.X, oldRect.Top), new Point(oldRect.Right, point.Y));
                default:
                    return NormalizeRect(new Point(oldRect.Left, oldRect.Top), point);
            }
        }

        private static Pen CreatePen(Color color, float width)
        {
            return new Pen(color, width)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
        }

        private static void DrawArrowShape(Graphics g, Point start, Point end, Pen pen, float headLength)
        {
            g.DrawLine(pen, start, end);

            double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            for (int i = 0; i < 2; i++)
            {
                double branch = angle + Math.PI + (i == 0 ? -1 : 1) * (Math.PI / 7.0);
                var tip = new PointF(
                    end.X + headLength * (float)Math.Cos(branch),
                    end.Y + headLength * (float)Math.Sin(branch));
                g.DrawLine(pen, end, new Point((int)Math.Round(tip.X), (int)Math.Round(tip.Y)));
            }
        }

        private static Image CreateSwatch(Color color)
        {
            var bitmap = new Bitmap(14, 14);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                using (Brush brush = new SolidBrush(color))
                {
                    g.FillRectangle(brush, 1, 1, 12, 12);
                }
                using (var border = new Pen(Color.FromArgb(120, 0, 0, 0)))
                {
                    g.DrawRectangle(border, 0, 0, 13, 13);
                }
            }
            return bitmap;
        }

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin { get { return Color.FromArgb(45, 45, 48); } }
            public override Color ToolStripGradientMiddle { get { return Color.FromArgb(45, 45, 48); } }
            public override Color ToolStripGradientEnd { get { return Color.FromArgb(45, 45, 48); } }
            public override Color ToolStripBorder { get { return Color.FromArgb(30, 30, 30); } }
            public override Color ToolStripDropDownBackground { get { return Color.FromArgb(45, 45, 48); } }
            public override Color ImageMarginGradientBegin { get { return Color.FromArgb(45, 45, 48); } }
            public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(45, 45, 48); } }
            public override Color ImageMarginGradientEnd { get { return Color.FromArgb(45, 45, 48); } }
            public override Color ButtonSelectedHighlight { get { return Color.FromArgb(0, 120, 215); } }
            public override Color ButtonSelectedHighlightBorder { get { return Color.FromArgb(0, 120, 215); } }
            public override Color ButtonSelectedBorder { get { return Color.FromArgb(0, 120, 215); } }
            public override Color ButtonPressedHighlight { get { return Color.FromArgb(0, 90, 170); } }
            public override Color ButtonPressedHighlightBorder { get { return Color.FromArgb(0, 90, 170); } }
            public override Color ButtonPressedBorder { get { return Color.FromArgb(0, 90, 170); } }
            public override Color ButtonCheckedHighlight { get { return Color.FromArgb(0, 120, 215); } }
            public override Color ButtonCheckedHighlightBorder { get { return Color.FromArgb(0, 120, 215); } }
            public override Color ButtonCheckedGradientBegin { get { return Color.FromArgb(0, 120, 215); } }
            public override Color ButtonCheckedGradientMiddle { get { return Color.FromArgb(0, 120, 215); } }
            public override Color ButtonCheckedGradientEnd { get { return Color.FromArgb(0, 120, 215); } }
            public override Color SeparatorDark { get { return Color.FromArgb(70, 70, 74); } }
            public override Color SeparatorLight { get { return Color.FromArgb(70, 70, 74); } }
        }

    }
}
