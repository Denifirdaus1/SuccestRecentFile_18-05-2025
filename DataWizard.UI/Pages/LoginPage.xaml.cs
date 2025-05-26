using Microsoft.UI.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using DataWizard.UI.Services;
using System;
using System.Threading.Tasks;

namespace DataWizard.UI.Pages
{
    public sealed partial class LoginPage : Page
    {
        private string _currentMode = "signin";
        private readonly AuthenticationService _authService;

        public LoginPage()
        {
            this.InitializeComponent();
            _authService = new AuthenticationService();
            UpdateButtonStates();
        }

        private async Task ShowDialogAsync(string title, string content)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        #region Event Handlers

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToSignIn();
        }

        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchToSignUp();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == UsernameTextBox)
            {
                UsernameCheckmark.Visibility = string.IsNullOrWhiteSpace(textBox.Text) ?
                    Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                UpdateAdditionalFieldsCheckmarks(textBox);
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            PasswordCheckmark.Visibility = string.IsNullOrWhiteSpace(passwordBox.Password) ?
                Visibility.Collapsed : Visibility.Visible;
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await ShowDialogAsync("Validation Error", "Please fill in all required fields.");
                return;
            }

            if (_currentMode == "signin")
            {
                var (success, error) = await _authService.SignInAsync(username, password);
                if (success)
                {
                    Frame.Navigate(typeof(MainPage), null, new DrillInNavigationTransitionInfo());
                }
                else
                {
                    await ShowDialogAsync("Sign In Error", error ?? "Invalid username or password");
                }
            }
            else
            {
                string email = string.Empty;
                string confirmPassword = string.Empty;

                var emailGrid = AdditionalFieldsPanel.Children[0] as Grid;
                if (emailGrid != null)
                {
                    var emailStackPanel = emailGrid.Children[1] as StackPanel;
                    if (emailStackPanel != null)
                    {
                        foreach (var child in emailStackPanel.Children)
                        {
                            if (child is TextBox textBox)
                            {
                                email = textBox.Text;
                                break;
                            }
                        }
                    }
                }

                var confirmPasswordGrid = AdditionalFieldsPanel.Children[1] as Grid;
                if (confirmPasswordGrid != null)
                {
                    var confirmPasswordStackPanel = confirmPasswordGrid.Children[1] as StackPanel;
                    if (confirmPasswordStackPanel != null)
                    {
                        foreach (var child in confirmPasswordStackPanel.Children)
                        {
                            if (child is PasswordBox passwordBox)
                            {
                                confirmPassword = passwordBox.Password;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    await ShowDialogAsync("Validation Error", "Please enter your email address.");
                    return;
                }

                if (password != confirmPassword)
                {
                    await ShowDialogAsync("Validation Error", "Passwords do not match.");
                    return;
                }

                var (success, error) = await _authService.SignUpAsync(username, password, email);
                if (success)
                {
                    await ShowDialogAsync("Success", "Account created successfully. Please sign in.");
                    SwitchToSignIn();
                }
                else
                {
                    await ShowDialogAsync("Sign Up Error", error ?? "Failed to create account");
                }
            }
        }

        #endregion

        #region UI State Management

        private void SwitchToSignIn()
        {
            if (_currentMode == "signin") return;

            FormTitle.Text = "Welcome Back";
            FormSubtitle.Text = "Welcome Back, Please Enter your details";
            SubmitButton.Content = "Sign In";

            AdditionalFieldsPanel.Children.Clear();

            _currentMode = "signin";
            UpdateButtonStates();

            AnimateContentChange();
        }

        private void SwitchToSignUp()
        {
            if (_currentMode == "signup") return;

            FormTitle.Text = "Create an Account";
            FormSubtitle.Text = "Please fill in the details to sign up";
            SubmitButton.Content = "Sign Up";

            AdditionalFieldsPanel.Children.Clear();

            Grid emailField = CreateInputField(
                "Email",
                "you@example.com",
                "/Assets/email.png",
                true);

            Grid confirmPasswordField = CreateInputField(
                "Confirm Password",
                "Confirm password",
                "/Assets/lock.png",
                false);

            AdditionalFieldsPanel.Children.Add(emailField);
            AdditionalFieldsPanel.Children.Add(confirmPasswordField);

            _currentMode = "signup";
            UpdateButtonStates();

            AnimateContentChange();
        }

        private void UpdateButtonStates()
        {
            if (_currentMode == "signin")
            {
                SignInButton.Background = new SolidColorBrush(Colors.White);
                SignUpButton.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 229, 231, 235));
            }
            else
            {
                SignInButton.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 229, 231, 235));
                SignUpButton.Background = new SolidColorBrush(Colors.White);
            }
        }

        private void AnimateContentChange()
        {
            var storyboard = new Storyboard();

            var fadeAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(300))
            };

            Storyboard.SetTarget(fadeAnimation, LoginFormPanel);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }

        #endregion

        #region Helper Methods

        private Grid CreateInputField(string label, string placeholder, string iconPath, bool isTextField)
        {
            Grid fieldGrid = new Grid
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 229, 231, 235)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12)
            };

            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Image icon = new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri($"ms-appx://{iconPath}")),
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(icon, 0);
            fieldGrid.Children.Add(icon);

            StackPanel content = new StackPanel
            {
                Name = "FieldContent"
            };
            Grid.SetColumn(content, 1);

            TextBlock labelText = new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 156, 163, 175))
            };
            content.Children.Add(labelText);

            if (isTextField)
            {
                TextBox inputField = new TextBox
                {
                    Name = "InputField",
                    PlaceholderText = placeholder,
                    BorderThickness = new Thickness(0),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Background = new SolidColorBrush(Colors.Transparent)
                };
                inputField.TextChanged += TextBox_TextChanged;
                content.Children.Add(inputField);
            }
            else
            {
                PasswordBox inputField = new PasswordBox
                {
                    Name = "InputField",
                    PlaceholderText = placeholder,
                    BorderThickness = new Thickness(0),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Background = new SolidColorBrush(Colors.Transparent)
                };
                inputField.PasswordChanged += PasswordBox_PasswordChanged;
                content.Children.Add(inputField);
            }

            fieldGrid.Children.Add(content);

            Grid checkmark = new Grid
            {
                Name = "Checkmark",
                Visibility = Visibility.Collapsed,
                Width = 20,
                Height = 20
            };

            Ellipse ellipse = new Ellipse
            {
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 126, 217, 192))
            };

            Path checkPath = new Path
            {
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1.5,
                StrokeLineJoin = PenLineJoin.Round,
                Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M6,10.5 L8.5,13 L14,7")
            };

            checkmark.Children.Add(ellipse);
            checkmark.Children.Add(checkPath);

            Grid.SetColumn(checkmark, 2);
            fieldGrid.Children.Add(checkmark);

            return fieldGrid;
        }

        private void UpdateAdditionalFieldsCheckmarks(TextBox textBox)
        {
            if (textBox.Parent is StackPanel stackPanel &&
                stackPanel.Parent is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Grid checkmarkGrid && checkmarkGrid.Name == "Checkmark")
                    {
                        checkmarkGrid.Visibility = string.IsNullOrWhiteSpace(textBox.Text) ?
                            Visibility.Collapsed : Visibility.Visible;
                        break;
                    }
                }
            }
        }

        #endregion
    }
}