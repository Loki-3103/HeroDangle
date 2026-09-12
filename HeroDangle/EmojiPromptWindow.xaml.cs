using System.Windows;

namespace HeroDangle;

public partial class EmojiPromptWindow : Window
{
    public string Emoji { get; private set; }

    public EmojiPromptWindow(string current)
    {
        InitializeComponent();
        Emoji = current;
        EmojiBox.Text = current;
        EmojiBox.SelectAll();
        Loaded += (_, _) => EmojiBox.Focus();
    }

    private void OnUse(object sender, RoutedEventArgs e)
    {
        string text = EmojiBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        Emoji = text;
        DialogResult = true;
        Close();
    }
}
