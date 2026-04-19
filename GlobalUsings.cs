// Resolve ambiguities between WPF and WinForms/System.Drawing namespaces.
// Prefer WPF types by default; use fully-qualified System.Drawing.* or the WinForms alias in files that need them.
global using Application = System.Windows.Application;
global using Brush = System.Windows.Media.Brush;
global using Color = System.Windows.Media.Color;
global using ColorConverter = System.Windows.Media.ColorConverter;
global using TextBox = System.Windows.Controls.TextBox;
global using MessageBox = System.Windows.MessageBox;
global using Image = System.Windows.Controls.Image;
