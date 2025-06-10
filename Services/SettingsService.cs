using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using KRGPMagic.Core.Services;
using System.Text.Json.Serialization;
using System.Xml;

namespace KRGPMagic.Plugins.FrameConnector.Services
{
    // Сервис для сохранения и загрузки настроек плагина
    public class SettingsService
    {
        #region Fields

        private readonly IPathService _pathService;
        private const string SETTINGS_FILENAME = "FrameConnectorSettings.json";
        private const string PLUGIN_NAME = "FrameConnector";

        #endregion

        #region Constructor

        public SettingsService(IPathService pathService)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        #endregion

        #region Public Methods

        // Сохраняет настройки группировки
        public void SaveGroupingSettings(string groupingParameter, Dictionary<string, string> connectorTypeMappings)
        {
            try
            {
                var settings = new PluginSettings
                {
                    LastGroupingParameter = groupingParameter,
                    ConnectorTypeMappings = connectorTypeMappings ?? new Dictionary<string, string>()
                };

                var json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
                var filePath = _pathService.GetPluginUserDataFilePath(PLUGIN_NAME, SETTINGS_FILENAME);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Игнорируем ошибки сохранения настроек
            }
        }

        // Загружает сохраненные настройки
        public PluginSettings LoadSettings()
        {
            try
            {
                var filePath = _pathService.GetPluginUserDataFilePath(PLUGIN_NAME, SETTINGS_FILENAME);
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        return JsonConvert.DeserializeObject<PluginSettings>(json) ?? new PluginSettings();
                    }
                }
            }
            catch
            {
                // При ошибке возвращаем настройки по умолчанию
            }

            return new PluginSettings();
        }

        // Очищает все сохраненные настройки
        public void ClearSettings()
        {
            try
            {
                var filePath = _pathService.GetPluginUserDataFilePath(PLUGIN_NAME, SETTINGS_FILENAME);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Игнорируем ошибки при удалении настроек
            }
        }

        #endregion
    }

    #region Settings Model

    // Модель настроек плагина
    public class PluginSettings
    {
        public string LastGroupingParameter { get; set; } = string.Empty;
        public Dictionary<string, string> ConnectorTypeMappings { get; set; } = new Dictionary<string, string>();
    }

    #endregion
}
