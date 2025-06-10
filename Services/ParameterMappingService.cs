using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KRGPMagic.Plugins.FrameConnector.Services
{
    // Сервис для передачи параметров между каркасами и соединителями
    public class ParameterMappingService
    {
        #region Fields

        private readonly Document _document;
        private readonly List<string> _frameParameters;
        private const string NESTED_FAMILY_NAME_PATTERN = "ВБ_";
        private const string TARGET_NESTED_FAMILY = "KRGP_СБ_Крайние стержни с большим диаметром";
        private const string SOURCE_PARAMETER = "Средние_Диаметр арматуры";

        #endregion

        #region Constructor

        public ParameterMappingService(Document document)
        {
            _document = document;
            _frameParameters = InitializeParameterList();
        }

        #endregion

        #region Public Methods

        // Передает параметры из каркаса в соединитель
        public void TransferParameters(FamilyInstance frame, FamilyInstance connector)
        {
            try
            {
                // Передача основных параметров
                TransferMainParameters(frame, connector);

                // Передача параметров из вложенных семейств
                TransferNestedFamilyParameters(frame, connector);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при передаче параметров: {ex.Message}");
            }
        }

        // Получает список всех параметров элемента для группировки
        public List<string> GetElementParameters(FamilyInstance element)
        {
            var parameters = new List<string>();

            // Параметры экземпляра
            foreach (Parameter param in element.Parameters)
            {
                if (param.HasValue && !param.IsReadOnly)
                {
                    parameters.Add(param.Definition.Name);
                }
            }

            // Параметры типа
            var elementType = _document.GetElement(element.GetTypeId());
            if (elementType != null)
            {
                foreach (Parameter param in elementType.Parameters)
                {
                    if (param.HasValue && !param.IsReadOnly &&
                        !parameters.Contains(param.Definition.Name))
                    {
                        parameters.Add(param.Definition.Name);
                    }
                }
            }

            return parameters.OrderBy(p => p).ToList();
        }

        #endregion

        #region Private Methods

        // Инициализирует список параметров для передачи
        private List<string> InitializeParameterList()
        {
            return new List<string>
            {
                "Количество_Длина",
                "Количество_Ширина",
                "Колонна_Длина",
                "Колонна_Ширина",
                "Колонна_Высота",
                "ВБ_СмещениеСнизу",
                "ВБ_РазбежкаСнизу",
                "ВБ_СмещениеСверху",
                "ВБ_РазбежкаСверху",
                "Расстояние от грани колонны до центра стержня",
                "ВБ_Диаметр арматуры",
                "ADSK_Марка изделия",
                "ADSK_Категория основы",
                "ADSK_Метка основы",
                "ADSK_Количество основы"
            };
        }

        // Передает основные параметры каркаса
        private void TransferMainParameters(FamilyInstance frame, FamilyInstance connector)
        {
            foreach (string paramName in _frameParameters)
            {
                try
                {
                    var sourceParam = GetParameterByName(frame, paramName);
                    var targetParam = GetParameterByName(connector, paramName);

                    if (sourceParam != null && targetParam != null && sourceParam.HasValue)
                    {
                        CopyParameterValue(sourceParam, targetParam);
                    }
                }
                catch (Exception ex)
                {
                    // Продолжаем обработку других параметров
                    continue;
                }
            }
        }

        // Передает параметры из вложенных семейств
        private void TransferNestedFamilyParameters(FamilyInstance frame, FamilyInstance connector)
        {
            try
            {
                var nestedInstances = GetNestedFamilyInstances(frame);
                var targetNestedInstance = GetNestedFamilyInstance(connector, TARGET_NESTED_FAMILY);

                if (targetNestedInstance != null)
                {
                    foreach (var nestedInstance in nestedInstances)
                    {
                        var sourceParam = GetParameterByName(nestedInstance, SOURCE_PARAMETER);
                        var targetParam = GetParameterByName(targetNestedInstance, SOURCE_PARAMETER);

                        if (sourceParam != null && targetParam != null && sourceParam.HasValue)
                        {
                            CopyParameterValue(sourceParam, targetParam);
                            break; // Берем значение из первого найденного вложенного семейства
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ошибки при работе с вложенными семействами не критичны
            }
        }

        // Получает параметр элемента по имени
        private Parameter GetParameterByName(Element element, string parameterName)
        {
            // Сначала ищем в параметрах экземпляра
            var instanceParam = element.LookupParameter(parameterName);
            if (instanceParam != null)
                return instanceParam;

            // Затем ищем в параметрах типа
            var typeId = element.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                var typeElement = _document.GetElement(typeId);
                return typeElement?.LookupParameter(parameterName);
            }

            return null;
        }

        // Копирует значение из одного параметра в другой
        private void CopyParameterValue(Parameter source, Parameter target)
        {
            if (source.StorageType != target.StorageType)
                return;

            switch (source.StorageType)
            {
                case StorageType.String:
                    target.Set(source.AsString());
                    break;
                case StorageType.Integer:
                    target.Set(source.AsInteger());
                    break;
                case StorageType.Double:
                    target.Set(source.AsDouble());
                    break;
                case StorageType.ElementId:
                    target.Set(source.AsElementId());
                    break;
            }
        }

        // Получает вложенные семейства каркаса
        private List<FamilyInstance> GetNestedFamilyInstances(FamilyInstance parentInstance)
        {
            var nestedInstances = new List<FamilyInstance>();

            try
            {
                var subComponents = parentInstance.GetSubComponentIds();
                foreach (var id in subComponents)
                {
                    var element = _document.GetElement(id);
                    if (element is FamilyInstance fi &&
                        fi.Symbol.FamilyName.Contains(NESTED_FAMILY_NAME_PATTERN))
                    {
                        nestedInstances.Add(fi);
                    }
                }
            }
            catch
            {
                // Возвращаем пустой список при ошибке
            }

            return nestedInstances;
        }

        // Получает конкретное вложенное семейство по имени
        private FamilyInstance GetNestedFamilyInstance(FamilyInstance parentInstance, string familyName)
        {
            try
            {
                var subComponents = parentInstance.GetSubComponentIds();
                foreach (var id in subComponents)
                {
                    var element = _document.GetElement(id);
                    if (element is FamilyInstance fi && fi.Symbol.FamilyName == familyName)
                    {
                        return fi;
                    }
                }
            }
            catch
            {
                // Возвращаем null при ошибке
            }

            return null;
        }

        #endregion
    }
}
