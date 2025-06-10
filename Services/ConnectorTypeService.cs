using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace KRGPMagic.Plugins.FrameConnector.Services
{
    // Сервис для работы с типами соединителей
    public class ConnectorTypeService
    {
        #region Fields

        private readonly Document _document;
        private const string CONNECTOR_FAMILY_PATTERN = "KRGP_СБ";

        #endregion

        #region Constructor

        public ConnectorTypeService(Document document)
        {
            _document = document;
        }

        #endregion

        #region Public Methods

        // Получает все типы соединителей в проекте с их именами
        public Dictionary<string, FamilySymbol> GetConnectorTypesWithNames()
        {
            var result = new Dictionary<string, FamilySymbol>();

            var collector = new FilteredElementCollector(_document)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_GenericModel);

            var connectorTypes = collector
                .Cast<FamilySymbol>()
                .Where(fs => fs.FamilyName.Contains(CONNECTOR_FAMILY_PATTERN))
                .ToList();

            foreach (var connectorType in connectorTypes)
            {
                var displayName = $"{connectorType.FamilyName}: {connectorType.Name}";
                result[displayName] = connectorType;
            }

            return result;
        }

        // Находит тип соединителя по отображаемому имени
        public FamilySymbol FindConnectorTypeByDisplayName(string displayName)
        {
            var connectorTypes = GetConnectorTypesWithNames();
            return connectorTypes.ContainsKey(displayName) ? connectorTypes[displayName] : null;
        }

        #endregion
    }
}
