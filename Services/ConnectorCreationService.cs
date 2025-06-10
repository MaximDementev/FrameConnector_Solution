using Autodesk.Revit.DB;
using System;

namespace KRGPMagic.Plugins.FrameConnector.Services
{
    // Сервис для создания экземпляров соединителей
    public class ConnectorCreationService
    {
        #region Fields

        private readonly Document _document;

        #endregion

        #region Constructor

        public ConnectorCreationService(Document document)
        {
            _document = document;
        }

        #endregion

        #region Public Methods

        // Создает экземпляр соединителя в том же месте, где находится каркас
        public FamilyInstance CreateConnector(FamilyInstance frame, FamilySymbol connectorType)
        {
            try
            {
                // Активируем тип семейства, если он не активен
                if (!connectorType.IsActive)
                {
                    connectorType.Activate();
                }

                // Получаем позицию и ориентацию каркаса
                var location = frame.Location as LocationPoint;
                if (location == null)
                    throw new Exception("Не удалось получить позицию каркаса");

                var position = location.Point;
                var rotation = location.Rotation;

                // Создаем экземпляр соединителя
                var connector = _document.Create.NewFamilyInstance(
                    position,
                    connectorType,
                    Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                // Применяем поворот, если он есть
                if (Math.Abs(rotation) > 1e-6)
                {
                    var axis = Line.CreateBound(position, position + XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(_document, connector.Id, axis, rotation);
                }

                return connector;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при создании соединителя: {ex.Message}");
            }
        }

        #endregion
    }
}
