using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;

namespace ProjetoIntegradorMenuz.ViewModels
{
    public enum OptionKind { Size, Addon, Ingredient }

    /// <summary>Linha editável de tamanho, adicional ou ingrediente.</summary>
    public partial class EditableOptionViewModel : ObservableObject
    {
        public EditableOptionViewModel(OptionKind kind, int? id = null, string name = "", decimal price = 0, bool isRemovable = true)
        {
            Kind = kind;
            Id = id;
            this.name = name;
            priceText = price == 0 ? string.Empty : price.ToString("0.00", CultureInfo.CurrentCulture);
            this.isRemovable = isRemovable;
        }

        public OptionKind Kind { get; }

        /// <summary>Null = opção nova (a API cria); preenchido = opção existente (a API mantém o id).</summary>
        public int? Id { get; }

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string priceText;

        [ObservableProperty]
        private bool isRemovable;

        public string PricePlaceholder => Kind == OptionKind.Size ? "+ R$ (0 se igual)" : "R$ (0 = grátis)";
    }

    /// <summary>Cadastro/edição de produto pela gerência. Recebe ?id= (0 = novo) e ?categoryId= pela navegação do Shell.</summary>
    public partial class ProductEditViewModel : ObservableObject, IQueryAttributable
    {
        private readonly AdminApiService _admin;
        private int _id;
        private int _initialCategoryId;

        public ProductEditViewModel(AdminApiService admin) => _admin = admin;

        public bool IsNew => _id == 0;
        public bool IsExisting => !IsNew;
        public string PageTitle => IsNew ? "Novo produto" : "Editar produto";

        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string description = string.Empty;
        [ObservableProperty] private string priceText = string.Empty;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ImagePreview))]
        [NotifyPropertyChangedFor(nameof(HasImage))]
        [NotifyPropertyChangedFor(nameof(HasNoImage))]
        [NotifyPropertyChangedFor(nameof(ImageLink))]
        [NotifyPropertyChangedFor(nameof(ImageOriginText))]
        private string imageUrl = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PickImageCommand))]
        private bool isUploading;

        [ObservableProperty] private bool isAvailable = true;

        // ---- Foto ----

        private static bool IsUploadedPath(string? url) => url?.StartsWith("uploads/", StringComparison.Ordinal) == true;

        public ImageSource? ImagePreview => ApiConfig.ResolveImage(ImageUrl) is { } uri ? ImageSource.FromUri(uri) : null;
        public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
        public bool HasNoImage => !HasImage;
        public string ImageOriginText => IsUploadedPath(ImageUrl) ? "Arquivo enviado do dispositivo" : "Link externo";

        /// <summary>Campo "ou cole um link": fica vazio quando a foto foi enviada como arquivo.</summary>
        public string ImageLink
        {
            get => IsUploadedPath(ImageUrl) ? string.Empty : ImageUrl;
            set
            {
                if (value == ImageLink) return;
                ImageUrl = value?.Trim() ?? string.Empty;
            }
        }

        private bool CanPickImage() => !IsUploading;

        /// <summary>Escolhe uma foto do computador/tablet e envia para o servidor.</summary>
        [RelayCommand(CanExecute = nameof(CanPickImage))]
        private async Task PickImageAsync()
        {
            ErrorMessage = null;
            FileResult? file;
            try
            {
                file = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Foto do produto",
                    FileTypes = FilePickerFileType.Images
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Não foi possível abrir os arquivos: {ex.Message}";
                return;
            }
            if (file is null) return; // cancelou

            IsUploading = true;
            try
            {
                // Copia para a memória para conferir o tamanho antes de enviar.
                await using var source = await file.OpenReadAsync();
                using var buffer = new MemoryStream();
                await source.CopyToAsync(buffer);
                if (buffer.Length > 5 * 1024 * 1024)
                {
                    ErrorMessage = "A imagem pode ter no máximo 5 MB.";
                    return;
                }

                buffer.Position = 0;
                ImageUrl = await _admin.UploadImageAsync(buffer, file.FileName);
            }
            catch (Exception ex) when (ex is StaffAccessException or OrderRejectedException)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível enviar a imagem. Verifique a conexão com o servidor.";
            }
            finally
            {
                IsUploading = false;
            }
        }

        [RelayCommand]
        private void RemoveImage() => ImageUrl = string.Empty;
        [ObservableProperty] private CategoryAdmin? selectedCategory;

        public ObservableCollection<CategoryAdmin> Categories { get; } = new();
        public ObservableCollection<EditableOptionViewModel> Sizes { get; } = new();
        public ObservableCollection<EditableOptionViewModel> Addons { get; } = new();
        public ObservableCollection<EditableOptionViewModel> Ingredients { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private bool isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _id = query.TryGetValue("id", out var id) && int.TryParse(id?.ToString(), out var i) ? i : 0;
            _initialCategoryId = query.TryGetValue("categoryId", out var c) && int.TryParse(c?.ToString(), out var ci) ? ci : 0;
            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(IsExisting));
            OnPropertyChanged(nameof(PageTitle));
        }

        public async Task LoadAsync()
        {
            await RunAsync(async () =>
            {
                var categories = await _admin.GetCategoriesAsync();
                Categories.Clear();
                foreach (var c in categories) Categories.Add(c);

                if (IsNew)
                {
                    SelectedCategory = Categories.FirstOrDefault(c => c.Id == _initialCategoryId) ?? Categories.FirstOrDefault();
                    return;
                }

                var p = await _admin.GetProductAsync(_id);
                Name = p.Name;
                Description = p.Description;
                PriceText = p.BasePrice.ToString("0.00", CultureInfo.CurrentCulture);
                ImageUrl = p.ImageUrl ?? string.Empty;
                IsAvailable = p.IsAvailable;
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == p.CategoryId);

                Sizes.Clear();
                foreach (var s in p.Sizes) Sizes.Add(new EditableOptionViewModel(OptionKind.Size, s.Id, s.Name, s.PriceDelta));
                Addons.Clear();
                foreach (var a in p.Addons) Addons.Add(new EditableOptionViewModel(OptionKind.Addon, a.Id, a.Name, a.Price));
                Ingredients.Clear();
                foreach (var i in p.Ingredients) Ingredients.Add(new EditableOptionViewModel(OptionKind.Ingredient, i.Id, i.Name, isRemovable: i.IsRemovable));
            });
        }

        [RelayCommand] private void AddSize() => Sizes.Add(new EditableOptionViewModel(OptionKind.Size));
        [RelayCommand] private void AddAddon() => Addons.Add(new EditableOptionViewModel(OptionKind.Addon));
        [RelayCommand] private void AddIngredient() => Ingredients.Add(new EditableOptionViewModel(OptionKind.Ingredient));

        [RelayCommand]
        private void RemoveOption(EditableOptionViewModel? option)
        {
            if (option is null) return;
            (option.Kind switch
            {
                OptionKind.Size => Sizes,
                OptionKind.Addon => Addons,
                _ => Ingredients
            }).Remove(option);
        }

        private bool CanSave() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Informe o nome do produto."; return; }
            if (SelectedCategory is null) { ErrorMessage = "Escolha uma categoria."; return; }
            if (!TryParseMoney(PriceText, out var price)) { ErrorMessage = "Preço inválido. Use, por exemplo, 24,90."; return; }

            var request = new SaveProductRequest
            {
                Name = Name.Trim(),
                Description = Description?.Trim(),
                BasePrice = price,
                ImageUrl = string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl.Trim(),
                IsAvailable = IsAvailable,
                CategoryId = SelectedCategory.Id
            };

            foreach (var s in Sizes)
            {
                if (!TryParseMoney(s.PriceText, out var delta, allowEmpty: true)) { ErrorMessage = $"Acréscimo inválido no tamanho \"{s.Name}\"."; return; }
                request.Sizes.Add(new SaveSizeRequest { Id = s.Id, Name = s.Name?.Trim() ?? string.Empty, PriceDelta = delta });
            }
            foreach (var a in Addons)
            {
                if (!TryParseMoney(a.PriceText, out var addonPrice, allowEmpty: true)) { ErrorMessage = $"Preço inválido no adicional \"{a.Name}\"."; return; }
                request.Addons.Add(new SaveAddonRequest { Id = a.Id, Name = a.Name?.Trim() ?? string.Empty, Price = addonPrice });
            }
            foreach (var i in Ingredients)
                request.Ingredients.Add(new SaveIngredientRequest { Id = i.Id, Name = i.Name?.Trim() ?? string.Empty, IsRemovable = i.IsRemovable });

            var saved = await RunAsync(async () =>
            {
                if (IsNew) await _admin.CreateProductAsync(request);
                else await _admin.UpdateProductAsync(_id, request);
            });

            if (saved) await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task DeleteAsync()
        {
            if (IsNew || Shell.Current?.CurrentPage is not { } page) return;

            var confirm = await page.DisplayAlertAsync(
                $"Excluir \"{Name}\"?",
                "O produto sai do cardápio definitivamente. Pedidos antigos continuam com o nome e o preço da época. " +
                "Para tirar do cardápio só por um tempo, use \"Disponível\".",
                "Excluir", "Voltar");
            if (!confirm) return;

            if (await RunAsync(() => _admin.DeleteProductAsync(_id)))
                await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

        /// <summary>Aceita "24,90", "24.90" e "R$ 24,90".</summary>
        private static bool TryParseMoney(string? text, out decimal value, bool allowEmpty = false)
        {
            value = 0;
            var t = (text ?? string.Empty).Replace("R$", string.Empty).Trim();
            if (t.Length == 0) return allowEmpty;

            // Com vírgula: formato brasileiro (ponto vira separador de milhar). Sem vírgula: ponto decimal.
            return t.Contains(',')
                ? decimal.TryParse(t.Replace(".", string.Empty), NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out value)
                : decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private async Task<bool> RunAsync(Func<Task> action)
        {
            IsBusy = true;
            ErrorMessage = null;
            try
            {
                await action();
                return true;
            }
            catch (Exception ex) when (ex is StaffAccessException or OrderRejectedException)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível falar com o servidor. Verifique se a API está em execução.";
            }
            finally
            {
                IsBusy = false;
            }
            return false;
        }
    }
}
