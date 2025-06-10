using Autodesk.Revit.DB;
using System;

namespace KRGPMagic.Plugins.FrameConnector.Services
{
    // Сервис для перемещения соединителей
    public class ConnectorMovementService
    {
        #region Fields

        private readonly Document _document;

        #endregion

        #region Constructor

        public ConnectorMovementService(Document document)
        {
            _document = document;
        }

        #endregion

        #region Public Methods

        // Перемещает соединитель в позицию каркаса
        public void MoveConnectorToFrame(FamilyInstance connector, FamilyInstance frame)
        {
            try
            {
                var connectorLocation = connector.Location as LocationPoint;
                var frameLocation = frame.Location as LocationPoint;

                if (connectorLocation == null || frameLocation == null)
                    throw new Exception("Не удалось получить позиции элементов");

                var currentPosition = connectorLocation.Point;
                var targetPosition = frameLocation.Point;
                var translation = targetPosition - currentPosition;

                // Перемещаем соединитель
                ElementTransformUtils.MoveElement(_document, connector.Id, translation);

                // Синхронизируем поворот
                var frameRotation = frameLocation.Rotation;
                var connectorRotation = connectorLocation.Rotation;
                var rotationDifference = frameRotation - connectorRotation;

                if (Math.Abs(rotationDifference) > 1e-6)
                {
                    var axis = Line.CreateBound(targetPosition, targetPosition + XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(_document, connector.Id, axis, rotationDifference);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при перемещении соединителя: {ex.Message}");
            }
        }

        #endregion
    }
}
