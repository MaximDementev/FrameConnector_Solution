using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KRGPMagic.Core.Interfaces;
using KRGPMagic.Core.Models;
using KRGPMagic.Core.Services;
using KRGPMagic.Plugins.FrameConnector.Services;
using KRGPMagic.Plugins.FrameConnector.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KRGPMagic.Plugins.FrameConnector.Commands
{
    // Команда для группировки каркасов и создания соединителей
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FrameConnectorCommand : IExternalCommand, IPlugin
    {
        #region Fields

        private ElementSelectionService _selectionService;
        private ParameterMappingService _parameterService;
        private ConnectorCreationService _connectorService;
        private ConnectorTypeService _connectorTypeService;

        #endregion

        #region IPlugin Implementation

        public PluginInfo Info { get; set; }
        public bool IsEnabled { get; set; }

        // Выполняет внутреннюю инициализацию плагина
        public bool Initialize()
        {
            return true;
        }

        // Освобождает ресурсы
        public void Shutdown()
        {
        }

        #endregion

        #region IExternalCommand Implementation

        // Точка входа для выполнения команды группировки каркасов
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Получение сервисов от KRGPMagic
                var pathService = KRGPMagicServiceProvider.GetService<IPathService>();
                var initializationService = KRGPMagicServiceProvider.GetService<IPluginInitializationService>();

                if (pathService == null || initializationService == null)
                {
                    TaskDialog.Show("Ошибка", "Не удалось получить сервисы KRGPMagic. Проверьте установку системы.");
                    return Result.Failed;
                }

                // Проверка готовности плагина
                if (!initializationService.IsPluginReady("FrameConnector"))
                {
                    if (!initializationService.InitializePlugin("FrameConnector"))
                    {
                        TaskDialog.Show("Ошибка", "Плагин не готов к работе. Проверьте установку.");
                        return Result.Failed;
                    }
                }

                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Инициализация сервисов
                _selectionService = new ElementSelectionService(doc);
                _parameterService = new ParameterMappingService(doc);
                _connectorService = new ConnectorCreationService(doc);
                _connectorTypeService = new ConnectorTypeService(doc);

                // Выбор каркасов пользователем
                var selectedFrames = _selectionService.SelectFramesByRectangle(uiDoc);
                if (selectedFrames == null || !selectedFrames.Any())
                {
                    TaskDialog.Show("Предупреждение", "Не выбраны каркасы колонн для обработки.");
                    return Result.Cancelled;
                }

                // Открытие окна группировки
                var groupingForm = new FrameGroupingForm(selectedFrames, doc, _connectorTypeService, pathService);
                var dialogResult = groupingForm.ShowDialog();

                if (dialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    // Получение результатов группировки
                    var groupedFrames = groupingForm.GroupedFrames;
                    var connectorMappings = groupingForm.ConnectorMappings;

                    // Создание соединителей
                    using (Transaction trans = new Transaction(doc, "Создание соединителей каркасов"))
                    {
                        trans.Start();

                        int createdCount = 0;
                        foreach (var group in groupedFrames)
                        {
                            if (connectorMappings.ContainsKey(group.Key) &&
                                connectorMappings[group.Key] != null)
                            {
                                var connectorType = connectorMappings[group.Key];

                                foreach (var frame in group.Value)
                                {
                                    var connector = _connectorService.CreateConnector(frame, connectorType);
                                    if (connector != null)
                                    {
                                        _parameterService.TransferParameters(frame, connector);
                                        createdCount++;
                                    }
                                }
                            }
                        }

                        trans.Commit();
                        TaskDialog.Show("Успех", $"Создано соединителей: {createdCount} из {selectedFrames.Count} каркасов.");
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Ошибка", $"Ошибка при выполнении команды: {ex.Message}");
                return Result.Failed;
            }
        }

        #endregion
    }
}
