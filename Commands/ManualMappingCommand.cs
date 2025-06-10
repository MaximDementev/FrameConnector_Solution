using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using KRGPMagic.Core.Interfaces;
using KRGPMagic.Core.Models;
using KRGPMagic.Core.Services;
using KRGPMagic.Plugins.FrameConnector.Services;
using System;

namespace KRGPMagic.Plugins.FrameConnector.Commands
{
    // Команда для ручного сопоставления каркаса и соединителя
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ManualMappingCommand : IExternalCommand, IPlugin
    {
        #region Fields

        private ElementSelectionService _selectionService;
        private ParameterMappingService _parameterService;
        private ConnectorMovementService _movementService;

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

        // Точка входа для выполнения ручного сопоставления
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Получение сервисов от KRGPMagic
                var initializationService = KRGPMagicServiceProvider.GetService<IPluginInitializationService>();

                if (initializationService == null)
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
                _movementService = new ConnectorMovementService(doc);

                TaskDialog.Show("Информация",
                    "Режим ручного сопоставления активирован.\n" +
                    "Последовательно выбирайте каркас и соединитель.\n" +
                    "Для завершения нажмите Esc при выборе элемента.");

                // Цикл выбора элементов
                int processedCount = 0;
                while (true)
                {
                    try
                    {
                        // Выбор каркаса
                        var frame = _selectionService.SelectSingleFrame(uiDoc);
                        if (frame == null)
                            break;

                        // Выбор соединителя
                        var connector = _selectionService.SelectSingleConnector(uiDoc);
                        if (connector == null)
                            break;

                        // Обработка выбранных элементов
                        using (Transaction trans = new Transaction(doc, "Ручное сопоставление каркаса и соединителя"))
                        {
                            trans.Start();

                            // Перемещение соединителя к каркасу
                            _movementService.MoveConnectorToFrame(connector, frame);

                            // Передача параметров
                            _parameterService.TransferParameters(frame, connector);

                            trans.Commit();
                        }

                        processedCount++;
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        // Пользователь нажал Esc
                        break;
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Ошибка", $"Ошибка при обработке элементов: {ex.Message}");
                        continue;
                    }
                }

                if (processedCount > 0)
                {
                    TaskDialog.Show("Завершено", $"Обработано пар каркас-соединитель: {processedCount}");
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
