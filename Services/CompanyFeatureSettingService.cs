using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Dtos.FeatureSettingsDTOs;
using SearchTool_ServerSide.Features;
using SearchTool_ServerSide.Models;
using SearchTool_ServerSide.Data;

namespace SearchTool_ServerSide.Services
{
    public class CompanyFeatureSettingService
    {
        private readonly SearchToolDBContext _context;

        public CompanyFeatureSettingService(SearchToolDBContext context)
        {
            _context = context;
        }

        public List<FeatureSettingsViewDto> GetCatalog()
        {
            return FeatureCatalog.Items
                .Select(feature => new FeatureSettingsViewDto
                {
                    FeatureKey = feature.FeatureKey,
                    FeatureName = feature.FeatureName,
                    Description = feature.Description,
                    SelectionType = feature.SelectionType,
                    DefaultOptionKeys = feature.DefaultOptionKeys,
                    SelectedOptionKeys = feature.DefaultOptionKeys,
                    IsEnabled = true,
                    Options = feature.Options.Select(option => new FeatureOptionViewDto
                    {
                        OptionKey = option.OptionKey,
                        OptionName = option.OptionName,
                        Description = option.Description
                    }).ToList()
                })
                .ToList();
        }

        public async Task<List<FeatureSettingsViewDto>> GetCompanySettingsViewAsync(int mainCompanyId)
        {
            var settings = await _context.MainCompanyFeatureSettings
                .Where(x => x.MainCompanyId == mainCompanyId)
                .ToListAsync();

            var result = new List<FeatureSettingsViewDto>();

            foreach (var feature in FeatureCatalog.Items)
            {
                var setting = settings.FirstOrDefault(x => x.FeatureKey == feature.FeatureKey);

                var selectedOptions = setting == null
                    ? feature.DefaultOptionKeys
                    : DeserializeOptionKeys(setting.SelectedOptionKeysJson);

                result.Add(new FeatureSettingsViewDto
                {
                    FeatureKey = feature.FeatureKey,
                    FeatureName = feature.FeatureName,
                    Description = feature.Description,
                    SelectionType = feature.SelectionType,
                    DefaultOptionKeys = feature.DefaultOptionKeys,
                    SelectedOptionKeys = selectedOptions,
                    IsEnabled = setting?.IsEnabled ?? true,
                    Options = feature.Options.Select(option => new FeatureOptionViewDto
                    {
                        OptionKey = option.OptionKey,
                        OptionName = option.OptionName,
                        Description = option.Description
                    }).ToList()
                });
            }

            return result;
        }

        public async Task<List<string>> GetSelectedOptionKeysAsync(int mainCompanyId, string featureKey)
        {
            var feature = GetFeatureOrThrow(featureKey);

            var setting = await _context.MainCompanyFeatureSettings
                .FirstOrDefaultAsync(x =>
                    x.MainCompanyId == mainCompanyId &&
                    x.FeatureKey == featureKey &&
                    x.IsEnabled);

            if (setting == null)
                return feature.DefaultOptionKeys;

            var selectedOptions = DeserializeOptionKeys(setting.SelectedOptionKeysJson);

            if (selectedOptions.Count == 0)
                return feature.DefaultOptionKeys;

            return selectedOptions;
        }

        public async Task<bool> IsOptionAllowedAsync(
            int mainCompanyId,
            string featureKey,
            string optionKey)
        {
            var selectedOptions = await GetSelectedOptionKeysAsync(mainCompanyId, featureKey);

            return selectedOptions.Contains(optionKey);
        }

        public async Task UpdateAsync(
            int mainCompanyId,
            string featureKey,
            UpdateMainCompanyFeatureSettingDto dto,
            int? updatedByUserId = null)
        {
            var feature = GetFeatureOrThrow(featureKey);

            ValidateSelectedOptions(feature, dto.SelectedOptionKeys);

            var setting = await _context.MainCompanyFeatureSettings
                .FirstOrDefaultAsync(x =>
                    x.MainCompanyId == mainCompanyId &&
                    x.FeatureKey == featureKey);

            var json = JsonSerializer.Serialize(dto.SelectedOptionKeys.Distinct().ToList());

            if (setting == null)
            {
                setting = new MainCompanyFeatureSetting
                {
                    MainCompanyId = mainCompanyId,
                    FeatureKey = featureKey,
                    SelectedOptionKeysJson = json,
                    IsEnabled = dto.IsEnabled,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = updatedByUserId
                };

                await _context.MainCompanyFeatureSettings.AddAsync(setting);
            }
            else
            {
                setting.SelectedOptionKeysJson = json;
                setting.IsEnabled = dto.IsEnabled;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedByUserId = updatedByUserId;
            }

            await _context.SaveChangesAsync();
        }

        private static FeatureCatalogItem GetFeatureOrThrow(string featureKey)
        {
            var feature = FeatureCatalog.Items
                .FirstOrDefault(x => x.FeatureKey == featureKey);

            if (feature == null)
                throw new ArgumentException($"Unknown feature key: {featureKey}");

            return feature;
        }

        private static void ValidateSelectedOptions(
            FeatureCatalogItem feature,
            List<string> selectedOptionKeys)
        {
            if (selectedOptionKeys == null || selectedOptionKeys.Count == 0)
                throw new ArgumentException("At least one option must be selected.");

            var distinctOptions = selectedOptionKeys.Distinct().ToList();

            if (feature.SelectionType == FeatureCatalog.SingleChoice && distinctOptions.Count > 1)
                throw new ArgumentException($"{feature.FeatureName} accepts only one selected option.");

            var validOptionKeys = feature.Options
                .Select(x => x.OptionKey)
                .ToHashSet();

            var invalidOptions = distinctOptions
                .Where(x => !validOptionKeys.Contains(x))
                .ToList();

            if (invalidOptions.Any())
                throw new ArgumentException($"Invalid option(s): {string.Join(", ", invalidOptions)}");
        }

        private static List<string> DeserializeOptionKeys(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}